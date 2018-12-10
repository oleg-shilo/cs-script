cd cscs.exe.core
md "..\out\.NET Core"
"..\out\.NET Core\css.exe" -server:stop
dotnet publish -c Release -f netcoreapp2.1 -o "..\out\.NET Core"
copy ..\css\bin\Release\css.exe "..\out\.NET Core\css.exe"
cd ..\out\.NET Core
del *.dbg
del *.pdb
rd /S /Q runtimes
pause