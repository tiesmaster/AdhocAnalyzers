using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using AdhocAnalyzers.Test.Helpers;
using AdhocAnalyzers.Test.Helpers.Xunit;
using AdhocAnalyzers.Xunit;

using Microsoft.CodeAnalysis;
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

        [Theory]
        [MarkupData(
@"using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

[TestClass]
public class Class1
{
    [||][TestMethod]
    [||]public void MyTestMethod1()
    {[||]
    }

    [TestMethod]
    public void MyTestMethod2()
    {
    }

    [Fact]
    public void Fact()
    {
    }
}")]
        public void UnitTestWithExistingFacts_ConvertsToFact_And_DoesNotAddNamespace(string markupSource)
        {
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

            VerifyRefactoring(markupSource, newSource, "Convert MSTest method to Fact");
        }

        [Fact]
        public void UnitTestWithoutExistingFactsButMultipleTestMethods_ConvertsToFact_And_AddsNamespace()
        {
            var oldSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}";

            VerifyRefactoring(oldSource, newSource, "Convert MSTest method to Fact");
        }

        [Fact]
        public void UnitTestWithOnlySingleTestMethod_ConvertsToFact_RemovesTestClassAttribute_And_MsTestNamespace()
        {
            var oldSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Class1
{
    [TestMethod]
    $$public void MyTestMethod1()
    {
    }
}";

            var newSource =
@"using Xunit;

public class Class1
{
    [Fact]
    public void MyTestMethod1()
    {
    }
}";

            VerifyRefactoring(oldSource, newSource, "Convert MSTest method to Fact");
        }

        [Fact]
        public void TestMethodAttributeWithComments_ConvertsToFact_KeepsTrivia()
        {
            var oldSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Class1
{
    [TestMethod/*foo*/]
    $$public void MyTestMethod1()
    {
    }
}";

            var newSource =
@"using Xunit;

public class Class1
{
    [Fact/*foo*/]
    public void MyTestMethod1()
    {
    }
}";

            VerifyRefactoring(oldSource, newSource, "Convert MSTest method to Fact");
        }

        [Fact]
        public void UnitTestWithMultipleTestMethodsAndTestInitializeAndCleanup_ConvertsToFact_AddsConstructorAndDisposer()
        {
            var oldSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Class1
{
    [TestInitialize]
    public void Setup()
    {
    }

    [TestMethod]
    $$public void MyTestMethod1()
    {
    }

    [TestMethod]
    public void MyTestMethod2()
    {
    }

    [TestCleanup]
    public void Cleanup()
    {
    }
}";

            var newSource =
@"using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

[TestClass]
public class Class1 : IDisposable
{
    public Class1()
    {
        Setup();
    }

    [TestInitialize]
    public void Setup()
    {
    }

    [Fact]
    public void MyTestMethod1()
    {
    }

    [TestMethod]
    public void MyTestMethod2()
    {
    }

    [TestCleanup]
    public void Cleanup()
    {
    }

    public void Dispose()
    {
        Cleanup();
    }
}";

            VerifyRefactoring(oldSource, newSource, "Convert MSTest method to Fact");
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new MstestToXunitTRefactoringProvider();
        }

        protected override IEnumerable<MetadataReference> AdditionalMetadataReferences
        {
            get
            {
                var factAttributeTypeInfo = typeof(FactAttribute).GetTypeInfo();

                var mscorlibFacadesAssemblyName = factAttributeTypeInfo
                    .Assembly
                    .GetReferencedAssemblies()
                    .Single(asm => asm.Name == "System.Runtime");
                var mscorlibFacadesAssembly = Assembly.Load(mscorlibFacadesAssemblyName);

                yield return MetadataReference.CreateFromFile(mscorlibFacadesAssembly.Location);
                yield return MetadataReference.CreateFromFile(factAttributeTypeInfo.Assembly.Location);
            }
        }
    }
}