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

        // TODO: add support for providing the refactoring for the entire line of "fixture...., until the end of it

        // TODO: add missing tests for scenario where we handle NOT providing the refactoring 

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new CreateToBuildRefactoringProvider();
        }
    }
}