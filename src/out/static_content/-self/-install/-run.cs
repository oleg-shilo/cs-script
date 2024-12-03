using static System.Reflection.Assembly;
using static System.IO.Path;
using static System.IO.File;
using static System.Environment;
using static System.Diagnostics.Process;

using System;
using System.Linq;
using System.IO;
using System.Diagnostics;

if (args.Contains("?") || args.Contains("-?") || args.Contains("-help"))
    Console.WriteLine(
        "Integrates CS-Script with the environment by setting the `CSSCRIPT_ROOT` " +
        "environment variable for the script engine CLI executable  parent folder.");

var engine_asm_folder = Path.GetDirectoryName(GetEntryAssembly().Location);

$"CSSCRIPT_ROOT is set to `{engine_asm_folder}`".print();

Environment.SetEnvironmentVariable("CSSCRIPT_ROOT", engine_asm_folder, EnvironmentVariableTarget.User);