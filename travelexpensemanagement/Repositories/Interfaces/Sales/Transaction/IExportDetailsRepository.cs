using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IExportDetailsRepository
    {
        Task<object> SaveAndUpdateDataAsync(ExportDetailsModel model);

        Task<object> LoadEditDataAsync(int REF_NO, string REF_TYPE);

        Task<object> DeleteDataAsync(int refNo, string refType, int vNo);
    }
}
