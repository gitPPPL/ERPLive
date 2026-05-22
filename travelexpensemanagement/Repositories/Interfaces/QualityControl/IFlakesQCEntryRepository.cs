using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl
{
    public interface IFlakesQCEntryRepository
    {
        string SubmitRequest( FlakesQCEntryLIst_Header header, List<FlakesQCEntryList_Details> details, string action);
    }
}