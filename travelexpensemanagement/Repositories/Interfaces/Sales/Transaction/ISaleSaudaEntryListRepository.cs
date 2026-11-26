namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISaleSaudaEntryListRepository
    {
        Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20);

        Task<object> ValidateEditAsync(int vNo, string vType);

        Task<object> ValidateDeleteAsync(int vNo, string vType);

        Task<object> DeleteDataAsync(int vNo, string vType);
    }
}
