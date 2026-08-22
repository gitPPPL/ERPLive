namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IImportPaymentListRepository
    {
        Task<(List<object> Data, int TotalCount)> LoadListDataAsync(string searchTerm = "",int pageNumber = 1,int pageSize = 10);

        Task<(bool Success, string Message)> DeleteImportPaymentEntryAsync(string docId);

    }
}
