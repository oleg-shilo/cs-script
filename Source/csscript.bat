@echo off
rem Pinpoint to location where you keep installation of C# script.
rem set "CS-S_DEV_ROOT=%~dp0"

set "CS-S_DEV_ROOT=C:\Installs\tools\cs-script"

if not exist "%CS-S_DEV_ROOT%" (
    echo Directory '%CS-S_DEV_ROOT%' does not exist
    exit /b 1
)
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\devenv.exe" csscript.sln
