echo off

echo Extracting version from release_notes.md 
css .\out\ci\set_version 

1.build-binaries.cmd

explorer .\out

pause