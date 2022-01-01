echo off

echo Extracting version from release_notes.md 

set css_exe=css
rem if exists .\out\Windows\css.exe (set css_exe=.\out\Windows\css.exe)

%css_exe% -c:0 .\out\ci\set_version 

1.build-binaries.cmd

rem explorer .\out

pause