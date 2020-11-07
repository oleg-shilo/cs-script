#region License...

//-----------------------------------------------------------------------------
// Date:	24/01/13	Time: 9:00
// Module:	CSScriptLib.Eval.cs
// Classes:	CSScript
//			Evaluator
//
// This module contains the definition of the Evaluator class. Which wraps the common functionality
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.CSharp;
using csscript;

using MCS = Mono.CSharp;

namespace CSScriptLibrary
{
    /// <summary>
    /// Type of the build (compile) configuration
    /// </summary>
    [Obsolete("Use boolean IEvaluator.DebugBuild instead", true)]
    public enum BuildConfiguration
    {
        /// <summary>
        /// The typical Debug build configuration
        /// </summary>
        Debug,

        /// <summary>
        /// The typical Release build configuration
        /// </summary>
        Release
    }

    /// <summary>
    /// A wrapper class that encapsulates the functionality of the Mono.CSharp.Evaluator.
    /// </summary>
    public class MonoEvaluator : IEvaluator
    {
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
        /// <example>
        ///<code>
        /// var eval1 = CSScript.MonoEvaluator.Clone();
        /// var eval2 = CSScript.MonoEvaluator.Clone();
        ///
        /// var sub = eval1.LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                            @"int Sub(int a, int b) {
        ///                                  return a - b;
        ///                              }");
        ///
        /// var sum = eval2.LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                            @"int Sub(int a, int b) {
        ///                                  return a + b;
        ///                              }");
        ///
        /// var result = sum(7, sub(4,2));
        /// </code>
        /// </example>
        public IEvaluator Clone(bool copyRefAssemblies = true)
        {
            var clone = new MonoEvaluator();
            clone.ThrowOnError = this.ThrowOnError;
            clone.AutoResetEvaluatorOnError = this.AutoResetEvaluatorOnError;
            if (copyRefAssemblies)
            {
                clone.Reset(false);
                foreach (var a in this.GetReferencedAssemblies())
                    clone.ReferenceAssembly(a);
            }
            return clone;
        }

        /// <summary>
        /// Gets or sets the compiler settings.
        /// </summary>
        /// <value>The compiler settings.</value>
        public CompilerSettings CompilerSettings { get; set; }

        /// <summary>
        /// Gets or sets the compiling result.
        /// </summary>
        /// <value>The compiling result.</value>
        public CompilingResult CompilingResult { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating if the compilation error should throw an exception.
        /// </summary>
        /// <value>The throw on error.</value>
        public bool ThrowOnError { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating if the script code should be analyzed and the assemblies
        /// that the script depend on (via '//css_...' and 'using ...' directives) should be referenced.
        /// </summary>
        /// <value></value>
        public bool DisableReferencingFromCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to reset <c>Mono.Evaluator</c> automatically after it fails
        /// to compile the code.
        /// <para>It is a work around for the <c>Mono.Evaluator</c> (v4.0.0.0), which cannot longer compile the valid C# code
        /// after the first compilation failure.</para>
        /// <para>This setting allows auto recreation (reset) of the actual <c>Mono.Evaluator</c> service.</para>
        /// </summary>
        /// <value>
        /// <c>true</c> if <c>Mono.Evaluator</c> is to be reset automatically; otherwise, <c>false</c>.
        /// </value>
        public bool AutoResetEvaluatorOnError { get; set; }

        /// <summary>
        /// Gets or sets the warnings as errors.
        /// </summary>
        /// <value>The warnings as errors.</value>
        public bool WarningsAsErrors { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonoEvaluator" /> class.
        /// </summary>
        public MonoEvaluator()
        {
            ThrowOnError = true;
            AutoResetEvaluatorOnError = true;
            Reset(CSScript.EvaluatorConfig.RefernceDomainAsemblies);
            DebugBuild = CSScript.EvaluatorConfig.DebugBuild;
        }

        /// <summary>
        /// Gets or sets the flag for defining the conditional compiling symbol "DEBUG".
        /// </summary>
        /// <value>The flag indicating if the "DEBUG" symbol defined.</value>
        public bool IsDebugSymbolDefined
        {
            get
            {
                return ConditionalSymbols.Contains("DEBUG");
            }

            set
            {
                if (value)
                {
                    if (!ConditionalSymbols.Contains("DEBUG"))
                        ConditionalSymbols.Add("DEBUG");
                }
                else
                {
                    if (ConditionalSymbols.Contains("DEBUG"))
                        ConditionalSymbols.Remove("DEBUG");
                }
            }
        }

        //BuildConfiguration configuration;

        /// <summary>
        /// Gets or sets the build configuration.
        /// </summary>
        /// <value>The configuration value.</value>
        //[Obsolete("Use IEvaluator.DebugBuild instead", true)]
        //public BuildConfiguration Configuration
        //{
        //    get
        //    {
        //        return configuration;
        //    }

        //    set
        //    {
        //        configuration = value;
        //        IsDebugSymbolDefined = (value == BuildConfiguration.Debug);
        //        IsTraceSymbolDefined = (value == BuildConfiguration.Debug);
        //        CompilerSettings.GenerateDebugInfo = (value == BuildConfiguration.Debug);
        //    }
        //}

        bool? debugBuild;

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
        public bool? DebugBuild
        {
            get
            {
                return debugBuild;
            }

            set
            {
                debugBuild = value;
                IsDebugSymbolDefined = debugBuild ?? false;
                IsTraceSymbolDefined = debugBuild ?? false;
                CompilerSettings.GenerateDebugInfo = debugBuild ?? false;
            }
        }

        /// <summary>
        /// Gets or sets the flag for defining the conditional compiling symbol "TRACE".
        /// </summary>
        /// <value>The flag indicating if the "TRACE" symbol defined.</value>
        public bool IsTraceSymbolDefined
        {
            get
            {
                return ConditionalSymbols.Contains("TRACE");
            }

            set
            {
                if (value)
                {
                    if (!ConditionalSymbols.Contains("TRACE"))
                        ConditionalSymbols.Add("TRACE");
                }
                else
                {
                    if (ConditionalSymbols.Contains("TRACE"))
                        ConditionalSymbols.Remove("TRACE");
                }
            }
        }

#if net35
        /// <summary>
        /// Resets Evaluator.
        /// <para>
        /// The <see cref="Mono.CSharp.CompilerSettings"/> and <see cref="CompilingResult"/> are reinitialized.
        /// All reference assemblies are also cleared.
        /// </para>
        /// <para>The all current AppDomain assemblies will be referenced automatically.</para>
        /// </summary>
        public void Reset()
        {
            Reset(true);
        }
#endif
        /// <summary>
        /// Resets Evaluator.
        /// <para>
        /// Resetting means clearing all referenced assemblies, recreating <see cref="Mono.CSharp.CompilerSettings"/>,
        /// <see cref="CompilingResult"/> and underlying compiling services.
        /// </para>
        /// <para>Optionally the default current AppDomain assemblies can be referenced automatically with
        /// <paramref name="referenceDomainAssemblies"/>.</para>
        /// </summary>
        /// <param name="referenceDomainAssemblies">if set to <c>true</c> the default assemblies of the current AppDomain
        /// will be referenced (see <see cref="ReferenceDomainAssemblies(DomainAssemblies)"/> method).
        /// </param>
        /// <returns>The freshly initialized instance of the <see cref="CSScriptLibrary.IEvaluator"/>.</returns>
#if net35
        public IEvaluator Reset(bool referenceDomainAssemblies)
#else

        public IEvaluator Reset(bool referenceDomainAssemblies = true)
#endif
        {
            CompilingResult = new CompilingResult();

            //This is how CompilerSettings supposed to be created if you don't
            //want non default settings:
            //var cmd = new CommandLineParser(new Report(CompilingResult));
            //CompilerSettings settings = cmd.ParseArguments(new string[] { "-debug" });
            //
            // Weird I know...
            //
            //Fortunately the defaults are OK.
            CompilerSettings = CreateCompilerSettings();
            service = new MCS.Evaluator(new CompilerContext(CompilerSettings, CompilingResult));

            if (referenceDomainAssemblies)
                ReferenceDomainAssemblies();

            return this;
        }

        /// <summary>
        /// The delegate for creating Mono compiler settings instance (<see cref="CompilerSettings"/>). The delegate
        /// is a convenient way to specify global defaults for compiler settings.
        /// <example>
        /// <code>MonoEvaluator.CreateCompilerSettings = () => new CompilerSettings {Unsafe = true };</code>
        /// </example>
        /// </summary>
        public static Func<CompilerSettings> CreateCompilerSettings = () => new CompilerSettings();

        void SoftReset()
        {
            //Mono compiler (v4.0.0.0) has problem with compiling the code if the previous attempt has failed (e.g. code syntax error).
            //Thus MCS.Evaluator cannot longer compile even the valid code and needs to be re-instantiated
            service = new MCS.Evaluator(new CompilerContext(CompilerSettings, CompilingResult));
        }

        static Assembly mscorelib = 333.GetType().Assembly; //actual runtime assembly

#if net35
        /// <summary>
        /// References the assemblies the are already loaded into the current <c>AppDomain</c>.
        /// <para>This method is an equivalent of <see cref="CSScriptLibrary.IEvaluator.ReferenceDomainAssemblies"/>
        /// with the hard codded <c>DomainAssemblies.AllStaticNonGAC</c> input parameter.
        /// </para>
        /// </summary>
        /// <returns>The instance of the <see cref="CSScriptLibrary.IEvaluator"/> to allow  fluent interface.</returns>
        public IEvaluator ReferenceDomainAssemblies() //if GAC assemblies are loaded the duplicated object definitions are reported even if CompilerSettings.LoadDefaultReferences = false
        {
            return ReferenceDomainAssemblies(DomainAssemblies.AllStaticNonGAC);
        }
#endif

        /// <summary>
        /// The name prefixes of the assemblies that will be ignored when <see cref="Mono.CSharp.CompilerSettings"/> and <see cref="CompilingResult"/>
        /// references the loaded assemblies of the current AppDomain.
        /// </summary>
        public string IgnoreDomainAssembliesWithPrefixes = "xunit,Roslyn,cscscript,Microsoft.CodeAnalysis";

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
            //if GAC assemblies are loaded the duplicated object definitions are reported even if CompilerSettings.LoadDefaultReferences = false

            //NOTE: It is important to avoid loading the runtime itself (mscorelib) as it
            //will break the code evaluation (compilation).
            //
            //On .NET mscorelib is filtered out by GlobalAssemblyCache check but
            //on Mono it passes through so there is a need to do a specific check for mscorelib assembly.
            var relevantAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            var delimiters = new[] { ',', ';' };

            foreach (var prefix in (IgnoreDomainAssembliesWithPrefixes ?? "").Split(delimiters).Select(x => x.Trim()))
            {
                relevantAssemblies = relevantAssemblies.Where(a => !a.FullName.StartsWith(prefix.Trim())).ToArray();
            }

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

            var dirs = globalProbingDirs.Where(x => !string.IsNullOrEmpty(x)).ToArray();

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
            if (!Assembly2Definition.ContainsKey(assembly))
                try
                {
                    service.ReferenceAssembly(assembly);
                }
                catch { } //some assemblies can fail (e.g. xunit)
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
        /// References the assemblies from the script code.
        /// <para>The method analyses and tries to resolve CS-Script directives (e.g. '//css_ref') and 'used' namespaces based on the optional search directories.</para>
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

        ReflectionImporter Importer
        {
            get
            {
                FieldInfo info = service.GetType().GetField("importer", BindingFlags.Instance | BindingFlags.NonPublic);
                return (ReflectionImporter)info.GetValue(service);
            }
        }

        List<string> ConditionalSymbols
        {
            get
            {
                FieldInfo info = CompilerSettings.GetType().GetField("conditional_symbols", BindingFlags.Instance | BindingFlags.NonPublic);
                return (List<string>)info.GetValue(CompilerSettings);
            }
        }

        /// <summary>
        /// Returns set of referenced assemblies.
        /// <para>
        /// Notre: the set of assemblies is get cleared on Reset.
        /// </para>
        /// </summary>
        /// <returns></returns>
        public Assembly[] GetReferencedAssemblies()
        {
            return Assembly2Definition.Keys.ToArray();
        }

        static FieldInfo _FieldInfo;

        Dictionary<Assembly, IAssemblyDefinition> Assembly2Definition
        {
            get
            {
                if (_FieldInfo == null)
                    _FieldInfo = Importer.GetType().GetField("assembly_2_definition", BindingFlags.Instance | BindingFlags.NonPublic);
                return (Dictionary<Assembly, IAssemblyDefinition>)_FieldInfo.GetValue(Importer);
            }
        }

        /// <summary>
        /// Evaluates and loads C# code to the current AppDomain. Returns instance of the first class defined in the code.
        /// </summary>
        /// <example>The following is the simple example of the LoadCode usage:
        ///<code>
        /// dynamic script = CSScript.MonoEvaluator
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
            //Starting with from Mono v3.3.0 Mono.CSharp.Evaluator does not
            //return compiled class reliably (as the first '*' type).
            //This is because Evaluator now injects "<InteractiveExpressionClass>" as the first class.

            return CompileCode(scriptText).CreateObject("*", args);
        }

        /// <summary>
        /// Evaluates and loads C# code from the specified file to the current AppDomain. Returns instance of the first
        /// class defined in the script file.
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// dynamic script = CSScript.MonoEvaluator
        ///                          .LoadFile("calc.cs");
        ///
        /// int result = script.Sum(1, 2);
        /// </code>
        /// </example>/// <param name="scriptFile">The C# script file.</param>
        /// <returns>Instance of the class defined in the script file.</returns>
        public object LoadFile(string scriptFile)
        {
            return LoadCode(File.ReadAllText(scriptFile));
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
        /// ICalc calc = CSScript.MonoEvaluator
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
            var script = LoadCode(scriptText, args);
            if (script is T)
                return (T)script;

            this.ReferenceAssemblyOf<T>();
            string type = "";
            string proxyClass = script.BuildAlignToInterfaceCode<T>(out type, true);
            CompileCode(proxyClass);
            var proxyType = GetCompiledType(type);

            return (T)Activator.CreateInstance(proxyType, script);
        }

        /// <summary>
        /// Gets referenced assemblies from the script code.
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
        /// ICalc calc = CSScript.MonoEvaluator
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
            return LoadCode<T>(File.ReadAllText(scriptFile));
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads (returns instance) the class to the current AppDomain.
        /// </summary>
        /// <example>The following is the simple example of the LoadMethod usage:
        /// <code>
        /// dynamic script = CSScript.MonoEvaluator
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
            string scriptText = CSScript.WrapMethodToAutoClass(code, false);

            return LoadCode(scriptText);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads (returns instance) the class to the current AppDomain.
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
        /// ICalc script = CSScript.MonoEvaluator
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
            string scriptText = CSScript.WrapMethodToAutoClass(code, false, true, typeof(T).FullName);

            return LoadCode<T>(scriptText);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns non-typed <see cref="CSScriptLibrary.MethodDelegate"/> for class-less style of invoking.</para>
        /// </summary>
        /// <example>
        /// <code>
        /// var log = CSScript.MonoEvaluator
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
            string scriptText = CSScript.WrapMethodToAutoClass(code, true);
            return CompileCode(scriptText).GetStaticMethod();
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns typed <see cref="CSScriptLibrary.MethodDelegate{T}"/> for class-less style of invoking.</para>
        /// </summary>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <example>
        /// <code>
        /// var product = CSScript.MonoEvaluator
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
            string scriptText = CSScript.WrapMethodToAutoClass(code, true);
            return CompileCode(scriptText).GetStaticMethod<T>();
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads
        /// the class to the current AppDomain.
        /// <para>Returns instance of <c>T</c> delegate for the first method in the auto-generated class.</para>
        /// </summary>
        ///  <example>
        /// <code>
        /// var Product = CSScript.MonoEvaluator
        ///                       .LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                                      @"int Product(int a, int b)
        ///                                        {
        ///                                            return a * b;
        ///                                        }");
        ///
        /// int result = Product(3, 2);
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns>Instance of <c>T</c> delegate.</returns>
        public T LoadDelegate<T>(string code) where T : class
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, true);
            Assembly asm = CompileCode(scriptText);
            var method = asm.GetType("Scripting.DynamicClass").GetMethods().First();
            return System.Delegate.CreateDelegate(typeof(T), method) as T;
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>) and evaluates it.
        /// <para>
        /// This method is a logical equivalent of <see cref="CSScriptLibrary.IEvaluator.CompileCode"/> but is allows you to define
        /// your script class by specifying class method instead of whole class declaration.</para>
        /// </summary>
        /// <example>
        ///<code>
        /// dynamic script = CSScript.MonoEvaluator
        ///                          .CompileMethod(@"int Sum(int a, int b)
        ///                                           {
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
            string scriptText = CSScript.WrapMethodToAutoClass(code, false);
            return CompileCode(scriptText);
        }

        Assembly GetCompiledAssembly(int id)
        {
            string className = GetConnectionPointGetTypeExpression(id);
            return ((Type)service.Evaluate(className)).Assembly;
        }

        string GetConnectionPointClassDeclaration(int id)
        {
            return Environment.NewLine + " public struct CSS_ConnectionPoint_" + id + " {}";
        }

        string GetConnectionPointGetTypeExpression(int id)
        {
            return "typeof(CSS_ConnectionPoint_" + id + ");";
        }

        /// <summary>
        /// Gets a type from the last Compile/Evaluate/Load call.
        /// </summary>
        /// <param name="type">The type name.</param>
        /// <returns>The type instance</returns>
        public Type GetCompiledType(string type)
        {
            return (Type)service.Evaluate("typeof(" + type + ");");
        }

        static int AsmCounter = 0;

        /// <summary>
        /// Evaluates (compiles) C# code (script). The C# code is a typical C# code containing a single or multiple class definition(s).
        /// </summary>
        /// <example>
        ///<code>
        /// Assembly asm = CSScript.MonoEvaluator
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
            Assembly result = null;

            if (!DisableReferencingFromCode)
                ReferenceAssembliesFromCode(scriptText);

            try
            {
                HandleCompilingErrors(() =>
                {
                    int id = AsmCounter++;
                    var method = service.Compile(scriptText + GetConnectionPointClassDeclaration(id));
                    result = GetCompiledAssembly(id);
                    //cannot rely on 'method' as it is null in CS-Script scenarios
                });
            }
            catch
            {
                throw;
            }

            return result;
        }

        //use for troubleshooting
        void ReportAssemblies(string logFile)
        {
            using (var file = new StreamWriter(logFile))
            {
                foreach (var asm in this.GetReferencedAssemblies())
                {
                    file.WriteLine("asm: " + asm.ToString());
                    try
                    {
                        foreach (var type in asm.GetTypes())
                        {
                            try
                            {
                                file.WriteLine("\ttype: " + type.ToString());
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        MCS.Evaluator service;

        /// <summary>
        /// Evaluates the specified C# statement and returns the result of the execution.
        /// </summary>
        /// <example>
        /// <code>
        /// string upperCaseText = (string)CSScript.MonoEvaluator.Evaluate("\"Hello\".ToUpper();");
        /// int sum = (int)CSScript.MonoEvaluator.Evaluate("1+2;");
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# statement.</param>
        /// <returns>Result of the evaluation (execution).</returns>
        public object Evaluate(string scriptText)
        {
            object retval = null;

            HandleCompilingErrors(() =>
                {
                    retval = service.Evaluate(scriptText);
                });

            return retval;
        }

        /// <summary>
        /// Evaluates the specified C# statement. The statement must be "void" (returning no result).
        /// </summary>
        /// <example>
        /// <code>
        /// CSScript.MonoEvaluator.Run("using System;");
        /// CSScript.MonoEvaluator.Run("Console.WriteLine(\"Hello World!\");");
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# statement.</param>
        public void Run(string scriptText)
        {
            HandleCompilingErrors(() =>
                {
                    service.Run(scriptText);
                });
        }

        void HandleCompilingErrors(Action action)
        {
            //Debug.Assert(false);
            CompilingResult.Reset();

            Assembly[] initialRefAsms = this.GetReferencedAssemblies();

            try
            {
                try
                {
                    action();
                }
                catch (Exception)
                {
                    if (!CompilingResult.HasErrors)
                    {
                        throw;
                    }
                    else
                    {
                        //The exception is most likely related to the compilation error
                        //so do noting. Alternatively (may be in the future) we can add
                        //it to the errors collection.
                        //CompilingResult.Errors.Add(e.ToString());
                    }
                }

                if (ThrowOnError)
                {
                    if (CompilingResult.HasErrors || (WarningsAsErrors && CompilingResult.HasWarnings))
                    {
                        throw CompilingResult.CreateException();
                    }
                }
            }
            finally
            {
                if (CompilingResult.HasErrors && AutoResetEvaluatorOnError)
                {
                    //After reset evaluator will loose all references so need to restore them.
                    //Though all ref asms should be noted before the execution as otherwise Mono can add new refasms
                    //during the asm probing.
                    SoftReset();
                    foreach (var asm in initialRefAsms)
                        ReferenceAssembly(asm);
                }
            }
        }

        /// <summary>
        /// Gets the underlying <see cref="Mono.CSharp.Evaluator"/>.It is the actual Mono "compiler as service".
        /// </summary>
        /// <returns>Instance of <see cref="Mono.CSharp.Evaluator"/>.</returns>
        public MCS.Evaluator GetService()
        {
            return service;
        }
    }

    /// <summary>
    /// Custom implementation of <see cref="Mono.CSharp.ReportPrinter"/> required by
    /// <see cref="Mono.CSharp"/> API model for handling (reporting) compilation errors.
    /// <para><see cref="Mono.CSharp"/> default compiling error reporting (e.g. <see cref="Mono.CSharp.ConsoleReportPrinter"/>)
    /// is not dev-friendly, thus <c>CompilingResult</c> is acting as an adapter bringing the Mono API close to the
    /// traditional CodeDOM error reporting model.</para>
    /// </summary>
    public class CompilingResult : ReportPrinter
    {
        /// <summary>
        /// The collection of compiling errors.
        /// </summary>
        public List<string> Errors = new List<string>();

        /// <summary>
        /// The collection of compiling warnings.
        /// </summary>
        public List<string> Warnings = new List<string>();

        /// <summary>
        /// Indicates if the last compilation yielded any errors.
        /// </summary>
        /// <value>If set to <c>true</c> indicates presence of compilation error(s).</value>
        public bool HasErrors
        {
            get
            {
                return Errors.Count > 0;
            }
        }

        /// <summary>
        /// Indicates if the last compilation yielded any warnings.
        /// </summary>
        /// <value>If set to <c>true</c> indicates presence of compilation warning(s).</value>
        public bool HasWarnings
        {
            get
            {
                return Warnings.Count > 0;
            }
        }

#if net35
        /// <summary>
        /// Creates the <see cref="System.Exception"/> containing combined error information.
        /// </summary>
        /// <returns>Instance of the <see cref="CompilerException"/>.</returns>
        public CompilerException CreateException()
        {
            return CreateException(false);
        }
#endif
        /// <summary>
        /// Creates the <see cref="System.Exception"/> containing combined error information.
        /// Optionally warnings can also be included in the exception info.
        /// </summary>
        /// <param name="hideCompilerWarnings">The flag indicating if compiler warnings should be included in the error (<see cref="System.Exception"/>) info.</param>
        /// <returns>Instance of the <see cref="CompilerException"/>.</returns>
#if net35
        public CompilerException CreateException(bool hideCompilerWarnings)
#else

        public CompilerException CreateException(bool hideCompilerWarnings = false)
#endif
        {
            var compileErr = new StringBuilder();
            foreach (string err in Errors)
                compileErr.AppendLine(err);

            if (!hideCompilerWarnings)
                foreach (string item in Warnings)
                    compileErr.AppendLine(item);

            CompilerException retval = new CompilerException(compileErr.ToString());

            retval.Data.Add("Errors", Errors);

            if (!hideCompilerWarnings)
                retval.Data.Add("Warnings", Warnings);

            return retval;
        }

        /// <summary>
        /// Clears all errors and warnings.
        /// </summary>
        public new void Reset()
        {
            Errors.Clear();
            Warnings.Clear();
            base.Reset();
        }

        /// <summary>
        /// Handles compilation event message.
        /// </summary>
        /// <param name="msg">The compilation event message.</param>
        /// <param name="showFullPath">if set to <c>true</c> [show full path].</param>
        public override void Print(Mono.CSharp.AbstractMessage msg, bool showFullPath)
        {
            string msgInfo = string.Format("{0} {1} CS{2:0000}: {3}", msg.Location, msg.MessageType, msg.Code, msg.Text);
            if (!msg.IsWarning)
            {
                Errors.Add(msgInfo);
            }
            else
            {
                Warnings.Add(msgInfo);
            }
        }
    }
}