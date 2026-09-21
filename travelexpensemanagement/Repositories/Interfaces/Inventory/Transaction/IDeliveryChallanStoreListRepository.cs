using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IDeliveryChallanStoreListRepository
    {
        RepositoryResponseList<DeliveryChallanStoreListModel> GetAllDeliveryChallanList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        RepositoryResponse Delete(int vNo, string docType);
        Task<RepositoryResponseData<bool>> GetApprovalStatus(int vNo, string vType);
    }
}
