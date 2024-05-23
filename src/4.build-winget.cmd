echo off


cd .\..\..\winget-pkgs

rem # Add the remote, call it "upstream":
git remote add upstream https://github.com/microsoft/winget-pkgs.git

rem # Fetch all the branches of that remote into remote-tracking branches
git fetch upstream

rem # Make sure that you're on your master branch:
git checkout master

rem git branch -D oleg-shilo_cs-script_4.8.15.0
rem git branch 

rem # Rewrite your master branch so that any commits of yours that
rem # aren't already in upstream/master are replayed on top of that
rem # other branch:
git rebase upstream/master

cd ..\cs-script\src
.\out\Windows\cscs.exe -c:0 -ng:csc .\out\ci\update_winget_scripts

