using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdhocAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoMapperDeprecatedProfileCodeFixProvider)), Shared]
    public class AutoMapperDeprecatedProfileCodeFixProvider : CodeFixProvider
    {
        private const string TITLE = "Convert to constructor";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AutoMapperDeprecatedProfileAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TITLE,
                    createChangedDocument: c => ConvertToConstructor(context.Document, declaration, c),
                    equivalenceKey: TITLE),
                diagnostic);
        }

        private async Task<Document> ConvertToConstructor(Document document, MethodDeclarationSyntax oldMethodNode, CancellationToken cancellationToken)
        {
            var classIdentifier = oldMethodNode.Ancestors().OfType<ClassDeclarationSyntax>().Single().Identifier;
            var constructorIdentifier = classIdentifier.NormalizeWhitespace();

            var newMethodNode = SyntaxFactory
                .ConstructorDeclaration(constructorIdentifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .NormalizeWhitespace()
                .WithAttributeLists(oldMethodNode.AttributeLists)
                .WithParameterList(oldMethodNode.ParameterList)
                .WithBody(oldMethodNode.Body)
                .WithTriviaFrom(oldMethodNode);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(oldMethodNode, newMethodNode);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}