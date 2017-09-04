using System.Collections.Generic;

using FluentAssertions;

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;

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

            actions.Should().BeEmpty("because no refactorings should have been registered");
        }

        protected void VerifyRefactoring(
            string oldSource,
            string newSource,
            int position,
            string codeActionTitle,
            IDictionary<OptionKey, object> changedOptionSet = null)
        {
            var document = DocumentFactory.CreateDocument(oldSource);

            var actions = GetCodeRefactoringProvider().GetCodeActions(document, position);

            var codeActionToApply = actions
                .Should()
                .ContainSingle(
                    action => action.Title == codeActionTitle,
                    "because the refactoring should register exactly one code action with title '{0}'",
                    codeActionTitle)
                .Subject;

            document = document.ApplyCodeAction(codeActionToApply);

            var actual = document.ToStringAndFormat(changedOptionSet);
            Assert.Equal(newSource, actual);
        }
    }
}