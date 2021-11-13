using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace CSScripting
{
    class Directives
    {
        public const string compiler = "//css_engine";
        public const string compiler_csc = "csc";
        public const string compiler_csc_inproc = "csc-inproc";
        public const string compiler_dotnet = "dotnet";
        public const string compiler_roslyn = "roslyn";
        public const string compiler_roslyn_inproc = "roslyn-inproc";
    }

    /// <summary>
    /// Various Reflection extensions for implementing assembly unloading
    /// </summary>
    public static class AssemblyUnloadingExtensions
    {
        /// <summary>
        /// Retrieves <see cref="AssemblyLoadContext"/> associated with the assembly and unloads it.
        /// <para>It will throw an exception if the <see cref="AssemblyLoadContext"/> is not created as
        /// unloadable ('IsCollectible').</para>
        /// <para>Use <see cref="CSScriptLib.IEvaluator.IsAssemblyUnloadingEnabled"/> to control
        /// how the assemblies (compiled scripts) are loaded.</para>
        /// <para>
        /// Note, unloading of assembly is implemented by CLR not CS-Script. This method extension is simply
        /// redirecting the call to the .NET <see cref="System.Runtime.Loader.AssemblyLoadContext.Unload"/>
        /// . Thus it is subject of the 
        /// underlying limitations. Thus an AssemblyLoadContext can only be unloaded if it is collectible. And 
        /// Unloading will occur asynchronously.
        /// </para>
        /// <para>
        /// Note, using 'dynamic` completely breaks CLR unloading mechanism. Most likely it triggers
        /// an accidental referencing of the assembly or <see
        /// cref="System.Runtime.Loader.AssemblyLoadContext"/>. Meaning that if you are planing to
        /// use assembly unloading you need to use interface based scripting. See `Test_Unloading`
        /// (https://github.com/oleg-shilo/cs-script/blob/master/src/CSScriptLib/src/Client.NET-Core/Program.cs)
        /// sample for details.
        /// </para>
        /// </summary>
        /// <param name="asm"></param>
        public static void Unload(this Assembly asm)
        {
            dynamic context = AssemblyLoadContext.GetLoadContext(asm);
            try
            {
                context.Unload();
            }
            catch (System.InvalidOperationException e)
            {
                var error = IsUnloadingSupported ?
                                "The problem may be caused by the assembly loaded with non-collectible `AssemblyLoadContext` (default CLR behavior)."
                                :
                                "Your runtime version may not support unloading assemblies. The host application needs to target .NET 5 and higher.";

                throw new NotImplementedException(error, e);
            }
        }

#if class_lib

        static ConstructorInfo AssemblyLoadContextConstructor = typeof(AssemblyLoadContext).GetConstructor(new Type[] { typeof(string), typeof(bool) });
        static bool IsUnloadingSupported = AssemblyLoadContextConstructor != null;

        static AssemblyUnloadingExtensions()
        {
            if (IsUnloadingSupported)
                CSScriptLib.Runtime.CreateUnloadableAssemblyLoadContext =
                    () => (AssemblyLoadContext)AssemblyLoadContextConstructor.Invoke(new object[] { Guid.NewGuid().ToString(), true });
        }

#else
        static bool IsUnloadingSupported = false;
#endif

        internal static Assembly LoadCollectableAssemblyFrom(this AppDomain appDomain, string assembly)
        {
#if !class_lib
            return Assembly.LoadFrom(assembly);
#else
            if (CSScriptLib.Runtime.CreateUnloadableAssemblyLoadContext == null)
                return Assembly.LoadFrom(assembly);
            else
                return CSScriptLib.Runtime.CreateUnloadableAssemblyLoadContext()
                                          .LoadFromAssemblyPath(assembly);
#endif
        }

        internal static Assembly LoadCollectableAssembly(this AppDomain appDomain, byte[] assembly, byte[] assemblySymbols = null)
        {
            Assembly asm = null;

            Assembly legacy_load()
                => (assemblySymbols != null) ?
                    appDomain.Load(assembly, assemblySymbols) :
                    appDomain.Load(assembly);
#if !class_lib
            asm = legacy_load();
#else
            if (CSScriptLib.Runtime.CreateUnloadableAssemblyLoadContext == null)
            {
                asm = legacy_load();
            }
            else
            {
                using (var stream = new MemoryStream(assembly))
                {
                    var context = CSScriptLib.Runtime.CreateUnloadableAssemblyLoadContext();

                    if (assemblySymbols != null)
                    {
                        using (var symbols = new MemoryStream(assemblySymbols))
                            asm = context.LoadFromStream(stream, symbols);
                    }
                    else
                    {
                        asm = context.LoadFromStream(stream);
                    }
                }
            }
#endif
            return asm;
        }
    }

    /// <summary>
    /// Various Reflection extensions
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Returns directory where the specified assembly file is.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <returns>The directory path</returns>
        public static string Directory(this Assembly asm)
        {
            var file = asm.Location();
            if (file.IsNotEmpty())
                return Path.GetDirectoryName(file);
            else
                return "";
        }

        /// <summary>
        /// Returns location of the specified assembly. Avoids throwing an exception in case
        /// of dynamic assembly.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <returns>The path to the assembly file</returns>
        public static string Location(this Assembly asm)
        {
            if (asm.IsDynamic())
            {
                string location = Environment.GetEnvironmentVariable("location:" + asm.GetHashCode());
                if (location == null)
                {
                    // Note assembly can contain only single AssemblyDescriptionAttribute
                    var locationFromDescAttr = asm
                        .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true)?
                        .Cast<AssemblyDescriptionAttribute>()
                        .FirstOrDefault()?
                        .Description;
                    if (locationFromDescAttr.FileExists())
                        return locationFromDescAttr;

#pragma warning disable SYSLIB0012
                    var validPath = asm.CodeBase?.FromUriToPath();
#pragma warning restore SYSLIB0012

                    if (validPath.FileExists())
                        return validPath;

                    return "";
                }
                else
                    return location;
            }
            else
                return asm.Location;
        }

        internal static string FromUriToPath(this string uri)
            => new Uri(uri).LocalPath;

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Thew name of the type.</returns>
        public static string GetName(this Type type)
        {
            return type.GetTypeInfo().Name;
        }

        /// <summary>
        /// Creates instance of a class from underlying assembly.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
        /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>
        /// Instance of the 'Type'. Throws an ApplicationException if the instance cannot be created.
        /// </returns>
        public static object CreateObject(this Assembly asm, string typeName, params object[] args)
        {
            return CreateInstance(asm, typeName, args);
        }

        /// <summary>
        /// Creates instance of a Type from underlying assembly.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <param name="typeName">Name of the type to be instantiated. Allows wild card character (e.g. *.MyClass can be used to instantiate MyNamespace.MyClass).</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>
        /// Created instance of the type.
        /// </returns>
        /// <exception cref="System.Exception">Type " + typeName + " cannot be found.</exception>
        private static object CreateInstance(Assembly asm, string typeName, params object[] args)
        {
            //note typeName for FindTypes does not include namespace
            if (typeName == "*")
            {
                //instantiate the user first type found (but not auto-generated types)
                //Ignore Roslyn internal root type: "Submission#0"; real script class will be Submission#0+Script

                var firstUserTypes = asm.OrderedUserTypes()
                                        .FirstOrDefault();

                if (firstUserTypes != null)
                    return Activator.CreateInstance(firstUserTypes, args);

                return null;
            }
            else
            {
                var name = typeName.Replace("*.", "");

                Type[] types = asm.OrderedUserTypes()
                                  .Where(t => (t.FullName == name
                                               || t.FullName == ($"{Globals.RootClassName}+{name}")
                                               || t.Name == name))
                                      .ToArray();

                if (types.Length == 0)
                    throw new Exception("Type " + typeName + " cannot be found.");

                return Activator.CreateInstance(types.First(), args);
            }
        }

        static bool IsRoslynInternalType(this Type type)
            => type.FullName.Contains("<<Initialize>>"); // Submission#0+<<Initialize>>d__0

        static bool IsScriptRootClass(this Type type)
            => type.FullName.Contains($"{Globals.RootClassName}+"); // Submission#0+Script

        internal static IEnumerable<Type> OrderedUserTypes(this Assembly asm)
           => asm.ExportedTypes
                  .Where(t => !t.IsRoslynInternalType())
                  .OrderBy(t => !t.IsScriptRootClass());  // ScriptRootClass will be on top

        internal static Type FirstUserTypeAssignableFrom<T>(this Assembly asm)
            => asm.OrderedUserTypes().FirstOrDefault(x => typeof(T).IsAssignableFrom(x));

        /// <summary>
        /// Determines whether the assembly is dynamic.
        /// </summary>
        /// <param name="asm">The asm.</param>
        /// <returns>
        ///   <c>true</c> if the specified asm is dynamic; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDynamic(this Assembly asm)
        {
            try
            {
                //http://bloggingabout.net/blogs/vagif/archive/2010/07/02/net-4-0-and-notsupportedexception-complaining-about-dynamic-assemblies.aspx
                //Will cover both System.Reflection.Emit.AssemblyBuilder and System.Reflection.Emit.InternalAssemblyBuilder
                return asm.GetType().FullName.EndsWith("AssemblyBuilder") || asm.Location == null || asm.Location == "";
            }
            catch { return false; }
        }
    }
}