using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AdhocAnalyzers.Prism;
using AdhocAnalyzers.Test.Helpers;

using Microsoft.CodeAnalysis.CodeRefactorings;
using Xunit;

namespace AdhocAnalyzers.Test.Prism
{
    public class ToPrismPropertyTests : CodeRefactoringVerifier
    {
        [Fact]
        public void EmptySource_ShouldNotProvideRefactoring()
        {
            var source = "";
            VerifyNoRefactoring(source, 0);
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
            => new ToPrismPropertyRefactoringProvider();
    }
}