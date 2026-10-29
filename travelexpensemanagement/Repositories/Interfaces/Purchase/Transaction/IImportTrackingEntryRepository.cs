using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IImportTrackingEntryRepository
    {
        Task<RepositoryResponseData<ImportTrackingSaudaDetailsModel>> GetSaudaDetails(string saudaNo);
        Task<RepositoryResponseData<object>> GetEntryBySauda(string saudaNo, string vType);
        Task<RepositoryResponseData<object>> GetEntry(string vType, int vNo);
        Task<RepositoryResponse> Save(ImportTrackingSaveRequest req);
    }
}
