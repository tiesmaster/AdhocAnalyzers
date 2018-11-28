using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdhocAnalyzers.Pricing
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LiteralCollectionInitializerNotSortedAnalyzer : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(
            "PRICING0001",
            nameof(LiteralCollectionInitializerNotSortedAnalyzer),
            "Collection initializer for field '{0}' has unsorted members.",
            "PRICING",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Reports collection intializer without sorted members.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeCollectionInitializer, SyntaxKind.CollectionInitializerExpression);
        }

        private void AnalyzeCollectionInitializer(SyntaxNodeAnalysisContext context)
        {
            var collectionInitializer = (InitializerExpressionSyntax)context.Node;

            var objectCreation = (ObjectCreationExpressionSyntax)collectionInitializer.Parent;
            var genericName = (GenericNameSyntax)objectCreation.Type;
            var firstTypeArgument = genericName.TypeArgumentList.Arguments.First();

            if (!firstTypeArgument.IsKind(SyntaxKind.PredefinedType)
                || !((PredefinedTypeSyntax)firstTypeArgument).Keyword.IsKind(SyntaxKind.StringKeyword))
            {
                return;
            }

            var strings = collectionInitializer
                .Expressions
                .Cast<InitializerExpressionSyntax>()
                .Select(x => x.Expressions.First())
                .Cast<LiteralExpressionSyntax>()
                .Select(x => x.Token.ValueText)
                .ToArray();

            if (true /* IsSorted(strings) */)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _rule,
                    collectionInitializer.GetLocation(),
                    GetFieldIdentifier(objectCreation).ValueText));
            }
        }

        private SyntaxToken GetFieldIdentifier(ObjectCreationExpressionSyntax objectCreation)
            => objectCreation.FirstAncestorOrSelf<VariableDeclaratorSyntax>().Identifier;
    }
}