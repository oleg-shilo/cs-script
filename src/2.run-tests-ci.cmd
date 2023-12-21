echo off

.\out\Windows\css.exe -servers:stop
dotnet test ".\Tests.CSScriptLib\Tests.CSScriptLib.csproj"
rem dotnet test ".\Tests.cscs\Tests.CLI.csproj" -e CI="true"
