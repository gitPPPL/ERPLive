using System.Dynamic;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IQuotationRateApprovalListRepository
    {
        Task<IReadOnlyList<ExpandoObject>> GetQuotationRateListAsync();

        Task<IReadOnlyList<ExpandoObject>> GetPurchaseReceiptHistoryAsync(string itemCode);

        Task<IReadOnlyList<ExpandoObject>> GetQuotationApprovalHistoryAsync(string itemCode);

        Task<IReadOnlyList<ExpandoObject>> GetPurchaseOrderHistoryAsync(string itemCode);

        Task<IReadOnlyList<ExpandoObject>> GetPurchaseOrderApprovalEntryDetailsAsync(string docId);

        Task<IReadOnlyList<ExpandoObject>> ExportAllDocumentsAsync();

        Task<int> DeleteQuotationRateApprovalAsync(string docId);
    }
}
