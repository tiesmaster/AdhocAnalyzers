using System;
using System.Composition;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace AdhocAnalyzers.Prism
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(StructureNamespaceUsingsRefactoringProvider))]
    [Shared]
    public class ToPrismPropertyRefactoringProvider : CodeRefactoringProvider
    {
        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            return Task.CompletedTask;
        }
    }
}