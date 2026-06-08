using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IUOMMasterRepository
    {
        RepositoryResponse SaveUOM(QCPUNIT_MAST model);
        RepositoryResponseData<bool> IsQcUOMDeletable(int docId);
        RepositoryResponse DeleteUOMByCode(int docId);
    }
}
