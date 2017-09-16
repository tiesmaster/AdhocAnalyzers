using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            => context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation.Expression is IdentifierNameSyntax name && name.Identifier.ValueText == "SetProperty"
                && invocation.ArgumentList.Arguments.Count > 2
                && invocation.Ancestors().Any(x => x.IsKind(SyntaxKind.SetAccessorDeclaration)))
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, invocation.GetLocation()));
            }
        }
    }
}