using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        private readonly Func<Document, Task<Document>> _organizeImportsAsync;

        public StructureNamespaceUsingsRefactoringProvider()
        {
            var featuresAssembly = Assembly.Load(new AssemblyName("Microsoft.CodeAnalysis.Features, Version=2.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"));
            var staticServiceType = featuresAssembly.GetType("Microsoft.CodeAnalysis.OrganizeImports.OrganizeImportsService").GetTypeInfo();

            var targetMethod = staticServiceType.GetDeclaredMethod("OrganizeImportsAsync");
            _organizeImportsAsync = document => (Task<Document>)targetMethod.Invoke(null, new object[] { document, true, CancellationToken.None });
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (IsOutsideUsings(root, context.Span))
            {
                return;
            }

            var rootCompilation = (CompilationUnitSyntax)root;
            var listOfUsings = rootCompilation.Usings;

            if (listOfUsings.Count < 2)
            {
                return;
            }

            var nodesMissingNewline = new List<SyntaxNode>();
            for (int i = 1; i < listOfUsings.Count; i++)
            {
                var previousUsing = listOfUsings[i - 1];
                var currentUsing = listOfUsings[i];

                if (!TopLevelNamespaceEquals(previousUsing, currentUsing) && !HasLeadingNewline(currentUsing))
                {
                    nodesMissingNewline.Add(previousUsing);
                }
            }

            if (nodesMissingNewline.Any())
            {
                var newlineTrivia = GetNewlineTrivia(context);

                context.RegisterRefactoring(
                    CodeAction.Create(
                        "Add newline betweeen using groups",
                        _ => AddNewlinesToNodes(context.Document, root, nodesMissingNewline, newlineTrivia)));

                context.RegisterRefactoring(
                    CodeAction.Create(
                        "Remove unnecessary usings, and add newline betweeen using groups",
                        _ => OrganizeImportsAndAddNewlinesToNodesAsync(context.Document, newlineTrivia)));
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
            Document document,
            SyntaxNode root,
            IEnumerable<SyntaxNode> nodesMissingNewline,
            SyntaxTrivia newLineTrivia)
        {
            var newRoot = root.ReplaceNodes(nodesMissingNewline, (oldNode, _) => AddTrailingNewline(oldNode, newLineTrivia));
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private async Task<Document> OrganizeImportsAndAddNewlinesToNodesAsync(
            Document document,
            SyntaxTrivia newLineTrivia)
        {
            var organizedDocument = await _organizeImportsAsync(document).ConfigureAwait(false);
            var root = await organizedDocument.GetSyntaxRootAsync().ConfigureAwait(false);
            var rootCompilation = (CompilationUnitSyntax)root;
            var listOfUsings = rootCompilation.Usings;

            if (listOfUsings.Count < 2)
            {
                return organizedDocument;
            }

            var nodesMissingNewline = new List<SyntaxNode>();
            for (int i = 1; i < listOfUsings.Count; i++)
            {
                var previousUsing = listOfUsings[i - 1];
                var currentUsing = listOfUsings[i];

                if (!TopLevelNamespaceEquals(previousUsing, currentUsing) && !HasLeadingNewline(currentUsing))
                {
                    nodesMissingNewline.Add(previousUsing);
                }
            }

            if (nodesMissingNewline.Any())
            {
                return await AddNewlinesToNodes(organizedDocument, root, nodesMissingNewline, newLineTrivia).ConfigureAwait(false);
            }
            else
            {
                return organizedDocument;
            }
        }

        private static SyntaxNode AddTrailingNewline(SyntaxNode node, SyntaxTrivia newLineTrivia)
        {
            var oldTrivia = node.GetTrailingTrivia();

            return node.WithTrailingTrivia(oldTrivia.Add(newLineTrivia));
        }

        private static SyntaxTrivia GetNewlineTrivia(CodeRefactoringContext context)
        {
            var workspace = context.Document.Project.Solution.Workspace;
            var newlineText = workspace.Options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);
            return SyntaxFactory.EndOfLine(newlineText);
        }
    }
}