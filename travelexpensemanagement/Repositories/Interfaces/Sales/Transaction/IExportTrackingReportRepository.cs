using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IExportTrackingReportRepository
    {
        Task<RepositoryResponseList<ExportTrackingReportRow>> GetReportData(DateTime fromDate, DateTime toDate, string status, int withContainerDetails);
    }
}
