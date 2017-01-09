<img align="right" src="https://raw.githubusercontent.com/oleg-shilo/cs-script/master/Source/wiki/images/css_logo_codeplex_256x256_2.png" alt="" style="float:right">
# CS-Script - v3.20.0

<sub>_The project has been migrated to GitHub from CodePlex: [CS-Script](http://csscriptsource.codeplex.com/)._</sub> 

CS-Script is a CLR (Common Language Runtime) based scripting system which uses ECMA-compliant C# as a programming language.

CS-Script is one of the most mature C# scripting solutions. I become publicly available in a two years after the first release of .NET. 
CS-Script supports both hosted and standalone execution model. It allows seamlessly switching underlying compiling technology without affecting the code base. Currently supported compilers are Mono, Roslyn and CodeDOM. It offers comprehensive integration with most common development tools. 

_**For the all CS-Script details go to the project [Documentation Wiki](https://github.com/oleg-shilo/cs-script/wiki).**_
<hr/>

The following is a simple code sample just to give you the idea about the product:

_**Executing script from shell**_

Updating media file tags. 
Note, the script is using optional classless layout.

_Script file: `mp4_retag.cs`_

```C#
//css_nuget taglib
using System;
using System.IO;

string source = @"\\media-server\tv_shows\Get Smart%\Season1";

void main()
{
    foreach (string file in Directory.GetFiles(source, "*.mp4"))
    {
        string episode_name = Path.GetFileNameWithoutExtension(file);

        var mp4 = TagLib.File.Create(file);
        mp4.Tag.Title = episode_name;
        mp4.Save();

        Console.WriteLine(episode_name);
    }
}
```
Execute script file directly in cmd-promt without building an executable assembly:
```
C:\Temp>cscs mp4_retag.cs
```


_**Hosting script engine**_

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