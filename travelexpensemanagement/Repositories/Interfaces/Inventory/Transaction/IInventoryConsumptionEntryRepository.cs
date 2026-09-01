using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventroy.Transaction
{
    public interface IInventoryConsumptionEntryRepository
    {
        Task<object> SaveAndUpdateDataAsync(InventoryConsumptionEntryModel model);

        Task<object> LoadEditDataAsync(string docId);

        Task<object> GetCapitalItemDataAsync(DateTime vDate);

        Task<object> GetCopyFromDataAsync(string vType, int placeCode,DateTime vDate);
    }
}
