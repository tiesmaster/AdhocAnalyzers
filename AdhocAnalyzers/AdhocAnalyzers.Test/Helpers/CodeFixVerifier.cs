using System.Collections.Generic;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
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

            var actions = GetCodeActions(document, codeFixProvider, analyzerDiagnostics[0]);
            document = document.ApplyCodeAction(actions[0]);

            var actual = document.ToStringAndFormat();
            Assert.Equal(newSource, actual);
        }

        private List<CodeAction> GetCodeActions(Document document, CodeFixProvider codeFixProvider, Diagnostic diagnostic)
        {
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(document, diagnostic, (a, d) => actions.Add(a), CancellationToken.None);
            codeFixProvider.RegisterCodeFixesAsync(context).Wait();

            return actions;
        }
    }
}