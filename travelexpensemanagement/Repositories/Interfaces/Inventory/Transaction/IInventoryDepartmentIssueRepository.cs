namespace travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction
{
    public interface IInventoryDepartmentIssueRepository
    {
        object DDlVType(string formName);
        object DDlItemName(string formName,string V_TYPE);
        object CopyData(string V_TYPE);
    }
}