using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace AdhocAnalyzers.Test.Helpers
{
    internal static class RoslynExtensions
    {
        public static string ToStringAndFormat(this Document document, IDictionary<OptionKey, object> changedOptionSet = null)
        {
            var simplifiedDoc = Simplifier.ReduceAsync(document, Simplifier.Annotation).Result;
            var root = simplifiedDoc.GetSyntaxRootAsync().Result;

            var simplifiedSolution = simplifiedDoc.Project.Solution;

            root = Formatter.Format(
                root,
                Formatter.Annotation,
                simplifiedSolution.Workspace,
                GetFormattingOptionSet(simplifiedSolution.Options, changedOptionSet));

            return root.GetText().ToString();
        }

        private static OptionSet GetFormattingOptionSet(OptionSet baseOptionSet, IDictionary<OptionKey, object> changedOptionSet)
            => changedOptionSet?.Aggregate(
                baseOptionSet,
                (newOptionSet, kvp) => newOptionSet.WithChangedOption(kvp.Key, kvp.Value));

        public static Document ApplyCodeAction(this Document document, CodeAction codeAction)
        {
            var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;

            return solution.GetDocument(document.Id);
        }

        public static List<CodeAction> GetCodeActions(this CodeFixProvider codeFixProvider, Document document, Diagnostic diagnostic)
        {
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);
            codeFixProvider.RegisterCodeFixesAsync(context).Wait();

            return actions;
        }

        public static List<CodeAction> GetCodeActions(
            this CodeRefactoringProvider codeRefactoringProvider,
            Document document,
            int position)
        {
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(
                document,
                TextSpan.FromBounds(position, position),
                actions.Add,
                CancellationToken.None);

            codeRefactoringProvider.ComputeRefactoringsAsync(context).Wait();

            return actions;
        }
    }
}