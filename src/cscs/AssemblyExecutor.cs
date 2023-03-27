using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CSScripting;

namespace csscript
{
    /// <summary>
    /// Executes "public static void Main(..)" of assembly in a separate domain.
    /// </summary>
    class AssemblyExecutor
    {
        LocalExecutor executor;
        string assemblyFileName;

        public AssemblyExecutor(string fileName, string domainName)
        {
            assemblyFileName = fileName;
            executor = new LocalExecutor(ExecuteOptions.options.searchDirs);
        }

        public void Execute(string[] args)
        {
            executor.ExecuteAssembly(assemblyFileName, args);
        }
    }

    /// <summary>
    /// Invokes static method 'Main' from the assembly.
    /// </summary>
    class LocalExecutor
    {
        public string[] searchDirs;

        public LocalExecutor(params string[] searchDirs)
        {
            this.searchDirs = searchDirs;
        }

        /// <summary>
        /// AppDomain event handler. This handler will be called if CLR cannot resolve
        /// referenced local assemblies
        /// </summary>
        public Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var fullName = args.Name;
            var shortName = args.Name.Split(',').First();

            //it is tempting to throw but should not as there can be other (e.g. host) ResolveEventHandler(s)
            //and throwing will prevent them from being invoked
            bool throwExceptions = false;

            var dirs = searchDirs.Where(x => !x.StartsWith(Settings.dirs_section_prefix));

            Assembly probe_appdomain(string name)
                => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);

            Assembly probe_dir(string name)
            {
                foreach (string dir in dirs)
                {
                    var retval = AssemblyResolver.ResolveAssembly(name, dir, throwExceptions);
                    if (retval != null)
                        return retval;
                }

                return null;
            }

            return probe_dir(fullName) ??
                   probe_dir(shortName) ??
                   probe_appdomain(shortName); // repeat it again but with the short name
        }

        public Assembly ResolveResEventHandler(object sender, ResolveEventArgs args) => Assembly.LoadFile(this.asmFile);

        string asmFile = "";

        public void ExecuteAssembly(string filename, string[] args) =>
            ExecuteAssembly(filename, args, null);

        public void ExecuteAssembly(string filename, string[] args, SystemWideLock asmLock)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;
            AppDomain.CurrentDomain.ResourceResolve += ResolveResEventHandler;

            asmFile = filename;
            Assembly assembly;

            if (!ExecuteOptions.options.inMemoryAsm)
            {
                assembly = Assembly.LoadFile(filename);
            }
            else
            {
                //Load(byte[]) does not lock the assembly file as LoadFrom(filename) does
                byte[] data = File.ReadAllBytes(filename);
                string dbg = Utils.DbgFileOf(filename);

                if (ExecuteOptions.options.DBG && File.Exists(dbg))
                {
                    byte[] dbgData = File.ReadAllBytes(dbg);
                    assembly = Assembly.Load(data, dbgData);
                }
                else
                    assembly = Assembly.Load(data);

                asmLock?.Release();
            }

            SetScriptReflection(assembly, Path.GetFullPath(filename), true);

            InvokeStaticMain(assembly, args);
        }

        internal static void SetScriptReflection(Assembly assembly, string location, bool setScriptLocationReflection)
        {
            if (setScriptLocationReflection)
                Environment.SetEnvironmentVariable("location:" + assembly.GetHashCode(), location);

            string source = null;
            //Note assembly can contain only single AssemblyDescriptionAttribute
            foreach (AssemblyDescriptionAttribute attribute in assembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true))
                source = attribute.Description;

            //check if executing the primary script and not hosted execution ("CSScriptRuntime" == null)
            if (Environment.GetEnvironmentVariable("CSScriptRuntime") != null && source == Environment.GetEnvironmentVariable("EntryScript"))
                Environment.SetEnvironmentVariable("EntryScriptAssembly", location);
        }

        public void InvokeStaticMain(Assembly compiledAssembly, string[] scriptArgs)
        {
            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Static;

            MethodInfo[] methods = compiledAssembly.GetModules()
                                                   .SelectMany(m => m.GetTypes())
                                                   .SelectMany(t => t.GetMembers(bf)
                                                   .OfType<MethodInfo>())
                                                   .Where(x => x.Name.IsOneOf("Main", "$Main", "<Main>$") && x.IsStatic)
                                                   .ToArray();
            if (methods.Any())
            {
                if (methods.Count() > 1)
                    throw new ApplicationException("Multiple entry points are defined in the script.");

                var method = methods.First();

                object retval = null;

                if (method.GetParameters().Length != 0)
                    retval = method.Invoke(new object(), new object[] { (Object)scriptArgs });
                else
                    retval = method.Invoke(new object(), null);

                if (retval != null)
                {
                    if (retval is Task<int>)
                    {
                        // Environment.ExitCode = await (retval as Task<int>); // does not work
                        Environment.ExitCode = (retval as Task<int>).Result;
                    }
                    else if (retval is Task)
                    {
                        (retval as Task).Wait();
                    }
                    else
                    {
                        if (int.TryParse(retval?.ToString(), out var i))
                            Environment.ExitCode = i;
                    }
                }
            }
            else
            {
                throw new ApplicationException("Cannot find entry point. Make sure script file contains method: 'public static Main(...)'");
            }
        }
    }
}