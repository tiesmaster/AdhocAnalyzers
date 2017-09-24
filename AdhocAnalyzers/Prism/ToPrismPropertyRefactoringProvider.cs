using System;
using System.Linq;
using System.Composition;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;

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

            var assignmentExpression = (AssignmentExpressionSyntax)currentNode
                .AncestorsAndSelf()
                .FirstOrDefault(x => x.IsKind(SyntaxKind.SimpleAssignmentExpression));

            if ((assignmentExpression?.Right is IdentifierNameSyntax identifier)
                && identifier.Identifier.ValueText == "value"
                && assignmentExpression.Left is IdentifierNameSyntax fieldIdentifier)
            {
                var isInsideSetter = assignmentExpression.Ancestors().Any(x => x.IsKind(SyntaxKind.SetAccessorDeclaration));
                if (isInsideSetter)
                {
                    context.RegisterRefactoring(
                        CodeAction.Create("Convert to PRISM property", _ =>
                        {
                            var newSetPropertyStatement = SyntaxFactory
                                .ParseExpression($"SetProperty(ref {fieldIdentifier.Identifier.ValueText}, value)")
                                .WithTriviaFrom(assignmentExpression)
                                .WithAdditionalAnnotations(Formatter.Annotation);

                            var newRoot = root.ReplaceNode(assignmentExpression, newSetPropertyStatement);
                            return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                        }));
                }
            }
        }
    }
}