using System;
using AdhocAnalyzers.Pricing;
using AdhocAnalyzers.Test.Helpers;

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

using Xunit;

namespace AdhocAnalyzers.Test.Pricing
{
    public class LiteralCollectionInitializerNotSortedTests : CodeFixVerifier
    {
        [Fact]
        public void EmptySourceNoDiagnostics()
        {
            var test = "";

            VerifyDiagnostic(test);
        }

        [Fact]
        public void ClassWithoutCollectionInitializerNoDiagnostics()
        {
            var test =
@"$$class Class1
{
}";
            VerifyDiagnostic(test);
        }

        [Fact]
        public void ClassWitSortedCollectionInitializerNoDiagnostics()
        {
            var test =
@"$$class Class1
{
}";
            VerifyDiagnostic(test);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
            => new LiteralCollectionInitializerNotSortedAnalyzer();

        protected override CodeFixProvider GetCodeFixProvider()
        {
            throw new NotImplementedException();
        }
    }
}