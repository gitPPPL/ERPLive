using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISalesExportCostingListRepository
    {
        RepositoryResponseList<SalesExportCostingListModel> GetAllSalesExportCostingList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        RepositoryResponse Delete(int vNo);
    }
}
