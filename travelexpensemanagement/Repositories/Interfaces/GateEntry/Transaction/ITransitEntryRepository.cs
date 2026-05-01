using travelexpensemanagement.Models;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface ITransitEntryRepository
    {
        Task<RepositoryResponseData<string>> MaxVNo(string Vtype);
        RepositoryResponseList<object> GetDDl(string type, string VTypeId = "");
        Task<RepositoryResponseList<object>> PartyGstinNo(int Partycode);
        Task<RepositoryResponse> SaveData(TransitEntryModel data);
    }
}
