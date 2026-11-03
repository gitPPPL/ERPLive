using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IDeliveryOrderRepository
    {
        Task<object> SaveAndUpdateDataAsync(DeliveryOrderModel model);
        Task<object> LoadEditDataAsync(int vNo, string vType);
        Task<object> SaveProformaDataAsync(DeliveryOrderModel model, string vNo);
        Task<object> SaveSaleInvoiceDataAsync(DeliveryOrderModel model, string vNo);
        Task<object> UpdateDOAsync(DeliveryOrderModel model);
    }
}
