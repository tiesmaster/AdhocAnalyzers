using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdhocAnalyzers.Test.Helpers;
using AdhocAnalyzers.Xunit;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Xunit;

namespace AdhocAnalyzers.Test.Xunit
{
    public class MstestToXunitTests : CodeRefactoringVerifier
    {
        // TODO:
        //  * Add action to convert TestMethod -> Fact (fix namespace import)
        //    + Add support to also add ctor/dispose, that will call TI/TC
        //  * Add action to remove last mstest TM (and remove the mstest namespace import)
        //     + Add support to convert TI/TC into ctor/dispose, and cleanup added ctor, and dispose
        //  * Add action to convert remaining test to xUnit, and
        //      cleanup MStest -> xUnit bridge (remove TI/TC, and 

        // CONVERT ALL
        //  * Add action to convert all tests to xUnit
        //    * convert TM -> Fact
        //    * convert TI/TC -> ctor/dispose
        //    * remove mstest namespace import

        [Fact]
        public void EmptySource_ShouldNotProvideRefactoring()
        {
            var source = "$$";
            VerifyNoRefactoring(source);
        }

        [Fact]
        public void UnitTestWithExistingFacts_ConvertsToFact_And_DoesNotAddNamespace()
        {
            var oldSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

[TestClass]
public class Class1
{
    [TestMethod]
    $$public void MyTestMethod1()
    {
    }

    [TestMethod]
    public void MyTestMethod2()
    {
    }

    [Fact]
    public void Fact()
    {
    }
}";

            var newSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

[TestClass]
public class Class1
{
    [Fact]
    public void MyTestMethod1()
    {
    }

    [TestMethod]
    public void MyTestMethod2()
    {
    }

    [Fact]
    public void Fact()
    {
    }
}";

            VerifyRefactoring(oldSource, newSource, "Convert MSTest method to Fact");
        }

        // TODO: verify trivia around Fact (test: [TestMethod/*foo*/] ...)

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new MstestToXunitTRefactoringProvider();
        }
    }
}