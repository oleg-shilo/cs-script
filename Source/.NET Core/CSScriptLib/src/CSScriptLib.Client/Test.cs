using System;
using System.Linq;
using System.Collections.Generic;
using CSScriptLibrary;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;

public interface IScript
{
    int Sum(int a, int b);
}

namespace CSScriptLib.Client
{
    public class Test
    {
        static public void CompileCode()
        {
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

        static public void CompileMethod()
        {
            dynamic script = CSScript.Evaluator
                                     .CompileMethod(@"using System;
                                                      public int Sum(int a, int b)
                                                      {
                                                          return a+b;
                                                      }")
                                     .CreateObject("*");

            var result = script.Sum(7, 3);
            Console.WriteLine(result);
        }

        static public void CompileCSharp_7()
        {
            dynamic script = CSScript.Evaluator
                                     .CompileMethod(@"using System;
                                                      public int Sum(int a, int b)
                                                      {
                                                          int sum(int x, int y)
                                                          {
                                                               return x+y;
                                                          }

                                                          return sum(a, b);
                                                      }")
                                     .CreateObject("*");

            var result = script.Sum(7, 3);
            Console.WriteLine(result);
        }

        static public void CompileDelegate()
        {
            var product = CSScript.Evaluator
                                  .CreateDelegate<int>(@"int Product(int a, int b)
                                                        {
                                                            return a * b;
                                                        }");

            int result = product(3, 2);
            Console.WriteLine(result);
        }

        static public void CompileDelegate1()
        {
            var product = CSScript.Evaluator
                                  .CreateDelegate(@"int Product(int a, int b)
                                                    {
                                                        return a * b;
                                                    }");

            int result = (int)product(3, 2);
            Console.WriteLine(result);
        }

        static public void LoadCode()
        {
            dynamic script = CSScript.Evaluator
                                     .LoadCode(@"using System;
                                                 public class Script
                                                 {
                                                     public int Sum(int a, int b)
                                                     {
                                                         return a+b;
                                                     }
                                                 }");
            int result = script.Sum(1, 2);
            Console.WriteLine(result);
        }

        static public void LoadCode2()
        {
            var eval = CSScript.Evaluator;

            var script = eval.ReferenceAssemblyOf<IScript>()
                             .LoadCode<IScript>(@"using System;
                                                 public class Script : IScript
                                                 {
                                                     public int Sum(int a, int b)
                                                     {
                                                         return a+b;
                                                     }
                                                 }");
            int result = script.Sum(1, 2);
            Console.WriteLine(result);
        }

        static public void LoadCode_RefAsm()
        {
            var script = (IScript)CSScript.Evaluator
                                     .ReferenceAssemblyOf<IScript>()
                                     .LoadCode(@"using System;
                                                 public class Script : IScript
                                                 {
                                                     public int Sum(int a, int b)
                                                     {
                                                         return a+b;
                                                     }
                                                 }");
            int result = script.Sum(1, 2);
            Console.WriteLine(result);
        }
    }
}