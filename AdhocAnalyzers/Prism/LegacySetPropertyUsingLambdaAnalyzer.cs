using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdhocAnalyzers.Prism
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LegacySetPropertyUsingLambdaAnalyzer : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
            "PRISM0001",
            nameof(LegacySetPropertyUsingLambdaAnalyzer),
            "SetProperty() using lambda syntax is deprecated, use the default version instead.",
            "PRISM",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Reports usage of deprecated version of PRISM's SetProperty.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}