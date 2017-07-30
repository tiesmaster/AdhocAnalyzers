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

namespace AdhocAnalyzers.AutoFixture
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnneededBuilderPatternCodeFixProvider))]
    [Shared]
    public class UnneededBuilderPatternCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("AF0001");

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticsLocation = diagnostic.Location;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var outerInvocationNode = (InvocationExpressionSyntax)root.FindNode(diagnosticsLocation.SourceSpan);

            var innerInvocationNode = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)outerInvocationNode.Expression).Expression;

            var buildIdentifierNode = innerInvocationNode.DescendantNodes().OfType<GenericNameSyntax>().Single();
            var buildToken = buildIdentifierNode.Identifier;
            var newInvocationNode = innerInvocationNode.ReplaceToken(buildToken, SyntaxFactory.Identifier("Create"));

            var newRoot = root.ReplaceNode(outerInvocationNode, newInvocationNode);

            var targetTypeName = buildIdentifierNode.TypeArgumentList.Arguments[0].ToString();
            var title = $"Simplify '.Build<{targetTypeName}>().Create()' to '.Create<{targetTypeName}>()'.";
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    _ => Task.FromResult(context.Document.WithSyntaxRoot(newRoot))),
                diagnostic);
        }
    }
}   