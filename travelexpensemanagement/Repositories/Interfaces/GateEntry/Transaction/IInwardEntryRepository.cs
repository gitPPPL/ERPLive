

using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Models.GateEntry.Transaction;
using static travelexpensemanagement.Controllers.GateEntry.Transaction.InwardEntryController;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IInwardEntryRepository
    {
        Task<string> GetVNoAsync(string vType, string tableName = "GATE1");
        Task<List<object>> GetDataByPartyCodeAsync(int partyId, int addressId);

        Task<List<object>> GetPartyAddressByCodeAsync(int partyId);
        Task<List<object>> FetchShipFromAddressAsync(int shipFromId);

        Task<RepositoryResponse> ValidateBillNoAsync(int partyCode, string billNo, int vNo);

        Task<RepositoryResponse> ValidateGateNoAsync(string vType, int vNo);

        Task<RcRequest> GetVehicleInfoAsync(string rcNumber);
        Task<ApiResponse> SaveVehicleInfoAsync(RcRequest vehicleInfo, string vType, int vNo);
        Task<RcRequest?> GetVehicleDetailAsync(int vNo, string vType);

    }
}
