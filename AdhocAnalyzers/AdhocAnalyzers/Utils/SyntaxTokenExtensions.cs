using Microsoft.CodeAnalysis;

namespace AdhocAnalyzers.Utils
{
    public static class SyntaxTokenExtensions
    {
        public static bool IsNamed(this SyntaxToken identifierToken, string identifierString)
            => identifierToken.Text == identifierString;
    }
}