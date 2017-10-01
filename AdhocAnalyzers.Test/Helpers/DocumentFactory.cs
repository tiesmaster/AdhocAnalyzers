using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace AdhocAnalyzers.Test.Helpers
{
    public static class DocumentFactory
    {
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).GetTypeInfo().Assembly.Location);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).GetTypeInfo().Assembly.Location);

        private static MetadataReference[] DefaultmetadataReferences
            => new[] {
                CorlibReference,
                SystemCoreReference,
                CSharpSymbolsReference,
                CodeAnalysisReference
            };
        internal static string TestProjectName = "TestProject";

        public static Document CreateDocument(string source, params MetadataReference[] additionalMetadataReferences)
        {
            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp)
                .AddMetadataReferences(projectId, DefaultmetadataReferences.Concat(additionalMetadataReferences));

            var newFileName = "Test0.cs";
            var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
            solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));

            return solution.GetProject(projectId).Documents.First();
        }
    }
}