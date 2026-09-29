using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IDeliveryOrderRepository
    {
        Task<object> SaveAndUpdateDataAsync(DeliveryOrderModel model);

        Task<object> LoadEditDataAsync(int vNo, string vType);
    }
}
