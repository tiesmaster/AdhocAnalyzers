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
            var assemblyName = typeof(Document).GetTypeInfo().Assembly.GetName();
            assemblyName.Name = "Microsoft.CodeAnalysis.Features";

            var roslynFeaturesAssembly = Assembly.Load(assemblyName);

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