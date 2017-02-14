using System;
using CSScriptLibrary;
using System.IO;
  
class CustomProbing
{
    public static void Test()
    {
        CSScript.ShareHostRefAssemblies = false; //just to ensure we are not referencing AppDoamin assemblies

        var findFile = CSScript.ResolveSourceAlgorithm;
        CSScript.ResolveSourceAlgorithm = (file, searchDirs, throwOnError) =>
            {
                if (file == "env")  
                {
                    //create source file dynamically
                    var env = Path.GetFullPath("env");
                    File.WriteAllText(env, "class Env { public static string Name = \"CS-Script Testing\"; }");
                    return env;
                }
                return findFile(file, searchDirs, throwOnError);
            };

        var findAsm = CSScript.ResolveAssemblyAlgorithm;
        CSScript.ResolveAssemblyAlgorithm = (name, searchDirs) =>
            {
                if (name == "forms")
                    return new[] { typeof(System.Windows.Forms.Form).Assembly.Location };
                else
                    return findAsm(name, searchDirs);
            };

        var code = @"//css_inc env
                     //css_ref forms
                     void SayHello(string greeting)
                     {
                         System.Windows.Forms.MessageBox.Show(greeting + "" from "" + Env.Name);
                     }";

        var sayHello = CSScript.LoadMethod(code).GetStaticMethod();

        sayHello("Hello");
    }
}
