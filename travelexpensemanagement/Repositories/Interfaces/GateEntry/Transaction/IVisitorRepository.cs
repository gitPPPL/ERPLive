using travelexpensemanagement.Models.Gate_Entry.Transaction;
namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IVisitorRepository
    {
        bool IsDuplicate(string docId);

        VISITOR GetVisitorImage(string docId);

        bool SaveUpdateVisitor(VISITOR model, string action);

        bool DeleteVisitor(string docId);

        string GenerateVNo();

        object GetVisitorByMobile(string mobileNo);
        
    }
}
