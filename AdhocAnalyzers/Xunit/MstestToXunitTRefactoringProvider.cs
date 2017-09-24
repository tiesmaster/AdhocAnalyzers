using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace AdhocAnalyzers.Xunit
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(StructureNamespaceUsingsRefactoringProvider))]
    [Shared]
    public class MstestToXunitTRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);
            var currentNode = root.FindNode(context.Span);

            if (currentNode is MethodDeclarationSyntax methodDeclaration)
            {
                var isMsTestMethod = methodDeclaration
                    .AttributeLists
                    .Any(attributeList => attributeList
                        .DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Any(identifier => identifier.Identifier.ValueText == "TestMethod"));

                if (isMsTestMethod)
                {
                    context.RegisterRefactoring(
                        CodeAction.Create("Convert MSTest method to Fact", _ =>
                        {
                            var testMethodToken = methodDeclaration
                                .DescendantTokens()
                                .Single(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestMethod");

                            var factToken = SyntaxFactory.Identifier("Fact");

                            var newRoot = root.ReplaceToken(testMethodToken, factToken);
                            return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                        }));
                }
            }
        }
    }
}