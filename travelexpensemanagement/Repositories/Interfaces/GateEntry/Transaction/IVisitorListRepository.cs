using travelexpensemanagement.Models.Gate_Entry.Transaction;
using System.Data;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IVisitorListRepository
    {
        (List<VISITOR> visitors, int totalCount) GetAllVisitors(string searchTerm, int pageNumber, int pageSize);

        VISITOR GetVisitorByVno(string docId, out string base64Image);
        Task<DataTable> ExportVisitorToExcel(string searchTerm);

        Task<DataTable> ExportVisitorToPdf(string searchTerm);

    }
}
