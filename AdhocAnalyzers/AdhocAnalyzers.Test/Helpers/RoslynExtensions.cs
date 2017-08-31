using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace AdhocAnalyzers.Test.Helpers
{
    internal static class RoslynExtensions
    {
        public static string ToStringAndFormat(this Document document)
        {
            var simplifiedDoc = Simplifier.ReduceAsync(document, Simplifier.Annotation).Result;
            var root = simplifiedDoc.GetSyntaxRootAsync().Result;
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
            return root.GetText().ToString();
        }

        public static Document ApplyCodeAction(this Document document, CodeAction codeAction)
        {
            var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;

            return solution.GetDocument(document.Id);
        }
    }
}