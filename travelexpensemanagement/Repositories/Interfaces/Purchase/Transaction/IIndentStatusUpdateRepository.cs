using static travelexpensemanagement.Models.Purchase.Transaction.IndentStatusUpdateModel;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IIndentStatusUpdateRepository
    {
        Task<List<StorePurchaseOrderStatusModel>> GetStorePurchaseOrderStatusAsync(DateTime fromDate, DateTime toDate, int? supplierCode);

        Task<(bool Success, string Message)> SaveIndentStatusAsync(List<IndentStatusUpdateSaveModel> model);

    }
}
