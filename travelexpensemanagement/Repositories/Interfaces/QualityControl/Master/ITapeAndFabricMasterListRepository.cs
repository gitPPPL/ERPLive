using travelexpensemanagement.Controllers.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface ITapeAndFabricMasterListRepository
    {
        Task<RepositoryResponseList<TapeAndFabricMasterListController.QCStandardMasterDto>> GetTape_FabricListAsync(string searchTerm, int pageNumber, int pageSize);
        RepositoryResponseData<bool> IsTapeFabricDeletableAsync(int docId);
        Task<RepositoryResponse> DelTape_FabricMastAsync(int docId);
    }
}
