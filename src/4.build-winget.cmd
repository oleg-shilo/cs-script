echo off


cd .\..\..\winget-pkgs


Sync the https://github.com/oleg-shilo/winget-pkgs fork with the MS upstream repo...

pause

git checkout master
git pull

rem rem # Add the remote, call it "upstream":
rem git remote add upstream https://github.com/microsoft/winget-pkgs.git

rem rem # Fetch all the branches of that remote into remote-tracking branches
rem git fetch upstream


rem rem # Make sure that you're on your master branch:
rem git checkout master

rem rem rem # then: (like "git pull" which is fetch + merge)
rem rem git merge upstream/master master

rem rem # Rewrite your master branch so that any commits of yours that
rem rem # aren't already in upstream/master are replayed on top of that
rem rem # other branch:
rem git rebase upstream/master


cd ..\cs-script\src
.\out\Windows\cscs.exe -c:0 -ng:csc .\out\ci\update_winget_scripts

