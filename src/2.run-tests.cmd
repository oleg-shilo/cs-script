echo off

rem VS xUnit test runner fails to exit (even though executed all tests) if it 
rem has a child process running (e.g. css build server). So starting the server before the tests
.\out\Windows\css.exe -server:start
dotnet test ".\Tests.CSScriptLib\Tests.CSScriptLib.csproj"
dotnet test ".\Tests.cscs\Tests.CLI.csproj" -e CI="true"
