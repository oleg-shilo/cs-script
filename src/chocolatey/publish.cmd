echo off
echo *******************
echo *  must be admin  *
echo *******************
rem choco apikey --key ???????? --source https://push.chocolatey.org/
choco push cs-script.core.2.0.0.0 --source https://push.chocolatey.org/
