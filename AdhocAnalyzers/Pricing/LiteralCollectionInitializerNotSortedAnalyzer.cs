using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdhocAnalyzers.Pricing
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LiteralCollectionInitializerNotSortedAnalyzer : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
            "PRICING0001",
            nameof(LiteralCollectionInitializerNotSortedAnalyzer),
            "Given collection initializer has unsorted members.",
            "PRICING",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Reports collection intializer without sorted members.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}