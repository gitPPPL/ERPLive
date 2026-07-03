using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces
{
    public interface IApprovalService
    {
        Task<string> GetApprovalStatus(string vType, int vNo, string tableName);
    }
}
