using System.Linq;

using Microsoft.CodeAnalysis.CodeRefactorings;

using Xunit;

namespace AdhocAnalyzers.Test.Helpers
{
    public abstract class CodeRefactoringVerifier
    {
        protected abstract CodeRefactoringProvider GetCodeRefactoringProvider();

        protected void VerifyNoRefactoring(string source, int position)
        {
            var document = DocumentFactory.CreateDocument(source);
            var actions = GetCodeRefactoringProvider().GetCodeActions(document, position);
            Assert.Empty(actions);
        }

        protected void VerifyRefactoring(string oldSource, string newSource, int position, string codeActionTitle)
        {
            var document = DocumentFactory.CreateDocument(oldSource);

            var actions = GetCodeRefactoringProvider().GetCodeActions(document, position);

            var codeActionToApply = actions.Single(action => action.Title == codeActionTitle);
            document = document.ApplyCodeAction(codeActionToApply);

            var actual = document.ToStringAndFormat();
            Assert.Equal(newSource, actual);
        }
    }
}