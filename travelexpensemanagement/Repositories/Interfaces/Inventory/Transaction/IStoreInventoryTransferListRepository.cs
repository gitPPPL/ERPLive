namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IStoreInventoryTransferListRepository
    {
        Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20);

        Task<object> DeleteDataAsync(string docId);

    }
}
