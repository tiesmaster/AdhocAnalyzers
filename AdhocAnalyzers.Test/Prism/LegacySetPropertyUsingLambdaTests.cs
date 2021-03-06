﻿using AdhocAnalyzers.Prism;
using AdhocAnalyzers.Test.Helpers;

using Microsoft.CodeAnalysis;
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

        [Fact]
        public void LegacySetPropertyInvocation_ShouldProvideDiagnosticAndCodeFix()
        {
            var oldSource =
@"class Class1
{
    private string _foo;

    public string Foo
    {
        get
        {
            return _foo;
        }
        set
        {
            SetProperty(() => _foo = value, _foo, value);
        }
    }
}";
            var newSource =
@"class Class1
{
    private string _foo;

    public string Foo
    {
        get
        {
            return _foo;
        }
        set
        {
            SetProperty(ref _foo, value);
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "PRISM0001",
                Message = "SetProperty() using lambda syntax is deprecated, use the default version instead.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 13, 13)
                        }
            };

            VerifyDiagnostic(oldSource, expected);
            VerifyFix(oldSource, newSource);
        }

        [Fact]
        public void DefaultSetPropertyInvocation_DoesNotProvideDiagnostic()
        {
            var source =
@"class Class1
{
    private string _foo;

    public string Foo
    {
        get
        {
            return _foo;
        }
        set
        {
            SetProperty(ref _foo, value);
        }
    }
}";

            VerifyDiagnostic(source);
        }

        [Fact]
        public void SetPropertyInvocation_NotInGetter_DoesNotProvideDiagnostic()
        {
            var source =
@"class Class1
{
    private string _foo;

    public string Method1()
    {
        SetProperty(() => _foo = value, _foo, value);
    }
}";

            VerifyDiagnostic(source);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
            => new LegacySetPropertyUsingLambdaAnalyzer();
        protected override CodeFixProvider GetCodeFixProvider()
            => new LegacySetPropertyUsingLambdaCodeFixProvider();
    }
}
