using csscript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CSScriptLibrary
{
    /// <summary>
    /// <para> Do not use as this interface as it's intended for use by CS-Script engine only. </para>
    /// Interface that is used by CS-Script engine to build transparent proxies for EvaluatorRemoting extensions.
    /// This interface has to be public as it is passed across assemblies. 
    /// </summary>
    public interface IRemoteAgent
    {
#pragma warning disable 1591
        object Method(params object[] paramters);
        MethodDelegate Implementation { get; set; }
#pragma warning restore 1591
    }


    /// <summary>
    /// The extremely simple implementation of generic "Extension Properties".
    /// Originally published on CodeProject: http://www.codeproject.com/Articles/399932/Extension-Properties-Revised
    /// </summary>
    public static class AttachedProperies
    {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
        public static ConditionalWeakTable<object, Dictionary<string, object>> ObjectCache = new ConditionalWeakTable<object, Dictionary<string, object>>();
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Sets the named value to the object.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static T SetValue<T>(this T obj, string name, object value) where T : class
        {
            Dictionary<string, object> properties = ObjectCache.GetOrCreateValue(obj);

            if (properties.ContainsKey(name))
                properties[name] = value;
            else
                properties.Add(name, value);

            return obj;
        }

        /// <summary>
        /// Gets the named value of the object.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name of the value.</param>
        /// <returns></returns>
        public static T GetValue<T>(this object obj, string name)
        {
            Dictionary<string, object> properties;
            if (ObjectCache.TryGetValue(obj, out properties) && properties.ContainsKey(name))
                return (T) properties[name];
            else
                return default(T);
        }

        /// <summary>
        /// Gets the named value of the object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="name">The name of the value.</param>
        /// <returns></returns>
        public static object GetValue(this object obj, string name)
        {
            return obj.GetValue<object>(name);
        }
    }

    /// <summary>
    /// Extension methods for invocation of <see cref="CSScriptLibrary.IEvaluator"/> methods in a remote AppDomain. 
    /// </summary>
    public static class EvaluatorRemoting
    {
        //  ----------------------------------------------------------------------
        // Not supported
        // - CompileCode - returns assembly, which cannot be passed back to the caller domain
        // - CompileMethod  - returns assembly, which cannot be passed back to the caller domain
        // - LoadCode - returns proxy which cannot be invoked via 'dynamic'
        // - LoadFile - returns proxy which cannot be invoked via 'dynamic'
        // - LoadMethod - returns proxy which cannot be invoked via 'dynamic'
        // - LoadDelegate - will require emitting a proxy delegate into the called domain defeating the purpose of not 
        //                  loading anything to the caller domain. 
        //                  But if really needed can be done as "https://msdn.microsoft.com/en-au/library/z43fsh67(v=vs.110).aspx"

        class RemoteLoadingContext : MarshalByRefObject
        {
            public string code;
            public string[] refAsms;
            public bool debug;
            public Type evaluatorType;
            public object scriptObj;
            public string error;

            public static IEvaluator CreateEvaluator(RemoteLoadingContext context) //must be static to ensure the instance is created in the calling AppDomain
            {
                IEvaluator eval;
                if (context.evaluatorType == typeof(MonoEvaluator))
                    eval = CSScript.MonoEvaluator;
                else if (context.evaluatorType == typeof(CodeDomEvaluator))
                    eval = CSScript.CodeDomEvaluator;
#if net45
                else if (context.evaluatorType == typeof(RoslynEvaluator))
                    eval = CSScript.RoslynEvaluator;
#endif
                else
                    throw new Exception("Unknown evaluator type: " + context.evaluatorType.FullName);

                eval.DebugBuild = context.debug;
                foreach (string asm in context.refAsms)
                    eval.ReferenceAssembly(asm);
                return eval;
            }


            public static RemoteLoadingContext NewFor(IEvaluator evaluator, string scriptCode)
            {
                var asms = evaluator.GetReferencedAssemblies()
                                    .Select(x =>
                                            {
                                                try
                                                {
                                                    return x.Location;
                                                }
                                                catch { }
                                                return null;
                                            })
                                    .Where(x => x != null);

                return new RemoteLoadingContext
                {
                    code = scriptCode,
                    refAsms = asms.ToArray(),
                    debug = evaluator.DebugBuild,
                    evaluatorType = evaluator.GetType()
                };
            }
        }

        /// <summary>
        /// Gets the remote AppDomain associated with the evaluator. 
        /// The AppDomain is set to the Evaluator with the last call of 
        /// any EvaluatorRemoting extension methods. 
        /// </summary>
        /// <param name="evaluator">The evaluator.</param>
        /// <returns></returns>
        public static AppDomain GetRemoteDomain(this IEvaluator evaluator)
        {
            return evaluator.GetValue<AppDomain>("RemoteDomain");
        }

        static IEvaluator SetRemoteDomain(this IEvaluator evaluator, AppDomain domain)
        {
            return evaluator.SetValue("RemoteDomain", domain);
        }

        static object SetOwnerDomain(this object obj, AppDomain domain)
        {
            return obj.SetValue("OwnerDomain", domain);
        }

        static object SetOwnerObject(this object obj, object owner)
        {
            return obj.SetValue("OwnerObject", owner);
        }

        /// <summary>
        /// Gets the owner/parent of the object. This method is used to access the
        /// remote object owning the MethodDelegate returned to the caller AppDomain
        /// with the EvaluatorRemoting extension methods.
        /// </summary>
        /// <typeparam name="T">The type of the .</typeparam>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static T GetOwnerObject<T>(this object obj)
        {
            return (T) obj.GetValue("OwnerObject");
        }

        internal static object CopyOwnerDomainTo(this object src, object dest)
        {
            dest.SetOwnerDomain(src.GetOwnerDomain());
            return src;
        }

        internal static object CopyOwnerObjectTo(this object src, object dest)
        {
            dest.SetOwnerObject(src.GetOwnerObject<object>());
            return src;
        }

        /// <summary>
        /// Gets the AppDomain owning the actual object the transparent proxy is associated with.
        /// The AppDomain is set to the proxy object on any EvaluatorRemoting extension methods call. 
        /// </summary>
        /// <param name="obj">The transparent proxy object.</param>
        /// <returns></returns>
        public static AppDomain GetOwnerDomain(this object obj)
        {
            return obj.GetValue<AppDomain>("OwnerDomain");
        }

        /// <summary>
        /// Unloads the AppDomain owning the actual object the transparent proxy is associated with.
        /// </summary>
        /// <param name="obj">The transparent proxy object.</param>
        public static void UnloadOwnerDomain(this object obj)
        {
            AppDomain domain = obj.GetOwnerDomain();
            if (domain != null)
                domain.Unload();
        }

        /// <summary>
        /// Loads the method remotely.
        /// LoadMethodRemotely is essentially the same as <see cref="CSScriptLibrary.EvaluatorRemoting.LoadCodeRemotely{T}"/>. 
        /// It just deals not with the whole class definition but a method(s) only. And the rest of the class definition is 
        /// added automatically by CS-Script. 
        /// </summary>
        /// <example>
        ///<code>
        /// var script = CSScript.Evaluator
        ///                      .LoadMethodRemotely&lt;ICalc&gt;(
        ///                                         @"public int Sum(int a, int b)
        ///                                           {
        ///                                               return a+b;
        ///                                           }
        ///                                           public int Sub(int a, int b)
        ///                                           {
        ///                                               return a-b;
        ///                                           }");
        /// 
        /// int result = script.Sum(15, 3));
        /// 
        /// // after the next line call the remote domain with loaded script will be unloaded
        /// script.UnloadOwnerDomain();
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the T.</typeparam>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The code.</param>
        /// <returns></returns>
        public static T LoadMethodRemotely<T>(this IEvaluator evaluator, string code) where T : class
        {
            string scriptCode = CSScript.WrapMethodToAutoClass(code, false, false, "MarshalByRefObject, " + typeof(T));
            return evaluator.LoadCodeRemotely<T>(scriptCode);
        }

        /// <summary>
        /// Loads the script file remotely.
        /// <para>This method is essentially identical to <see cref="M:CSScriptLibrary.EvaluatorRemoting.LoadCodeRemotely{T}"/>.
        /// Except it loads the code not from the in-memory string but from the file.</para>
        /// </summary>
        /// <typeparam name="T">The interface type the remote object should be casted or aligned (duck-typed) to.</typeparam>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="scriptFile">The script file.</param>
        /// <returns></returns>
        public static T LoadFileRemotely<T>(this IEvaluator evaluator, string scriptFile) where T : class
        {
            return evaluator.LoadCodeRemotely<T>(File.ReadAllText(scriptFile));
        }

        /// <summary>
        /// Loads/evaluates C# code into a remote AppDomain and returns a transparent proxy of the 
        /// instance of the first class defined in the code.
        /// <para>The returned proxy can be used to unload the AppDomain owning the actual object 
        /// the proxy points to.</para>
        /// </summary>
        /// <remarks>
        /// Note, the concrete type of the return value depends on the script class definition. 
        /// It the class implement interface then an ordinary type castes proxy object is returned. 
        /// However if the class doesn't implement the interface the a dynamically emitted duck-typed
        /// proxy returned instead. Such proxy cannot be built for the types implemented in file-less 
        /// (in-memory) assemblies. Thus neither Mono nor Roslyn engines cannot be used with this 
        /// technique. Meaning that 
        /// <see cref="CSScriptLibrary.CSScript.CodeDomEvaluator"/> needs to be used.
        /// <para>While the script class to be evaluated doesn't have to implement from 'T' interface but 
        /// it must inherit <see cref="System.MarshalByRefObject"/> though.</para>
        /// </remarks>
        /// <example>
        ///<code>
        /// // duck-typed proxy; must use CodeDomEvaluator                       
        /// var script = CSScript.CodeDomEvaluator
        ///                      .LoadCodeRemotely&lt;ICalc&gt;(
        ///                      @"using System;
        ///                        public class Calc : MarshalByRefObject
        ///                        {
        ///                            public int Sum(int a, int b)
        ///                            {
        ///                                return a + b;
        ///                            }
        ///                        }");
        ///                        
        /// // ordinary type casted proxy                       
        /// var script2 = CSScript.Evaluator
        ///                       .LoadCodeRemotely&lt;ICalc&gt;(
        ///                       @"using System;
        ///                         public class Calc : MarshalByRefObject : ICalc
        ///                         {
        ///                             public int Sum(int a, int b)
        ///                             {
        ///                                 return a + b;
        ///                             }
        ///                         }");
        ///                         
        /// int result = script.Sum(15, 3);
        ///  
        /// // after the next line call the remote domain with loaded script will be unloaded
        /// script.UnloadOwnerDomain();
        /// </code>
        /// </example>
        /// <typeparam name="T">The interface type the remote object should be casted or aligned (duck-typed) to.</typeparam>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="scriptCode">The script code that defines the script class to be loaded.  
        /// <para>The script class doesn't have to implement from 'T' interface but 
        /// it must inherit <see cref="System.MarshalByRefObject"/> though.</para>
        /// </param>
        /// <param name="probingDirs">The probing directories for the assemblies the script 
        /// assembly depends on.</param>
        /// <returns></returns>
        public static T LoadCodeRemotely<T>(this IEvaluator evaluator, string scriptCode, params string[] probingDirs) where T : class
        {
            var cx = RemoteLoadingContext.NewFor(evaluator, scriptCode);
            var searchDirs = Utils.Concat(probingDirs, Path.GetDirectoryName(typeof(T).Assembly.Location));

            var remoteDomain = evaluator.GetRemoteDomain();
            if (remoteDomain == null)
                remoteDomain = AppDomain.CurrentDomain.Clone();

            remoteDomain.Execute(context =>
            {
                try
                {
                    IEvaluator eval = RemoteLoadingContext.CreateEvaluator(context);

                    context.scriptObj = eval.CompileCode(context.code)
                                            .CreateObject("*");

                    bool implementsInterface = typeof(T).IsAssignableFrom(context.scriptObj.GetType());

                    if (!implementsInterface) //try to align to T
                        context.scriptObj = context.scriptObj.AlignToInterface<T>();
                }
                catch (Exception e)
                {
                    context.error = e.ToString();
                }
            }, cx, searchDirs);

            if (cx.error != null)
                throw new CompilerException("Exception in the remote AppDomain: " + cx.error);

            var result = (T) cx.scriptObj;

            evaluator.SetRemoteDomain(remoteDomain);
            result.SetOwnerDomain(remoteDomain);

            return result;
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>), 
        /// evaluates it and loads the class to the remote AppDomain.
        /// <para>Returns non-typed <see cref="CSScriptLibrary.MethodDelegate"/> of the remote object for 
        /// class-less style of invoking.</para>
        /// </summary>
        /// <example>
        /// <code>
        /// 
        /// var log = CSScript.Evaluator
        ///                   .CreateDelegateRemotely(
        ///                                   @"void Log(string message)
        ///                                     {
        ///                                         Console.WriteLine(message);
        ///                                     }");
        ///
        /// log("Test message");
        ///
        /// log.UnloadOwnerDomain();
        /// </code>
        /// </example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The C# code.</param>
        /// <param name="probingDirs">The probing directories for the assemblies the script 
        /// assembly depends on.</param>
        /// <returns> The instance of a 'duck typed' <see cref="CSScriptLibrary.MethodDelegate"/></returns>
        public static MethodDelegate CreateDelegateRemotely(this IEvaluator evaluator, string code, params string[] probingDirs)
        {
            string scriptCode = CSScript.WrapMethodToAutoClass(code, true, false, "MarshalByRefObject");

            var agentDef = @"
                             public class RemoteAgent : MarshalByRefObject, CSScriptLibrary.IRemoteAgent
                             {
                                 public object Method(params object[] parameters)
                                 {
                                      return Implementation(parameters);
                                 }
                                 public CSScriptLibrary.MethodDelegate Implementation {get; set;}
                             }";

            var cx = RemoteLoadingContext.NewFor(evaluator, scriptCode + agentDef);

            var remoteDomain = evaluator.GetRemoteDomain();
            if (remoteDomain == null)
                remoteDomain = AppDomain.CurrentDomain.Clone();

            remoteDomain.Execute(context =>
            {
                try
                {
                    IEvaluator eval = RemoteLoadingContext.CreateEvaluator(context);

                    var script = eval.ReferenceAssemblyOf<CSScript>()
                                     .CompileCode(context.code);

#if net45
                    string agentTypeName = script.DefinedTypes.Where(t => t.Name == "RemoteAgent").First().FullName;
#else
                    string agentTypeName = script.GetModules()
                                                 .SelectMany(m => m.GetTypes())
                                                 .Where(t => t.Name == "RemoteAgent")
                                                 .First()
                                                 .FullName;
#endif             
                    var agent = (IRemoteAgent) script.CreateObject(agentTypeName);
                    agent.Implementation = script.GetStaticMethod();
                    context.scriptObj = agent;
                }
                catch (Exception e)
                {
                    context.error = e.ToString();
                }
            }, cx, probingDirs);

            if (cx.error != null)
                throw new CompilerException("Exception in the remote AppDomain: " + cx.error);

            var agentProxy = (IRemoteAgent) cx.scriptObj;

            MethodDelegate result = (param) => agentProxy.Method(param);

            evaluator.SetRemoteDomain(remoteDomain);
            result.SetOwnerDomain(remoteDomain)
                  .SetOwnerObject(agentProxy);

            return result;
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>DynamicClass</c>) implementing interface T, 
        /// evaluates it and loads the class to the remote AppDomain.
        /// <para>Returns typed <see cref="CSScriptLibrary.MethodDelegate{T}"/> for class-less style of invoking.</para>
        /// </summary>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <example>
        /// <code>
        /// var product = CSScript.Evaluator
        ///                       .CreateDelegateRemotely&lt;int&gt;(
        ///                                           @"int Product(int a, int b)
        ///                                             {
        ///                                                 return a * b;
        ///                                             }");
        ///
        /// int result = product(3, 2);
        ///
        /// product.UnloadOwnerDomain();
        /// </code>
        /// </example>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="code">The C# code.</param>
        /// <param name="probingDirs">The probing directories for the assemblies the script 
        /// assembly depends on.</param>
        /// <returns> The instance of a 'duck typed' <see cref="CSScriptLibrary.MethodDelegate"/></returns>
        public static MethodDelegate<T> CreateDelegateRemotely<T>(this IEvaluator evaluator, string code, params string[] probingDirs)
        {
            var method = evaluator.CreateDelegateRemotely(code, probingDirs);
            MethodDelegate<T> result = (param) => (T) method(param);
            method.CopyOwnerDomainTo(result)
                  .CopyOwnerObjectTo(result);
            return result;
        }
    }

}