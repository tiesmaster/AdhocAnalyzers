using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestHelper;

namespace AdhocAnalyzers.Test
{
    [TestClass]
    public class AutoMapperDeprecatedProfileTests : CodeFixVerifier
    {
        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
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

        [TestMethod]
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