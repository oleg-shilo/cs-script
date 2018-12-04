using csscript;
using CSScripting.CodeDom;

/// <summary>
/// .NET Full/Mono host (app launcher) for CS-Script engine class library assembly.
/// Runtime: .NET Framework 4.6.1
/// File name: cscs.exe
/// Example: "cscs.exe script.cs"
/// </summary>

namespace cscs.exe.full
{
    class Program
    {
        static void Main(string[] args)
        {
            CSharpCompiler.DefaultCompilerRuntime = DefaultCompilerRuntime.Standard;
            CSExecutionClient.Main(args);
        }
    }
}