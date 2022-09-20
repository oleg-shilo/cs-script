echo off
echo *******************
echo *  must be admin  *
echo *******************
rem choco apikey --key ???????? --source https://push.chocolatey.org/
choco push cs-script.4.4.8.0.nupkg --source https://push.chocolatey.org/
