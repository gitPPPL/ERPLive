namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction
{
    public interface IQCTemperatureEntryListRepository
    {
        Task<RepositoryResponseList<dynamic>> GetList(string searchTerm, int pageNumber, int pageSize);
        Task<RepositoryResponse> Delete(string docId);
    }
}
