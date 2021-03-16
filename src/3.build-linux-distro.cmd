echo off

cd out\ci
..\Windows\cscs.exe build-deb.cs

explorer ..\

pause