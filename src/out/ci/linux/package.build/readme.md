# Build Debian package

ref: https://ubuntuforums.org/showthread.php?t=910717
     https://www.debian.org/doc/debian-policy/ch-controlfields.html#s-f-package-type

Note: chmod 775 cs-script_4.0-0/DEBIAN/p* will cover postinst,prerm and postrm
Packaging must be done on native Linux FS folders. If it is done on mounted drives dpkg-deb will fail with file permissions error
mkdir cs-script_4.0-0
mkdir cs-script_4.0-0/usr
mkdir cs-script_4.0-0/usr/local
mkdir cs-script_4.0-0/usr/local/bin
mkdir cs-script_4.0-0/usr/local/bin/cs-script
mkdir cs-script_4.0-0/usr/local/bin/cs-script/-selftest
cp -r /mnt/d/dev/Galos/cs-script.core/src/out/Linux/cs-script_4.0-0/* /usr/local/bin/cs-script
cp -r /mnt/d/dev/Galos/cs-script.core/src/out/Linux/cs-script_4.0-0/-selftest* /usr/local/bin/cs-script/-selftest
mkdir cs-script_4.0-0v/DEBIAN

sudo chmod 775 cs-script_4.0-0/DEBIAN/p*

dpkg-deb --build cs-script_4.0-0


    - Test the package:
        - sudo dpkg --purge cs-script
        - sudo dpkg -i ../cs-script_*.deb  (or specify the version instead of *)