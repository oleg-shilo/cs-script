using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using CSScripting.CodeDom;
using CSScriptLib;
using Xunit;

namespace Misc
{
    [Collection("Sequential")]
    public class IssuesTests
    {
        public string GetTempFileName(string seed)
            => Path.GetFullPath($"{this.GetHashCode()}.{seed}");

        public string GetTempScript(string seed, string content)
        {
            var script = GetTempFileName(seed);
            File.WriteAllText(script, content);
            return script;
        }

        [Fact]
        public void issue_445()
        {
            var script = GetTempScript(nameof(issue_445),
                                       @"
                                        //css_res Resources1.resx;
                                        using System;
                                        public class Calc
                                        {
                                            error triggering line
                                            public int Sum(int a, int b) => a+b;
                                        }");
            try
            {
                string asmFile = CSScript.CodeDomEvaluator.CompileAssemblyFromFile(script, "MyScript.asm");
            }
            catch (CompilerException ex)
            {
                Assert.NotEmpty(ex.CompileCommand);
            }
            catch (Exception ex)
            {
                Assert.IsType<CompilerException>(ex);
            }
        }

        [Fact]
        public void issue_279()
        {
            // more like a playground than u-test

            // CSScript.EvaluatorConfig.Access = EvaluatorAccess.AlwaysCreate;
            // CSScript.EvaluatorConfig.DebugBuild = true;
            // CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;
            // CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Pdb;
            // CSScript.EvaluatorConfig.ReferenceDomainAssemblies = true;
            // CSScript.EvaluatorConfig.CompilerOptions = " /preferreduilang:en-US";

            // var script = @"..\support\cs-script_#297\cs-script_.297.1\ScriptTest\Scripts\TheFirst\TheFirstScript.cs";
            // CSScript.Evaluator
            //     .ReferenceAssembly(@"..\support\cs-script_#297\cs-script_.297.1\ScriptTest\ScriptContracts\bin\Debug\ScriptContracts.dll")
            //     .LoadFile(script);
        }

        [Fact]
        public void issue_436()
        {
            // This test shows how to use CSScripting.CodeDom.CompilerError
            // Note in .NET Core the AppDomain isolation/unloading is not available as in .NET Framework.
            // The modern, supported replacement is: AssemblyLoadContext(ALC)
            // It provides:
            // - Isolated assembly loading
            // - Deterministic unloading
            // - A clean mental model for “temporary execution domains”

            // Create a simple script with a compilation error
            string scriptWithError = @"
                using System;

                public class TestClass
                {
                    public void TestMethod()
                    {
                        UndeclaredVariable = 5; // This will cause a compiler error
                    }
                }";

            try
            {
                // Attempt to compile the script using the evaluator
                // This should trigger compilation in an external domain context
                var result = CSScript.Evaluator.CompileCode(scriptWithError);

                // If we get here, compilation succeeded when it should have failed
                Assert.Fail("Expected compilation to fail but it succeeded");
            }
            catch (CompilerException e)
            {
                // Verify that we got a compiler error
                Assert.Contains("CS0103", e.Message); // CS0103: The name 'UndeclaredVariable' does not exist in the current context

                // Additional verification that the error contains expected information
                Assert.Contains("UndeclaredVariable", e.Message);
            }

            // Test CompilerError.Parse functionality to ensure it can handle cross-domain scenarios
            string compilerOutput = "TestScript.cs(7,25): error CS0103: The name 'UndeclaredVariable' does not exist in the current context";
            var parsedError = CompilerError.Parse(compilerOutput);

            Assert.NotNull(parsedError);
            Assert.Equal("CS0103", parsedError.ErrorNumber);
            Assert.Equal("TestScript.cs", parsedError.FileName);
            Assert.Equal(7, parsedError.Line);
            Assert.Equal(25, parsedError.Column);
            Assert.False(parsedError.IsWarning);
            Assert.Contains("UndeclaredVariable", parsedError.ErrorText);
        }
    }
}