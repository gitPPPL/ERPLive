using travelexpensemanagement.Models.Inventory.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IDeliveryChallanStoreRepository
    {
        RepositoryResponseData<AddressDetails> GetAddressByBillToParty(int code, int addressId);
        RepositoryResponseData<DeliveryChallanStoreOrderDetails> GetDetailsOnRefNoLoad(string vType, int vNo);
        RepositoryResponseList<DeliveryChallanStoreOrderItem> GetDetailsOnWBLoad(int vNo);
        Task<RepositoryResponse> SaveDeliveryChallanStore(DeliveryChallanStoreModel model);
        RepositoryResponseData<DeliveryChallanStoreModel> GetDataById(string docId, string docType);
        Task<RepositoryResponse> CheckWeight(WBCheckRequest request);
    }
}
