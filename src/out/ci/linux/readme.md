## Instal the latest version

repo=http://www.cs-script.net/cs-script/linux/ubuntu/; file=$(echo cs-script_)$(curl -L $repo/version.txt --silent)$(echo _all.deb); rm $file; wget $repo$file; sudo dpkg -i $file

## Install Specific version

repo=https://github.com/oleg-shilo/cs-script.core/releases/download/; file=cs-script_1.4-5.deb; rm $file; wget $repo$file; sudo dpkg -i $file

https://github.com/oleg-shilo/cs-script.core/releases/download/v1.4.5.0-NET5-RC5/cs-script_1.4-5.deb

