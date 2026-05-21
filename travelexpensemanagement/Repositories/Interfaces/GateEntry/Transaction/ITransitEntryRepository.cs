using travelexpensemanagement.Models;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface ITransitEntryRepository
    {
        Task<RepositoryResponseData<bool>> IsExist(int vNo, string form_No);
        RepositoryResponseList<object> GetDDl(string type, string VTypeId = "");
        Task<RepositoryResponseList<object>> PartyGstinNo(int Partycode);
        Task<RepositoryResponse> SaveData(TransitEntryModel data);
    }
}
