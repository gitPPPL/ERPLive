using travelexpensemanagement.Models.GateEntry;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IOutwardEntryListRepository
    {
        Task<object> GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
    }
}
