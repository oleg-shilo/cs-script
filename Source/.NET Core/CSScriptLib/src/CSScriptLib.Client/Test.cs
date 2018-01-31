using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

// using CSScriptLib;
using System.Reflection;
using System.Runtime.CompilerServices;

public interface IScript
{
    int Sum(int a, int b);
}

namespace CSScriptLib.Client
{
    public class Test
    {
        static public void ReferencingPackagesCode()
        {
            // CSScript.GlobalSettings.AddSearchDirsFromHost();

            dynamic script = CSScript.Evaluator
                                        //.ReferenceAssemblyByName("Microsoft.CSharp")
                                        //.ReferenceAssemblyOf<WebClient>()
                                        //.ReferenceAssemblyOf<JArray>()
                                        .LoadCode(@"
                                        //css_ref Microsoft.CSharp
                                        //css_ref System.Net.WebClient
                                        //css_ref Newtonsoft.Json
                                        using System;
                                        using System.Text;
                                        using System.Net;
                                        using Newtonsoft.Json.Linq;

                                        public class Script
                                        {
                                            public string Run(string url)
                                            {
                                                var client = new WebClient();
                                                client.Headers.Add(""user-agent"", ""anything"");

                                                var releases = client.DownloadString(url);

                                                var report = new StringBuilder();
                                                foreach (dynamic release in JArray.Parse(releases))
                                                    report.AppendLine(release.name.ToString());
                                                return report.ToString();
                                            }
                                        }");

            var report = script.Run("https://api.github.com/repos/oleg-shilo/cs-script/releases");
            Console.WriteLine(report);
        }

        static public void CrossReferenceCode()
        {
            IScript calcEngine = CSScript.Evaluator
                                         .LoadCode<IScript>(@"using System;
                                                              public class ScriptImpl : IScript
                                                              {
                                                                  public int Sum(int a, int b)
                                                                  {
                                                                      return a+b;
                                                                  }
                                                              }");

            dynamic calc = CSScript.Evaluator
                                   .ReferenceAssemblyOf<IScript>()
                                   .LoadCode(@"using System;
                                               public class Calc
                                               {
                                                   public IScript Engine;

                                                   public int Sum(int a, int b)
                                                   {
                                                       return Engine.Sum(a, b);
                                                   }
                                               }");

            calc.Engine = calcEngine;
            var result = calc.Sum(7, 3);
            Console.WriteLine(result);
        }

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