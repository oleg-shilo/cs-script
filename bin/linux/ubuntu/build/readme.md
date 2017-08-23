For any new build:

1. Build and pack the release

2. Prepare package sources by executing `prepare_package_sources.cs`. It will in turn:
    - update `install` with the location of the path to the files to be included in the package
    - update `changelog` with the new version and release notes

3. Copy entire `build` folder on Ubuntu. <br>
    IMPORTANT: do not copy files via VMware tools (drag-n-drop) as it screws the files. Instead:

    - zip the build directory (done by `prepare_package_sources.cs`)
    - copy (drag-n-drop) build.zip file to the guest system
    - unzip files on the guest system

4. From the build folder in the terminal execute `debuild -b`

    - Test the package:
        - sudo dpkg --purge cs-script
        - sudo dpkg -i ../cs-script_*.deb  (or specify the version instead of *)

5. Copy cs-script*.deb package back to Win 

6. Commit to the GitHub
