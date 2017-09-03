using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdhocAnalyzers.AutoFixture
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnneededBuilderPatternAnalyzer : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
            "AF0001",
            nameof(UnneededBuilderPatternAnalyzer),
            "Build<{0}>() directly followed by Create() can be simplified",
            "AutoFixture",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Reports excessive usage of AutoFixture's builder pattern.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeGenericIdentifier, SyntaxKind.GenericName);

        private void AnalyzeGenericIdentifier(SyntaxNodeAnalysisContext context)
        {
            var genericNode = (GenericNameSyntax)context.Node;
            if (genericNode.Identifier.ValueText != "Build")
            {
                return;
            }

            var genericArguments = genericNode.TypeArgumentList.Arguments;
            if (genericArguments.Count != 1)
            {
                return;
            }

            var fixtureBuildMemberAccessNode = genericNode.Parent as MemberAccessExpressionSyntax;
            if (fixtureBuildMemberAccessNode == null
                || !fixtureBuildMemberAccessNode.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return;
            }

            var buildInvocationNode = fixtureBuildMemberAccessNode.Parent as InvocationExpressionSyntax;
            if (buildInvocationNode == null)
            {
                return;
            }

            var createMemberAccessNode = buildInvocationNode.Parent as MemberAccessExpressionSyntax;
            if (createMemberAccessNode == null
                || !createMemberAccessNode.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return;
            }

            var createIdentifierNode = createMemberAccessNode.Name as IdentifierNameSyntax;
            if (createIdentifierNode == null || createIdentifierNode.Identifier.ValueText != "Create")
            {
                return;
            }

            var createInvocation = createMemberAccessNode.Parent as InvocationExpressionSyntax;
            if (createMemberAccessNode == null)
            {
                return;
            }

            var targetTypeName = genericArguments[0].ToString();
            context.ReportDiagnostic(Diagnostic.Create(_rule, createInvocation.GetLocation(), targetTypeName));
        }
    }
}