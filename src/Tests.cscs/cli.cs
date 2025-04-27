using System;
using System.Collections.Generic;
using System.Diagnostics;
using static System.Environment;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using csscript;
using CSScripting;
using CSScriptLib;
using Xunit;
using Xunit.Abstractions;

// disable warning CA1416
#pragma warning disable CA1416

namespace CLI
{
    public class CliTestFolder
    {
        public static string root = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("test.cli").EnsureDir();

        public CliTestFolder()
        {
            Directory.GetFiles(root, "*", SearchOption.AllDirectories)
                     .ToList()
                     .ForEach(File.Delete);

            Environment.CurrentDirectory = root;
        }

        public static void Set(string path)
            => Environment.CurrentDirectory = root = path.EnsureDir();
    }

    public class cscs_cli : IClassFixture<CliTestFolder>
    {
        static string preferredCompiler => OperatingSystem.IsWindows() ? "-ng:dotnet" : "-ng:csc";

        // the test in VS_xUnit test runner integration works just fine. But assembly loading fails under "dotnet test ..."
        // so will need to exclude impacted test
        static bool SkipIfIncompatibile => ("CI".GetEnvar() != null);

        ITestOutputHelper output;

        public cscs_cli(ITestOutputHelper output)
        {
            this.output = output;
#if DEBUG
            var config = "Debug";
#else
            var config = "Release";
#endif
            cscs_exe = Environment.GetEnvironmentVariable("css_test_asm") ?? $@"..\..\..\..\..\cscs\bin\{config}\net{Environment.Version.Major}.0\cscs.dll".GetFullPath();
            var cmd_dir = cscs_exe.ChangeFileName("-self");

            if (cmd_dir.DirExists())
                static_content = cmd_dir.GetDirName();
            else
                static_content = $@"..\..\..\..\..\out\static_content".GetFullPath();

            ".".PathJoin("temp").EnsureDir();

            output.WriteLine($"Running under {(SkipIfIncompatibile ? "DOTNET" : "VS")}");
        }

        public string cscs_exe;
        public string static_content;

        string cscs_run(string args, string dir = null)
        {
            // print method input
            // Console.WriteLine($"{cscs_exe} {args}");

            var output = "dotnet".Run($"\"{cscs_exe}\" {args}", dir).output.Trim();

            return output;
        }

        (string output, int exitCode) cscs_runx(string args, string dir = null) => "dotnet".Run($"\"{cscs_exe}\" {args}", dir);

        [Fact]
        public void create_and_execute_default_script()
        {
            if (SkipIfIncompatibile) return;

            var script_file = nameof(create_and_execute_default_script) + ".cs";

            var output = cscs_run($"-new {script_file}");

            Assert.True(File.Exists(script_file));

            Assert.True(File.Exists(script_file));
            Assert.StartsWith("Created:", output);

            output = cscs_run(script_file);
            Assert.Contains("Hello from C#", output);
        }

        [Fact]
        public void switch_engine_from_CLI()
        {
            if (SkipIfIncompatibile) return;

            var script_file = nameof(switch_engine_from_CLI);

            var output = cscs_run($"-new {script_file}");

            output = cscs_run($"-check -verbose -ng:dotnet {script_file}");
            Assert.Contains("Compiler engine: dotnet", output);

            output = cscs_run($"-check -verbose -ng:csc {script_file}");
            Assert.Contains("Compiler engine: csc", output);
        }

        [Fact(Skip = "just a testbed")]
        public void should_discover_nuget_package_native_dlls()
        {
        }

        [Fact]
        public void switch_engine_from_code_with_full_directive()
        {
            if (SkipIfIncompatibile) return;

            var script_file = nameof(switch_engine_from_code_with_full_directive) + ".cs";

            var output = cscs_run($"-new {script_file}");

            var script_code = File.ReadAllText(script_file);

            // full directive //css_engine
            File.WriteAllText(script_file, "//css_engine dotnet" + NewLine + script_code);
            output = cscs_run($"-check -verbose {script_file}");
            Assert.Contains("Compiler engine: dotnet", output);

            File.WriteAllText(script_file, "//css_engine csc" + NewLine + script_code);
            output = cscs_run($"-check -verbose {script_file}");
            Assert.Contains("Compiler engine: csc", output);
        }

        [Fact]
        public void switch_engine_from_code_with_short_directive()
        {
            if (SkipIfIncompatibile) return;

            var script_file = nameof(switch_engine_from_code_with_short_directive) + ".cs";

            var output = cscs_run($"-new {script_file}");
            var script_code = File.ReadAllText(script_file);

            // short directive //css_ng
            File.WriteAllText(script_file, "//css_ng dotnet" + NewLine + script_code);
            output = cscs_run($"-check -verbose {script_file}");
            Assert.Contains("Compiler engine: dotnet", output);

            File.WriteAllText(script_file, "//css_ng csc" + NewLine + script_code);
            output = cscs_run($"-check -verbose {script_file}");
            Assert.Contains("Compiler engine: csc", output);
        }

        [Fact]
        public void switch_engine_from_settings()
        {
            if (SkipIfIncompatibile) return;

            var script_file = nameof(switch_engine_from_settings);
            var settings_file = nameof(switch_engine_from_settings).GetFullPath() + ".config";
            var settings = new Settings();

            var output = cscs_run($"-new {script_file}");

            // --------------

            settings.DefaultCompilerEngine = "dotnet";
            settings.Save(settings_file);

            output = cscs_run($"-check -verbose -config:{settings_file} {script_file}");
            Assert.Contains("Compiler engine: dotnet", output);

            // --------------

            settings.DefaultCompilerEngine = "csc";
            settings.Save(settings_file);

            output = cscs_run($"-check -verbose -config:\"{settings_file}\" {script_file}");
            Assert.Contains("Compiler engine: csc", output);
        }

        [Fact]
        public void new_toplevel()
        {
            if (SkipIfIncompatibile) return;

            var script_file = nameof(new_toplevel);

            var output = cscs_run($"-new:toplevel {script_file}");

            output = cscs_run($"-check -ng:dotnet {script_file}");
            Assert.Equal("Compile: OK", output);

            output = cscs_run($"-check -ng:csc {script_file}");
            Assert.Equal("Compile: OK", output);
        }

        [Fact]
        public void compile_dll()
        {
            if (SkipIfIncompatibile) return;

            // Issue #255: Relative path for cscs.exe -out option results in wrong output folder

            var script_file = ".".PathJoin("temp", $"{nameof(compile_dll)}");
            var dll_file = ".".PathJoin("temp", $"{nameof(compile_dll)}.dll");
            var output = cscs_run($"-new:console {script_file}");

            output = cscs_run($"-ng:dotnet -cd -out:{dll_file} {script_file}");
            Assert.Contains("Created: " + dll_file.GetFullPath(), output);

            output = cscs_run($"-ng:csc -cd -out:{dll_file} {script_file}");
            Assert.Contains("Created: " + dll_file.GetFullPath(), output);
        }

        [Fact]
        public void new_console()
        {
            if (SkipIfIncompatibile) return;

            var script_file = nameof(new_console);
            var output = cscs_run($"-new:console {script_file}");

            output = cscs_run($"-check {script_file}");
            Assert.Equal("Compile: OK", output);
        }

        [Fact]
        public void new_cmd()
        {
            if (SkipIfIncompatibile) return;

            var output = cscs_run($"-new:cmd ttt");
            var script_file = output.Replace("Created:", "").Trim();

            if (!IsProcessRunningAsRoot())
            {
                if (!output.HasText())
                    Assert.Fail("Error: Cannot create new custom command. Ensure you run as a root user.");
            }

            try
            {
                Assert.Equal("-run.cs", script_file.GetFileName());
                Assert.Equal("-ttt", script_file.GetDirName().GetFileName());
            }
            finally
            {
                script_file.GetDirName().DeleteDir(true);
            }
        }

        [Fact]
        [FactWinOnly]
        public void syntax_version_10()
        {
            if (SkipIfIncompatibile) return;

            var script = (nameof(syntax_version_10) + ".cs").GetFullPath();
            var script_g = script.ChangeExtension(".g.cs");

            File.WriteAllText(script, @$"
//css_inc {script_g}
Console.WriteLine(""Hello, World!"");");

            File.WriteAllText(script_g, @"
global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Net.Http;
global using global::System.Threading;
global using global::System.Threading.Tasks;");

            var output = cscs_run($"-ng:dotnet \"{script}\"");
            Assert.Equal("Hello, World!", output);

            output = cscs_run($"-ng:csc \"{script}\"");
            Assert.Equal("Hello, World!", output);

            output = cscs_run($"-ng:roslyn \"{script}\"");
            Assert.Equal("Hello, World!", output);
        }

        [Fact]
        public void distro_commands()
        {
            if (SkipIfIncompatibile) return;

            var is_win = (Environment.OSVersion.Platform == PlatformID.Win32NT);

            var commands = Directory.GetFiles(static_content, "-run.cs", SearchOption.AllDirectories);
            foreach (var file in commands)
            {
                var cmd = file.GetDirName().GetFileName();

                if (cmd == "-exe") continue;

                var probing_dir = $"-dir:{static_content}";
                var extra_arg = cmd.IsOneOf("-web", "-test", "-exe", "-update", "-alias") ? "-?" : "";

                (var output, var exitCode) = cscs_runx($"{file} {extra_arg}");

                var context = $"{file}\n{output.TakeMax(100)}";

                Assert.True(0 == exitCode, $"Failed to build/execute: \n{context}");
                Assert.False(output.Contains("Index was"), $"Failed to start: \n{context}");
            }
        }

        [Fact]
        public void complex_commands()
        {
            if (SkipIfIncompatibile) return;

            var is_win = (Environment.OSVersion.Platform == PlatformID.Win32NT);
            var probing_dir = $"-dir:{static_content}";

            var output = cscs_run($"{probing_dir} -self");
            Assert.Equal(cscs_exe, output);

            output = cscs_run($"{probing_dir} -self-run");
            Assert.Equal(cscs_exe, output);

            // -------------- it will fail on Linux as elevation will be required. so building exe
            // needs to be tested manually.
            if (is_win && !Assembly.GetEntryAssembly().Location.EndsWith("css.dll"))
            {
                // css may be locked (e.g.it is running this test)
                output = cscs_run($"{probing_dir} -self");
                Assert.Equal(cscs_exe, output);

                return; // skip the rest as it will fail to create css.exe that is already running.

                // will need to use a different sample script to test the rest of the functionality

                output = cscs_run($"{probing_dir} -self-exe");
                Assert.Equal($"Created: {cscs_exe.ChangeFileName("css.exe")}", output);

                output = cscs_run($"{probing_dir} -self-exe-run");
                Assert.Equal($"Created: {cscs_exe.ChangeFileName("css.exe")}", output);

                output = cscs_run($"{probing_dir} -self-exe-build");
                Assert.Equal($"Created: {cscs_exe.ChangeFileName("css.exe")}", output);
            }
        }

        [Fact]
        [FactWinOnly]
        public void new_winform()
        {
            if (SkipIfIncompatibile) return;

            var script_file = nameof(new_winform);
            var output = cscs_run($"-new:winform {script_file}");

            output = cscs_run($"-check -ng:dotnet {script_file}");
            Assert.Equal("Compile: OK", output);

            output = cscs_run($"-check -ng:csc {script_file}");
            Assert.Equal("Compile: OK", output);
        }

        [Fact]
        [FactWinOnly]
        public void compile_netfx_script_dotnet()
        {
            if (SkipIfIncompatibile) return;

            // SkipException just does not work
            // throw SkipException.ForSkip("DOTNET TEST does not play nice with nested child processes");

            var script_file = ".".PathJoin("temp", $"{nameof(compile_netfx_script_dotnet)}");
            File.WriteAllText(script_file,
                @"using System;
                  class Program
                  {
                      static void Main()
                      {
                          Console.WriteLine(Environment.Version);
                      }
                  }");

            var output = cscs_run($"-ng:dotnet -netfx {script_file}");
            Assert.Equal("4.0.30319.42000", output);

            // enable caching
            output = cscs_run($"-c:1 -ng:dotnet -netfx {script_file}");
            Assert.Equal("4.0.30319.42000", output);
        }

        [Fact]
        [FactWinOnly]
        public void compile_netfx_script_csc()
        {
            // SkipException just does not work
            // throw SkipException.ForSkip("DOTNET TEST does not play nice with nested child processes");
            if (SkipIfIncompatibile) return;

            var script_file = ".".PathJoin("temp", $"{nameof(compile_netfx_script_csc)}");
            File.WriteAllText(script_file,
                @"using System;
                  class Program
                  {
                      static void Main()
                      {
                          Console.WriteLine(Environment.Version);
                      }
                  }");

            var output = cscs_run($"-ng:csc -netfx {script_file}");
            Assert.Equal("4.0.30319.42000", output);

            // enable caching
            output = cscs_run($"-c:1 -ng:csc -netfx {script_file}");
            Assert.Equal("4.0.30319.42000", output);
        }

        [Fact]
        [FactWinOnly]
        public void compile_x86_script_dotnet()
        {
            // SkipException just does not work
            // throw SkipException.ForSkip("DOTNET TEST does not play nice with nested child processes");
            if (SkipIfIncompatibile) return;

            var script_file = ".".PathJoin("temp", $"{nameof(compile_x86_script_dotnet)}");
            File.WriteAllText(script_file,
                @"using System;
                  class Program
                  {
                      static void Main()
                      {
                          Console.WriteLine(Environment.Is64BitProcess.ToString());
                      }
                  }");

            var output = cscs_run($"-ng:dotnet -c:0 -co:/platform:x86 {script_file}");
            // this.output.WriteLine(output);
            Assert.Equal("False", output);
        }

        [Fact]
        [FactWinOnly]
        public void compile_x86_script_csc()
        {
            // SkipException just does not work
            // throw SkipException.ForSkip("DOTNET TEST does not play nice with nested child processes");
            if (SkipIfIncompatibile) return;

            var script_file = ".".PathJoin("temp", $"{nameof(compile_x86_script_csc)}");
            File.WriteAllText(script_file,
                 @"using System;
                   class Program
                   {
                       static void Main()
                       {
                           Console.WriteLine(Environment.Is64BitProcess.ToString());
                       }
                   }");

            // When dotnet.exe generates exe for x86 it will produce
            // app.exe                 - assembly host file (native x86 PE)
            // app.dll                 - assembly file (.NET assembly x86)
            // app.runtimeconfig.json  - assembly config

            // When csc.exe generates exe for x86 it will produce
            // app.exe - assembly file (.NET assembly x86) that is also a self executable host (similar to .NET Framework compilation)
            //
            // IE
            //  set REF="C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\9.0.0\ref\net9.0"
            //  dotnet exec "C:\Program Files\dotnet\sdk\9.0.203\Roslyn\bincore\csc.dll" / platform:x86 / t:exe /out:test.exe test.cs ^
            //     /reference:% REF %\System.Runtime.dll ^
            //     /reference:% REF %\System.Console.dll
            //
            // Though when the exe is executed it produces:
            // System.IO.FileNotFoundException: Could not load file or assembly 'System.Runtime, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
            // or one of its dependencies. The system cannot find the file specified.
            // This is because it cannot produce the exe that can discover sys assemblies (like dotnet.exe compiled exe does) but requires all these assemblies to be
            // present in the local dir.
            // Using donet.exe launched for running app.dll compiled as x86 and /target:winexe) produces the invalid formal exception. This is because dotnet.exe starts .64 process and
            // the script assembly is x86. Unfortunately SDK does not have dotnet.exe 32 bit version.
            // That's why dotnet engine is a preferred compiler for x86 scripts

            // Debugger.Launch();
            var output = cscs_runx($"-ng:csc -co:/platform:x86 {script_file.GetFullPath()}");

            // Console.WriteLine(output.output);
            // Assert.Contains("Executing scripts targeting x86 platform with `csc` compiling engine is not supported", output);
        }

        [Fact]
        public void compiler_output()
        {
            if (SkipIfIncompatibile) return;

            // Debugger.Launch();

            var script_file = nameof(compiler_output);
            var output = cscs_run($"-new {script_file}");

            var output1 = cscs_run($"-check {script_file}");

            Assert.Equal("Compile: OK", output1);
        }

        [FactWinOnly]
        [Fact]
        public void new_wpf_with_cscs()
        {
            if (SkipIfIncompatibile) return;

            /*
            // WPF is a special case. If WPF script uses compiled (not dynamically loaded) XAML then
            // it needs to be compiled to BAML. The same way as MSBuild does for a WPF vs project.
            // Thus csc.exe simply not capable of doing this so you need to use dotnet engine.

            script_type   |  host_app   |  compiler_engine   |  compilation  |  hosting  |  overall execution  |  special_steps
            -------------------------------------------------|---------------|-----------|--------------------------------------
            console       |  cscs       |  csc               |  success      |  success  |  success            |
            console       |  csws       |  csc               |  success      |  success  |  success            |
            console       |  cscs       |  dotnet            |  success      |  success  |  success            |
            console       |  csws       |  dotnet            |  success      |  success  |  success            |

            winform       |  cscs       |  csc               |  success      |  success  |  success            |  //css_winapp
            winform       |  csws       |  csc               |  success      |  success  |  success            |  //css_winapp
            winform       |  cscs       |  dotnet            |  success      |  success  |  success            |  //css_winapp
            winform       |  csws       |  dotnet            |  success      |  success  |  success            |  //css_winapp

            WPF           |  cscs       |  csc               |  failure      |  failure  |  failure            |  //css_winapp
            WPF           |  csws       |  csc               |  failure      |  failure  |  failure            |  //css_winapp
            WPF           |  cscs       |  dotnet            |  success      |  failure  |  failure            |  //css_winapp
            WPF           |  cscs       |  dotnet            |  success      |  success  |  success            |  //css_winapp (execute `cscs -wpf:enable` once)
            WPF           |  csws       |  dotnet            |  success      |  success  |  success            |  //css_winapp
             */

            if (!Runtime.IsLinux)
            {
                var script_file = nameof(new_wpf_with_cscs) + ".cs";

                var output = cscs_run($"-new:wpf {script_file}"); Thread.Sleep(500); //Task.Delay(500).Wait();

                // --------------

                output = cscs_run($"-check {script_file}");
                Assert.Equal("Compile: OK", output);

                // --------------

                // removing in-code engine directive so the engine from CLI param will be used
                var content = File.ReadAllLines(script_file).Where(x => !x.Contains("//css_ng")).ToArray();
                File.WriteAllLines(script_file, content);

                output = cscs_run($"-check -ng:dotnet {script_file}");
                Assert.Equal("Compile: OK", output);

                // csc engine supposed to fail to compile WPF
                output = cscs_run($"-check -ng:csc {script_file}");
                Assert.Contains("BUILD: error : In order to compile XAML you need to use 'dotnet' compiler", output);
            }
        }

        bool IsProcessRunningAsRoot()
        {
            var is_win = (Environment.OSVersion.Platform == PlatformID.Win32NT);

            if (is_win)
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

            try
            {
                foreach (var line in File.ReadLines("/proc/self/status"))
                {
                    if (line.StartsWith("Uid:"))
                    {
                        string[] parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                        return parts.Length > 1 && parts[1] == "0"; // UID 0 means root
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking UID: {ex.Message}");
            }
            return false;
        }
    }
}