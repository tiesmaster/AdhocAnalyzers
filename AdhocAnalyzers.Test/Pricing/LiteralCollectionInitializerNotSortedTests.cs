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
@"using System.Collections.Generic;

public class Class1
{
    private static readonly Dictionary<string, bool> _users = new Dictionary<string, bool> {
            {""foo"", true},
            {""bar"", false},
            {""baz"", false},
        };
}";
            VerifyDiagnostic(test);
        }

        [Fact]
        public void CollectionInitializerWithoutStringKeysNoDiagnostics()
        {
            var test =
@"using System.Collections.Generic;

public class Class1
{
    private static readonly Dictionary<int, bool> _users = new Dictionary<int, bool> {
            {1, true},
            {2, false},
            {3, false},
        };
}";
            VerifyDiagnostic(test);
        }

        [Fact]
        public void ClassWitUnsortedCollectionInitializer_ShouldProvideDiagnostic /* AndCodeFix */()
        {
            var test =
@"using System.Collections.Generic;

public class Class1
{
    private static readonly Dictionary<string, bool> _users = new Dictionary<string, bool> {
            {""bar"", false},
            {""foo"", true},
            {""baz"", false},
        };
}";

            var expected = new DiagnosticResult
            {
                Id = "PRICING0001",
                Message = "Collection initializer for field '_users' has unsorted members.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 5, 92)
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