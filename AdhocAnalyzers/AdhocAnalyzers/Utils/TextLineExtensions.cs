using Microsoft.CodeAnalysis.Text;

namespace AdhocAnalyzers.Utils
{
    public static class TextLineExtensions
    {
        public static bool IsEmpty(this TextLine line)
            => line.Start == line.End;
    }
}