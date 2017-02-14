using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using CSScriptLibrary;

class Host
{
    static public void Main(string[] args)
    {
        try
        {
            // Create a new, empty permission set so we don't mistakenly grant some permission we don't want
            PermissionSet permissionSet = new PermissionSet(PermissionState.None);

            // Set the permissions that you will allow, in this case we only want to allow execution of code
            permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

            // Make sure we have the permissions currently
            permissionSet.Demand();

            // Create the security policy level for this application domain
            PolicyLevel policyLevel = PolicyLevel.CreateAppDomainLevel();

            // Give the policy level's root code group a new policy statement based on the new permission set.
            policyLevel.RootCodeGroup.PolicyStatement = new PolicyStatement(permissionSet);

            CSScript.GlobalSettings.AddSearchDir(Environment.CurrentDirectory);

            File.Copy("Danger.cs", "Danger1.cs", true);
            var script = new AsmHelper(CSScript.Load("Danger.cs"));

            // Update the application domain's policy now
            AppDomain.CurrentDomain.SetAppDomainPolicy(policyLevel);

            var script1 = new AsmHelper(CSScript.Load("Danger1.cs"));

            Console.WriteLine();
            Console.WriteLine("Access local file from host application assembly...");
            using (FileStream f = File.Open("somefile.txt", FileMode.OpenOrCreate)) //OK because executing assembly was loaded before the new policy set
                Console.WriteLine("  Ok");
            Console.WriteLine();

            Console.WriteLine("Access local file from Script assembly (before security policy set)...");
            script.Invoke("*.SayHello"); //OK because executing assembly was loaded before the new policy set
            Console.WriteLine();

            Console.WriteLine("Access local file from Script assembly (after security policy set)...\n");
            script1.Invoke("*.SayHello"); //ERROR because executing assembly was loaded after the new policy set

            Console.WriteLine("The end...");
        }
        catch (Exception e)
        {
            Console.WriteLine();
            Console.WriteLine(e.Message);
            Console.WriteLine();
        }
    }
}