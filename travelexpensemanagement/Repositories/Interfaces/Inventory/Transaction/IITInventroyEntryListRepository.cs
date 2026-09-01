namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IITInventroyEntryListRepository
    {
        Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20);
    }
}
