using travelexpensemanagement.Models.Purchase.Transiction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseQuotationListRepository
    {
        Task<(List<QUOTATION1> Quotations, int TotalCount)> GetAllQuotationsAsync(string searchTerm, int pageNumber, int pageSize);

        Task<QUOTATION1?> GetQuotationByCodeAsync(int vNo, string vType);
    }
}
