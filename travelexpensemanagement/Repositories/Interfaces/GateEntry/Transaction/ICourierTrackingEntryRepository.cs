using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{

    public interface ICourierTrackingEntryRepository
    {
        int GetNextDocNo(string docType);
        string SaveCourierData(CourierTrackingModel model);
        GetCourierTrackingModel GetCourierData(string docType, string docNo);
    }


}