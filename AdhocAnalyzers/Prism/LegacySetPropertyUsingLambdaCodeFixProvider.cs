using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdhocAnalyzers.Prism
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LegacySetPropertyUsingLambdaCodeFixProvider))]
    [Shared]
    public class LegacySetPropertyUsingLambdaCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("PRISM0001");

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticsLocation = diagnostic.Location;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var invocation = (InvocationExpressionSyntax)root.FindNode(diagnosticsLocation.SourceSpan);

            var argumentsNode = invocation.ArgumentList;
            var newRoot = root.ReplaceNode(
                argumentsNode,
                argumentsNode.WithArguments(ConvertToDefaultSetPropertyArguments(argumentsNode.Arguments)));

            var title = "Convert custom lambda SetProperty() to PRISM's default SetProperty().";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    _ => Task.FromResult(context.Document.WithSyntaxRoot(newRoot)),
                    nameof(LegacySetPropertyUsingLambdaCodeFixProvider)),
                diagnostic);
        }

        private static SeparatedSyntaxList<ArgumentSyntax> ConvertToDefaultSetPropertyArguments(
            SeparatedSyntaxList<ArgumentSyntax> lambdaArgumentList)
        {
            return lambdaArgumentList
                .Replace(
                    lambdaArgumentList[1],
                    lambdaArgumentList[1].WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword)))
                .RemoveAt(0);
        }
    }
}