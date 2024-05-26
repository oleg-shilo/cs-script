Steps

1. Make GitHub release
2. Create a new version branch in the manifests folder: .\manifests\o\oleg-shilo\cs-script\4.x.x.x\
3. Update individual files in this folder with the new version, the release github url and new SHA-256
4. Validate manifests: winget validate .\manifests\o\oleg-shilo\cs-script\4.x.x.x 
5. Install locally: winget install -m .\manifests\o\oleg-shilo\cs-script\4.x.x.x --ignore-local-archive-malware-scan
5. Optionally uninstall locally: winget uninstall -m .\manifests\o\oleg-shilo\cs-script\4.x.x.x 

Notes:
winget does not create app (installer) aliases for portable packages even though during the installation it reports successful creation.
See https://github.com/microsoft/winget-cli/issues/3345

Thus use hard link instead.
Execute from command prompt:
cmd /K "cd %userprofile%\AppData\Local\Microsoft\WinGet\Packages\oleg-shilo.cs-script__DefaultSource & mklink /H css.exe cscs.exe"

CS-Script now can do this with:
  cscs -self-alias 
