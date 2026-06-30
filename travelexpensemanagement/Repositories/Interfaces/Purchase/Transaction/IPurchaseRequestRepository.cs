using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseRequestModel;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseRequestRepository
    {
        RepositoryResponseData<bool> CheckIsApprovalBody();
        Task<RepositoryResponseData<bool>> CheckIsFinalApprovalBodyAsync();
        RepositoryResponseData<decimal?> GetApporxiateRate(int itemCode);
        RepositoryResponseData<decimal?> GetPendingQty(int itemCode);
        RepositoryResponseData<decimal?> GetTotalQty(int itemCode);
        RepositoryResponseData<string> GetTECH_DESC(int itemCode);
        RepositoryResponseData<decimal?> GetCurrentStock(int itemCode);
        RepositoryResponseData<decimal> GetAvgConsumption(int itemCode, DateTime vDate);
        Task<RepositoryResponse> SaveData(PurchaseRequest_model request);
        Task<RepositoryResponseData<string>> GetPurchaseRequestsAsync(int itemCode, int deptCode, int vNo);
        Task<RepositoryResponseData<bool>> GetItemMakeAsync(int itemCode, int makeCode);
        Task<RepositoryResponseData<bool>> CheckMonthlyReqAsync(int itemCode);
        Task<RepositoryResponseData<bool>> GetMaxRequestCountAsync(int vNo, DateTime vDate);
        RepositoryResponseData<string> GetApprovalStatus(int vNo);
        RepositoryResponseData<bool> ValidateDepartmentAccess(int deptCode);
        RepositoryResponseData<List<LastTenPurchaseRequestModel>> GetLastTenPurchaseRequest(List<int> itemCodes);
        RepositoryResponseData<List<LastTenConsumptionModel>> GetLastTenConsumptionDetails(List<int> itemCodes);
        RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetLastTenPurchaseHistory(List<int> itemCodes);
        RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetLastTenOrderHistory(List<int> itemCodes);
        RepositoryResponseData<List<LastTenPurchaseRequestModel>> GetItemWisePurchaseRequest(int itemCode);
        RepositoryResponseData<List<LastTenConsumptionModel>> GetItemWiseConsumptionHistory(int itemCode);
        RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetItemWisePurchaseOrderHistory(int itemCode);
        RepositoryResponseData<List<ItemWisePurchaseQuotationHistoryModel>> GetItemWisePurchaseQuotationHistory(int itemCode);
        RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetItemWisePurchaseReceiptHistory(int itemCode);
        RepositoryResponseData<List<LastTenPurchaseHistoryModel>> GetItemWisePurchaseHistory(int itemCode);
        RepositoryResponseData<string> PRPrintRequest(PRPrintModel model);
        RepositoryResponseData<(bool isExist, string userName)> CheckApprovalStatus(int vNo);
    }
}
