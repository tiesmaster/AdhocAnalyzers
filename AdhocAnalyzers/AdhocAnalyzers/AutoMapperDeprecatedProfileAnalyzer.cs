using System.Collections.Immutable;
using System.Linq;

using AdhocAnalyzers.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AdhocAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AutoMapperDeprecatedProfileAnalyzer : DiagnosticAnalyzer
    {
        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public const string DiagnosticId = "AM0001";

        private const string Category = "AutoMapperV5Migration";
        private const string Title = nameof(AutoMapperDeprecatedProfileAnalyzer);
        private const string MessageFormat = "Class '{0}' is not upgraded yet to AutoMapper V5";
        private const string Description = "Reports AutoMapper legacy \"protected override void Configure()\" methods";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodNode = (MethodDeclarationSyntax)context.Node;
            var classNode = (ClassDeclarationSyntax)methodNode.Parent;

            var isDeprecatedMethod =
                IsDeprecatedConfigureMethod(methodNode) &&
                IsInheritedFromProfile(classNode);

            if (isDeprecatedMethod)
            {
                Report(context, methodNode, classNode);
            }
        }

        private bool IsDeprecatedConfigureMethod(MethodDeclarationSyntax methodNode)
            => methodNode.Identifier.IsNamed("Configure") && IsProtectedOverrideMethod(methodNode);

        private bool IsProtectedOverrideMethod(MethodDeclarationSyntax methodNode)
            => methodNode.Modifiers.All(modifierToken =>
                modifierToken.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifierToken.IsKind(SyntaxKind.OverrideKeyword));

        private bool IsInheritedFromProfile(ClassDeclarationSyntax classNode)
            => classNode.BaseList
                ?.Types.Any(baseTypeNode =>
                    baseTypeNode.Type.IsKind(SyntaxKind.IdentifierName) &&
                    ((IdentifierNameSyntax)baseTypeNode.Type).IsNamed("Profile"))
                ?? false;

        private static void Report(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodNode,
            ClassDeclarationSyntax classNode)
        {
            var methodHeaderLocation = CalculateDiagnosticLocation(methodNode);
            var diagnostic = Diagnostic.Create(Rule, methodHeaderLocation, classNode.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }

        private static Location CalculateDiagnosticLocation(BaseMethodDeclarationSyntax methodNode)
        {
            var start = methodNode.SpanStart;
            var end = methodNode.ParameterList.Span.End;

            return Location.Create(methodNode.SyntaxTree, TextSpan.FromBounds(start, end));
        }
    }
}