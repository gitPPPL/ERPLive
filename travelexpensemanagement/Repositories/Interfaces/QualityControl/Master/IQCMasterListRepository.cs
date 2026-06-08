using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IQCMasterListRepository
    {
        Task<RepositoryResponseList<QCMasterList>> GetQCMasterListAsync(string searchTerm, int pageNumber, int pageSize);
        Task<RepositoryResponse> DeleteQcMasterAsync(int docId);
        Task<RepositoryResponseData<bool>> IsQcDeletableAsync(int docId);
    }
}
