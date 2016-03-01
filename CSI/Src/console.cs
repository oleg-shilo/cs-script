// CSI: A simple C# interpreter
// Copyright, Steve Donovan 2005
// Use freely, but please acknowledge!
using System;
using System.IO;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;

public delegate void StringHandler(string line);

class ConsoleTextBox : RichTextBox { 
    GuiConsoleForm parent;
    
    public ConsoleTextBox(GuiConsoleForm parent_) {
        parent = parent_;
    }        
        
    protected override bool IsInputKey(Keys keyData) {
        if (keyData == Keys.Enter) {
            int lineNo = GetLineFromCharIndex(SelectionStart);
            if (lineNo < Lines.Length) {
                string line = Lines[lineNo];       
                parent.DelayedExecute(line);
            }
       }    
       return base.IsInputKey(keyData);
   }
}

public class GuiConsoleForm : Form 	
{     
    RichTextBox textBox;
    string prompt;
    Timer timer = new Timer();
    string currentLine;
    StringHandler stringHandler;    
    
    public GuiConsoleForm(string caption, string cmdPrompt, StringHandler h) 
    {        
        Text = caption;
        prompt = cmdPrompt;
        stringHandler = h;
        textBox = new ConsoleTextBox(this);
        textBox.Dock = DockStyle.Fill;
        textBox.Font = new Font("Tahoma",10,FontStyle.Bold);
        textBox.WordWrap = false;
        
        Width = 750;
        Size = new Size(467, 400);

        timer.Interval = 50;
        timer.Tick += new EventHandler(Execute);
        
        this.Controls.Add(textBox);
    }
    
    public void DelayedExecute(string line) {
        currentLine = line;
        while (currentLine.IndexOf(prompt) == 0)
            currentLine = currentLine.Substring(prompt.Length);
        timer.Start();
    }   
     
    void Execute(object sender,EventArgs e) {
        timer.Stop();
        stringHandler(currentLine);
        Write(prompt);
    }

    public void Write(string s) {
        textBox.AppendText(s);
    }
    
    public RichTextBox TextBox {
        get { return textBox; }
    }
	
	public string Prompt {
		set { prompt = value; }
	}
    
}



