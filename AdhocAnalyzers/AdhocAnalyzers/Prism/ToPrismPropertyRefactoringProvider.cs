using System;
using System.Linq;
using System.Composition;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeActions;

namespace AdhocAnalyzers.Prism
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(StructureNamespaceUsingsRefactoringProvider))]
    [Shared]
    public class ToPrismPropertyRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);

            var currentNode = root.FindNode(context.Span);

            var propertyNode = currentNode.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (propertyNode == null)
            {
                return;
            }

            //Do we even have a getter, and setter?
            var accessors = propertyNode.AccessorList.Accessors;
            if (accessors.Count != 2)
            {
                return;
            }

            // Get fieldname
            var setter = accessors.Single(x => x.IsKind(SyntaxKind.SetAccessorDeclaration));
            var fieldAssignment = (AssignmentExpressionSyntax)setter.DescendantNodes().SingleOrDefault(x => x.IsKind(SyntaxKind.SimpleAssignmentExpression));
            if (fieldAssignment == null)
            {
                return;
            }

            if (fieldAssignment.Left is IdentifierNameSyntax identifier)
            {
                context.RegisterRefactoring(
                    CodeAction.Create("Convert to PRISM property", token =>
                    {
                        var assignmentStatement = setter.Body.Statements[0];

                        var newSetPropertyStatement = SyntaxFactory
                            .ParseStatement($"SetProperty(ref {identifier.Identifier.ValueText}, value);")
                            .WithTriviaFrom(assignmentStatement);

                        var newRoot = root.ReplaceNode(assignmentStatement, newSetPropertyStatement);
                        return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                    }));
            }
        }
    }
}