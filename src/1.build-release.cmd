echo off

echo Extracting version from release_notes.md 
css .\out\ci\set_version 

1.build-binaries.cmd

cd 
.\out\Windows\cscs.exe -? > ..\help.txt 

rem explorer .\out

pause