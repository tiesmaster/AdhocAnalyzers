using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
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
                            .WithTriviaFrom(testMethodAttributeIdentifier)
                            .WithAdditionalAnnotations(Simplifier.Annotation);

                        var newRoot = root.ReplaceNode(testMethodAttributeIdentifier, factAttributeIdentifier);

                        if (!newRoot.DescendantNodesAndSelf().OfType<MethodDeclarationSyntax>().Any(IsMsTestMethod))
                        {
                            var testClassAttributeToken = newRoot
                                .DescendantTokens()
                                .Single(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestClass");
                            var attributeListOfTestClassAttribute = testClassAttributeToken
                                .Parent
                                .Ancestors()
                                .OfType<AttributeListSyntax>()
                                .First();

                            newRoot = newRoot.RemoveNode(attributeListOfTestClassAttribute, SyntaxRemoveOptions.KeepNoTrivia);

                            var msTestImportDirective = newRoot
                                .DescendantNodes()
                                .OfType<UsingDirectiveSyntax>()
                                .SingleOrDefault(un => un.Name.ToString() == "Microsoft.VisualStudio.TestTools.UnitTesting");

                            if (msTestImportDirective != null)
                            {
                                newRoot = newRoot.RemoveNode(msTestImportDirective, SyntaxRemoveOptions.KeepNoTrivia);
                            }
                        }

                        var testInitializeAttributeToken = newRoot
                            .DescendantTokens()
                            .SingleOrDefault(token => token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == "TestInitialize");

                        if (!testInitializeAttributeToken.IsKind(SyntaxKind.None))
                        {
                            var testInitializerMethodDeclaration = testInitializeAttributeToken
                                .Parent
                                .Ancestors()
                                .OfType<MethodDeclarationSyntax>()
                                .First();
                            var classDeclaration = (ClassDeclarationSyntax)testInitializerMethodDeclaration.Parent;

                            var newConstructor = SyntaxFactory
                                .ConstructorDeclaration(classDeclaration.Identifier.WithoutTrivia())
                                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                .WithBody(SyntaxFactory.Block(
                                    SyntaxFactory.ParseStatement($"{testInitializerMethodDeclaration.Identifier.ValueText}();")));

                            var oldMembers = classDeclaration.Members;

                            var newMembers = oldMembers.Insert(0, newConstructor);

                            var newClassDeclaration = classDeclaration.WithMembers(newMembers);

                            newRoot = newRoot.ReplaceNode(classDeclaration, newClassDeclaration);
                        }

                        return ImportAdder.AddImportsAsync(context.Document.WithSyntaxRoot(newRoot));
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