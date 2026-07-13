using System.Dynamic;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IVehicleInwardListRepository
    {
        Task<RepositoryResponseList<TransportInwardListModel>> GetTransportInwardList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
        Task<RepositoryResponse> DeleteTransportInward(string docid);
        Task<RepositoryResponseList<ExpandoObject>> VehicleInwardEntryDetails(string docid);
    }
}
