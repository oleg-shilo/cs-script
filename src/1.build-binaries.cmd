echo off

set vs_edition=Community

if exist "C:\Program Files\Microsoft Visual Studio\2022\%vs_edition%" (
    echo Visual Studio 2022 (Community)
) else (
    set vs_edition=Professional
    echo Visual Studio 2022 (PRO)
)

set PATH=%PATH%;%%\out\ci\
set target=net9.0
set target_prev=net8.0
md "out\Linux\"
md "out\Linux\lib"
md "out\Windows"
md "out\Windows\lib"

rem in case some content is already there
del /S /Q "out\Linux\"
del /S /Q "out\Windows\"
rd /S /Q "out\Linux\-wdbg"
rd /S /Q "out\Windows\-wdbg"
del "CSScriptLib\src\CSScriptLib\output\*.nupkg"
del "CSScriptLib\src\CSScriptLib\output\*.snupkg"
del "out\cs-script.win.7z"
del "out\cs-script.linux.7z"

rem goto:agregate

echo =====================
echo Building (cd: %cd%)
echo ---------------------

del .\out\*.*nupkg 

cd BuildServer
echo ----------------
echo Building build.dll from %cd%
echo ----------------
dotnet publish -c Release 

cd ..\cscs
echo ----------------
echo Building cscs.dll from %cd%
echo ----------------

dotnet publish -c Release -f %target_prev% -o "..\out\win.net8" cscs.8.csproj
rd /s /q .\obj
dotnet publish -c Release -f %target% -o "..\out\Windows\console" cscs.csproj

rem goto:exit

echo ----------------
echo Building cs-script.cli .NET tool from %cd%
echo ----------------
del .\nupkg\*.*nupkg 
dotnet pack cscs.csproj
copy .\nupkg\*  ..\out\

echo ----------------
echo Building cscs.dll (Linux) from %cd%
echo ----------------

dotnet publish -c Release -f %target% -r linux-x64 --self-contained false -o "..\out\Linux" cscs.csproj

cd ..\csws
echo ----------------
echo Building csws.dll from %cd%
echo ----------------
dotnet publish -c Release -f %target%-windows -o "..\out\Windows\win"

cd ..\CSScriptLib\src\CSScriptLib
echo ----------------
echo Building CSScriptLib.dll from %cd%
echo ----------------
dotnet build -c Release

cd ..\..\..

pushd .\

cd .\out\static_content\-wdbg\dbg-server
echo ----------------
echo Building WDBG from %cd%
dotnet publish -o .\output server.csproj
popd

:agregate
echo =====================
echo Aggregating (cd: %cd%)
echo ---------------------
copy "out\Windows\win" "out\Windows" /Y
copy "out\Windows\console" "out\Windows" /Y
del "out\Linux\*.pdb" 
del "out\Windows\*.pdb" 
rd "out\Windows\win" /S /Q
rd "out\Windows\console" /S /Q

rem .\static_content contains Linux and Win specific files
copy "out\static_content\global-usings.cs" "out\Windows\lib\global-usings.cs" 
copy "out\static_content\global-usings.cs" "out\Linux\lib\global-usings.cs"

xcopy /s /q /y "out\static_content\-mkshim\*" "out\Windows\-mkshim\" 

xcopy /s /q /y "out\static_content\-web\*" "out\Windows\-web\" 
xcopy /s /q /y "out\static_content\-web\*" "out\Linux\-web\" 

xcopy /s /q /y "out\static_content\-set\*" "out\Windows\-set\" 
xcopy /s /q /y "out\static_content\-set\*" "out\Linux\-set\" 

xcopy /s /q /y "out\static_content\-self\*" "out\Windows\-self\" 
xcopy /s /q /y "out\static_content\-self\*" "out\Linux\-self\" 

xcopy /s /q /y "out\static_content\-wdbg\*" "out\Windows\-wdbg\" 
xcopy /s /q /y "out\static_content\-wdbg\*" "out\Linux\-wdbg\" 

rem goto:exit
echo =====================
echo Clearing possible WDBG test/dev files
echo (it's normal if files are not found)
echo ---------------------
rd /S /Q .\out\Linux\-wdbg\dbg-server\bin\
rd /S /Q .\out\Windows\-wdbg\dbg-server\bin\
rd /S /Q .\out\WindowLinux\-wdbg\dbg-server\obj\
rd /S /Q .\out\Windows\-wdbg\dbg-server\obj\
del out\Linux\-wdbg\test*.cs
del out\Windows\-wdbg\test*.cs

copy "Tests.cscs\cli.cs" "out\Linux\-self\-test\cli.cs" 
copy "Tests.cscs\cli.cs" "out\Windows\-self\-test\cli.cs" 

copy "out\static_content\readme.md" "out\Linux\readme.md" 
copy "out\static_content\install.sh" "out\Linux\install.sh" 

cd out\Windows


echo =====================
echo Aggregating packages (cd: %cd%)
echo ---------------------
.\cscs -c:0 ..\..\CSScriptLib\src\CSScriptLib\output\aggregate.cs
cd ..\..

copy CSScriptLib\src\CSScriptLib\output\*.*nupkg out\

echo =====================
echo Packaging (cd: %cd%)
echo ---------------------

cd out\Linux
echo cd: %cd%
..\ci\7z.exe a -r "..\cs-script.linux.7z" "*.*"
cd ..\..

cd out\win.net8
echo cd: %cd%
..\ci\7z.exe a -r "..\cs-script.win.net8.7z" "*.*"
cd ..\..

cd out\Windows
echo cd: %cd%

echo ==========================================
echo .\cscs -l:0 -c:0 -ng:csc -mkshim css.exe cscs.exe
.\cscs -l:0 -c:0 -ng:csc -mkshim css.exe cscs.exe
echo ==========================================

..\ci\7z.exe a -r "..\cs-script.win.zip" "*.*"
..\ci\7z.exe a -r "..\cs-script.win.7z" "*.*"
cd ..\..

echo =====================
echo Injecting version in file names
echo ---------------------

cd out\Windows
.\cscs -c:0 ..\..\CSScriptLib\src\CSScriptLib\output\aggregate.cs
.\cscs -engine:dotnet -code Console.WriteLine(Assembly.LoadFrom(#''cscs.dll#'').GetName().Version)
.\cscs -engine:dotnet -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.win.7z#'', $#''..\\cs-script.win.v{version}.7z#'', true);
.\cscs -engine:dotnet -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.win.net8.7z#'', $#''..\\cs-script.win.net8.v{version}.7z#'', true);
.\cscs -engine:dotnet -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.win.zip#'', $#''..\\cs-script.win.v{version}.zip#'', true);
.\cscs -engine:dotnet -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.linux.7z#'', $#''..\\cs-script.linux.v{version}.7z#'', true);
@REM md ..\..\..\cs-script.wiki
.\cscs -help cli:md > ..\..\..\..\cs-script.wiki\CS-Script---Command-Line-Interface.md
.\cscs -help syntax:md > ..\..\..\..\cs-script.wiki\Script-Syntax.md

cd ..\..

move "CSScriptLib\src\CSScriptLib\output\*.nupkg" ".\out"
move "CSScriptLib\src\CSScriptLib\output\*.snupkg" ".\out"

del out\cs-script.win.7z
del out\cs-script.win.net8.7z
del out\cs-script.linux.7z

echo Updating help.txt
.\out\Windows\cscs.exe -help > ..\help.txt 


echo Published: %cd%
rem cd ..\..\.
:exit 
