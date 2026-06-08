using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IUOMMasterListRepository
    {
        RepositoryResponseList<QCPUNIT_MAST> GetAllUOMsAsync(string searchTerm, int pageNumber, int pageSize);
        RepositoryResponseData<QCPUNIT_MAST> GetUOMByCodeAsync(int code);
    }
}
