using System.Collections.Generic;

using AdhocAnalyzers.Prism;
using AdhocAnalyzers.Test.Helpers;
using AdhocAnalyzers.Test.Helpers.Xunit;

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;

using Xunit;

namespace AdhocAnalyzers.Test.Prism
{
    public class ToPrismPropertyTests : CodeRefactoringVerifier
    {
        [Fact]
        public void EmptySource_ShouldNotProvideRefactoring()
        {
            var markupSource = "$$";
            VerifyNoRefactoring(markupSource);
        }

        [Fact]
        public void EmptyClass_ShouldNotProvideRefactoring()
        {
            var markupSource =
@"$$class Class1
{
}";

            VerifyNoRefactoring(markupSource);
        }

        [Theory]
        [MarkupData(
@"[||]class Class1
{
    private int _property1;

    [||]public int Property1
    {
        [||]get
        {
            return _property1;
        }
        [||]set
        {
            _property1 = value;
        }[||]
    }
}")]
        public void PropertyWithBackingField_PositionOutsideProperty_ShouldNotProvideRefactoring(string markupSource)
        {
            VerifyNoRefactoring(markupSource);
        }

        [Theory]
        [MarkupData(
@"[||]class Class1
{
    [||]public int Property1 { get; set; }
[||]}")]
        public void AutoProperty_ShouldNotProvideRefactoring(string markupSource)
        {
            VerifyNoRefactoring(markupSource);
        }

        [Theory]
        [MarkupData(
@"[||]class Class1
{
    private int _property1;

    [||]public int Property1
    {
        [||]get
        {
            return _property1;
        }
        [||]set
        {
            [||]SetProperty(ref _property1, value);
        }
    }
}")]
        public void PropertyAlreadyPrismProperty_ShouldNotProvideRefactoring(string markupSource)
        {
            VerifyNoRefactoring(markupSource);
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
            VerifyRefactoringOld(oldSource, newSource, 184, "Convert to PRISM property");
        }

        [Fact]
        public void Property_WithOnlySetterSettingBackingField_ShouldProvideRefactoring()
        {
            var oldSource =
@"class Class1
{
    private int _property1;

    public int Property1
    {
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
        set
        {
            SetProperty(ref _property1, value);
        }
    }
}";
            VerifyRefactoringOld(oldSource, newSource, 117, "Convert to PRISM property");
        }

        [Fact]
        public void Property_AccessorsAreExpressionBodies_ShouldProvideRefactoring()
        {
            var oldSource =
@"class Class1
{
    private int _property1;

    public int Property1
    {
        get => _property1;
        set => _property1 = value;
    }
}";

            var newSource =
@"class Class1
{
    private int _property1;

    public int Property1
    {
        get => _property1;
        set => SetProperty(ref _property1, value);
    }
}";
            VerifyRefactoringOld(oldSource, newSource, 124, "Convert to PRISM property");
        }

        [Fact]
        public void Property_SetterWithAdditionalLogic_ShouldProvideRefactoring()
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
            OnPropertyChanged();
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
            OnPropertyChanged();
        }
    }
}";
            VerifyRefactoringOld(oldSource, newSource, 184, "Convert to PRISM property");
        }

        [Fact]
        public void Property_WorkspaceWithDifferentFormattingOptions_ShouldReturnCodeFormattedAccordingToThat()
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
            SetProperty(ref _property1,value);
        }
    }
}";
            var changedOptionSet = new Dictionary<OptionKey, object> { [CSharpFormattingOptions.SpaceAfterComma] = false };
            VerifyRefactoringOld(oldSource, newSource, 184, "Convert to PRISM property", changedOptionSet);
        }

        [Fact]
        public void Property_WithOnlyGetter_ShouldNotProvideRefactoring()
        {
            var markupSource =
@"class Class1
{
    private int _property1;

    public int Property1
    {
        get
        {
            $$return _property1;
        }
    }
}";

            VerifyNoRefactoring(markupSource);
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
            => new ToPrismPropertyRefactoringProvider();
    }
}