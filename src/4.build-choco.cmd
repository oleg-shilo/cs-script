echo off

.\out\Windows\cscs.exe -c:0 -ng:csc .\out\ci\update_choco_scripts
.\out\Windows\cscs.exe -c:0 -ng:csc .\chocolatey\update_package

cd .\chocolatey
choco pack	

cd ..
