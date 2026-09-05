using travelexpensemanagement.Models.Inventory.Transaction;
using static travelexpensemanagement.Controllers.GateEntry.Transaction.InwardEntryListController;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IInventoryDepartmentIssueListRepository
    {
        Task<(List<InventryDepartmentIssue_Header> Lists, int TotalCount)> GetListAsync( string searchTerm = "", int pageNumber = 1,  int pageSize = 10, string FormName = "");


        Task<bool> DeleteAsync(string docId, int V_NO, string V_TYPE);
        Task<List<InwardEntryDetailDto>> DocDetailsCodeAsync(string docCode);
    }
}