echo off
rem Many builds are commented out as they are no longer included into distro. But they are kept to allow anyone to build them if required.
@set BATCH_BUILD=true
@set oldPATH=%PATH%;
@set PATH=%windir%\Microsoft.NET\Framework\v1.1.4322;%PATH%;
rem @set net4_tools=C:\Program Files (x86)\MSBuild\12.0\Bin
@set net4_tools=C:\Windows\Microsoft.NET\Framework\v4.0.30319
rem @set net45_tools=C:\Program Files (x86)\MSBuild\14.0\Bin
rem @set net45_tools=C:\Program Files (x86)\MSBuild\15.0\Bin
rem @set net45_tools=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\
rem @set net45_tools=C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\
rem @set net45_tools=C:\Program Files (x86)\Microsoft Visual Studio\2017\%VS_EDITION%\MSBuild\15.0\Bin\
@set net45_tools=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin
@set local_dev=%CS-S_DEV_ROOT%
@set is_local_dev=true
if "%local_dev%" == "" @set is_local_dev=false

if %is_local_dev% == true echo --- LOCAL DEVELOPMENT ENVIRONMENT ---

rem @set net4_asms=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5
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

rem CS-Script utils  ----------------------------------------------------

rem ECHO Building csscript resources:
rem ECHO Building csscript resources: >> ..\Build\build.log
rem "resgen.exe" ..\cscscript\Resources.resx  >> build.log
rem move ..\cscscript\Resources.resources ..\cscscript\cscscript.Resources.resources
rem ECHO ------------ >> build.log

ECHO Building css_config.exe:
ECHO Building css_config.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo %common_4_params% /unsafe- /nowarn:1701,1702 /o /out:css_config.exe /win32manifest:..\ChooseDefaultProgram\app.manifest /win32icon:..\Logo\css_logo.ico /resource:..\css_config\SplashScreen.resources /target:winexe ..\css_config\AssemblyInfo.cs ..\css_config\css_config.cs ..\css_config\Program.cs ..\css_config\SplashForm.cs ..\css_config\VistaSecurity.cs /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Drawing.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building CS-Script.exe:
ECHO Building CS-Script.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo %common_4_params%  /unsafe- /nowarn:1701,1702 /o /out:CS-Script.exe /resource:..\CS-Script\CSScript.Properties.Resources.resources /target:winexe /win32icon:..\Logo\css_logo.ico ..\CS-Script\Properties\AssemblyInfo.cs ..\CS-Script\Program.cs /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Drawing.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building ChooseDefaultProgram.exe:
ECHO Building ChooseDefaultProgram.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo %common_4_params%  /unsafe- /nowarn:1701,1702 /o /out:ChooseDefaultProgram.exe /win32manifest:..\ChooseDefaultProgram\app.manifest /resource:..\ChooseDefaultProgram\CSScript.Resources.resources /target:exe /win32icon:..\Logo\css_logo.ico  ..\ChooseDefaultProgram\Resources.Designer.cs ..\ChooseDefaultProgram\AssemblyInfo.cs ..\ChooseDefaultProgram\ChooseDefaultProgram.cs /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> build.log
ECHO ------------ >> build.log

rem .NET v1.1-4.0 ----------------------------------------------------

rem ECHO Building cscs.v3.5.exe:
rem ECHO Building cscs.v3.5.exe: >> build.log
rem %windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,618 /o /define:net35 /out:cscs.exe /t:exe %common_source_files% ..\cscscript\CSExecutionClient.cs ..\cscscript\Properties\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll /r:System.Core.dll %common_ref_files% >> build.log
rem rem %windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,618 /o /define:net35 /out:cscs.v3.5.exe /t:exe %common_source_files% ..\cscscript\CSExecutionClient.cs ..\cscscript\Resources.Designer.cs ..\cscscript\Properties\AssemblyInfo.cs /resource:..\cscscript\cscscript.Resources.resources /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll /r:System.Core.dll %common_ref_files% >> build.log
rem ECHO ------------ >> build.log
rem move cscs.exe cscs.v3.5.exe

rem ECHO Building csws.v3.5.exe:
rem ECHO Building csws.v3.5.exe: >> build.log
rem %windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,618 /o /define:net35 /out:csws.exe /t:winexe %common_source_files% ..\cswscript\CSExecutionClient.cs ..\cswscript\Properties\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Core.dll /r:System.Windows.Forms.dll %common_ref_files% >> build.log
rem ECHO ------------ >> build.log
rem move csws.exe csws.v3.5.exe

REM ECHO Building cscs.exe (v1.1):
REM ECHO Building cscs.exe (v1.1): >> build.log
REM %windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /nowarn:169,618,1699 /define:net1 /o /out:cscs.v1.1.exe /t:exe %common_source_files% ..\cscscript\CSExecutionClient.cs ..\cscscript\Properties\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll %common_ref_files% >> build.log
REM ECHO ------------ >> build.log

REM ECHO Building csws.exe (v1.1):
REM ECHO Building csws.exe (v1.1): >> build.log
REM %windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /nowarn:169,618,1699 /define:net1 /o /out:csws.v1.1.exe /t:winexe %common_source_files% ..\cswscript\CSExecutionClient.cs ..\cswscript\Properties\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll %common_ref_files% >> build.log
REM ECHO ------------ >> build.log

cd ..\CSScriptLibrary
REM ECHO Building CSScriptLibrary.v1.1.dll:
REM ECHO Building CSScriptLibrary.v1.1.dll: >> ..\Build\build.log
REM %windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /nowarn:169,618,1699 /define:net1 /o /doc:..\Build\temp\temp\CSScriptLibrary.v1.1.xml /out:..\Build\temp\temp\CSScriptLibrary.v1.1.dll /t:library %common_source_files% CSScriptLib.cs crc32.cs AsmHelper.cs Properties\AssemblyInfo.cs  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll %common_ref_files% >> ..\Build\build.log
REM ECHO ------------ >> ..\Build\build.log

rem ECHO Building CSScriptLibrary.v3.5.dll:
rem ECHO Building CSScriptLibrary.v3.5.dll: >> ..\Build\build.log
rem %windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,1699,618 /define:net35 /o /doc:..\Build\temp\temp\CSScriptLibrary.v3.5.xml /out:..\Build\temp\temp\CSScriptLibrary.dll /t:library %common_source_files% CSScriptLib.cs AsmHelper.cs ObjectCaster.cs Properties\AssemblyInfo.cs crc32.cs /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll %common_ref_files% >> ..\Build\build.log
rem ECHO ------------ >> ..\Build\build.log
rem move ..\Build\temp\temp\CSScriptLibrary.dll ..\Build\temp\temp\CSScriptLibrary.v3.5.dll

rem ECHO Building CSScriptLibrary.v3.5.dll (renamed):
rem ECHO Building CSScriptLibrary.v3.5.dll (renamed): >> ..\Build\build.log
rem %windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,1699,618 /define:net35 /o /doc:CSScriptLibrary.xml /out:..\Build\temp\temp\CSScriptLibrary.dll /t:library %common_source_files% CSScriptLib.cs AsmHelper.cs ObjectCaster.cs Properties\AssemblyInfo.cs crc32.cs /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll %common_ref_files% >> ..\Build\build.log
rem ECHO ------------ >> ..\Build\build.log

rem .NET v4.0 -------------------------------------------------------------------------------------

ECHO Building CSScript TargetFramework: v4.0:
ECHO Building CSScript.v4.0: >> ..\Build\build.log
"%net4_tools%\msbuild.exe" ..\CSScriptLibrary\CSScriptLibrary.v4.0.sln /p:configuration=Release /p:platform="Any CPU" %common_msbuild_params%
ECHO ------------ >> ..\Build\build.log

rem .NET v4.5 -------------------------------------------------------------------------------------

ECHO Building CSScript TargetFramework: v4.5:
ECHO Building cscs.exe: >> ..\Build\build.log
"%net4_tools%\msbuild.exe" ..\cscscript\cscscript.csproj /p:AssemblyName=cscs /p:TargetFrameworkVersion=v4.5  /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45" %common_msbuild_params%
ECHO ------------ >> ..\Build\build.log

move ..\cscscript\bin\Distro\cscs.exe ..\Build\cscs.exe
..\Build\cscs.exe -? > "%local_dev%\help.txt"
..\Build\cscs.exe -? > ..\..\..\cs-script\help.txt
ECHO Building cscs32.exe: >> ..\Build\build.log
"%net4_tools%\msbuild.exe" ..\cscscript\cscscript.csproj /p:AssemblyName=cscs /p:TargetFrameworkVersion=v4.5  /p:PlatformTarget=x86 /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45" %common_msbuild_params%
move ..\cscscript\bin\Distro\cscs.exe ..\Build\cscs32.exe
ECHO ------------ >> ..\Build\build.log

ECHO Building csws.exe: >> ..\Build\build.log
"%net4_tools%\msbuild.exe" ..\cswscript\cswscript.csproj /p:AssemblyName=csws /p:TargetFrameworkVersion=v4.5  /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45" %common_msbuild_params%
move ..\cswscript\bin\Distro\csws.exe ..\Build\csws.exe
ECHO ------------ >> ..\Build\build.log

ECHO Building csws32.exe: >> ..\Build\build.log
"%net4_tools%\msbuild.exe" ..\cscscript\cscscript.csproj /p:AssemblyName=csws /p:TargetFrameworkVersion=v4.5  /p:PlatformTarget=x86 /p:configuration=Release;Platform="AnyCPU" /p:OutDir=bin\Distro /p:DefineConstants="net4;net45" %common_msbuild_params%
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
copy ..\Build\CSScriptLibrary.dll %CS-S_DEV_ROOT%\lib\CSScriptLibrary.dll
copy ..\Build\CSScriptLibrary.xml %CS-S_DEV_ROOT%\lib\CSScriptLibrary.xml

rem -------------------------------------------------------------------------------------
cd ..\Build
rem need to ensure ConfigConsole.cs doesn't contain "{ 25D84CB0", which is a formatting CSScript.Npp artifact
cscs.exe /l /dbg /pvdr:none /nl "..\ConfigConsole\buildCheck.cs" >> ..\Build\build.log

rem neet to remap CSSCRIPT_DIR as otherwise ConfigConsole will be linked against choco CS-S binaries
set old_css_dir=%CSSCRIPT_DIR%
set CSSCRIPT_DIR=%CS-S_DEV_ROOT%

ECHO Building ConfigConsole.exe:
ECHO Building ConfigConsole.exe: >> ..\Build\build.log
cscs.exe /nl /l /dbg -pvdr:none /ew "..\ConfigConsole\ConfigConsole.cs"
move ..\ConfigConsole\ConfigConsole.exe ..\Build\ConfigConsole.exe
ECHO ------------ >> ..\Build\build.log
set CSSCRIPT_DIR=%old_css_dir%
set build_tools=E:\PrivateData\Galos

REM move temp\temp\CSScriptLibrary.v1.1.xml CSScriptLibrary.v1.1.xml
REM move temp\temp\CSScriptLibrary.v1.1.dll CSScriptLibrary.v1.1.dll
move temp\temp\CSScriptLibrary.v3.5.xml CSScriptLibrary.v3.5.xml
move temp\temp\CSScriptLibrary.v3.5.dll CSScriptLibrary.v3.5.dll

if %is_local_dev% == false goto after_update_local

REM copy cscs.v1.1.exe "%local_dev%\Lib\Bin\NET 1.1\cscs.exe"
REM copy csws.v1.1.exe "%local_dev%\Lib\Bin\NET 1.1\csws.exe"
REM copy CSScriptLibrary.v1.1.xml "%local_dev%\Lib\Bin\NET 1.1\CSScriptLibrary.v1.1.xml"
REM copy CSScriptLibrary.v1.1.dll "%local_dev%\Lib\Bin\NET 1.1\CSScriptLibrary.v1.1.dll"
copy cscs.v3.5.exe "%local_dev%\Lib\Bin\NET 3.5\cscs.exe"
copy csws.v3.5.exe "%local_dev%\Lib\Bin\NET 3.5\csws.exe"
copy cscs.exe "%CS-S_DEV_ROOT%\Lib\Bin\Linux\cscs.exe"
copy CSScriptLibrary.v3.5.xml "%local_dev%\Lib\Bin\NET 3.5\CSScriptLibrary.xml"
copy CSScriptLibrary.v3.5.dll "%local_dev%\Lib\Bin\NET 3.5\CSScriptLibrary.dll"
copy CSScriptLibrary.v3.5.dll "%local_dev%\Samples\Hosting\Legacy Samples\CodeDOM\VS2008 project\Lib\CSScriptLibrary.v3.5.dll"
copy CSScriptLibrary.v3.5.dll "%local_dev%\Samples\Hosting\Legacy Samples\CodeDOM\Older versions of CLR\CSScriptLibrary.v3.5.dll"
copy CSScriptLibrary.v3.5.xml "%local_dev%\Samples\Hosting\Legacy Samples\CodeDOM\VS2008 project\Lib\CSScriptLibrary.v3.5.xml"
copy ..\CSScriptLibrary\bin\Release.4.0\CSScriptLibrary.xml "%local_dev%\Lib\Bin\NET 4.0\CSScriptLibrary.xml"
copy ..\CSScriptLibrary\bin\Release.4.0\CSScriptLibrary.dll "%local_dev%\Lib\Bin\NET 4.0\CSScriptLibrary.dll"
copy ..\cswscript\bin\Release.4.0\csws.exe "%local_dev%\Lib\Bin\NET 4.0\csws.exe"
copy ..\cscscript\bin\Release.4.0\cscs.exe "%local_dev%\Lib\Bin\NET 4.0\cscs.exe"

copy ..\CSScriptLibrary\bin\Release.4.0\CSScriptLibrary.dll "%local_dev%\Samples\Hosting\Legacy Samples\CodeDOM\VS2010 project\Lib\CSScriptLibrary.dll"
copy ..\CSScriptLibrary\bin\Release.4.0\CSScriptLibrary.xml "%local_dev%\Samples\Hosting\Legacy Samples\CodeDOM\VS2010 project\Lib\CSScriptLibrary.xml"

copy cscs.exe "%local_dev%\Lib\Bin\NET 4.5\cscs.exe"
copy csws.exe "%local_dev%\Lib\Bin\NET 4.5\csws.exe"
copy cscs32.exe "%local_dev%\Lib\Bin\NET 4.5\cscs32.exe"
copy csws32.exe "%local_dev%\Lib\Bin\NET 4.5\csws32.exe"
copy CSScriptLibrary.xml "%local_dev%\Lib\Bin\NET 4.5\CSScriptLibrary.xml"
copy CSScriptLibrary.dll "%local_dev%\Lib\Bin\NET 4.5\CSScriptLibrary.dll"
copy CSScriptLibrary.xml "%local_dev%\Samples\Hosting\Legacy Samples\CodeDOM\VS2012 project\Lib\CSScriptLibrary.xml"
copy CSScriptLibrary.dll "%local_dev%\Samples\Hosting\Legacy Samples\CodeDOM\VS2012 project\Lib\CSScriptLibrary.dll"
copy CSScriptLibrary.dll.unsigned "%local_dev%\Lib\Bin\NET 4.5\CSScriptLibrary.dll.unsigned"
copy cscs.exe "%local_dev%\cscs.exe"
copy csws.exe "%local_dev%\csws.exe"
copy css_config.exe "%local_dev%\css_config.exe"
rem copy css_config.exe ..\..\..\css_config.exe
copy ConfigConsole.exe "%local_dev%\Lib\ConfigConsole\ConfigConsole.exe"
copy ..\Mono.CSharp.dll Mono.CSharp.dll
copy ..\Mono.CSharp.dll "%local_dev%\Lib\Mono.CSharp.dll"
copy CSScriptLibrary.dll "%local_dev%\Lib\CSScriptLibrary.dll"
copy CSScriptLibrary.xml "%local_dev%\Lib\CSScriptLibrary.xml"
copy CS-Script.exe "%local_dev%\Lib\ShellExtensions\CS-Script.exe"

:after_update_local

REM ECHO Building CSSCodeProvider.v1.1.dll: >> ..\Build\build.log
REM rem cscs.exe /nl /noconfig /cd ..\CSSCodeProvider\CSSCodeProvider.cs >> ..\Build\build.log
REM %windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /nowarn:618,162 /define:net1 /o /out:..\Build\temp\temp\CSSCodeProvider.v1.1.dll /t:library ..\CSSCodeProvider\CSSCodeProvider.cs ..\CSSCodeProvider\ccscompiler.cs ..\CSSCodeProvider\AssemblyInfo.cs ..\CSSCodeProvider\cppcompiler.cs ..\CSSCodeProvider\xamlcompiler.cs /r:System.dll /r:Microsoft.JScript.dll /r:System.Windows.Forms.dll >> build.log
REM move ..\Build\temp\temp\CSSCodeProvider.v1.1.dll CSSCodeProvider.v1.1.dll
REM rem ECHO ------------ECHO ------------ >> ..\Build\build.log
REM ECHO ------------ >> build.log

rem ECHO Building CSSCodeProvider.v3.5.dll: >> ..\Build\build.log
rem %windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:618 /o /out:..\Build\temp\temp\CSSCodeProvider.v3.5.dll /t:library ..\CSSCodeProvider.v3.5\CSSCodeProvider.cs ..\CSSCodeProvider.v3.5\ccscompiler.cs ..\CSSCodeProvider.v3.5\AssemblyInfo.cs ..\CSSCodeProvider.v3.5\cppcompiler.cs ..\CSSCodeProvider.v3.5\xamlcompiler.cs /r:System.dll /r:Microsoft.JScript.dll /r:System.Windows.Forms.dll >> build.log
rem move ..\Build\temp\temp\CSSCodeProvider.v3.5.dll CSSCodeProvider.v3.5.dll
rem ECHO ------------ >> ..\Build\build.log

rem ECHO Building CSScript.Tasks.dll: >> ..\Build\build.log
rem "%net4_tools%\csc.exe" /nologo /nowarn:618,162 /debug+ /debug:full /o /out:..\Build\temp\temp\CSScript.Tasks.dll /t:library ..\NAnt.CSScript\CSScript.Tasks.cs ..\NAnt.CSScript\AssemblyInfo.cs /r:System.dll /r:CSScriptLibrary.dll /r:System.Core.dll /r:System.Xml.dll /r:..\BuildTools\nant\bin\NAnt.Core.dll >> build.log
rem move ..\Build\temp\temp\CSScript.Tasks.dll CSScript.Tasks.dll
rem copy CSScript.Tasks.dll "%local_dev%\Lib\CSScript.Tasks.dll"
rem copy CSScript.Tasks.dll "%local_dev%\Samples\NAnt\CSScript.Tasks.dll"
rem copy CSScriptLibrary.dll "%local_dev%\Samples\NAnt\CSScriptLibrary.dll"
rem ECHO ------------ >> ..\Build\build.log

rem ECHO Building CSSCodeProvider.dll: >> ..\Build\build.log
rem "%net4_tools%\csc.exe" /nologo /nowarn:618 /o  /out:..\Build\temp\temp\CSSCodeProvider.dll /t:library ..\CSSCodeProvider.v4.0\CSSCodeProvider.cs ..\CSSCodeProvider.v4.0\ccscompiler.cs ..\CSSCodeProvider.v4.0\AssemblyInfo.cs ..\CSSCodeProvider.v4.0\cppcompiler.cs ..\CSSCodeProvider.v4.0\xamlcompiler.cs /r:System.dll /r:Microsoft.JScript.dll /r:System.Windows.Forms.dll >> build.log
rem copy ..\Build\temp\temp\CSSCodeProvider.dll "%local_dev%\Lib\CSSCodeProvider.dll"
rem move ..\Build\temp\temp\CSSCodeProvider.dll CSSCodeProvider.dll
rem ECHO ------------ >> ..\Build\build.log

rem ECHO Building CSSCodeProvider.v4.6.dll: >> ..\Build\build.log
rem cannot build v4.6 with csc.exe it needs to be VS+manual build
rem ECHO Building CSSCodeProvider.v4.6.dll: >> ..\Build\build.log
rem "%net4_tools%\csc.exe" /nologo /nowarn:618 /o /out:..\Build\temp\temp\CSSCodeProvider.v4.6.dll /t:library ..\CSSCodeProvider.v4.6\CSSCodeProvider.cs ..\CSSCodeProvider.v3.5\AssemblyInfo.cs /r:System.dll /r:%build_tools%\Projects\CS-Script\Src\CSSCodeProvider.v4.6\bin\roslyn\Microsoft.CodeDom.Providers.DotNetCompilerPlatform.dll >> build.log
rem ECHO Copying Roslyn binaries... >> ..\Build\build.log
rem copy ..\CSSCodeProvider.Legacy\CSSCodeProvider.v4.6\bin\roslyn\*.* "%local_dev%\Lib\Bin\Roslyn\"

rem ECHO Building CSSCodeProvider.v4.6.dll: >> ..\Build\build.log
copy ..\CSSRoslynProvider\bin\Release\CSSRoslynProvider.dll "%local_dev%\Lib\CSSRoslynProvider.dll"
copy ..\CSSRoslynProvider\bin\Release\CSSRoslynProvider.dll "%local_dev%\Lib\Bin\Linux\CSSRoslynProvider.dll"
copy ..\CSSRoslynProvider\bin\Release\CSSRoslynProvider.dll CSSRoslynProvider.dll
ECHO ------------ >> ..\Build\build.log

rem ECHO Building css.exe: >> ..\Build\build.log
rem %windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /define:net1 /o /out:css.exe /win32icon:..\Logo\css_logo.ico /t:exe ..\css.cs ..\CS-S.AsmInfo.cs /r:System.dll >> build.log
rem ECHO ------------ >> ..\Build\build.log

ECHO Building runasm32.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo  /o /platform:x86 /out:runasm32.exe /t:exe ..\runasm32.cs /r:System.dll >> build.log
ECHO ------------ >> ..\Build\build.log

ECHO Building CSSPostSharp.dll: >> ..\Build\build.log
%windir%\Microsoft.NET\Framework\v3.5\csc /nologo /o /out:CSSPostSharp.dll /t:library ..\CSSPostSharp.cs /r:System.dll /r:System.Core.dll  >> build.log
ECHO ------------ >> ..\Build\build.log

copy CSSPostSharp.dll "%local_dev%\Lib\CSSPostSharp.dll"
copy runasm32.exe "%local_dev%\Lib\runasm32.exe"

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


