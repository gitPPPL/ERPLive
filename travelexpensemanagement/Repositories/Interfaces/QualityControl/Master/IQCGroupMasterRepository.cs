using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IQCGroupMasterRepository
    {
        RepositoryResponse SaveQCGroup(QCG_MAST model);
        RepositoryResponseData<bool> IsQcGroupDeletable(int docId);
        RepositoryResponse DeleteQCGroupByCode(int docId);
    }
}
