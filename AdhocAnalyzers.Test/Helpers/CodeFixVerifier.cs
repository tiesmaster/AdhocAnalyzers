using Microsoft.CodeAnalysis.CodeFixes;

using Xunit;

namespace AdhocAnalyzers.Test.Helpers
{
    public abstract class CodeFixVerifier : DiagnosticVerifier
    {
        protected abstract CodeFixProvider GetCodeFixProvider();

        protected void VerifyFix(string oldSource, string newSource)
        {
            var analyzer = GetDiagnosticAnalyzer();
            var codeFixProvider = GetCodeFixProvider();

            var document = DocumentFactory.CreateDocument(oldSource);
            var analyzerDiagnostics = GetSortedDiagnosticsFromDocuments(analyzer, document);

            var actions = codeFixProvider.GetCodeActions(document, analyzerDiagnostics[0]);
            document = document.ApplyCodeAction(actions[0]);

            var actual = document.ToStringAndFormat();
            Assert.Equal(newSource, actual);
        }
    }
}