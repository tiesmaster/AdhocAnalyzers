using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdhocAnalyzers.AutoFixture;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

using TestHelper;

using Xunit;

namespace AdhocAnalyzers.Test.AutoFixture
{
    public class UnneededBuilderPatternTests : CodeFixVerifier
    {
        [Fact]
        public void EmptySourceNoDiagnostics()
        {
            var test = "";

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void BuildWithKeywordType()
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

            var expected = new DiagnosticResult
            {
                Id = "AF0001",
                Message = "Build<string>() directly followed by Create() can be simplified",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 9)
                        }
            };

            VerifyCSharpDiagnostic(oldSource, expected);
            VerifyCSharpFix(oldSource, newSource, allowNewCompilerDiagnostics: false);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new UnneededBuilderPatternAnalyzer();
        protected override CodeFixProvider GetCSharpCodeFixProvider() => new UnneededBuilderPatternCodeFixProvider();
    }
}