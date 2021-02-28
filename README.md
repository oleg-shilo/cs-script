# CS-Script
<img align="right" src="https://raw.githubusercontent.com/oleg-shilo/cs-script/master/Source/wiki/images/css_logo_codeplex_256x256_2.png" alt="" style="float:right">

---

Latest .NET 5 release: https://github.com/oleg-shilo/cs-script.core/releases/tag/v2.0.0.0

---
 
[![Build status](https://ci.appveyor.com/api/projects/status/jruj9dmf2dwjn5p3?svg=true)](https://ci.appveyor.com/project/oleg-shilo/cs-script) [![Chocolatey Version](http://img.shields.io/chocolatey/v/cs-script.svg?style=flat-square)](http://chocolatey.org/packages/cs-script) [![Chocolatey Downloads](http://img.shields.io/chocolatey/dt/cs-script.svg?style=flat-square)](http://chocolatey.org/packages/cs-script) [![NuGet version (CS-Script)](https://img.shields.io/nuget/v/CS-Script.svg?style=flat-square)](https://www.nuget.org/packages/CS-Script/)

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://oleg-shilo.github.io/cs-script/Donation.html)

CS-Script is a CLR based scripting system which uses ECMA-compliant C# as a programming language.

CS-Script is one of the most mature C# scripting solutions. It became publicly available in 2004, just two years after the first release of .NET. And it was the first comprehensive scripting platform for .NET

CS-Script supports both hosted and standalone execution model. This makes it possible to use the script engine as a pure C# alternative for PowerShell. As well as extending .NET applications with C# scripts executed at runtime by the hosted script engine.

CS-Script allows seamlessly switching underlying compiling technology without affecting the code base. Currently supported compilers are Mono, Roslyn and CodeDOM. 

CS-Script also offers comprehensive integration with most common development tools: 
- [Visual Studio](https://github.com/oleg-shilo/CS-Script.VSIX), 
- [VSCode](https://github.com/oleg-shilo/cs-script.vscode), 
- [Sublime Text 3](https://github.com/oleg-shilo/cs-script-sublime), 
- [Notepad++](https://github.com/oleg-shilo/cs-script.npp).

It can be run on Win, Linux and Mac. And it is compatible with .NET, Mono and .NET Core.

Over the long history of CS-Script it has been downloaded through Notepad++ x86 plugin manager alone over ![](https://oleg-shilo.github.io/cs-script/download.stats/css.npp.count.jpeg) times ([see stats](https://www.cs-script.net/cs-script/download.stats/daily_downloads.html)).

\* _statistics does not include x64 downloads nor downloads after Notepad++ discontinued shiping editor with the plugin manager x86 included_ 

_**For the all CS-Script details go to the project [Documentation Wiki](https://github.com/oleg-shilo/cs-script/wiki).**_
<hr/>

_**For the roadmap for .NET 5 development see this [article](https://github.com/oleg-shilo/cs-script/wiki/Roadamap).**_

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

string source = @"\\media-server\tv_shows\Get Smart\Season1";

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
Execute script file directly in cmd-prompt without building an executable assembly:
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
                                     MessageBox.Show(""Greeting: "" + greeting);
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
                         .GetStaticMethod("*.SayHello" , ""); 
SayHello("Hello again!");
``` 
