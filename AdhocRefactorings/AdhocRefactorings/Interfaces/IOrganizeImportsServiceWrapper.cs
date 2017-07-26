using System.Threading.Tasks;

using Microsoft.CodeAnalysis;

namespace AdhocRefactorings.Interfaces
{
    public interface IOrganizeImportsServiceWrapper
    {
        Task<Document> OrganizeImportsAsync(Document document);
    }
}