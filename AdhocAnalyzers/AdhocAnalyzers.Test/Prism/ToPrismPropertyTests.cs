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
        public void PropertyWithBackingField_PositionOutsideProperty_ShouldNotProvideRefactoring(int position)
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

        [Theory]
        [InlineData(0)]
        [InlineData(21)]
        [InlineData(57)]
        public void AutoProperty_ShouldNotProvideRefactoring(int position)
        {
            var source =
@"class Class1
{
    public int Property1 { get; set; }
}";

            VerifyNoRefactoring(source, position);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(52)]
        [InlineData(239)]
        public void PropertyAlreadyPrismProperty_ShouldNotProvideRefactoring(int position)
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
            SetProperty(ref _property1, value);
        }
    }
}";

            VerifyNoRefactoring(source, position);
        }

        [Fact]
        public void Property_WithRegularBackingField_ShouldProvideRefactoring()
        {
            var oldSource =
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

            var newSource =
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
            SetProperty(ref _property1, value);
        }
    }
}";

            VerifyRefactoring(oldSource, newSource, 52, "Convert to PRISM property");
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
            => new ToPrismPropertyRefactoringProvider();
    }
}