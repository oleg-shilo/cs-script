using CSScripting;
using Xunit;

namespace CLI
{
    public class CliAlgorithms
    {
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
    }
}