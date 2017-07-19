﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace AdhocRefactorings.Test.Helpers
{
    public abstract class CodeRefactoringVerifier
    {
        protected abstract CodeRefactoringProvider GetCodeRefactoringProvider();

        protected void VerifyNoRefactoring(string source, int position)
        {
            var document = DocumentFactory.CreateDocument(source, LanguageNames.CSharp);
            var actions = GetCodeActions(document, position);
            Assert.Empty(actions);
        }

        protected void VerifyRefactoring(string oldSource, string newSource, int position, string codeActionTitle)
        {
            var document = DocumentFactory.CreateDocument(oldSource, LanguageNames.CSharp);

            var actions = GetCodeActions(document, position);

            var codeActionToApply = actions.Single(action => action.Title == codeActionTitle);
            document = ApplyCodeAction(document, codeActionToApply);

            var actual = GetStringFromDocument(document);
            Assert.Equal(newSource, actual);
        }

        private List<CodeAction> GetCodeActions(Document document, int position)
        {
            var codeRefactoringProvider = GetCodeRefactoringProvider();

            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, TextSpan.FromBounds(position, position), a => actions.Add(a), CancellationToken.None);

            codeRefactoringProvider.ComputeRefactoringsAsync(context).Wait();
            return actions;
        }

        private static Document ApplyCodeAction(Document document, CodeAction codeAction)
        {
            var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;
            var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;

            return solution.GetDocument(document.Id);
        }

        private static string GetStringFromDocument(Document document)
        {
            var simplifiedDoc = Simplifier.ReduceAsync(document, Simplifier.Annotation).Result;
            var root = simplifiedDoc.GetSyntaxRootAsync().Result;
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);

            return root.GetText().ToString();
        }
    }
}