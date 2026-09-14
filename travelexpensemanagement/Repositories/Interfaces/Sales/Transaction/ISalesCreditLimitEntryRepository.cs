using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISalesCreditLimitEntryRepository
    {
        Task<RepositoryResponseData<object>> GetDrCrAmtByPartyCodeAsync(int code);
        Task<RepositoryResponse> SaveSalesCreditLimitAsync(SalesCreditLimitModel model, string doctype);
        Task<RepositoryResponseData<SalesCreditLimitModel>> GetDataById(int vNo, string doctype);
    }
}
