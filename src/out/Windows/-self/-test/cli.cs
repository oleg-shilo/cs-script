using csscript;
using CSScripting;
using CSScriptLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using static System.Environment;

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
        public cscs_cli()
        {
#if DEBUG
            var config = "Debug";
#else
            var config = "Release";
#endif
            cscs_exe = Environment.GetEnvironmentVariable("css_test_asm") ?? $@"..\..\..\..\..\cscs\bin\{config}\net{Environment.Version.Major}.0\cscs.dll".GetFullPath();
            var cmd_dir = cscs_exe.ChangeFileName("-self");

            if (cmd_dir.DirExists())
                static_content = cmd_dir;
            else
                static_content = $@"..\..\..\..\..\out\static_content".GetFullPath();
        }

        public string cscs_exe;
        public string static_content;

        string cscs_run(string args, string dir = null) => "dotnet".Run($"\"{cscs_exe}\" {args}", dir).Trim();

        [Fact]
        public void create_and_execute_default_script()
        {
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
            var script_file = nameof(switch_engine_from_CLI);

            var output = cscs_run($"-new {script_file}");

            output = cscs_run($"-check -verbose -ng:dotnet {script_file}");
            Assert.Contains("Compiler engine: dotnet", output);

            output = cscs_run($"-check -verbose -ng:csc {script_file}");
            Assert.Contains("Compiler engine: csc", output);
        }

        [Fact]
        public void switch_engine_from_code_with_full_directive()
        {
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
            // Issue #255: Relative path for cscs.exe -out option results in wrong output folder

            var script_file = ".".PathJoin("temp", $"{nameof(compile_dll)}");
            var dll_file = ".".PathJoin("temp", $"{nameof(compile_dll)}.dll");
            var output = cscs_run($"-new:console {script_file}");

            output = cscs_run($"-cd -out:{dll_file} {script_file}");
            Assert.Contains("Created: " + dll_file.GetFullPath(), output);

            output = cscs_run($"-ng:csc -cd -out:{dll_file} {script_file}");
            Assert.Contains("Created: " + dll_file.GetFullPath(), output);
        }

        [Fact]
        public void new_console()
        {
            var script_file = nameof(new_console);
            var output = cscs_run($"-new:console {script_file}");

            output = cscs_run($"-check -ng:dotnet {script_file}");
            Assert.Equal("Compile: OK", output);
        }

        [Fact]
        public void syntax_version_10()
        {
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
        public void complex_commands()
        {
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
            var script_file = nameof(new_winform);
            var output = cscs_run($"-new:winform {script_file}");

            output = cscs_run($"-check -ng:dotnet {script_file}");
            Assert.Equal("Compile: OK", output);

            output = cscs_run($"-check -ng:csc {script_file}");
            Assert.Equal("Compile: OK", output);
        }

        [Fact]
        public void compiler_output()
        {
            var script_file = nameof(compiler_output);
            var output = cscs_run($"-new {script_file}");

            output = cscs_run($"-check -ng:csc {script_file}");
            Assert.Equal("Compile: OK", output);
        }

        [Fact]
        [FactWinOnly]
        public void new_wpf_with_cscs()
        {
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

                var output = cscs_run($"-new:wpf {script_file}");

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
    }
}