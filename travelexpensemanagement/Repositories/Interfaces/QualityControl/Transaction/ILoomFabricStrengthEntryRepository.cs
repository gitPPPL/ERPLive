using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction
{
    public interface ILoomFabricStrengthEntryRepository
    {
        Task<object> GetMaxVNoAsync();

        Task<object> GetPlaceMastAsync();

        Task<object> GetShiftListAsync();

        Task<object> GetUserMastAsync();

        Task<object> GetLoomListAsync(int placeCode);

        Task<object> GetProd2ListAsync(int loomCode, int placeCode, DateTime? vDate);

        Task<object> GetItemListAsync(int itemCode);

        Task<object> GetColorAsync();

        Task<object> GetItemTypeAsync();

        Task<(object Data, bool IsExist, string MatchingCode)> GetStrengthListAsync(decimal minStd, decimal maxStd);

        Task<(object Header, object Detail)> GetLoomFabricSByIdAsync(string docId);

        Task<(bool Status, string Message)> SaveOrUpdateLoomFabricEntryAsync(LoomFabricEntryModel model);

        Task<object> GetLastQCEntryAsync();

    }
}
