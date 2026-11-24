using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IDispatchPlanningRepository
    {
        Task<object> SaveAndUpdateDataAsync(DispatchPlanningModel model);

        Task<object> LoadEditDataAsync(int vNo, string vType);
    }
}
