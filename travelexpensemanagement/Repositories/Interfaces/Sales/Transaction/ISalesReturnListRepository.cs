namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISalesReturnListRepository
    {
        RepositoryResponseList<SalesReturnListModel> GetSalesReturnList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        RepositoryResponseData<SalesEditStatus> GetSalesEditStatus(string vType, int vNo);
        RepositoryResponseData<SalesDeleteStatus> GetSalesDeleteStatus(string vType, int vNo);
        RepositoryResponse DeleteSales(string vType, int vNo);

    }
}
