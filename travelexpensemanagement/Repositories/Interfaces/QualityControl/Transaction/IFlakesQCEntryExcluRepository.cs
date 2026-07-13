using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl
{
    public interface IFlakesQCEntryExcluRepository
    {
        string SubmitRequest(FlexQCEntryExcru_Header header, List<FlexQCEntryExcru_Details> details, string action);
    }
}