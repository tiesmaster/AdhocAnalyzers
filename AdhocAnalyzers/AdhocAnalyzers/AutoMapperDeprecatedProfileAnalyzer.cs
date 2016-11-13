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
        private readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
            DIAGNOSTIC_ID,
            TITLE,
            MESSAGE_FORMAT,
            CATEGORY,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: DESCRIPTION);

        public const string DIAGNOSTIC_ID = "AM0001";

        private const string CATEGORY = "AutoMapperV5Migration";
        private const string TITLE = nameof(AutoMapperDeprecatedProfileAnalyzer);
        private const string MESSAGE_FORMAT = "Class '{0}' is not upgraded yet to AutoMapper V5";
        private const string DESCRIPTION = "Reports AutoMapper legacy \"protected override void Configure()\" methods";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);

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

        private void Report(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodNode,
            ClassDeclarationSyntax classNode)
        {
            var methodHeaderLocation = CalculateDiagnosticLocation(methodNode);
            var diagnostic = Diagnostic.Create(_rule, methodHeaderLocation, classNode.Identifier.Text);
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