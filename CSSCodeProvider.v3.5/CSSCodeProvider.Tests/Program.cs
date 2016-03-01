using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptCompilers;

namespace CSSCodeProvider.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new CCSharpParser(@"E:\cs-script\engine\CSSCodeProvider.v3.5\Script.cs");
            parser.ToTempFile(false);
        }
    }
}
