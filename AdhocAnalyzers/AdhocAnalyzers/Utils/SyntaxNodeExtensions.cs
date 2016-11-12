using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AdhocAnalyzers.Utils
{
    public static class SyntaxNodeExtensions
    {
        public static bool IsNamed(this IdentifierNameSyntax identifierNode, string identifierString)
            => identifierNode.Identifier.IsNamed(identifierString);

        public static SyntaxTrivia FindTriviaIncludingZeroLength(this SyntaxNode node, int position)
            => node.DescendantTrivia().Single(trivia => trivia.SpanStart == position);

        public static IEnumerable<int> GetNonEmptyLinePositions(this SyntaxNode node)
        {
            return
                from line in node.GetText().Lines
                where !line.IsEmpty()
                let offset = node.FullSpan.Start
                select offset + line.Start;
        }
    }
}
