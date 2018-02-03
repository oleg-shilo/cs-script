#region License...

//-----------------------------------------------------------------------------
// Date:	20/12/15	Time: 9:00
// Module:	CSScriptLib.Eval.Roslyn.cs
//
// This module contains the definition of the Roslyn Evaluator class. Which wraps the common functionality
// of the Mono.CScript.Evaluator class (compiler as service)
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2004-2018 Oleg Shilo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------

#endregion License...

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using csscript;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using System.Text;

// <summary>
//<package id="Microsoft.Net.Compilers" version="1.2.0-beta-20151211-01" targetFramework="net45" developmentDependency="true" />
// Roslyn limitations:
// Script cannot have namespaces
// File-less (in-memory assemblies) cannot be referenced
// The compiled assembly is a file-less assembly so it cannot be referenced in other scripts in a normal way but only via Roslyn
// All script types are nested classes !????
// Compiling time is heavily affected by number of ref assemblies (Mono is not affected)
// Everything (e.g. class code) is compiled as a nested class with the parent class name
// "Submission#N" and the number-sign makes it extremely difficult to reference from other scripts
// </summary>
namespace CSScriptLibrary
{
    public partial class CSScript
    {
        static public void compile_local()
        {
            string path = Path.GetFullPath("script.cs");

            var lines = File.ReadAllLines(path);
            var sb = new StringBuilder();
            sb.AppendLine($"#line 1 \"{path}\"");
            foreach (var l in lines)
            {
                if (l.StartsWith("write"))
                {
                    var res = l.Substring(l.IndexOf(" ", StringComparison.Ordinal)).Trim();
                    sb.AppendLine($"System.Console.WriteLine(\"{res}\");");
                }
                else
                    sb.AppendLine(l);
            }
            // string code = $"#line 1 \"{Path.GetFullPath(file)}\";{Environment.NewLine}System.Console.WriteLine(\"Hello\");";
            // File.WriteAllText(file, code);
            string code = sb.ToString();
            var options = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default;
            var roslynScript = CSharpScript.Create(code, options);
            var compilation = roslynScript.GetCompilation();

            compilation = compilation.WithOptions(compilation.Options
               .WithOptimizationLevel(OptimizationLevel.Debug)
               .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            using (var assemblyStream = new MemoryStream())
            {
                using (var symbolStream = new MemoryStream())
                {
                    var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
                    var result = compilation.Emit(assemblyStream, symbolStream, options: emitOptions);
                    if (!result.Success)
                    {
                        var errors = string.Join(Environment.NewLine, result.Diagnostics.Select(x => x));
                        Console.WriteLine(errors);
                        return;
                    }

                    var assembly = Assembly.Load(assemblyStream.ToArray(), symbolStream.ToArray());
                    var type = assembly.GetType("Submission#0");
                    var method = type.GetMethod("<Factory>", BindingFlags.Static | BindingFlags.Public);

                    method.Invoke(null, new object[] { new object[2] });
                }
            }
        }

        /// <summary>
        /// Global instance of <see cref="CSScriptLibrary.RoslynEvaluator"/>. This object is to be used for
        /// dynamic loading of the  C# code by using Roslyn "compiler as service".
        /// <para>If you need to use multiple instances of th evaluator then you will need to call
        /// <see cref="CSScriptLibrary.IEvaluator"/>.Clone().
        /// </para>
        /// </summary>
        /// <value> The <see cref="CSScriptLibrary.RoslynEvaluator"/> instance.</value>
        static public RoslynEvaluator RoslynEvaluator
        {
            get
            {
                if (EvaluatorConfig.Access == EvaluatorAccess.AlwaysCreate)
                    return (RoslynEvaluator)roslynEvaluator.Value.Clone();
                else
                    return roslynEvaluator.Value;
            }
        }

        static Lazy<RoslynEvaluator> roslynEvaluator = new Lazy<RoslynEvaluator>();
    }

    /// <summary>
    /// Method extensions for Roslyn.<see cref="Microsoft.CodeAnalysis.CSharp.Scripting"/>
    /// </summary>
    public class CSScriptRoslynExtensions
    {
        /// <summary>
        /// Single step evaluating method for Roslyn compiler. Compiles specified code, loads the compiled assembly and returns it to the caller.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        public static Assembly Load(string code)
        {
            var asmName = CSharpScript.Create(code, ScriptOptions.Default)
                                      .RunAsync()
                                      .Result
                                      .Script
                                      .GetCompilation()
                                      .AssemblyName;

            return AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith(asmName, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// A wrapper class that encapsulates the functionality of the Roslyn  evaluator (<see cref="Microsoft.CodeAnalysis.CSharp.Scripting"/>).
    /// </summary>
    public class RoslynEvaluator : IEvaluator
    {
        static Assembly mscorelib = 333.GetType().Assembly;

        /// <summary>
        /// Gets or sets a value indicating whether to compile script with debug symbols.
        /// <para>Note, affect of setting <c>DebugBuild</c> will always depend on the compiler implementation:
        /// <list type="bullet">
        ///    <item><term>CodeDom</term><description>Fully supports. Generates degugging symbols (script can be debugged) and defines <c>DEBUG</c> and <c>TRACE</c> conditional symbols</description> </item>
        ///    <item><term>Mono</term><description>Partially supports. Defines <c>DEBUG</c> and <c>TRACE</c> conditional symbols</description> </item>
        ///    <item><term>Roslyn</term><description>Doesn't supports at all.</description> </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <value><c>true</c> if 'debug build'; otherwise, <c>false</c>.</value>
        public bool? DebugBuild { get; set; }

        bool IsDebug { get { return this.DebugBuild ?? CSScript.EvaluatorConfig.DebugBuild; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoslynEvaluator" /> class.
        /// </summary>
        public RoslynEvaluator()
        {
            if (CSScript.EvaluatorConfig.RefernceDomainAsemblies)
                ReferenceDomainAssemblies();
        }

        /// <summary>
        /// Clones itself as <see cref="CSScriptLibrary.IEvaluator"/>.
        /// <para>
        /// This method returns a freshly initialized copy of the <see cref="CSScriptLibrary.IEvaluator"/>.
        /// The cloning 'depth' can be controlled by the <paramref name="copyRefAssemblies"/>.
        /// </para>
        /// <para>
        /// This method is a convenient technique when multiple <see cref="CSScriptLibrary.IEvaluator"/> instances
        /// are required (e.g. for concurrent script evaluation).
        /// </para>
        /// </summary>
        /// <param name="copyRefAssemblies">if set to <c>true</c> all referenced assemblies from the parent <see cref="CSScriptLibrary.IEvaluator"/>
        /// will be referenced in the cloned copy.</param>
        /// <returns>The freshly initialized instance of the <see cref="CSScriptLibrary.IEvaluator"/>.</returns>
        public IEvaluator Clone(bool copyRefAssemblies = true)
        {
            var clone = new RoslynEvaluator();
            if (copyRefAssemblies)
            {
                clone.Reset(false);
                foreach (var a in this.GetReferencedAssemblies())
                    clone.ReferenceAssembly(a);
            }
            return clone;
        }

        ScriptOptions compilerSettings = ScriptOptions.Default;

        /// <summary>
        /// Gets or sets the compiler settings.
        /// </summary>
        /// <value>The compiler settings.</value>
        public ScriptOptions CompilerSettings
        {
            get { return compilerSettings; }
            set { compilerSettings = value; }
        }

        /// <summary>
        /// Loads the assemblies implementing Roslyn compilers.
        /// <para>Roslyn compilers are extremely heavy and loading the compiler assemblies for with the first
        /// evaluation call can take a significant time to complete (in some cases up to 4 seconds) while the consequent
        /// calls are very fast.
        /// </para>
        /// <para>
        /// You may want to call this method to pre-load the compiler assembly your script evaluation performance.
        /// </para>
        /// </summary>
        public static void LoadCompilers()
        {
            CSharpScript.EvaluateAsync("1 + 2"); //this will loaded all required assemblies
        }

        /// <summary>
        /// Evaluates (compiles) C# code (script). The C# code is a typical C# code containing a single or multiple class definition(s).
        /// </summary>
        /// <example>
        ///<code>
        /// Assembly asm = CSScript.RoslynEvaluator
        ///                        .CompileCode(@"using System;
        ///                                       public class Script
        ///                                       {
        ///                                           public int Sum(int a, int b)
        ///                                           {
        ///                                               return a+b;
        ///                                           }
        ///                                       }");
        ///
        /// dynamic script =  asm.CreateObject("*");
        /// var result = script.Sum(7, 3);
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# script text.</param>
        /// <returns>The compiled assembly.</returns>
        public Assembly CompileCode(string scriptText)
        {
            if (!DisableReferencingFromCode)
                ReferenceAssembliesFromCode(scriptText);

            return CompileCode(scriptText, null);
        }

        public Assembly CompileCode2(string scriptText, string scriptFile)
        {
            string tempScriptFile = null;
            try
            {
                if (this.IsDebug)
                {
                    // Excellent example of debugging support
                    // http://www.michalkomorowski.com/2016/10/roslyn-how-to-create-custom-debuggable_27.html

                    if (scriptFile == null)
                    {
                        tempScriptFile = CSScript.GetScriptTempFile();
                        File.WriteAllText(tempScriptFile, scriptText, Encoding.Default);
                    }

                    scriptText = $"#line 1 \"{scriptFile ?? tempScriptFile}\"{Environment.NewLine}" + scriptText;
                }

                // {
                //     var options = CompilerSettings;
                //     var roslynScript = CSharpScript.Create(scriptText, options);
                //     var compilation1 = roslynScript.GetCompilation();

                //     compilation1 = compilation1.WithOptions(compilation1.Options
                //        .WithOptimizationLevel(OptimizationLevel.Debug)
                //        .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

                //     using (var assemblyStream = new MemoryStream())
                //     {
                //         using (var symbolStream = new MemoryStream())
                //         {
                //             var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
                //             var result = compilation1.Emit(assemblyStream, symbolStream, options: emitOptions);
                //             if (!result.Success)
                //             {
                //                 var errors = string.Join(Environment.NewLine, result.Diagnostics.Select(x => x));
                //                 Console.WriteLine(errors);
                //             }
                //         }
                //     }
                // }

                var compilation = CSharpScript.Create(scriptText, CompilerSettings.WithFilePath(scriptFile ?? tempScriptFile))
                                              .GetCompilation();

                if (this.IsDebug)
                    compilation = compilation.WithOptions(compilation.Options
                                                                     .WithOptimizationLevel(OptimizationLevel.Debug)
                                                                     .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

                using (var pdb = new MemoryStream())
                using (var asm = new MemoryStream())
                {
                    var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);

                    EmitResult result;
                    if (IsDebug)
                        result = compilation.Emit(asm, pdb, options: emitOptions);
                    else
                        result = compilation.Emit(asm);

                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(d => d.IsWarningAsError ||
                                                                                         d.Severity == DiagnosticSeverity.Error);

                        var message = new StringBuilder();
                        foreach (Diagnostic diagnostic in failures)
                        {
                            string error_location = "";
                            if (diagnostic.Location.IsInSource)
                            {
                                var error_pos = diagnostic.Location.GetLineSpan().StartLinePosition;

                                int error_line = error_pos.Line + 1;
                                int error_column = error_pos.Character + 1;

                                // the actual source contains an injected '#line' directive f compiled with debug symbols
                                if (IsDebug)
                                    error_line--;

                                error_location = $"{diagnostic.Location.SourceTree.FilePath}({error_line},{ error_column}): ";
                            }
                            message.AppendLine($"{error_location}error {diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                        var errors = message.ToString();
                        throw new CompilerException(errors);
                    }
                    else
                    {
                        asm.Seek(0, SeekOrigin.Begin);
                        if (IsDebug)
                        {
                            pdb.Seek(0, SeekOrigin.Begin);
                            return AppDomain.CurrentDomain.Load(asm.GetBuffer(), pdb.GetBuffer());
                        }
                        else
                            return AppDomain.CurrentDomain.Load(asm.GetBuffer());
                    }
                }
            }
            finally
            {
                if (this.IsDebug)
                    CSScript.NoteTempFile(tempScriptFile);
                else
                    Utils.FileDelete(tempScriptFile, false);
            }
        }

        Assembly CompileCode(string scriptText, string scriptFile)
        {
            string tempScriptFile = null;
            try
            {
                if (this.IsDebug)
                {
                    // Excellent example of debugging support
                    // http://www.michalkomorowski.com/2016/10/roslyn-how-to-create-custom-debuggable_27.html

                    if (scriptFile == null)
                    {
                        tempScriptFile = CSScript.GetScriptTempFile();
                        File.WriteAllText(tempScriptFile, scriptText, Encoding.UTF8);
                    }

                    scriptText = $"#line 1 \"{scriptFile ?? tempScriptFile}\"{Environment.NewLine}" + scriptText;
                }

                var options = CompilerSettings;
                if (!IsDebug)
                {
                    // There is a Roslyn bug that prevents emitting debug symbols if file path is specified. And fails
                    // the whole compilation:
                    // It triggers "error CS8055: Cannot emit debug information for a source text without encoding."
                    // Thus disable setting the source path until Roslyn is fixed.
                    options = CompilerSettings.WithFilePath(scriptFile ?? tempScriptFile);
                }

                var compilation = CSharpScript.Create(scriptText, options)
                                              .GetCompilation();

                if (this.IsDebug)
                    compilation = compilation.WithOptions(compilation.Options
                                                                     .WithOptimizationLevel(OptimizationLevel.Debug)
                                                                     .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

                using (var pdb = new MemoryStream())
                using (var asm = new MemoryStream())
                {
                    var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);

                    EmitResult result;
                    if (IsDebug)
                        result = compilation.Emit(asm, pdb, options: emitOptions);
                    else
                        result = compilation.Emit(asm);

                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(d => d.IsWarningAsError ||
                                                                                         d.Severity == DiagnosticSeverity.Error);

                        var message = new StringBuilder();
                        foreach (Diagnostic diagnostic in failures)
                        {
                            string error_location = "";
                            if (diagnostic.Location.IsInSource)
                            {
                                var error_pos = diagnostic.Location.GetLineSpan().StartLinePosition;

                                int error_line = error_pos.Line + 1;
                                int error_column = error_pos.Character + 1;

                                // the actual source contains an injected '#line' directive f compiled with debug symbols
                                if (IsDebug)
                                    error_line--;

                                error_location = $"{diagnostic.Location.SourceTree.FilePath}({error_line},{ error_column}): ";
                            }
                            message.AppendLine($"{error_location}error {diagnostic.Id}: {diagnostic.GetMessage()}");
                        }
                        var errors = message.ToString();
                        throw new CompilerException(errors);
                    }
                    else
                    {
                        asm.Seek(0, SeekOrigin.Begin);
                        if (IsDebug)
                        {
                            pdb.Seek(0, SeekOrigin.Begin);
                            return AppDomain.CurrentDomain.Load(asm.GetBuffer(), pdb.GetBuffer());
                        }
                        else
                            return AppDomain.CurrentDomain.Load(asm.GetBuffer());
                    }
                }
            }
            finally
            {
                if (this.IsDebug)
                    CSScript.NoteTempFile(tempScriptFile);
                else
                    Utils.FileDelete(tempScriptFile, false);
            }
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>) and evaluates it.
        /// <para>
        /// This method is a logical equivalent of <see cref="CSScriptLibrary.IEvaluator.CompileCode"/> but is allows you to define
        /// your script class by specifying class method instead of whole class declaration.</para>
        /// </summary>
        /// <example>
        ///<code>
        /// dynamic script = CSScript.RoslynEvaluator
        ///                          .CompileMethod(@"int Sum(int a, int b)
        ///                                              {
        ///                                               return a+b;
        ///                                           }")
        ///                          .CreateObject("*");
        ///
        /// var result = script.Sum(7, 3);
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns>The compiled assembly.</returns>
        public Assembly CompileMethod(string code)
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, false, false);
            return CompileCode(scriptText);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns non-typed <see cref="CSScriptLibrary.MethodDelegate"/> for class-less style of invoking.</para>
        /// </summary>
        /// <example>
        /// <code>
        /// var log = CSScript.RoslynEvaluator
        ///                   .CreateDelegate(@"void Log(string message)
        ///                                     {
        ///                                         Console.WriteLine(message);
        ///                                     }");
        ///
        /// log("Test message");
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns> The instance of a non-typed <see cref="CSScriptLibrary.MethodDelegate"/></returns>
        public MethodDelegate CreateDelegate(string code)
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, true, false);
            return CompileCode(scriptText).GetStaticMethod();
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns typed <see cref="CSScriptLibrary.MethodDelegate{T}"/> for class-less style of invoking.</para>
        /// </summary>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <example>
        /// <code>
        /// var product = CSScript.RoslynEvaluator
        ///                       .CreateDelegate&lt;int&gt;(@"int Product(int a, int b)
        ///                                             {
        ///                                                 return a * b;
        ///                                             }");
        ///
        /// int result = product(3, 2);
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns> The instance of a typed <see cref="CSScriptLibrary.MethodDelegate{T}"/></returns>
        public MethodDelegate<T> CreateDelegate<T>(string code)
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, true, false);
            return CompileCode(scriptText).GetStaticMethod<T>();
        }

        /// <summary>
        /// Returns set of referenced assemblies.
        /// <para>
        /// Notre: the set of assemblies is cleared on Reset.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public Assembly[] GetReferencedAssemblies()
        {
            //Note all ref assemblies are already loaded as the Evaluator interface is "align" to behave as Mono evaluator,
            //which only referenced already loaded assemblies but not file locations
            var assemblies = CompilerSettings.MetadataReferences
                                             .OfType<PortableExecutableReference>()
                                             .Select(r => Assembly.LoadFrom(r.FilePath))
                                             .ToArray();

            return assemblies;
        }

        /// <summary>
        /// Analyses the script code and returns set of locations for the assemblies referenced from the code with CS-Script directives (//css_ref).
        /// </summary>
        /// <param name="code">The script code.</param>
        /// <param name="searchDirs">The assembly search/probing directories.</param>
        /// <returns>Array of the referenced assemblies</returns>
        public string[] GetReferencedAssemblies(string code, params string[] searchDirs)
        {
            var retval = new List<string>();

            var parser = new csscript.CSharpParser(code);

            var globalProbingDirs = Environment.ExpandEnvironmentVariables(CSScript.GlobalSettings.SearchDirs).Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var dirs = searchDirs.Concat(new string[] { Assembly.GetCallingAssembly().GetAssemblyDirectoryName() })
                                 .Concat(parser.ExtraSearchDirs)
                                 .Concat(globalProbingDirs)
                                 .ToArray();

            dirs = CSScript.RemovePathDuplicates(dirs);

            var asms = new List<string>(parser.RefAssemblies);

            if (!parser.IgnoreNamespaces.Any(x => x == "*"))
                asms.AddRange(parser.RefNamespaces.Except(parser.IgnoreNamespaces));

            foreach (var asm in asms)
                foreach (string asmFile in AssemblyResolver.FindAssembly(asm, dirs))
                    retval.Add(asmFile);

            return retval.Distinct().ToArray();
        }

        /// <summary>
        /// Evaluates and loads C# code to the current AppDomain. Returns instance of the first class defined in the code.
        /// </summary>
        /// <example>The following is the simple example of the LoadCode usage:
        ///<code>
        /// dynamic script = CSScript.RoslynEvaluator
        ///                          .LoadCode(@"using System;
        ///                                      public class Script
        ///                                      {
        ///                                          public int Sum(int a, int b)
        ///                                          {
        ///                                              return a+b;
        ///                                          }
        ///                                      }");
        /// int result = script.Sum(1, 2);
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# script text.</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>Instance of the class defined in the script.</returns>
        public object LoadCode(string scriptText, params object[] args)
        {
            return CompileCode(scriptText).CreateObject("*", args);
        }

        /// <summary>
        /// Evaluates and loads C# code to the current AppDomain. Returns instance of the first class defined in the code.
        /// After initializing the class instance it is aligned to the interface specified by the parameter <c>T</c>.
        /// <para><c>Note:</c> Because the interface alignment is a duck typing implementation the script class doesn't have to
        /// inherit from <c>T</c>.</para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// public interface ICalc
        /// {
        ///     int Sum(int a, int b);
        /// }
        /// ....
        /// ICalc calc = CSScript.RoslynEvaluator
        ///                      .LoadCode&lt;ICalc&gt;(@"using System;
        ///                                         public class Script
        ///                                         {
        ///                                             public int Sum(int a, int b)
        ///                                             {
        ///                                                 return a+b;
        ///                                             }
        ///                                         }");
        /// int result = calc.Sum(1, 2);
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the interface type the script class instance should be aligned to.</typeparam>
        /// <param name="scriptText">The C# script text.</param>
        /// <param name="args">The non default type <c>T</c> constructor arguments.</param>
        /// <returns>Aligned to the <c>T</c> interface instance of the class defined in the script.</returns>
        public T LoadCode<T>(string scriptText, params object[] args) where T : class
        {
            return LoadCode<T>(scriptText, null, args);
        }

        T LoadCode<T>(string scriptText, string scriptFile, params object[] args) where T : class
        {
            //Debug.Assert(false);
            if (!DisableReferencingFromCode)
                ReferenceAssembliesFromCode(scriptText);

            if (this.IsDebug)
            {
                //compile script and proxy as two separate actions

                //fin the exact class name of the script by test compiling it and browsing the assembly
                Assembly testAsm = CompileCode(scriptText, scriptFile);
                Type scriptType = AsmBrowser.FindFirstScriptUserType(testAsm);

                this.ReferenceAssemblyOf<T>();

                string proxyTypeName = "";
                string proxyClass = scriptType.BuildAlignToInterfaceCode<T>(out proxyTypeName, false);
                string parentClass = scriptType.FullName.Split('+').First(); // the full name is "Submission#0+Script"
                proxyClass = proxyClass.Replace(parentClass + ".", ""); //Compiler cannot compile the full name Submission#0.Script so convert it into short name Script
                string separator = Environment.NewLine + "// start of user code" + Environment.NewLine;

                Assembly scriptAsm = CompileCode(proxyClass + separator + scriptText, scriptFile);

                scriptType = AsmBrowser.FindFirstScriptUserType(scriptAsm, scriptType.Name);
                var proxyType = AsmBrowser.FindFirstScriptUserType(scriptAsm, proxyTypeName);

                var scriptObject = Activator.CreateInstance(scriptType, args);
                var proxyObject = Activator.CreateInstance(proxyType, new object[] { scriptObject });

                return (T)proxyObject;
            }
            else
            {
                //compile script and proxy as two separate actions
                var scriptComp = CSharpScript.Create(scriptText, CompilerSettings).RunAsync().Result;
                var scriptAsmName = scriptComp.Script.GetCompilation().AssemblyName;
                Assembly scriptAsm = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith(scriptAsmName, StringComparison.OrdinalIgnoreCase));

                var scriptObject = scriptAsm.CreateObject("*", args);
                Type scriptType = scriptObject.GetType();

                this.ReferenceAssemblyOf<T>();
                string type = "";
                string proxyClass = scriptObject.BuildAlignToInterfaceCode<T>(out type, false);
                string parentClass = scriptType.FullName.Split('+').First(); //Submission#0+Script
                proxyClass = proxyClass.Replace(parentClass + ".", ""); //Compiler cannot compile Submission#0.Script so convert it into Script

                var proxyComp = scriptComp.ContinueWithAsync(proxyClass).Result;
                var proxyAsmName = proxyComp.Script.GetCompilation().AssemblyName;
                Assembly proxyAsm = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith(proxyAsmName, StringComparison.OrdinalIgnoreCase));

                var proxyObject = proxyAsm.CreateObject("*", new object[] { scriptObject });

                return (T)proxyObject;
            }
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads
        /// the class to the current AppDomain.
        /// <para>Returns instance of <c>T</c> delegate for the first method in the auto-generated class.</para>
        /// </summary>
        ///  <example>
        /// <code>
        /// var Product = CSScript.RoslynEvaluator
        ///                       .LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                                   @"int Product(int a, int b)
        ///                                     {
        ///                                         return a * b;
        ///                                     }");
        ///
        /// int result = Product(3, 2);
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns>Instance of <c>T</c> delegate.</returns>
        public T LoadDelegate<T>(string code) where T : class
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, true, false);
            Assembly asm = CompileCode(scriptText);
            var method = asm.GetTypes().First(t => t.Name == "DynamicClass").GetMethods().First();
            return System.Delegate.CreateDelegate(typeof(T), method) as T;
        }

        /// <summary>
        /// Evaluates and loads C# code from the specified file to the current AppDomain. Returns instance of the first
        /// class defined in the script file.
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// dynamic script = CSScript.RoslynEvaluator
        ///                          .LoadFile("calc.cs");
        ///
        /// int result = script.Sum(1, 2);
        /// </code>
        /// </example>/// <param name="scriptFile">The C# script file.</param>
        /// <returns>Instance of the class defined in the script file.</returns>
        public object LoadFile(string scriptFile)
        {
            return CompileCode(File.ReadAllText(scriptFile), scriptFile).CreateObject("*");
        }

        /// <summary>
        /// Evaluates and loads C# code from the specified file to the current AppDomain. Returns instance of the first
        /// class defined in the script file.
        /// After initializing the class instance it is aligned to the interface specified by the parameter <c>T</c>.
        /// <para><c>Note:</c> the script class does not have to inherit from the <c>T</c> parameter as the proxy type
        /// will be generated anyway.</para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// public interface ICalc
        /// {
        ///     int Sum(int a, int b);
        /// }
        /// ....
        /// ICalc calc = CSScript.Evaluator
        ///                      .LoadFile&lt;ICalc&gt;("calc.cs");
        ///
        /// int result = calc.Sum(1, 2);
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the interface type the script class instance should be aligned to.</typeparam>
        /// <param name="scriptFile">The C# script text.</param>
        /// <returns>Aligned to the <c>T</c> interface instance of the class defined in the script file.</returns>
        public T LoadFile<T>(string scriptFile) where T : class
        {
            return LoadCode<T>(File.ReadAllText(scriptFile), scriptFile);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads
        /// the class to the current AppDomain.
        /// </summary>
        /// <example>The following is the simple example of the LoadMethod usage:
        /// <code>
        /// dynamic script = CSScript.RoslynEvaluator
        ///                          .LoadMethod(@"int Product(int a, int b)
        ///                                        {
        ///                                            return a * b;
        ///                                        }");
        ///
        /// int result = script.Product(3, 2);
        /// </code>
        /// </example>
        /// <param name="code">The C# script text.</param>
        /// <returns>Instance of the first class defined in the script.</returns>
        public object LoadMethod(string code)
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, false, false);

            return LoadCode(scriptText);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads
        /// the class to the current AppDomain.
        /// <para>
        /// After initializing the class instance it is aligned to the interface specified by the parameter <c>T</c>.
        /// </para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        /// <code>
        /// public interface ICalc
        /// {
        ///     int Sum(int a, int b);
        ///     int Div(int a, int b);
        /// }
        /// ....
        /// ICalc script = CSScript.RoslynEvaluator
        ///                        .LoadMethod&lt;ICalc&gt;(@"public int Sum(int a, int b)
        ///                                             {
        ///                                                 return a + b;
        ///                                             }
        ///                                             public int Div(int a, int b)
        ///                                             {
        ///                                                 return a/b;
        ///                                             }");
        /// int result = script.Div(15, 3);
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the interface type the script class instance should be aligned to.</typeparam>
        /// <param name="code">The C# script text.</param>
        /// <returns>Aligned to the <c>T</c> interface instance of the auto-generated class defined in the script.</returns>
        public T LoadMethod<T>(string code) where T : class
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, false, false);

            return LoadCode<T>(scriptText);
        }

        /// <summary>
        /// Gets or sets the flag indicating if the script code should be analyzed and the assemblies
        /// that the script depend on (via '//css_...' and 'using ...' directives) should be referenced.
        /// </summary>
        /// <value></value>
        public bool DisableReferencingFromCode { get; set; }

        /// <summary>
        /// References the assemblies from the script code.
        /// <para>The method analyses and tries to resolve CS-Script directives (e.g. '//css_ref') and 'used' namespaces based on the
        /// optional search directories.</para>
        /// </summary>
        /// <param name="code">The script code.</param>
        /// <param name="searchDirs">The assembly search/probing directories.</param>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceAssembliesFromCode(string code, params string[] searchDirs)
        {
            foreach (var asm in GetReferencedAssemblies(code, searchDirs))
                ReferenceAssembly(asm);
            return this;
        }

        /// <summary>
        /// References the given assembly by the assembly path.
        /// <para>It is safe to call this method multiple times for the same assembly. If the assembly already referenced it will not
        /// be referenced again.</para>
        /// </summary>
        /// <param name="assembly">The path to the assembly file.</param>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceAssembly(string assembly)
        {
            var globalProbingDirs = Environment.ExpandEnvironmentVariables(CSScript.GlobalSettings.SearchDirs).Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            globalProbingDirs.Add(Assembly.GetCallingAssembly().GetAssemblyDirectoryName());

            var dirs = globalProbingDirs.ToArray();

            string asmFile = AssemblyResolver.FindAssembly(assembly, dirs).FirstOrDefault();
            if (asmFile == null)
                throw new Exception("Cannot find referenced assembly '" + assembly + "'");

            ReferenceAssembly(Utils.AssemblyLoad(asmFile));
            return this;
        }

        /// <summary>
        /// References the given assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <param name="assembly">The assembly instance.</param>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceAssembly(Assembly assembly)
        {
            //Microsoft.Net.Compilers.1.2.0 - beta
            if (Utils.IsNullOrWhiteSpace(assembly.Location()))
                throw new ApplicationException(
                    "Current version of Microsoft.CodeAnalysis.Scripting.dll doesn't support referencing assemblies " +
                    "which are not loaded from the file location. You may want to use CS-Script MonoEvaluator (Mono.CSharp)");

            if (!CompilerSettings.MetadataReferences.Cast<PortableExecutableReference>().Any(r => Utils.IsSamePath(r.FilePath, assembly.Location())))
                //Future assembly aliases support:
                //MetadataReference.CreateFromFile("asm.dll", new MetadataReferenceProperties().WithAliases(new[] { "lib_a", "external_lib_a" } })
                CompilerSettings = CompilerSettings.AddReferences(assembly);

            return this;
        }

        /// <summary>
        /// References the name of the assembly by its partial name.
        /// <para>Note that the referenced assembly will be loaded into the host AppDomain in order to resolve assembly partial name.</para>
        /// <para>It is an equivalent of <c>Evaluator.ReferenceAssembly(Assembly.LoadWithPartialName(assemblyPartialName))</c></para>
        /// </summary>
        /// <param name="assemblyPartialName">Partial name of the assembly.</param>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceAssemblyByName(string assemblyPartialName)
        {
            return ReferenceAssembly(Assembly.LoadWithPartialName(assemblyPartialName));
        }

        /// <summary>
        /// References the assembly by the given namespace it implements.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <param name="resolved">Set to <c>true</c> if the namespace was successfully resolved (found) and
        /// the reference was added; otherwise, <c>false</c>.</param>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator TryReferenceAssemblyByNamespace(string @namespace, out bool resolved)
        {
            resolved = false;
            foreach (string asm in AssemblyResolver.FindGlobalAssembly(@namespace))
            {
                resolved = true;
                ReferenceAssembly(asm);
            }
            return this;
        }

        /// <summary>
        /// References the assembly by the given namespace it implements.
        /// <para>Adds assembly reference if the namespace was successfully resolved (found) and, otherwise does nothing</para>
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceAssemblyByNamespace(string @namespace)
        {
            foreach (string asm in AssemblyResolver.FindGlobalAssembly(@namespace))
                ReferenceAssembly(asm);
            return this;
        }

        /// <summary>
        /// References the assembly by the object, which belongs to this assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <param name="obj">The object, which belongs to the assembly to be referenced.</param>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceAssemblyOf(object obj)
        {
            ReferenceAssembly(obj.GetType().Assembly);
            return this;
        }

        /// <summary>
        /// References the assembly by the object, which belongs to this assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type which is implemented in the assembly to be referenced.</typeparam>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceAssemblyOf<T>()
        {
            return ReferenceAssembly(typeof(T).Assembly);
        }

#if net35
        /// <summary>
        /// References the assemblies the are already loaded into the current <c>AppDomain</c>.
        /// <para>This method is an equivalent of <see cref="CSScriptLibrary.IEvaluator.ReferenceDomainAssemblies"/>
        /// with the hard codded <c>DomainAssemblies.AllStaticNonGAC</c> input parameter.
        /// </para>
        /// </summary>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceDomainAssemblies()
        {
            return ReferenceDomainAssemblies(DomainAssemblies.AllStaticNonGAC);
        }
#endif

        /// <summary>
        /// References the assemblies the are already loaded into the current <c>AppDomain</c>.
        /// </summary>
        /// <param name="assemblies">The type of assemblies to be referenced.</param>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
#if net35
        public IEvaluator ReferenceDomainAssemblies(DomainAssemblies assemblies)
#else

        public IEvaluator ReferenceDomainAssemblies(DomainAssemblies assemblies = DomainAssemblies.AllStaticNonGAC)
#endif
        {
            //NOTE: It is important to avoid loading the runtime itself (mscorelib) as it
            //will break the code evaluation (compilation).
            //
            //On .NET mscorelib is filtered out by GlobalAssemblyCache check but
            //on Mono it passes through so there is a need to do a specific check for mscorelib assembly.
            var relevantAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (assemblies == DomainAssemblies.AllStatic)
            {
                relevantAssemblies = relevantAssemblies.Where(x => !x.IsDynamic() && x != mscorelib).ToArray();
            }
            else if (assemblies == DomainAssemblies.AllStaticNonGAC)
            {
                relevantAssemblies = relevantAssemblies.Where(x => !x.GlobalAssemblyCache && !x.IsDynamic() && x != mscorelib).ToArray();
            }
            else if (assemblies == DomainAssemblies.None)
            {
                relevantAssemblies = new Assembly[0];
            }

            foreach (var asm in relevantAssemblies)
                ReferenceAssembly(asm);

            return this;
        }

        /// <summary>
        /// Resets Evaluator.
        /// <para>
        /// Resetting means clearing all referenced assemblies, recreating evaluation infrastructure (e.g. compiler setting)
        /// and reconnection to or recreation of the underlying compiling services.
        /// </para>
        /// <para>Optionally the default current AppDomain assemblies can be referenced automatically with
        /// <paramref name="referenceDomainAssemblies"/>.</para>
        /// </summary>
        /// <param name="referenceDomainAssemblies">if set to <c>true</c> the default assemblies of the current AppDomain
        /// will be referenced (see <see cref="ReferenceDomainAssemblies(DomainAssemblies)"/> method).
        /// </param>
        /// <returns>The freshly initialized instance of the <see cref="CSScriptLibrary.IEvaluator"/>.</returns>
        public IEvaluator Reset(bool referenceDomainAssemblies = true)
        {
            CompilerSettings = ScriptOptions.Default;

            if (referenceDomainAssemblies)
                ReferenceDomainAssemblies();

            return this;
        }
    }
}