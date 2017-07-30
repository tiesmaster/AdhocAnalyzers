using System.Composition;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdhocRefactorings.AutoFixture
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CreateToBuildRefactoringProvider)), Shared]
    public class CreateToBuildRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var currentNode = root.FindNode(context.Span);

            var genericNode = currentNode as GenericNameSyntax;
            if (genericNode == null || genericNode.Identifier.ValueText != "Create")
            {
                return;
            }

            var genericArguments = genericNode.TypeArgumentList.Arguments;
            if (genericArguments.Count != 1)
            {
                return;
            }

            var fixtureCreateMemberAccessNode = genericNode.Parent as MemberAccessExpressionSyntax;
            if (fixtureCreateMemberAccessNode == null
                || !fixtureCreateMemberAccessNode.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return;

            }

            var invocationNode = fixtureCreateMemberAccessNode.Parent as InvocationExpressionSyntax;
            if (invocationNode == null)
            {
                return;
            }

            context.RegisterRefactoring(
                CodeAction.Create(
                    "Convert Create<...>() to Build<...>().Create()",
                    _ =>
                    {
                        var fixtureBuildMemberAccessNode =
                            SyntaxFactory.InvocationExpression(
                                fixtureCreateMemberAccessNode.ReplaceToken(
                                    genericNode.Identifier,
                                    SyntaxFactory.Identifier("Build")));


                        var newInvocationNode = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                fixtureBuildMemberAccessNode,
                                SyntaxFactory.IdentifierName("Create")));

                        var newRoot = root.ReplaceNode(invocationNode, newInvocationNode);
                        return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                    }));
        }
    }
}