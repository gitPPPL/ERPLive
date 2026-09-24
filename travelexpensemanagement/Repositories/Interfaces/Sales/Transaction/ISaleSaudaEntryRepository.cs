
using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISaleSaudaEntryRepository
    {
        Task<object> SaveAndUpdateDataAsync(SaleSaudaEntryModel model);

        Task<object> LoadEditDataAsync(int vNo, string vType);

        Task<object> CreateSalesOrderAsync(int saudaVNo);
    }
}
