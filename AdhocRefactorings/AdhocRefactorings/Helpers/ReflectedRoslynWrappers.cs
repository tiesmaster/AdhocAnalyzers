using System;
using System.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using AdhocRefactorings.Interfaces;

using Microsoft.CodeAnalysis;

namespace AdhocRefactorings.Helpers
{
    [Export(typeof(IOrganizeImportsServiceWrapper))]
    [Shared]
    public class ReflectedRoslynWrappers : IOrganizeImportsServiceWrapper
    {
        private readonly Lazy<Func<Document, Task<Document>>> _organizeImportsCallbackLazy;

        public ReflectedRoslynWrappers() =>
            _organizeImportsCallbackLazy = new Lazy<Func<Document, Task<Document>>>(RetrieveOrganizeImportsCallback);

        private Func<Document, Task<Document>> RetrieveOrganizeImportsCallback()
        {
            // TODO: remove hardcoded version number, and derive it from typeof(Document)

            const string fullAssemblyName = "Microsoft.CodeAnalysis.Features, Version=2.2.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

            var roslynFeaturesAssembly = Assembly.Load(new AssemblyName(fullAssemblyName));

            var staticServiceType = roslynFeaturesAssembly
                .GetType("Microsoft.CodeAnalysis.OrganizeImports.OrganizeImportsService")
                .GetTypeInfo();

            var targetMethod = staticServiceType.GetDeclaredMethod("OrganizeImportsAsync");
            return document => (Task<Document>)targetMethod.Invoke(null, new object[] { document, true, CancellationToken.None });
        }

        public Task<Document> OrganizeImportsAsync(Document document)
        {
            var callback = _organizeImportsCallbackLazy.Value;
            return callback(document);
        }
    }
}