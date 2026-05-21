namespace travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction
{
    public interface IBigWeighbridgeListRepository
    {
        Task<(object Data, int TotalCount)> GetBigWBridgeListAsync(string searchTerm = "", int pageNumber = 1, int pageSize = 10);

        Task<(bool Status, string Message, string Data)> CheckDeleteBigWBridgeEntryAsync(string docid);

        Task<(bool Status, string Message)> DeleteBigWBridgeEntryAsync(string docid);

        Task<object> GetBigWBridgeEntryDetailsAsync(string docid);

        Task<object> ExportAllDocsAsync();
    }
}
