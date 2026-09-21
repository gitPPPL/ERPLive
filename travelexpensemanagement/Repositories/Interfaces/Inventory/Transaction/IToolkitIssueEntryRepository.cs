using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IToolkitIssueEntryRepository
    {
        RepositoryResponse SaveOrUpdate(ToolKitIssueModel model);
        Task<RepositoryResponseData<ToolKitIssueModel>> GetDataByIdAsync(string vType, string vNo);
        Task<RepositoryResponse> PrepareToolKitBalReportAsync(DateTime fromDate, DateTime toDate);
    }
}
