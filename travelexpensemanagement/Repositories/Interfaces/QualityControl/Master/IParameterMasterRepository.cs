using travelexpensemanagement.Controllers.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IParameterMasterRepository
    {
        RepositoryResponseData<bool> GetExistOrNotAsync(string inputData);
        Task<RepositoryResponse> SaveQParamMastAsync(ParameterMasterController.ParameterModel model);
        Task<RepositoryResponseData<ParameterMasterController.ParameterModel>> GetQParameterDetailsByIdAsync(string id);
        Task<RepositoryResponse> UpdateQParameterMastAsync(ParameterMasterController.ParameterModel model);
    }
}
