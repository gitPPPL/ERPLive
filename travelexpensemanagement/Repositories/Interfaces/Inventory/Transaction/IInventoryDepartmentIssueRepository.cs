using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IInventoryDepartmentIssueRepository
    {
        object DDlVType(string formName);
        object DDlItemName(string formName,string V_TYPE);
        object CopyData(string V_TYPE);

        object DDlPlaceFrom(string formName);
        Task<(string Status, string Message)> SubmitRequest( Models.Inventory.Transaction.InventryDepartmentIssue_Header header, List<InventryDepartmentIssue_Details> details, string action);

    }
}