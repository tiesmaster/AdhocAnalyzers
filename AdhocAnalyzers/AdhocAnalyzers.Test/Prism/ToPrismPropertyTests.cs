using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdhocAnalyzers.Prism;
using AdhocAnalyzers.Test.Helpers;

using Microsoft.CodeAnalysis.CodeRefactorings;
using Xunit;

namespace AdhocAnalyzers.Test.Prism
{
    public class ToPrismPropertyTests : CodeRefactoringVerifier
    {
        [Fact]
        public void EmptySource_ShouldNotProvideRefactoring()
        {
            var source = "";
            VerifyNoRefactoring(source, 0);
        }

        [Fact]
        public void EmptyClass_ShouldNotProvideRefactoring()
        {
            var source =
@"class Class1
{
}";

            VerifyNoRefactoring(source, 0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(52)]
        [InlineData(221)]
        public void ClassWithPrismProperty_ShouldNotProvideRefactoring(int position)
        {
            var source =
@"class Class1
{
    private int _property1;

    public int Property1
    {
        get
        {
            return _property1;
        }
        set
        {
            _property1 = value;
        }
    }
}";

            VerifyNoRefactoring(source, position);
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
            => new ToPrismPropertyRefactoringProvider();
    }
}