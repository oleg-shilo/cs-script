#region Licence...

//-----------------------------------------------------------------------------
// Date:	10/9/05	Time: 3:00p
// Module:	AsmHelper.cs
// Classes:	AsmHelper
//
// This module contains the definition of the AsmHelper class. Which implements
// dynamic assembly loading/unloading and invoking methods from loaded assembly.
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using csscript;
using CSScriptLibrary;

/// <summary>
/// Method extensions for
/// </summary>
public static class CSScriptLibraryExtensionMethods
{
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
    static public string GetScriptName(this Assembly assembly)
    {
        return CSScript.GetScriptName(assembly);
    }

    /// <summary>
    /// Executes an action as a "singleton".
    /// <para>This extension method takes a delegate and executes it in the context of the claimed named mutex. It doesn't execute the action and
    /// returns immediately if any previously invoked action from the same context is still in progress: mutex is not released.</para>
    /// <para>Mutex name uniqueness is based on the assembly identity (location).</para>
    /// <code>
    /// //application
    /// class Program
    /// {
    ///     static void Main()
    ///     {
    ///         Assembly.GetExecutingAssembly()
    ///                 .SingletonExecute(main,
    ///                                   ()=>MessageBox.Show("Another instance is already running."));
    ///     }
    ///
    ///     static void main()
    ///     {
    ///         //business logic
    ///     }
    /// }
    ///
    /// //script
    /// var script = @"using System.Reflection;
    ///                public static void DoWork()
    ///                {
    ///                    Assembly.GetExecutingAssembly()
    ///                            .SingletonExecute(() =>
    ///                            {
    ///                                //business logic;
    ///                            },
    ///                            () => Console.WriteLine(""another instance of 'script' is being executed""));
    ///                }";
    ///
    /// var DoWork = CSScript.LoadMethod(script).GetStaticMethod();
    /// Task.Run(DoWork);
    /// Task.Run(DoWork);
    /// Task.Run(DoWork);
    /// </code>
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="action">The action.</param>
    /// <param name="onBusyAction">The on busy action.</param>
#if net4

    public static void SingletonExecute(this Assembly assembly, Action action, Action onBusyAction = null)
#else
    public static void SingletonExecute(this Assembly assembly, Action action, Action onBusyAction)
#endif
    {
        //AssemblyInstance should not resemble a path. Otherwise Mutex constructor will throw.
        using (var mutex = new Mutex(false, "AssemblyInstance - " + assembly.Location().Replace("\\", "-")))
        {
            try
            {
                if (mutex.WaitOne(0, false))
                    action();
                else if (onBusyAction != null)
                    onBusyAction();
            }
            finally
            {
                try { mutex.ReleaseMutex(); } //it may throw
                catch { }
            }
        }
    }

    /// <summary>
    /// Constructs and returns an instance of CSScriptLibrary.AsmHelper class from the underlying Assembly.
    /// </summary>
    /// <returns>CSScriptLibrary.AsmHelper</returns>
    /// <param name="obj">Instance of the type to be extended</param>
    /// <returns>CSScriptLibrary.AsmHelper</returns>
    public static CSScriptLibrary.AsmHelper GetHelper(this Assembly obj)
    {
        return new CSScriptLibrary.AsmHelper(obj);
    }

    /// <summary>
    /// Returns which emitted delegate based on MethodInfo of the underlying assembly.
    /// </summary>
    /// <param name="obj">Instance of the type to be extended</param>
    /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
    /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or even *.*).</param>
    /// <param name="list">List of 'Method' arguments.
    /// Note that values of the items in the list do not have any importance. The type of the list item is
    /// to be used for method search. For example if class Calc has method Sum(int a, int b) the method invoker
    /// can be obtained as following:
    /// <para>
    /// GetStaticMethod("Calc.Sum", 0, 0)
    /// </para>
    /// You can pass any integer as the second and third parameter because it will be used only to obtain the
    /// information about the parameter type (in this case System.Int32).</param>
    /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
    public static CSScriptLibrary.MethodDelegate GetStaticMethod(this Assembly obj, string methodName, params object[] list)
    {
        return new CSScriptLibrary.AsmHelper(obj).GetStaticMethod(methodName, list);
    }

    /// <summary>
    /// Returns which emitted delegate based on MethodInfo of the underlying assembly.
    /// </summary>
    /// <typeparam name="T">The delegate return type.</typeparam>
    /// <param name="obj">Instance of the type to be extended</param>
    /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
    /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or even *.*).</param>
    /// <param name="list">List of 'Method' arguments.
    /// Note that values of the items in the list do not have any importance. The type of the list item is
    /// to be used for method search. For example if class Calc has method Sum(int a, int b) the method invoker
    /// can be obtained as following:
    /// <para>
    /// GetStaticMethod("Calc.Sum", 0, 0)
    /// </para>
    /// You can pass any integer as the second and third parameter because it will be used only to obtain the
    /// information about the parameter type (in this case System.Int32).</param>
    /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
    public static CSScriptLibrary.MethodDelegate<T> GetStaticMethod<T>(this Assembly obj, string methodName, params object[] list)
    {
        return new CSScriptLibrary.AsmHelper(obj).GetStaticMethod<T>(methodName, list);
    }

    /// <summary>
    /// Returns which emitted delegate based on MethodInfo of the underlying assembly.
    /// </summary>
    /// <param name="obj">Instance of the type to be extended</param>
    /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
    /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or even *.*).</param>
    /// <param name="list">List of 'Method' arguments.</param>
    /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
    public static CSScriptLibrary.MethodDelegate GetStaticMethodWithArgs(this Assembly obj, string methodName, params Type[] list)
    {
        return new CSScriptLibrary.AsmHelper(obj).GetStaticMethodWithArgs(methodName, list);
    }

    /// <summary>
    /// Returns which emitted delegate based on MethodInfo of the underlying assembly.
    /// </summary>
    /// <typeparam name="T">The delegate return type.</typeparam>
    /// <param name="obj">Instance of the type to be extended</param>
    /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
    /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or even *.*).</param>
    /// <param name="list">List of 'Method' arguments.</param>
    /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
    public static CSScriptLibrary.MethodDelegate<T> GetStaticMethodWithArgs<T>(this Assembly obj, string methodName, params Type[] list)
    {
        return new CSScriptLibrary.AsmHelper(obj).GetStaticMethodWithArgs<T>(methodName, list);
    }

    /// <summary>
    /// <param name="obj">Instance of the type to be extended</param>
    /// Specialized version of GetMethodInvoker which returns MethodDelegate of the very first method found in the
    /// underlying assembly. This method is an overloaded implementation of the GetStaticMethod(string methodName, params object[] list).
    ///
    /// Use this method when script assembly contains only one single type with one method.
    /// </summary>
    /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
    public static CSScriptLibrary.MethodDelegate GetStaticMethod(this Assembly obj)
    {
        return new CSScriptLibrary.AsmHelper(obj).GetStaticMethod();
    }

    /// <summary>
    /// <param name="obj">Instance of the type to be extended</param>
    /// Specialized version of GetMethodInvoker which returns MethodDelegate of the very first method found in the
    /// underlying assembly. This method is an overloaded implementation of the GetStaticMethod(string methodName, params object[] list).
    ///
    /// Use this method when script assembly contains only one single type with one method.
    /// </summary>
    /// <typeparam name="T">The delegate return type.</typeparam>
    /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
    public static CSScriptLibrary.MethodDelegate<T> GetStaticMethod<T>(this Assembly obj)
    {
        return new CSScriptLibrary.AsmHelper(obj).GetStaticMethod<T>();
    }

    /// <summary>
    /// Attempts to create instance of a class from underlying assembly.
    /// </summary>
    /// <param name="obj">Instance of the type to be extended</param>
    /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
    ///
    /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
    /// <returns>Instance of the 'Type'. Returns null if the instance cannot be created.</returns>
    public static Object TryCreateObject(this Assembly obj, string typeName)
    {
        return new CSScriptLibrary.AsmHelper(obj).TryCreateObject(typeName);
    }

    /// <summary>
    /// Creates instance of a class from underlying assembly.
    /// </summary>
    /// <param name="obj">Instance of the type to be extended</param>
    /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
    /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
    /// <param name="args">The non default constructor arguments.</param>
    /// <returns>Instance of the 'Type'. Throws an ApplicationException if the instance cannot be created.</returns>
    public static Object CreateObject(this Assembly obj, string typeName, params object[] args)
    {
        var browser = new AsmBrowser(obj);
        try
        {
            return browser.CreateInstance(typeName, args);
        }
        finally
        {
            browser.Dispose();
        }
    }

    /// <summary>
    /// Creates the instance of the Type.
    /// <para>It is nothing else but a more convenient version of Assembly. CreateInstance(string typeName, bool ignoreCase, BindingFlags...) method.
    /// It's objective is to simplify invoking the constructors with parameters.
    /// </para>
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <param name="typeName">Name of the type.</param>
    /// <param name="args">The arguments.</param>
    /// <returns></returns>
    public static object CreateInstance(this Assembly assembly, string typeName, params object[] args)
    {
        return assembly.CreateInstance(typeName, true, BindingFlags.CreateInstance, null, args, Thread.CurrentThread.CurrentCulture, new object[0]);
    }

    /// <summary>
    /// Extends the life of the instance created in the remote AppDomain.
    /// </summary>
    /// <param name="obj">The instance created in the remote AppDomain.</param>
    /// <param name="renewalTime">The renewal time.</param>
    /// <returns>Returns <see cref="T:System.Runtime.Remoting.LifetimeClientSponsor" />  object.</returns>
    [Obsolete("While this method is absolutely OK to use, inheriting script class from MarshalByRefObjectWithInfiniteLifetime is a more convenient single- step approach.", false)]
    public static ClientSponsor ExtendLife(this MarshalByRefObject obj, TimeSpan renewalTime)
    {
        var sponsor = new ClientSponsor();
        sponsor.RenewalTime = renewalTime;
        sponsor.Register(obj);

        return sponsor;
    }

    /// <summary>
    /// Extends the life of the instance created in the remote AppDomain.
    /// </summary>
    /// <param name="obj">The instance created in the remote AppDomain.</param>
    /// <param name="minutes">The renewal time in minutes.</param>
    /// <returns>Returns <see cref="T:System.Runtime.Remoting.LifetimeClientSponsor" />  object.</returns>
    [Obsolete("While this method is absolutely OK to use, inheriting script class from MarshalByRefObjectWithInfiniteLifetime is a more convenient single- step approach.", false)]
    public static ClientSponsor ExtendLifeFromMinutes(this object obj, int minutes)
    {
        if (obj is MarshalByRefObject)
        {
            return ((MarshalByRefObject)obj).ExtendLife(TimeSpan.FromMinutes(minutes));
        }
        else
        {
            //var owner = method.GetOwnerObject<MarshalByRefObject>();
            //if (owner == null)
            throw new Exception("MethodDelegate doesn't seem to be owned by the transparent proxy connected to the remote AppDomain object." +
                " You don't need to extend life local objects.");
            //return owner.ExtendLife(TimeSpan.FromMinutes(minutes));
        }
    }

#if net4

    /// <summary>
    /// Extends the life of the instance created in the remote AppDomain.
    /// </summary>
    /// <typeparam name="T">The delegate return type.</typeparam>
    /// <param name="method">The instance of the <see cref="CSScriptLibrary.MethodDelegate{T}"/> created in the remote AppDomain.</param>
    /// <param name="minutes">The renewal time in minutes.</param>
    /// <returns>Returns <see cref="T:System.Runtime.Remoting.LifetimeClientSponsor" />  object.</returns>
    /// <exception cref="System.Exception">MethodDelegate doesn't seem to be owned by the transparent proxy connected to the remote AppDomain object. +
    ///                  You don't need to extend life local objects.</exception>
    public static ClientSponsor ExtendLifeFromMinutes<T>(this MethodDelegate<T> method, int minutes)
    {
        var owner = method.GetOwnerObject<MarshalByRefObject>();
        if (owner == null)
            throw new Exception("MethodDelegate doesn't seem to be owned by the transparent proxy connected to the remote AppDomain object." +
                " You don't need to extend life local objects.");
        return owner.ExtendLife(TimeSpan.FromMinutes(minutes));
    }

    /// <summary>
    /// Extends the life of the instance created in the remote AppDomain.
    /// </summary>
    /// <param name="method">The instance of the <see cref="CSScriptLibrary.MethodDelegate"/> created in the remote AppDomain.</param>
    /// <param name="minutes">The renewal time in minutes.</param>
    /// <returns>Returns <see cref="T:System.Runtime.Remoting.LifetimeClientSponsor" />  object.</returns>
    /// <exception cref="System.Exception">MethodDelegate doesn't seem to be owned by the transparent proxy connected to the remote AppDomain object. +
    ///                  You don't need to extend life local objects.</exception>
    public static ClientSponsor ExtendLifeFromMinutes(this MethodDelegate method, int minutes)
    {
        var owner = method.GetOwnerObject<MarshalByRefObject>();
        if (owner == null)
            throw new Exception("MethodDelegate doesn't seem to be owned by the transparent proxy connected to the remote AppDomain object." +
                " You don't need to extend life local objects.");
        return owner.ExtendLife(TimeSpan.FromMinutes(minutes));
    }

#endif

    /// <summary>
    /// Attempts to align (pseudo typecast) object to interface.
    /// <para>The object does not necessarily need to implement the interface formally.</para>
    /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Interface definition to align with.</typeparam>
    /// <param name="obj">The object to be aligned with the interface.</param>
    /// <returns>Interface object or <c>null</c> if alignment was unsuccessful.</returns>
    public static T TryAlignToInterface<T>(this object obj) where T : class
    {
        return CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj);
    }

    internal static string BuildAlignToInterfaceCode<T>(this object obj, out string typeFullName, bool injectNamespace) where T : class
    {
        return CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.BuildProxyClassCode(typeof(T), obj.GetType(), out typeFullName, injectNamespace);
    }

    internal static string BuildAlignToInterfaceCode<T>(this Type type, out string typeFullName, bool injectNamespace) where T : class
    {
        return CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.BuildProxyClassCode(typeof(T), type, out typeFullName, injectNamespace);
    }

    /// <summary>
    /// Attempts to align (pseudo typecast) object to interface.
    /// <para>The object does not necessarily need to implement the interface formally.</para>
    /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Interface definition to align with.</typeparam>
    /// <param name="obj">The object to be aligned with the interface.</param>
    /// <param name="useAppDomainAssemblies">If set to <c>true</c> uses all loaded assemblies of the current <see cref="System.AppDomain"/>
    /// when emitting (compiling) aligned proxy object.</param>
    /// <returns>Interface object or <c>null</c> if alignment was unsuccessful.</returns>
    public static T TryAlignToInterface<T>(this object obj, bool useAppDomainAssemblies) where T : class
    {
        string[] refAssemblies;
        if (useAppDomainAssemblies)
            refAssemblies = CSSUtils.GetAppDomainAssemblies();
        else
            refAssemblies = new string[0];

        return CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj, refAssemblies);
    }

    /// <summary>
    /// Attempts to align (pseudo typecast) object to interface.
    /// <para>The object does not necessarily need to implement the interface formally.</para>
    /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Interface definition to align with.</typeparam>
    /// <param name="obj">The object to be aligned with the interface.</param>
    /// <param name="refAssemblies">The string array containing file nemes to the additional dependency
    /// assemblies the interface depends in. </param>
    /// <returns>Interface object or <c>null</c> if alignment was unsuccessful.</returns>
    public static T TryAlignToInterface<T>(this object obj, params string[] refAssemblies) where T : class
    {
        return CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj, refAssemblies);
    }

    /// <summary>
    /// Attempts to align (pseudo typecast) object to interface.
    /// <para>The object does not necessarily need to implement the interface formally.</para>
    /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Interface definition to align with.</typeparam>
    /// <param name="obj">The object to be aligned with the interface.</param>
    /// <param name="useAppDomainAssemblies">If set to <c>true</c> uses all loaded assemblies of the current <see cref="System.AppDomain"/>
    /// when emitting (compiling) aligned proxy object.</param>
    /// <param name="refAssemblies">The string array containing file names to the additional dependency
    /// assemblies the interface depends in. </param>
    /// <returns>Interface object or <c>null</c> if alignment was unsuccessful.</returns>
    public static T TryAlignToInterface<T>(this object obj, bool useAppDomainAssemblies, params string[] refAssemblies) where T : class
    {
        if (useAppDomainAssemblies)
            refAssemblies = CSSUtils.GetAppDomainAssemblies().Concat(refAssemblies).ToArray();

        return CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj, refAssemblies);
    }

    /// <summary>
    /// Aligns (pseudo typecasts) object to interface.
    /// <para>The object does not necessarily need to implement the interface formally.</para>
    /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Interface definition to align with.</typeparam>
    /// <param name="obj">The object to be aligned with the interface.</param>
    /// <param name="useAppDomainAssemblies">If set to <c>true</c> uses all non-GAC loaded assemblies of the current <see cref="System.AppDomain"/>
    /// when emitting (compiling) aligned proxy object.</param>
    /// <returns>Interface object.</returns>
    public static T AlignToInterface<T>(this object obj, bool useAppDomainAssemblies) where T : class
    {
        var retval = obj.TryAlignToInterface<T>(useAppDomainAssemblies);

        if (retval == null)
            throw new ApplicationException("The object (" + obj + ") cannot be aligned to " + typeof(T) + " interface.");

        return retval;
    }

    /// <summary>
    /// Aligns (pseudo typecasts) object to interface.
    /// <para>The object does not necessarily need to implement the interface formally.</para>
    /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Interface definition to align with.</typeparam>
    /// <param name="obj">The object to be aligned with the interface.</param>
    /// <param name="refAssemblies">The string array containing file names to the additional dependency
    /// assemblies the interface depends in. </param>
    /// <returns>Interface object.</returns>
    public static T AlignToInterface<T>(this object obj, params string[] refAssemblies) where T : class
    {
        var retval = obj.TryAlignToInterface<T>(refAssemblies);

        if (retval == null)
            throw new ApplicationException("The object (" + obj + ") cannot be aligned to " + typeof(T) + " interface.");

        return retval;
    }

    /// <summary>
    /// Aligns (pseudo typecasts) object to interface.
    /// <para>The object does not necessarily need to implement the interface formally.</para>
    /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Interface definition to align with.</typeparam>
    /// <param name="obj">The object to be aligned with the interface.</param>
    /// <param name="useAppDomainAssemblies">If set to <c>true</c> uses all non-GAC loaded assemblies of the current <see cref="System.AppDomain"/>
    /// when emitting (compiling) aligned proxy object.</param>
    /// <param name="refAssemblies">The string array containing file names to the additional dependency
    /// assemblies the interface depends in. </param>
    /// <returns>Interface object.</returns>
    public static T AlignToInterface<T>(this object obj, bool useAppDomainAssemblies, params string[] refAssemblies) where T : class
    {
        var retval = obj.TryAlignToInterface<T>(useAppDomainAssemblies, refAssemblies);

        if (retval == null)
            throw new ApplicationException("The object (" + obj + ") cannot be aligned to " + typeof(T) + " interface.");

        return retval;
    }

    /// <summary>
    /// Aligns (pseudo typecasts) object to interface.
    /// <para>The object does not necessarily need to implement the interface formally.</para>
    /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
    /// </summary>
    /// <typeparam name="T">Interface definition to align with.</typeparam>
    /// <param name="obj">The object to be aligned with the interface.</param>
    /// <returns>Interface object.</returns>
    public static T AlignToInterface<T>(this object obj) where T : class
    {
        if (obj == null)
            return null;

        var retval = obj.TryAlignToInterface<T>();

        if (retval == null)
            throw new ApplicationException("The object (" + obj + ") cannot be aligned to " + typeof(T) + " interface.");

        return retval;
    }
}

namespace CSScriptLibrary
{
    /// <summary>
    /// Delegate which is used as a return type for AsmHelper.GetMethodInvoker().
    ///
    /// AsmHelper.GetMethodInvoker() allows obtaining dynamic method delegate emitted on the base of the MethodInfo (from the compiled script type).
    /// </summary>
    /// <param name="instance">Instance of the type which method is to be invoked.</param>
    /// <param name="paramters">Optional method parameters.</param>
    /// <returns>Returns MethodInfo return value</returns>
    public delegate object FastInvokeDelegate(object instance, params object[] paramters);

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
    /// Helper class to simplify working with dynamically loaded assemblies.
    /// </summary>
    public class AsmHelper : IDisposable
    {
        IAsmBrowser asmBrowser;
        AppDomain remoteAppDomain;
        bool deleteOnExit = false;

        /// <summary>
        /// Aligns (pseudo typecasts) object to the specified interface.
        /// <para>The object does not necessarily need to implement the interface formally.</para>
        /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
        /// <remarks>
        /// The important difference between this method being called from <see cref="AsmHelper"/> working
        /// with the assembly in current and remote <see cref="AppDomain"/> is that is that the actual
        /// interface alignment is performed in the corresponding <see cref="AppDomain"/>.
        /// </remarks>
        /// </summary>
        /// <typeparam name="T">Interface definition to align with.</typeparam>
        /// <param name="obj">The object to be aligned with the interface.</param>
        /// <returns>Interface object.</returns>
        public T AlignToInterface<T>(object obj) where T : class
        {
            return this.asmBrowser.AlignToInterface<T>(obj);
        }

        /// <summary>
        /// Aligns (pseudo typecasts) object to the specified interface.
        /// <para>The object does not necessarily need to implement the interface formally.</para>
        /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
        /// <remarks>
        /// The important difference between this method being called from <see cref="AsmHelper"/> working
        /// with the assembly in current and remote <see cref="AppDomain"/> is that is that the actual
        /// interface alignment is performed in the corresponding <see cref="AppDomain"/>.
        /// </remarks>
        /// </summary>
        /// <typeparam name="T">Interface definition to align with.</typeparam>
        /// <param name="obj">The object to be aligned with the interface.</param>
        /// <param name="refAssemblies">The string array containing file names to the additional dependency
        /// assemblies the interface depends in. </param>
        /// <returns>Interface object.</returns>
        public T AlignToInterface<T>(object obj, params string[] refAssemblies) where T : class
        {
            return this.asmBrowser.AlignToInterface<T>(obj, refAssemblies);
        }

        /// <summary>
        /// Aligns (pseudo typecasts) object to the specified interface.
        /// <para>The object does not necessarily need to implement the interface formally.</para>
        /// <para>See <see cref="CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster{T}"/>.</para>
        /// <remarks>
        /// The important difference between this method being called from <see cref="AsmHelper"/> working
        /// with the assembly in current and remote <see cref="AppDomain"/> is that is that the actual
        /// interface alignment is performed in the corresponding <see cref="AppDomain"/>.
        /// </remarks>
        /// </summary>
        /// <typeparam name="T">Interface definition to align with.</typeparam>
        /// <param name="obj">The object to be aligned with the interface.</param>
        /// <param name="useAppDomainAssemblies">If set to <c>true</c> uses all loaded assemblies of the current <see cref="System.AppDomain"/></param>
        /// <returns>Interface object.</returns>
        public T AlignToInterface<T>(object obj, bool useAppDomainAssemblies) where T : class
        {
            return this.asmBrowser.AlignToInterface<T>(obj, useAppDomainAssemblies);
        }

        /// <summary>
        /// Creates object in remote or current <see cref="AppDomain"/> and aligns (pseudo typecasts) it to the specified interface.
        /// <para>Semantically it is an equivalent of calling
        /// <code>asmHelper.AlignToInterface(asmHelper.CreateObject(typeName))</code>
        /// </para>
        /// </summary>
        /// <typeparam name="T">Interface definition to align with.</typeparam>
        /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
        /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>Interface object.</returns>
        public T CreateAndAlignToInterface<T>(string typeName, params object[] args) where T : class
        {
            return this.asmBrowser.AlignToInterface<T>(this.CreateObject(typeName, args));
        }

        /// <summary>
        /// Instance of the AppDomain, which is used to execute the script.
        /// </summary>
        public AppDomain ScriptExecutionDomain
        {
            get { return remoteAppDomain != null ? remoteAppDomain : AppDomain.CurrentDomain; }
        }

        /// <summary>
        /// Reference to the <see cref="CSScriptLibrary.AsmHelper"/> "worker" object created in the remote AppDomain. This property is null
        /// unless <see cref="CSScriptLibrary.AsmHelper"/> was instantiated for the <c>remote execution</c> scenario.
        /// <para>This property can be useful when you need to access the remote object in order to manage the "life time" of the AsmHelper in
        /// Remoting and WCF scenarios.</para>
        /// </summary>
        /// <example>
        /// <code>
        /// var scriptHelper = new AsmHelper(scriptAsmFile, null, true);
        /// scriptHelper.RemoteObject.ExtendLifeFromMinutes(30);
        /// </code>
        /// </example>
        public MarshalByRefObject RemoteObject
        {
            get { return this.asmBrowser as AsmRemoteBrowser; }
        }

        /// <summary>
        /// Flag that indicates if method caching is enabled. It is set to true by default.
        /// <para></para>
        /// When caching is enabled AsmHelper generates (emits) extremely fast delegates for
        /// the methods being invoked. If AsmHelper is in cache mode it performs more than twice faster.
        /// However generation of the delegate does take some time that is why you may consider
        /// switching caching off if the method is to be invoked only once.
        /// </summary>
        public bool CachingEnabled
        {
            get { return this.asmBrowser.CachingEnabled; }
            set { this.asmBrowser.CachingEnabled = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to unpack <c>TargetInvocationException</c>s.
        /// <para>Script are often invoked via Reflection with <see cref="AsmHelper"/>, what leads to the script internal runrtime exceptions to
        /// be wrapped into noninformative <see cref="TargetInvocationException"/>. Thus <see cref="AsmHelper"/> by default unwraps the internal exception
        /// and rethrows it withour an additional <see cref="TargetInvocationException"/> container.
        /// </para>
        /// <para>While this is a more convenient approach, sometimes it may be required to investigate the Reflection callstack. Then you will need
        /// to suppress <see cref="TargetInvocationException"/> unpacking with this very property.</para>
        /// </summary>
        /// <value>
        ///   <c>true</c> if unpack <see cref="TargetInvocationException"/>; otherwise, <c>false</c>.
        /// </value>
        public static bool UnpackTargetInvocationExceptions
        {
            // need to use EnvVar as this property can be accessed from the different AppDomain and static
            // members are maintained per domain (not per process)
            get { return Environment.GetEnvironmentVariable("CSSCRIPT_SuppressUnpackingTargetInvocationExceptions") != "true"; }
            set { Environment.SetEnvironmentVariable("CSSCRIPT_SuppressUnpackingTargetInvocationExceptions", value ? null : "true"); }
        }

        /// <summary>
        /// This method returns extremely fast delegate for the method specified by "methodName" and
        /// method arguments "list". Invoking such delegate is ~100 times faster than invoking with pure reflection
        /// (MethodInfo.Invoke()).
        /// </summary>
        /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowd to use wild
        /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or "*.*").</param>
        /// <param name="list">List of 'Method' arguments.
        /// Note that values of the items in the list do not have any importance. The type of the list item is
        /// to be used for method search. For example if class Calc has method Sum(int a, int b) the method invoker
        /// can be obtained as following:
        /// <para></para>
        /// GetMethodInvoker("Calc.Sum", 0, 0)
        /// <para></para>
        /// You can pass any integer as the second and third parameter because it will be used only to obtain the
        /// information about the parameter type (in this case System.Int32).</param>
        /// <returns>Returns delegate of CSScriptLibrary.FastInvokeDelegate type.</returns>
        public FastInvokeDelegate GetMethodInvoker(string methodName, params object[] list)
        {
            return this.asmBrowser.GetMethodInvoker(methodName, list);
        }

        /// <summary>
        /// Specialized version of GetMethodInvoker which returns MethodDelegate thus you do not need to specify
        /// object instance (null) when calling static methods.
        /// </summary>
        /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
        /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or "*.*").</param>
        /// <param name="list">List of 'Method' arguments. </param>
        /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
        /// <remarks>
        /// <para>
        /// <para>
        /// Note that values of the items in the list do not have any importance. The type of the list item is
        /// to be used for method search. For example if class Calc has method Sum(int a, int b) the method invoker
        /// can be obtained as following:
        /// </para>
        /// </para>
        /// <example>
        /// <code>GetStaticMethod("Calc.Sum", 0, 0)</code>
        /// </example>
        /// <para>
        /// You can pass any integer as the second and third parameter because it will be used only to obtain the
        /// information about the parameter type (in this case System.Int32).
        /// </para>
        /// </remarks>
        public MethodDelegate GetStaticMethod(string methodName, params object[] list)
        {
            FastInvokeDelegate method = this.asmBrowser.GetMethodInvoker(methodName, list);
            return delegate (object[] paramters) { return method(null, paramters); };
        }

        /// <summary>
        /// Specialized version of GetMethodInvoker which returns MethodDelegate thus you do not need to specify
        /// object instance (null) when calling static methods.
        /// </summary>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
        /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or "*.*").</param>
        /// <param name="list">List of 'Method' arguments. </param>
        /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
        /// <remarks>
        /// <para>
        /// <para>
        /// Note that values of the items in the list do not have any importance. The type of the list item is
        /// to be used for method search. For example if class Calc has method Sum(int a, int b) the method invoker
        /// can be obtained as following:
        /// </para>
        /// </para>
        /// <example>
        /// <code>GetStaticMethod("Calc.Sum", 0, 0)</code>
        /// </example>
        /// <para>
        /// You can pass any integer as the second and third parameter because it will be used only to obtain the
        /// information about the parameter type (in this case System.Int32).
        /// </para>
        /// </remarks>
        public MethodDelegate<T> GetStaticMethod<T>(string methodName, params object[] list)
        {
            FastInvokeDelegate method = this.asmBrowser.GetMethodInvoker(methodName, list);
            return delegate (object[] paramters) { return (T)method(null, paramters); };
        }

        /// <summary>
        /// Specialized version of GetMethodInvoker which returns MethodDelegate thus you do not need to specify
        /// object instance (null) when calling static methods.
        /// </summary>
        /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
        /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or "*.*").</param>
        /// <param name="list">List of 'Method' arguments. </param>
        /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
        public MethodDelegate GetStaticMethodWithArgs(string methodName, params Type[] list)
        {
            FastInvokeDelegate method = this.asmBrowser.GetMethodInvoker(methodName, list);
            return delegate (object[] paramters) { return method(null, paramters); };
        }

        /// <summary>
        /// Specialized version of GetMethodInvoker which returns MethodDelegate thus you do not need to specify
        /// object instance (null) when calling static methods.
        /// </summary>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
        /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or "*.*").</param>
        /// <param name="list">List of 'Method' arguments. </param>
        /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
        public MethodDelegate<T> GetStaticMethodWithArgs<T>(string methodName, params Type[] list)
        {
            FastInvokeDelegate method = this.asmBrowser.GetMethodInvoker(methodName, list);
            return delegate (object[] paramters) { return (T)method(null, paramters); };
        }

        /// <summary>
        /// Specialized version of GetMethodInvoker which returns MethodDelegate of the very first method found in the
        /// underlying assembly. This method is an overloaded implementation of the GetStaticMethod(string methodName, params object[] list).
        /// <para>
        /// Use this method when script assembly contains only one single type with one method.
        /// </para>
        /// </summary>
        /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
        public MethodDelegate GetStaticMethod()
        {
            FastInvokeDelegate method = this.asmBrowser.GetMethodInvoker("*.*");
            return delegate (object[] paramters) { return method(null, paramters); };
        }

        /// <summary>
        /// Specialized version of GetMethodInvoker which returns MethodDelegate of the very first method found in the
        /// underlying assembly. This method is an overloaded implementation of the GetStaticMethod(string methodName, params object[] list).
        /// <para>
        /// Use this method when script assembly contains only one single type with one method.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
        public MethodDelegate<T> GetStaticMethod<T>()
        {
            FastInvokeDelegate method = this.asmBrowser.GetMethodInvoker("*.*");
            return delegate (object[] paramters) { return (T)method(null, paramters); };
        }

        /// <summary>
        /// Specialized version of GetMethodInvoker which returns MethodDelegate thus you do not need to specify
        /// object instance when calling instance methods as delegate will maintain the instance object internally.
        /// </summary>
        /// <param name="instance">Instance of the type, which implements method is to be wrapped by MethodDelegate.</param>
        /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
        /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or "*.*").</param>
        /// <param name="list">List of 'Method' arguments.
        /// <para>
        /// Note that values of the items in the list do not have any importance. The type of the list item is
        /// to be used for method search. For example if class Calc has method Sum(int a, int b) the method invoker
        /// can be obtained as following:
        /// <code>
        /// GetMethod(instance, "Sum", 0, 0)
        /// </code>
        /// You can pass any integer as the second and third parameter because it will be used only to obtain the
        /// information about the parameter type (in this case System.Int32).
        /// </para>
        /// </param>
        /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
        public MethodDelegate GetMethod(object instance, string methodName, params object[] list)
        {
            FastInvokeDelegate method = this.asmBrowser.GetMethodInvoker(instance.GetType().FullName + "." + methodName, list);
            return delegate (object[] paramters) { return method(instance, paramters); };
        }

        /// <summary>
        /// Specialized version of GetMethodInvoker which returns MethodDelegate thus you do not need to specify
        /// object instance when calling instance methods as delegate will maintain the instance object internally.
        /// </summary>
        /// <typeparam name="T">The delegate return type.</typeparam>
        /// <param name="instance">Instance of the type, which implements method is to be wrapped by MethodDelegate.</param>
        /// <param name="methodName">'Method' name including 'Type' name (eg. MyType.DoJob). It is allowed to use wild
        /// card character to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or "*.*").</param>
        /// <param name="list">List of 'Method' arguments.
        /// <para>
        /// Note that values of the items in the list do not have any importance. The type of the list item is
        /// to be used for method search. For example if class Calc has method Sum(int a, int b) the method invoker
        /// can be obtained as following:
        /// <code>
        /// GetMethod(instance, "Sum", 0, 0)
        /// </code>
        /// You can pass any integer as the second and third parameter because it will be used only to obtain the
        /// information about the parameter type (in this case System.Int32).
        /// </para>
        /// </param>
        /// <returns>Returns delegate of CSScriptLibrary.MethodDelegate type.</returns>
        public MethodDelegate<T> GetMethod<T>(object instance, string methodName, params object[] list)
        {
            FastInvokeDelegate method = this.asmBrowser.GetMethodInvoker(instance.GetType().FullName + "." + methodName, list);
            return delegate (object[] paramters) { return (T)method(instance, paramters); };
        }

        /// <summary>
        /// Creates an instance of AsmHelper for working with assembly dynamically loaded to current AppDomain.
        /// Calling "Dispose" is optional for "current AppDomain"scenario as no new AppDomain will be ever created.
        /// </summary>
        /// <param name="asm">Assembly object.</param>
        public AsmHelper(Assembly asm)
        {
            this.asmBrowser = (IAsmBrowser)(new AsmBrowser(asm));
            InitProbingDirs();
        }

        /// <summary>
        /// Creates an instance of AsmHelper for working with assembly dynamically loaded to non-current AppDomain.
        /// This method initializes instance and creates new ('remote') AppDomain with 'domainName' name. New AppDomain is automatically unloaded as result of "disposable" behaviour of AsmHelper.
        /// </summary>
        /// <param name="asmPath">The fully qualified path of the assembly file to load.</param>
        /// <param name="domainName">Name of the domain to be created.</param>
        /// <param name="deleteOnExit">'true' if assembly file should be deleted when new AppDomain is unloaded; otherwise, 'false'.</param>
        public AsmHelper(string asmPath, string domainName, bool deleteOnExit)
        {
            this.deleteOnExit = deleteOnExit;
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = Path.GetDirectoryName(asmPath);
            setup.PrivateBinPath = AppDomain.CurrentDomain.BaseDirectory;

            //setup.ConfigurationFile = AppDomain.CurrentDomain.?? AppDomain does not allow easy access to the config file name
            setup.ApplicationName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            setup.ShadowCopyFiles = "true";
            setup.ShadowCopyDirectories = Path.GetDirectoryName(asmPath);
            remoteAppDomain = AppDomain.CreateDomain(domainName != null ? domainName : "", null, setup);

            //Assembly.LoadFile
            AsmRemoteBrowser asmBrowser = (AsmRemoteBrowser)remoteAppDomain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().Location, typeof(AsmRemoteBrowser).ToString());
            asmBrowser.AsmFile = asmPath;
            asmBrowser.CustomHashing = ExecuteOptions.options.customHashing;
            this.asmBrowser = (IAsmBrowser)asmBrowser;

            InitProbingDirs();
        }

        /// <summary>
        /// Executes static method of the underlying assembly.
        /// </summary>
        /// <param name="methodName">'Method' name including 'Type' name (e.g. MyType.DoJob). It is allowed to use wild card character
        /// to indicate that the Type name of the method is irrelevant (e.g. "*.Method").</param>
        /// <param name="list">List of 'Method' arguments.</param>
        /// <returns>Returns object of the same type as 'Method' return type.</returns>
        public object Invoke(string methodName, params object[] list)
        {
            if (this.disposed)
                throw new ObjectDisposedException(this.ToString());
            return asmBrowser.Invoke(methodName, list);
        }

        /// <summary>
        /// Executes an instance method of the underlying assembly.
        /// </summary>
        /// <param name="obj">Instance of the object whose method is to be invoked.</param>
        /// <param name="methodName">'Method' name (excluding 'Type' name). It is allowed to use wild card character
        /// to indicate that the Type name of the method is irrelevant (e.g. "*.Method" or even "*.*").</param>
        /// <param name="list">List of 'Method' arguments.</param>
        /// <returns>Returns object of the same type as 'Method' return type.</returns>
        public object InvokeInst(object obj, string methodName, params object[] list)
        {
            if (this.disposed)
                throw new ObjectDisposedException(this.ToString());
            return asmBrowser.Invoke(obj, methodName, list);
        }

        /// <summary>
        /// Gets the information about the object members.
        /// <p>This is an extremely light way of getting Reflection information of a given object. Object can be either local one
        /// or a TransparentProxy of the object instantiated in a remote AppDomain.</p>
        /// <remarks>Note: Because none of the MemberInfo derivatives is serializable. This makes it impossible to use Reflection
        /// for discovering the type members instantiated in the different AppDomains. And the TransparentProxies do not provide any marshaled
        /// Reflection API neither.
        /// </remarks>
        /// <example>
        /// <code>
        /// using (var helper = new AsmHelper(CSScript.CompileCode(code), null, true))
        /// {
        ///     var script = helper.CreateObject("Script"); //script code has 'class Script' declared
        ///     foreach (string info in helper.GetMembersOf(script))
        ///         Debug.WriteLine(info);
        /// }
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>Collection of strings with each item representing human readable information about the Type member.
        /// <para>
        /// <c>Type implementation:</c><code>
        /// class Script
        /// {
        ///     public void SayHello(string greeting)
        ///     {
        ///         ...
        /// </code>
        /// </para>
        /// <para>
        /// <para><c>Type member information:</c> </para>
        /// "MemberType:Method;Name:SayHello;DeclaringType:Script;Signature:Void SayHello(System.String)"
        /// </para>
        /// </returns>
        public string[] GetMembersOf(object obj)
        {
            return asmBrowser.GetMembersOf(obj);
        }

        /// <summary>
        /// Attempts to create instance of a class from underlying assembly.
        /// </summary>
        /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
        /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>Instance of the 'Type'. Returns null if the instance cannot be created.</returns>
        public object TryCreateObject(string typeName, params object[] args)
        {
            if (this.disposed)
                throw new ObjectDisposedException(this.ToString());
            return asmBrowser.CreateInstance(typeName, args);
        }

        /// <summary>
        /// Creates instance of a class from underlying assembly.
        /// </summary>
        /// <param name="typeName">The 'Type' full name of the type to create. (see Assembly.CreateInstance()).
        /// You can use wild card meaning the first type found. However only full wild card "*" is supported.</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>Instance of the 'Type'. Throws an ApplicationException if the instance cannot be created.</returns>
        public object CreateObject(string typeName, params object[] args)
        {
            object retval = TryCreateObject(typeName, args);
            if (retval == null)
                throw new ApplicationException(typeName + " cannot be instantiated. Make sure the type name is correct.");
            else
                return retval;
        }

        /// <summary>
        /// Unloads 'remote' AppDomain if it was created.
        /// </summary>
        void Unload()
        {
            try
            {
                if (remoteAppDomain != null)
                {
                    string asmFile = ((AsmRemoteBrowser)this.asmBrowser).AsmFile;
                    AppDomain.Unload(remoteAppDomain);
                    remoteAppDomain = null;
                    if (deleteOnExit)
                    {
                        Utils.FileDelete(asmFile);
                    }
                }
            }
            catch
            {
                //ignore exception as it is possible that we are trying to unload AppDomain
                //during the object finalization (which is illegal).
            }
        }

        /// <summary>
        /// Implementation of IDisposable.Dispose(). Disposes allocated external resources if any. Call this method to unload non-current AppDomain (if it was created).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Actual implementation of IDisposable.Dispose()
        /// </summary>
        /// <param name="disposing">'false' if the method has been called by the runtime from inside the finalizer ; otherwise, 'true'.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (asmBrowser != null)
                    try { asmBrowser.Dispose(); }
                    catch { } // Dispose should never throw no matter what
                Unload();
            }
            disposed = true;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~AsmHelper()
        {
            Dispose(false);
        }

        bool disposed = false;

        /// <summary>
        ///Array of directories to be used for assembly probing.
        /// </summary>
        public string[] ProbingDirs
        {
            get { return this.asmBrowser.ProbingDirs; }
            set { this.asmBrowser.ProbingDirs = value; }
        }

        void InitProbingDirs()
        {
            ArrayList dirs = new ArrayList();
            if (CSScript.AssemblyResolvingEnabled && CSScript.ShareHostRefAssemblies)
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    try
                    {
                        string location = asm.Location();
                        if (location == "" || !File.Exists(location))
                            continue;

                        dirs.Add(Path.GetDirectoryName(location));
                    }
                    catch
                    {
                        //Under ASP.NET some assemblies do not have location (e.g. dynamically built/emitted assemblies)
                        //in such case NotSupportedException will be raised

                        //In fact ignore all exceptions as we should continue if for whatever reason the assembly location cannot be obtained
                    }

            if (CSScript.AssemblyResolvingEnabled)
                foreach (string dir in CSScript.GlobalSettings.SearchDirs.Split(';'))
                    if (dir != "")
                        dirs.Add(Environment.ExpandEnvironmentVariables(dir));

            ProbingDirs = CSScript.RemovePathDuplicates((string[])dirs.ToArray(typeof(string)));
        }
    }

    /// <summary>
    /// Defines method for calling assembly methods and instantiating assembly types.
    /// </summary>
    interface IAsmBrowser : IDisposable
    {
        object Invoke(string methodName, params object[] list);

        object Invoke(object obj, string methodName, params object[] list);

        object CreateInstance(string typeName, params object[] args);

        string[] ProbingDirs { get; set; }

        bool CachingEnabled { get; set; }

        T AlignToInterface<T>(object obj) where T : class;

        T AlignToInterface<T>(object obj, bool useAppDomainAssemblies) where T : class;

        T AlignToInterface<T>(object obj, params string[] refAssemblies) where T : class;

        FastInvokeDelegate GetMethodInvoker(string methodName, object[] list);

        FastInvokeDelegate GetMethodInvoker(string methodName, Type[] list);

        FastInvokeDelegate GetMethodInvoker(string methodName);

        string[] GetMembersOf(object obj);
    }

    internal class AsmRemoteBrowser : MarshalByRefObjectWithInfiniteLifetime, IAsmBrowser
    {
        string workingDir;

        AsmBrowser asmBrowser;

        // https://github.com/oleg-shilo/cs-script/issues/98
        // public override object InitializeLifetimeService()
        // {
        //     var lease = (ILease)base.InitializeLifetimeService();
        //     if (lease.CurrentState == LeaseState.Initial)
        //         lease.InitialLeaseTime = TimeSpan.Zero;

        //     return (lease);
        // }

        public AsmRemoteBrowser()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveEventHandler);
        }

        public AsmBrowser AsmBrowser
        {
            get
            {
                if (AsmFile == null)
                    throw new ApplicationException("Assembly name (asmFile) was not set");

                return asmBrowser;
            }
        }

        public void Dispose()
        {
            if (asmBrowser != null)
                try { asmBrowser.Dispose(); }
                catch { } // Dispose should never throw no matter what

            AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(ResolveEventHandler);
        }

        bool customHashing;

        internal bool CustomHashing
        {
            get { return customHashing; }
            set
            {
                customHashing = value;
                ExecuteOptions.options.customHashing = value;
            }
        }

        string asmFile;

        public string AsmFile
        {
            get
            {
                return asmFile;
            }
            set
            {
                asmFile = value;
                workingDir = Path.GetDirectoryName(AsmFile);
                asmBrowser = new AsmBrowser(Assembly.LoadFrom(AsmFile));
                asmBrowser.CachingEnabled = cachingEnabled;
            }
        }

        public string[] ProbingDirs
        {
            get { return probingDirs; }
            set { probingDirs = value; }
        }

        string[] probingDirs = new string[] { };

        public bool CachingEnabled
        {
            get { return cachingEnabled; }
            set { cachingEnabled = value; if (asmBrowser != null) asmBrowser.CachingEnabled = cachingEnabled; }
        }

        bool cachingEnabled = true;

        public FastInvokeDelegate GetMethodInvoker(string methodName)
        {
            return this.AsmBrowser.GetMethodInvoker(methodName, new Type[0]);
        }

        public FastInvokeDelegate GetMethodInvoker(string methodName, object[] list)
        {
            return this.AsmBrowser.GetMethodInvoker(methodName, list);
        }

        public FastInvokeDelegate GetMethodInvoker(string methodName, Type[] list)
        {
            return this.AsmBrowser.GetMethodInvoker(methodName, list);
        }

        Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly retval = AssemblyResolver.ResolveAssembly(args.Name, workingDir, false);
            if (retval == null)
                foreach (string dir in probingDirs)
                    if (null != (retval = AssemblyResolver.ResolveAssembly(args.Name, Environment.ExpandEnvironmentVariables(dir), false)))
                        break;

            return retval;
        }

        public object Invoke(string methodName, params object[] list)
        {
            return this.AsmBrowser.Invoke(methodName, list);
        }

        public object Invoke(object obj, string methodName, params object[] list)
        {
            return this.AsmBrowser.Invoke(obj, methodName, list);
        }

        //creates instance of a Type from underlying assembly
        public object CreateInstance(string typeName, params object[] args)
        {
            if (asmBrowser == null)
            {
                if (AsmFile == null)
                    throw new ApplicationException("Assembly name (asmFile) was not set");

                workingDir = Path.GetDirectoryName(AsmFile);
                asmBrowser = new AsmBrowser(Assembly.LoadFrom(AsmFile));
            }
            return asmBrowser.CreateInstance(typeName, args);
        }

        public T AlignToInterface<T>(object obj) where T : class
        {
            var retval = CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj);

            if (retval == null)
                throw new ApplicationException("The object cannot be aligned to this interface.");

            return retval;
        }

        public T AlignToInterface<T>(object obj, bool useAppDomainAssemblies) where T : class
        {
            string[] refAssemblies;
            if (useAppDomainAssemblies)
                refAssemblies = CSSUtils.GetAppDomainAssemblies();
            else
                refAssemblies = new string[0];

            var retval = CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj, refAssemblies);

            if (retval == null)
                throw new ApplicationException("The object cannot be aligned to this interface.");

            return retval;
        }

        public T AlignToInterface<T>(object obj, params string[] refAssemblies) where T : class
        {
            var retval = CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj, refAssemblies);

            if (retval == null)
                throw new ApplicationException("The object cannot be aligned to this interface.");

            return retval;
        }

        public string[] GetMembersOf(object obj)
        {
            return Reflector.GetMembersOf(obj);
        }
    }

    internal class Reflector
    {
        public static string[] GetMembersOf(object obj)
        {
            List<string> retval = new List<string>();

            foreach (MemberInfo info in obj.GetType().GetMembers())
                retval.Add(string.Format("MemberType:{0};Name:{1};DeclaringType:{2};Signature:{3}",
                                  info.MemberType, info.Name, info.DeclaringType.FullName, info.ToString()));

            return retval.ToArray();
        }
    }

    internal class AsmBrowser : IAsmBrowser
    {
        public string[] GetMembersOf(object obj)
        {
            return Reflector.GetMembersOf(obj);
        }

        string workingDir;

        Dictionary<MethodInfo, FastInvoker> methodCache = new Dictionary<MethodInfo, FastInvoker>(); //cached delegates of the type methods
        Dictionary<MethodSignature, MethodInfo> infoCache = new Dictionary<MethodSignature, MethodInfo>(); //cached MethodInfo(f) of the type method(s)

        Assembly asm;

        public AsmBrowser(Assembly asm)
        {
            if (asm == null)
                throw new ArgumentNullException("asm");

            if (ExecuteOptions.options == null)
                ExecuteOptions.options = new ExecuteOptions();

            this.asm = asm;

            try
            {
                if (!asm.IsDynamic())
                    workingDir = asm.GetAssemblyDirectoryName();
            }
            catch { }
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveEventHandler);
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(ResolveEventHandler);
        }

        public string[] ProbingDirs
        {
            get { return probingDirs; }
            set { probingDirs = value; }
        }

        string[] probingDirs = new string[] { };

        public bool CachingEnabled
        {
            get { return cachingEnabled; }
            set { cachingEnabled = value; }
        }

        bool cachingEnabled = true;

        Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly retval = AssemblyResolver.ResolveAssembly(args.Name, workingDir, false);
            if (retval == null)
                foreach (string dir in probingDirs)
                    if (null != (retval = AssemblyResolver.ResolveAssembly(args.Name, Environment.ExpandEnvironmentVariables(dir), false)))
                        break;

            return retval;
        }

        //executes static method of underlying assembly
        public object Invoke(string methodName, params object[] list)
        {
            return Invoke((object)null, methodName, list);
        }

        struct MethodSignature
        {
            public MethodSignature(string name, params object[] args)
            {
                this.name = name;
                this.parameters = new Type[args.Length];
                for (int i = 0; i < this.parameters.Length; i++)
                    this.parameters[i] = args[i].GetType();
            }

            public string name;
            public Type[] parameters;

            public static bool operator ==(MethodSignature x, MethodSignature y)
            {
                return x.Equals(y);
            }

            public static bool operator !=(MethodSignature x, MethodSignature y)
            {
                return !x.Equals(y);
            }

            public override bool Equals(object obj)
            {
                MethodSignature sig = (MethodSignature)obj;
                if (this.name != sig.name)
                    return false;
                if (this.parameters.Length != sig.parameters.Length)
                    return false;
                for (int i = 0; i < this.parameters.Length; i++)
                    if (this.parameters[i] != sig.parameters[i])
                        return false;

                return true;
            }

            public override int GetHashCode()
            {
                StringBuilder sb = new StringBuilder(name);

                foreach (Type param in parameters)
                    sb.Append(param.ToString());

                return CSSUtils.GetHashCodeEx(sb.ToString());
            }
        }

        //executes instance method of underlying assembly
        public object Invoke(object obj, string methodName, params object[] list)
        {
            if (list == null)
            {
                list = new object[] { null };
            }

            string[] names = methodName.Split(".".ToCharArray());
            if (names.Length < 2 && obj != null)
                methodName = obj.GetType().FullName + "." + methodName;

            if (list.Any(x => x == null))
                throw new Exception("At least one of the invoke parameters is null. This makes impossible to " +
                                    "match method signature by the parameter type. Consider using alternative invoking " +
                                    "mechanisms like:" + Environment.NewLine +
                                    " AsmHelper.GetMethod()" + Environment.NewLine +
                                    " AsmHelper.GetStaticMethod()" + Environment.NewLine +
                                    " Assembly.GetStaticMethod()" + Environment.NewLine +
                                    " using type 'dynamic'" + Environment.NewLine +
                                    " using interfaces");

            MethodSignature methodID = new MethodSignature(methodName, list);
            MethodInfo method;
            if (!infoCache.ContainsKey(methodID))
            {
                method = FindMethod(methodName, list);
                infoCache[methodID] = method;
            }
            else
            {
                method = infoCache[methodID];
            }

            try
            {
                if (cachingEnabled)
                {
                    if (!methodCache.ContainsKey(method))
                        methodCache[method] = new FastInvoker(method);

                    return methodCache[method].Invoke(obj, list);
                }
                else
                    return method.Invoke(obj, list);
            }
            catch (TargetInvocationException ex)
            {
                if (AsmHelper.UnpackTargetInvocationExceptions && ex.InnerException != null)
                    throw ex.InnerException; //unpack the exception
                else
                    throw;
            }
        }

        MethodInfo FindMethod(string methodName, object[] list)
        {
            Type[] args = new Type[list.Length];
            for (int i = 0; i < list.Length; i++)
                args[i] = list[i].GetType();

            return FindMethod(methodName, args);
        }

        MethodInfo FindMethod(string methodName, Type[] args)
        {
            string[] names = methodName.Split(".".ToCharArray());
            if (names.Length < 2)
                throw new ApplicationException("Invalid method name format (must be: \"<type>.<method>\")");

            string methodShortName = names[names.Length - 1];
            string typeName = names[names.Length - 2];
            MethodInfo method;

            foreach (Module m in asm.GetModules())
            {
                Type[] types;
                if (names[0] == "*")
                    types = m.GetTypes();
                else
                    types = m.FindTypes(Module.FilterTypeName, names[names.Length - 2]);

                foreach (Type t in types)
                {
                    bool isMonoInternalType = (t.FullName == "<InteractiveExpressionClass>");
                    bool isRoslynInternalType = (t.FullName.StartsWith("Submission#") && !t.FullName.Contains("+"));

                    if (isMonoInternalType || isRoslynInternalType)
                        continue;

                    if (methodShortName == "*")
                    {
                        MethodInfo[] methods = t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (methods.Length != 0)
                            return methods[0]; //return the very first method
                    }
                    else if ((method = t.GetMethod(methodShortName, args)) != null)
                    {
                        if (typeName.EndsWith("*"))
                            return method;
                        else if (methodName == method.DeclaringType.FullName + "." + methodShortName)
                            return method;
                    }
                }
            }

            string msg = "Method " + methodName + "(";
            if (args.Length > 0)
            {
                foreach (Type arg in args)
                    msg += arg.ToString() + ", ";
                msg = msg.Remove(msg.Length - 2, 2);
            }
            msg += ") cannot be found.";

            throw new ApplicationException(msg);
        }

        public FastInvokeDelegate GetMethodInvoker(string methodName)
        {
            MethodInfo method = FindMethod(methodName, new Type[0]); //if method cannot be found FindMethod will throw an exception
            return GetMethodInvoker(method);
        }

        public FastInvokeDelegate GetMethodInvoker(string methodName, Type[] list)
        {
            MethodInfo method = FindMethod(methodName, list); //if method cannot be found FindMethod will throw an exception
            return GetMethodInvoker(method);
        }

        public FastInvokeDelegate GetMethodInvoker(string methodName, object[] list)
        {
            MethodInfo method = FindMethod(methodName, list); //if method cannot be found FindMethod will throw an exception
            return GetMethodInvoker(method);
        }

        FastInvokeDelegate GetMethodInvoker(MethodInfo method)
        {
            try
            {
                if (!methodCache.ContainsKey(method))
                    methodCache[method] = new FastInvoker(method);

                return methodCache[method].GetMethodInvoker();
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException != null ? ex.InnerException : ex; //unpack the exception
            }
        }

        /// <summary>
        /// Creates instance of a Type from underlying assembly.
        /// </summary>
        /// <param name="typeName">Name of the type to be instantiated. Allows wild card character (e.g. *.MyClass can be used to instantiate MyNamespace.MyClass).</param>
        /// <param name="args">The non default constructor arguments.</param>
        /// <returns>Created instance of the type.</returns>
        public object CreateInstance(string typeName, params object[] args)
        {
            if (typeName.IndexOf("*") != -1)
            {
                //note typeName for FindTypes does not include namespace
                if (typeName == "*")
                {
                    //instantiate the first type found (but not auto-generated types)
                    var type = FindFirstScriptUserType(asm);
                    if (type != null)
                        return createInstance(asm, type.FullName, args);
                    return null;
                }
                else
                {
                    Type[] types = asm.GetModules()[0].FindTypes(Module.FilterTypeName, typeName);
                    if (types.Length == 0)
                        throw new ApplicationException("Type " + typeName + " cannot be found.");
                    return createInstance(asm, types[0].FullName, args);
                }
            }
            else
                return createInstance(asm, typeName, args);
        }

        static public Type FindFirstScriptUserType(Assembly asm)
        {
            return FindFirstScriptUserType(asm, null);
        }

        static public Type FindFirstScriptUserType(Assembly asm, string typeName)
        {
            //find the first type found (but not auto-generated types)
            //Ignore Roslyn internal type: "Submission#N"; real script class will be Submission#0+Script
            foreach (Type type in asm.GetModules()[0].GetTypes())
            {
                bool isMonoInternalType = (type.FullName == "<InteractiveExpressionClass>");
                bool isLikelyUserType = (type.FullName.StartsWith("Submission#") && type.FullName.Contains("+"));
                bool isRoslynInternalType = (type.FullName.StartsWith("Submission#") && !type.FullName.Contains("+"));
                bool isRoslynInternalType2 = type.FullName.StartsWith("<"); // Latest Roslyn on .NET 4.7 host

                if (!isMonoInternalType && !isRoslynInternalType && !isRoslynInternalType2)
                {
                    if (typeName == null || type.Name == typeName || type.Name == "DynamicClass" || isLikelyUserType)
                        return type;
                }
            }
            return null;
        }

        object createInstance(Assembly asm, string typeName, object[] args)
        {
            return asm.CreateInstance(typeName, args);
        }

        public T AlignToInterface<T>(object obj) where T : class
        {
            var retval = CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj);

            if (retval == null)
                throw new ApplicationException("The object cannot be aligned to this interface.");

            return retval;
        }

        public T AlignToInterface<T>(object obj, bool useAppDomainAssemblies) where T : class
        {
            string[] refAssemblies;
            if (useAppDomainAssemblies)
                refAssemblies = CSSUtils.GetAppDomainAssemblies();
            else
                refAssemblies = new string[0];

            var retval = CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj, refAssemblies);

            if (retval == null)
                throw new ApplicationException("The object cannot be aligned to this interface.");

            return retval;
        }

        public T AlignToInterface<T>(object obj, params string[] refAssemblies) where T : class
        {
            var retval = CSScriptLibrary.ThirdpartyLibraries.Rubenhak.Utils.ObjectCaster<T>.As(obj, refAssemblies);

            if (retval == null)
                throw new ApplicationException("The object cannot be aligned to this interface.");

            return retval;
        }
    }

    /// <summary>
    /// Class which is capable of emitting the dynamic method delegate based on the MethodInfo. Such delegate is
    /// extremely fast and it can demonstrate up to 100 times better performance comparing to the pure
    /// Reflection method invokation (MethodInfo.Invoke()).
    ///
    ///
    /// Based on http://www.codeproject.com/KB/cs/FastInvokerWrapper.aspx
    /// </summary>
    public class FastInvoker
    {
        //it is almost 100 times faster than reflection (MethodInfo.Invoke())

        FastInvokeDelegate method;

        /// <summary>
        /// MethodInfo instance which was used to generate dynamic method delegate.
        /// </summary>
        public MethodInfo info;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="info">MethodInfo instance which is to be used to generate dynamic method delegate.</param>
        public FastInvoker(MethodInfo info)
        {
            this.info = info;
            method = GenerateMethodInvoker(info);
        }

        /// <summary>
        /// Invokes dynamic method delegate generated from the MethodInfo object.
        /// </summary>
        /// <param name="instance">Instance of the type which method is to be invoked.</param>
        /// <param name="paramters">Optional method parameters.</param>
        /// <returns>Invokes dynamic method delegate return value</returns>
        public object Invoke(object instance, params object[] paramters)
        {
            return method(instance, paramters);
        }

        /// <summary>
        /// Returns dynamic method delegate generated from the MethodInfo object.
        /// </summary>
        /// <returns>FastInvokeDelegate instance.</returns>
        public FastInvokeDelegate GetMethodInvoker()
        {
            return method;
        }

        FastInvokeDelegate GenerateMethodInvoker(MethodInfo methodInfo)
        {
            //if(IsNet45OrNewer)
            DynamicMethod dynamicMethod = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object), typeof(object[]) }, methodInfo.DeclaringType.Module);
            ILGenerator il = dynamicMethod.GetILGenerator();
            ParameterInfo[] ps = methodInfo.GetParameters();
            Type[] paramTypes = new Type[ps.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    paramTypes[i] = ps[i].ParameterType.GetElementType();
                else
                    paramTypes[i] = ps[i].ParameterType;
            }
            LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];

            for (int i = 0; i < paramTypes.Length; i++)
            {
                locals[i] = il.DeclareLocal(paramTypes[i], true);
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                EmitFastInt(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                EmitCastToReference(il, paramTypes[i]);
                il.Emit(OpCodes.Stloc, locals[i]);
            }
            if (!methodInfo.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
            }
            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                    il.Emit(OpCodes.Ldloca_S, locals[i]);
                else
                    il.Emit(OpCodes.Ldloc, locals[i]);
            }
            if (methodInfo.IsStatic)
                il.EmitCall(OpCodes.Call, methodInfo, null);
            else
                il.EmitCall(OpCodes.Callvirt, methodInfo, null);
            if (methodInfo.ReturnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
                EmitBoxIfNeeded(il, methodInfo.ReturnType);

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldarg_1);
                    EmitFastInt(il, i);
                    il.Emit(OpCodes.Ldloc, locals[i]);
                    if (locals[i].LocalType.IsValueType)
                        il.Emit(OpCodes.Box, locals[i].LocalType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }

            il.Emit(OpCodes.Ret);
            FastInvokeDelegate invoder = (FastInvokeDelegate)dynamicMethod.CreateDelegate(typeof(FastInvokeDelegate));
            return invoder;
        }

        static void EmitCastToReference(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, type);
            }
            else
            {
                il.Emit(OpCodes.Castclass, type);
            }
        }

        static void EmitBoxIfNeeded(ILGenerator il, System.Type type)
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }
        }

        static void EmitFastInt(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;

                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;

                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;

                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;

                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;

                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;

                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;

                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;

                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;

                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }

            if (value > -129 && value < 128)
            {
                il.Emit(OpCodes.Ldc_I4_S, (SByte)value);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, value);
            }
        }
    }
}