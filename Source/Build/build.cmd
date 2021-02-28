echo off
@set BATCH_BUILD=true
@set oldPATH=%PATH%;
@set PATH=%windir%\Microsoft.NET\Framework\v1.1.4322;%PATH%;
@set net4_tools=C:\Windows\Microsoft.NET\Framework\v4.0.30319
@set vs_edition=Community
rem @set vs_edition=Professional

@set net45_tools=C:\Program Files (x86)\Microsoft Visual Studio\2019\%vs_edition%\MSBuild\Current\Bin

@set net4_asms=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6

ECHO off
ECHO Preparing to build...

ECHO You may need to adjust this file to exclude not needed projects and file copying actions
ECHO Add /debug+ /debug:full /o- args to do a debug build

del *.exe
del *.dll
del *.dll.unsigned

MD temp
MD temp\temp
copy ..\CSScriptLibrary\sgKey.snk sgKey.snk


if exist build.log del build.log

@set common_msbuild_params=/nologo /t:Rebuild /verbosity:quiet /noconsolelogger /fl /flp:logfile=..\Build\build.log;verbosity=quiet;append=true
@set common_4_params=/noconfig /nostdlib+ /r:"%net4_asms%\System.Design.dll" /r:"%net4_asms%\System.Drawing.dll" /r:"%net4_asms%\mscorlib.dll"
@set common_ref_files=/r:..\Mono.Posix.dll
@set common_source_files=..\GACHelper.cs ..\fileparser.cs ..\Precompiler.cs ..\extensions.cs ..\csscript.cs ..\csparser.cs ..\AssemblyResolver.cs ..\AssemblyExecutor.cs  ..\Exceptions.cs ..\ExecuteOptions.cs ..\ScriptLauncherBuilder.cs ..\Settings.cs ..\Utils.cs ..\SystemWideLock.cs ..\Unix.FileMutex.cs ..\HelpProvider.cs ..\NuGet.cs ..\Project.cs

REM ECHO Building...

cd ..\CSScriptLibrary

ECHO Building CSScript TargetFramework: v4.5:
ECHO Building cscs.exe: >> ..\Build\build.log
"%net45_tools%\msbuild.exe" ..\cscscript\cscscript.csproj /p:AssemblyName=cscs /p:TargetFrameworkVersion=v4.5  /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45" %common_msbuild_params%
ECHO ------------ >> ..\Build\build.log
move ..\cscscript\bin\Distro\cscs.exe ..\Build\cscs.exe
..\Build\cscs.exe -? > "%local_dev%\help.txt"
..\Build\cscs.exe -? > ..\..\..\cs-script\help.txt
ECHO Building cscs32.exe: >> ..\Build\build.log
"%net45_tools%\msbuild.exe" ..\cscscript\cscscript.csproj /p:AssemblyName=cscs /p:TargetFrameworkVersion=v4.5  /p:PlatformTarget=x86 /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45" %common_msbuild_params%
move ..\cscscript\bin\Distro\cscs.exe ..\Build\cscs32.exe
ECHO ------------ >> ..\Build\build.log

ECHO Building csws.exe: >> ..\Build\build.log
"%net45_tools%\msbuild.exe" ..\cswscript\cswscript.csproj /p:AssemblyName=csws /p:TargetFrameworkVersion=v4.5  /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45" %common_msbuild_params%
move ..\cswscript\bin\Distro\csws.exe ..\Build\csws.exe
ECHO ------------ >> ..\Build\build.log

ECHO Building csws32.exe: >> ..\Build\build.log
"%net45_tools%\msbuild.exe" ..\cscscript\cscscript.csproj /p:AssemblyName=csws /p:TargetFrameworkVersion=v4.5  /p:PlatformTarget=x86 /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45" %common_msbuild_params%
move ..\cscscript\bin\Distro\csws.exe ..\Build\csws32.exe
ECHO ------------ >> ..\Build\build.log

ECHO Building CSScriptLibrary.dll (unsigned): >> ..\Build\build.log
"%net45_tools%\msbuild.exe" ..\CSScriptLibrary\CSScriptLibrary.csproj /p:AssemblyName=CSScriptLibrary /p:TargetFrameworkVersion=v4.5  /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45;InterfaceAssembly;CSSLib_BuildUnsigned" %common_msbuild_params%
ECHO ------------ >> ..\Build\build.log
move ..\CSScriptLibrary\bin\Distro\CSScriptLibrary.dll ..\Build\CSScriptLibrary.dll.unsigned

ECHO Building CSScriptLibrary.dll: >> ..\Build\build.log
"%net45_tools%\msbuild.exe" ..\CSScriptLibrary\CSScriptLibrary.csproj /p:AssemblyName=CSScriptLibrary /p:TargetFrameworkVersion=v4.5  /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45;InterfaceAssembly" /t:Rebuild  %common_msbuild_params%
ECHO ------------ >> ..\Build\build.log
move ..\CSScriptLibrary\bin\Distro\CSScriptLibrary.dll ..\Build\CSScriptLibrary.dll
move ..\CSScriptLibrary\bin\Distro\CSScriptLibrary.xml ..\Build\CSScriptLibrary.xml

ECHO Building css_config:
ECHO Building css_config: >> ..\Build\build.log
"%net45_tools%\msbuild.exe" ..\css_config\css_config.csproj /p:TargetFrameworkVersion=v4.5  /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro %common_msbuild_params%
move ..\css_config\bin\Distro\css_config.exe ..\Build\css_config.exe
ECHO ------------ >> ..\Build\build.log

set CS-S_DEV_ROOT=%cd%

rem rem -------------------------------------------------------------------------------------
cd ..\Build
rem rem need to ensure ConfigConsole.cs doesn't contain "{ 25D84CB0", which is a formatting CSScript.Npp artifact
cscs.exe /l /dbg /pvdr:none /nl "..\ConfigConsole\buildCheck.cs" >> ..\Build\build.log
rem need to remap CSSCRIPT_DIR as otherwise ConfigConsole will be linked against choco CS-S binaries
set old_css_dir=%CSSCRIPT_DIR%
set CSSCRIPT_DIR=%CS-S_DEV_ROOT%

ECHO Building ConfigConsole.exe:
ECHO Building ConfigConsole.exe: >> ..\Build\build.log
cscs.exe /nl /l /dbg -pvdr:none /ew "..\ConfigConsole\ConfigConsole.cs"
move ..\ConfigConsole\ConfigConsole.exe ..\Build\ConfigConsole.exe
ECHO ------------ >> ..\Build\build.log

set CSSCRIPT_DIR=%old_css_dir%

ECHO Copying CSSRoslynProvider.dll: >> ..\Build\build.log
copy ..\CSSRoslynProvider\CSSRoslynProvider.dll CSSRoslynProvider.dll
ECHO ------------ >> ..\Build\build.log

ECHO Building runasm32.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo  /o /platform:x86 /out:runasm32.exe /t:exe ..\runasm32.cs /r:System.dll >> build.log
ECHO ------------ >> ..\Build\build.log

rem pause to allow all apps (ant-viruses, compilers) exit and release the temp files
ping 1.1.1.1 -n 1 -w 2000 > nul

ECHO Cleaning up...
del  sgKey.snk
del /S /Q temp\*
RD temp\temp
RD temp

notepad build.log
del build.log
:exit

