using AdhocAnalyzers.AutoFixture;
using AdhocAnalyzers.Test.Helpers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

using Xunit;

namespace AdhocAnalyzers.Test.AutoFixture
{
    public class UnneededBuilderPatternTests : CodeFixVerifier
    {
        [Fact]
        public void EmptySourceNoDiagnostics()
        {
            var test = "";

            VerifyDiagnostic(test);
        }

        [Fact]
        public void BuildAndCreateOnOneLine()
        {
            var oldSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Build<string>().Create();
    }
}";
            var newSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Create<string>();
    }
}";

            var expected = new DiagnosticResult2
            {
                Id = "AF0001",
                Message = "Build<string>() directly followed by Create() can be simplified",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 9)
                        }
            };

            VerifyDiagnostic(oldSource, expected);
            VerifyFix(oldSource, newSource);
        }

        [Fact]
        public void BuildAndCreateOnMultipleLines()
        {
            var oldSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Build<string>()
               .Create();
    }
}";
            var newSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Create<string>();
    }
}";

            var expected = new DiagnosticResult2
            {
                Id = "AF0001",
                Message = "Build<string>() directly followed by Create() can be simplified",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 9)
                        }
            };

            VerifyDiagnostic(oldSource, expected);
            VerifyFix(oldSource, newSource);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() => new UnneededBuilderPatternAnalyzer();
        protected override CodeFixProvider GetCodeFixProvider() => new UnneededBuilderPatternCodeFixProvider();
    }
}