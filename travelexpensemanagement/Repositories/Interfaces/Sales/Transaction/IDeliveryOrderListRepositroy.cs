namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IDeliveryOrderListRepositroy
    {
        Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20);
    }
}
