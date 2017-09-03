using System;

using Microsoft.CodeAnalysis;

namespace AdhocAnalyzers.Test.Helpers
{
    public struct DiagnosticResultLocation
    {
        public DiagnosticResultLocation(string path, int line, int column)
        {
            if (line < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
            }

            if (column < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
            }

            Path = path;
            Line = line;
            Column = column;
        }

        public string Path { get; }
        public int Line { get; }
        public int Column { get; }
    }

    public struct DiagnosticResult
    {
        private DiagnosticResultLocation[] locations;

        public string Id { get; set; }
        public string Message { get; set; }
        public DiagnosticSeverity Severity { get; set; }

        public DiagnosticResultLocation[] Locations
        {
            get
            {
                if (locations == null)
                {
                    locations = new DiagnosticResultLocation[] { };
                }
                return locations;
            }

            set => locations = value;
        }

        public string Path => Locations.Length > 0 ? Locations[0].Path : "";
        public int Line => Locations.Length > 0 ? Locations[0].Line : -1;
        public int Column => Locations.Length > 0 ? Locations[0].Column : -1;
    }
}