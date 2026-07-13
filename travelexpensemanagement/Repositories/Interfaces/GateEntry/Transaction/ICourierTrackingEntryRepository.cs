using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.GateEntry.Transaction;
using static travelexpensemanagement.Controllers.GateEntry.Transaction.CourierTrackingEntryController;


namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{

    public interface ICourierTrackingEntryRepository
    {
        int GetNextDocNo(string docType);
        string SaveCourierData(CourierTrackingModel model);
        GetCourierTrackingModel GetCourierData(string docType, string docNo);
        CourierTrackingReportModel PrintCourierReport(PrintCourierReportModel model);
    }


}