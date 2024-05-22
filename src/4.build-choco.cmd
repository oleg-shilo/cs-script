echo off

rem .\out\Windows\cscs.exe -version
.\out\Windows\cscs.exe -c:0 -ng:csc .\out\ci\update_choco_scripts
.\out\Windows\cscs.exe -c:0 -ng:csc .\chocolatey\update_package

pushd
cd .\chocolatey
choco pack	
popd
rem cd ..
