using travelexpensemanagement.Models.Weighbridge.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction
{
    public interface IBigWeighbridgeRepository
    {
        Task<(string DocId, string VNo)> GetMaxVNoAsync(string vType);

        Task<object> GetGateNoAsync(string wbType);

        Task<object> GetDocTypeAsync();

        Task<object> GetItemListAsync();

        Task<object> GetPlaceMastAsync();

        Task<object> GetPartyListAsync();

        Task<(object Header, object Detail)> GetWeighBridgeByIdAsync(string id);

        Task<(bool Status, string Message)> SaveOrUpdateWeighBridgeEntryAsync(WBEntryModel model);

        Task<(bool Status, string Message)> ValidateMRNAsync(WBEntryModel model);
    }
}
