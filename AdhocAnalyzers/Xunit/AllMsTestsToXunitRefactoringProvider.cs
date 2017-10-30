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
            var currentNode = root.FindNode(context.Span);

            var methodDeclaration = currentNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault(IsMsTestMethod);

            if (methodDeclaration != null)
            {
                var classDeclaration = currentNode.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
                var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(IsMsTestMethod);

                if (methodDeclarations.Count() > 1)
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

        private static SyntaxNode RemoveTestClassAttributeIfNeeded(SyntaxNode root)
        {
            // Did we remove the last Mstest method?

            if (!root.DescendantNodesAndSelf().OfType<MethodDeclarationSyntax>().Any(IsMsTestMethod))
            {
                root = RemoveTestClassAttribute(root);
                root = RemoveUnusedMsTestImportDirective(root);
                root = ConvertTestInitializeIfNeeded(root);
                root = ConvertTestCleanupIfNeeded(root);
            }

            return root;
        }

        private static SyntaxNode ConvertTestInitializeIfNeeded(SyntaxNode root)
        {
            var testInitializeAttributeToken = (
                from node in root.DescendantNodes().OfType<AttributeSyntax>()
                from token in node.DescendantTokens()
                where token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestInitialize"
                select token).SingleOrDefault();

            var compatibilityConstructorPresent = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Any();

            if (testInitializeAttributeToken.IsKind(SyntaxKind.None))
            {
                return root;
            }

            var testInitializerMethodDeclaration = testInitializeAttributeToken.Parent
                .Ancestors().OfType<MethodDeclarationSyntax>().First();
            var classDeclaration = (ClassDeclarationSyntax)testInitializerMethodDeclaration.Parent;
            var newConstructor = SyntaxFactory
                .ConstructorDeclaration(classDeclaration.Identifier.WithoutTrivia())
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(testInitializerMethodDeclaration.Body);

            root = root.ReplaceNode(testInitializerMethodDeclaration, newConstructor);

            if (compatibilityConstructorPresent)
            {
                root = root.RemoveNode(
                    root.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First(),
                    SyntaxRemoveOptions.KeepNoTrivia);
            }

            return root;
        }

        private static SyntaxNode ConvertTestCleanupIfNeeded(SyntaxNode root)
        {
            var testCleanupAttributeToken = (
                from node in root.DescendantNodes().OfType<AttributeSyntax>()
                from token in node.DescendantTokens()
                where token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestCleanup"
                select token).SingleOrDefault();

            var compatibilityDisposerPresent = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Any(methodDeclaration => methodDeclaration.Identifier.ValueText == "Dispose");

            if (testCleanupAttributeToken.IsKind(SyntaxKind.None))
            {
                return root;
            }

            var testCleanupMethodDeclaration = testCleanupAttributeToken.Parent
                .Ancestors().OfType<MethodDeclarationSyntax>().First();
            var classDeclaration = (ClassDeclarationSyntax)testCleanupMethodDeclaration.Parent;
            var disposeMethodDeclaration = SyntaxFactory
                .MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Dispose")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(testCleanupMethodDeclaration.Body);

            if (!compatibilityDisposerPresent)
            {
                root = root.ReplaceNode(classDeclaration,
                    classDeclaration
                        .ReplaceNode(testCleanupMethodDeclaration, disposeMethodDeclaration)
                        .WithBaseList((classDeclaration.BaseList ?? SyntaxFactory.BaseList())
                            .AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("System.IDisposable"))))
                        // This is to remove the additional line ending after the class (when the base list was empty)
                        .WithIdentifier(classDeclaration.Identifier.WithoutTrivia()));
            }
            else
            {
                root = root.ReplaceNode(testCleanupMethodDeclaration, disposeMethodDeclaration);
            }

            if (compatibilityDisposerPresent)
            {
                var compatibilityDisposeMethodDeclaration = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Last(methodDeclaration => methodDeclaration.Identifier.ValueText == "Dispose");

                root = root.RemoveNode(compatibilityDisposeMethodDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
            }

            return root;
        }

        private static SyntaxNode RemoveUnusedMsTestImportDirective(SyntaxNode root)
        {
            var msTestImportDirective = root
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .SingleOrDefault(directive => directive.Name.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting");

            if (msTestImportDirective != null)
            {
                root = root.RemoveNode(msTestImportDirective, SyntaxRemoveOptions.KeepNoTrivia);
            }

            return root;
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

        private static SyntaxNode AddCompatibilityConstructorIfNeeded(SyntaxNode root)
        {
            var testInitializeAttributeToken = (
                from node in root.DescendantNodes().OfType<AttributeSyntax>()
                from token in node.DescendantTokens()
                where token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestInitialize"
                select token).SingleOrDefault();

            var alreadyAdded = root.DescendantNodes().OfType<ConstructorDeclarationSyntax>().Any();

            if (!testInitializeAttributeToken.IsKind(SyntaxKind.None) && !alreadyAdded)
            {
                var testInitializerMethodDeclaration = testInitializeAttributeToken.Parent
                    .Ancestors().OfType<MethodDeclarationSyntax>().First();
                var classDeclaration = (ClassDeclarationSyntax)testInitializerMethodDeclaration.Parent;
                var newConstructor = SyntaxFactory
                    .ConstructorDeclaration(classDeclaration.Identifier.WithoutTrivia())
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ParseStatement($"{testInitializerMethodDeclaration.Identifier.ValueText}();")));

                root = root.ReplaceNode(classDeclaration,
                    classDeclaration.WithMembers(classDeclaration.Members.Insert(0, newConstructor)));
            }

            return root;
        }

        private static SyntaxNode AddCompatibilityDisposerIfNeeded(SyntaxNode root)
        {
            var testCleanupAttributeToken = (
                from node in root.DescendantNodes().OfType<AttributeSyntax>()
                from token in node.DescendantTokens()
                where token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestCleanup"
                select token).SingleOrDefault();

            var alreadyAdded = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Any(methodDeclaration => methodDeclaration.Identifier.ValueText == "Dispose");

            if (!testCleanupAttributeToken.IsKind(SyntaxKind.None) && !alreadyAdded)
            {
                var testCleanupMethodDeclaration = testCleanupAttributeToken.Parent
                    .Ancestors().OfType<MethodDeclarationSyntax>().First();
                var classDeclaration = (ClassDeclarationSyntax)testCleanupMethodDeclaration.Parent;
                var disposeMethodDeclaration = SyntaxFactory
                    .MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Dispose")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ParseStatement($"{testCleanupMethodDeclaration.Identifier.ValueText}();")));

                root = root.ReplaceNode(classDeclaration,
                    classDeclaration
                        .WithMembers(classDeclaration.Members.Add(disposeMethodDeclaration))
                        .WithBaseList((classDeclaration.BaseList ?? SyntaxFactory.BaseList())
                            .AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("System.IDisposable"))))
                        // This is to remove the additional line ending after the class (when the base list was empty)
                        .WithIdentifier(classDeclaration.Identifier.WithoutTrivia()));
            }

            return root;
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