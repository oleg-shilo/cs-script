winget does not create app (installer) aliases for portable packages even though during the installation it reports successful creation.
See https://github.com/microsoft/winget-cli/issues/3345

Thus use hard link instead.
Execute from command prompt:
cmd /K "cd %userprofile%\AppData\Local\Microsoft\WinGet\Packages\oleg-shilo.cs-script__DefaultSource & mklink /H css.exe cscs.exe"
