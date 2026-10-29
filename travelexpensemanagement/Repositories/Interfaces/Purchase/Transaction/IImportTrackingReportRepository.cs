using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IImportTrackingReportRepository
    {
        Task<RepositoryResponseData<ImportTrackingReportResult>> GetReportData(DateTime fromDate, DateTime toDate);
        Task<RepositoryResponseList<DocumentSendListItem>> GetDocumentListData(DateTime fromDate, DateTime toDate, string? vType, string? docStatus, string? searchBy, 
            string? searchText);
        RepositoryResponseData<SendDocumentResult> SendDocument(List<ImportTrackingReport> model);
    }
}
