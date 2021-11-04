using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CSScriptLib
{
    /// <summary>
    /// Delegate which is used as a return type for AsmHelper.GetMethodInvoker().
    ///
    /// AsmHelper.GetStaticMethod() and AsmHelper.GetMethod() allow obtaining dynamic method delegate emitted on the base of the MethodInfo (from the compiled script type).
    /// </summary>
    /// <param name="paramters">Optional method parameters.</param>
    /// <returns>Returns MethodInfo return value</returns>
    public delegate object MethodDelegate(params object[] paramters);

    /// <summary>
    /// Delegate which is used as a return type for AsmHelper.GetMethodInvoker().
    ///
    /// AsmHelper.GetStaticMethod() and AsmHelper.GetMethod() allow obtaining dynamic method delegate emitted on the base of the MethodInfo (from the compiled script type).
    /// </summary>
    /// <typeparam name="T">The delegate return type.</typeparam>
    /// <param name="paramters">Optional method parameters.</param>
    /// <returns>Returns MethodInfo return value</returns>
    public delegate T MethodDelegate<T>(params object[] paramters);

    /// <summary>
    /// Type of the assemblies to be loaded/referenced.
    /// </summary>
    public enum DomainAssemblies
    {
        /// <summary>
        /// No assemblies
        /// </summary>
        None,

        /// <summary>
        /// All static current AppDomain assemblies
        /// </summary>
        AllStatic,

        /// <summary>
        /// All static and non-GAC current AppDomain assemblies
        /// </summary>
        AllStaticNonGAC,

        /// <summary>
        /// All current AppDomain assemblies
        /// </summary>
        All
    }

    /// <summary>
    /// Type of the evaluator engine.
    /// <para>This enum is used to control type of compiler the generic
    /// CSScript.<see cref="CSScriptLib.CSScript.Evaluator"/> encapsulates.</para>
    /// </summary>
    public enum EvaluatorEngine
    {
        /// <summary>
        /// Roslyn compilation services
        /// </summary>
        Roslyn,

        /// <summary>
        /// CodeDom compilation infrastructure
        /// </summary>
        CodeDom
    }

    /// <summary>
    /// Runtime instantiation model for CS-Script evaluators (e.g CSScript.<see cref="CSScriptLib.CSScript.Evaluator"/>).
    /// </summary>
    public enum EvaluatorAccess
    {
        /// <summary>
        /// Every time the member variable is accessed the same static object is returned.
        /// </summary>
        Singleton,

        /// <summary>
        /// Every time the member variable is accessed a new object is created.
        /// </summary>
        AlwaysCreate
    }

    /// <summary>
    /// Class that contains configuration options for controlling dynamic code evaluation with CSScript.<see cref="CSScriptLib.CSScript.Evaluator"/>.
    /// </summary>
    public class EvaluatorConfig
    {
        EvaluatorAccess defaultAccess = EvaluatorAccess.AlwaysCreate;

        /// <summary>
        /// Gets or sets the default access type for CS-Script evaluators.
        ///    <para>This property controls the how the generic
        /// CS-Script evaluators are instantiated when accessed (e.g.
        /// CSScript.<see cref="CSScriptLib.CSScript.Evaluator"/> or ).
        /// </para>
        /// </summary>
        /// <value>The access.</value>
        public EvaluatorAccess Access
        {
            get { return defaultAccess; }
            set { defaultAccess = value; }
        }

        EvaluatorEngine engine = EvaluatorEngine.Roslyn;

        /// <summary>
        /// Default value of the <see cref="CSScriptLib.IEvaluator"/>.
        /// DebugBuild property controlling the generation of the debug symbols.
        /// <example>
        /// <code>
        /// CSScript.EvaluatorConfig.DebugBuild = true;
        /// dynamic script = CSScript.Evaluator
        ///                          .LoadMethod(...
        /// </code>
        /// </example>
        /// </summary>
        public bool DebugBuild { get; set; } = false;

        bool refDomainAsms = true;

        /// <summary>
        /// Flag that controls if the host AppDo,main referenced assemblies are automatically referenced at creation
        /// of <see cref="CSScriptLib.IEvaluator"/>.
        /// </summary>
        [Obsolete("The name of the property method is misspelled. Use `ReferenceDomainAssemblies` instead", false)]
        public bool RefernceDomainAsemblies
        {
            get { return refDomainAsms; }
            set { refDomainAsms = value; }
        }

        /// <summary>
        /// Flag that controls if the host AppDo,main referenced assemblies are automatically referenced at creation
        /// of <see cref="CSScriptLib.IEvaluator"/>.
        /// </summary>
        public bool ReferenceDomainAssemblies
        {
            get { return refDomainAsms; }
            set { refDomainAsms = value; }
        }

        /// <summary>
        /// Gets or sets the default evaluator engine type.
        /// <para>This property controls the type of compiler the generic
        /// CSScript.<see cref="CSScriptLib.CSScript.Evaluator"/> encapsulates.</para>
        /// </summary>
        /// <value>The default evaluator engine.</value>
        public EvaluatorEngine Engine
        {
            get { return engine; }
            set { engine = value; }
        }
    }

    /// <summary>
    /// A generic interface of the CS-Script evaluator. It encapsulates the generic functionality of the evaluator regardless
    /// of the nature of the underlying compiling services (e.g. Mono, Roslyn, CodeDom).
    /// </summary>
    public interface IEvaluator
    {
        /// <summary>
        /// Gets or sets a value indicating whether to compile script with debug symbols.
        /// <para>Note, setting <c>DebugBuild</c> will only affect the current instance of Evaluator.
        /// If you want to emit debug symbols for all instances of Evaluator then use
        /// <see cref="CSScriptLib.CSScript.EvaluatorConfig"/>.DebugBuild.
        /// </para>
        /// </summary>
        /// <value><c>true</c> if 'debug build'; otherwise, <c>false</c>.</value>
        bool? DebugBuild { get; set; }

        /// <summary>
        /// This property controls script caching.
        /// <para>Caching mechanism allows avoiding multiple compilation of the scripts that have been already compiled and has not changes
        /// since then for the duration of the host process. This feature can dramatically improve the performance in the cases when you are executing
        /// the same script again and again. Even though in such cases caching is not the greatest optimization that can be achieved.</para>
        /// <para>Note that caching has some limitations. Thus the algorithm for checking if the script is changed since the last execution
        /// is limited to verifying the script code (text) only. Thus it needs to be used with caution. </para>
        /// <para>Script caching is disabled by default.</para>
        /// </summary>
        /// <example>The following is an example of caching the compilation.
        ///<code>
        /// dynamic printerScript = CSScript.Evaluator
        ///                                 .With(eval => eval.IsCachingEnabled = true)
        ///                                 .LoadFile(script_file);
        /// printerScript.Print();
        /// </code>
        /// </example>
        bool IsCachingEnabled { get; set; }

        /// <summary>
        /// CS-Script assembly unloading functionality is implemented as a combination of
        /// loading assembly into <see cref="System.Runtime.Loader.AssemblyLoadContext"/> that is marked as "IsCollectible"
        /// and the <c>ReflectionExtensions</c>.<see cref="CSScripting.AssemblyUnloadingExtensions.Unload(Assembly)"/> extension method.
        /// Unloading is only available on the runtimes that support it. Otherwise <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
        /// throws an exception on attempt to load the compiled script assembly.
        /// <para><see cref="IsAssemblyUnloadingEnabled"/> is designed to allow enabling/disabling of the
        /// assembly unloading should you find that the limitations associated with this .NET Core specific feature
        /// are not acceptable. E.g., collectible assemblies cannot be referenced from other scripts or
        /// in fact any dynamically loaded assembly for that matter.</para>
        /// </summary>
        bool IsAssemblyUnloadingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating if the script code should be analyzed and the assemblies
        /// that the script depend on (via '//css_...' and 'using ...' directives) should be referenced.
        /// </summary>
        /// <value></value>
        bool DisableReferencingFromCode { get; set; }

        /// <summary>
        /// Evaluates (compiles) C# code (script). The C# code is a typical C# code containing a single or multiple class definition(s).
        /// </summary>
        /// <example>
        ///<code>
        /// Assembly asm = CSScript.Evaluator
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
        /// <param name="info">The information about compilation context (e.g. location of the compiler output -
        /// assembly and pdb file).</param>
        /// <returns>The compiled assembly.</returns>
        Assembly CompileCode(string scriptText, CompileInfo info = null);

        /// <summary>
        /// Compiles the specified script text without loading it into the AppDomain or
        /// writing to the file system.
        /// </summary>
        /// <example>
        ///<code>
        /// try
        /// {
        ///     CSScript.Evaluator
        ///             .Check(@"using System;
        ///                      public class Script
        ///                      {
        ///                          public int Sum(int a, int b)
        ///                          {
        ///                              error
        ///                              return a+b;
        ///                          }
        ///                      }");
        /// }
        /// catch (Exception e)
        /// {
        ///     Console.WriteLine("Compile error: " + e.Message);
        /// }
        /// </code>
        /// </example>
        /// <param name="scriptText">The script text.</param>
        void Check(string scriptText);

        /// <summary>
        /// Compiles C# code (script) into assembly file. The C# code is a typical C# code containing a single or multiple class definition(s).
        /// </summary>
        /// <example>
        ///<code>
        /// string asmFile = CSScript.Evaluator
        ///                          .CompileAssemblyFromCode(
        ///                                 @"using System;
        ///                                   public class Script
        ///                                   {
        ///                                       public int Sum(int a, int b)
        ///                                       {
        ///                                           return a+b;
        ///                                       }
        ///                                   }",
        ///                                   "MyScript.dll");
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# script text.</param>
        /// <param name="outputFile">The path to the assembly file to be compiled.</param>
        /// <returns>The compiled assembly file path.</returns>
        string CompileAssemblyFromCode(string scriptText, string outputFile);

        /// <summary>
        /// Compiles C# file (script) into assembly file. The C# contains typical C# code containing a single or multiple class definition(s).
        /// </summary>
        /// <example>
        ///<code>
        /// string asmFile = CSScript.Evaluator
        ///                          .CompileAssemblyFromFile(
        ///                                 "MyScript.cs",
        ///                                 "MyScript.dll");
        /// </code>
        /// </example>
        /// <param name="scriptFile">The C# script file.</param>
        /// <param name="outputFile">The path to the assembly file to be compiled.</param>
        /// <returns>The compiled assembly file path.</returns>
        string CompileAssemblyFromFile(string scriptFile, string outputFile);

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>) and evaluates it.
        /// <para>
        /// This method is a logical equivalent of <see cref="CSScriptLib.IEvaluator.CompileCode"/> but is allows you to define
        /// your script class by specifying class method instead of whole class declaration.</para>
        /// </summary>
        /// <example>
        ///<code>
        /// dynamic script = CSScript.Evaluator
        ///                          .CompileMethod(@"int Sum(int a, int b)
        ///                                         {
        ///                                             return a+b;
        ///                                         }")
        ///                          .CreateObject("*");
        ///
        /// var result = script.Sum(7, 3);
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns>The compiled assembly.</returns>
        Assembly CompileMethod(string code);

        /// <summary>
        /// Sets the filter for referenced assemblies. The filter is to be applied just before the assemblies are to be referenced
        /// during the script execution.
        /// <code>
        /// dynamic script = CSScript.Evaluator
        ///                          .SetRefAssemblyFilter(asms =>
        ///                              asms.Where(a => !a.FullName.StartsWith("Microsoft."))
        ///                          .LoadCode(scriptCode);
        /// </code>
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        IEvaluator SetRefAssemblyFilter(Func<IEnumerable<Assembly>, IEnumerable<Assembly>> filter);

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns non-typed <see cref="CSScriptLib.MethodDelegate"/> for class-less style of invoking.</para>
        /// </summary>
        /// <example>
        /// <code>
        /// var log = CSScript.Evaluator
        ///                   .CreateDelegate(@"void Log(string message)
        ///                                     {
        ///                                         Console.WriteLine(message);
        ///                                     }");
        ///
        /// log("Test message");
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns> The instance of a 'duck typed' <see cref="CSScriptLib.MethodDelegate"/></returns>
        MethodDelegate CreateDelegate(string code);

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns typed <see cref="CSScriptLib.MethodDelegate{T}"/> for class-less style of invoking.</para>
        /// </summary>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <example>
        /// <code>
        /// var product = CSScript.Evaluator
        ///                       .CreateDelegate&lt;int&gt;(@"int Product(int a, int b)
        ///                                             {
        ///                                                 return a * b;
        ///                                             }");
        ///
        /// int result = product(3, 2);
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns> The instance of a typed <see cref="CSScriptLib.MethodDelegate{T}"/></returns>
        MethodDelegate<T> CreateDelegate<T>(string code);

        /// <summary>
        /// Analyses the script code and returns set of locations for the assemblies referenced from the code with CS-Script directives (//css_ref).
        /// </summary>
        /// <param name="code">The script code.</param>
        /// <param name="searchDirs">The assembly search/probing directories.</param>
        /// <returns>Array of the referenced assemblies</returns>
        string[] GetReferencedAssemblies(string code, params string[] searchDirs);

        /// <summary>
        /// Returns set of referenced assemblies.
        /// <para>
        /// Notre: the set of assemblies is cleared on Reset.
        /// </para>
        /// </summary>
        /// <returns>The method result.</returns>
        Assembly[] GetReferencedAssemblies();

        /// <summary>
        /// Evaluates and loads C# code to the current AppDomain. Returns instance of the first class defined in the code.
        /// </summary>
        /// <example>The following is the simple example of the LoadCode usage:
        ///<code>
        /// dynamic script = CSScript.Evaluator
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
        object LoadCode(string scriptText, params object[] args);

        /// <summary>
        /// Evaluates and loads C# code to the current AppDomain. Returns instance of the first class defined in the code.
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// public interface ICalc
        /// {
        ///     int Sum(int a, int b);
        /// }
        /// ....
        /// ICalc calc = CSScript.Evaluator
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
        /// <typeparam name="T">The type of the script class instance should be type casted to.</typeparam>
        /// <param name="scriptText">The C# script text.</param>
        /// <param name="args">The non default type <c>T</c> constructor arguments.</param>
        /// <returns>Typecasted to the <c>T</c> interface instance of the class defined in the script.</returns>
        T LoadCode<T>(string scriptText, params object[] args) where T : class;

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads
        /// the class to the current AppDomain.
        /// <para>Returns instance of <c>T</c> delegate for the first method in the auto-generated class.</para>
        /// </summary>
        ///  <example>
        /// <code>
        /// var Product = CSScript.Evaluator
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
        [ObsoleteAttribute("This method is not implemented for .NET Core oriented CS-Script versions (.NET 5 and above). " +
            "Consider using interfaces with LoadCode/LoadMethod or use CreateDelegate instead.", error: true)]
        T LoadDelegate<T>(string code) where T : class;

        /// <summary>
        /// Evaluates and loads C# code from the specified file to the current AppDomain. Returns instance of the first
        /// class defined in the script file.
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// dynamic script = CSScript.Evaluator
        ///                          .LoadFile("calc.cs");
        ///
        /// int result = script.Sum(1, 2);
        /// </code>
        /// </example>
        /// <param name="scriptFile">The C# script file.</param>
        /// <param name="args">Optional non-default constructor arguments.</param>
        /// <returns>Instance of the class defined in the script file.</returns>
        object LoadFile(string scriptFile, params object[] args);

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
        /// <param name="args">Optional non-default constructor arguments.</param>
        /// <returns>Aligned to the <c>T</c> interface instance of the class defined in the script file.</returns>
        T LoadFile<T>(string scriptFile, params object[] args) where T : class;

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), evaluates it and loads
        /// the class to the current AppDomain.
        /// </summary>
        /// <example>The following is the simple example of the LoadMethod usage:
        /// <code>
        /// dynamic script = CSScript.Evaluator
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
        object LoadMethod(string code);

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
        /// ICalc script = CSScript.Evaluator
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
        T LoadMethod<T>(string code) where T : class;

        /// <summary>
        /// References the assemblies from the script code.
        /// <para>The method analyses and tries to resolve CS-Script directives (e.g. '//css_ref') and 'used' namespaces based on the
        /// optional search directories.</para>
        /// </summary>
        /// <param name="code">The script code.</param>
        /// <param name="searchDirs">The assembly search/probing directories.</param>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator ReferenceAssembliesFromCode(string code, params string[] searchDirs);

        /// <summary>
        /// References the given assembly by the assembly path.
        /// <para>It is safe to call this method multiple times for the same assembly. If the assembly already referenced it will not
        /// be referenced again.</para>
        /// </summary>
        /// <param name="assembly">The path to the assembly file.</param>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator ReferenceAssembly(string assembly);

        /// <summary>
        /// References the given assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <param name="assembly">The assembly instance.</param>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator ReferenceAssembly(Assembly assembly);

        /// <summary>
        /// References the name of the assembly by its partial name.
        /// <para>Note that the referenced assembly will be loaded into the host AppDomain in order to resolve assembly partial name.</para>
        /// <para>It is an equivalent of <c>Evaluator.ReferenceAssembly(Assembly.LoadWithPartialName(assemblyPartialName))</c></para>
        /// </summary>
        /// <param name="assemblyPartialName">Partial name of the assembly.</param>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator ReferenceAssemblyByName(string assemblyPartialName);

        /// <summary>
        /// References the assembly by the given namespace it implements.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <param name="resolved">Set to <c>true</c> if the namespace was successfully resolved (found) and
        /// the reference was added; otherwise, <c>false</c>.</param>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator TryReferenceAssemblyByNamespace(string @namespace, out bool resolved);

        /// <summary>
        /// References the assembly by the given namespace it implements.
        /// <para>Adds assembly reference if the namespace was successfully resolved (found) and, otherwise does nothing</para>
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator ReferenceAssemblyByNamespace(string @namespace);

        /// <summary>
        /// References the assembly by the object, which belongs to this assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <param name="obj">The object, which belongs to the assembly to be referenced.</param>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator ReferenceAssemblyOf(object obj);

        /// <summary>
        /// References the assembly by the object, which belongs to this assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type which is implemented in the assembly to be referenced.</typeparam>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator ReferenceAssemblyOf<T>();

        /// <summary>
        /// References the assemblies the are already loaded into the current <c>AppDomain</c>.
        /// </summary>
        /// <param name="assemblies">The type of assemblies to be referenced.</param>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
#if net35
        IEvaluator ReferenceDomainAssemblies(DomainAssemblies assemblies);
#else

        IEvaluator ReferenceDomainAssemblies(DomainAssemblies assemblies = DomainAssemblies.AllStaticNonGAC);

#endif

#if net35
        /// <summary>
        /// References the assemblies the are already loaded into the current <c>AppDomain</c>.
        /// <para>This method is an equivalent of <see cref="CSScriptLib.IEvaluator.ReferenceDomainAssemblies"/>
        /// with the hard codded <c>DomainAssemblies.AllStaticNonGAC</c> input parameter.
        /// </para>
        /// </summary>
        /// <returns>The instance of the <see cref="CSScriptLib.IEvaluator"/> to allow  fluent interface.</returns>
        IEvaluator ReferenceDomainAssemblies();
#endif

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
        /// <returns>The freshly initialized instance of the <see cref="CSScriptLib.IEvaluator"/>.</returns>
        IEvaluator Reset(bool referenceDomainAssemblies = true);

        /// <summary>
        /// Clones the parent <see cref="CSScriptLib.IEvaluator"/>.
        /// <para>
        /// This method returns a freshly initialized copy of the <see cref="CSScriptLib.IEvaluator"/>.
        /// The cloning 'depth' can be controlled by the <paramref name="copyRefAssemblies"/>.
        /// </para>
        /// <para>
        /// This method is a convenient technique when multiple <see cref="CSScriptLib.IEvaluator"/> instances
        /// are required (e.g. for concurrent script evaluation).
        /// </para>
        /// </summary>
        /// <param name="copyRefAssemblies">if set to <c>true</c> all referenced assemblies from the parent <see cref="CSScriptLib.IEvaluator"/>
        /// will be referenced in the cloned copy.</param>
        /// <returns>The freshly initialized instance of the <see cref="CSScriptLib.IEvaluator"/>.</returns>
        /// <example>
        ///<code>
        /// var eval1 = CSScript.Evaluator.Clone();
        /// var eval2 = CSScript.Evaluator.Clone();
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
        IEvaluator Clone(bool copyRefAssemblies = true);
    }

    /// <summary>
    /// Some convenient extension methods for working with <see cref="IEvaluator"/>.
    /// </summary>
    public static class EvaluatorExtensions
    {
        /// <summary>
        /// Sets referenced assemblies filter for exclusion of some "undesired" assemblies.
        /// It is a convenient method for fine controlling referencing assemblies but without specifying
        /// the complete predicates with
        /// <see cref="IEvaluator.SetRefAssemblyFilter(Func{IEnumerable{Assembly}, IEnumerable{Assembly}})"/>.
        /// <code>
        /// dynamic script = CSScript.Evaluator
        ///                          .ExcludeReferencedAssemblies(new[]{this.GetType().Assembly})
        ///                          .LoadCode(scriptCode);
        /// </code>
        /// </summary>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="excludedAssemblies">The excluded assemblies.</param>
        /// <returns></returns>
        public static IEvaluator ExcludeReferencedAssemblies(this IEvaluator evaluator, IEnumerable<Assembly> excludedAssemblies)
            => evaluator.SetRefAssemblyFilter(asms => asms.Where(a => !excludedAssemblies.Contains(a)));

        /// <summary>
        /// Sets referenced assemblies filter for exclusion of some "undesired" assemblies.
        /// It is a convenient method for fine controlling referencing assemblies but without specifying
        /// the complete predicates with
        /// <see cref="IEvaluator.SetRefAssemblyFilter(Func{IEnumerable{Assembly}, IEnumerable{Assembly}})"/>.
        /// <code>
        /// dynamic script = CSScript.Evaluator
        ///                          .ExcludeReferencedAssemblies(this.GetType().Assembly)
        ///                          .LoadCode(scriptCode);
        /// </code>
        /// </summary>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="excludedAssemblies">The excluded assemblies.</param>
        /// <returns></returns>
        public static IEvaluator ExcludeReferencedAssemblies(this IEvaluator evaluator, params Assembly[] excludedAssemblies)
            => evaluator.SetRefAssemblyFilter(asms => asms.Where(a => !excludedAssemblies.Contains(a)));
    }
}