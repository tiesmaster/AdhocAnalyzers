using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AdhocAnalyzers.Prism
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LegacySetPropertyUsingLambdaCodeFixProvider))]
    [Shared]
    public class LegacySetPropertyUsingLambdaCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => throw new NotImplementedException();

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            throw new NotImplementedException();
        }
    }
}