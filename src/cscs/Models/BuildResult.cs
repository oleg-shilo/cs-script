using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

// using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;
using System.Linq;

namespace csscript
{
    public class BuildResult
    {
        // using XML or JSON serializer is the best approach, though the first one  is heavy
        // and the second one requires an additional NuGet package, which even does not exist
        // for .NET Standard 2.0.
        // Thus for the temp solution like this go with a simple line serialization
        public class Diagnostic
        {
            /* Microsoft.CodeAnalysis.Diagnostic is coupled to the source tree and other CodeDOM objects
             * so need to use an adapter.
             */

            public static BuildResult.Diagnostic From(Microsoft.CodeAnalysis.Diagnostic data)
            {
                return new BuildResult.Diagnostic
                {
                    IsWarningAsError = data.IsWarningAsError,
                    Severity = data.Severity,
                    Location_IsInSource = data.Location.IsInSource,
                    Location_StartLinePosition_Line = data.Location.GetLineSpan().StartLinePosition.Line,
                    Location_StartLinePosition_Character = data.Location.GetLineSpan().StartLinePosition.Character,
                    Location_FilePath = data.Location.SourceTree.FilePath,
                    Id = data.Id,
                    Message = data.GetMessage()
                };
            }

            public bool IsWarningAsError;

            public DiagnosticSeverity Severity;
            public bool Location_IsInSource;

            public int Location_StartLinePosition_Line;
            public int Location_StartLinePosition_Character;
            public string Location_FilePath;
            public string Id;
            public string Message;
        }

        public bool Success;
        public List<BuildResult.Diagnostic> Diagnostics = new List<BuildResult.Diagnostic>();

        public static BuildResult From(EmitResult data)
        {
            return new BuildResult
            {
                Success = data.Success,
                Diagnostics = data.Diagnostics.Select(x => Diagnostic.From(x)).ToList(),
            };
        }
    }
}