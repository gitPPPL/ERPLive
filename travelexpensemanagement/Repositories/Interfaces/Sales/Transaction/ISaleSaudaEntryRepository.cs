
using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISaleSaudaEntryRepository
    {
        Task<object> SaveAndUpdateDataAsync(SaleSaudaEntryModel model);

    }
}
