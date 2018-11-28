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
            "Given collection initializer has unsorted members.",
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
            if (!firstTypeArgument.IsKind(SyntaxKind.PredefinedType) ||
                ((PredefinedTypeSyntax)firstTypeArgument).Keyword != SyntaxFactory.Token(SyntaxKind.StringKeyword))
            {
                return;
            }

            throw new NotImplementedException();
        }
    }
}