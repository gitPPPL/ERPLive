using travelexpensemanagement.Models.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISalesOrderListRepository
    {
        RepositoryResponseList<SalesOrderListModel> GetSalesOrderList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        Task<RepositoryResponse> DeleteSalesOrderEntry(int vNo, string docType);
        RepositoryResponseData<PurchaseEditStatus> GetSalesEditStatus(string vType, int vNo);
        RepositoryResponseData<object> CheckSaleInvoiceBeforeDelete(string vType, int vNo);
    }
}
