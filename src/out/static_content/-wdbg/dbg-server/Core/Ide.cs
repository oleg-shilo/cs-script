using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using wdbg.cs_script;
using wdbg.Pages;

public class ActiveState
{
    public Dictionary<string, (DateTime timestamp, string content)> AllDocumentsContents = new();

    // document path -> array of line numbers with breakpoints (even invalid breakpoints that will be validated on doc save)
    public Dictionary<string, int[]> AllDocumentsBreakpoints = new();

    // document path -> view state (caret position and scroll position)
    public Dictionary<string, DocumentViewState> AllDocumentsViewStates = new();

    public int[] GetDocumentBreakpoints(string document) => AllDocumentsBreakpoints.ContainsKey(document) ? AllDocumentsBreakpoints[document] : [];

    public DocumentViewState GetDocumentViewState(string document) => AllDocumentsViewStates.ContainsKey(document) ? AllDocumentsViewStates[document] : new DocumentViewState();

    public void UpdateState(string path, Document document, string changeHistory)
    {
        var state = GetDocumentViewState(path);
        AllDocumentsViewStates[path] = state;
        state.CaretLine = document.CaretLine;
        state.CaretCh = document.CaretCh;
        state.CaretPos = document.CaretPos;
        state.ChangeHistory = changeHistory;

        // Debug.WriteLine($"write = file: {path.GetFileName()}({state.CaretLine}, {state.CaretCh})");
    }

    public void SaveDocumentViewState(string document, DocumentViewState viewState)
    {
        if (document.HasText() && viewState != null)
        {
            AllDocumentsViewStates[document] = viewState;
        }
    }

    public bool HasInfoFor(string document) => AllDocumentsBreakpoints.ContainsKey(document) && AllDocumentsBreakpoints.ContainsKey(document);

    public bool AnyScriptFilesModified()
    {
        foreach (var doc in AllDocumentsContents.Keys.ToArray())
            if (AllDocumentsContents[doc].content.HasText() && AllDocumentsContents[doc].timestamp > File.GetLastWriteTimeUtc(doc))
                return true;
        return false;
    }

    public string FindDocument(string fileName)
        => AllDocumentsContents.Keys.FirstOrDefault(x => x.GetFileName() == fileName);

    public List<string> SaveAllFilesIfModified()
    {
        List<string> result = new();
        foreach (var doc in AllDocumentsContents.Keys.ToArray())
        {
            if (AllDocumentsContents[doc].content.HasText() && AllDocumentsContents[doc].timestamp > File.GetLastWriteTimeUtc(doc))
            {
                try
                {
                    result.Add(doc);
                    File.WriteAllText(doc, AllDocumentsContents[doc].content);
                    AllDocumentsContents[doc] = (File.GetLastWriteTimeUtc(doc), AllDocumentsContents[doc].content);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving document {doc}: {ex.Message}");
                }
            }
        }
        return result;
    }

    public void UpdateFor(string path, string content, int[] breakpoints)
    {
        if (breakpoints != null)
        {
            // add all new breakpoints even if they are not valid
            AllDocumentsBreakpoints[path] = breakpoints.Distinct().ToArray();
        }

        if (content != null)
        {
            if (AllDocumentsContents.ContainsKey(path))
            {
                int cacheHashCode = AllDocumentsContents[path].content.GetHashCode();
                int contentHashCode = content.GetHashCode();

                // if (AllDocumentsContents[path].content != content)
                if (cacheHashCode != contentHashCode)
                    AllDocumentsContents[path] = (DateTime.UtcNow, content);
            }
            else
                AllDocumentsContents[path] = (DateTime.UtcNow, content);
        }
    }

    public async Task<(string content, bool isModified)> GetContentFromFileOrCache(string path)
    {
        var newDocument = !AllDocumentsContents.ContainsKey(path) || !AllDocumentsContents[path].content.HasText();

        if (newDocument || File.GetLastWriteTimeUtc(path) > AllDocumentsContents[path].timestamp)
        {
            AllDocumentsContents[path] = (File.GetLastWriteTimeUtc(path), await File.ReadAllTextAsync(path));
        }

        var isModified = AllDocumentsContents[path].timestamp > File.GetLastWriteTimeUtc(path);

        return (AllDocumentsContents[path].content, isModified);
    }

    public bool IsDocumentModified(string path)
    {
        if (AllDocumentsContents.ContainsKey(path) && AllDocumentsContents[path].content.HasText())
        {
            var isModified = AllDocumentsContents[path].timestamp > File.GetLastWriteTimeUtc(path);
            return isModified;
        }

        return false;
    }
}

public class Ide
{
    public localStorage Storage;
    public UINotificationService UINotification;
    public CodeMirrorPage MainPage;
    IJSRuntime Interop;
    public ActiveState State = new();

    public Ide()
    {
    }

    public Ide(CodeMirrorPage page, IJSRuntime interop, UINotificationService uiEvents)
    {
        MainPage = page;
        UINotification = uiEvents;
        Storage = new localStorage(interop);
        Interop = interop;
    }

    public bool DocLoadingInProgress;
    public string LoadedScript = "Untitled";
    public string LoadedDocument = "Untitled";
    public string PreviousLoadedDocument = "";
    public string DebugGenerationError;
    public string LoadedScriptDbg;
    public bool IsLoadedScriptDbg;

    public Dictionary<string, int[]> AllEnabledBreakpoints
    {
        get
        {
            var result = new Dictionary<string, int[]>();

            foreach (var script in State.AllDocumentsBreakpoints.Keys)
            {
                if (dbgScriptMaping.ContainsKey(script))
                {
                    var decoratedScript = dbgScriptMaping[script];
                    result[decoratedScript] = State.AllDocumentsBreakpoints[script].ToArray();
                }
            }
            return result;
        }
    }

    public async Task<int[]> SaveStateOf(string script)
    {
        // ensure the latest debug info is fetched before and State.AllDocumentsBreakpoints has only valid breakpoints
        await FetchLatestDebugInfo(script);

        var dbgFile = script.LocateLoadedScriptDebugCode() + ".bp";
        var persistedDbgInfo = ReadBreakpoints(dbgFile);

        foreach (var file in State.AllDocumentsBreakpoints.Keys)
        {
            if (persistedDbgInfo.ContainsKey(file))
            {
                var documentBreakpoints = State.AllDocumentsBreakpoints[file];
                var persistedBreakpoints = persistedDbgInfo[file];
                for (int i = 0; i < persistedBreakpoints.Length; i++)
                {
                    var line = persistedBreakpoints[i].line;
                    var enabled = documentBreakpoints.Contains(line);
                    persistedBreakpoints[i] = (enabled, line);
                }
            }
        }

        SaveBreakpoints(dbgFile, persistedDbgInfo);

        foreach (var file in State.AllDocumentsContents.Keys)
        {
            // not implemented yet, but will be used to store document contents.
        }

        return State.GetDocumentBreakpoints(this.LoadedDocument); // return fresh updated breakpoints
    }

    public async Task ReadSavedStateOf(string script)
    {
        if (!script.HasText())
            return;

        var dbgFile = script.LocateLoadedScriptDebugCode() + ".bp";
        var dbgInfo = ReadBreakpoints(dbgFile);

        State.AllDocumentsBreakpoints = dbgInfo.ToDictionary(x => x.Key,
                                                             x => x.Value.Where(x => x.enabled).Select(y => y.line).ToArray());
        // not ready yet, but will be used to store document contents.
        // it's not clear what is the best way to save cross-session data for the documents.
        // so save "very old" empty contents for now.
        State.AllDocumentsContents = dbgInfo.ToDictionary(x => x.Key, x => (DateTime.MinValue, ""));
    }

    public async Task FetchLatestDebugInfo(string script)
    {
        // var currentState = State.AllDocumentsBreakpoints;
        var persistedDbgInfo = ReadBreakpoints(script.LocateLoadedScriptDebugCode() + ".bp");

        // update State.AllDocumentsBreakpoints to ensure it contains only valid
        foreach (var file in State.AllDocumentsBreakpoints.Keys)
        {
            var documentBreakpoints = State.AllDocumentsBreakpoints[file].ToList();

            // persistedDbgInfo may not have file. continue so if any breakpoints are defined for the file
            // they will be saved on next save
            if (!persistedDbgInfo.ContainsKey(file))
                continue;

            var persistedBreakpoints = persistedDbgInfo[file];

            foreach (var lineNumber in documentBreakpoints.ToArray()) // iterate through the cloned list to avoid modifying it while iterating
            {
                var valid = persistedBreakpoints.Any(x => x.line == lineNumber);
                if (!valid)
                    documentBreakpoints.Remove(lineNumber); // remove invalid breakpoints
            }

            State.AllDocumentsBreakpoints[file] = documentBreakpoints.ToArray();
        }
    }

    public static void SaveBreakpoints(string breakpointsFile, Dictionary<string, (bool enabled, int line)[]> breakpoints)
    {
        // IMPORTANT: script.csbp contain lines that are 1-based; IDE uses 0-based line numbers
        var dbgCacheDir = breakpointsFile.GetDirName().EnsureDir();
        var lines = breakpoints.Select(item =>
        {
            // file|decoratedFile|-1,-2,+3
            var file = item.Key;
            var decoratedFile = file.ChangeDir(dbgCacheDir);

            return $"{file}|{decoratedFile}|" +
                   $"{(item.Value.Select(x => $"{(x.enabled ? "+" : "-")}{x.line + 1}").JoinBy(","))}";
        }).ToArray();

        File.WriteAllLines(breakpointsFile, lines);
    }

    public Dictionary<string, string> dbgScriptMaping = new();

    public Dictionary<string, (bool enabled, int line)[]> ReadBreakpoints(string breakpointsFile)
    {
        // IMPORTANT: script.cs.bp contain lines that are 1-based; IDE uses 0-based line numbers
        Dictionary<string, (bool enabled, int line)[]> result = new();
        dbgScriptMaping.Clear();

        if (File.Exists(breakpointsFile))
        {
            foreach (var line in File.ReadAllLines(breakpointsFile))
                try
                {
                    // file|decoratedFile|-1,-2,+3
                    var parts = line.Split('|', 3);
                    var file = parts[0].Trim();
                    var decoratedFile = parts[1].Trim();
                    dbgScriptMaping[file] = decoratedFile;
                    var breakpoints = parts[2].Split(',').Select(x => (x.StartsWith("+"), x.Substring(1).ToInt() - 1)).ToArray();
                    result[file] = breakpoints;
                }
                catch { } // ignore malformed lines
        }
        return result;
    }

    public Process DbgGenerator;

    public void LocateLoadedScriptDebugInfo()
    {
        var decoratedScript = LoadedScript.LocateLoadedScriptDebugCode();

        if (decoratedScript.HasText() &&
            File.Exists(LoadedScript) && File.Exists(decoratedScript) &&
            File.GetLastWriteTimeUtc(decoratedScript) >= File.GetLastWriteTimeUtc(LoadedScript))
        {
            LoadedScriptDbg = decoratedScript;
        }
    }

    public async Task<(string script, int pid)[]> GetRunningScripts()
    {
        // #  | PID        | Arguments
        // ----------------------------
        // 01 | 0000035112 | C:\Users\user\.dotnet\tools\.store\cs-script.cli\4.9.6\cs-script.cli\4.9.6\tools\net9.0\any\cscs.dll -ls
        try
        {
            var output = CSScriptHost.CssRun(["-ls"]);

            if (!output.Contains("No running scripts found.", StringComparison.OrdinalIgnoreCase))

            {
                var scripts = output.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !line.Trim().StartsWith("-") && !line.Trim().StartsWith("#"))
                    .Select(line => line.Split('|').Skip(1).Select(x => x.Trim()).ToArray())
                    .Select(parts => (script: parts[1].Split(' ', 2).LastOrDefault(), pid: int.Parse(parts[0])))
                    // .Where(x => x.pid != proc.Id)
                    .ToArray();

                return scripts;
            }
        }
        catch { }
        return [];
    }

    public bool IsScriptReadyForDebugging
    {
        get
        {
            bool filesDefined() => LoadedScript.HasText() && LoadedScriptDbg.HasText();
            bool filesExist() => File.Exists(LoadedScript) && File.Exists(LoadedScriptDbg);
            bool filesAreInSync() => File.GetLastWriteTimeUtc(LoadedScriptDbg) >= File.GetLastWriteTimeUtc(LoadedScript);

            return filesDefined() && filesExist() && filesAreInSync();
        }
    }

    public async Task LoadRecentScriptFile(string file) => await MainPage?.LoadRecentScriptFile(file);

    public async Task LoadDocFile(string file)
    {
        if (MainPage != null && !DocLoadingInProgress)
            await MainPage.LoadDocFile(file);
    }

    public void ResetDbgGenerator()
    {
        try
        {
            if (DbgGenerator != null && !DbgGenerator.HasExited)
                DbgGenerator?.Kill();
            DbgGenerator?.Dispose();
        }
        catch { }
        DbgGenerator = null;
    }

    public string[] Themes = new[]
    {
        "light", "3024-day", "3024-night", "darkone", "abcdef", "dracula", "blackboard", "material-darker", "monokai"
    };

    public string ToastMessage;
    public string ToastType = "toast-info";
    public string LastToastMessage;
    public string LastToastType = "toast-info";
    System.Timers.Timer toastTimer;

    public void ShowToastInfo(string message, int durationMs = 3000) => ShowToast(message, "toast-info", durationMs);

    public void ShowToastSuccess(string message, int durationMs = 3000) => ShowToast(message, "toast-success", durationMs);

    public void ShowToastError(string message, int durationMs = 3000) => ShowToast(message, "toast-error", durationMs);

    public void ConsoleLog(string message)
    {
        Storage._js.InvokeVoidAsync("console.log", message);
    }

    void ShowToast(string message, string type, int durationMs)
    {
        ToastMessage = message;
        ToastType = type;
        LastToastMessage = message;
        LastToastType = type;
        UINotification.NotifyStateChanged();
        ConsoleLog(message);

        toastTimer?.Dispose();
        toastTimer = new System.Timers.Timer(durationMs);
        toastTimer.Elapsed += (s, e) =>
        {
            HideToast();
            toastTimer?.Dispose();
        };
        toastTimer.AutoReset = false;
        toastTimer.Start();
    }

    public void HideToast()
    {
        ToastMessage = null;
        UINotification.NotifyStateChanged();
    }

    public void ShowLastToast()
    {
        if (!string.IsNullOrEmpty(LastToastMessage))
        {
            ShowToast(LastToastMessage, LastToastType, 3000);
        }
    }

    public void PauseToastTimer()
    {
        if (toastTimer != null)
            toastTimer.Stop();
    }

    public void ResumeToastTimer()
    {
        if (toastTimer != null)
            toastTimer.Start();
    }

    string _runStatus;

    public string RunStatus
    {
        get => _runStatus;
        set { _runStatus = value; UINotification.NotifyStateChanged(); }
    }

    public bool Busy;

    bool _ready = true;

    public bool Ready
    {
        get => _ready;
        set { _ready = value; UINotification.NotifyStateChanged(); }
    }

    bool _outputCharMode = false; // false = line mode, true = char mode

    public bool OutputCharMode
    {
        get => _outputCharMode;
        set { _outputCharMode = value; Storage.Write("cm_outputCharMode", _outputCharMode ? "1" : "0"); }
    }

    bool _autoFormatOnSave = false; // false = line mode, true = char mode

    public bool AutoFormatOnSave
    {
        get => _autoFormatOnSave;
        set { _autoFormatOnSave = value; Storage.Write("cm_formatOnSave", _autoFormatOnSave ? "1" : "0"); }
    }

    string _lastSessionFileName;

    public string LastSessionFileName
    {
        get => _lastSessionFileName;
        set { _lastSessionFileName = value; Storage.Write("cm_loadedFileName", _lastSessionFileName); }
    }

    string _theme;

    public string SelectedTheme
    {
        get => _theme;
        set { _theme = value; Storage.Write("cm_theme", _theme); }
    }

    public List<string> FavoriteFiles = new();

    public void AddToRecentFiles(string file)
    {
        if (RecentScripts.Contains(file))
            RecentScripts.Remove(file);
        RecentScripts.Insert(0, file);
        while (RecentScripts.Count > 10)
            RecentScripts.RemoveAt(10);
        Storage.Write("editor_recent_files", RecentScripts);
    }

    List<string> _recentFiles = new();

    public List<string> RecentScripts
    {
        get => _recentFiles;
        set { _recentFiles = value; Storage.Write("editor_recent_files", string.Join('\n', _recentFiles)); }
    }

    public List<string> CurrentScriptFiles
    {
        get => CSScriptHost.GetAllScriptFiles(this.LoadedScript).ToList();
    }

    // use accurate CM names of the themes
    public string SelectedCmTheme => SelectedTheme == "light" ? "default" : SelectedTheme;

    public async Task ReadPersistedConfig()
    {
        _outputCharMode = (await Storage.Read<string>("cm_outputCharMode")) == "1";
        _autoFormatOnSave = (await Storage.Read<string>("cm_formatOnSave")) == "1";
        _lastSessionFileName = await Storage.Read<string>("cm_loadedFileName");
        _theme = await Storage.Read<string>("cm_theme") ?? "darkone"; // application default for sheme is 'darkone' even though CM default is a light theme

        _recentFiles.Clear();
        var items = (await Storage.Read<string>("editor_recent_files") ?? "").Split('\n', ',').Distinct();
        _recentFiles.AddRange(items);
    }

    public List<string> Output = new();

    public class localStorage // needs to be lowercase to allow consistency with JS naming conventions
    {
        public IJSRuntime _js;

        public localStorage(IJSRuntime js) => _js = js;

        public async Task<T> Read<T>(string name) => await _js.InvokeAsync<T>("localStorage.getItem", name);

        public async Task Write(string name, object value) => await _js.InvokeVoidAsync("localStorage.setItem", name, value);
    }
}