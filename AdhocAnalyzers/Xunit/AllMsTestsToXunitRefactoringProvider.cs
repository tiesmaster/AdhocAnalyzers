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
            var newRoot = root.ReplaceNodes(
                msTestMethodDeclarations.Select(GetMsTestMethodAttributeIdentifier),
                ConvertMsTestMethodAttributeToFact);
            newRoot = RemoveTestClassAttribute(newRoot);
            newRoot = RemoveUnusedMsTestImportDirective(newRoot);

            return ImportAdder.AddImportsAsync(originalDocument.WithSyntaxRoot(newRoot));
        }

        private static SyntaxNode ConvertMsTestMethodAttributeToFact(
            IdentifierNameSyntax originalNode,
            IdentifierNameSyntax newNode)
        {
            return SyntaxFactory
                .ParseName("Xunit.FactAttribute")
                .WithTriviaFrom(newNode)
                .WithAdditionalAnnotations(Simplifier.Annotation);
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
            => GetMsTestMethodAttributeIdentifier(methodDeclaration) != null;

        private static bool IsMsTestDirective(UsingDirectiveSyntax directive)
            => directive.Name.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting";

        private static IdentifierNameSyntax GetMsTestMethodAttributeIdentifier(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration
                .AttributeLists
                .SelectMany(attr => attr.DescendantNodes())
                .OfType<IdentifierNameSyntax>()
                .SingleOrDefault(node => node.Identifier.ValueText == "TestMethod");
        }
    }
}