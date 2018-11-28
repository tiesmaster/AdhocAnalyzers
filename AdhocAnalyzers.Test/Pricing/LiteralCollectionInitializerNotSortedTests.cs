using System;

using AdhocAnalyzers.Pricing;
using AdhocAnalyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
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
@"class Class1
{
}";
            VerifyDiagnostic(test);
        }

        [Fact]
        public void ClassWitSortedCollectionInitializerNoDiagnostics()
        {
            var test =
@"public class Class1
{
    private static readonly string[] _items = new string[] {
        ""foo"",
        ""bar"",
        ""baz""
    };
}";
            VerifyDiagnostic(test);
        }

        [Fact]
        public void ClassWitUnsortedCollectionInitializerNoDiagnostics()
        {
            var test =
@"public class Class1
{
    private static readonly string[] _items = new string[] {
        ""bar"",
        ""foo"",
        ""baz""
    };
}";

            var expected = new DiagnosticResult
            {
                Id = "PRICING0001",
                Message = "Collection initializer for field '_items' has unsorted members.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 3, 47)
                        }
            };

            VerifyDiagnostic(test, expected);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
            => new LiteralCollectionInitializerNotSortedAnalyzer();

        protected override CodeFixProvider GetCodeFixProvider()
        {
            throw new NotImplementedException();
        }
    }
}