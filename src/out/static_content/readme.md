## Manual installations steps

# build OS specific script engine launcher 'css'
# sudo dotnet ./cscs.dll -e ./css.cs

1. Enable execute permissions for the script engine executable
```
sudo chmod +x /usr/local/bin/cs-script/cscs
```
2. Create public symbolic link shim so we do not need to tweek PATH
```
sudo ln -s /usr/local/bin/cs-script/cscs /usr/bin/css
```

3. Optionally you can add the installation dir to envars:
```
export CSSCRIPT_ROOT=$(current_dir)
# updaing envars in bash 
echo 'export CSSCRIPT_ROOT='"$(current_dir)" >> ~/.bashrc
```

