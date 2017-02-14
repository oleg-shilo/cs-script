using System;

public class Script : IScript
{
    public IHost Parent { set; get; }
    public void Execute(string context)
    {
        Console.WriteLine("Script Context: " + context);
        if (Parent != null)
            Parent.Who();
    }
}

