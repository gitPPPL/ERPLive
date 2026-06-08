using travelexpensemanagement.Controllers.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IParameterMasterListRepository
    {
        Task<RepositoryResponseList<ParameterMasterListController.QCprameterDto>> GetQualityParamListAsync(string searchTerm, int pageNumber, int pageSize);
        RepositoryResponseData<bool> IsQcParamDeletableAsync(int docId);
        Task<RepositoryResponse> DelQParamMastAsync(int docId);
    }
}
