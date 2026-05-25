

using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Models.GateEntry.Transaction;
using static travelexpensemanagement.Controllers.GateEntry.Transaction.InwardEntryController;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IInwardEntryRepository
    {

        Task<List<object>> GetDataByPartyCodeAsync(int partyId, int addressId);
        Task<List<object>> GetPartyAddressByCodeAsync(int partyId);
        Task<List<object>> FetchShipFromAddressAsync(int shipFromId);
        Task<RepositoryResponse> ValidateBillNoAsync(int partyCode, string billNo, int vNo);
        Task<RepositoryResponse> ValidateGateNoAsync(string vType, int vNo);
        Task<RepositoryResponseData<int>> GetSEARCHCONTAINERAsync(string Container_No);
        Task<RepositoryResponseList<int>>  DDlTransitNoAsync(string v_type, int v_no, int partycode, DateTime ExpiryDate);

    }
}
