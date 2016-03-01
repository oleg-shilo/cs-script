// CSI: A simple C# interpreter
// Copyright, Steve Donovan 2005
// Use freely, but please acknowledge!
//------------------------------------
//css_inc console.cs;
//css_inc interpreter.cs;
//css_inc prepro.cs;
using System;
using System.IO;

class RunCSI {
    const string caption = "CSI Simple C# Interpreter vs 0.8",
                       prompt = "# ", block_prompt = ". ";
    
    public static void Main(string [] args) {
        Interpreter.Console = new TextConsole();
        Utils.Write(caption+"\n"+prompt);        
        Interpreter interp = new Interpreter();        
        string defs = args.Length > 0 ? args[0] : interp.DefaultIncludeFile();
        interp.ReadIncludeFile(defs);        
        while (interp.ProcessLine(Utils.ReadLine())) 
            Utils.Write(interp.BlockLevel > 0 ? block_prompt : prompt);
    }
}

class TextConsole : IConsole {
    public string ReadLine() {
        return Console.In.ReadLine();
    }    
    
    public void Write(string s) {
        Console.Write(s);
    }
}
