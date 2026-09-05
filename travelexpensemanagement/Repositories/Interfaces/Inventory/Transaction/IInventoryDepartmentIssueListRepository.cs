using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IInventoryDepartmentIssueListRepository
    {
        Task<(List<InventryDepartmentIssue_Header> Lists, int TotalCount)> GetListAsync( string searchTerm = "", int pageNumber = 1,  int pageSize = 10, string FormName = "");
    }
}