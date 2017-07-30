using System;
using AdhocRefactorings.AutoFixture;
using AdhocRefactorings.Test.Helpers;

using Microsoft.CodeAnalysis.CodeRefactorings;

using Xunit;

namespace AdhocRefactorings.Test.AutoFixture
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

            VerifyRefactoring(oldSource, newSource, 100, "Convert Create<...>() to Build<...>().Create()");
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
        fixture.Create<Class1>();
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

            VerifyRefactoring(oldSource, newSource, 100, "Convert Create<...>() to Build<...>().Create()");
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
        foo.Create<string>();
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

            VerifyRefactoring(oldSource, newSource, 92, "Convert Create<...>() to Build<...>().Create()");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(14)]
        [InlineData(45)]
        [InlineData(117)]
        public void NotAvailableOutsideInvocationLine(int position)
        {
            var source =
@"class Class1
{
    void Method1()
    {
        var fixture = new Fixture();
        fixture.Create<string>();
    }
}";
            VerifyNoRefactoring(source, position);
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
        fixture.Create<string, int>();
    }
}";
            VerifyNoRefactoring(source, 100);
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
        fixture.Foo<string>();
    }
}";
            VerifyNoRefactoring(source, 100);
        }

        [Fact]
        public void NotAvailableOnConstructors()
        {
            var source =
@"class Class1
{
    void Method1()
    {
        new Create<string>();
    }
}";
            VerifyNoRefactoring(source, 56);
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
        Func<string> foo = fixture.Create<string>;
    }
}";
            VerifyNoRefactoring(source, 117);
        }

        // TODO: add support for providing the refactoring for the entire line of "fixture...., until the end of it

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new CreateToBuildRefactoringProvider();
        }
    }
}