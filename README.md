# CS-Script
CS-Script is a CLR (Common Language Runtime) based scripting system which uses ECMA-compliant C# as a programming language.

_The project is currently being migrated to GitHub from CodePlex: [CS-Script](http://csscriptsource.codeplex.com/). The source code at the moment is available from this [Git repository](http://csscriptsource.codeplex.com/SourceControl/latest)._

CS-Script is one of the most mature C# scripting solutions. I become publicly available in a two years after the first release of .NET. 
CS-Script supports both hosted and standalone execution model. It allows seamlessly switching underlying compiling technology without affecting the code base. Currently supported compilers are Mono, Roslyn and CodeDOM. 
It offers comprehensive integration with most common development tools. The project [[documentation|Home]] describes all aspects of CS-Script in details.

The following is a simple code sample just to give you the idea bout the product:

```C#
dynamic script = CSScript.LoadCode(
                           @"using System.Windows.Forms;
                             public class Script
                             {
                                 public void SayHello(string greeting)
                                 {
                                     MessageBox.Show($""Greeting: {greeting}"");
                                 }
                             }")
                             .CreateObject("*");
script.SayHello("Hello World!");
//-----------------
var product = CSScript.CreateFunc<int>(@"int Product(int a, int b)
                                         {
                                             return a * b;
                                         }");
int result = product(3, 4);
//-----------------
var SayHello = CSScript.LoadMethod(
                        @"using System.Windows.Forms;
                          public static void SayHello(string greeting)
                          {
                              MessageBoxSayHello(greeting);
                              ConsoleSayHello(greeting);
                          }
                          static void MessageBoxSayHello(string greeting)
                          {
                              MessageBox.Show(greeting);
                          }
                          static void ConsoleSayHello(string greeting)
                          {
                              Console.WriteLine(greeting);
                          }")
                         .GetStaticMethod("SayHello" , typeof(string)); 
SayHello("Hello again!");
``` 