using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using CSScripting;
using CSScripting.CodeDom;
using CSScriptLib;
using Xunit;

namespace Misc
{
    [Collection("Sequential")]
    public class IssuesTests
    {
        public static string root = Assembly.GetExecutingAssembly().Location.GetDirName().PathJoin("test", "TestFolder", "TestData").EnsureDir();

        public string GetTempFileName(string seed)
            => Path.GetFullPath($"{this.GetHashCode()}.{seed}");

        public string GetTempScript(string content, object seed = null, [CallerMemberName] string caller = null)
        {
            var dir = root.PathJoin(nameof(IssuesTests));
            Directory.CreateDirectory(dir);
            var script = Path.Combine(dir, GetTempFileName($"{seed}.{caller}"));
            File.WriteAllText(script, content);
            return script;
        }

        [Fact]
        public void issue_445_compile_OK()
        {
            var thisAsm = this.GetType().Assembly.Location;
            var workingDir = Path.GetDirectoryName(thisAsm);

            CSScript.GlobalSettings.AddSearchDir(@"c:\test");
            CSScript.GlobalSettings.AddSearchDir(workingDir);

            var scriptToImport = GetTempScript(@"//css_dir testDir2
                                                 //css_ref xunit.core.dll",
                                               "imported");

            var script = GetTempScript(@"//css_dir testDir;
                                         //css_ref Newtonsoft.Json.dll
                                         //css_inc " + scriptToImport + @"
                                         public class Calc
                                         {
                                             // comment to avoid caching nased on teh cod ehash
                                             public int Sum(int a, int b) => a+b;
                                         }");
            // ---
            var asm = CSScript.CodeDomEvaluator.CompileFile(script);
            var proj = asm.GetAttached<Project>();
            // ---
            Assert.NotNull(proj);

            Assert.Equal(script, proj.Files.FirstOrDefault());

            Debug.Assert(4 == proj.SearchDirs.Count());

            /*
                D:\dev\cs-script\src\Tests.CSScriptLib\bin\Debug\net10.0
                C
                D
                c:\test
                D:\dev\cs-script\src\Tests.CSScriptLib\bin\Debug\net10.0\testDir
                D:\dev\cs-script\src\Tests.CSScriptLib\bin\Debug\net10.0\testDir2
            */

            Assert.Equal(4, proj.SearchDirs.Count());

            Assert.Contains(@"c:\test", proj.SearchDirs);
            Assert.Contains(workingDir, proj.SearchDirs);
            Assert.Contains(Path.Combine(workingDir, "testDir"), proj.SearchDirs);
            Assert.Contains(Path.Combine(workingDir, "testDir2"), proj.SearchDirs);

            Assert.Contains(Path.Combine(workingDir, "Newtonsoft.Json.dll"), proj.Refs);
            Assert.Contains(Path.Combine(workingDir, "xunit.core.dll"), proj.Refs);
        }

        [Fact]
        public void issue_445_compile_Roslyn()
        {
            var script = GetTempScript(@"
                                        //css_res Resources1.resx;
                                        using System;
                                        public class Calc
                                        {
                                            public int Sum(int a, int b) => a+b;
                                        }");

            var asm = CSScript.RoslynEvaluator.CompileFile(script);

            var proj = asm.GetAttached<Project>();

            // skip next assertions if Globals.DefaultRoslynCompilationToScript as it forces creation of an artificial single script
            // file (even including imported scripts) what makes the concept of Project incompatible.
            if (Globals.DefaultRoslynCompilationToScript)
                Assert.Null(proj);
            else
                Assert.NotNull(proj);
        }

        [Fact]
        public void issue_464()
        {
            var assemblyPath = this.GetType().Assembly.Location;

            IEvaluator evaluator = CSScript.Evaluator
                                           .With(eval => eval.IsAssemblyUnloadingEnabled = true)
                                           .Reset(referenceDomainAssemblies: false);

            var list1 = evaluator.GetReferencedAssemblies();

            evaluator.ReferenceAssembly(assemblyPath);

            var list2 = evaluator.GetReferencedAssemblies();

            evaluator = evaluator.Reset(false);

            var list3 = evaluator.GetReferencedAssemblies();

            Assert.Equal(list1.Count(), list2.Count() - 1);
            Assert.Empty(list3);
        }

        [Fact]
        public void issue_449()
        {
            dynamic script = CSScript.RoslynEvaluator
                                     .LoadMethod(@"void Test()
                                                   {
                                                   }");

            var scriptAssembly = (Assembly)script.GetType().Assembly;
            var runtimeAssembly = "".GetType().Assembly;

            Assert.False(scriptAssembly.IsFrameworkAssembly());
            Assert.True(runtimeAssembly.IsFrameworkAssembly());
        }

        [Fact]
        public void issue_445_compile_error()
        {
            var script = GetTempScript(@"
                                        //css_res Resources1.resx;
                                        using System;
                                        public class Calc
                                        {
                                            error triggering line
                                            public int Sum(int a, int b) => a+b;
                                        }");
            try
            {
                var asm = CSScript.CodeDomEvaluator.CompileFile(script);

                var proj = (Project)asm.GetAttachedValue(nameof(CSScriptLib.Project));
            }
            catch (CompilerException ex)
            {
                Assert.NotNull(ex.CompilerInput);
                Assert.Equal(script, ex.CompilerInput.Files.FirstOrDefault());
            }
            catch (Exception ex)
            {
                Assert.IsType<CompilerException>(ex); // to allow informative test output
            }
        }

        [Fact]
        public void issue_445_compile_error_Roslyn()
        {
            var script = GetTempScript(@"
                                        //css_res Resources1.resx;
                                        using System;
                                        public class Calc
                                        {
                                            error triggering line
                                            public int Sum(int a, int b) => a+b;
                                        }");
            try
            {
                var asm = CSScript.RoslynEvaluator.CompileFile(script);

                var proj = (Project)asm.GetAttachedValue(nameof(CSScriptLib.Project));
            }
            catch (CompilerException ex)
            {
                Assert.Null(ex.CompilerInput);  // Roslyn evaluator does not process CS-Script directives, so no project info is attached
            }
            catch (Exception ex)
            {
                Assert.IsType<CompilerException>(ex); // to allow informative test output
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