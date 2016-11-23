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
using Microsoft.CodeAnalysis.Formatting;

namespace AdhocAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AutoMapperDeprecatedProfileCodeFixProvider)), Shared]
    public class AutoMapperDeprecatedProfileCodeFixProvider : CodeFixProvider
    {
        private const string TITLE = "Convert to constructor";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AutoMapperDeprecatedProfileAnalyzer.DIAGNOSTIC_ID);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document
                .GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root
                .FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TITLE,
                    createChangedDocument: c => ApplyFix(context.Document, declaration, c),
                    equivalenceKey: TITLE),
                diagnostic);
        }

        private async Task<Document> ApplyFix(
            Document document,
            MethodDeclarationSyntax oldMethodNode,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(oldMethodNode, ConvertToConstructor(oldMethodNode));
            return document.WithSyntaxRoot(newRoot);
        }

        private static ConstructorDeclarationSyntax ConvertToConstructor(MethodDeclarationSyntax oldMethodNode)
        {
            return SyntaxFactory
                .ConstructorDeclaration(GetConstructorIdentifier(oldMethodNode))
                .WithModifiers(SyntaxFactory.TokenList(PublicModifierToken))
                .WithAttributeLists(oldMethodNode.AttributeLists)
                .WithParameterList(oldMethodNode.ParameterList)
                .WithBody(GetMethodBody(oldMethodNode))
                .WithTriviaFrom(oldMethodNode)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static SyntaxToken PublicModifierToken => SyntaxFactory.Token(SyntaxKind.PublicKeyword);

        private static SyntaxToken GetConstructorIdentifier(MethodDeclarationSyntax oldMethodNode)
            => oldMethodNode.Ancestors().OfType<ClassDeclarationSyntax>().Single().Identifier;

        private static BlockSyntax GetMethodBody(MethodDeclarationSyntax methodNode)
            => methodNode.Body ?? AsBlock(methodNode.ExpressionBody.Expression);

        private static BlockSyntax AsBlock(ExpressionSyntax expressionNode)
        {
            var expressionBodyAsStatement = SyntaxFactory.ExpressionStatement(expressionNode);
            return SyntaxFactory.Block(expressionBodyAsStatement);
        }
    }
}