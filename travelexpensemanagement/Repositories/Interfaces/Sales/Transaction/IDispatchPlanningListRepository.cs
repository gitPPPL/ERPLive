namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IDispatchPlanningListRepository
    {

        Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20);

        Task<object> DeleteDataAsync(int vNo, string vType);
    }
}
