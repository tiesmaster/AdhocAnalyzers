using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace AdhocAnalyzers.Xunit
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AllMsTestsToXunitRefactoringProvider))]
    [Shared]
    public class AllMsTestsToXunitRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);
            var currentNode = root.FindNode(context.Span);

            var methodDeclaration = currentNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault(IsMsTestMethod);

            // First of all, are we in a mstest method?
            if (methodDeclaration != null)
            {
                var classDeclaration = currentNode.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
                var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(IsMsTestMethod);

                // Next, do we have at least 2 mstest methods to convert?
                if (methodDeclarations.Skip(1).Any())
                {
                    context.RegisterRefactoring(
                        CodeAction.Create(
                            "Convert MSTest methods to Facts",
                            _ => ConvertAllTestToFactsAsync(context.Document, root, methodDeclarations)));
                }
            }
        }

        private static Task<Document> ConvertAllTestToFactsAsync(
            Document originalDocument,
            SyntaxNode root,
            IEnumerable<MethodDeclarationSyntax> msTestMethodDeclarations)
        {
            var newRoot = root.ReplaceNodes(msTestMethodDeclarations, ConvertMsTestMethodToFact);
            newRoot = RemoveTestClassAttribute(newRoot);
            newRoot = RemoveUnusedMsTestImportDirective(newRoot);

            return ImportAdder.AddImportsAsync(originalDocument.WithSyntaxRoot(newRoot));
        }

        private static SyntaxNode ConvertMsTestMethodToFact(MethodDeclarationSyntax originalNode, MethodDeclarationSyntax newNode)
        {
            var msTestMethodDeclaration = newNode;

            var msTestMethodAttributeIdentifier = msTestMethodDeclaration
                .DescendantNodes().OfType<IdentifierNameSyntax>()
                .Single(identifier => identifier.Identifier.ValueText == "TestMethod");

            var factAttributeIdentifier = SyntaxFactory
                .ParseName("Xunit.FactAttribute")
                .WithTriviaFrom(msTestMethodAttributeIdentifier)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            return msTestMethodDeclaration.ReplaceNode(msTestMethodAttributeIdentifier, factAttributeIdentifier);
        }

        private static SyntaxNode RemoveTestClassAttribute(SyntaxNode root)
        {
            var testClassAttributeToken = root
                .DescendantTokens()
                .Single(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestClass");

            var attributeListOfTestClassAttribute = testClassAttributeToken
                .Parent
                .Ancestors()
                .OfType<AttributeListSyntax>()
                .First();

            return root.RemoveNode(attributeListOfTestClassAttribute, SyntaxRemoveOptions.KeepNoTrivia);
        }

        private static SyntaxNode RemoveUnusedMsTestImportDirective(SyntaxNode root)
        {
            var msTestImportDirective = root
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Single(IsMsTestDirective);

            return root.RemoveNode(msTestImportDirective, SyntaxRemoveOptions.KeepNoTrivia);
        }

        private static bool IsMsTestMethod(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration
                .AttributeLists
                .SelectMany(attr => attr.DescendantTokens())
                .Any(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestMethod");
        }

        private static bool IsMsTestDirective(UsingDirectiveSyntax directive)
            => directive.Name.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting";
    }
}