using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.JSInterop;
using wdbg.cs_script;
using wdbg.Pages;
using wdbg.Shared;

public class DocumentViewState
{
    public int CaretLine { get; set; } = 0;
    public int CaretCh { get; set; } = 0;
    public int CaretPos { get; set; } = 0;
    public string ChangeHistory { get; set; } = "";
    public double ScrollTop { get; set; } = 0;
    public double ScrollLeft { get; set; } = 0;
}

public class Document
{
    public int[] Breakpoints = [];

    public int CaretLine = 0;
    public int CaretCh = 0;
    public int CaretPos = 0;

    public string EditorContent;
    private bool isModified = false;

    public bool IsModified { get => isModified; set => isModified = value; }
}

public class UINotificationService
{
    public string SessionId = Guid.NewGuid().ToString();

    public event Action OnChange;

    public event Action OnCaretPositionChange;

    public event Action<string, string> OnDbgChange;

    public event Action<string> OnObjectValue;

    public void NotifyCaretPositionChanged() => OnCaretPositionChange?.Invoke();

    public void NotifyStateChanged() => OnChange?.Invoke();

    public void NotifyDbgChanged(string variables = null, string callStack = null) => OnDbgChange?.Invoke(variables, callStack);

    public void NotifyObjectValueReceived(string variables) => OnObjectValue?.Invoke(variables);
}