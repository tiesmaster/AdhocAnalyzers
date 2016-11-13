using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AdhocAnalyzers.Utils
{
    public static class SyntaxNodeManipulationExtensions
    {
        /// <summary>
        /// This adds indentation by manipulating the node
        /// </summary>
        public static SyntaxNode AddIndentationFromTrivia(this SyntaxNode oldNode, SyntaxTrivia indentation)
        {
            var baseIndentation = indentation.ToString();

            var tokensToLineStartPositionsDictionary = oldNode.GetNonEmptyLinePositions()
                .GroupBy(lineStart => oldNode.FindTriviaIncludingZeroLength(lineStart).Token)
                .ToDictionary(group => group.Key, group => group.ToList());

            return oldNode.ReplaceTokens(tokensToLineStartPositionsDictionary.Keys, (originalToken, rewrittenToken) =>
            {
                var triviasToReplace = tokensToLineStartPositionsDictionary[originalToken]
                    .Select(lineStart => originalToken.Parent.FindTriviaIncludingZeroLength(lineStart));
                return originalToken.ReplaceTrivia(triviasToReplace, (originalTrivia, rewrittenTrivia)
                    => SyntaxFactory.Whitespace(baseIndentation + originalTrivia));
            });
        }
    }
}