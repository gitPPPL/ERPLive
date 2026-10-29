using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IImportTrackingEntryListRepository
    {
        public RepositoryResponse DeleteImportTrackingEntry(int docId, string docType);
        public RepositoryResponseList<ImportTrackingEntryListModel> GetAllImportTrackingList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
    }
}
