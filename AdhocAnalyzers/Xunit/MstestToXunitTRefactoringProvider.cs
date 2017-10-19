using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Simplification;

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

            var methodDeclaration = currentNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodDeclaration != null && IsMsTestMethod(methodDeclaration))
            {
                context.RegisterRefactoring(
                    CodeAction.Create("Convert MSTest method to Fact", _ =>
                    {
                        var testMethodAttributeIdentifier = methodDeclaration
                            .DescendantNodes()
                            .OfType<IdentifierNameSyntax>()
                            .Single(identifier => identifier.Identifier.ValueText == "TestMethod");

                        var factAttributeIdentifier = SyntaxFactory
                            .ParseName("Xunit.FactAttribute")
                            .WithAdditionalAnnotations(Simplifier.Annotation);

                        var newRoot = root.ReplaceNode(testMethodAttributeIdentifier, factAttributeIdentifier);
                        return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                    }));
            }
        }

        private static bool IsMsTestMethod(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration
                .AttributeLists
                .SelectMany(attr => attr.DescendantTokens())
                .Any(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestMethod");
        }
    }
}