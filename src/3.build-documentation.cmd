echo off

.\out\Windows\css.exe -server_r:stop
.\out\Windows\css.exe -server:stop

set vs_edition=undefined
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community" set vs_edition=Community
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional" set vs_edition=Professional

echo Visual Studio 2022 (%vs_edition%)

if exist .\CSScriptLib.Doc\help rd /Q /S .\CSScriptLib.Doc\help

set msbuild="C:\Program Files\Microsoft Visual Studio\2022\%vs_edition%\MSBuild\Current\Bin\MSBuild.exe"
%msbuild% ".\CSScriptLib.Doc\CSScriptLib.Doc.shfbproj" -p:Configuration=Release -t:rebuild /p:WarningLevel=0

cd ".\out\Windows"
.\css -code var version = Assembly.LoadFrom(#''cscs.dll#'').GetName().Version.ToString();#nFile.Copy(#''..\\..\\CSScriptLib.Doc\\Help\\Documentation.chm#'', $#''..\\CSScriptLib.v{version}.chm#'', true);

cd ..\..\

explorer .\out

