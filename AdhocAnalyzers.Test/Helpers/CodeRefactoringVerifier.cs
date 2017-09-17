using System;
using System.Collections.Generic;

using FluentAssertions;

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;

using Roslyn.UnitTestFramework;

using Xunit;

namespace AdhocAnalyzers.Test.Helpers
{
    public abstract class CodeRefactoringVerifier
    {
        protected abstract CodeRefactoringProvider GetCodeRefactoringProvider();

        protected void VerifyNoRefactoring(string markup)
        {
            MarkupTestFile.GetPosition(markup, out var source, out var position);
            var document = DocumentFactory.CreateDocument(source);
            var actions = GetCodeRefactoringProvider().GetCodeActions(document, position);

            actions.Should().BeEmpty("because no refactorings should have been registered");
        }

        protected void VerifyRefactoring(
            string initialMarkup,
            string expectedSource,
            string codeActionTitle,
            IDictionary<OptionKey, object> changedOptionSet = null)
        {
            MarkupTestFile.GetPosition(initialMarkup, out var initialSource, out var position);
            var document = DocumentFactory.CreateDocument(initialSource);

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
            Assert.Equal(expectedSource, actual);
        }

        [Obsolete]
        protected void VerifyNoRefactoringOld(string source, int position)
        {
            string markup = source.Insert(position, "$$");
            Console.WriteLine(markup);
            VerifyNoRefactoring(markup);
        }

        [Obsolete]
        protected void VerifyRefactoringOld(
            string oldSource,
            string newSource,
            int position,
            string codeActionTitle,
            IDictionary<OptionKey, object> changedOptionSet = null)
            => VerifyRefactoring(oldSource.Insert(position, "$$"), newSource, codeActionTitle, changedOptionSet);
    }
}