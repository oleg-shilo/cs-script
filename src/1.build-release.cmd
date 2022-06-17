echo off

echo Extracting version from release_notes.md 

rem use `dotnet` to ensure the file is not locked after the execution 
.\out\Windows\css.exe -c:0 -ng:dotnet .\out\ci\set_version

1.build-binaries.cmd

rem explorer .\out
