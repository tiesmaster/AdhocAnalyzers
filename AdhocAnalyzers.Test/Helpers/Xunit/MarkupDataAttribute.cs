using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.Text;

using Roslyn.UnitTestFramework;

using Xunit.Sdk;

namespace AdhocAnalyzers.Test.Helpers.Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MarkupDataAttribute : DataAttribute
    {
        private readonly string _markup;

        public MarkupDataAttribute(string markup)
        {
            _markup = markup;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            MarkupTestFile.GetSpans(_markup, out var source, out IList<TextSpan> spans);
            return spans.Select(span => new object[] { source.Insert(span.Start, "$$") });
        }
    }
}