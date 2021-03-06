﻿using System.Collections.Generic;
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
    public class AllMsTestToXunitTests : CodeRefactoringVerifier
    {
        // CONVERT ALL
        //  * Add action to convert all tests to xUnit
        //    * convert TI/TC -> ctor/dispose

        // TODO: handle class without [TestClass] attribute
        // TODO: handle class with compatibility ctor/disposer
        // TODO: handle methods with [TestMethod] multiple times? (to make it more robust)

        [Fact]
        public void EmptySource_DoesNotProvideRefactoring()
        {
            var source = "$$";
            VerifyNoRefactoring(source);
        }

        [Fact]
        public void ConvertAllFacts_WithoutMultipleFacts_DoesNotProvideRefactoring()
        {
            var source =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Class1
{
    [TestMethod]
    $$public void MyTestMethod1()
    {
    }
}";

            VerifyNoRefactoring(source);
        }

        [Fact]
        public void ConvertAllFacts_MultipleTestMethodsButPositionElsewhere_DoesNotProvideRefactoring()
        {
            var source =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Class1
$${
    [TestMethod]
    public void MyTestMethod1()
    {
    }

    [TestMethod]
    public void TestMethod()
    {
    }
}";

            VerifyNoRefactoring(source);
        }

        [Fact]
        public void ConvertAllFacts_WithMultipleTestMethods_ConvertsToFactsRemovesTestClassAttribute()
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
    public void TestMethod()
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

    [Fact]
    public void TestMethod()
    {
    }
}";
            VerifyRefactoring(oldSource, newSource, "Convert MSTest methods to Facts");
        }

        [Fact]
        public void ConvertAllFacts_WithTestInitialize_ConvertsToConstructor()
        {
            var oldSource =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Class1
{
    [TestInitialize]
    public void TestInitialize()
    {
        int i = 0;
    }

    [TestMethod]
    $$public void MyTestMethod1()
    {
    }

    [TestMethod]
    public void TestMethod()
    {
    }
}";

            var newSource =
@"using Xunit;

public class Class1
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
    public void TestMethod()
    {
    }
}";
            VerifyRefactoring(oldSource, newSource, "Convert MSTest methods to Facts");
        }

        [Fact]
        public void ConvertAllFacts_WithTestCleanup_ConvertsToDisposable()
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
    public void TestMethod()
    {
    }

    [TestCleanup]
    public void TestCleanup()
    {
        int i = 0;
    }
}";

            var newSource =
@"using System;
using Xunit;

public class Class1 : IDisposable
{
    [Fact]
    public void MyTestMethod1()
    {
    }

    [Fact]
    public void TestMethod()
    {
    }

    public void Dispose()
    {
        int i = 0;
    }
}";
            VerifyRefactoring(oldSource, newSource, "Convert MSTest methods to Facts");
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new AllMsTestsToXunitRefactoringProvider();
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