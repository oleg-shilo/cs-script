using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using System.Reflection;
using System.Runtime.Remoting;
using CSScriptLibrary;

//The Sandboxer class needs to derive from MarshalByRefObject so that we can create it in another 
// AppDomain and refer to it from the default AppDomain.
class Sandboxer : MarshalByRefObject
{
    const string pathToUntrusted = @"..\..\..\UntrustedCode\bin\Debug";
    const string untrustedAssembly = "UntrustedCode";
    const string untrustedClass = "UntrustedCode.UntrustedClass";
    const string entryPoint = "IsFibonacci";
    private static Object[] parameters = { 45 };
    static void Main()
    {
        //Setting the AppDomainSetup. It is very important to set the ApplicationBase to a folder 
        //other than the one in which the sandboxer resides.
        AppDomainSetup adSetup = new AppDomainSetup();
        adSetup.ApplicationBase = Path.GetFullPath(pathToUntrusted);

        //Setting the permissions for the AppDomain. We give the permission to execute and to 
        //read/discover the location where the untrusted code is loaded.
        PermissionSet permSet = new PermissionSet(PermissionState.None);
        permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

        //We want the sandboxer assembly's strong name, so that we can add it to the full trust list.
        StrongName fullTrustAssembly = typeof(Sandboxer).Assembly.Evidence.GetHostEvidence<StrongName>();

        //Now we have everything we need to create the AppDomain, so let's create it.
        AppDomain newDomain = AppDomain.CreateDomain("Sandbox", null, adSetup, permSet, fullTrustAssembly);

        //Use CreateInstanceFrom to load an instance of the Sandboxer class into the
        //new AppDomain. 
        ObjectHandle handle = Activator.CreateInstanceFrom(
            newDomain, typeof(Sandboxer).Assembly.ManifestModule.FullyQualifiedName,
            typeof(Sandboxer).FullName
            );
        //Unwrap the new domain instance into a reference in this domain and use it to execute the 
        //untrusted code.
        Sandboxer newDomainInstance = (Sandboxer) handle.Unwrap();
        newDomainInstance.ExecuteUntrustedCode(untrustedAssembly, untrustedClass, entryPoint, parameters);
    }
    public void ExecuteUntrustedCode(string assemblyName, string typeName, string entryPoint, Object[] parameters)
    {
        //Load the MethodInfo for a method in the new Assembly. This might be a method you know, or 
        //you can use Assembly.EntryPoint to get to the main function in an executable.
        MethodInfo target = Assembly.Load(assemblyName).GetType(typeName).GetMethod(entryPoint);
        try
        {
            //Now invoke the method.
            bool retVal = (bool)target.Invoke(null, parameters);
        }
        catch (Exception ex)
        {
            // When we print informations from a SecurityException extra information can be printed if we are 
            //calling it with a full-trust stack.
            (new PermissionSet(PermissionState.Unrestricted)).Assert();
            Console.WriteLine("SecurityException caught:\n{0}", ex.ToString());
            CodeAccessPermission.RevertAssert();
            Console.ReadLine();
        }
    }
}