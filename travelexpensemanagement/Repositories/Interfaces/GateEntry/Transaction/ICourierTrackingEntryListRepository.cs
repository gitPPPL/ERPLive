using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{

    public interface ICourierTrackingEntryListRepository
    {
        RepositoryResponseList<GetCourierTrackingModel> GetCourierTrackingEntryList(string searchTerm, int pageNumber, int pageSize);
        Task<RepositoryResponse> DeleteCourierTrackingEntry(string vNo, string docType);
    }
}
