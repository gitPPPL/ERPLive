using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseRequestModel;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseRequestListRepository
    {
        RepositoryResponseList<Header> GetPurchaseRequestList(string searchTerm, int pageNumber, int pageSize);
        RepositoryResponseData<PurchaseRequest_model> GetPurchaseRequestByCode(int code);
        Task<RepositoryResponseData<List<object>>> GetDataCopyFormAsync();
        Task<RepositoryResponseData<List<ItamDetails>>> GetMonthlyRequirementAsync(int deptId);
        RepositoryResponse DeletePurchaseRequest(int docId);
    }
}
