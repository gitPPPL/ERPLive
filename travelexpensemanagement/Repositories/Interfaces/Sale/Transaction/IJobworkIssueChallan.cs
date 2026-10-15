using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sale.Transaction
{
    public interface IJobworkIssueChallan
    {
        Task<(string Status, string Message)> SubmitRequest(JobworkIssueChallanModel_Header header, List<JobworkIssueChallanModel_Detail> details, string action);
    }
}