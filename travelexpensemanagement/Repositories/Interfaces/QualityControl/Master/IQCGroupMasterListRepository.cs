using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IQCGroupMasterListRepository
    {
        RepositoryResponseList<QCG_MAST> GetAllQCGroupsAsync(string searchTerm, int pageNumber, int pageSize);
        RepositoryResponseData<QCG_MAST> GetQCGroupByCodeAsync(int code);
    }
}
