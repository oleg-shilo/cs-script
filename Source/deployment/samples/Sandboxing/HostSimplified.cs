using System;
using System.Security;
using System.Security.Permissions;
using CSScriptLibrary;

class Host
{
    static public void Main()
    {
        var CreateSomeFile = CSScript.LoadMethod(
                        @"using System.IO;
                          public static void Test()
                          {
                              try
                              {  
                                  using (var f = File.Open(""somefile.txt"", FileMode.OpenOrCreate))
                                    Console.WriteLine(""File.Open: success"");
                               }
                               catch (Exception e)
                               {
                                   Console.WriteLine(e.GetType().ToString() + "": "" + e.Message);
                               }
                          }")
                         .GetStaticMethod();

        var permissionSet = new PermissionSet(PermissionState.None);
        permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

        CreateSomeFile(); //call will succeed as as the set of permissions is a default permissions set for this assembly

        Sandbox.With(SecurityPermissionFlag.Execution) //call will fail as the set of permissions is insufficient
               .Execute(()=>CreateSomeFile());

        CreateSomeFile(); //call will succeed as as the set of permissions set back to default

        //this is a logical equivalent of Sandbox.With.Execute syntactic sugar
        ExecuteInSandbox(permissionSet,               //call will fail as the set of permissions is insufficient
                        ()=>CreateSomeFile());

        CreateSomeFile(); //call will succeed as as the set of permissions set back to default
    }

    static void ExecuteInSandbox(PermissionSet permissionSet, Action action)
    {
        permissionSet.PermitOnly();
        try
        {
            action();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.GetType().ToString() + ": " + e.Message);
        }
        finally
        {
            CodeAccessPermission.RevertPermitOnly();
        }
    }
}

