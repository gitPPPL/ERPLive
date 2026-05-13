using System.Dynamic;
using travelexpensemanagement.Models.Weighbridge.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction
{
    public interface IStoreWeighbridgeEntryRepository
    {
        Task<RepositoryResponse> saveOrUpdate(WBEntryModel model);
        Task<RepositoryResponseData<WeighBridgeEntryDto>> getStoreWbById(string id);
    }
    public class WeighBridgeEntryDto
    {
        public List<ExpandoObject> Header { get; set; }
        public List<ExpandoObject> Detail { get; set; }  
    }
}
