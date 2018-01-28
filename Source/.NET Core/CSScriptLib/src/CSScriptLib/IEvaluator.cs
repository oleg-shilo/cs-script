using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CSScriptLib
{
    public delegate object MethodDelegate(params object[] paramters);

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
        /// Mono compilation services
        /// </summary>
        Mono,

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
        /// </summary>
        public bool DebugBuild { get; set; } = false;

        bool refDomainAsms = true;

        /// <summary>
        /// Flag that controls if the host AppDo,main referenced assemblies are automatically referenced at creation
        /// of <see cref="CSScriptLib.IEvaluator"/>.
        /// </summary>
        public bool RefernceDomainAsemblies
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

    public partial class CSScript
    {
        static EvaluatorConfig evaluatorConfig = new EvaluatorConfig();

        /// <summary>
        /// Gets the CSScript.<see cref="CSScriptLib.EvaluatorConfig"/>, which controls the way code evaluation is conducted at runtime.
        /// </summary>
        /// <value>The evaluator CSScript.<see cref="CSScriptLib.EvaluatorConfig"/>.</value>
        public static EvaluatorConfig EvaluatorConfig
        {
            get { return evaluatorConfig; }
        }

        /// <summary>
        /// Global instance of the generic <see cref="CSScriptLib.IEvaluator"/>. This object is to be used for
        /// dynamic loading of the  C# code by "compiler as service" based on the
        /// <see cref="P:CSScriptLib.CSScript.EvaluatorConfig.Engine"/> value.
        /// <para>Generic <see cref="CSScriptLib.IEvaluator"/> interface provides a convenient way of accessing
        /// compilers without 'committing' to a specific compiler technology (e.g. Mono, Roslyn, CodeDOM). This may be
        /// required during troubleshooting or performance tuning.</para>
        /// <para>Switching between compilers can be done via global
        /// CSScript.<see cref="P:CSScriptLib.CSScript.EvaluatorConfig.Engine"/>.</para>
        /// <remarks>
        /// By default CSScript.<see cref="CSScriptLib.CSScript.Evaluator"/> always returns a new instance of
        /// <see cref="CSScriptLib.IEvaluator"/>. If this behavior is undesired change the evaluator access
        /// policy by setting <see cref="CSScriptLib.CSScript.EvaluatorConfig"/>.Access value.
        /// </remarks>
        /// </summary>
        /// <value>The <see cref="CSScriptLib.IEvaluator"/> instance.</value>
        /// <example>
        ///<code>
        /// if(testingWithMono)
        ///     CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Mono;
        /// else
        ///     CSScript.EvaluatorConfig.Engine = EvaluatorEngine.Roslyn;
        ///
        /// var sub = CSScript.Evaluator
        ///                   .LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                               @"int Sub(int a, int b) {
        ///                                     return a - b;
        ///                                 }");
        /// </code>
        /// </example>
        static public IEvaluator Evaluator
        {
            get
            {
                switch (CSScript.EvaluatorConfig.Engine)
                {
                    case EvaluatorEngine.Roslyn: return RoslynEvaluator;
                    default: return null;
                }
            }
        }

        //static Assembly RoslynAssemblyResolve(object sender, ResolveEventArgs args)
        //{
        //    //Microsoft.CodeAnalysis, Microsoft.CodeAnalysis.Scripting, Microsoft.CodeAnalysis.CSharp.Scripting
        //    if (args.Name.StartsWith("Microsoft.CodeAnalysis"))
        //    {
        //        var localDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //        var asm = Path.Combine(localDir, args.Name.Split(',')[0]) + ".dll";
        //        if (File.Exists(asm))
        //            return Assembly.LoadFrom(asm);
        //    }
        //    return null;
        //}
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
        /// <returns>The compiled assembly.</returns>
        Assembly CompileCode(string scriptText);

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
        /// <returns></returns>
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
        /// <typeparam name="T">The type of the interface type the script class instance should be aligned to.</typeparam>
        /// <param name="scriptText">The C# script text.</param>
        /// <param name="args">The non default type <c>T</c> constructor arguments.</param>
        /// <returns>Aligned to the <c>T</c> interface instance of the class defined in the script.</returns>
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
}