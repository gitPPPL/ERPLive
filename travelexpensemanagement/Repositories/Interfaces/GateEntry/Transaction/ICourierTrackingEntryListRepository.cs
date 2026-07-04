using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Repositories;

public interface ICourierTrackingEntryListRepository
{
    RepositoryResponseList<GetCourierTrackingModel> GetCourierTrackingEntryList(
        string searchTerm,
        int pageNumber,
        int pageSize);

    Task<RepositoryResponse> DeleteCourierTrackingEntry(string vNo, string docType);
}