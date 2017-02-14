//css_ref ..\..\..\CSScriptLibrary.dll
//css_ref ..\..\..\Lib\Mono.CSharp.dll;
//css_ref Microsoft.CSharp;
//css_ref System.Core;

using CSScriptLibrary;
using System;

public interface ICalc
{
    HostApp Host { get; set; }
    int Sum(int a, int b);
}

public class HostApp
{
    static void Main()
    {
        var host = new HostApp();

        host.CalcTest_InterfaceAlignment();
        host.CalcTest_InterfaceInheritance();
        host.HelloTest();
    }

    void HelloTest()
    {
        dynamic script = CSScript.Evaluator
                                 .LoadMethod(@"void SayHello(string greeting)
                                               {
                                                   Console.WriteLine(greeting);
                                               }");

        script.SayHello("Hello World!");
    }

    void CalcTest_InterfaceInheritance()
    {
        ICalc calc = (ICalc)CSScript.Evaluator
                                    .LoadCode(@"public class Script : ICalc
                                                { 
                                                    public int Sum(int a, int b)
                                                    {
                                                        if(Host != null) 
                                                            Host.Log(""Sum is invoked"");
                                                        return a + b;
                                                    }
                             
                                                    public HostApp Host { get; set; }
                                                }");
        calc.Host = this;                             
        int result = calc.Sum(1, 2);
    }
    
    void CalcTest_InterfaceAlignment()
    {
        ICalc calc = (ICalc)CSScript.Evaluator
                                    .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                                         {
                                                             if(Host != null) 
                                                                 Host.Log(""Sum is invoked"");
                                                             return a + b;
                                                         }

                                                         public HostApp Host { get; set; }");
        calc.Host = this;
        int result = calc.Sum(1, 2);
    }
    
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}