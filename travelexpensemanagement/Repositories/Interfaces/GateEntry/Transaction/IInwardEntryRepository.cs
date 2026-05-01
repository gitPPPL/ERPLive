

using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IInwardEntryRepository
    {
        Task<string> GetVNoAsync(string vType, string tableName = "GATE1");

    }
}
