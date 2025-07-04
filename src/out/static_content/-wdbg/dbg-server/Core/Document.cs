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

    public int CaretLine = 0;
    public int CaretCh = 0;
    public int CaretPos = 0;

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