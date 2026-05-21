using System.Dynamic;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction
{
    public interface IStoreWeighbridgeEntryListRepository
    {
        Task<RepositoryResponseList<dynamic>> GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        Task<RepositoryResponse> DeleteStoreWb(string docId);
        Task<RepositoryResponseList<ExpandoObject>> StoreWBDetails(string docId);
        Task<RepositoryResponseList<ExpandoObject>> ExportAllDocs();
        Task<RepositoryResponseData<string>> ValidateDeleteStoreWb(string docId);
    }
}
