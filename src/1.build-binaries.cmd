echo off

set vs_edition=Community

if exist "C:\Program Files\Microsoft Visual Studio\2022\%vs_edition%" (
    echo Visual Studio 2022 (Community)
) else (
    set vs_edition=Professional
    echo Visual Studio 2022 (PRO)
)

set PATH=%PATH%;%%\out\ci\
set target=net7.0
md "out\Windows"
md "out\Windows\lib"
md "out\Linux\"
md "out\Linux\lib"
md "out\Linux\-self"
md "out\Linux\-self\-exe"
md "out\Linux\-self\-test"
md "out\Windows\-self"
md "out\Windows\-self\-exe"
md "out\Windows\-self\-test"
md "out\Windows\-wdbg"
md "out\Windows\-wdbg\dbg-server"

rem in case some content is already there
del /S /Q "out\Linux\"
del /S /Q "out\Windows\"
rd /S /Q "out\Linux\-wdbg"
rd /S /Q "out\Windows\-wdbg"
del "CSScriptLib\src\CSScriptLib\output\*.nupkg"
del "CSScriptLib\src\CSScriptLib\output\*.snupkg"
del "out\cs-script.win.7z"
del "out\cs-script.linux.7z"


rem set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2019\%vs_edition%\MSBuild\Current\Bin\MSBuild.exe"
rem %msbuild% ".\css\css (win launcher).csproj" -p:Configuration=Release -t:rebuild
rem copy .\css\bin\Release\css.exe ".\out\Windows\css.exe"


echo =====================
echo Building (cd: %cd%)
echo =====================

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
dotnet publish -c Release -f %target% -o "..\out\Windows\console"

echo ----------------
echo Building cscs.dll (Linux) from %cd%
echo ----------------

dotnet publish -c Release -f %target% -r linux-x64 --self-contained false -o "..\out\Linux"

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
dotnet publish -o .\output
popd


echo =====================
echo Aggregating (cd: %cd%)
echo =====================
copy "out\Windows\win" "out\Windows" /Y
copy "out\Windows\console" "out\Windows" /Y
del "out\Linux\*.pdb" 
del "out\Windows\*.pdb" 
rd "out\Windows\win" /S /Q
rd "out\Windows\console" /S /Q

copy "out\static_content\global-usings.cs" "out\Windows\lib\global-usings.cs" 
copy "out\static_content\global-usings.cs" "out\Linux\lib\global-usings.cs"

copy "out\static_content\-self\*" "out\Windows\-self\" 
copy "out\static_content\-self\*" "out\Linux\-self\" 

copy "out\static_content\-self\-exe\*" "out\Windows\-self\-exe\" 
copy "out\static_content\-self\-exe\*" "out\Linux\-self\-exe\" 

copy "out\static_content\-self\-test\*" "out\Windows\-self\-test\" 
copy "out\static_content\-self\-test\*" "out\Linux\-self\-test\" 

xcopy /s /q "out\static_content\-wdbg\*" "out\Windows\-wdbg\" 
xcopy /s /q "out\static_content\-wdbg\*" "out\Linux\-wdbg\" 
rd /S /Q .\out\Linux\-wdbg\dbg-server\bin\
rd /S /Q .\out\Windows\-wdbg\dbg-server\bin\
rd /S /Q .\out\WindowLinux\-wdbg\dbg-server\obj\
rd /S /Q .\out\Windows\-wdbg\dbg-server\obj\
rd /S /Q .\out\WindowLinux\-wdbg\test\
rd /S /Q .\out\Windows\-wdbg\test\
del out\Linux\-wdbg\test*.cs
del out\Windows\-wdbg\test*.cs

copy "Tests.cscs\cli.cs" "out\Linux\-self\-test\cli.cs" 
copy "Tests.cscs\cli.cs" "out\Windows\-self\-test\cli.cs" 

copy "out\static_content\readme.md" "out\Linux\readme.md" 

cd out\Windows
.\cscs.exe -self-exe-build 

echo =====================
echo Aggregating packages (cd: %cd%)
echo =====================
.\cscs -c:0 ..\..\CSScriptLib\src\CSScriptLib\output\aggregate.cs
cd ..\..

copy CSScriptLib\src\CSScriptLib\output\*.*nupkg out\

echo =====================
echo Packaging (cd: %cd%)
echo =====================

cd out\Linux
echo cd: %cd%
..\ci\7z.exe a -r "..\cs-script.linux.7z" "*.*"
cd ..\..

cd out\Windows
echo cd: %cd%
..\ci\7z.exe a -r "..\cs-script.win.7z" "*.*"
cd ..\..

echo =====================
echo Injecting version in file names
echo =====================

cd out\Windows
.\cscs -c:0 ..\..\CSScriptLib\src\CSScriptLib\output\aggregate.cs
.\cscs -engine:dotnet -code Console.WriteLine(Assembly.LoadFrom(#''cscs.dll#'').GetName().Version)
.\cscs -engine:dotnet -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.win.7z#'', $#''..\\cs-script.win.v{version}.7z#'', true);
.\cscs -engine:dotnet -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\cs-script.linux.7z#'', $#''..\\cs-script.linux.v{version}.7z#'', true);
.\cscs -help cli:md > ..\CS-Script---Command-Line-Interface.md
cd ..\..

move "CSScriptLib\src\CSScriptLib\output\*.nupkg" ".\out"
move "CSScriptLib\src\CSScriptLib\output\*.snupkg" ".\out"

del out\cs-script.win.7z
del out\cs-script.linux.7z

echo Updating help.txt
.\out\Windows\cscs.exe -? > ..\help.txt 

rem echo Published: %cd%
rem cd ..\..\.
:exit 
