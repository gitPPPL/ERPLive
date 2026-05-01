using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using travelexpensemanagement.Models.GateEntry;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IVehicleInwardRepository
    {
        Task<RepositoryResponseData<DocInfo>> MaxVNo(string V_type);
        Task<RepositoryResponseList<ExpandoObject>> DocType();
        Task<RepositoryResponseList<ExpandoObject>> PartyList();
        Task<RepositoryResponseList<ExpandoObject>> TransportationList();
        Task<RepositoryResponseList<ExpandoObject>> DONo();
        Task<RepositoryResponse> SaveOrUpdate(TransportInwardModel POmodel);
        Task<RepositoryResponseData<DriverDetail>> DriverDetails(string mobileNo);
        Task<RepositoryResponseData<RcRequest>> VehicleInfoApi(string rc_number);
        Task<RepositoryResponseData<vehicleInfoDb>> VehicleInfoFromDB(string vehicleNo);
        Task<RepositoryResponseData<List<TransportInwardModel>>> TransportInwardRecordsById(string id);
    }
}
