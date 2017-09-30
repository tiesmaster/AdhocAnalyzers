using AdhocAnalyzers.Test.Helpers;

using Microsoft.CodeAnalysis.CodeRefactorings;

using Xunit;

namespace AdhocAnalyzers.Test
{
    public class StructureNamespaceUsingsRefactoringProviderTests : CodeRefactoringVerifier
    {
        [Fact]
        public void NamespacesWithoutDots()
        {
            var oldMarkupSource =
@"$$using System;
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

            VerifyRefactoring(oldMarkupSource, newSource, "Add newline betweeen using groups");
        }

        [Fact]
        public void NamespaceWithSingleDot()
        {
            var oldMarkupSource =
@"$$using System;
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

            VerifyRefactoring(oldMarkupSource, newSource, "Add newline betweeen using groups");
        }

        [Fact]
        public void GroupsSeparatedWithCommentAndNotNewline()
        {
            var oldMarkupSource =
@"$$using System;
using System.Text;
// Hoi
using Microsoft;

class Class1
{
}";
            var newSource =
@"using System;
using System.Text;

// Hoi
using Microsoft;

class Class1
{
}";

            VerifyRefactoring(oldMarkupSource, newSource, "Add newline betweeen using groups");
        }

        [Fact]
        public void NamespaceWithMultipleDots()
        {
            var oldMarkupSource =
@"$$using System;
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

            VerifyRefactoring(oldMarkupSource, newSource, "Add newline betweeen using groups");
        }

        [Fact]
        public void ListOfNamespacesWithMultipleGroups_ShouldAddNewLinesBetweenAllGroups()
        {
            var oldMarkupSource =
@"$$using System;
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

            VerifyRefactoring(oldMarkupSource, newSource, "Add newline betweeen using groups");
        }

        [Fact]
        public void ListOfNamespacesWithMultipleGroupsWhereSomeGroupsHaveMultipleUsings_ShouldOnlyAddNewlinesBetweenToplevelGroups()
        {
            var oldMarkupSource =
@"$$using System;
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

            VerifyRefactoring(oldMarkupSource, newSource, "Add newline betweeen using groups");
        }

        [Fact]
        public void GroupsWhichAreAlreadySeparated_ShouldNotGetTwoNewlines()
        {
            var oldMarkupSource =
@"$$using System;
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

            VerifyRefactoring(oldMarkupSource, newSource, "Add newline betweeen using groups");
        }

        [Fact]
        public void NoUsings_ShouldNotProvideRefactoring()
        {
            var markupSource =
@"$$class Class1
{
}";
            VerifyNoRefactoring(markupSource);
        }

        [Fact]
        public void OneUsings_ShouldNotProvideRefactoring()
        {
            var markupSource =
@"$$using System;

class Class1
{
}";
            VerifyNoRefactoring(markupSource);
        }

        [Fact]
        public void TwoUsingsOfSameGroup_ShouldNotProvideRefactoring()
        {
            var markupSource =
@"$$using System;
using System.Threading.Tasks;

class Class1
{
}";
            VerifyNoRefactoring(markupSource);
        }

        [Fact]
        public void TwoGroupsWithExistingSeparator_ShouldNotProvideRefactoring()
        {
            var markupSource =
@"$$using System;
using System.Threading.Tasks;

using Microsoft;

class Class1
{
}";
            VerifyNoRefactoring(markupSource);
        }

        [Fact]
        public void RefactoringShouldNotBeProvidedOutsideUsingsLocation()
        {
            var markupSource =
@"using System;
using System.Threading.Tasks;
using Microsoft;
$$
class Class1
{
}";
            VerifyNoRefactoring(markupSource);
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
            => new StructureNamespaceUsingsRefactoringProvider();
    }
}