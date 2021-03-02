//css_include cmd
using System.IO;
using static System.Environment;
using static System.IO.Directory;
using static System.IO.Path;
using static System.IO.File;
using static cmd;
using static System.Diagnostics.Process;
using System;
using System.Linq;

(string full_version,
 string version,
 string changes) = vs.parse_release_notes(args.FirstOrDefault() ?? Path.GetFullPath(@"..\..\release_notes.md"));

var lnx_root = @"\\wsl$\Ubuntu\home\user";
																															
var win_root = @".\";
var bld_root = lnx_root.join($@"lnx-build\cs-script_{version}");

var build_sh_script = win_root.join("build.sh");
var build_cmd_script = win_root.join("build.cmd").getFullPath();

xcopy(
    win_root.join(@"..\Linux\*"),
    bld_root.join(@"usr\local\bin\cs-script"));

xcopy(
    win_root.join(@"linux\package.build\DEBIAN_template\*"),
    bld_root.join("DEBIAN"));

copy(
    win_root.join(@"linux\package.build\build.sh.template"),
    build_sh_script);

replaceInFile(bld_root.join(@"DEBIAN\control"), "{$version}", version);
replaceInFile(build_sh_script, "{$version}", version);
replaceInFile(build_sh_script, "{$local_root}", GetFullPath(bld_root.join("..")).toLnx());

WriteAllText(build_cmd_script, "bash build.sh");
Start(build_cmd_script).WaitForExit();

var package = win_root.join($@"..\cs-script_{version}.deb").getFullPath();
Copy(bld_root.join($@"..\cs-script_{version}.deb"), package, true);

Console.WriteLine("...............");
Console.WriteLine("Package: " + package);

static class extensions
{
    public static string toLnx(this string path) => path.Replace(@"\\wsl$\Ubuntu", "").Replace(@"\", "/");
}