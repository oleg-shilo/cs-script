echo =============
echo Clearing the old build
echo . . .
[ -d /home/user/dev ] || mkdir /home/user/dev
if [ -d /home/user/dev/build/ ]; then rm -r /home/user/dev/build/; fi
mkdir /home/user/dev/build/
echo =============

echo Copying the new binaries
echo . . .
cd /home/user/dev/build/
mkdir cs-script_4.0-0
mkdir cs-script_4.0-0/DEBIAN/
mkdir cs-script_4.0-0/usr
mkdir cs-script_4.0-0/usr/local
mkdir cs-script_4.0-0/usr/local/bin
mkdir cs-script_4.0-0/usr/local/bin/cs-script
mkdir cs-script_4.0-0/usr/local/bin/cs-script/-selftest
cp -r /mnt/d/dev/Galos/cs-script.core/src/out/Linux/* cs-script_4.0-0/usr/local/bin/cs-script
cp -r /mnt/d/dev/Galos/cs-script.core/src/out/Linux/-selftest* cs-script_4.0-0/usr/local/bin/cs-script/-selftest
cp -r /mnt/d/dev/Galos/cs-script.core/src/out/ci/linux/package.build/cs-script_4.0-0/DEBIAN/* cs-script_4.0-0/DEBIAN/
sudo chmod 775 cs-script_4.0-0/DEBIAN/p*
dpkg-deb --build cs-script_4.0-0
if [ -d /home/user/dev/build/ ] ; then cp cs-script_4.0-0.deb /mnt/d/dev/Galos/cs-script.core/src/out/ci/linux/; fi