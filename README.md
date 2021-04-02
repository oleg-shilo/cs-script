# CS-Script
<img align="right" src="https://raw.githubusercontent.com/oleg-shilo/cs-script/master/src/logo/css_logo_100x100.png" alt="" style="float:right">

---

_Please note that this repository is hosting the releases of CS-Script (v4.0.0 and higher), which target .NET 5.
Meaning that both CLI and class library (Nuget package) can transparently be hosted on and interact with both .NET Framework and .NET Core runtimes (.NET 5 SDK is required to be installed)._

_Previous content of this repository and Wiki, which was exclusively dedicated to .NET Framework edition of CS-Script has been  is  has been moved to a new repository: https://github.com/oleg-shilo/cs-script.net-framework_

---
 
[![Build status](https://ci.appveyor.com/api/projects/status/jruj9dmf2dwjn5p3?svg=true)](https://ci.appveyor.com/project/oleg-shilo/cs-script) [![Chocolatey Version](http://img.shields.io/chocolatey/v/cs-script.svg?style=flat-square)](http://chocolatey.org/packages/cs-script) [![Chocolatey Downloads](http://img.shields.io/chocolatey/dt/cs-script.svg?style=flat-square)](http://chocolatey.org/packages/cs-script) [![NuGet version (CS-Script)](https://img.shields.io/nuget/v/CS-Script.svg?style=flat-square)](https://www.nuget.org/packages/CS-Script/)

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://oleg-shilo.github.io/cs-script/Donation.html)

CS-Script is a CLR based scripting system which uses ECMA-compliant C# as a programming language.

CS-Script is one of the most mature C# scripting solutions. It became publicly available in 2004, just two years after the first release of .NET. And it was the first comprehensive scripting platform for .NET

CS-Script supports both hosted and standalone (CLI) execution model. This makes it possible to use the script engine as a pure C# alternative for PowerShell. As well as extending .NET applications with C# scripts executed at runtime by the hosted script engine.

CS-Script allows seamlessly switching underlying compiling technology without affecting the code base. Currently supported compilers are _dotnet.exe_ and _csc.exe_. 

CS-Script also offers comprehensive integration with most common development tools:

- Visual Studio (built-in feature via `-vs` CLI argument)
- [VSCode](https://github.com/oleg-shilo/cs-script.vscode)
- [Sublime Text 3](https://github.com/oleg-shilo/cs-script-sublime)
- _Legacy .NET Framework plugins that are yet to be ported to .NET 5:_
  - [Notepad++](https://github.com/oleg-shilo/cs-script.npp) .
  - [Visual Studio](https://github.com/oleg-shilo/CS-Script.VSIX)

It can be run on Win and Linux. Class library for hosting the script engine is compiled for ".NET Standard" so it can be hosted by any managed application.

Over the long history of CS-Script it has been downloaded through Notepad++ x86 plugin manager alone over ![](https://oleg-shilo.github.io/cs-script/download.stats/css.npp.count.jpeg) times ([stats](https://www.cs-script.net/cs-script/download.stats/daily_downloads.html) till July 2017).

<hr/>

_**Documentation disclaimer**_
Please be aware that currently the online [Documentation Wiki](https://github.com/oleg-shilo/cs-script/wiki) is being reviewed and updated. This is review/rework hs been triggered by the convergence of two CS-Script development and distribution streams for .NET Core and .NET Framework.Meaning that until this review is completed and this disclaimer is removed some of the Wiki content may not be fully accurate.

The most accurate and concise documentation is embedded in the engine executable and can be accessed via CLI "help" command:
```ps
css -help
```

A copy of the help content can be accessed [here](help.txt)

<hr/>

The following section describes a few use cases just to give you the idea about the product:

_**CLI: Executing script from shell**_

CS-Script follows many elements of the Python user experience model. Thus a script can reference other scripts, .NET assemblies or even NuGet packages. It also uses Python style caching ensuring that script that was executed at least once is never compiled again unless the script code is changes. This ensures ultimate performance. Thus script execution speed of the consecutive runs matches the excution of fully compiled .NET applications.  

Create a simple console script:
_You can script any type of application that .NET supports (e.g. WinForm, WPF, WEB API) and in two supported syntaxes: C# and VB._

```ps
cscs -new script.cs
```

This creates a sample script file with a sample code. Of course you can also create the script file by yourself. This is a top-level class script:

```C#
using System;

Console.WriteLine(user());

string user()
    => Environment.UserName;
```

Execute script file directly in cmd-prompt/linux-shell without building an executable assembly:
```
css .\script.cs
```
Note, while the script engine CLI executables is `cscs.exe` (on Win) and `cscs` (on Linux) you can use `css` shim that is available in both win and linux distro.

_**CLI: Executing script from IDE**_

While various IDEs can be used VSCode is arguably the simplest one.
You can either load the existing script into it with `css -vscode .\script.cs` or create a new script within the IDE if you install the [CS-Script extension](https://marketplace.visualstudio.com/items?itemName=oleg-shilo.cs-script):
![](https://user-images.githubusercontent.com/16729806/108838856-53b3e500-7628-11eb-8979-9b464484afec.gif)  


_**Hosting script engine**_

You can host the script engine in any .NET application. The class library is distributed as a NuGet package [CS-Script](https://www.nuget.org/packages/CS-Script/). 

```ps
Install-Package CS-Script
```

The library is built against _.NET Standard 2.0_ so it can be hosted on any edition of runtime. However the script evaluation is done via .NET 5 tool chain so it needs to be installed on the host PC even if the application is implemented with the older framework (e.g. .NET Framework).

These are just a few samples:
_The complete content of samples can be found [here](https://github.com/oleg-shilo/cs-script.core/blob/master/src/CSScriptLib/src/CSScriptLib/samples.cs)._

```C#
public interface ICalc
{
    int Sum(int a, int b);
}
...
// you can but don't have to inherit your script class from ICalc
ICalc calc = CSScript.Evaluator
                     .LoadCode<ICalc>(@"using System;
                                        public class Script
                                        {
                                            public int Sum(int a, int b)
                                            {
                                                return a+b;
                                            }
                                        }");
int result = calc.Sum(1, 2);
```

```C#
dynamic script = CSScript.Evaluator
                         .LoadMethod(@"int Product(int a, int b)
                                       {
                                           return a * b;
                                       }");

int result = script.Product(3, 2);
```

```C#
public interface ICalc
{
    int Sum(int a, int b);
    int Div(int a, int b);
}
...
ICalc script = CSScript.Evaluator
                       .LoadMethod<ICalc>(@"public int Sum(int a, int b)
                                            {
                                                return a + b;
                                            }
                                            public int Div(int a, int b)
                                            {
                                                return a/b;
                                            }");
int result = script.Div(15, 3);
```

```C#
var log = CSScript.Evaluator
                  .CreateDelegate(@"void Log(string message)
                                    {
                                        Console.WriteLine(message);
                                    }");

log("Test message");
```

