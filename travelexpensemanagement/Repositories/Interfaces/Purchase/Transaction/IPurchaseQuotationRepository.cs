using travelexpensemanagement.Models.Purchase.Transiction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseQuotationRepository
    {
        string GenerateVNo(string vType);
        Task<object> GetFullQuotationByVno(int vNo, string vType);

        Task<(bool Success, string Message)> SaveQuotation(QuotationWrapper data);

        //Task<(bool IsValid, string Message)> ValidateQuotationAsync(QUOTATION1 model, List<QUOTATION2> lineRows,List<QUOTATION3> attachments);

        //Task<bool> DeletePurchaseQuotationByCode(int code, string vType, int compCode, int branchCode,int yearCode);

        Task<object> CopyData(string actionType, DateTime? vDate);

        Task<object> GetPurchaseHistory(int itemcode);

        Task<object> GetPurchaseQuotation(int itemcode);

        Task<object> OrderHistory(int itemcode, DateTime? vDate);

        Task<byte[]> ExportToExcel(int vNo, string vType);

        decimal GetLastOrderRate(int itemCode, DateTime vDate);


    }
}
