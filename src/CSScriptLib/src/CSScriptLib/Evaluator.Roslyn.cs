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
// Copyright (c) 2016 Oleg Shilo
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

using csscript;
using CSScripting;
using CSScripting.CodeDom;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp.Scripting
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
namespace CSScriptLib
{
    /// <summary>
    /// The information about the location of the compiler output - assembly and pdb file.
    /// </summary>
    public class CompileInfo
    {
        /// <summary>
        /// Gets or sets the compiler options for csc.exe.
        /// <para>
        /// This property is only applicable for CodeDOM based script execution as Roslyn engine does
        /// not accept string options for compilation.
        /// </para>
        /// </summary>
        /// <value>The compiler options.</value>
        public string CompilerOptions { get; set; }

        string assemblyFile;

        /// <summary>
        /// The assembly file path. If not specified it will be composed as "&lt;RootClass&gt;.dll".
        /// </summary>
        public string AssemblyFile
        {
            get
            {
                if (assemblyFile == null)
                    return this.CodeKind == SourceCodeKind.Script ? null : $"{RootClass}.dll".GetFullPath();
                else
                    return assemblyFile.GetFullPath();
            }
            set => assemblyFile = value;
        }

        /// <summary>
        /// The PDB file path.
        /// <para>
        /// Even if the this value is specified the file will not be generated unless <see
        /// cref="CSScript.EvaluatorConfig"/>.DebugBuild is set to <c>true</c>.
        /// </para>
        /// </summary>
        public string PdbFile { set; get; }

        /// <summary>
        /// Gets or sets the root class name.
        /// <para>
        /// This setting is required as Roslyn cannot produce compiled scripts with the user script
        /// class defined as a top level class. Thus all user defined classes are in fact nested
        /// classes with the root class named by Roslyn as "Submission#0". This leads to the
        /// complications when user wants to reference script class in another script. Specifically
        /// because C# treats "Submission#0" as an illegal class name.
        /// </para>
        /// <para>
        /// C# helps the situation by allowing user specified root name <see
        /// cref="CSScriptLib.CompileInfo.RootClass"/>, which is by default is "css_root".
        /// </para>
        /// </summary>
        /// <value>The root class name.</value>
        public string RootClass { set; get; } = Globals.RootClassName;

        /// <summary>
        /// Gets or sets the name of the assembly to be built from the script.
        /// </summary>
        /// <value>
        /// The name of the assembly.
        /// </value>
        public string AssemblyName { set; get; }

        /// <summary>
        /// Gets or sets a value indicating whether to prefer loading compiled script from the
        /// assembly file when it is available.
        /// </summary>
        /// <value><c>true</c> if [prefer loading from file]; otherwise, <c>false</c>.</value>
        public bool PreferLoadingFromFile { set; get; } = true;

        /// <summary>
        /// Gets or sets the kind of the script code. This property is used to control the way Roslyn engine is compiling the script code.
        /// <para>
        /// By default it is <see cref="SourceCodeKind.Regular"/> used for processing the scripts the same way as .cs/.vb files.
        /// While <see cref="SourceCodeKind.Script"/> used for processing the scripts as a canonical single file script with the
        /// top-level code.
        /// </para>
        /// <para>
        /// The <see cref="SourceCodeKind.Script"/> is the code kind that is used by <see cref="IEvaluator.Eval"/> setting which is
        /// the only API supported in the .NET applications published as single-file. Though this value can also be used in other Roslyn
        /// evaluator scenarios too (e.g. <see cref="IEvaluator.CompileCode(string, CompileInfo)"/>).
        /// </para>
        ///
        /// </summary>
        /// <value>
        /// The kind of the code.
        /// </value>
        public SourceCodeKind CodeKind { set; get; } = SourceCodeKind.Regular;

        internal string ScriptEntryPointType;
        internal string ScriptEntryPoint;
    }

    /// <summary>
    /// The exception that is thrown when a the script compiler error occurs.
    /// </summary>
    [Serializable]
    public class CompilerException : ApplicationException
    {
        /// <summary>
        /// Gets or sets the error count associated with the last script compilation.
        /// </summary>
        /// <value>The error count.</value>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Initialises a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        public CompilerException() { }

        /// <summary>
        /// Initialises a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        public CompilerException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CompilerException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public CompilerException(string message, Exception exception)
            : base(message, exception)
        {
        }

        /// <summary>
        /// Creates the CompilerException instance from the specified compiler errors.
        /// </summary>
        /// <param name="Errors">The compiler errors.</param>
        /// <param name="hideCompilerWarnings">if set to <c>true</c> hide compiler warnings.</param>
        /// <param name="resolveAutogenFilesRefs">
        /// if set to <c>true</c> all references to the path of the derived auto-generated files
        /// (e.g. errors in the decorated classless scripts) will be replaced with the path of the
        /// original files (e.g. classless script itself).
        /// </param>
        /// <returns>The method result.</returns>
        public static CompilerException Create(IEnumerable<CompilerError> Errors, bool hideCompilerWarnings, bool resolveAutogenFilesRefs)
        {
            var compileErr = new StringBuilder();

            int errorCount = 0;

            foreach (CompilerError err in Errors)
            {
                if (!err.IsWarning)
                    errorCount++;

                if (err.IsWarning && hideCompilerWarnings)
                    continue;

                if (err.FileName.HasText())
                {
                    string file = err.FileName;
                    int line = err.Line;

                    if (resolveAutogenFilesRefs)
                        CoreExtensions.NormaliseFileReference(ref file, ref line);

                    compileErr.Append(file)
                              .Append("(")
                              .Append(line)
                              .Append(",")
                              .Append(err.Column)
                              .Append("): ");
                }
                else
                {
                    compileErr.Append("BUILD: ");
                }

                if (err.IsWarning)
                    compileErr.Append("warning ");
                else
                    compileErr.Append("error ");
                compileErr.Append(err.ErrorNumber)
                          .Append(": ")
                          .Append(err.ErrorText.Trim(' '))
                          .Append(Environment.NewLine);
            }

            var retval = new CompilerException(compileErr.ToString());
            retval.Data.Add("Errors", Errors);
            retval.ErrorCount = errorCount;
            return retval;
        }
    }

    static class localExtensions
    {
        public static (string file, int line) Translate(this Dictionary<(int, int), (string, int)> mapping, int line)
        {
            foreach ((int start, int end) range in mapping.Keys)
                if (range.start <= line && line <= range.end)
                {
                    (string file, int lineOffset) = mapping[range];
                    return (file, line - range.start + lineOffset);
                }

            return ("", 0);
        }

        static public string[] SeparateUsingsFromCode(this string code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            int pos = root.Usings.FullSpan.End;

            return new[] { code.Substring(0, pos).TrimEnd(), code.Substring(pos) };
        }
    }

    /// <summary>
    /// </summary>
    /// <seealso cref="CSScriptLib.IEvaluator"/>
    public class RoslynEvaluator : EvaluatorBase<RoslynEvaluator>, IEvaluator
    {
        ScriptOptions compilerSettings = ScriptOptions.Default;

        /// <summary>
        /// Loads and returns set of referenced assemblies.
        /// <para>Notre: the set of assemblies is cleared on Reset.</para>
        /// </summary>
        /// <returns>The method result.</returns>
        public override Assembly[] GetReferencedAssemblies()
        {
            // Note all ref assemblies are already loaded as the Evaluator interface is "align" to
            // behave as Mono evaluator, which only referenced already loaded assemblies but not
            // file locations
            var assemblies = CompilerSettings.MetadataReferences
                                             .OfType<PortableExecutableReference>()
                                             .Select(r => Assembly.LoadFile(r.FilePath))
                                             .ToArray();

            return assemblies;
        }

        /// <summary>
        /// Gets or sets the compiler settings.
        /// </summary>
        /// <value>The compiler settings.</value>
        public ScriptOptions CompilerSettings
        {
            get => compilerSettings;
            set => compilerSettings = value;
        }

        /// <summary>
        /// Loads the assemblies implementing Roslyn compilers.
        /// <para>
        /// Roslyn compilers are extremely heavy and loading the compiler assemblies for with the
        /// first evaluation call can take a significant time to complete (in some cases up to 4
        /// seconds) while the consequent calls are very fast.
        /// </para>
        /// <para>
        /// You may want to call this method to pre-load the compiler assembly your script
        /// evaluation performance.
        /// </para>
        /// </summary>
        public static void LoadCompilers()
        {
            CSharpScript.EvaluateAsync("1 + 2"); //this will loaded all required assemblies
        }

        /// <summary>
        /// Compiles the specified script text.
        /// </summary>
        /// <param name="scriptText">The script text.</param>
        /// <param name="scriptFile">The script file.</param>
        /// <param name="info">The information.</param>
        /// <returns>The method result.</returns>
        /// <exception cref="CSScriptLib.CompilerException"></exception>
        override protected (byte[] asm, byte[] pdb) Compile(string scriptText, string scriptFile, CompileInfo info)
        {
            // http://www.michalkomorowski.com/2016/10/roslyn-how-to-create-custom-debuggable_27.html

            string tempScriptFile = null;

            try
            {
                if (scriptText == null && scriptFile != null)
                    scriptText = File.ReadAllText(scriptFile);

                int scriptHash = base.GetHashFor(scriptText, scriptFile);

                if (IsCachingEnabled)
                {
                    if (scriptCache.ContainsKey(scriptHash))
                        return scriptCache[scriptHash];
                }

                ////////////////////////////////////////

                var mapping = new Dictionary<(int, int), (string, int)>();

                if (scriptFile == null && new CSharpParser(scriptText, false).Imports.Any())
                {
                    tempScriptFile = CSScript.GetScriptTempFile();
                    File.WriteAllText(tempScriptFile, scriptText);
                }

                if (scriptFile == null && tempScriptFile == null)
                {
                    // if (!DisableReferencingFromCode && info?.CodeKind != SourceCodeKind.Script)
                    if (!DisableReferencingFromCode)
                    {
                        var localDir = this.GetType().Assembly.Location()?.GetDirName();
                        if (localDir.HasText())
                            ReferenceAssembliesFromCode(scriptText, localDir);
                    }

                    if (this.IsDebug)
                    {
                        tempScriptFile = CSScript.GetScriptTempFile();
                        File.WriteAllText(tempScriptFile, scriptText);
                        scriptText = $"#line 1 \"{tempScriptFile}\"{Environment.NewLine}" + scriptText;
                    }
                    else
                        scriptText = $"#line 1 \"script\"{Environment.NewLine}" + scriptText;
                }
                else
                {
                    var searchDirs = new[] { scriptFile?.GetDirName(), this.GetType().Assembly.Location()?.GetDirName() };
                    var parser = new ScriptParser(scriptFile ?? tempScriptFile, searchDirs, false);

                    var importedSources = new Dictionary<string, (int, string[])>(); // file, usings count, code lines
                    var combinedScript = new List<string>();

                    var single_source = scriptFile.ChangeExtension(".g" + scriptFile.GetExtension());
                    foreach (string file in parser.FilesToCompile.Skip(1))
                    {
                        var parts = File.ReadAllText(file).SeparateUsingsFromCode();
                        var usings = parts[0].GetLines();
                        var code = parts[1].GetLines();

                        importedSources[file] = (usings.Count(), code);
                        add_code(file, usings, 0);
                    }

                    void add_code(string file, string[] codeLines, int lineOffset)
                    {
                        int start = combinedScript.Count;
                        combinedScript.AddRange(codeLines);
                        int end = combinedScript.Count;
                        mapping[(start, end)] = (file, lineOffset);
                    }

                    combinedScript.Add($"#line 1 \"{(scriptFile ?? tempScriptFile)}\"");
                    add_code(scriptFile, scriptText.GetLines(), 0);

                    foreach (string file in importedSources.Keys)
                    {
                        (var usings_count, var code) = importedSources[file];

                        combinedScript.Add($"#line {usings_count + 1} \"{file}\""); // zos
                        add_code(file, code, usings_count);
                    }

                    scriptText = combinedScript.JoinBy(Environment.NewLine);

                    if (!DisableReferencingFromCode)
                        foreach (var asm in ProbeAssembliesOf(parser, searchDirs))
                            ReferenceAssembly(asm);
                }

                ////////////////////////////////////////

                // PrepareRefAssemblies just updates CompilerSettings.MetadataReferences
                // however SourceCodeKind.Script will require completely different referencing mechanism
                if (info == null || info.CodeKind != SourceCodeKind.Script)
                    PrepareRefAssemblies();

                var scriptOptions = CompilerSettings;

                // unfortunately the next code block will not work. Roslyn scripting fails to
                // create compilation if ParseOptions are set

                // if (this.IsDebug)
                //     try
                //     {
                //         var WithParseOptions = typeof(ScriptOptions)
                //                 .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                //                     .FirstOrDefault(x => x.Name.EndsWith("WithParseOptions"));

                //         var op = new CSharpParseOptions(preprocessorSymbols: new[] { "DEBUG", "TRACE" });
                //         scriptOptions = (ScriptOptions)WithParseOptions.Invoke(scriptOptions, new object[] { po });
                //     }
                //     catch
                //     {
                //     }

                Compilation compilation;

                if (info?.CodeKind == SourceCodeKind.Script)
                {
                    var syntaxTree = SyntaxFactory.ParseSyntaxTree(
                            scriptText,
                            new CSharpParseOptions(
                                kind: SourceCodeKind.Script,
                                languageVersion: LanguageVersion.Latest));

                    var references = new List<MetadataReference>();

                    var refs = AppDomain.CurrentDomain.GetAssemblies(); // from appdomain
                    var explicitRefs = this.refAssemblies.Except(refs); // from code
                    foreach (var asm in refs.Concat(explicitRefs))
                    {
                        unsafe
                        {
                            if (asm.TryGetRawMetadata(out var blob, out var length))
                                references.Add(AssemblyMetadata.Create(ModuleMetadata.CreateFromMetadata((IntPtr)blob, length)).GetReference());
                        }
                    }

                    // add references from code and host-specified

                    compilation = CSharpCompilation.CreateScriptCompilation(
                                           assemblyName: "Script" + Guid.NewGuid(),
                                           syntaxTree,
                                           references,
                                           returnType: typeof(object));

                    var entryPoint = compilation.GetEntryPoint(CancellationToken.None);
                    info.ScriptEntryPoint = entryPoint.MetadataName;
                    info.ScriptEntryPointType = $"{entryPoint.ContainingNamespace.MetadataName}.{entryPoint.ContainingType.MetadataName}";
                }
                else
                {
                    compilation = CSharpScript.Create(scriptText, scriptOptions)
                                              .GetCompilation();
                }

                if (info?.AssemblyName.HasText() == true)
                    compilation = compilation.WithAssemblyName(info.AssemblyName);

                if (this.IsDebug)
                    compilation = compilation.WithOptions(compilation.Options.WithOptimizationLevel(OptimizationLevel.Debug));

                compilation = compilation.WithOptions(compilation.Options.WithScriptClassName(info?.RootClass ?? Globals.RootClassName)
                                                                         .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

                using (var pdb = new MemoryStream())
                using (var asm = new MemoryStream())
                {
                    var emitOptions = new EmitOptions(false, CSScript.EvaluatorConfig.PdbFormat);

                    EmitResult result;

                    if (IsDebug)
                    {
                        if (CSScript.EvaluatorConfig.PdbFormat == DebugInformationFormat.Embedded)
                            result = compilation.Emit(asm, options: emitOptions);
                        else
                            result = compilation.Emit(asm, pdb, options: emitOptions);
                    }
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

                                var source = "<script>";
                                if (mapping.Any())
                                    (source, error_line) = mapping.Translate(error_line);
                                else
                                    error_line--; // no mapping as it was a single file so translation is minimal

                                // the actual source contains an injected '#line' directive of
                                // compiled with debug symbols so increment line after formatting
                                error_location = $"{(source.HasText() ? source : "<script>")}({error_line},{error_column}): ";
                            }
                            message.AppendLine($"{error_location}error {diagnostic.Id}: {diagnostic.GetMessage()}");
                        }

                        var errors = message.ToString();
                        throw new CompilerException(errors);
                    }
                    else
                    {
                        (byte[], byte[]) binaries;

                        asm.Seek(0, SeekOrigin.Begin);
                        byte[] buffer = asm.GetBuffer();

                        if (info?.AssemblyFile != null && info?.CodeKind != SourceCodeKind.Script)
                            File.WriteAllBytes(info.AssemblyFile, buffer);

                        if (IsDebug && CSScript.EvaluatorConfig.PdbFormat != DebugInformationFormat.Embedded)
                        {
                            pdb.Seek(0, SeekOrigin.Begin);
                            byte[] pdbBuffer = pdb.GetBuffer();

                            if (info != null && info.PdbFile.IsEmpty() && info.AssemblyFile.IsNotEmpty())
                                info.PdbFile = Path.ChangeExtension(info.AssemblyFile, ".pdb");

                            if (info?.PdbFile != null)
                                File.WriteAllBytes(info.PdbFile, pdbBuffer);

                            binaries = (buffer, pdbBuffer);
                        }
                        else
                            binaries = (buffer, null);

                        if (IsCachingEnabled)
                            scriptCache[scriptHash] = binaries;

                        return binaries;
                    }
                }
            }
            finally
            {
                if (this.IsDebug)
                    CSScript.NoteTempFile(tempScriptFile);
                else
                    tempScriptFile.FileDelete(false);
            }
        }

        /// <summary>
        /// References the given assembly.
        /// <para>
        /// It is safe to call this method multiple times for the same assembly. If the assembly
        /// already referenced it will not be referenced again.
        /// </para>
        /// </summary>
        /// <param name="assembly">The assembly instance.</param>
        /// <returns>
        /// The instance of the <see cref="T:CSScriptLib.IEvaluator"/> to allow fluent interface.
        /// </returns>
        /// <exception cref="System.Exception">
        /// Current version of {EngineName} doesn't support referencing assemblies " + "which are
        /// not loaded from the file location.
        /// </exception>
        public override IEvaluator ReferenceAssembly(Assembly assembly)
        {
            //Microsoft.Net.Compilers.1.2.0 - beta
            if (assembly.Location.IsEmpty() && !Runtime.IsSingleFileApplication)
                throw new Exception(
                    $"Current version of Roslyn-based evaluator does not support referencing assemblies " +
                     "which are not loaded from the file location.");

            if (!refAssemblies.Contains(assembly))
                refAssemblies.Add(assembly);
            return this;
        }

        /// <summary>
        /// Evaluates (executes) the specified script text, which is a top-level C# code.
        /// <para>It is the most direct equivalent of "eval" available in dynamic languages. This method is only
        /// available for Roslyn evaluator.</para>
        /// You can evaluate simple expressions:
        /// <code>
        /// var result = CSScript.Evaluator.Eval("1 + 2");
        /// </code>
        /// Or it can be a complex script, which defines its own types:
        /// <code>
        /// var calc = CSScript.Evaluator
        ///                    .Eval(@"using System;
        ///                            public class Script
        ///                            {
        ///                                public int Sum(int a, int b)
        ///                                {
        ///                                    return a+b;
        ///                                }
        ///                            }
        ///
        ///                            return new Script();");
        /// int sum = calc.Sum(1, 2);
        /// </code>
        /// <remarks>
        /// Note <see cref="IEvaluator.Eval"/> compiles and executes the script in the current AppDoman.
        /// All AppDomain loaded assemblies of the AppDomain being referenced from the script regardless of
        /// <see cref="CSScript.EvaluatorConfig"></see> setting.
        /// <para>During the script compilation, this method uses:
        /// <para>
        /// <c>CompileInfo.CodeKind=Microsoft.CodeAnalysis.SourceCodeKind.Script</c>.
        /// </para>
        /// This is the only option that supports script execution for applications published with
        /// PublishSingleFile option.</para>
        /// </remarks>
        /// <para>This method is the only option that supports script execution for applications published with
        /// PublishSingleFile option.</para>
        /// </summary>
        /// <param name="scriptText">The script text.</param>
        /// <returns>
        /// The object returned by the script.
        /// </returns>
        /// <exception cref="System.Exception">This method is only available for Roslyn evaluator.</exception>
        /// <exception cref="System.InvalidOperationException">Script entry point method could be found.</exception>
        public new dynamic Eval(string scriptText)
        {
            if (this.GetType() != typeof(RoslynEvaluator))
                throw new Exception("This method is only available for Roslyn evaluator.");

            var info = new CompileInfo { CodeKind = SourceCodeKind.Script };
            var asm = CompileCode(scriptText, info);

            var entryPointType = asm.GetType($".{info.RootClass}", true, false);
            var entryPointMethod = entryPointType?.GetTypeInfo().GetDeclaredMethod(info.ScriptEntryPoint) ?? throw new InvalidOperationException("Script entry point method could be found.");

            // var allTypes = asm.GetTypes(); // [1] is our method. Will be needed if SourceCodeKind.Script support is extended to "LoadCode" API

            var submissionFactory = (Func<object[], Task<object>>)entryPointMethod.CreateDelegate(typeof(Func<object[], Task<object>>));
            dynamic instance = submissionFactory.Invoke(new object[] { null, null }).Result;

            return instance;
        }

        List<Assembly> refAssemblies = new List<Assembly>();

        IEvaluator PrepareRefAssemblies()
        {
            foreach (var assembly in FilterAssemblies(refAssemblies))
            {
                if (assembly != null)//this check is needed when trying to load partial name assemblies that result in null
                {
                    if (assembly.Location() == null)
                    {
                        // unsafe
                        {
                            // if (asm.TryGetRawMetadata(out var blob, out var length))
                            //     references.Add(AssemblyMetadata.Create(ModuleMetadata.CreateFromMetadata((IntPtr)blob, length)).GetReference());

                            CompilerSettings = CompilerSettings.AddReferences(assembly);
                        }
                    }
                    else if (!CompilerSettings.MetadataReferences.OfType<PortableExecutableReference>().Any(r => r.FilePath.SamePathAs(assembly.Location)))
                    {    // Future assembly aliases support:
                        // MetadataReference.CreateFromFile("asm.dll", new
                        // MetadataReferenceProperties().WithAliases(new[] { "lib_a",
                        // "external_lib_a" } })
                        CompilerSettings = CompilerSettings.AddReferences(assembly);
                    }
                }
            }

            // var refs = CompilerSettings.MetadataReferences.OfType<PortableExecutableReference>()
            //                            .Select(r => r.FilePath.GetFileName())
            //                            .OrderBy(x => x)
            //                            .ToArray();
            return this;
        }

        /// <summary>
        /// Resets Evaluator.
        /// <para>
        /// Resetting means clearing all referenced assemblies, recreating evaluation infrastructure
        /// (e.g. compiler setting) and reconnection to or recreation of the underlying compiling services.
        /// </para>
        /// <para>
        /// Optionally the default current AppDomain assemblies can be referenced automatically with
        /// <paramref name="referenceDomainAssemblies"/>.
        /// </para>
        /// </summary>
        /// <param name="referenceDomainAssemblies">
        /// if set to <c>true</c> the default assemblies of the current AppDomain will be referenced
        /// (see <see
        /// cref="M:CSScriptLib.EvaluatorBase`1.ReferenceDomainAssemblies(CSScriptLib.DomainAssemblies)"/> method).
        /// </param>
        /// <returns>The freshly initialized instance of the <see cref="T:CSScriptLib.IEvaluator"/>.</returns>
        public override IEvaluator Reset(bool referenceDomainAssemblies = true)
        {
            CompilerSettings = ScriptOptions.Default;

            if (referenceDomainAssemblies)
                ReferenceDomainAssemblies();

            return this;
        }
    }
}