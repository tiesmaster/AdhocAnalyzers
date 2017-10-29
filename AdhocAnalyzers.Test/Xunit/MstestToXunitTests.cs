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
        public void ConvertSingleFact_WithExistingFacts_DoesNotAddNamespace(string markupSource)
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
        public void ConvertSingleFact_WithoutExistingXunitReferences_AddsNamespace()
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
        public void ConvertSingleFact_WithLastRemainingTestMethod_RemovesTestClassAttribute_And_MsTestNamespace()
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
        public void ConvertSingleFact_TestMethodAttributeWithComments_KeepsTrivia()
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
        public void ConvertSingleFact_WithTestInitializeAndCleanups_AddsConstructorAndDisposer()
        {
            var oldSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Class1
{
    [TestInitialize]
    public void TestInitialize()
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
    public void TestCleanup()
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
        TestInitialize();
    }

    [TestInitialize]
    public void TestInitialize()
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
    public void TestCleanup()
    {
    }

    public void Dispose()
    {
        TestCleanup();
    }
}";

            VerifyRefactoring(oldSource, newSource, "Convert MSTest method to Fact");
        }

        [Fact]
        public void ConvertSingleFact_AlreadyWithConstructorAndDisposer_DoesntAddThemAgain()
        {
            var oldSource =
@"using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

[TestClass]
public class Class1 : IDisposable
{
    public Class1()
    {
        TestInitialize();
    }

    [TestInitialize]
    public void TestInitialize()
    {
    }

    [Fact]
    public void MyTestMethod1()
    {
    }

    [TestMethod]
    $$public void MyTestMethod2()
    {
    }

    [TestMethod]
    public void MyTestMethod3()
    {
    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    public void Dispose()
    {
        TestCleanup();
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
        TestInitialize();
    }

    [TestInitialize]
    public void TestInitialize()
    {
    }

    [Fact]
    public void MyTestMethod1()
    {
    }

    [Fact]
    public void MyTestMethod2()
    {
    }

    [TestMethod]
    public void MyTestMethod3()
    {
    }

    [TestCleanup]
    public void TestCleanup()
    {
    }

    public void Dispose()
    {
        TestCleanup();
    }
}";
            VerifyRefactoring(oldSource, newSource, "Convert MSTest method to Fact");
        }

        [Fact]
        public void ConvertSingleFact_WithLastRemainingTestMethodAndTestInitializeAndCleanups_ConvertsThemToConstructorAndDisposer()
        {
            var oldSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Class1
{
    [TestInitialize]
    public void Setup()
    {
        int i = 0;
    }

    [TestMethod]
    $$public void MyTestMethod1()
    {
    }

    [TestCleanup]
    public void Cleanup()
    {
        int j = 1;
    }
}";

            var newSource =
@"using System;
using Xunit;

public class Class1 : IDisposable
{
    public Class1()
    {
        int i = 0;
    }

    [Fact]
    public void MyTestMethod1()
    {
    }

    public void Dispose()
    {
        int j = 1;
    }
}";
            VerifyRefactoring(oldSource, newSource, "Convert MSTest method to Fact");
        }

        [Fact]
        public void ConvertSingleFact_LastRemainingTestMethodAndAlreadyWithConstructorAndDisposer_CleansUpEverything()
        {
            var oldSource =
@"using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit;

[TestClass]
public class Class1 : IDisposable
{
    public Class1()
    {
        TestInitialize();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        int i = 0;
    }

    [Fact]
    public void MyTestMethod1()
    {
    }

    [TestMethod]
    $$public void MyTestMethod2()
    {
    }

    [TestCleanup]
    public void TestCleanup()
    {
        int j = 1;
    }

    public void Dispose()
    {
        TestCleanup();
    }
}";

            var newSource =
@"using System;
using Xunit;
public class Class1 : IDisposable
{
    public Class1()
    {
        int i = 0;
    }

    [Fact]
    public void MyTestMethod1()
    {
    }

    [Fact]
    public void MyTestMethod2()
    {
    }

    public void Dispose()
    {
        int j = 1;
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