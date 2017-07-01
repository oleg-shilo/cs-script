# CS-Script CodeProvider for Roslyn

CS-Script uses _System.CodeDom.Compiler.ICodeCompiler_ infrastructure to allow compiling script source code into assembly. Though the CS-Script doesn't implement _ICodeCompiler_ directly and instead redirects the compiler instantiation calls to the _Microsoft.CSharp.CSharpCodeProvider_.

The problem arises when trying to handle C# 6 (and above) syntax. MS .NET hasn't implemented code provider neither for C# 6 nor C# 7. Mono handles C# 6 but not C#7. For not supported syntax features one expecter to use Roslyn compiling services instead. 

Though integration with Roslyn is not simple. For a start Roslyn is not part of .NET and it is expected to be used as a NuGet package. Mono (v5.0.1) on the other hand is distributed with the Roslyn compilers included. _Though Mono's Roslyn cannot be used from a .NET host application but only from Mono CLR._ 

However both Mono and .NET completely lack Roslyn adapter assembly (_CSharpCodeProvider_). Which is yet another NuGet package - _Microsoft.CodeDom.Providers.DotNetCompilerPlatform_ (_Roslyn Provider_). And to make things worse this package works only for .NET and completely fails when hosted on Mono. 

__.NET__
- Direct C#6 support: no 
- Direct C#7 support: no
- Roslyn included: no (but available as a NuGet package)
- Roslyn adapter available: no (but available as a NuGet package). Though the package can be used only as part of ASP.NET project.

__Mono__
- Direct C#6 support: yes 
- Direct C#7 support: no
- Roslyn included: yes
- Roslyn adapter available: no

CS-Script _CodeProvider_ is an attempt to solve all these problems. Thus as part of this effort the [Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CodeDomProvider](https://github.com/aspnet/RoslynCodeDomProvider) package source code has been adjusted to support:

- Integrating a shadow copy of Roslyn files from a custom location (e.g. mono environment)
- Running on Mono runtime.

All these changes serve an important purpose of allowing CS-Script  integration with Mono. This, in turn, lets not only CS-Script to target Linux and Mac but also to be integrated with VSCode, which currently relies solely on Mono for non-ASP.NET scenarios.


## Roslyn probing

CS-Script CodeProvider (_CSSRoslynProvider.dll_) depends on two logically related subsystems _Roslyn Provider_ and _Roslyn_ itself. CS-Script CodeProvider embeds _Roslyn Provider_ so it simplifies the deployment. Though the location of the Roslyn needs to be configured or programmatically resolved at runtime. The default _Roslyn Provider_ implementation doesn't allow probing at all and has the location of _Roslyn_ binaries embarrassingly hardcoded.  CS-Script CodeProvider is distributed with the patched version of _Roslyn Provider_ to solve the problem. 

The following is the order of probing for _Roslyn_ location at runtime:
1. Setting `CSSCodeProvider.CompilerPath` explicitly from the host application (e.g. CS-Script CodeProvider).
2. Setting `CSSCodeProvider.CompilerPath` implicitly via environment variable `CSSCRIPT_ROSLYN`, where its value is the directory where the Roslyn compiler (_csc.exe_) is located.
3. Locating _csc.exe_ in the directory/subdirectory with respect to _CSSRoslynProvider.dll_ location.
    - `./csc.exe` 
    - `./bin/csc.exe`
    - `./roslyn/csc.exe`
    - `./bin/roslyn/csc.exe`
4. Locating _csc.exe_ in the Mono installation directory (e.g. /usr/lib/mono/4.5). This step is only performed if running under Mono. 
             
### _Roslyn Provider_ changes             
Apart from fixing _Roslyn Provider_ shortcomings for C# CS-Script CodeProvider also does the same for VB.NET syntax. It also introduces some troubleshooting API improvements.

* `CSSCodeProvider.CompilerPath` - location of _csc.exe_
* `CSSCodeProvider.CompilerServerTimeToLive` - Number of seconds with no activity before the Roslyn C# compiling server times out and closes.</br></br>
* `VBCodeProvider.CompilerPath` - location of _vbc.exe_
* `VBCodeProvider.CompilerServerTimeToLive` - Number of seconds with no activity before the Roslyn C# compiling server times out and closes.</br></br>
* `Environment.SetEnvironmentVariable("CSS_PROVIDER_TRACE", "true")` to print Roslyn probing outcome at runtime.  


### Performance considerations

__*Windows*__


|Runtime| Roslyn Disro | Execution context | Compile time (sec) |           |
|-------| -------------|-------------------| ------------------:|-------------------|
| Mono  | Mono         | First run         |                3.5 |                   |
| Mono  | Mono         | Consecutive runs  |                2.2 |                   |
| Mono  | NuGet        | First run         |                2.2 |                   |
| Mono  | NuGet        | Consecutive runs  |                2.2 |                   |
| .NET  | Mono         | First run         |                n/a |    (incompatible) |
| .NET  | Mono         | Consecutive runs  |                n/a |    (incompatible) |
| .NET  | NuGet        | First run         |                4.0 |                   |
| .NET  | NuGet        | Consecutive runs  |                0.2 |                   |

__*Linux*__


|Runtime| Roslyn Disro | Execution context | Compile time (sec) |         |
|-------| -------------|-------------------| ------------------:|-------------------|
| Mono  | Mono         | First run         |                1.4 |                   |
| Mono  | Mono         | Consecutive runs  |                0.8 |                   |
| Mono  | NuGet        | First run         |                2.3 |                   |
| Mono  | NuGet        | Consecutive runs  |                1.8 |                   |
