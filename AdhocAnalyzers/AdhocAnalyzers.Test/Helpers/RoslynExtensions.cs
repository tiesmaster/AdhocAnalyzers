using Microsoft.CodeAnalysis;
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
    }
}