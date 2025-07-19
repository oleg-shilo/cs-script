using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using wdbg.cs_script;

namespace wdbg.Pages;

public partial class CodeMirrorPage : ComponentBase, IDisposable
{
    // need to initialize element-bound objects to allow the initial rendering. Then these objects will be reset to the real ones in OnAfterRenderAsync
    public Document Document = new();

    public Ide Editor = new();
    public UINotificationService UIEvents = new();
    public DbgSession DebugSession;
    public UserSession UserSession;

    int maxRecentItemsCount = 10;
    int currentOutputIndex = -1;

    private bool _isUpdating = false;

    void StateHasChangedSafeOptimized()
    {
        if (_isUpdating) return;

        _isUpdating = true;
        InvokeAsync(async () =>
        {
            await Task.Delay(10); // Small delay to batch updates
            StateHasChanged();
            _isUpdating = false;
        });
    }

    public void Dispose()
    {
        try
        {
            UIEvents.OnChange -= StateHasChangedSafe;
            UIEvents.OnDbgChange -= DebugStateHasChanged;
            UIEvents.OnObjectValue -= DebugObjectStateChanged;
        }
        catch (Exception e) { e.Log(); }
    }

    // void StateHasChangedSafe() => InvokeAsync(StateHasChanged); // Ensure StateHasChanged is called on the UI thread

    void ClearLocals()
    {
        Variables.Clear();
        WatchVariables.ForEach(x => x.Reset());
    }

    public void RequestSelectedVariablesInfo(bool locals, bool watch)
    {
        // This method is called to request the evaluation of the selected watch variable
        if (watch)
            if (selectedWatchName.HasText())
            {
                DebugSession.RequestObjectInfo(selectedWatchName);
            }

        // repeat for locals
        if (locals)
            if (selectedLocalName.HasText())
            {
                DebugSession.RequestObjectInfo(selectedLocalName);
            }
    }

    public void DebugStateHasChanged(string variables, string callStack) // received on breakpoint hit
    {
        InvokeAsync(async () =>
        {
            try
            {
                if (callStack.HasText())
                {
                    CallStack.Clear();
                    CallStack.AddRange(callStack.Split('|'));
                }

                if (DebugSession.StackFrameFileName != Editor.LoadedDocument)
                {
                    await setCurrentStepLine(-1);
                    await LoadDocFile(DebugSession.StackFrameFileName);
                }

                _ = setCurrentStepLine(DebugSession.CurrentStepLineNumber);

                if (DebugSession.IsScriptExecutionInProgress)
                {
                    if (DebugSession.CurrentStepLineNumber != -1)
                        _ = ScrollLineToView(DebugSession.CurrentStepLineNumber);

                    var prevSelectefLocalName = selectedLocalName; // save it before Variables are updated

                    if (variables.HasText())
                        Variables = JsonSerializer.Deserialize<List<VariableInfo>>(variables);

                    // set selectedLocalIndex to the index of Variables item with the prevSelectefLocalName
                    selectedLocalIndex = Variables.FindIndex(item => item.Name == prevSelectefLocalName);

                    WatchVariables.ForEach(expression => DebugSession.RequestEvaluate(expression.Name));
                    RequestSelectedVariablesInfo(locals: true, watch: true);
                }
            }
            catch (Exception e) { e.Log(); }
            StateHasChangedSafe();
        });
    }

    public void DebugObjectStateChanged(string data) // received on debug stack variable value evaluated
    {
        try
        {
            string variables = null;
            string objectInfo = null;

            if (data.StartsWith("variables:"))
                variables = data.Substring("variables:".Length).Trim();
            else if (data.StartsWith("objectInfo:"))
                objectInfo = data.Substring("objectInfo:".Length).Trim();

            if (variables.HasText())
            {
                var variablesInfo = JsonSerializer.Deserialize<List<VariableInfo>>(variables);

                foreach (var item in variablesInfo)
                {
                    var impactedVariables =
                        Variables.Where(x => x.Name == item.Name).Concat(
                        WatchVariables.Where(x => x.Name == item.Name)).ToList();

                    impactedVariables.ForEach(x =>
                        {
                            x.Type = item.Type;
                            x.Value = item.Value;
                        });
                }
            }
            else if (objectInfo.HasText())
            {
                var variableName = objectInfo.Split(':').FirstOrDefault()?.Trim();

                if (variableName == selectedLocalName)
                    selectedLocalValue = objectInfo;

                if (variableName == selectedWatchName)
                    selectedWatchValue = objectInfo;
            }
        }
        catch (Exception e) { e.Log(); }
        finally
        {
            StateHasChangedSafe();
        }
    }

    public async Task LoadDocFromServer()
    {
        try
        {
            if (Editor.LoadedDocument.HasText() && File.Exists(Editor.LoadedDocument))
            {
                bool isModified;
                (Document.EditorContent, isModified) = await Editor.State.GetContentFromFileOrCache(Editor.LoadedDocument);

                if (!Editor.State.HasInfoFor(Editor.LoadedDocument)) // the debug file is missing or corrupted
                {
                    _ = StartGeneratingDebugMetadata(true);
                }

                (Document.EditorContent, _) = Document.EditorContent.NormalizeLineBreaks();
                Document.Breakpoints = Editor.State.GetDocumentBreakpoints(Editor.LoadedDocument);

                Editor.DocLoadingInProgress = true;
                await SetDocumentContent(Document.EditorContent);
                Document.IsModified = isModified;
                // $"Document.IsModified = {Document.IsModified}".ProfileLog();
                // $"Editor.State.AnyScriptFilesModified:{Editor.State.AnyScriptFilesModified()}".ProfileLog();

                Editor.LastSessionFileName = Editor.LoadedScript;
                Editor.LocateLoadedScriptDebugInfo();
                Editor.RunStatus = "ready";
                Editor.Ready = true;
                Editor.AddToRecentFiles(Editor.LoadedScript);

                await RenderBreakpoints();
                if (DebugSession.IsInBreakMode && DebugSession.StackFrameFileName == Editor.LoadedDocument)
                    _ = setCurrentStepLine(DebugSession.CurrentStepLineNumber);
                UIEvents.NotifyStateChanged();
            }
            else
            {
                if (!File.Exists(Editor.LoadedScript))
                {
                    Editor.ShowToastError($"File '{Editor.LoadedScript}' cannot be found.");
                }
            }
        }
        catch (Exception e) { e.Log(); }
    }

    public async Task LoadScriptFromServer()
    {
        try
        {
            if (Editor.LoadedScript.HasText() && File.Exists(Editor.LoadedScript))
            {
                Editor.LoadedDocument = Editor.LoadedScript;

                await Editor.ReadSavedStateOf(Editor.LoadedScript);

                await LoadDocFromServer();

                if (!Editor.IsScriptReadyForDebugging)
                    _ = StartGeneratingDebugMetadata(false);
            }
            else
            {
                Editor.ShowToastError($"File '{Editor.LoadedScript}' cannot be found.");
            }
        }
        catch (Exception e) { e.Log(); }
    }

    public async Task LoadDocFile(string file, int lineNum = -1, int colNum = -1)
    {
        var currentDoc = Editor.LoadedDocument;
        Editor.State.UpdateState(Editor.LoadedDocument, Document, await GetUndoBuffer());

        try
        {
            if (file.HasText())
            {
                if (Editor.LoadedDocument != file)
                {
                    var content = await GetDocumentContent();
                    Editor.State.UpdateFor(Editor.LoadedDocument, content, Document.Breakpoints);
                }
                Editor.LoadedDocument = file;
                await LoadDocFromServer();
                UIEvents.NotifyStateChanged();

                if (lineNum != -1)
                {
                    _ = ScrollToAndHighlightLine(lineNum, colNum);
                }
                else
                {
                    var state = Editor.State.GetDocumentViewState(file);
                    // Debug.WriteLine($"read = file: {file.GetFileName()}({state.CaretLine}, {state.CaretCh})");

                    await SetCaretPosition(state.CaretPos);
                    _ = SetUndoBuffer(state.ChangeHistory);
                    await ScrollCurrentLineToView();
                }

                await FocusEditor();
            }
        }
        catch (Exception e) { e.Log(); }
        if (currentDoc != file)
            Editor.PreviousLoadedDocument = currentDoc; // save the previous document for the optimized version
    }

    public async Task SwapLastTabs()
    {
        if (Editor.PreviousLoadedDocument.HasText() && Editor.PreviousLoadedDocument != Editor.LoadedDocument)
            await LoadDocFile(Editor.PreviousLoadedDocument);
    }

    public async Task LoadRecentScriptFile(string file)
    {
        try
        {
            if (file.HasText())
            {
                if (Editor.LoadedScript.HasText() && Editor.LoadedScript != file)
                {
                    Document.Breakpoints = await Editor.SaveStateOf(Editor.LoadedScript);
                }

                Editor.LoadedScript = file;
                await LoadScriptFromServer();

                StateHasChanged();
                _ = AutoSizeFileNameInput(null);
            }
        }
        catch (Exception e) { e.Log(); }
    }

    public async Task OpenScriptFolder()
    {
        try
        {
            if (UserSession.IsLocalClient)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (Editor.LoadedScript.HasText() && File.Exists(Editor.LoadedScript))
                    {
                        // Open Explorer and select the file
                        var argument = $"/select,\"{Editor.LoadedScript}\"";
                        Process.Start(new ProcessStartInfo("explorer.exe", argument) { UseShellExecute = true });
                    }
                }
            }
        }
        catch (Exception e) { e.Log(); }
    }

    public async Task SaveToFileOnServer(bool showError)
    {
        try
        {
            if (Editor.AutoFormatOnSave)
                await OnFormatRequest();

            var content = await GetDocumentContent();
            (content, _) = content.NormalizeLineBreaks();

            if (Document.IsModified || Editor.State.AnyScriptFilesModified())
            {
                var isModified = Document.IsModified;
                Editor.State.UpdateFor(Editor.LoadedDocument, content, Document.Breakpoints);
                Document.Breakpoints = await Editor.SaveStateOf(Editor.LoadedScript);
                await File.WriteAllTextAsync(Editor.LoadedDocument, content);

                "Document.IsModified = false".ProfileLog();
                Document.IsModified = false;

                UIEvents.NotifyStateChanged();

                // this will save all other files of the script files that are modified but not in the active view
                var updatedFiles = Editor.State.SaveAllFilesIfModified();

                if (isModified || updatedFiles.Any())
                    _ = StartGeneratingDebugMetadata(showError);
            }
            else
            {
                // Optionally, show an error or prompt for a file path
            }

            // Debug.WriteLine(Document.Breakpoints.FirstOrDefault());
            await RenderBreakpoints();
        }
        catch (Exception e) { e.Log(); }
    }

    public void ClearOutput()
    {
        try
        {
            Editor.Output.Clear();
            currentOutputIndex = -1;
            StateHasChanged();
        }
        catch (Exception e) { e.Log(); }
    }

    public void AddOutputLine(string data)
    {
        lock (this)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                    return;

                // Normalize line endings
                data = data.Replace("\r\n", "\n").Replace('\r', '\n');

                // If the message starts with a single \r (carriage return), replace the last line
                if (data.StartsWith("\n") && Editor.Output.Count > 0)
                {
                    // Remove the first \n for processing
                    data = data.Substring(1);

                    var lines = data.Split('\n');
                    // Replace the last line with the first new line
                    Editor.Output[Editor.Output.Count - 1] = lines[0];
                    // Add any additional lines as new output
                    for (int i = 1; i < lines.Length; i++)
                    {
                        Editor.Output.Add(lines[i]);
                    }
                }
                else
                {
                    // Standard append for all other cases
                    foreach (var line in data.Split('\n'))
                    {
                        Editor.Output.Add(line);
                    }
                }
                StateHasChanged(); // it's speed critical so it's better than UIEvents.NotifyStateChanged()
                OutputScrollToEnd();
            }
            catch (Exception e) { e.Log(); }
        }
    }

    public void AddOutputChar(string data)
    {
        lock (this)
        {
            try
            {
                if (data == "\n")
                {
                    Editor.Output.Add("");
                    outputPosition = 0;
                }
                else if (data == "\r")
                {
                    outputPosition = 0;
                }
                else
                {
                    var lastLine = Editor.Output.LastOrDefault() ?? "";

                    if (outputPosition < lastLine.Length)
                        lastLine = data + lastLine.Substring(outputPosition + 1);
                    else
                        lastLine += data;

                    if (Editor.Output.Count == 0)
                        Editor.Output.Add(lastLine);
                    else
                        Editor.Output[Editor.Output.Count - 1] = lastLine;

                    outputPosition++;
                }

                StateHasChanged();
                OutputScrollToEnd();
            }
            catch (Exception e) { e.Log(); }
        }
    }

    [JSInvokable]
    public async Task UserUpdatedBreakpoints(int[] lines)
    {
        try
        {
            Editor.State.UpdateFor(Editor.LoadedDocument, null, lines);

            if (DebugSession.IsScriptExecutionInProgress)
            {
                await Editor.FetchLatestDebugInfo(Editor.LoadedScript); // this will get rid of invalid breakpoints in the state
                Document.Breakpoints = Editor.State.AllDocumentsBreakpoints[Editor.LoadedDocument];
                DebugSession.dbgScriptMaping = Editor.dbgScriptMaping;
                DebugSession.Breakpoints = Editor.AllEnabledBreakpoints;

                _ = RenderBreakpoints(); // this will remove invalid breakpoints from the view
            }
            else
            {
                Document.Breakpoints = lines;
                UIEvents.NotifyStateChanged();
            }
        }
        catch (Exception e) { e.Log(); }
    }

    public async Task StartGeneratingDebugMetadata(bool showNotification)
    {
        try
        {
            if (!Editor.Ready)
                return; // still busy preparing the debug info

            Editor.LoadedScriptDbg = null;
            Editor.RunStatus = "Generating debug metadata...";
            Editor.Ready = false;
            Editor.DebugGenerationError = null;
            UIEvents.NotifyStateChanged();

            if (!Editor.IsScriptReadyForDebugging)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Editor.ResetDbgGenerator();

                        Editor.LoadedScriptDbg = DbgService.Prepare(Editor.LoadedScript, process => Editor.DbgGenerator = process);

                        await Editor.FetchLatestDebugInfo(Editor.LoadedScript);
                        Document.Breakpoints = Editor.State.GetDocumentBreakpoints(Editor.LoadedDocument); // this will update the Document.Breakpoints with the new breakpoints
                        await RenderBreakpoints(); // this will update the UI with the new breakpoints
                    }
                    catch (Exception e)
                    {
                        var error = e.Message ?? "Generating debug information for the loaded script has failed.";

                        if (e.Message?.Contains("Error: Specified file could not be compiled.") == true)
                        {
                            Editor.DebugGenerationError =
                            error = "Generating debug metadata for the loaded script has failed. Check the script for the compile errors.";
                            UIEvents.NotifyStateChanged();
                        }

                        if (showNotification)
                            Editor.ShowToastError(error); // failures can happen (e.g. *.dbg.cs is still locked)

                        Editor.ResetDbgGenerator();
                        Editor.LoadedScriptDbg = null;
                    }

                    Document.Breakpoints = await Editor.SaveStateOf(Editor.LoadedScript);
                    Editor.RunStatus = $"Ready";
                    Editor.Ready = true;
                });
        }
        catch (Exception e) { e.Log(); }
    }

    bool syntaxCheckAvailable => DebugSession?.IsScriptExecutionInProgress == false;
    bool startDetachedlAvailable => DebugSession?.IsScriptExecutionInProgress == false;
    bool startAvailable => Editor.Ready && (DebugSession?.IsScriptExecutionInProgress == false || DebugSession?.IsInBreakMode == true);
    bool stepCommandsAvailable => Editor.Ready && (DebugSession?.IsScriptExecutionInProgress == false || DebugSession?.IsInBreakMode == true);
    bool pauseAvailable => Editor.Ready && DebugSession?.IsScriptExecutionInProgress == true && DebugSession?.IsInBreakMode == false;
    bool stopAvailable => Editor.Ready && DebugSession?.IsScriptExecutionInProgress == true;

    public async void OnSyntaxCheckClicked()
    {
        if (!syntaxCheckAvailable)
        {
            Editor.ShowToastInfo("The command is not available in the current state of the application.");
            return;
        }

        try
        {
            var scriptPath = Editor.LoadedScript;

            if (!scriptPath.HasText() || !File.Exists(scriptPath))
            {
                Editor.ShowToastError("Script file not found.");
            }
            else
            {
                if (Editor.State.AnyScriptFilesModified() || Document.IsModified)
                    await SaveToFileOnServer(false);

                ClearOutput();

                await CSScriptHost.Start(
                    script: scriptPath,
                    ngArgs: ["-check"], // Use the syntax check argument
                    onOutput: (proc, data) => InvokeAsync(() => AddOutputLine(data)));
            }
        }
        catch (Exception ex)
        {
            Editor.ShowToastError(ex.Message);
        }
    }

    public async void OnStartExternalClicked()
    {
        if (!startDetachedlAvailable)
        {
            Editor.ShowToastInfo("The command is not available in the current state of the application.");
            return;
        }

        try
        {
            var scriptPath = Editor.LoadedScript;

            if (!scriptPath.HasText() || !File.Exists(scriptPath))
            {
                Editor.ShowToastError("Script file not found.");
            }
            else
            {
                var result = Shell.StartProcessInTerminal("css", scriptPath.qt(), workingDir: Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory);

                Editor.ShowToastSuccess(result);
            }
        }
        catch (Exception ex)
        {
            Editor.ShowToastError($"Failed to start in terminal: {ex.Message}");
        }
    }

    public async void OnStartAndPauseClicked() => Start(breakAtStart: true, detached: false);

    public async void OnStartClicked()
    {
        if (!startAvailable)
            Editor.ShowToastInfo("The command is not available in the current state of the application.");
        else
            Start(breakAtStart: false, detached: false);
    }

    async void Start(bool breakAtStart, bool detached)
    {
        try
        {
            if (DebugSession.IsScriptExecutionInProgress)
            {
                if (DebugSession.IsInBreakMode)
                {
                    DebugSession.RequestResume();
                    UIEvents.NotifyDbgChanged(); // to clear current step
                }
                else
                    Editor.ShowToastError("Script is already running. Please stop it before starting a new one.");
            }
            else
            {
                if (Document.IsModified || !Editor.IsScriptReadyForDebugging || Editor.State.AnyScriptFilesModified())
                {
                    await SaveToFileOnServer(true); // this will trigger debugger info generation, which will be done when Editor.Ready istrue
                    if (!detached)
                        while (!Editor.IsScriptReadyForDebugging)
                            await Task.Delay(500);
                }

                Document.Breakpoints = await Editor.SaveStateOf(Editor.LoadedScript);

                _ = RenderBreakpoints();

                ClearOutput();
                ClearLocals();
                this.DebugSession.Reset();
                this.DebugSession.Breakpoints = Editor.AllEnabledBreakpoints; // not State.AllDocumentsBreakpoints that is not mapped to the decorated scripts
                this.DebugSession.dbgScriptMaping = Editor.dbgScriptMaping;

                try
                {
                    var scriptToExecute = detached ? Editor.LoadedScript : Editor.LoadedScriptDbg;

                    if (!scriptToExecute.HasText() || !File.Exists(scriptToExecute))
                    {
                        Editor.ShowToastError("File not found.");
                        return;
                    }

                    await CSScriptHost.Start(
                        scriptToExecute, null, null,
                        onStart: proc =>
                        {
                            DebugSession.RunningScript = proc;
                            Editor.RunStatus = $"execution started (pid: {proc.Id})";
                        },
                        onExit: () => InvokeAsync(() =>
                        {
                            Editor.RunStatus = "execution completed";
                            if (DebugSession.RunningScript != null)
                            {
                                var proc = DebugSession.RunningScript;
                                AddOutputLine($"> Script (pid: {proc.Id}) has exited with code {proc.ExitCode} (0x{proc.ExitCode:X}).");
                            }
                            DebugSession.Reset();
                        }),
                        onOutput: (proc, data) => InvokeAsync(() =>
                        {
                            if (Editor.OutputCharMode)
                                AddOutputChar(data);
                            else
                                AddOutputLine(data);
                        }),
                        onError: error => Editor.ShowToastError(error),
                        DebugSession.Id,
                        Editor.OutputCharMode,
                        envars: breakAtStart ?
                                    [("EntryScript", Editor.LoadedScript), ("pauseOnStart", "true")] :
                                    [("EntryScript", Editor.LoadedScript)]);
                }
                catch (Exception ex)
                {
                    Editor.ShowToastError(ex.Message);
                }
            }
        }
        catch (Exception e) { e.Log(); }
    }

    public void OnStopClicked()
    {
        if (!stopAvailable)
        {
            Editor.ShowToastInfo("The command is not available in the current state of the application.");
            return;
        }

        if (DebugSession.IsScriptExecutionInProgress)
            try
            {
                using (DebugSession.RunningScript)
                {
                    var id = DebugSession.RunningScript.Id;
                    DebugSession.RunningScript.Kill(); // This will stop the script execution
                    DebugSession.Reset();
                    AddOutputLine($"> Script (pid: {id}) has been terminated.");
                }
            }
            catch (Exception ex)
            {
                Editor.ShowToastError($"Failed to stop script: {ex.Message}");
            }
    }

    public void OnPauseClicked()
    {
        try
        {
            if (!pauseAvailable)
                Editor.ShowToastInfo("The command is not available in the current state of the application.");
            else
                DebugSession.RequestPause(); // no need to to clear current step as we are not in the break mode
                                             // Editor.ShowToastError($"Pausing Script is not implemented by DbgHost");
        }
        catch (Exception e) { e.Log(); }
    }

    public void OnStepOverClicked(MouseEventArgs args)
    {
        OnStepClicked(DebugSession.RequestStepOver);
    }

    public void OnStepIntoClicked(MouseEventArgs args)
    {
        OnStepClicked(DebugSession.RequestStepIn);
    }

    public void OnStepOutClicked(MouseEventArgs args)
    {
        OnStepClicked(DebugSession.RequestStepOut);
    }

    public void OnStepClicked(Action command)
    {
        try
        {
            if (!stepCommandsAvailable)
            {
                Editor.ShowToastInfo("The command is not available in the current state of the application.");
                return;
            }

            if (DebugSession.IsScriptExecutionInProgress)
            {
                command();
                UIEvents.NotifyDbgChanged(); // to clear current step
            }
            else
            {
                OnStartAndPauseClicked();
            }
        }
        catch (Exception e) { e.Log(); }
    }

    string newWatchExpression = "";

    void AddWatchVariable()
    {
        try
        {
            var expression = newWatchExpression?.Trim();

            if (!expression.HasText())
                return;

            // Prevent duplicates
            if (WatchVariables.Any(v => v.Name == expression))
            {
                Editor.ShowToastInfo("Watch already exists.");
                return;
            }

            // Add with placeholder value; in a real app, trigger evaluation here
            WatchVariables.Add(VariableInfo.New(expression));
            newWatchExpression = "";
            // selectedWatchIndex = WatchVariables.Count - 1;
            UIEvents.NotifyStateChanged();

            DebugSession.RequestEvaluate(expression);  // send the request to evaluate and return the value
        }
        catch (Exception e) { e.Log(); }
    }

    void RemoveWatchVariable(int index)
    {
        try
        {
            if (index >= 0 && index < WatchVariables.Count)
            {
                WatchVariables.RemoveAt(index);
                if (selectedWatchIndex >= WatchVariables.Count)
                    selectedWatchIndex = WatchVariables.Count - 1;
                UIEvents.NotifyStateChanged();
            }
        }
        catch (Exception e) { e.Log(); }
    }

    public void GoToNextCompileError()
    {
        try
        {
            if (Editor.Output.Count == 0)
                return;

            for (int i = 0; i < Editor.Output.Count; i++)
            {
                currentOutputIndex = (currentOutputIndex + 1) % Editor.Output.Count;
                if (OnOutputLineClick(currentOutputIndex))
                    break;
            }
        }
        catch (Exception e) { e.Log(); }
        StateHasChangedSafe();
    }
}