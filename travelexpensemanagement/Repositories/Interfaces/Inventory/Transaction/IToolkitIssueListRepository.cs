using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IToolkitIssueListRepository
    {
        RepositoryResponseList<ToolKitIssueModel> GetAllToolkitIssue(string docType, string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        RepositoryResponse Delete(int vNo, string docType);
    }
}
