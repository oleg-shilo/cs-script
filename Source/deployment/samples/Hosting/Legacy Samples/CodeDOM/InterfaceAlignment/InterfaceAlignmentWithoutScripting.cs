using System;
using CSScriptLibrary;

public interface IDocument
{
    void Open(string file);
    void Save(string file);
}

public class RTFDocument
{
    public void Open(string file)
    {
        Console.WriteLine("Open: " + file);
    }

    public void Save(string file)
    {
        Console.WriteLine("Save: " + file);
    }

    public void SetFormat(int startPosition, int endPosition, string formatName)
    {
        Console.WriteLine("ImplementSetFormat: " + formatName);
    }
}

class TestScript
{
    static public void Main()
    {
        //note RTFDocument does not implement IDocument interface and yet it can be "type casetd" into it
        IDocument doc = new RTFDocument().AlignToInterface<IDocument>();
        doc.Save("readme.txt");
    }
}

