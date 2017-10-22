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
                    CodeAction.Create("Convert MSTest method to Fact", _ => ConvertSingleTestToFactAsync(context.Document, root, methodDeclaration)));
            }
        }

        private static Task<Document> ConvertSingleTestToFactAsync(
            Document originalDocument,
            SyntaxNode root,
            MethodDeclarationSyntax msTestMethodDeclaration)
        {
            var newRoot = ConvertMsTestMethodToFact(root, msTestMethodDeclaration);
            newRoot = RemoveTestClassAttributeIfNeeded(newRoot);
            newRoot = AddCompatibilityConstructorIfNeeded(newRoot);

            return ImportAdder.AddImportsAsync(originalDocument.WithSyntaxRoot(newRoot));
        }

        private static SyntaxNode ConvertMsTestMethodToFact(SyntaxNode root, MethodDeclarationSyntax msTestMethodDeclaration)
        {
            var msTestMethodAttributeIdentifier = msTestMethodDeclaration
                .DescendantNodes().OfType<IdentifierNameSyntax>()
                .Single(identifier => identifier.Identifier.ValueText == "TestMethod");

            var factAttributeIdentifier = SyntaxFactory
                .ParseName("Xunit.FactAttribute")
                .WithTriviaFrom(msTestMethodAttributeIdentifier)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            return root.ReplaceNode(msTestMethodAttributeIdentifier, factAttributeIdentifier);
        }

        private static SyntaxNode RemoveTestClassAttributeIfNeeded(SyntaxNode root)
        {
            // Did we remove the last Mstest method?

            if (!root.DescendantNodesAndSelf().OfType<MethodDeclarationSyntax>().Any(IsMsTestMethod))
            {
                root = RemoveTestClassAttribute(root);
                root = RemoveUnusedMsTestImportDirective(root);
            }

            return root;
        }

        private static SyntaxNode RemoveUnusedMsTestImportDirective(SyntaxNode root)
        {
            var msTestImportDirective = root
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .SingleOrDefault(un => un.Name.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting");

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

        private static SyntaxNode AddCompatibilityConstructorIfNeeded(SyntaxNode newRoot)
        {
            var testInitializeAttributeToken = newRoot
                .DescendantTokens()
                .SingleOrDefault(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestInitialize");

            if (!testInitializeAttributeToken.IsKind(SyntaxKind.None))
            {
                var testInitializerMethodDeclaration = testInitializeAttributeToken.Parent
                    .Ancestors().OfType<MethodDeclarationSyntax>().First();
                var classDeclaration = (ClassDeclarationSyntax)testInitializerMethodDeclaration.Parent;
                var newConstructor = SyntaxFactory
                    .ConstructorDeclaration(classDeclaration.Identifier.WithoutTrivia())
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ParseStatement($"{testInitializerMethodDeclaration.Identifier.ValueText}();")));

                newRoot = newRoot.ReplaceNode(classDeclaration,
                    classDeclaration.WithMembers(classDeclaration.Members.Insert(0, newConstructor)));
            }

            return newRoot;
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