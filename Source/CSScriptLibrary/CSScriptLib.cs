#region Licence...

//-----------------------------------------------------------------------------
// Date:	10/11/04	Time: 3:00p
// Module:	CSScriptLib.cs
// Classes:	CSScript
//			AppInfo
//
// This module contains the definition of the CSScript class. Which implements
// compiling C# script engine (CSExecutor). Can be used for hosting C# script engine
// from any CLR application
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

#endregion Licence...

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using csscript;

namespace CSScriptLibrary
{
    /// <summary>
    /// Enum for controlling concurrency in script hosting scenarios
    /// </summary>
    public enum HostingConcurrencyControl
    {
        /// <summary>
        /// Any call to CSScript.Compile within a given process is synchronized
        /// </summary>
        ProcessScope,

        /// <summary>
        /// Any call to CSScript.Compile a specific script within a given process is synchronized
        /// </summary>
        ScriptProcessScope,

        /// <summary>
        /// Any call to CSScript.Compile a specific script is synchronized system wide
        /// </summary>
        ScriptSystemScope,
    }

    /// <summary>
    /// This class encapsulates another object for the purpose of passing it to the anonymous methods as parameters.
    /// <para>This lass helps to overcome the problem when <c>struct</c> or <c>immutable</c> types cannot be passed to the
    /// anonymous methods with the <c>ref</c> modifier.</para>
    /// <example>The following is the example of the updating the local variable of a value type from the anonymous method:
    ///<code>
    /// var count = new Ref&lt;int&gt;(1);
    ///
    /// Action&lt;Ref&lt;int&gt;&gt; increment =
    ///                   arg =>
    ///                   {
    ///                       arg.Value += 1;
    ///                   };
    ///
    /// increment(count);
    /// </code>
    /// </example>
    /// </summary>
    /// <typeparam name="T">Type of the encapsulated object.</typeparam>
    public class Ref<T> : MarshalByRefObjectWithInfiniteLifetime
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ref&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="value">The object to be encapsulated.</param>
        public Ref(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets or sets the value of the encapsulated object.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents encapsulated object.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents encapsulated object.
        /// </returns>
        public override string ToString()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    /// Class for automated assembly probing. It implments extremely simple ('optimistic')
    /// probing algorithm. At runtime it attempts to resolve the assemblies via AppDomain.Assembly resolve
    /// event by looking up the assembly files in the user dfined list of probing direcctories.
    /// The algorithm relies on the simple relationship between assembluy name and assembly file name:
    ///  &lt;assembly file&gt; = &lt;asm name&gt; + ".dll"
    /// </summary>
    ///<example>The following is an example of automated assembly probing.
    ///<code>
    /// using (SimpleAsmProbing.For(@"E:\Dev\Libs", @"E:\Dev\Packages"))
    /// {
    ///     dynamic script = CSScript.Load(script_file)
    ///                              .CreateObject("Script");
    ///     script.Print();
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="System.IDisposable" />
    public class SimpleAsmProbing : IDisposable
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            Uninit();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SimpleAsmProbing"/> class.
        /// </summary>
        ~SimpleAsmProbing()
        {
            Dispose(false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleAsmProbing"/> class.
        /// </summary>
        public SimpleAsmProbing() { }

        /// <summary>
        /// Creates and initializes a new instance of the <see cref="SimpleAsmProbing"/> class.
        /// </summary>
        /// <param name="probingDirs">The probing dirs.</param>
        /// <returns></returns>
        public static SimpleAsmProbing For(params string[] probingDirs)
        {
            return new SimpleAsmProbing(probingDirs);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleAsmProbing"/> class.
        /// </summary>
        /// <param name="probingDirs">The probing dirs.</param>
        public SimpleAsmProbing(params string[] probingDirs)
        {
            Init(probingDirs);
        }

        static bool initialized = false;
        static string[] probingDirs = new string[0];

        /// <summary>
        /// Sets probing dirs and subscribes to the <see cref="System.AppDomain.AssemblyResolve"/> event.
        /// </summary>
        /// <param name="probingDirs">The probing dirs.</param>
        public void Init(params string[] probingDirs)
        {
            SimpleAsmProbing.probingDirs = probingDirs;
            if (!initialized)
            {
                initialized = true;
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }
        }

        /// <summary>
        /// Unsubscribes to the <see cref="System.AppDomain.AssemblyResolve"/> event.
        /// </summary>
        public void Uninit()
        {
            if (initialized)
            {
                initialized = false;
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (string dir in probingDirs)
            {
                try
                {
                    string file = Path.Combine(dir, args.Name.Split(',').First().Trim() + ".dll");
                    if (File.Exists(file))
                        return Assembly.LoadFrom(file);
                }
                catch { }
            }
            return null;
        }
    }

    /// <summary>
    /// Simple helper class for extending functionality of <see cref="System.AppDomain" />.
    /// <para>
    /// This class mainly consist of the extension methods for <see cref="System.AppDomain" /> and it is to be used for executing the arbitrary
    /// code routines in the separate (temporary) <see cref="System.AppDomain" /> with the optional unloading.
    /// </para>This class is particularly useful for executing the CS-Script script in the separate <see cref="System.AppDomain" /> as this is the
    /// only way to unload the script assembly after the execution (known .NET limitation).
    /// <para>
    /// <example>The following are the examples of the execution CS-Script scripts and unloading them after the execution:
    /// <code>
    /// AppDomain.CurrentDomain
    ///          .Clone()
    ///          .Execute(Job)
    ///          .Unload();
    /// ...
    /// void Job()
    /// {
    ///     var script = CSScript.LoadMethod("some C# script code")
    ///                          .GetStaticMethod();
    ///     script();
    /// };
    /// </code>
    /// <code>
    /// AppDomain remote = AppDomain.CurrentDomain.Clone();
    /// remote.Execute(() =&gt;
    /// {
    ///     var Sum = CSScript.BuildEval(@"func(float a, float b) {
    ///                                        return a + b;
    ///                                    }");
    ///     var Average = CSScript.BuildEval(@"func(float a, float b) {
    ///                                            return (a + b)/2;
    ///                                        }");
    ///     Console.WriteLine("Sum = {0}\nAverage={1}", Sum(1f, 2f), Average(1f, 2f));
    /// });
    /// remote.Unload();
    /// </code>
    /// </example>
    /// </para>
    /// <remarks>
    /// The functionality of this class is very similar to the <see cref="CSScriptLibrary.AsmHelper" />, which also allows executing and unloading the script(s).
    /// However <see cref="CSScriptLibrary.AppDomainHelper" /> is designed as a generic class and as such it is more suitable for executing a "job" routines instead of individual scripts.
    /// <para>
    /// This creates some attractive opportunities for grouping scripting routines in a single <see cref="CSScriptLibrary.AsmHelper" />, which allows simple calling conventions (e.g. <c>CSScript.Load()</c>
    /// instead of <c>CSScript.Compile()</c>) lighter type system (e.g. no need for MarshalByRefObject inheritance).
    /// </para>
    /// </remarks>
    /// </summary>
    public static class AppDomainHelper
    {
        class RemoteExecutor : MarshalByRefObjectWithInfiniteLifetime
        {
            SimpleAsmProbing AsmProbing = new SimpleAsmProbing();

            public void InitProbing(string[] probingDrs)
            {
                AsmProbing.Init(probingDrs);
            }

            public void UninitProbing()
            {
                AsmProbing.Uninit();
            }

            public void Execute(Action action)
            {
                action();
            }

            public void Execute<T>(Action<T> action, T context)
            {
                action(context);
            }
        }

        /// <summary>
        /// Executes the <see cref="System.Action"/> delegate in the specified <see cref="System.AppDomain"/>.
        /// <example>The following are the examples of the execution CS-Script scripts and unloading them after the execution:
        ///<code>
        /// var remoteDomain = AppDomain.CurrentDomain.Clone();
        /// remoteDomain.Execute(Job)
        /// remoteDomain.Unload();
        /// ...
        ///
        /// void Job()
        /// {
        ///     var script = CSScript.LoadMethod("some C# script code")
        ///                          .GetStaticMethod();
        ///     script();
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="domain">The <see cref="System.AppDomain"/> the delegate should be executed in.</param>
        /// <param name="action">The delegate.</param>
        /// <param name="probingDirs">The assembly probing directories of the AppDomain.</param>
        /// <returns>Reference to the <see cref="System.AppDomain"/>. It is the same object, which is passed as the <paramref name="domain"/>.</returns>
        public static AppDomain Execute(this AppDomain domain, Action action, params string[] probingDirs)
        {
            var remote = (RemoteExecutor)domain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location, typeof(RemoteExecutor).ToString());
            remote.InitProbing(probingDirs);
            remote.Execute(action);
            remote.UninitProbing();
            return domain;
        }

        /// <summary>
        /// Executes the <see cref="System.Action"/> delegate in the specified <see cref="System.AppDomain"/>.
        /// <para>This method is allows you to pass the execution context parameter of the <c>T</c> type. Note <c>T</c> type must be serializable or inherited from
        /// <c>MarshalByRefObject</c>.</para>
        /// <para>This technique allows using AppDomain-neutral anonymous methods, which do not directly reference any variables from the primary
        /// <c>AddDomain</c> and yet allow interacting with this domain through the context parameter.</para>
        /// <example>The following is the example of the updating the primary AppDomain local variable <c>foo</c> from the routine executed in the remote domain:
        ///<code>
        /// var foo = new Ref&lt;string&gt;("foo");
        /// AppDomain.CurrentDomain
        ///          .Clone()
        ///          .Execute(context =&gt;
        ///                   {
        ///                       context.Value = "FOO";
        ///                   },
        ///                   context:foo)
        ///          .Unload();
        /// </code>
        /// Note that the example is using <see cref="CSScriptLibrary.Ref&lt;T&gt;"/> type to to allow custom boxing. This is because the anonymous methods do not allow ref/out parameter modifiers.
        /// </example>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="domain">The <see cref="System.AppDomain"/> the delegate should be executed in.</param>
        /// <param name="action">The delegate.</param>
        /// <param name="context">The context data. Note the type of this parameter must be either <c>serializable</c> or inherited from <c>MarshalByRefObject </c> .</param>
        /// <param name="probingDirs">The assembly probing directories of the AppDomain.</param>
        /// <returns>Reference to the <see cref="System.AppDomain"/>. It is the same object, which is passed as the <paramref name="domain"/>.</returns>
        public static AppDomain Execute<T>(this AppDomain domain, Action<T> action, T context, params string[] probingDirs)
        {
            //also possible to serialize lambda and execute it in remote AppDomain (yest it is dangerous)
            //look at MetaLinq\ExpressionBuilder
            var remote = (RemoteExecutor)domain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location, typeof(RemoteExecutor).ToString());
            remote.InitProbing(probingDirs);
            remote.Execute(action, context);
            remote.UninitProbing();
            return domain;
        }

        /// <summary>
        /// <para>Executes the delegate in the temporary <see cref="System.AppDomain"/> with the following unloading of this domain.
        /// </para>
        /// <example>The following code the complete equivalent implementation of the <c>ExecuteAndUnload</c>:
        ///<code>
        /// AppDomain.CurrentDomain
        ///          .Clone()
        ///          .Execute(action)
        ///          .Unload();
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="action">The delegate to be executed.</param>
        public static void ExecuteAndUnload(Action action)
        {
            AppDomain.CurrentDomain
                     .Clone()
                     .Execute(action)
                     .Unload();
        }

        /// <summary>
        /// Unloads the specified <see cref="System.AppDomain"/>.
        /// </summary>
        /// <param name="domain">The <see cref="System.AppDomain"/> to be unloaded.</param>
        public static void Unload(this AppDomain domain)
        {
            AppDomain.Unload(domain);
        }

        /// <summary>
        /// Clones the specified <see cref="System.AppDomain"/>. The mandatory "creation" properties of the <paramref name="domain"/> are used to create the new instance of <see cref="System.AppDomain"/>.
        /// <para>The "friendly name" of the cloned <see cref="System.AppDomain"/> is a string representation of the random <c>GUID</c>.</para>
        /// </summary>
        /// <param name="domain">The <see cref="System.AppDomain"/> to be cloned.</param>
        /// <returns>The newly created <see cref="System.AppDomain"/>.</returns>
        public static AppDomain Clone(this AppDomain domain)
        {
            return domain.Clone(Guid.NewGuid().ToString(), null);
        }

#if net4

        /// <summary>
        /// Gets the strong name of the assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>Assembly strong name.</returns>
        public static StrongName GetStrongName(this Assembly assembly)
        {
            return assembly.Evidence.GetHostEvidence<StrongName>();
        }

        /// <summary>
        /// Gets the original location of the script that the assemble been compiled from.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        public static string GetOriginalLocation(this Assembly assembly)
        {
            string location = assembly.Location();
            if (location != null && location != "")
                return location;
            return null;
        }

        /// <summary>
        /// Gets the name of the directory from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static string GetDirName(this string path)
        {
            return Path.GetDirectoryName(path ?? "");
        }

#endif

        /// <summary>
        /// Clones the specified <see cref="System.AppDomain"/>. The mandatory "creation" properties of the <paramref name="domain"/> are used to create the new instance of <see cref="System.AppDomain"/>.
        /// <para>The <paramref name="name"/> parameter is used as the "friendly name" for the cloned <see cref="System.AppDomain"/>.</para>
        /// </summary>
        /// <param name="domain">The <see cref="System.AppDomain"/> to be cloned.</param>
        /// <param name="name">The "friendly name" of the new <see cref="System.AppDomain"/> to be created.</param>
        /// <param name="permissions">The permissions.</param>
        /// <param name="fullyTrustedAssemblies">The fully trusted assemblies.</param>
        /// <returns>The newly created <see cref="System.AppDomain"/>.</returns>
#if net4

        public static AppDomain Clone(this AppDomain domain, string name, PermissionSet permissions = null, params StrongName[] fullyTrustedAssemblies)
#else

        public static AppDomain Clone(this AppDomain domain, string name, PermissionSet permissions, params StrongName[] fullyTrustedAssemblies)
#endif
        {
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            setup.PrivateBinPath = AppDomain.CurrentDomain.BaseDirectory;
            setup.ShadowCopyFiles = Environment.GetEnvironmentVariable("AppDomainSetup.ShadowCopyFiles") ?? "true";
            setup.ShadowCopyDirectories = setup.ApplicationBase;

            if (permissions != null)
                return AppDomain.CreateDomain(name, null, setup, permissions, fullyTrustedAssemblies);
            else
                return AppDomain.CreateDomain(name, null, setup);
        }
    }

    /// <summary>
    /// Simple security helper class. This class is nothing else but a syntactic sugar.
    /// <para>
    /// <example>The following is an example of execution under .NET sandbox:
    /// <code>
    /// Sandbox.With(SecurityPermissionFlag.Execution)
    ///        .Execute(() =>
    ///                 {
    ///                     //call sandboxed actions
    ///                 });
    /// </code>
    /// </example>
    /// </para>
    /// </summary>
    public static class Sandbox
    {
        /// <summary>
        /// Generic void/void delegate
        /// </summary>
        public delegate void Action();

        /// <summary>
        /// Extension method. To assist with Fluent API.
        /// </summary>
        /// <param name="obj">The object that is a subject of Fluent invocation.</param>
        /// <param name="action">The action to be performed against object.</param>
        public static T With<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }

        /// <summary>
        /// Extension method. Executes <see cref="T:System.Action"/> with the specified array of permissions
        /// </summary>
        /// <param name="permissions">The permissions set to be used for the execution.</param>
        /// <param name="action">The action to be executed.</param>
        public static void Execute(this PermissionSet permissions, Action action)
        {
            permissions.PermitOnly();

            try
            {
                action();
            }
            finally
            {
                CodeAccessPermission.RevertPermitOnly();
            }
        }

        /// <summary>
        /// Returns the specified permissions as <see cref="System.Security.PermissionSet"/> to be used with <see cref="CSScriptLibrary.Sandbox.Execute"/>.
        /// </summary>
        /// <param name="permissions">The permissions.</param>
        /// <returns><see cref="System.Security.PermissionSet"/> instance.</returns>
        public static PermissionSet With(params IPermission[] permissions)
        {
            PermissionSet permissionSet = new PermissionSet(PermissionState.None);

            foreach (IPermission permission in permissions)
                permissionSet.AddPermission(permission);

            return permissionSet;
        }

        /// <summary>
        /// Returns the specified permissions as <see cref="System.Security.PermissionSet"/> to be used with <see cref="М:csscript.Sandbox.Execute"/>.
        /// </summary>
        /// <param name="permissionsFlag">The permissions flag. Can be combination of multiple values.</param>
        /// <returns><see cref="System.Security.PermissionSet"/> instance.</returns>
        public static PermissionSet With(SecurityPermissionFlag permissionsFlag)
        {
            PermissionSet permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(new SecurityPermission(permissionsFlag));

            return permissionSet;
        }
    }

    /// <summary>
    /// Delegate to handle output from script
    /// </summary>
    public delegate void PrintDelegate(string msg);

    /// <summary>
    /// Delegate to determine if the script assembly is out of data and needs to be recompiled
    /// </summary>
    /// <param name="scriptSource">The script source.</param>
    /// <param name="scriptAssembly">The script assembly.</param>
    /// <returns>'true' if the script assembly is out of date.</returns>
    public delegate bool IsOutOfDateResolver(string scriptSource, string scriptAssembly);

    /// <summary>
    /// Class which is implements CS-Script class library interface.
    /// </summary>
    public partial class CSScript
    {
        static string dummy = "";

        /// <summary>
        /// Default constructor
        /// </summary>
        public CSScript()
        {
            rethrow = false;
        }

        static CSScript()
        {
            AssemblyResolvingEnabled = true;
#if net45
            AppDomain.CurrentDomain.AssemblyResolve += CSScript.RoslynAssemblyResolve;
#endif
        }

        /// <summary>
        /// Determines whether the specified assembly is a script assembly (compiled script) and returns full path of the script file
        /// used to compile the assembly. The analysis is based on the fact that script assembly (in hosing scenarios) is always
        /// stamped with <see cref="System.Reflection.AssemblyDescriptionAttribute"/>, which contains name of the script file the
        /// assembly was compiled from.
        /// <para>The format of the description </para>
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>
        /// 	Script file path if the specified assembly is a script assembly otherwise <c>null</c>.
        /// </returns>
        static public string GetScriptName(Assembly assembly)
        {
            //Note assembly can contain only single AssemblyDescriptionAttribute
            foreach (AssemblyDescriptionAttribute attribute in assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true))
                return attribute.Description;
            return null;
        }

        /// <summary>
        /// Force caught exceptions to be re-thrown.
        /// </summary>
        static public bool Rethrow
        {
            get { return rethrow; }
            set { rethrow = value; }
        }

        /// <summary>
        /// Aggregates the referenced assemblies found by parser.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="searchDirs">Extra search/probing directories.</param>
        /// <param name="defaultRefAssemblies">The default reference assemblies. It is a semicolon separated assembly names string
        /// (e.g. "System.Core; System.Linq;").</param>
        /// <returns></returns>
        public static string[] AggregateReferencedAssemblies(ScriptParser parser, string[] searchDirs, string defaultRefAssemblies)
        {
            //Interface to be used via reflection (e.g. by Notepad++ plugin)
            CSExecutor executor = new CSExecutor();
            executor.GetOptions().searchDirs = searchDirs;
            executor.GetOptions().defaultRefAssemblies = defaultRefAssemblies;
            return executor.AggregateReferencedAssemblies(parser);
        }

        /// <summary>
        /// Enables automatic resolving of unsuccessful assembly probing on the base of the Settings.SearchDirs.
        /// Default value is true.
        ///
        /// CLR does assembly probing only in GAC and in the local (with respect to the application) directories. CS-Script
        /// however allows you to specify extra directory(es) for assembly probing by setting enabling CS-Script assembly resolving
        /// through setting the AssemblyResolvingEnabled to true and changing the Settings.SearchDirs appropriately.
        /// </summary>
        static public bool AssemblyResolvingEnabled
        {
            get { return assemblyResolvingEnabled; }
            set
            {
                if (value)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);
                    callingResolveEnabledAssembly = Assembly.GetCallingAssembly();
                }
                else
                    AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(OnAssemblyResolve);

                assemblyResolvingEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether setting 'location:&lt;assm_hash&gt;' environment variable is enabled.
        /// <para>
        /// This variable is useful for finding the compiled assembly file from the inside of the script code.
        /// Even when the script loaded in-memory (InMemoryAssembly setting) but not from the original file.
        /// </para>
        /// <example>
        /// <code>
        /// // called from the script code
        /// var asm_hash = Assembly.GetExecutingAssembly().GetHashCode();
        /// var script_location = Environment.GetEnvironmentVariable(\"location:\" + asm_hash);
        /// </code>
        /// </example>
        /// <para>
        /// Enabling script reflection can lead to exhausting the capacity of the process environment variables dictionary.
        /// That is why this feature is disabled by default.
        /// </para>
        /// </summary>
        /// <value>
        /// <c>true</c> if script location reflection is enabled otherwise, <c>false</c>.
        /// </value>
        static public bool EnableScriptLocationReflection { get; set; }

        static bool assemblyResolvingEnabled = true; //default value will be set from the static constructor to ensure the property setter execution

        /// <summary>
        /// Gets or sets the assembly sharing mode. If set to true all assemblies (including the host assembly itself)
        /// currently loaded to the host application AppDomain are automatically available/accessible from the script code.
        /// Default value is true.
        ///
        /// Sharing the same assembly set between the host application and the script require AssemblyResolvingEnabled to
        /// be enabled. Whenever SharesHostRefAssemblies is changed to true it automatically sets AssemblyResolvingEnabled to
        /// true as well.
        /// </summary>
        static public bool ShareHostRefAssemblies
        {
            get { return shareHostRefAssemblies; }
            set
            {
                if (shareHostRefAssemblies != value)
                {
                    shareHostRefAssemblies = value;
                    if (shareHostRefAssemblies)
                        AssemblyResolvingEnabled = true;
                }
            }
        }

        static bool shareHostRefAssemblies = true;
        static Assembly callingResolveEnabledAssembly;

        static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly retval = null;
            if (args.Name == "GetExecutingAssembly()")
                retval = callingResolveEnabledAssembly;//Assembly.GetExecutingAssembly();
            else if (args.Name == "GetEntryAssembly()")
                retval = Assembly.GetEntryAssembly();
            else
            {
                ExecuteOptions options = InitExecuteOptions(new ExecuteOptions(), CSScript.GlobalSettings, null, ref dummy);

                foreach (string dir in options.searchDirs)
                {
                    if ((retval = AssemblyResolver.ResolveAssembly(args.Name, dir, false)) != null)
                        break;
                }
            }
            return retval;
        }

        /// <summary>
        /// Settings object containing runtime settings, which controls script compilation/execution.
        /// This is Settings class essentially is a deserialized content of the CS-Script configuration file (css_config.xml).
        /// </summary>
        public static Settings GlobalSettings = Settings.Load(Environment.ExpandEnvironmentVariables(@"%CSSCRIPT_DIR%\css_config.xml"));

        /// <summary>
        /// Collection of all compiling results. Every time the script is compiled the compiling result is added to this collection regardless of
        /// the success or failure of the actual compilation.
        /// </summary>
        public static Dictionary<FileInfo, CompilingInfo> CompilingHistory = new Dictionary<FileInfo, CompilingInfo>();

        /// <summary>
        /// The hosting concurrency control.
        /// <list type="bullet">
        /// <item ><description>
        /// <para><c>ProcessScope</c></para>
        /// Mutex name is a derived from the hosting process id.
        /// Two calls to compile any script will be synchronized if the calls are made from the same host process.
        /// </description></item>
        /// <item ><description>
        /// <para><c>ScriptProcessScope</c></para>
        /// Mutex name is a derived from the combination of the script path and hosting process id.
        /// Two calls to compile the same script file will be synchronized but only if the calls are made from the same host process.
        /// </description></item>
        /// <item ><description>
        /// <para><c>ScriptSystemScope</c></para>
        /// Mutex name is a derived from the script path.
        /// Two calls to compile the same script file will be synchronized system wide.
        /// </description></item>
        /// </list>
        /// </summary>
        static public HostingConcurrencyControl HostingConcurrencyControl = HostingConcurrencyControl.ScriptSystemScope;

        /// <summary>
        /// The last script compilation result. Note, invoking CSScript.Compile/CSScript.Load may not trigger the actual compilation if script caching is
        /// engaged. Thus the <c>LastCompilingResult</c> value can be null.
        /// </summary>
        public static CompilingInfo LastCompilingResult = null;

        static bool keepCompilingHistory = false;

        /// <summary>
        /// Gets or sets a value indicating whether compiling history should be kept. The compilation results are stored in <see cref="CompilingHistory"></see>.
        /// </summary>
        /// <value>
        /// <c>true</c> if compiling history should be kept; otherwise, <c>false</c>.
        /// </value>
        public static bool KeepCompilingHistory
        {
            get { return keepCompilingHistory; }
            set { keepCompilingHistory = value; }
        }

        /// <summary>
        /// Invokes global (static) CSExecutor (C# script engine)
        /// </summary>
        /// <param name="print">Print delegate to be used (if not null) to handle script engine output (eg. compilation errors).</param>
        /// <param name="args">Script arguments.</param>
        static public void Execute(CSScriptLibrary.PrintDelegate print, string[] args)
        {
            lock (typeof(CSScript))
            {
                ExecuteOptions oldOptions = CSExecutor.options;
                try
                {
                    csscript.AppInfo.appName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                    csscript.CSExecutor exec = new csscript.CSExecutor();
                    exec.Rethrow = Rethrow;

                    InitExecuteOptions(CSExecutor.options, CSScript.GlobalSettings, null, ref dummy);

                    exec.Execute(args, new csscript.PrintDelegate(print != null ? print : new CSScriptLibrary.PrintDelegate(DefaultPrint)), null);
                }
                finally
                {
                    CSExecutor.options = oldOptions;
                }
            }
        }

        /// <summary>
        /// Invokes CSExecutor (C# script engine)
        /// </summary>
        /// <param name="print">Print delegate to be used (if not null) to handle script engine output (eg. compilation errors).</param>
        /// <param name="args">Script arguments.</param>
        /// <param name="rethrow">Flag, which indicated if script exceptions should be rethrowed by the script engine without any handling.</param>
        public void Execute(CSScriptLibrary.PrintDelegate print, string[] args, bool rethrow)
        {
            lock (typeof(CSScript))
            {
                ExecuteOptions oldOptions = CSExecutor.options;
                try
                {
                    AppInfo.appName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                    CSExecutor exec = new CSExecutor();
                    exec.Rethrow = rethrow;

                    InitExecuteOptions(CSExecutor.options, CSScript.GlobalSettings, null, ref dummy);

                    exec.Execute(args, new csscript.PrintDelegate(print != null ? print : new CSScriptLibrary.PrintDelegate(DefaultPrint)), null);
                }
                finally
                {
                    CSExecutor.options = oldOptions;
                }
            }
        }

        /// <summary>
        /// Compiles script code into assembly with CSExecutor
        /// </summary>
        /// <param name="scriptText">The script code to be compiled.</param>
        /// <param name="refAssemblies">The string array containing file nemes to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileCode(string scriptText, params string[] refAssemblies)
        {
            return CompileCode(scriptText, null, false, refAssemblies);
        }

        /// <summary>
        /// Compiles script code into assembly with CSExecutor
        /// </summary>
        /// <param name="scriptText">The script code to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileCode(string scriptText, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            lock (typeof(CSScript))
            {
                string tempFile = CSExecutor.GetScriptTempFile();
                try
                {
                    using (StreamWriter sw = new StreamWriter(tempFile))
                    {
                        sw.Write(scriptText);
                    }
                    return CompileFile(tempFile, assemblyFile, debugBuild, refAssemblies);
                }
                finally
                {
                    if (!debugBuild)
                        Utils.FileDelete(tempFile);
                    else
                        CSScript.NoteTempFile(tempFile);
                }
            }
        }

        /// <summary>
        /// Returns the name of the temporary file in the CSSCRIPT subfolder of Path.GetTempPath().
        /// </summary>
        /// <returns>Temporary file name.</returns>
        static public string GetScriptTempFile()
        {
            return CSExecutor.GetScriptTempFile();
        }

        /// <summary>
        /// Returns the name of the CSScript temporary folder.
        /// </summary>
        /// <returns>Temporary folder name.</returns>
        static public string GetScriptTempDir()
        {
            return CSExecutor.GetScriptTempDir();
        }

        /// <summary>
        /// Compiles multiple C# files into a single assembly with CSExecutor
        /// </summary>
        /// <param name="sourceFiles">Collection of the files to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileFiles(string[] sourceFiles, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            StringBuilder code = new StringBuilder();

            foreach (string item in sourceFiles)
                code.AppendFormat("//css_inc {0};{1}", item, Environment.NewLine);

            return CompileCode(code.ToString(), assemblyFile, debugBuild, refAssemblies);
        }

        /// <summary>
        /// Compiles multiple C# files into a single assembly with CSExecutor
        /// </summary>
        /// <param name="sourceFiles">Collection of the files to be compiled.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileFiles(string[] sourceFiles, params string[] refAssemblies)
        {
            return CompileFiles(sourceFiles, null, false, refAssemblies);
        }

        /// <summary>
        /// Compiles script file into assembly with CSExecutor
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        [Obsolete("This method is renamed to better align with Mono and Roslyn based CS-Script evaluators. Use CompileFile instead.", true)]
        static public string Compile(string scriptFile, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            return CompileFile(scriptFile, assemblyFile, debugBuild, refAssemblies);
        }

        /// <summary>
        /// Compiles script file into assembly with CSExecutor
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileFile(string scriptFile, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            return CompileWithConfig(scriptFile, assemblyFile, debugBuild, CSScript.GlobalSettings, null, refAssemblies);
        }

        /// <summary>
        /// Compiles script file into assembly (temporary file) with CSExecutor.
        /// This method is an equivalent of the CSScript.Compile(scriptFile, null, false);
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        [Obsolete("This method has been renamed to better align with Mono and Roslyn based CS-Script evaluators. Use CompileFile instead.", true)]
        static public string Compile(string scriptFile, params string[] refAssemblies)
        {
            return CompileFile(scriptFile, refAssemblies);
        }

        /// <summary>
        /// Compiles script file into assembly (temporary file) with CSExecutor.
        /// This method is an equivalent of the CSScript.Compile(scriptFile, null, false);
        /// </summary>
        /// <param name="scriptFile">The path to script file to be compiled.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileFile(string scriptFile, params string[] refAssemblies)
        {
            return CompileFile(scriptFile, null, false, refAssemblies);
        }

        /// <summary>
        /// Compiles script file into assembly with CSExecutor. Uses specified config file to load script engine settings.
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="cssConfigFile">The name of CS-Script configuration file. If null the default config file will be used (appDir/css_config.xml).</param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileWithConfig(string scriptFile, string assemblyFile, bool debugBuild, string cssConfigFile)
        {
            return CompileWithConfig(scriptFile, assemblyFile, debugBuild, cssConfigFile, null, null);
        }

        /// <summary>
        /// Compiles script file into assembly with CSExecutor. Uses specified config file to load script engine settings and compiler specific options.
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="cssConfigFile">The name of CS-Script configuration file. If null the default config file will be used (appDir/css_config.xml).</param>
        /// <param name="compilerOptions">The string value to be passed directly to the language compiler. </param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileWithConfig(string scriptFile, string assemblyFile, bool debugBuild, string cssConfigFile, string compilerOptions, params string[] refAssemblies)
        {
            lock (typeof(CSScript))
            {
                Settings settings = Settings.Load(ResolveConfigFilePath(cssConfigFile));
                if (settings == null)
                    throw new ApplicationException("The configuration file \"" + cssConfigFile + "\" cannot be loaded");

                return CompileWithConfig(scriptFile, assemblyFile, debugBuild, settings, compilerOptions, refAssemblies);
            }
        }

        static string ResolveConfigFilePath(string cssConfigFile)
        {
            return cssConfigFile != null ? cssConfigFile : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "css_config.xml");
        }

        static string GetCompilerLockName_old(string script, bool optimisticConcurrencyModel)
        {
            if (optimisticConcurrencyModel)
            {
                return Process.GetCurrentProcess().Id.ToString(); //less aggressive lock
            }
            else
            {
                return string.Format("{0}.{1}", Process.GetCurrentProcess().Id, CSSUtils.GetHashCodeEx(script));
            }
        }

        static string GetCompilerLockName(string script)
        {
            var scriptPath = Path.GetFullPath(script);

            if (!Runtime.IsLinux)
                scriptPath = scriptPath.ToLower();

            switch (HostingConcurrencyControl)
            {
                // Mutex name is a derived from the hosting process id.
                // Two calls to compile any script will be synchronized if the calls are made from the same host process.
                case HostingConcurrencyControl.ProcessScope:
                    return "cs-script." + Process.GetCurrentProcess().Id;

                // Mutex name is a derived from the combination of the script path and hosting process id.
                // Two calls to compile the same script file will be synchronized but only if the calls are made from the same host process.
                case HostingConcurrencyControl.ScriptProcessScope:
                    return string.Format("cs-script.{0}.{1}", Process.GetCurrentProcess().Id, CSSUtils.GetHashCodeEx(scriptPath));

                // Mutex name is a derived from the script path.
                // Two calls to compile the same script file will be synchronized system wide.
                case HostingConcurrencyControl.ScriptSystemScope:
                default:
                    return "cs-script." + CSSUtils.GetHashCodeEx(scriptPath);
            }
        }

        /// <summary>
        /// Creates the compiler lock object (<see cref="System.Threading.Mutex"/>). The Mutex object is now initially owned.
        /// <para>This object is to be used for the access synchronization to the compiled script file and it can be useful for the
        /// tasks like cache purging or explicit script recompilation.</para>
        /// <para>The optimisticConcurrencyModel has the same meaning as <see cref="csscript.Settings.OptimisticConcurrencyModel"/>.
        /// And it is to be used to control the concurrency scope.</para>
        /// </summary>
        /// <param name="compiledScriptFile">The script file.</param>
        /// <param name="optimisticConcurrencyModel">if set to <c>true</c> the operation is thread-safe within the current process.
        /// Otherwise the operation is thread-safe system wide.</param>
        /// <returns></returns>
        [Obsolete("This method is no longer supported use CreateCompilerLock(string compiledScriptFile) instead, which offers more comprehensive concurrency control.")]
        static public Mutex CreateCompilerLock(string compiledScriptFile, bool optimisticConcurrencyModel)
        {
            return new Mutex(false, GetCompilerLockName(compiledScriptFile));
        }

        /// <summary>
        /// Creates the compiler lock object (<see cref="System.Threading.Mutex"/>). The Mutex object is now initially owned.
        /// <para>This object is to be used for the access synchronization to the compiled script file and it can be useful for the
        /// tasks like cache purging or explicit script recompilation.</para>
        /// <para>The concurrency/lock scope is controlled by <see cref="CSScriptLibrary.CSScript.HostingConcurrencyControl"/>.
        /// And it is to be used to control the concurrency scope.</para>
        /// </summary>
        /// <param name="compiledScriptFile">The script file.</param>
        /// <returns></returns>
        static public Mutex CreateCompilerLock(string compiledScriptFile)
        {
            return new Mutex(false, GetCompilerLockName(compiledScriptFile));
        }

        /// <summary>
        /// Compiles script file into assembly with CSExecutor. Uses script engine settings object and compiler specific options.
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="scriptSettings">The script engine Settings object.</param>
        /// <param name="compilerOptions">The string value to be passed directly to the language compiler.  </param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly file name.</returns>
        static public string CompileWithConfig(string scriptFile, string assemblyFile, bool debugBuild, Settings scriptSettings, string compilerOptions, params string[] refAssemblies)
        {
            CompilingInfo compilingInfo;
            return CompileWithConfig(scriptFile, assemblyFile, debugBuild, scriptSettings, compilerOptions, out compilingInfo, refAssemblies);
        }

        /// <summary>
        /// Compiles script file into assembly with CSExecutor. Uses script engine settings object and compiler specific options.
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="scriptSettings">The script engine Settings object.</param>
        /// <param name="compilerOptions">The string value to be passed directly to the language compiler.</param>
        /// <param name="compilingInfo">The compiling information. Populated with the compilation context.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script.</param>
        /// <returns>
        /// Compiled assembly file name.
        /// </returns>
        static public string CompileWithConfig(string scriptFile, string assemblyFile, bool debugBuild, Settings scriptSettings, string compilerOptions, out CompilingInfo compilingInfo, params string[] refAssemblies)
        {
            lock (typeof(CSScript))
            {
                using (Mutex fileLock = new Mutex(false, GetCompilerLockName(assemblyFile ?? scriptFile)))
                {
                    compilingInfo = null;
                    ExecuteOptions oldOptions = CSExecutor.options;
                    try
                    {
                        int start = Environment.TickCount;
                        //Infinite timeout is not good choice here as it may block forever but continuing while the file is still locked will
                        //throw a nice informative exception.
                        fileLock.WaitOne(5000, false); //let other thread/process (if any) to finish loading/compiling the same file; 5 seconds should be enough, if you need more use more sophisticated synchronization

                        //Trace.WriteLine(">>>  Waited  " + (Environment.TickCount - start));

                        CSExecutor exec = new csscript.CSExecutor();
                        exec.Rethrow = true;

                        InitExecuteOptions(CSExecutor.options, scriptSettings, compilerOptions, ref scriptFile);
                        CSExecutor.options.DBG = debugBuild;

                        if (refAssemblies != null && refAssemblies.Length != 0)
                        {
                            string dir;
                            foreach (string file in refAssemblies)
                            {
                                dir = Path.GetDirectoryName(file);
                                CSExecutor.options.AddSearchDir(dir, Settings.cmd_dirs_section); //settings used by Compiler
                                CSScript.GlobalSettings.AddSearchDir(dir); //settings used by AsmHelper
                            }
                            CSExecutor.options.refAssemblies = refAssemblies;
                        }

                        if (CacheEnabled)
                        {
                            if (assemblyFile != null)
                            {
                                if (!IsOutOfDateAlgorithm(scriptFile, assemblyFile))
                                    return assemblyFile;
                            }
                            else
                            {
                                string path = GetCachedScriptAssemblyFile(scriptFile);
                                if (path != null)
                                    return path;
                            }
                        }

                        try
                        {
                            return exec.Compile(scriptFile, assemblyFile, debugBuild);
                        }
                        finally
                        {
                            compilingInfo = LastCompilingResult = exec.LastCompileResult;
                            if (KeepCompilingHistory)
                                CompilingHistory.Add(new FileInfo(scriptFile), exec.LastCompileResult);
                        }
                    }
                    finally
                    {
                        CSExecutor.options = oldOptions;
                        try { fileLock.ReleaseMutex(); }
                        catch { }
                    }
                }
            }
        }

        static ExecuteOptions InitExecuteOptions(ExecuteOptions options, Settings scriptSettings, string compilerOptions, ref string scriptFile)
        {
            Settings settings = (scriptSettings == null ? CSScript.GlobalSettings : scriptSettings);

            options.altCompiler = settings.ExpandUseAlternativeCompiler();
            options.roslynDir = Environment.ExpandEnvironmentVariables(settings.RoslynDir);
            options.compilerOptions = compilerOptions != null ? compilerOptions : "";
            options.apartmentState = settings.DefaultApartmentState;
            options.InjectScriptAssemblyAttribute = settings.InjectScriptAssemblyAttribute;
            options.resolveAutogenFilesRefs = settings.ResolveAutogenFilesRefs;
            options.reportDetailedErrorInfo = settings.ReportDetailedErrorInfo;
            options.cleanupShellCommand = settings.CleanupShellCommand;
            options.customHashing = settings.CustomHashing;
            options.enableDbgPrint = settings.EnableDbgPrint;
            options.inMemoryAsm = settings.InMemoryAssembly;
            //options.DLLExtension = settings.DLLExtension;
            options.hideCompilerWarnings = settings.HideCompilerWarnings;
            options.TargetFramework = settings.TargetFramework;
            options.defaultRefAssemblies = settings.ExpandDefaultRefAssemblies();
            options.doCleanupAfterNumberOfRuns = settings.DoCleanupAfterNumberOfRuns;
            options.useCompiled = CSScript.CacheEnabled;
            options.useSmartCaching = CSScript.CacheEnabled;
            options.useSurrogateHostingProcess = false; //regardless of the input useSurrogateHostingProcess is not appropriate for teh hosting scenarios, so set it to 'false'

            ArrayList dirs = new ArrayList();

            options.shareHostRefAssemblies = ShareHostRefAssemblies;
            if (options.shareHostRefAssemblies)
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    try
                    {
                        string location = Utils.Location(asm);
                        if (location == "" || !File.Exists(location))
                            continue;

                        dirs.Add(Path.GetDirectoryName(location));
                    }
                    catch
                    {
                        //Under ASP.NET some assemblies do not have location (e.g. dynamically built/emitted assemblies)
                        //in such case NotSupportedException will be raised

                        //In fact ignore all exceptions as we should continue if for whatever reason assembly the location cannot be obtained
                    }
            }

            string libDir = Environment.ExpandEnvironmentVariables("%CSSCRIPT_DIR%" + Path.DirectorySeparatorChar + "lib");
            if (!libDir.StartsWith("%"))
                dirs.Add(libDir);

            if (settings != null)
                dirs.AddRange(Environment.ExpandEnvironmentVariables(settings.SearchDirs).Split(",;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));

            if (scriptFile != "")
            {
                scriptFile = FileParser.ResolveFile(scriptFile, (string[])dirs.ToArray(typeof(string))); //to handle the case when the script file is specified by file name only
                dirs.Add(Path.GetDirectoryName(scriptFile));
            }

            options.searchDirs = RemovePathDuplicates((string[])dirs.ToArray(typeof(string)));

            options.scriptFileName = scriptFile;

            return options;
        }

        /// <summary>
        /// Surrounds the method implementation code into a class and compiles it code into assembly with CSExecutor and loads it in current AppDomain.
        /// The most convenient way of using dynamic methods is to declare them as static methods. In this case they can be invoked with wild card character
        /// as a class name (e.g. asmHelper.Invoke("*.SayHello")). Otherwise you will need to instantiate class "DyamicClass.Script" in order to call dynamic method.
        ///
        /// You can have multiple methods implementations in the single methodCode. Also you can specify namespaces at the beginning of the code:
        ///
        /// CSScript.LoadMethod(
        ///     @"using System.Windows.Forms;
        ///
        ///     public static void SayHello(string greeting)
        ///     {
        ///         MessageBoxSayHello(greeting);
        ///         ConsoleSayHello(greeting);
        ///     }
        ///     public static void MessageBoxSayHello(string greeting)
        ///     {
        ///         MessageBox.Show(greeting);
        ///     }
        ///     public static void ConsoleSayHello(string greeting)
        ///     {
        ///         Console.WriteLine(greeting);
        ///     }");
        /// </summary>
        /// <param name="methodCode">The C# code, containing method implementation.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        static public Assembly LoadMethod(string methodCode, params string[] refAssemblies)
        {
            return LoadMethod(methodCode, null, false, refAssemblies);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns instance of <c>T</c> delegate for the first method in the auto-generated class.</para>
        /// </summary>
        ///  <example>The following is the simple example of the interface alignment:
        /// <code>
        /// var Product = CSScript.LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                         @"int Product(int a, int b)
        ///                           {
        ///                               return a * b;
        ///                           }");
        ///
        /// int result = Product(3, 2);
        /// </code>
        /// </example>
        /// <param name="methodCode">The C# code.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Instance of <c>T</c> delegate.</returns>
        public static T LoadDelegate<T>(string methodCode, string assemblyFile, bool debugBuild, params string[] refAssemblies) where T : class
        {
            lock (LoadAutoCodeSynch)
            {
                string code = WrapMethodToAutoClass(methodCode, true);
                Assembly asm = LoadCode(code, assemblyFile, debugBuild, refAssemblies);
                MethodInfo method = asm.GetType("Scripting.DynamicClass").GetMethods()[0];
                return Delegate.CreateDelegate(typeof(T), method) as T;
            }
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns instance of <see cref="CSScriptLibrary.MethodDelegate"/> for the first method in the auto-generated class.</para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        /// <code>
        /// var SayHello = CSScript.CreateAction(@"void SayHello(string greeting)
        ///                                        {
        ///                                            Console.WriteLine(greeting);
        ///                                        }");
        /// SayHello("Hello World!");
        /// </code>
        /// </example>
        /// <param name="methodCode">The C# code.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Instance of <see cref="CSScriptLibrary.MethodDelegate"/>.</returns>
        public static MethodDelegate CreateAction(string methodCode, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            lock (LoadAutoCodeSynch)
            {
                return LoadMethod(methodCode, assemblyFile, debugBuild, refAssemblies).GetStaticMethod();
            }
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns instance of <see cref="CSScriptLibrary.MethodDelegate"/> for the first method in the auto-generated class.</para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        /// <code>
        /// var product = CSScript.CreateFunc&lt;intgt;(@"int Product(int a, int b)
        ///                                          {
        ///                                              return a * b;
        ///                                          }");
        /// int result = product(3, 4);
        /// </code>
        /// </example>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <param name="methodCode">The C# code.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Instance of <see cref="CSScriptLibrary.MethodDelegate"/>.</returns>
        public static MethodDelegate<T> CreateFunc<T>(string methodCode, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            lock (LoadAutoCodeSynch)
            {
                return LoadMethod(methodCode, assemblyFile, debugBuild, refAssemblies).GetStaticMethod<T>();
            }
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns instance of <c>T</c> delegate for the first method in the auto-generated class.</para>
        /// </summary>
        ///  <example>The following is the simple example of the interface alignment:
        /// <code>
        /// var Product = CSScript.LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                         @"int Product(int a, int b)
        ///                           {
        ///                               return a * b;
        ///                           }");
        ///
        /// int result = Product(3, 2);
        /// </code>
        /// </example>
        /// <param name="methodCode">The C# code.</param>
        /// <returns>Instance of <c>T</c> delegate.</returns>
        public static T LoadDelegate<T>(string methodCode) where T : class
        {
            return LoadDelegate<T>(methodCode, null, false);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns instance of <see cref="CSScriptLibrary.MethodDelegate"/> for the first method in the auto-generated class.</para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        /// <code>
        /// var SayHello = CSScript.CreateAction(@"void SayHello(string greeting)
        ///                                        {
        ///                                            Console.WriteLine(greeting);
        ///                                        }");
        /// SayHello("Hello World!");
        /// </code>
        /// </example>
        /// <param name="methodCode">The C# code.</param>
        /// <returns>Instance of <see cref="CSScriptLibrary.MethodDelegate"/>.</returns>
        public static MethodDelegate CreateAction(string methodCode)
        {
            lock (LoadAutoCodeSynch)
            {
                string code = WrapMethodToAutoClass(methodCode, true);
                return LoadCode(code, null, false).GetStaticMethod();
            }
        }

        /// <summary>
        /// Stop any running instances of the compiler server if any.
        /// <para>
        /// Stopping is needed in order to prevent any problems with copying/moving CS-Script binary files (e.g. Roslyn compilers).
        /// Servers restart automatically on any attempt to compile any C#/VB.NET code by any client (e.g. Visual Studio, MSBuild, CS-Script).
        /// </para>
        /// </summary>
        public static void StopVBCSCompilers()
        {
            lock (LoadAutoCodeSynch)
            {
                CSSUtils.StopVBCSCompilers();
            }
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns instance of <see cref="CSScriptLibrary.MethodDelegate"/> for the first method in the auto-generated class.</para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        /// <code>
        /// var product = CSScript.CreateFunc&lt;intgt;(@"int Product(int a, int b)
        ///                                          {
        ///                                              return a * b;
        ///                                          }");
        /// int result = product(3, 4);
        /// </code>
        /// </example>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <param name="methodCode">The C# code.</param>
        /// <returns>Instance of <see cref="CSScriptLibrary.MethodDelegate"/>.</returns>
        public static MethodDelegate<T> CreateFunc<T>(string methodCode)
        {
            lock (LoadAutoCodeSynch)
            {
                string code = WrapMethodToAutoClass(methodCode, true);
                return LoadCode(code, null, false).GetStaticMethod<T>();
            }
        }

        static object LoadAutoCodeSynch = new object();

        /// <summary>
        /// Surrounds the method implementation code into a class and compiles it code into
        /// assembly with CSExecutor and loads it in current AppDomain. The most convenient way of
        /// using dynamic methods is to declare them as static methods. In this case they can be
        /// invoked with wild card character as a class name (e.g. asmHelper.Invoke("*.SayHello")).
        /// Otherwise you will need to instantiate class "DyamicClass.Script" in order to call dynamic method.
        ///
        /// You can have multiple methods implementations in the single methodCode. Also you can specify namespaces at the beginning of the code:
        /// <code>
        /// CSScript.LoadMethod(
        ///     @"using System.Windows.Forms;
        ///
        ///     public static void SayHello(string greeting)
        ///     {
        ///         MessageBoxSayHello(greeting);
        ///         ConsoleSayHello(greeting);
        ///     }
        ///     public static void MessageBoxSayHello(string greeting)
        ///     {
        ///         MessageBox.Show(greeting);
        ///     }
        ///     public static void ConsoleSayHello(string greeting)
        ///     {
        ///         Console.WriteLine(greeting);
        ///     }");
        /// </code>
        /// </summary>
        /// <param name="methodCode">The C# code, containing method implementation.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        static public Assembly LoadMethod(string methodCode, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            lock (LoadAutoCodeSynch)
            {
                string code = WrapMethodToAutoClass(methodCode, false);
                return LoadCode(code, assemblyFile, debugBuild, refAssemblies);
            }
        }

        static internal string WrapMethodToAutoClass(string methodCode, bool injectStatic)
        {
            return WrapMethodToAutoClass(methodCode, injectStatic, true, null);
        }

        static internal string ClasslessScriptTypeName = "DynamicClass";
#if !net35 && !net1

        static internal string WrapMethodToAutoClass(string methodCode, bool injectStatic, bool injectNamespace, string inheritFrom = null)
#else
        static internal string WrapMethodToAutoClass(string methodCode, bool injectStatic, bool injectNamespace, string inheritFrom)
#endif
        {
            StringBuilder code = new StringBuilder(4096);
            code.AppendLine("//Auto-generated file");
            code.AppendLine("using System;");

            bool headerProcessed = false;

            string line;

            using (StringReader sr = new StringReader(methodCode))
                while ((line = sr.ReadLine()) != null)
                {
                    if (!headerProcessed && !line.TrimStart().StartsWith("using ")) //not using...; statement of the file header
                    {
                        string trimmed = line.Trim();
                        if (!trimmed.StartsWith("//") && trimmed != "") //not comments or empty line
                        {
                            headerProcessed = true;

                            if (injectNamespace)
                            {
                                code.AppendLine("namespace Scripting");
                                code.AppendLine("{");
                            }

                            if (inheritFrom != null)
                                code.AppendLine("   public class " + ClasslessScriptTypeName + " : " + inheritFrom);
                            else
                                code.AppendLine("   public class " + ClasslessScriptTypeName);

                            code.AppendLine("   {");
                            string[] tokens = line.Split("\t ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);

                            if (injectStatic)
                            {
                                if (!tokens.Contains("static")) //unsafe public static
                                    code.AppendLine("   static");
                            }

                            if (!tokens.Contains("public"))
                                code.AppendLine("   public");
                        }
                    }

                    code.AppendLine(line);
                }

            code.AppendLine("   }");
            if (injectNamespace)
                code.AppendLine("}");

            return code.ToString();
        }

        /// <summary>
        /// Compiles script code from the specified file into assembly with CSExecutor and loads it in current AppDomain.
        /// <para>This method is a logical equivalent of the corresponding <c>LoadCode</c> method except the code is
        /// not specified as a call argument but read from the file instead.</para>
        /// <para>It is recommended to use LoadFrom method instead as it is more straight forward and arguably faster.
        /// Use LoadCodeFrom only if you indeed want to disassociate your script code from the script file or
        /// if the original location of the source file some how is incompatible with the actual C# compiler.</para>
        /// </summary>
        /// <param name="scriptFile">The script file.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        [Obsolete("This method is obsolete due to the low value/popularity. Use CSScript.Load instead. " +
                  "Alternatively you can achieve the same functionality with LoadCode(File.ReadAllText(scriptFile), refAssemblies).", true)]
        static public Assembly LoadCodeFrom(string scriptFile, params string[] refAssemblies)
        {
            return LoadCode(File.ReadAllText(scriptFile), refAssemblies);
        }

        /// <summary>
        /// Compiles code from the specified files into assembly with CSExecutor and loads it in current AppDomain.
        /// <para>This method is a logical equivalent of the corresponding <c>LoadCode</c> method except the code is
        /// not specified as a call argument but read from the file instead.</para>
        /// </summary>
        /// <param name="sourceFiles">The source files to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        static public Assembly LoadFiles(string[] sourceFiles, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            return Assembly.LoadFrom(CompileFiles(sourceFiles, assemblyFile, debugBuild, refAssemblies));
        }

        /// <summary>
        /// Compiles code from the specified files into assembly with CSExecutor and loads it in current AppDomain.
        /// <para>This method is a logical equivalent of the corresponding <c>LoadCode</c> method except the code is
        /// not specified as a call argument but read from the file instead.</para>
        /// </summary>
        /// <param name="sourceFiles">The source files to be compiled.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        static public Assembly LoadFiles(string[] sourceFiles, params string[] refAssemblies)
        {
            return Assembly.LoadFrom(CompileFiles(sourceFiles, refAssemblies));
        }

        /// <summary>
        /// Compiles script code from the specified file into assembly with CSExecutor and loads it in current AppDomain.
        /// <para>This method is a logical equivalent of the corresponding <c>LoadCode</c> method except the code is
        /// not specified as a call argument but read from the file instead.</para>
        /// <para>It is recommended to use LoadFrom method instead as it is more straight forward and arguably faster.
        /// Use LoadCodeFrom only if you indeed want to disassociate your script code from the script file or
        /// if the original location of the source file some how is incompatible with the actual C# compiler.</para>
        /// </summary>
        /// <param name="scriptFile">The script file.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        [Obsolete("This method is obsolete due to the low value/popularity. Use CSScript.Load instead. " +
                  "Alternatively you can achieve the same functionality with LoadCode(File.ReadAllText(scriptFile), refAssemblies).", true)]
        static public Assembly LoadCodeFrom(string scriptFile, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            return LoadCode(File.ReadAllText(scriptFile), assemblyFile, debugBuild, refAssemblies);
        }

        /// <summary>
        /// Compiles script code from the specified file into assembly with CSExecutor and loads it in current AppDomain.
        /// </summary>
        /// <para>This method is a logical equivalent of the corresponding <c>LoadCode</c> method except the code is
        /// not specified as a call argument but read from the file instead.</para>
        /// <para>It is recommended to use LoadFrom method instead as it is more straight forward and arguably faster.
        /// Use LoadCodeFrom only if you indeed want to disassociate your script code from the script file or
        /// if the original location of the source file some how is incompatible with the actual C# compiler.</para>
        /// <param name="scriptFile">The script file.</param>
        /// <param name="tempFileExtension">The file extension of the temporary file to hold script code during compilation. This parameter may be
        /// needed if custom CS-Script compilers rely on file extension to identify the script syntax.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        [Obsolete("This method is obsolete due to the low value/popularity. Use CSScript.Load instead. " +
                  "Alternatively you can achieve the same functionality with LoadCode(File.ReadAllText(scriptFile), refAssemblies).", true)]
        static public Assembly LoadCodeFrom(string scriptFile, string tempFileExtension, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            return LoadCode(File.ReadAllText(scriptFile), tempFileExtension, assemblyFile, debugBuild, refAssemblies);
        }

        /// <summary>
        /// Compiles script code into assembly with CSExecutor and loads it in current AppDomain.
        /// </summary>
        /// <param name="scriptText">The script code to be compiled.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        static public Assembly LoadCode(string scriptText, params string[] refAssemblies)
        {
            return LoadCode(scriptText, null, false, refAssemblies);
        }

        /// <summary>
        /// Compiles script code into assembly with CSExecutor and loads it in current AppDomain.
        /// </summary>
        /// <param name="scriptText">The script code to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        static public Assembly LoadCode(string scriptText, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            return LoadCode(scriptText, "", assemblyFile, debugBuild, refAssemblies);
        }

#if !net1
        static Dictionary<UInt32, string> dynamicScriptsAssemblies = new Dictionary<UInt32, string>();
#else
        static Hashtable dynamicScriptsAssemblies = new Hashtable();
#endif

        /// <summary>
        /// Compiles script code into assembly with CSExecutor and loads it in current AppDomain.
        /// </summary>
        /// <param name="scriptText">The script code to be compiled.</param>
        /// <param name="tempFileExtension">The file extension of the temporary file to hold script code during compilation. This parameter may be
        /// needed if custom CS-Script compilers rely on file extension to identify the script syntax.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled assembly.</returns>
        static public Assembly LoadCode(string scriptText, string tempFileExtension, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            lock (typeof(CSScript))
            {
                ScheduleCleanup();

                UInt32 scriptTextCRC = 0;
                if (CacheEnabled)
                {
                    scriptTextCRC = Crc32.Compute(Encoding.UTF8.GetBytes(scriptText));
                    if (dynamicScriptsAssemblies.ContainsKey(scriptTextCRC))
                        try
                        {
                            var location = dynamicScriptsAssemblies[scriptTextCRC];
                            if (location.StartsWith("inmem:"))
                            {
                                string name = location.Substring("inmem:".Length);
                                return AppDomain.CurrentDomain.GetAssemblies().
                                       SingleOrDefault(assembly => assembly.FullName == name);
                            }
                            else
                                return Assembly.LoadFrom(location);
                        }
                        catch
                        {
                            Trace.WriteLine("Cannot use cache...");
                        }
                }

                string tempFile = CSExecutor.GetScriptTempFile("dynamic");
                if (tempFileExtension != null && tempFileExtension != "")
                    tempFile = Path.ChangeExtension(tempFile, tempFileExtension);

                try
                {
                    Mutex fileLock = new Mutex(false, tempFile.Replace(Path.DirectorySeparatorChar, '|').ToLower());
                    fileLock.WaitOne(1); //do not release mutex. The file may be needed to be to locked until the
                    //host process exits (e.g. debugging). Thus the mutex will be released by OS when the process is terminated

                    using (StreamWriter sw = new StreamWriter(tempFile))
                    {
                        sw.Write(scriptText);
                    }

                    Assembly asm = Load(tempFile, assemblyFile, debugBuild, refAssemblies);

                    string location = asm.Location();

                    if (CacheEnabled)
                    {
                        if (String.IsNullOrEmpty(location))
                        {
                            if (Environment.GetEnvironmentVariable("CSS_DISABLE_INMEM_ASM_CACHING") != "true")
                                location = "inmem:" + asm.FullName;
                        }

                        if (!string.IsNullOrEmpty(location))
                        {
                            if (dynamicScriptsAssemblies.ContainsKey(scriptTextCRC))
                                dynamicScriptsAssemblies[scriptTextCRC] = location;
                            else
                                dynamicScriptsAssemblies.Add(scriptTextCRC, location);
                        }
                    }
                    return asm;
                }
                finally
                {
                    if (!debugBuild)
                        Utils.FileDelete(tempFile);
                    else
                        tempFiles.Add(tempFile);
                }
            }
        }

        internal static void NoteTempFile(string file)
        {
            if (tempFiles == null)
            {
                tempFiles = new ArrayList();
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnApplicationExit);
            }
            tempFiles.Add(file);
        }

        static void ScheduleCleanup()
        {
            if (tempFiles == null)
            {
                tempFiles = new ArrayList();

                //Note: ApplicationExit will not be called if this library is hosted by an application.
                //Thus CS-Script periodical cleanup will take care of the temp files
                //Application.ApplicationExit += new EventHandler(OnApplicationExit); //will not be available on .NET CE
                //AppDomain.CurrentDomain.DomainUnload += new EventHandler(OnApplicationExit);
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnApplicationExit);

                //start background cleaning just in case if previous session crashed
                //ThreadPool.QueueUserWorkItem((x) => CleanDynamicSources());
            }
        }

        static ArrayList tempFiles;

        static void OnApplicationExit(object sender, EventArgs e)
        {
            Cleanup();
        }

        static void Cleanup()
        {
            if (tempFiles != null)
                foreach (string file in tempFiles)
                {
                    Utils.FileDelete(file);
                }

            CleanupDynamicSources();
        }

        /// <summary>
        /// Cleans up all abandoned dynamic C# sources (temp files) generated by LodCode*() API methods. You don't need to use this
        /// method as it is scheduled for execution at the end of the application.
        /// <para>
        /// However automatic cleanup may fail under certain circumstances as it is scheduled by the
        /// AppDomain.CurrentDomain.ProcessExit event, which is not guaranteed to be always executed.
        /// In such cases or if you want to clear the directory prior the execution you can call <c>CSScript.CleanupDynamicSources()</c> explicitly.
        /// </para>
        /// </summary>
        public static void CleanupDynamicSources()
        {
            string dir = Path.Combine(CSExecutor.GetScriptTempDir(), "dynamic");
            if (Environment.GetEnvironmentVariable("CSScript_Suspend_Housekeeping") == null)
            {
                Utils.CleanUnusedTmpFiles(dir, "*????????-????-????-????-????????????.???", true);
                string cachForDynamicFiles = CSExecutor.GetCacheDirectory(Path.Combine(dir, "dummy.cs"));
                Utils.CleanUnusedTmpFiles(cachForDynamicFiles, "*????????-????-????-????-????????????.???*", true);
            }
        }

        /// <summary>
        /// Compiles script file into assembly with CSExecutor and loads it in current AppDomain
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled/Loaded assembly.</returns>
        [Obsolete("This method has been renamed to better align with Mono and Roslyn based CS-Script evaluators. Use LoadFile instead.", false)]
        static public Assembly Load(string scriptFile, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            return LoadWithConfig(scriptFile, assemblyFile, debugBuild, null, "", refAssemblies);
        }

        /// <summary>
        /// Compiles script file into assembly with CSExecutor and loads it in current AppDomain
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled/Loaded assembly.</returns>
        static public Assembly LoadFile(string scriptFile, string assemblyFile, bool debugBuild, params string[] refAssemblies)
        {
            return LoadWithConfig(scriptFile, assemblyFile, debugBuild, null, "", refAssemblies);
        }

        //static object threadSynch = new object();
        /// <summary>
        /// Compiles script file into assembly with CSExecutor and loads it in current AppDomain
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="assemblyFile">The path of the compiled assembly to be created. If set to null a temporary file name will be used.</param>
        /// <param name="debugBuild">'true' if debug information should be included in assembly; otherwise, 'false'.</param>
        /// <param name="scriptSettings">The script engine Settings object. You can pass null to load <c>CSScript.GlobalSettings</c>. </param>
        /// <param name="compilerOptions">The string value to be passed directly to the language compiler.  </param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled/Loaded assembly.</returns>
        static public Assembly LoadWithConfig(string scriptFile, string assemblyFile, bool debugBuild, Settings scriptSettings, string compilerOptions, params string[] refAssemblies)
        {
            lock (typeof(CSScript))
            {
                using (Mutex fileLock = new Mutex(false, GetCompilerLockName(assemblyFile ?? scriptFile)))
                {
                    ExecuteOptions oldOptions = CSExecutor.options;

                    try
                    {
                        int start = Environment.TickCount;
                        //Infinite timeout is not good choice here as it may block forever but continuing while the file is still locked will
                        //throw a nice informative exception.
                        fileLock.WaitOne(5000, false); //let other thread/process (if any) to finish loading/compiling the same file; 2 seconds should be enough, if you need more use more sophisticated synchronization

                        //Trace.WriteLine(">>>  Waited  " + (Environment.TickCount - start));

                        CSExecutor exec = new CSExecutor();
                        exec.Rethrow = true;

                        InitExecuteOptions(CSExecutor.options, scriptSettings, compilerOptions, ref scriptFile);
                        CSExecutor.options.DBG = debugBuild;

                        if (refAssemblies != null && refAssemblies.Length != 0)
                        {
                            string dir;
                            foreach (string file in refAssemblies)
                            {
                                dir = Path.GetDirectoryName(file);
                                if (!string.IsNullOrEmpty(dir))
                                {
                                    CSExecutor.options.AddSearchDir(dir, Settings.code_dirs_section); //settings used by Compiler
                                    CSScript.GlobalSettings.AddSearchDir(dir); //settings used by AsmHelper
                                }
                            }
                            CSExecutor.options.refAssemblies = refAssemblies;
                        }

                        Assembly retval = null;

                        if (CacheEnabled)
                        {
                            if (assemblyFile != null)
                            {
                                if (!IsOutOfDateAlgorithm(scriptFile, assemblyFile))
                                {
                                    if (ExecuteOptions.options.inMemoryAsm)
                                        retval = LoadInMemory(assemblyFile, debugBuild);
                                    else
                                        retval = Assembly.LoadFrom(assemblyFile);
                                }
                            }
                            else
                            {
                                retval = GetLoadedCachedScriptAssembly(scriptFile);
                            }
                        }

                        if (retval == null)
                        {
                            string outputFile = exec.Compile(scriptFile, assemblyFile, debugBuild);

                            LastCompilingResult = exec.LastCompileResult;

                            if (KeepCompilingHistory)
                                CompilingHistory.Add(new FileInfo(scriptFile), exec.LastCompileResult);

                            if (ExecuteOptions.options.inMemoryAsm)
                                retval = LoadInMemory(outputFile, debugBuild);
                            else
                                retval = Assembly.LoadFrom(outputFile);

                            if (retval != null)
                                scriptCache.Add(new LoadedScript(scriptFile, retval));

                            RemoteExecutor.SetScriptReflection(retval, outputFile, CSScript.EnableScriptLocationReflection);
                        }
                        return retval;
                    }
                    finally
                    {
                        try { fileLock.ReleaseMutex(); }
                        catch { }
                        CSExecutor.options = oldOptions;
                    }
                }
            }
        }

        static Assembly LoadInMemory(string asmFile, bool debugBuild)
        {
            //Load(byte[]) does not lock the assembly file as LoadFrom(filename) does
            byte[] data = new byte[0];
            using (FileStream fs = new FileStream(asmFile, FileMode.Open))
            {
                data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
            }

            string dbg = Utils.DbgFileOf(asmFile);
            if (debugBuild && File.Exists(dbg))
            {
                byte[] dbgData = new byte[0];
                using (FileStream fsDbg = new FileStream(dbg, FileMode.Open))
                {
                    dbgData = new byte[fsDbg.Length];
                    fsDbg.Read(dbgData, 0, dbgData.Length);
                }
                return Assembly.Load(data, dbgData);
            }
            else
                return Assembly.Load(data);
        }

        /// <summary>
        /// Compiles script file into assembly (temporary file) with CSExecutor and loads it in current AppDomain.
        /// This method is an equivalent of the CSScript.Load(scriptFile, null, false);
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <returns>Compiled/Loaded assembly.</returns>
        [Obsolete("This method has been renamed to better align with Mono and Roslyn based CS-Script evaluators. Use LoadFile instead.", false)]
        static public Assembly Load(string scriptFile)
        {
            return Load(scriptFile, null, false, null);
        }

        /// <summary>
        /// Compiles script file into assembly (temporary file) with CSExecutor and loads it in current AppDomain.
        /// This method is an equivalent of the CSScript.Load(scriptFile, null, false);
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <returns>Compiled/Loaded assembly.</returns>
        static public Assembly LoadFile(string scriptFile)
        {
            return Load(scriptFile, null, false, null);
        }

        /// <summary>
        /// Compiles script file into assembly (temporary file) with CSExecutor and loads it in current AppDomain.
        /// This method is an equivalent of the CSScript.Load(scriptFile, null, false);
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled/Loaded assembly.</returns>
        [Obsolete("This method has been renamed to better align with Mono and Roslyn based CS-Script evaluators. Use LoadFile instead.", false)]
        static public Assembly Load(string scriptFile, params string[] refAssemblies)
        {
            return Load(scriptFile, null, false, refAssemblies);
        }

        /// <summary>
        /// Compiles script file into assembly (temporary file) with CSExecutor and loads it in current AppDomain.
        /// This method is an equivalent of the CSScript.Load(scriptFile, null, false);
        /// </summary>
        /// <param name="scriptFile">The path to the script file to be compiled.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional assemblies referenced by the script. </param>
        /// <returns>Compiled/Loaded assembly.</returns>
        static public Assembly LoadFile(string scriptFile, params string[] refAssemblies)
        {
            return Load(scriptFile, null, false, refAssemblies);
        }

        /// <summary>
        /// Default implementation of displaying application messages.
        /// </summary>
        static void DefaultPrint(string msg)
        {
            //do nothing
        }

        static bool rethrow;

        /// <summary>
        /// LoadedScript is a class, which holds information about the script file location and it's compiled and loaded assmbly (current AppDomain).
        /// </summary>
        public class LoadedScript
        {
            /// <summary>
            /// Creates instance of LoadedScript
            /// </summary>
            /// <param name="script">Script file location.</param>
            /// <param name="asm">Compiled script assembly loaded into current AppDomain.</param>
            public LoadedScript(string script, Assembly asm)
            {
                this.script = Path.GetFullPath(script);
                this.asm = asm;
            }

            /// <summary>
            /// Script file location.
            /// </summary>
            public string script;

            /// <summary>
            /// Compiled script assembly loaded into current AppDomain.
            /// </summary>
            public Assembly asm;
        }

        /// <summary>
        /// Controls if ScriptCache should be used when script file loading is requested (CSScript.Load(...)). If set to true and the script file was previously compiled and already loaded
        /// the script engine will use that compiled script from the cache instead of compiling it again.
        /// Note the script cache is always maintained by the script engine. The CacheEnabled property only indicates if the cached script should be used or not when CSScript.Load(...) method is called.
        /// </summary>
        public static bool CacheEnabled = true;

        /// <summary>
        /// Ignores the system wide configuration for CS-Script (e.g. default referenced assemblies, alternative compiler).
        /// </summary>
        /// <returns>Folder where the current system wide configuration file (css_config.xml) is located.</returns>
        public static string IgnoreSystemWideConfig()
        {
            var config_path = Environment.GetEnvironmentVariable("CSSCRIPT_DIR");
            Environment.SetEnvironmentVariable("CSSCRIPT_DIR", null);
            return config_path;
        }

        /// <summary>
        /// The resolve source file algorithm.
        /// <para>
        /// The default algorithm searches for script file by given script name. Search order:
        /// 1. Current directory
        /// 2. extraDirs (usually %CSSCRIPT_DIR%\Lib and ExtraLibDirectory)
        /// 3. PATH
        /// Also fixes file name if user did not provide extension for script file (assuming .cs extension)
        /// </para>
        /// <para>If the default implementation isn't suitable then you can set <c>CSScript.ResolveSourceAlgorithm</c>
        /// to the alternative implementation of the probing algorithm.</para>
        /// </summary>
        public static ResolveSourceFileAlgorithm ResolveSourceAlgorithm
        {
            get { return FileParser.ResolveFilesAlgorithm; }
            set { FileParser.ResolveFilesAlgorithm = value; }
        }

        /// <summary>
        /// Resolves namespace/assembly(file) name into array of assembly locations (local and GAC ones).
        /// </summary>
        /// <para>If the default implementation isn't suitable then you can set <c>CSScript.ResolveAssemblyAlgorithm</c>
        /// to the alternative implementation of the probing algorithm.</para>
        /// <returns>collection of assembly file names where namespace is implemented</returns>
        public static ResolveAssemblyHandler ResolveAssemblyAlgorithm
        {
            get { return AssemblyResolver.FindAssemblyAlgorithm; }
            set { AssemblyResolver.FindAssemblyAlgorithm = value; }
        }

        /// <summary>
        /// Cache of all loaded script files for the current process.
        /// </summary>
        public static LoadedScript[] ScriptCache
        {
            get
            {
#if net1
                return (LoadedScript[])scriptCache.ToArray(typeof(LoadedScript));
#else
                return scriptCache.ToArray();
#endif
            }
        }

#if net1
        static ArrayList scriptCache = new ArrayList();
#else
        static List<LoadedScript> scriptCache = new List<LoadedScript>();
#endif

        /// <summary>
        /// Returns file path of the cached script assembly matching the script file name.
        /// </summary>
        /// <param name="file">Script file path.</param>
        /// <returns>Path to the previously compiled script assembly.
        /// Returns null if the cached script cannot be found.
        /// </returns>
        public static string GetCachedScriptAssemblyFile(string file)
        {
            string path = Path.GetFullPath(file);

            foreach (LoadedScript item in ScriptCache)
            {
                string location = item.asm.Location();
                if (item.script == path && !IsOutOfDateAlgorithm(path, location))
                    return location;
            }
            string cacheFile = GetCachedScriptPath(path);

            if (File.Exists(cacheFile) && !IsOutOfDateAlgorithm(path, cacheFile))
                return cacheFile;

            return null;
        }

        /// <summary>
        /// Returns loaded cached script assembly matching the script file name.
        /// </summary>
        /// <param name="file">Script file path.</param>
        /// <returns>Assembly loaded in the current AppDomain.
        /// Returns null if the loaded script cannot be found.
        /// </returns>
        public static Assembly GetLoadedCachedScriptAssembly(string file)
        {
            string path = Path.GetFullPath(file);

            foreach (LoadedScript item in ScriptCache)
            {
                string location = item.asm.Location();
                if (item.script == path && !IsOutOfDateAlgorithm(path, location))
                    return item.asm;
            }

            string cacheFile = GetCachedScriptPath(path);

            if (File.Exists(cacheFile) && !IsOutOfDateAlgorithm(path, cacheFile))
                return Assembly.LoadFrom(cacheFile);

            return null;
        }

        /// <summary>
        /// Returns path to the cached script assembly matching the script file name.
        /// </summary>
        /// <param name="scriptFile">Script file path.</param>
        /// <returns>Cached (compiled) script assembly file path.</returns>
        public static string GetCachedScriptPath(string scriptFile)
        {
            string path = Path.GetFullPath(scriptFile);
            return Path.Combine(csscript.CSSEnvironment.GetCacheDirectory(path), Path.GetFileName(path) + ".compiled");
        }

        static IsOutOfDateResolver isOutOfDateAlgorithm = CachProbing.Advanced;

        /// <summary>
        /// Compiled script probing handler. Analyses the script file and the compiled script (script assembly)
        /// and determines if the script assembly is out of data and needs to be recompiled.
        /// <para>The default implementation is <see cref="CSScriptLibrary.CSScript.CachProbing.Advanced"/>.</para>
        /// <para>You can always supply your custom algorithm. For example <p>
        /// <c>CSScript.IsOutOfDateAlgorithm = (s, a) => true;</c></p>
        /// fill force CS-Script to recompile the script every time it is loaded.</para>
        /// </summary>
        public static IsOutOfDateResolver IsOutOfDateAlgorithm
        {
            get { return isOutOfDateAlgorithm; }
            set { isOutOfDateAlgorithm = value; }
        }

        /// <summary>
        /// Compiled script probing handler. Analyses the script file and the compiled script (script assembly)
        /// and determines if the script assembly is out of data and needs to be recompiled.
        /// <para>The default implementation is <see cref="CSScriptLibrary.CSScript.CachProbing.Simplified"/>.</para>
        /// <para>You can always supply your custom algorithm. For example <p>
        /// <c>CSScript.IsOutOfDate = (s, a) => true;</c></p>
        /// will force CS-Script to recompile the script every time it is loaded.</para>
        /// </summary>
#if !net35 && !net1

        [Obsolete("This type member will be removed in the future releases. Please use CSScript.Evaluator instead.")]
#endif
        public static IsOutOfDateResolver IsOutOfDate
        {
            get { return isOutOfDateAlgorithm; }
            set { isOutOfDateAlgorithm = value; }
        }

        /// <summary>
        /// This class contains default implementations of the <see cref="CSScriptLibrary.CSScript.IsOutOfDateAlgorithm"/>.
        /// </summary>
        public class CachProbing
        {
            /// <summary>
            /// Gets the simplified IsOutOfDateAlgorithm implementation. The implementation is based on analysis of the 'LastWriteTimeUtc' timestamps of the script and compiled script assembly. But not the script dependencies.
            /// </summary>
            public static IsOutOfDateResolver Simplified
            {
                get { return ScriptAsmOutOfDateSimplified; }
            }

            /// <summary>
            /// Gets the comprehensive IsOutOfDateAlgorithm implementation. The implementation is based on analysis of the 'LastWriteTimeUtc' timestamps of the script, compiled script assembly and all script dependencies.
            /// </summary>
            public static IsOutOfDateResolver Advanced
            {
                get { return ScriptAsmOutOfDateAdvanced; }
            }

            internal static bool ScriptAsmOutOfDateAdvanced(string scriptFileName, string assemblyFileName)
            {
                if (assemblyFileName == "" || assemblyFileName == null || !File.Exists(assemblyFileName))
                    return true;

                if (Settings.legacyTimestampCaching)
                    if (File.GetLastWriteTimeUtc(scriptFileName) != File.GetLastWriteTimeUtc(assemblyFileName))
                        return true;

                return MetaDataItems.IsOutOfDate(scriptFileName, assemblyFileName);
            }

            internal static bool ScriptAsmOutOfDateSimplified(string scriptFileName, string assemblyFileName)
            {
                return (File.GetLastWriteTimeUtc(scriptFileName) != File.GetLastWriteTimeUtc(assemblyFileName));
            }
        }

        internal static string[] RemovePathDuplicates(string[] list)
        {
            lock (typeof(CSScript))
            {
                return Utils.RemovePathDuplicates(list);
            }
        }
    }
}

namespace csscript
{
    /// <summary>
    /// This class implements access to the CS-Script global configuration settings.
    /// </summary>
    public class CSSEnvironment
    {
        /// <summary>
        /// Generates the name of the cache directory for the specified script file.
        /// </summary>
        /// <param name="file">Script file name.</param>
        /// <returns>Cache directory name.</returns>
        public static string GetCacheDirectory(string file)
        {
            return CSExecutor.GetCacheDirectory(file);
        }

        /// <summary>
        /// Saves code to the script file in the dedicated CS-Script <c>temporary files</c> location. You do not have to delete the script file after the execution.
        /// It will be deleted as part of the periodical automatic CS-Script maintenance.
        /// </summary>
        /// <param name="content">The script file content.</param>
        /// <returns>Name of the created temporary script file.</returns>
        public static string SaveAsTempScript(string content)
        {
            string tempFile = CSExecutor.GetScriptTempFile();
            using (StreamWriter sw = new StreamWriter(tempFile))
            {
                sw.Write(content);
            }
            return tempFile;
        }

        /// <summary>
        /// Generates the script file path in the dedicated CS-Script <c>temporary files</c> location. You do not have to delete such file after the execution.
        /// It will be deleted as part of the periodical automatic CS-Script maintenance.
        /// </summary>
        /// <returns>Name of the temporary script file.</returns>
        public static string GetTempScriptName()
        {
            return CSExecutor.GetScriptTempFile();
        }

        /// <summary>
        /// Sets the location for the CS-Script temporary files directory.
        /// </summary>
        /// <param name="path">The path for the temporary directory.</param>
        static public void SetScriptTempDir(string path)
        {
            CSExecutor.SetScriptTempDir(Path.GetFullPath(path));
        }

        /// <summary>
        /// Gets the location of the CS-Script temporary files directory.
        /// </summary>
        /// <returns></returns>
        static public string GetScriptTempDir()
        {
            return CSExecutor.GetScriptTempDir();
        }

        /// <summary>
        /// The full name of the script file being executed.
        /// </summary>
        [Obsolete("This member may not work correctly in the hosted and cached scenarios. Use alternative techniques demonstrated in the ReflectScript.cs sample." +
            "Including environment variable 'EntryScript' and AssemblyDescriptionAttribute (of the script assembly) containing the full path of the script file.", true)]
        public static string ScriptFile
        {
            get
            {
                scriptFile = FindExecuteOptionsField(Assembly.GetExecutingAssembly(), "scriptFileName");
                if (scriptFile == null)
                    scriptFile = FindExecuteOptionsField(Assembly.GetEntryAssembly(), "scriptFileName");
                return scriptFile;
            }
        }

        static string scriptFile = null;

        /// <summary>
        /// The full name of the primary script file being executed. Usually it is the same file as ScriptFile.
        /// However these fields are different if analyzed from the pre/post-script.
        /// </summary>
        [Obsolete("This member may not work correctly in the hosted and cached scenarios. Use alternative techniques demonstrated in the ReflectScript.cs sample." +
            "Including environment variable 'EntryScript' and AssemblyDescriptionAttribute (of the script assembly) containing the full path of the script file.", true)]
        public static string PrimaryScriptFile
        {
            get
            {
                if (scriptFileNamePrimary == null)
                {
                    scriptFileNamePrimary = FindExecuteOptionsField(Assembly.GetExecutingAssembly(), "scriptFileNamePrimary");
                    if (scriptFileNamePrimary == null || scriptFileNamePrimary == "")
                        scriptFileNamePrimary = FindExecuteOptionsField(Assembly.GetEntryAssembly(), "scriptFileNamePrimary");
                }
                return scriptFileNamePrimary;
            }
        }

        static string scriptFileNamePrimary = null;

        static string FindExecuteOptionsField(Assembly asm, string field)
        {
            Type t = asm.GetModules()[0].GetType("csscript.ExecuteOptions");
            if (t != null)
            {
                foreach (FieldInfo fi in t.GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    if (fi.Name == "options")
                    {
                        //need to use reflection as we might be running either cscs.exe or the script host application
                        //thus there is no warranty which assembly contains correct "options" object
                        object otionsObject = fi.GetValue(null);
                        if (otionsObject != null)
                        {
                            object scriptFileObject = otionsObject.GetType().GetField(field).GetValue(otionsObject);
                            if (scriptFileObject != null)
                                return scriptFileObject.ToString();
                        }
                        break;
                    }
                }
            }
            return null;
        }

        CSSEnvironment()
        {
        }
    }

    delegate void PrintDelegate(string msg);

    /// <summary>
    /// Repository for application specific data
    /// </summary>
    internal class AppInfo
    {
        public static string appName = "CSScriptLibrary";
        public static bool appConsole = false;

        public static string appLogo
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + "." + Environment.NewLine + "Copyright (C) 2004 Oleg Shilo." + Environment.NewLine; }
        }

        public static string appLogoShort
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + Environment.NewLine; }
        }

        //#pragma warning disable 414
        public static string appParams = "[/nl]:";

        //#pragma warning restore 414
        public static string appParamsHelp = "nl	-	No logo mode: No banner will be shown at execution time." + Environment.NewLine;
    }

    /// <summary>
    /// Enables access to objects across application domain boundaries in applications that support
    /// Remoting, giving them infinite lease time by setting <see cref="ILease.InitialLeaseTime"/>
    /// to <see cref="TimeSpan.Zero"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var code = @"using System;
    ///              public class Script : MarshalByRefObjectWithInfiniteLifetime
    ///              {
    ///                  public void Hello(string greeting)
    ///                  {
    ///                      Console.WriteLine(greeting);
    ///                  }
    ///              }";
    ///
    /// using (var helper = new AsmHelper(CSScript.CompileCode(code), null, deleteOnExit: true))
    /// {
    ///     IScript script = helper.CreateAndAlignToInterface&lt;IScript&gt;("*");
    ///     script.Hello("Hi there...");
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// The script engine mechanism is not a real inter-process communication. It does use remoting,
    /// but within a single process. So it is acceptable to switch off the timeout mechanism. When
    /// the hosting application is terminated, all objects will be finalized anyway.
    ///
    /// Without the modification, if there is no interaction between script and host for more than
    /// 5 minutes (the default lease time for .NET Remoting), a <see cref="T:System.Runtime.RemotingException"/>
    /// may be thrown, stating:
    /// <![CDATA[Object '/<guid>/<id>.rem' has been disconnected or does not exist at the server.]]>
    /// </remarks>
    public class MarshalByRefObjectWithInfiniteLifetime : MarshalByRefObject
    {
        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for this instance.
        /// </summary>
        public override object InitializeLifetimeService()
        {
            var lease = (ILease)base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial)
            {
                // If the 'InitialLeaseTime' property is set to 'TimeSpan.Zero', then the lease will
                // never time out and the object associated with it will have an infinite lifetime:
                lease.InitialLeaseTime = TimeSpan.Zero;
            }

            return (lease);
        }
    }
}