using csscript;
using CSScripting;
using CSScriptLib;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Testing;
using Xunit;
using static System.Environment;

namespace EvaluatorTests
{
    // Precompiler related tests; will be executed as part of both API_Roslyn and API_CodeDom (inherits from API_Roslyn) test classes
    public partial class API_Roslyn
    {
        public class Sample_Precompiler
        {
            public static bool Compile(ref string scriptCode, string scriptFile, bool isPrimaryScript, Hashtable context)
            {
                scriptCode = "#define MY_DEFINE" + System.Environment.NewLine + scriptCode;
                return true;
            }
        }

        [Fact]
        public void Precompiler_LoadMethod_LambdaPrecompiler()
        {
            var precompiler = Precompiler.FromLambda(
                context =>
                {
                    context.Content = "#define MY_DEFINE" + System.Environment.NewLine + context.Content;
                });

            var code =
                $"{precompiler.ToCssDirective()}" + Environment.NewLine +
                """
                string foo()
                {
                    #if MY_DEFINE
                    return "MY_DEFINE is defined";
                    #else
                    return "MY_DEFINE is not defined";
                    #endif
                }
                """;

            dynamic script = new_evaluator.LoadMethod(code);
            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadMethod_LambdaPrecompiler2()
        {
            var precompiler = Precompiler.FromLambda(
                context =>
                {
                    context.NewCompilerOptions = "-define:MY_DEFINE";
                });

            CSScript.EvaluatorConfig.CompilerOptions += " -define:MY_DEFINE2";

            var code =
                $"{precompiler.ToCssDirective()}" + Environment.NewLine +
                """
                string foo()
                {
                    #if MY_DEFINE
                        #if MY_DEFINE2
                    return "MY_DEFINE is defined";
                        #endif
                    #else
                    return "MY_DEFINE is not defined";
                    #endif
                }
                """;

            dynamic script = new_evaluator.LoadMethod(code);
            //dynamic script = CSScript.CodeDomEvaluator.LoadMethod(code);
            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadMethod_LambdaPrecompiler_EvaluatorDirective()
        {
            var precompiler = Precompiler.FromLambda(
                context =>
                {
                    context.Content = "#define MY_DEFINE" + System.Environment.NewLine + context.Content;
                });

            dynamic script = new_evaluator
                .With(x => x.Directives.Add(precompiler.ToCssDirective()))
                .LoadMethod("""
                    string foo()
                    {
                        #if MY_DEFINE
                        return "MY_DEFINE is defined";
                        #else
                        return "MY_DEFINE is not defined";
                        #endif
                    }
                    """);

            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadMethod_LambdaPrecompiler_Extension()
        {
            dynamic script = new_evaluator
                .WithPrecompiler(script =>
                {
                    script.Content = "#define MY_DEFINE" + NewLine + script.Content;
                })
                .LoadMethod("""
                    string foo()
                    {
                        #if MY_DEFINE
                        return "MY_DEFINE is defined";
                        #else
                        return "MY_DEFINE is not defined";
                        #endif
                    }
                    """);

            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadFile_LambdaPrecompiler()
        {
            var precompiler = Precompiler.FromLambda(
                context =>
                {
                    context.Content = "#define MY_DEFINE" + System.Environment.NewLine + context.Content;
                });

            var code = GetTempScript(@" using System;
                                        public class test
                                        {
                                            public string foo()
                                            {
                                                #if MY_DEFINE
                                                return ""MY_DEFINE is defined"";
                                                #else
                                                return ""MY_DEFINE is not defined"";
                                                #endif
                                            }
                                        }");

            dynamic script = new_evaluator
                .With(x => x.Directives.Add(precompiler.ToCssDirective()))
                .LoadFile(code);

            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadMethodWith_HostPrecompiler()
        {
            var code =
                $"//css_pc {Assembly.GetExecutingAssembly().Location}|{typeof(Sample_Precompiler).FullName}" + Environment.NewLine +
                """
                
                string foo()
                {
                    #if MY_DEFINE
                    return "MY_DEFINE is defined";
                    #else
                    return "MY_DEFINE is not defined";
                    #endif
                }
                """;

            dynamic script = new_evaluator.LoadMethod(code);
            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadMethod_EndPointsShort()
        {
            var precompilerScript = GetPrecompilerScript("scriptCode = \"#define MY_DEFINE\" + System.Environment.NewLine + scriptCode;");

            dynamic script = new_evaluator.LoadMethod(
                $"//css_pc {precompilerScript}|Sample_Precompiler" + Environment.NewLine +
                """
                string foo()
                    {
                #if MY_DEFINE
                        return "MY_DEFINE is defined";
                #else
                        return "MY_DEFINE is not defined";
                #endif
                    }
                """);
            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadMethod_EndPointsFull()
        {
            var precompilerScript = GetPrecompilerScript("scriptCode = \"#define MY_DEFINE\" + System.Environment.NewLine + scriptCode;");

            dynamic script = new_evaluator.LoadMethod(
                $"//css_pc {precompilerScript}|Sample_Precompiler:Compile" + Environment.NewLine +
                """
                string foo()
                    {
                #if MY_DEFINE
                        return "MY_DEFINE is defined";
                #else
                        return "MY_DEFINE is not defined";
                #endif
                    }
                """);
            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadFile()
        {
            var script = GetTempScript(@"using System;
                                         public class Calc
                                         {
                                             public int Sum(int a, int b) => a+b;
                                         }");

            dynamic calc = new_evaluator
                .WithPrecompiler(ctx => { /* */ })
                .LoadFile(script);
            var result = calc.Sum(1, 2);

            Assert.Equal(3, result);
        }

        [Fact]
        public void Precompiler_LoadMethod()
        {
            var precompilerScript = GetPrecompilerScript("scriptCode = \"#define MY_DEFINE\" + System.Environment.NewLine + scriptCode;");

            dynamic script = new_evaluator.LoadMethod(
                $"//css_pc {precompilerScript}" + Environment.NewLine +
                """
                string foo()
                    {
                #if MY_DEFINE
                        return "MY_DEFINE is defined";
                #else
                        return "MY_DEFINE is not defined";
                #endif
                    }
                """);
            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }

        [Fact]
        public void Precompiler_LoadMethod_LegacySignature()
        {
            var precompilerScript = GetPrecompilerScriptLegacy("scriptCode = \"#define MY_DEFINE\" + System.Environment.NewLine + scriptCode; return true;");

            dynamic script = new_evaluator.LoadMethod(
                $"//css_pc {precompilerScript}" + Environment.NewLine +
                """
                string foo()
                    {
                #if MY_DEFINE
                        return "MY_DEFINE is defined";
                #else
                        return "MY_DEFINE is not defined";
                #endif
                    }
                """);
            var result = script.foo();

            Assert.Equal("MY_DEFINE is defined", result);
        }
    }
}
