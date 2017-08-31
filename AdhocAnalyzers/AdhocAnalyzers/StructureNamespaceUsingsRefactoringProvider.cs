using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace AdhocRefactorings
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(StructureNamespaceUsingsRefactoringProvider)), Shared]
    public class StructureNamespaceUsingsRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (IsOutsideUsings(root, context.Span))
            {
                return;
            }

            var rootCompilation = (CompilationUnitSyntax)root;
            var nodesMissingNewline = GetNodesMissingNewline(rootCompilation.Usings);

            if (nodesMissingNewline.Any())
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "Add newline betweeen using groups",
                        _ => AddNewlinesToNodes(context, context.Document, root, nodesMissingNewline)));
            }
        }

        private IEnumerable<SyntaxNode> GetNodesMissingNewline(SyntaxList<UsingDirectiveSyntax> usings)
        {
            if (usings.Count < 2)
            {
                return Enumerable.Empty<SyntaxNode>();
            }
            else
            {
                return YieldNodesMissingNewline(usings);
            }

            IEnumerable<SyntaxNode> YieldNodesMissingNewline(SyntaxList<UsingDirectiveSyntax> listOfUsings)
            {
                for (int i = 1; i < listOfUsings.Count; i++)
                {
                    var previousUsing = listOfUsings[i - 1];
                    var currentUsing = listOfUsings[i];

                    if (!TopLevelNamespaceEquals(previousUsing, currentUsing) && !HasLeadingNewline(currentUsing))
                    {
                        yield return previousUsing;
                    }
                }
            }
        }

        private static bool IsOutsideUsings(SyntaxNode root, TextSpan span)
        {
            var currentNode = root.FindNode(span);
            return currentNode.FirstAncestorOrSelf<UsingDirectiveSyntax>() == null;
        }

        private bool TopLevelNamespaceEquals(UsingDirectiveSyntax left, UsingDirectiveSyntax right)
        {
            var leftToplevel = GetToplevelNamespaceName(right);
            var rightToplevel = GetToplevelNamespaceName(left);

            return leftToplevel == rightToplevel;
        }

        private string GetToplevelNamespaceName(UsingDirectiveSyntax usingNode)
            => GetFirstNamespaceName(usingNode.Name);

        private string GetFirstNamespaceName(NameSyntax nameNode)
        {
            var identifierNode = nameNode as IdentifierNameSyntax;
            if (identifierNode != null)
            {
                return identifierNode.ToString();
            }
            else
            {
                var qualifiedNode = (QualifiedNameSyntax)nameNode;
                return GetFirstNamespaceName(qualifiedNode.Left);
            }
        }

        private static bool HasLeadingNewline(SyntaxNode node)
        {
            if (!node.HasLeadingTrivia)
            {
                return false;
            }

            return node.GetLeadingTrivia().First().IsKind(SyntaxKind.EndOfLineTrivia);
        }

        private Task<Document> AddNewlinesToNodes(
            CodeRefactoringContext context,
            Document document,
            SyntaxNode root,
            IEnumerable<SyntaxNode> nodesMissingNewline)
        {
            var newLineTrivia = GetNewLineTrivia(context);

            var newRoot = root.ReplaceNodes(nodesMissingNewline, (oldNode, _) => AddTrailingNewline(oldNode, newLineTrivia));
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static SyntaxNode AddTrailingNewline(SyntaxNode node, SyntaxTrivia newLineTrivia)
        {
            var oldTrivia = node.GetTrailingTrivia();

            return node.WithTrailingTrivia(oldTrivia.Add(newLineTrivia));
        }

        private static SyntaxTrivia GetNewLineTrivia(CodeRefactoringContext context)
        {
            var workspace = context.Document.Project.Solution.Workspace;
            var newlineText = workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);
            return SyntaxFactory.EndOfLine(newlineText);
        }
    }
}