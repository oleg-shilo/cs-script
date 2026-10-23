cd /home/user/lnx-build
sudo chmod 775 cs-script_4.14-13/DEBIAN/p*
dpkg-deb --build cs-script_4.14-13

sudo dotnet ./cs-script_4.14-13/usr/local/bin/cs-script/cscs.dll -self-test
dotnet ./cs-script_4.14-13/usr/local/bin/cs-script/cscs.dll -server:stop