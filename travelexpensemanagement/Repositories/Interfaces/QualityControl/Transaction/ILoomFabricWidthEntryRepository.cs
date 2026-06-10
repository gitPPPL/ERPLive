using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction
{
    public interface ILoomFabricWidthEntryRepository
    {
        Task<object> GetMaxVNoAsync();
        Task<(bool Status, string Message)> SaveOrUpdateLoomFabricEntryAsync(LoomFabricEntryModel model);
        Task<object> GetLastQCEntryAsync();

    }
}
