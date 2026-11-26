using travelexpensemanagement.Models.Sales.OtherUnused;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.OtherUnused
{
    public interface IPackingSlipEntryListRepository
    {
        RepositoryResponseList<PackingSlipListModel> GetPackingSlipList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        Task<RepositoryResponse> Delete(int vNo, string docType);
    }
}
