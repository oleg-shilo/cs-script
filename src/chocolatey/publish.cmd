echo off
echo *******************
echo *  must be admin  *
echo *******************
rem choco apikey --key ???????? --source https://push.chocolatey.org/
choco push cs-script.4.6.2.0.nupkg --source https://push.chocolatey.org/
