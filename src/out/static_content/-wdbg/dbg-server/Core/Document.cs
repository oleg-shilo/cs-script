using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using wdbg.cs_script;
using wdbg.Pages;
using wdbg.Shared;

public class Document
{
    public HashSet<int> Breakpoints = new();

    public string CurrentLineText = "";

    public int CaretLine = 0;
    public int CaretCh = 0;
    public string EditorContent;
    public bool IsModified = false;
}

public class UINotificationService
{
    public string SessionId = Guid.NewGuid().ToString();

    public event Action OnChange;

    public event Action<string> OnDbgChange;

    public event Action<string> OnObjectValue;

    public void NotifyStateChanged() => OnChange?.Invoke();

    public void NotifyDbgChanged(string variables = null) => OnDbgChange?.Invoke(variables);

    public void NotifyObjectValueReceived(string variables) => OnObjectValue?.Invoke(variables);
}

public class Ide
{
    public localStorage Storage;
    public UINotificationService UINotification;
    public CodeMirrorPage MainPage;
    IJSRuntime Interop;

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

    public string LoadedScript = "Untitled";
    public string DebugGenerationError;
    public string LoadedScriptDbg;
    public bool IsLoadedScriptDbg;
    public int[] LoadedScriptValidBreakpoints;
    public Process DbgGenerator;

    public void LocateLoadedScriptDebuggInfo()
    {
        var decoratedScript = Path.ChangeExtension(LoadedScript, ".dbg.cs");

        if (File.Exists(LoadedScript) && File.Exists(decoratedScript) &&
            File.GetLastWriteTimeUtc(decoratedScript) == File.GetLastWriteTimeUtc(LoadedScript))
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
            bool filesAreInSync() => File.GetLastWriteTimeUtc(LoadedScriptDbg) == File.GetLastWriteTimeUtc(LoadedScript);

            return filesDefined() && filesExist() && filesAreInSync();
        }
    }

    public async Task LoadRecentFile(string file) => await MainPage?.LoadRecentFile(file);

    public async void ShowExternalFile(string path, int navigateToline = -1)
    {
        try
        {
            Interop.InvokeVoidAsync("open", $"/fullscreen-editor?file={Uri.EscapeDataString(path)}&line={navigateToline}", "_blank");
        }
        catch (Exception ex)
        {
            ConsoleLog($"Error opening external file: {ex.Message}");
            ShowToastError($"Error opening file: {ex.Message}");
        }
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
        if (RecentFiles.Contains(file))
            RecentFiles.Remove(file);
        RecentFiles.Insert(0, file);
        while (RecentFiles.Count > 10)
            RecentFiles.RemoveAt(10);
        Storage.Write("editor_recent_files", RecentFiles);
    }

    List<string> _recentFiles = new();

    public List<string> RecentFiles
    {
        get => _recentFiles;
        set { _recentFiles = value; Storage.Write("editor_recent_files", string.Join('\n', _recentFiles)); }
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