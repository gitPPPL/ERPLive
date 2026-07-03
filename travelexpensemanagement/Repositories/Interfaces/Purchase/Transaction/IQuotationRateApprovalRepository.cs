using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IQuotationRateApprovalRepository
    {
        Task<object> GetQuotRtApvrlDetailsById(string id);

        Task<object> GetFilterItemdetails(FilterItemload filtrItmModel);

        Task<object> SaveOrUpdateQuotRateApproval(QuotationRateApproval equotmodel);

    }
}
