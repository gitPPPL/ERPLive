using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISalesExportCostingRepository
    {
        RepositoryResponse SaveSalesExportCosting(SalesExportCostingModel model);
        RepositoryResponseData<SalesExportCostingModel> GetDataById(int docId);
    }
}
