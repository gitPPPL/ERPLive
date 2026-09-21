using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IInventoryDeliveryChallanMemoListRepository
    {
        Task<RepositoryResponseList<InventoryDeliveryChallanMemoModel>> GetAllDeliveryMemoList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        RepositoryResponse Delete(int vNo);
    }
}
