using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using CSScriptLibrary;

//The Sandboxer class needs to derive from MarshalByRefObject so that we can create it in another
// AppDomain and refer to it from the default AppDomain.

static class Extensions
{
    public static StrongName GetStrongName(this Assembly assembly)
    {
        return assembly.Evidence.GetHostEvidence<StrongName>();
    }

    public static AppDomain Clone(this AppDomain domain, string name, PermissionSet permissions = null, params StrongName[] fullyTrustedAddemblies)
    {
        AppDomainSetup setup = new AppDomainSetup();
        setup.ApplicationBase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        setup.PrivateBinPath = AppDomain.CurrentDomain.BaseDirectory;
        setup.ShadowCopyFiles = "true";
        setup.ShadowCopyDirectories = setup.ApplicationBase;

        if (permissions != null)
            return AppDomain.CreateDomain(name, null, setup, permissions, fullyTrustedAddemblies);
        else
            return AppDomain.CreateDomain(name, null, setup);
    }
}

class Sandboxer : MarshalByRefObject
{
    const string pathToUntrusted = @"E:\cs-script\Samples\Sandboxing\NET4.0\Host";

    static string untrustedAssembly = "UntrustedCode";
    const string untrustedClass = "UntrustedCode.UntrustedClass";
    const string entryPoint = "IsFibonacci";
    private static Object[] parameters = { 45 };

    static void Main()
    {
        //prepare assembly
        CSScript.Compile(Path.Combine(pathToUntrusted, "UntrustedCode.cs"), Path.GetFullPath(untrustedAssembly + ".dll"), true);

        //the rest is as usual
        var permSet = new PermissionSet(PermissionState.None);
        permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

        AppDomain newDomain = AppDomain.CurrentDomain.Clone("Sandbox", permSet, Assembly.GetExecutingAssembly().GetStrongName());
        

        var sandboxer = (Sandboxer)Activator.CreateInstanceFrom(newDomain, Assembly.GetExecutingAssembly().Location, typeof(Sandboxer).FullName)
                                            .Unwrap();

        sandboxer.ExecuteUntrustedCode(untrustedAssembly, untrustedClass, entryPoint, parameters);
    }

    public void ExecuteUntrustedCode(string assembly, string typeName, string entryPoint, Object[] parameters)
    {
        //Load the MethodInfo for a method in the new Assembly. This might be a method you know, or
        //you can use Assembly.EntryPoint to get to the main function in an executable.

        MethodInfo target = Assembly.Load(assembly).GetType(typeName).GetMethod(entryPoint);
        
        try
        {
            //Now invoke the method.
            bool retVal = (bool)target.Invoke(null, parameters);

        }
        catch (Exception ex)
        {
            // When we print information from a SecurityException extra information can be printed if we are
            //calling it with a full-trust stack.
            //(new PermissionSet(PermissionState.Unrestricted)).Assert();
            //Console.WriteLine("SecurityException caught:\n{0}", ex.ToString());
            //CodeAccessPermission.RevertAssert();
            Console.WriteLine("Exception caught:\n{0}", ex.InnerException.Message);
            Console.ReadLine();
        }
    }
}