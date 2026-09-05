using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IInventoryDeliveryChallanMemoRepository
    {
        RepositoryResponse SaveDeliveryChallanMemo(InventoryDeliveryChallanMemoModel model);
        RepositoryResponseData<InventoryDeliveryChallanMemoModel> GetDataById(string docId);
    }
}
