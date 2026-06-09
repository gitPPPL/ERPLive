using travelexpensemanagement.Models.GateEntry;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IOutwardEntryListRepository
    {
        Task<object> GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);

        Task<object> GetDataByCode(int rowId, string vtype);

        Task<object> Delete(string docId);


        Task<object> DocDetailsCode(string docCode);

        Task<byte[]> ExportToExcel(string searchTerm = null);

        Task<byte[]> ExportToPdf(string searchTerm = null);

        Task<object> GetDataByPendingorder(int PartyCode, string Type,  DateTime v_date);
    }
}
