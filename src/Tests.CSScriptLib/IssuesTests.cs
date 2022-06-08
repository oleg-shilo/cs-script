using System.IO;
using CSScriptLib;
using Xunit;

namespace Misc
{
    [Collection("Sequential")]
    public class IssuesTests
    {
        [Fact]
        public void issue_279()
        {
            // more like a playground than u-test

            // CSScript.EvaluatorConfig.Access = EvaluatorAccess.AlwaysCreate;
            // CSScript.EvaluatorConfig.DebugBuild = true;
            // CSScript.EvaluatorConfig.Engine = EvaluatorEngine.CodeDom;
            // CSScript.EvaluatorConfig.PdbFormat = Microsoft.CodeAnalysis.Emit.DebugInformationFormat.Pdb;
            // CSScript.EvaluatorConfig.ReferenceDomainAssemblies = true;
            // CSScript.EvaluatorConfig.CompilerOptions = " /preferreduilang:en-US";

            // var script = @"..\support\cs-script_#297\cs-script_.297.1\ScriptTest\Scripts\TheFirst\TheFirstScript.cs";
            // CSScript.Evaluator
            //     .ReferenceAssembly(@"..\support\cs-script_#297\cs-script_.297.1\ScriptTest\ScriptContracts\bin\Debug\ScriptContracts.dll")
            //     .LoadFile(script);
        }
    }
}