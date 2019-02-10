@echo off
rem ------------------------------------------------------------------------------------
rem Setting up from Visual Studio: Add post build step:
rem
rem     $(ProjectDir)projexport.bat $(TargetPath)
rem ------------------------------------------------------------------------------------
setlocal enabledelayedexpansion
if not "%~1" == "" goto next1
    echo Usage: %0 ^<binary to copy^>
    exit /b 1

:next1

set ProbeDir.AnyCPU.1="C:\anyfolder"

for /f "tokens=2 delims==" %%a in ('set ProbeDir.AnyCPU. 2^>nul') do (
    if exist "%%~a" (
        echo Copying %~nx1/pdb/xml to %%~a...
        if exist %~dpn1.pdb (
            copy /Y %~dpn1.pdb "%%~a" >NUL
        )
        rem API interface xml description file
        if exist %~dpn1.xml (
            copy /Y %~dpn1.xml "%%~a" >NUL
        )
        copy /Y %1 "%%~a" >NUL
    )
)

endlocal
exit /b 

