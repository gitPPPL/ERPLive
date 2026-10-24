using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sale.Transaction
{
    public interface IJobworkReceiveChallan
    {
        Task<(string Status, string Message)> SubmitRequest(JobworkReceiveChallanModel_Header header, List<JobworkReceiveChallanModel_Detail> details, string action);
    }
}