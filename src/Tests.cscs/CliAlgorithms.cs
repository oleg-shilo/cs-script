using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using csscript;
using CSScripting;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CLI
{
    public class CliSettings
    {
        [Fact]
        public void DefaultSettings()
        {
            // we are not testing JsonSerializer but only the options we configure to support enums
            var settings = new csscript.Settings
            {
                ConcurrencyControl = ConcurrencyControl.HighResolution, // enum
            };

            string json = settings.ToJson();

            var settings2 = json.FromJson<csscript.Settings>();

            Assert.Equal(settings.ConcurrencyControl, settings2.ConcurrencyControl);
        }
    }

    public partial class CliAlgorithms
    {
        string testTempFile(string fileName, [CallerMemberName] string caller = null)
        {
            var rootDir = "TestData".PathJoin(nameof(CliAlgorithms), caller).GetFullPath().EnsureDir();
            return Path.Combine(rootDir, fileName);
        }

        string testTempDir([CallerMemberName] string caller = null)
        {
            var rootDir = "TestData".PathJoin(nameof(CliAlgorithms), caller).GetFullPath().EnsureDir();
            return rootDir;
        }

        string ToCode(string staticMain)
        {
            return @"
using System;
using System.Xml;
using WixSharp;

public class Script
{
    $staticMain$
    {
    }
}
".Replace("$staticMain$", staticMain);
        }

        [Fact]
        public void inject_asm_probing_in_void_main_with_args()
        {
            var processedCode = CSSUtils.InjectModuleInitializer(ToCode(
                "static public void Main(string[] args)"), "HostingRuntime.Init();");

            Assert.Contains("static void Main(string[] args) { HostingRuntime.Init();  impl_Main(args); } static public void impl_Main(string[] args)",
                processedCode);
        }

        [Fact]
        public void inject_asm_probing_in_nonvoid_main_with_args()
        {
            var processedCode = CSSUtils.InjectModuleInitializer(ToCode(
                "static public int Main(string[] args)"), "HostingRuntime.Init();");
            Assert.Contains("static int Main(string[] args) { HostingRuntime.Init(); return  impl_Main(args); } static public int impl_Main(string[] args)",
                processedCode);
        }

        [Fact]
        public void inject_asm_probing_in_void_main_with_no_args()
        {
            var processedCode = CSSUtils.InjectModuleInitializer(ToCode(
                "static public void Main()"), "HostingRuntime.Init();");

            Assert.Contains("static void Main() { HostingRuntime.Init(); impl_Main(); } static public void impl_Main()",
                processedCode);
        }

        [Fact]
        public void FindNetCoreAsmRefs_prefers_ref_pack_from_DOTNET_ROOT()
        {
            var runtime = Environment.Version;
            var root = testTempDir();

            var expected = root.PathJoin("packs",
                                         "Microsoft.NETCore.App.Ref",
                                         $"{runtime.Major}.9999.0",
                                         "ref",
                                         $"net{runtime.Major}.0");

            expected.EnsureDir();
            File.WriteAllText(expected.PathJoin("System.Runtime.dll"), "test");

            var oldDotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");

            try
            {
                Environment.SetEnvironmentVariable("DOTNET_ROOT", root);

                var actual = Globals.FindNetCoreAsmRefs();

                Assert.True(actual.HasText(), "Expected non-empty refs path.");
                Assert.True(actual.SamePathAs(expected),
                    $"Expected '{expected}' but got '{actual}'.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DOTNET_ROOT", oldDotnetRoot);
                root.DeleteDir(handleExceptions: true);
            }
        }
    }
}