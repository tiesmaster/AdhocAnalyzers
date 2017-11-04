using System;
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
            // Convert [TestMethod] -> [Fact]
            var newRoot = root.ReplaceNodes(
                msTestMethodDeclarations.Select(GetMsTestMethodAttributeIdentifier),
                ConvertToFact);

            // Remove [TestClass]
            var classDeclaration = newRoot.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().Single();

            var testClassAttributes = GetTestClassAttributeIdentifiers(classDeclaration);
            newRoot = newRoot.RemoveNodes(
                testClassAttributes.Select(testClassIdentifier => testClassIdentifier.Parent.Parent),
                SyntaxRemoveOptions.KeepNoTrivia);

            // Remove 'using MSTEST;'
            newRoot = newRoot.RemoveNode(GetMsTestDirective(newRoot), SyntaxRemoveOptions.KeepNoTrivia);

            // Convert [TestInitialize] -> ctor
            classDeclaration = newRoot.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().Single();
            var testInitializeMethod = GetTestInitializeMethod(classDeclaration);
            if (testInitializeMethod != null)
            {
                newRoot = newRoot.ReplaceNode(
                    testInitializeMethod,
                    ConvertToConstructor(testInitializeMethod, classDeclaration));
            }

            return ImportAdder.AddImportsAsync(originalDocument.WithSyntaxRoot(newRoot));
        }

        private static SyntaxNode ConvertToFact(
            IdentifierNameSyntax originalNode,
            IdentifierNameSyntax newNode)
        {
            return SyntaxFactory
                .ParseName("Xunit.FactAttribute")
                .WithTriviaFrom(newNode)
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        private static SyntaxNode ConvertToConstructor(
            MethodDeclarationSyntax testInitializeMethod,
            ClassDeclarationSyntax classDeclaration)
        {
            return SyntaxFactory
                .ConstructorDeclaration(classDeclaration.Identifier.WithoutTrivia())
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(testInitializeMethod.Body);
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

        private static IEnumerable<IdentifierNameSyntax> GetTestClassAttributeIdentifiers(
            ClassDeclarationSyntax classDeclaration)
        {
            return from attribute in classDeclaration.AttributeLists
                   from node in attribute.DescendantNodes().OfType<IdentifierNameSyntax>()
                   where node.Identifier.ValueText == "TestClass"
                   select node;
        }

        private static MethodDeclarationSyntax GetTestInitializeMethod(
            ClassDeclarationSyntax classDeclaration)
        {
            var query = from method in classDeclaration.Members.OfType<MethodDeclarationSyntax>()
                        from attribute in method.AttributeLists
                        from node in attribute.DescendantNodes().OfType<IdentifierNameSyntax>()
                        where node.Identifier.ValueText == "TestInitialize"
                        select method;
            return query.SingleOrDefault();
        }

        private static UsingDirectiveSyntax GetMsTestDirective(SyntaxNode newRoot)
            => newRoot.DescendantNodes().OfType<UsingDirectiveSyntax>().Single(IsMsTestDirective);
    }
}