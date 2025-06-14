using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
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

    public void Dispose()
    {
        UIEvents.OnChange -= StateHasChangedSafe;
        UIEvents.OnDbgChange -= DebugStateHasChanged;
        UIEvents.OnObjectValue -= DebugObjectStateChanged;
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

    async Task CheckIfModified()
    {
        var content = await GetDocumentContent(); // very current content
        (content, _) = content.NormalizeLineBreaks();
        Document.IsModified = content != Document.LastSavedContent;
        UIEvents.NotifyStateChanged();
    }

    public void DebugStateHasChanged(string variables) // received on breakpoint hit
    {
        InvokeAsync(() =>
        {
            setCurrentStepLine(DebugSession.CurrentStepLineNumber);

            if (DebugSession.IsScriptExecutionInProgress)
            {
                if (DebugSession.CurrentStepLineNumber != -1)
                    ScrollLineToView(DebugSession.CurrentStepLineNumber);

                var prevSelectefLocalName = selectedLocalName; // save it before Variables are updated

                if (variables.HasText())
                    Variables = JsonSerializer.Deserialize<List<VariableInfo>>(variables);

                // set selectedLocalIndex to the index of Variables item with the prevSelectefLocalName
                selectedLocalIndex = Variables.FindIndex(item => item.Name == prevSelectefLocalName);

                WatchVariables.ForEach(expression => DebugSession.RequestEvaluate(expression.Name));
                RequestSelectedVariablesInfo(locals: true, watch: true);
            }

            StateHasChangedSafe();
        });
    }

    public void DebugObjectStateChanged(string data) // received on debug stack variable value evaluated
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

        StateHasChangedSafe();
    }

    public async Task LoadFileFromServer()
    {
        if (Editor.LoadedScript.HasText() && File.Exists(Editor.LoadedScript))
        {
            Document.EditorContent = await File.ReadAllTextAsync(Editor.LoadedScript);

            (Document.EditorContent, _) = Document.EditorContent.NormalizeLineBreaks();

            await SetDocumentContent(Document.EditorContent);

            Document.LastSavedContent = Document.EditorContent;
            Document.IsModified = false;

            Editor.LastSessionFileName = Editor.LoadedScript;
            Editor.LocateLoadedScriptDebuggInfo();
            Editor.RunStatus = "ready";
            Editor.Ready = true;
            Editor.AddToRecentFiles(Editor.LoadedScript);

            await LoadBreakpoints();
            UIEvents.NotifyStateChanged();

            if (!Editor.IsScriptReadyForDebugging)
                StartGeneratingDebugMetadata(false);
        }
        else
        {
            // Optionally show an error message
        }
    }

    public async Task LoadRecentFile(string file)
    {
        if (file.HasText())
        {
            Editor.LoadedScript = file;
            await LoadFileFromServer();
            StateHasChanged();
            AutoSizeFileNameInput(null);
        }
    }

    public async Task OpenScriptFolder()
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

    public async Task SaveToFileOnServer(bool showError)
    {
        var content = await GetDocumentContent();
        (content, _) = content.NormalizeLineBreaks();

        if (Editor.LoadedScript.HasText() && File.Exists(Editor.LoadedScript))
        {
            File.WriteAllTextAsync(Editor.LoadedScript, content);

            var currentBreakpoints = await GetBreakpoints();
            await UpdateBreakpoints(currentBreakpoints);

            Document.LastSavedContent = content; // Track saved content
            UIEvents.NotifyStateChanged();
            await CheckIfModified();

            if (!Editor.IsScriptReadyForDebugging)
                StartGeneratingDebugMetadata(showError);
        }
        else
        {
            // Optionally, show an error or prompt for a file path
        }
    }

    public void ClearOutput()
    {
        Editor.Output.Clear();
        currentOutputIndex = -1;
        StateHasChanged();
    }

    public void AddOutputLine(string data)
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

    public void AddOutputChar(string data)
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

    public async Task SaveBreakpoints()
    {
        await Editor.Storage.Write("breakpoints", Document.Breakpoints.ToArray());
    }

    public async Task LoadBreakpoints()
    {
        try
        {
            var bpList = await Editor.Storage.Read<string>("breakpoints");

            if (bpList.HasText())
            {
                var lines = bpList.Split(',').Select(int.Parse).ToArray();
                Document.Breakpoints = lines?.ToHashSet() ?? new();
            }
            else
            {
                Document.Breakpoints = new();
                // No breakpoints saved
            }
            await RenderBreakpoints();
            UIEvents.NotifyStateChanged();
        }
        catch { } // Ignore errors in loading breakpoints, e.g. if the storage is empty or corrupted
    }

    [JSInvokable]
    public Task UpdateBreakpoints(int[] lines)
    {
        Document.Breakpoints = lines.ToHashSet();
        DebugSession.Breakpoints = lines.ToList(); // <-- Synchronize with DebugSession
        UIEvents.NotifyStateChanged();
        SaveBreakpoints();
        return Task.CompletedTask;
    }

    public async Task StartGeneratingDebugMetadata(bool showNotification)
    {
        if (!Editor.Ready)
            return; // still busy preparing the debug info

        Editor.LoadedScriptDbg = null;
        Editor.RunStatus = "Generating debug metadata...";
        Editor.Ready = false;
        Editor.DebugGenerationError = null;
        UIEvents.NotifyStateChanged();

        var ttt = Editor.LoadedScript.HasText() && Editor.LoadedScriptDbg.HasText() &&
            File.Exists(Editor.LoadedScript) && File.Exists(Editor.LoadedScript) &&
            File.GetLastWriteTimeUtc(Editor.LoadedScriptDbg) == File.GetLastWriteTimeUtc(Editor.LoadedScript);

        if (!Editor.IsScriptReadyForDebugging)
            Task.Run(() =>
            {
                try
                {
                    Editor.ResetDbgGenerator();

                    (Editor.LoadedScriptDbg, Editor.LoadedScriptValidBreakpoints) = DbgService.Prepare(Editor.LoadedScript,
                        p => Editor.DbgGenerator = p);
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

                Editor.RunStatus = $"Ready";
                Editor.Ready = true;
            });
    }

    bool syntaxCheckAvailable => DebugSession?.IsScriptExecutionInProgress == false;
    bool startDetachedlAvailable => DebugSession?.IsScriptExecutionInProgress == false;
    bool startAvailable => Editor.Ready && (DebugSession?.IsScriptExecutionInProgress == false || DebugSession?.IsInBreakMode == true);
    bool stepCommandsAvailable => Editor.Ready && (DebugSession?.IsScriptExecutionInProgress == false || DebugSession?.IsInBreakMode == true);
    bool pauseAvailable => Editor.Ready && DebugSession?.IsScriptExecutionInProgress == true && DebugSession?.IsInBreakMode == false;
    bool stopAvailable => Editor.Ready && DebugSession?.IsScriptExecutionInProgress == true;

    public async void OnSyntaxCheckClicked()
    {
        try
        {
            var scriptPath = Editor.LoadedScript;

            if (!scriptPath.HasText() || !File.Exists(scriptPath))
            {
                Editor.ShowToastError("Script file not found.");
            }
            else
            {
                if (Document.IsModified)
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

    public async void OnStartClicked() => Start(breakAtStart: false, detached: false);

    async void Start(bool breakAtStart, bool detached)
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
            if (Document.IsModified || !Editor.IsScriptReadyForDebugging)
            {
                await SaveToFileOnServer(true); // this will trigger debugger info generation, which will be done when Editor.Ready istrue
                if (!detached)
                    while (!Editor.IsScriptReadyForDebugging)
                        await Task.Delay(500);
            }

            ClearOutput();
            ClearLocals();
            this.DebugSession.Reset();
            this.DebugSession.Breakpoints.AddRange(Document.Breakpoints);

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

    public void OnStopClicked()
    {
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
        DebugSession.RequestPause(); // no need to to clear current step as we are not in the break mode
                                     // Editor.ShowToastError($"Pausing Script is not implemented by DbgHost");
    }

    public void OnStepOverClicked(MouseEventArgs args)
    {
        if (DebugSession.IsScriptExecutionInProgress)
        {
            DebugSession.RequestStepOver();
            UIEvents.NotifyDbgChanged(); // to clear current step
        }
        else
        {
            OnStartAndPauseClicked();
        }
    }

    public void OnStepIntoClicked(MouseEventArgs args)
    {
        if (DebugSession.IsScriptExecutionInProgress)
        {
            DebugSession.RequestStepIn();
            UIEvents.NotifyDbgChanged(); // to clear current step
        }
        else
        {
            OnStartAndPauseClicked();
        }
    }

    string newWatchExpression = "";

    void AddWatchVariable()
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

    void RemoveWatchVariable(int index)
    {
        if (index >= 0 && index < WatchVariables.Count)
        {
            WatchVariables.RemoveAt(index);
            if (selectedWatchIndex >= WatchVariables.Count)
                selectedWatchIndex = WatchVariables.Count - 1;
            UIEvents.NotifyStateChanged();
        }
    }

    public void GoToNextCompileError()
    {
        if (Editor.Output.Count == 0)
            return;

        for (int i = 0; i < Editor.Output.Count; i++)
        {
            currentOutputIndex = (currentOutputIndex + 1) % Editor.Output.Count;
            if (OnOutputLineClick(currentOutputIndex))
                break;
        }

        StateHasChangedSafe();
    }
}