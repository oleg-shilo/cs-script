#region Licence...

//-----------------------------------------------------------------------------
// Date:	30/10/10	Time: 8:21
// Module:	ScriptLauncherBuilder.cs
// Classes:	ScriptLauncherBuilder
//			
// This module contains the definition of the ScriptLauncherBuilder class. Which implements
// compiling light-weigh host application for the script execution.
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

#endregion Licence...

using System;
using System.IO;
using System.Reflection;

#if net1
using System.Collections;
#else

using System.Collections.Generic;

#endif

using System.Text;
using CSScriptLibrary;
using System.Runtime.InteropServices;
using System.Threading;
using System.CodeDom.Compiler;
//using System.Windows.Forms;
using System.Globalization;
using System.Diagnostics;
using Microsoft.CSharp;




namespace csscript
{
    internal class ScriptLauncherBuilder
    {
        public static string GetLauncherName(string assembly)
        {
            return assembly + ".host.exe";
        }

        // UnitTest
        // Make Surrogate scenario to compile conditionally 
        // + check and delete the exe before building
        // + set Appartment state
        // + update all ExecutionClients incliding csslib
        // + when starting remove css and //x args 
        //+ try to solve limitations with console Input redurectionlimi
        //+ ensure launcher is not build when building dll/exe without execution
        public string BuildSurrogateLauncher(string scriptAssembly, string tragetFramework, CompilerParameters compilerParams, ApartmentState appartmentState, string consoleEncoding)
        {
            //Debug.Assert(false);
#if !net4
            throw new ApplicationException("Cannot build surrogate host application because this script engine is build against early version of CLR.");
#else
            var provider = CodeDomProvider.CreateProvider("C#", new Dictionary<string, string> { { "CompilerVersion", tragetFramework } });

            compilerParams.OutputAssembly = GetLauncherName(scriptAssembly);
            compilerParams.GenerateExecutable = true;
            compilerParams.GenerateInMemory = false;
            compilerParams.IncludeDebugInformation = false;


            try
            {
                Utils.FileDelete(compilerParams.OutputAssembly, true);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Cannot build surrogate host application", e);
            }

            if (compilerParams.CompilerOptions != null)
                compilerParams.CompilerOptions = compilerParams.CompilerOptions.Replace("/d:TRACE", "")
                                                                               .Replace("/d:DEBUG", "");

            if (!AppInfo.appConsole)
                compilerParams.CompilerOptions += " /target:winexe";

            string refAssemblies = "";
            string appartment = "[STAThread]";
            if (appartmentState == ApartmentState.MTA)
                appartment = "[" + appartmentState + "Thread]";
            else if (appartmentState == ApartmentState.Unknown)
                appartment = "";

            foreach (string asm in compilerParams.ReferencedAssemblies)
                if (File.Exists(asm)) //ignore GAC (not full path) assemblies 
                    refAssemblies += Assembly.ReflectionOnlyLoadFrom(asm).FullName + ":" + asm + ";";

            compilerParams.ReferencedAssemblies.Clear(); //it is important to remove all asms as they can have absolute path to the wrong CLR asms branch
            compilerParams.ReferencedAssemblies.Add("System.dll");

            foreach (var item in compilerParams.ReferencedAssemblies)
            {
                Debug.WriteLine(item);
            }

            string setEncodingSatement = "";

            if (string.Compare(consoleEncoding, Settings.DefaultEncodingName, true) != 0)
                setEncodingSatement = "try { Console.OutputEncoding = System.Text.Encoding.GetEncoding(\""+consoleEncoding+"\"); } catch {}";

            string code = launcherCode
                                .Replace("${REF_ASSEMBLIES}", refAssemblies)
                                .Replace("${APPARTMENT}", appartment)
                                .Replace("${CONSOLE_ENCODING}", setEncodingSatement)
                                .Replace("${ASM_MANE}", Path.GetFileName(scriptAssembly));

            CompilerResults retval;

            compilerParams.IncludeDebugInformation = true;

            bool debugLauncher = false;
            if (debugLauncher)
            {
                compilerParams.CompilerOptions += " /d:DEBUG";

                string launcherFile = Environment.ExpandEnvironmentVariables(@"C:\Users\%USERNAME%\Desktop\New folder\script.launcher.cs");
                File.WriteAllText(launcherFile, code);
                retval = provider.CompileAssemblyFromFile(compilerParams, launcherFile);
            }
            else
                retval = provider.CompileAssemblyFromSource(compilerParams, code);

            foreach (CompilerError err in retval.Errors)
                if (!err.IsWarning)
                    throw CompilerException.Create(retval.Errors, true);

            CSSUtils.SetTimestamp(compilerParams.OutputAssembly, scriptAssembly);
            return compilerParams.OutputAssembly;
#endif
        }
        const string launcherCode =
@"using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Diagnostics;

class Script
{
    ${APPARTMENT}
    static public int Main(string[] args)
    {
        ${CONSOLE_ENCODING}
        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            MainImpl(args);
        }
        catch(Exception e)
        {
            Console.WriteLine(e.ToString());
            return 1;
        }
        return Environment.ExitCode;
    }
   
    static public void MainImpl(string[] args)
    {
        //System.Diagnostics.Debug.Assert(false);

        string scriptAssembly = """";
        bool debug = false;

        ArrayList newArgs = new ArrayList();
        foreach (string arg in args)
        {
            if (arg.StartsWith(""/css_host_""))
            {
                if (arg.StartsWith(""/css_host_dbg:""))
                    debug = (arg == ""/css_host_dbg:true"");
                else if (arg.StartsWith(""/css_host_parent:""))
                    parentHostProcess = int.Parse(arg.Substring(""/css_host_parent:"".Length));
                else if (arg.StartsWith(""/css_host_asm:""))
                    scriptAssembly = arg.Substring(""/css_host_asm:"".Length);
            }
            else
                newArgs.Add(arg);
        }

        if (debug)
        {
            System.Diagnostics.Debugger.Launch();
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
        }
        
        ThreadPool.QueueUserWorkItem(MonitorParentHost);

        if (scriptAssembly == """")
        {
            scriptAssembly = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ""${ASM_MANE}"");
        }
        InvokeStaticMain(Assembly.LoadFrom(scriptAssembly), (string[])newArgs.ToArray(typeof(string)));
    }

    static int parentHostProcess = -1;
    static void MonitorParentHost(object state)
    {
        if (parentHostProcess != -1)
        {
            while (IsProcessRunning(parentHostProcess))
                Thread.Sleep(500);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }

    static bool IsProcessRunning(int id)
    {
        try
        {
            return System.Diagnostics.Process.GetProcessById(parentHostProcess) != null;
        }
        catch { }
        return false;
    }

    static void InvokeStaticMain(Assembly compiledAssembly, string[] scriptArgs)
    {
        MethodInfo method = null;
        foreach (Module m in compiledAssembly.GetModules())
        {
            foreach (Type t in m.GetTypes())
            {
                BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Static;
                foreach (MemberInfo mi in t.GetMembers(bf))
                {
                    if (mi.Name == ""Main"")
                    {
                        method = t.GetMethod(mi.Name, bf);
                    }
                    if (method != null)
                        break;
                }
                if (method != null)
                    break;
            }
            if (method != null)
                break;
        }
        if (method != null)
        {
            object retval = null;
            if (method.GetParameters().Length != 0)
                retval = method.Invoke(new object(), new object[] { (Object)scriptArgs });
            else
                retval = method.Invoke(new object(), null);

            if (retval != null)
            {
                try
                {
                    Environment.ExitCode = int.Parse(retval.ToString());
                }
                catch { }
            }
        }
        else
        {
            throw new ApplicationException(""Cannot find entry point. Make sure script file contains method: 'public static Main(...)'"");
        }
    }

    static string refAssemblies = @""${REF_ASSEMBLIES}"";
    static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        if (refAssemblies != """")
        {
            foreach (string asm in refAssemblies.Split(';'))
                if (asm.StartsWith(args.Name))
                    return Assembly.LoadFrom(asm.Substring(args.Name.Length + 1));
        }
        return null;
    }
}";
    }
}