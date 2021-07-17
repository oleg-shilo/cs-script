echo off

.\src\out\Windows\css.exe -server_r:stop
.\src\out\Windows\css.exe -server:stop
dotnet test ".\Tests.CSScriptLib\Tests.CSScriptLib.csproj"
dotnet test ".\Tests.cscs\Tests.CLI.csproj"

pause