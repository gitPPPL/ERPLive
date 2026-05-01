using travelexpensemanagement.Models;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface ITransitEntryListRepository
    {
        Task<RepositoryResponseList<TransitEntryModel>> GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        Task<RepositoryResponseData<TransitEntryModel>> GetById(int code, string vtype);
        Task<RepositoryResponse> DeleteById(int code, string VType);
    }
}
