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
            var methodDeclarations = root.DescendantNodesAndSelf().OfType<MethodDeclarationSyntax>().Where(IsMsTestMethod);
            if (methodDeclarations.Skip(1).Any()
                && methodDeclarations.Any(method => method.Span.IntersectsWith(context.Span)))
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "Convert MSTest methods to Facts",
                        _ => ConvertAllTestsToFactsAsync(context.Document, root, methodDeclarations)));
            }
        }

        private static Task<Document> ConvertAllTestsToFactsAsync(
            Document originalDocument,
            SyntaxNode root,
            IEnumerable<MethodDeclarationSyntax> msTestMethodDeclarations)
        {
            var newRoot = root.ReplaceNodes(
                msTestMethodDeclarations.Select(GetMsTestMethodAttributeIdentifier),
                ConvertMsTestMethodAttributeToFact);

            var testClassAttributes = GetTestClassAttributeIdentifier(
                newRoot.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().Single());

            newRoot = newRoot.RemoveNodes(
                testClassAttributes.Select(testClassIdentifier => testClassIdentifier.Parent.Parent),
                SyntaxRemoveOptions.KeepNoTrivia);

            newRoot = newRoot.RemoveNode(GetMsTestDirective(newRoot), SyntaxRemoveOptions.KeepNoTrivia);

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

        private static bool IsMsTestMethod(MethodDeclarationSyntax methodDeclaration)
            => GetMsTestMethodAttributeIdentifier(methodDeclaration) != null;

        private static bool IsMsTestDirective(UsingDirectiveSyntax directive)
            => directive.Name.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting";

        private static IdentifierNameSyntax GetMsTestMethodAttributeIdentifier(MethodDeclarationSyntax methodDeclaration)
        {
            var query = from attribute in methodDeclaration.AttributeLists
                        from node in attribute.DescendantNodes().OfType<IdentifierNameSyntax>()
                        where node.Identifier.ValueText == "TestMethod"
                        select node;
            return query.SingleOrDefault();
        }

        private static IEnumerable<IdentifierNameSyntax> GetTestClassAttributeIdentifier(
            ClassDeclarationSyntax classDeclaration)
        {
            return from attribute in classDeclaration.AttributeLists
                   from node in attribute.DescendantNodes().OfType<IdentifierNameSyntax>()
                   where node.Identifier.ValueText == "TestClass"
                   select node;
        }

        private static UsingDirectiveSyntax GetMsTestDirective(SyntaxNode newRoot)
            => newRoot.DescendantNodes().OfType<UsingDirectiveSyntax>().Single(IsMsTestDirective);
    }
}