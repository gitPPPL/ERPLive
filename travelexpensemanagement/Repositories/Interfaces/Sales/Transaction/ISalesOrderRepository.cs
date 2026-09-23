using System.Text.Json;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISalesOrderRepository
    {
        Task<RepositoryResponseData<object>> GetPartyAddress(int code, int addressId);

        Task<RepositoryResponseData<object>> GetSaudaDataList(string saudaNo);

        Task<RepositoryResponseData<object>> GetIssueDetails(string issueNo);

        Task<RepositoryResponseData<object>> GetPackingDetail(int vno, string packingNo, string doctype);

        Task<RepositoryResponseData<object>> GetLinkedOrders(int saudaNo);

        Task<RepositoryResponseData<object>> GetSaudaRate(GetSaudaRateRequest request);

        Task<RepositoryResponseData<decimal>> GetPackAmount(int itemCode);

        Task<RepositoryResponseData<List<CalculateAmtItemModel>>> GetCalculateAmtData(int itemCode);

        Task<RepositoryResponse> ValidateData(ValidateDataModel model);

        Task<RepositoryResponseData<object>> GetPurchaseOrderRecordsById(int id, string vType);

        Task<RepositoryResponseData<object>> SaveOrUpdateSalesOrder(PurchaseOrder POmodel, string doctype);

        RepositoryResponseData<object> GetOrderAdjustment(int ordNo);

        RepositoryResponse SaveDispatchDetails(List<DispatchDeliveryPlaning> dispatch, string doctype);

        RepositoryResponseData<List<DispatchDeliveryPlaning>> GetDispatchDeliveryPlan(int vNo, string doctype);

        Task<(bool IsApprovalRequired, string Message)> CheckSaudaApproval(PurchaseOrder model);
    }
}
