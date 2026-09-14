using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISalesCreditLimitListRepository
    {
        RepositoryResponseList<SalesCreditLimitListModel> GetAllSalesCreditLimitList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        RepositoryResponse Delete(string doctype, int vNo);
    }
}
