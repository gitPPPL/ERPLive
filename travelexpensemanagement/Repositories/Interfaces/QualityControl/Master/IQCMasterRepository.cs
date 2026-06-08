using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IQCMasterRepository
    {
        Task<RepositoryResponseData<bool>> GetExistOrNotAsync(string inputData);
        Task<RepositoryResponse> InsertDataQcMasterAsync(QCMaster model);
        Task<RepositoryResponse> UpdateDataQcMasterAsync(QCMaster model);
        Task<RepositoryResponse> SaveDeductRatesAsync(List<DeductRateModel> rates);
        Task<RepositoryResponseData<List<DeductRateModelList>>> CheckDeductRatesAsync(CheckDeductRateRequest request);
        Task<RepositoryResponseData<QCMaster>> GetQCMasterListByCodeAsync(int code);
        Task<RepositoryResponseData<bool>> CheckDeductRateExistAsync(int code, int qcpCode);
    }
}
