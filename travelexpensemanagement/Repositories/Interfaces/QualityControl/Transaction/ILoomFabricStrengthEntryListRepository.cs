namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction
{
    public interface ILoomFabricStrengthEntryListRepository
    {
        Task<(object Data, int TotalCount)> GetLoomFabricStrengthListAsync(string searchTerm, int pageNumber, int pageSize);

        Task<(bool Success, string Message)> DeleteLoomFabricStrengthEntryAsync(string docId);

        Task<object> GetLoomFabricStrengthEntryDetailsAsync(string docId);

        Task<object> ExportAllDocsAsync();

    }
}
