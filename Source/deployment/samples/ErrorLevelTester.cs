//css_host /platform:x86
//The example contributed by Sten Frostholm.
using System;

namespace Scripting
{
    class ErrorLevelTester
    {
        [STAThread]
        static int Main(string[] args)
        {
            bool is64BitProcess = (IntPtr.Size == 8);
            if(is64BitProcess)
                Console.WriteLine(">>>   x64");
            else
                Console.WriteLine(">>>   x86");

            int returnValue = int.Parse(args[0]);

            Console.WriteLine("Welcome to ERRORLEVEL tester...the following integer value was parsed from the first argument and will be returned at exit from this process: {0}", returnValue);

            return returnValue;
        }
    }
}