using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdhocAnalyzers.Prism;
using AdhocAnalyzers.Test.Helpers;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

using Xunit;

namespace AdhocAnalyzers.Test.Prism
{
    public class LegacySetPropertyUsingLambdaTests : CodeFixVerifier
    {
        [Fact]
        public void EmptySourceNoDiagnostics()
        {
            var test = "";

            VerifyDiagnostic(test);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
            => new LegacySetPropertyUsingLambdaAnalyzer();
        protected override CodeFixProvider GetCodeFixProvider()
            => new LegacySetPropertyUsingLambdaCodeFixProvider();
    }
}
