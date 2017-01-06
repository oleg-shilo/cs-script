using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace CSScriptLib.Client
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Test();return;

            Assembly asm = CSScript.Evaluator
                                   .CompileCode(@"using System;
                                                  public class Script
                                                  {
                                                      public int Sum(int a, int b)
                                                      {
                                                          return a+b;
                                                      }
                                                  }");

            dynamic script = asm.CreateObject("*");
            var result = script.Sum(7, 3);
            Console.WriteLine(result);
        }

        static void Test()
        {
            for (int i = 0; i < 100000; i++)
            {
                //    var code = @"using System;
                //                 public class Script
                //                 {
                //                     public int Sum(int a, int b)
                //                     {
                //                         return a+b;
                //                     }
                //                 }";

                //    CSharpScript.EvaluateAsync(
                //        @"using System; 
                //          var s = """+i+@"""; 
                //          Console.WriteLine(""Hello Roslyn.""+s);").Wait();

                var code = @"using System;
                         public class Script
                         {
                             public int Sum(int a, int b)
                             {
                                 return a+b;
                             }
                         }";

                //object result = CSharpScript.EvaluateAsync(code + @" class return new Script();").Result;
                //typeof(< a type in that assembly>).GetTypeInfo().Assembly

                var asm = (Assembly) CSharpScript.EvaluateAsync(code + @" class EntryPoint{}; return typeof(EntryPoint).Assembly;").Result;

                dynamic script = asm.CreateObject("*");
                var result = script.Sum(i, 1);
                Console.WriteLine(result);
            }

            //object result = CSharpScript.EvaluateAsync("1+5").Result;
            //Console.WriteLine(result);
            //asms = AppDomain.CurrentDomain.GetAssemblies();
        }
    }
}
