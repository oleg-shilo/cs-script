cd /home/user/lnx-build
sudo chmod 775 cs-script_4.14-9/DEBIAN/p*
dpkg-deb --build cs-script_4.14-9

sudo dotnet ./cs-script_4.14-9/usr/local/bin/cs-script/cscs.dll -self-test
dotnet ./cs-script_4.14-9/usr/local/bin/cs-script/cscs.dll -server:stop