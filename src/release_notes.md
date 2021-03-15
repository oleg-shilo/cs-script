# Release v4.0.0.0

CLI
- New commands:
	- `-server` - for managing build server 
	- `-vscode` - to open script in VSCode
	- `-vs` - to script project in Visual Studio
	- `-self-test` - for testing the engine on the target system
	- `-self-exe` - for building css launcher for manual deployment
	- `-engine:<csc|dotnet>`
	- `-new:toplevel` - CLI parameter
	- `-profile` - for testing script loading performance
	- `-speed` - for compiler performance testing
- Added css.exe
- Added suppotrrt for setting csc path with %css_csc_file%
- Improved error output for BuildServer run failures
- Added creation of `code.header` on first use of CLI command `-code`
- Added complex (multi-file) commands support (e.g. css -self-test-run)
- Implemented build server
- Implemented hot-loading for csc engine.
- Normalized all \n and \r\n CLI output by using Environment.NewLine
- Added reporting using of incompatible csc compiler for scripts requiring XAML compilation  

CSScriptLib
- Implemented //css_winapp for WinForm and WPF applications
- Added //css_engine (//css_ng) directive for choosing the compiling engine
- Completed CSScript.Evaluator.CodeDom interface.
- Implemented transparent hosting of CSScriptLib in .NET-Framework and .NET-Core
- Removed dependency on Roslyn for pure CodeDom evaluator use-case
- added sample with downloading the latest C# compiler
- added passing compiler options
- Implemented probing for default C#5 compiler when hosted on .NET Framework.
- Extending u-testing to cover new Evaluator features (CSScriptLib.dll)
- Added `CodeDomEvaluator.CompilerLastOutput`

