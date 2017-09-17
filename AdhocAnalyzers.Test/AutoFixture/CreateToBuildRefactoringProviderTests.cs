using AdhocAnalyzers.AutoFixture;
using AdhocAnalyzers.Test.Helpers;
using AdhocAnalyzers.Test.Helpers.Xunit;
using Microsoft.CodeAnalysis.CodeRefactorings;

using Xunit;

namespace AdhocAnalyzers.Test.AutoFixture
{
    public class CreateToBuildRefactoringProviderTests : CodeRefactoringVerifier
    {
        [Fact]
        public void CreateWithKeywordType()
        {
            var oldSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.$$Create<string>();
    }
}";
            var newSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Build<string>().Create();
    }
}";

            VerifyRefactoring(oldSource, newSource, "Convert '.Create<string>()' to '.Build<string>().Create().");
        }

        [Fact]
        public void CreateWithType()
        {
            var oldSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.$$Create<Class1>();
    }
}";
            var newSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Build<Class1>().Create();
    }
}";

            VerifyRefactoring(oldSource, newSource, "Convert '.Create<Class1>()' to '.Build<Class1>().Create().");
        }

        [Fact]
        public void CreateOnDifferentVariableName()
        {
            var oldSource =
@"class Class1
{
    void Method1()
    {
        var foo = new Fixture();
        foo.$$Create<string>();
    }
}";
            var newSource =
@"class Class1
{
    void Method1()
    {
        var foo = new Fixture();
        foo.Build<string>().Create();
    }
}";

            VerifyRefactoring(oldSource, newSource, "Convert '.Create<string>()' to '.Build<string>().Create().");
        }

        [Theory]
        [MarkupData(
@"[||]class Class1
[||]{
    [||]void Method1()
    [||]{
        [||]var fixture = new Fixture();
        fixture.Create<string>();
    [||]}
[||]}")]
        public void NotAvailableOutsideInvocationLine(string markup)
        {
            VerifyNoRefactoring(markup);
        }

        [Fact]
        public void NotAvailableWithWrongGenericCreateMethod()
        {
            var source =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.$$Create<string, int>();
    }
}";
            VerifyNoRefactoring(source);
        }

        [Fact]
        public void NotAvailableWithWrongMethodName()
        {
            var source =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.$$Foo<string>();
    }
}";
            VerifyNoRefactoring(source);
        }

        [Fact]
        public void NotAvailableOnConstructors()
        {
            var source =
@"class Class1
{
    void Method1()
    {
        new $$Create<string>();
    }
}";
            VerifyNoRefactoring(source);
        }

        [Fact]
        public void NotAvailableOnIfCreateMethodIsNotInvoked()
        {
            var source =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        Func<string> foo = fixture.$$Create<string>;
    }
}";
            VerifyNoRefactoring(source);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(90)]
        [InlineData(112)]
        [InlineData(113)]
        [InlineData(82)]
        public void AvailableOnEntireLine(int positionWithRefactoring)
        {
            var oldSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Create<string>();
    }
}";
            var newSource =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Build<string>().Create();
    }
}";

            VerifyRefactoringOld(oldSource, newSource, positionWithRefactoring, "Convert '.Create<string>()' to '.Build<string>().Create().");
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider() => new CreateToBuildRefactoringProvider();
    }
}