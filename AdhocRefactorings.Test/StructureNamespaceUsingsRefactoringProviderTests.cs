using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdhocRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings;
using AdhocRefactorings.Test.Helpers;
using Xunit;

namespace AdhocRefactorings.Test
{
    public class StructureNamespaceUsingsRefactoringProviderTests : CodeRefactoringVerifier
    {
        [Fact]
        public void NamespacesWithoutDots()
        {
            var oldSource =
@"using System;
using Microsoft;

class Class1
{
}";
            var newSource =
@"using System;

using Microsoft;

class Class1
{
}";

            VerifyRefactoring(oldSource, newSource, 0, "Add newline betweeen using groups");
        }

        [Fact]
        public void NamespaceWithSingleDot()
        {
            var oldSource =
@"using System;
using System.Text;
using Microsoft;

class Class1
{
}";
            var newSource =
@"using System;
using System.Text;

using Microsoft;

class Class1
{
}";

            VerifyRefactoring(oldSource, newSource, 0, "Add newline betweeen using groups");
        }

        [Fact]
        public void NamespaceWithMultipleDots()
        {
            var oldSource =
@"using System;
using System.Threading.Tasks;
using Microsoft;

class Class1
{
}";
            var newSource =
@"using System;
using System.Threading.Tasks;

using Microsoft;

class Class1
{
}";

            VerifyRefactoring(oldSource, newSource, 0, "Add newline betweeen using groups");
        }

        [Fact]
        public void ListOfNamespacesWithMultipleGroups_ShouldAddNewLinesBetweenAllGroups()
        {
            var oldSource =
@"using System;
using System.Threading.Tasks;
using Microsoft;
using Xunit;

class Class1
{
}";
            var newSource =
@"using System;
using System.Threading.Tasks;

using Microsoft;

using Xunit;

class Class1
{
}";

            VerifyRefactoring(oldSource, newSource, 0, "Add newline betweeen using groups");
        }

        [Fact]
        public void ListOfNamespacesWithMultipleGroupsWhereSomeGroupsHaveMultipleUsings_ShouldOnlyAddNewlinesBetweenToplevelGroups()
        {
            var oldSource =
@"using System;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis;
using Xunit;

class Class1
{
}";
            var newSource =
@"using System;
using System.Threading.Tasks;

using Microsoft;
using Microsoft.CodeAnalysis;

using Xunit;

class Class1
{
}";

            VerifyRefactoring(oldSource, newSource, 0, "Add newline betweeen using groups");
        }

        [Fact]
        public void GroupsWhichAreAlreadySeparated_ShouldNotGetTwoNewlines()
        {
            var oldSource =
@"using System;
using System.Threading.Tasks;
using Microsoft;

using Xunit;

class Class1
{
}";
            var newSource =
@"using System;
using System.Threading.Tasks;

using Microsoft;

using Xunit;

class Class1
{
}";

            VerifyRefactoring(oldSource, newSource, 0, "Add newline betweeen using groups");
        }

        [Fact]
        public void NoUsings_ShouldNotProvideRefactoring()
        {
            var source =
@"class Class1
{
}";
            VerifyNoRefactoring(source, 0);
        }

        [Fact]
        public void OneUsings_ShouldNotProvideRefactoring()
        {
            var source =
@"using System;

class Class1
{
}";
            VerifyNoRefactoring(source, 0);
        }

        [Fact]
        public void TwoUsingsOfSameGroup_ShouldNotProvideRefactoring()
        {
            var source =
@"using System;
using System.Threading.Tasks;

class Class1
{
}";
            VerifyNoRefactoring(source, 0);
        }

        [Fact]
        public void TwoGroupsWithExistingSeparator_ShouldNotProvideRefactoring()
        {
            var source =
@"using System;
using System.Threading.Tasks;

using Microsoft;

class Class1
{
}";
            VerifyNoRefactoring(source, 0);
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new StructureNamespaceUsingsRefactoringProvider();
        }
    }
}