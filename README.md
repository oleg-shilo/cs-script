# CS-Script
_CS-Script is a CLR (Common Language Runtime) based scripting system which uses ECMA-compliant C# as a programming language._

The project is currently being prepared for migration to GitHub from CodePlex: [CS-Script](http://csscriptsource.codeplex.com/). 
The project is currently being prepared for migration to GitHub from CodePlex: CS-Script. You can find the product documentation here:https://csscriptsource.codeplex.com/documentation

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