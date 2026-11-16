using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IDocumentEmailingRepository
    {
        Task<RepositoryResponseList<Dictionary<string, object>>> FillData(string mailType, string docType, DateTime fromDate, DateTime toDate,
            int[] partyCodes, int agentCode = 0, string itemType = "", int acGroup = 0);
        Task<RepositoryResponseData<object>> PrepareInvoiceExport(string vType, long vNo);
        Task<RepositoryResponseList<object>> SendMail(SendMailRequest req);
    }
}