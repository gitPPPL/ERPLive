using travelexpensemanagement.Models;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IMiscConsumptionListRepository
    {
        (List<MiscConsumptionEntry_Header>, int) GetList(string searchTerm, int pageNumber, int pageSize);

        MiscConsumptionEntryModel GetDataByCode(int rowId, string vtype);

        Task<RepositoryResponse> Delete(string vNo, string docType);

        List<object> GetPendingDocuments(int partyId);

    }
}
