using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdhocAnalyzers.Test.Helpers
{
    public static class StringExtensions
    {
        public static string NormalizeLineEndingsToDos(this string input)
        {
            if (input.Contains("\n") && !input.Contains("\r\n"))
            {
                input = input.Replace("\n", "\r\n");
            }

            return input;
        }
    }
}