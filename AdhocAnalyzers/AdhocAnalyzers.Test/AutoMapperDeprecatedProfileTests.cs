using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

using TestHelper;

using Xunit;

namespace AdhocAnalyzers.Test
{
    public class AutoMapperDeprecatedProfileTests : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [Fact]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void AnalyzerTest()
        {
            var test = @"
public class SomeProfile : Profile
{
    protected override void Configure()
    {
        CreateMap<int, string>();
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "AM0001",
                Message = "Class 'SomeProfile' is not upgraded yet to AutoMapper V5",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 4, 5)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void CodeFixTest()
        {
            var test = @"
public class SomeProfile : Profile
{
    protected override void Configure()
    {
        CreateMap<int, string>();
    }
}";

            var fixtest = @"
public class SomeProfile : Profile
{
    public SomeProfile()
    {
        CreateMap<int, string>();
    }
}";
            VerifyCSharpFix(test, fixtest, allowNewCompilerDiagnostics: true);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AutoMapperDeprecatedProfileAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AutoMapperDeprecatedProfileCodeFixProvider();
        }
    }
}