using travelexpensemanagement.Models.Sales.OtherUnused;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.OtherUnused
{
    public interface IPackingSlipEntryRepository
    {
        Task<RepositoryResponseData<object>> GetDOData(string docId);
        RepositoryResponseData<object> GetItemRowData(string vType, int itemCode, int rowIndex, string prevBaleNo, int placeCode);
        Task<PackingValidationResponse> ValidateData(PackingSlipValidationRequest data);
        RepositoryResponse SaveData(PackingSlipEntryModel model);
        RepositoryResponseData<PackingSlipEntryModel> GetById(int id, string vtype);
        Task<RepositoryResponseData<List<CopyFromItemModel>>> GetCopyFromList(int? itemCode, int? macCode, DateTime vDate);
    }
}