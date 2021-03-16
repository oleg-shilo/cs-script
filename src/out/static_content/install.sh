# eary approach; symbolic link for css does not work very well as cscs.exe is no longer IL based exe in .NET5 but native
# so cannot be taken from the Win distro 
# ln -s /usr/local/bin/cs-script/cscs.exe /usr/local/bin/cs-script/css
# sudo chmod +rwx /usr/local/bin/cs-script/css

# current_dir=/usr/local/bin/cs-script
# current_dir=$PWD

# build OS specific script engine launcher 'css'
# sudo dotnet ./cscs.dll -e ./css.cs


chmod +x /usr/local/bin/cs-script/cscs
sudo ln -s /usr/local/bin/cs-script/cscs /usr/bin/css

export CSSCRIPT_DIR=$(current_dir)
export PATH=$PATH:$(current_dir)

# updaing PATH in bash 
echo 'export CSSCRIPT_DIR='"$(current_dir)" >> ~/.bashrc
echo 'export PATH=$PATH:'"$(current_dir)" >> ~/.bashrc

