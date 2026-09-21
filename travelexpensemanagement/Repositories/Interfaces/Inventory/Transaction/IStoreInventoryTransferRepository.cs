using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IStoreInventoryTransferRepository
    {
        Task<object> SaveAndUpdateDataAsync(StoreInventoryTransferModel model);

        Task<object> LoadEditDataAsync(string docId);

        Task<object> GetLinkDataAsync(DateTime vDate);
    }
}
