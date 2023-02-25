## Manual installations steps

1. Create an alias  so we do not need to tweak PATH and specify dotnet launcher: 
```
alias css='dotnet /usr/local/bin/cs-script/cscs.dll'
```

2. Optionally you can add the installation dir to envars:
```
export CSSCRIPT_ROOT=$(current_dir)
# updaing envars in bash 
echo 'export CSSCRIPT_ROOT='"$(current_dir)" >> ~/.bashrc
```

