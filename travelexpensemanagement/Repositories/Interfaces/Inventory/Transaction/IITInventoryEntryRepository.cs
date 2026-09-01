using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IITInventoryEntryRepository
    {
        Task<object> SaveAndUpdateDataAsync(ITInventoryEntryModel model);

        Task<object> LoadEditDataAsync(string docId);
    }
}
