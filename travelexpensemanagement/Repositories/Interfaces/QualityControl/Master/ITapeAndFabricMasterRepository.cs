using travelexpensemanagement.Controllers.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface ITapeAndFabricMasterRepository
    {
        Task<RepositoryResponseData<bool>> GetExistOrNotAsync(string inputData);
        Task<RepositoryResponse> SaveTapeAndFabricAsync(TapeAndFabricMasterController.TapeNFabricModel model);
        Task<RepositoryResponseData<TapeAndFabricMasterController.TapeNFabricModel>> GetTapeAndFabricDetailsByIdAsync(string id);
        Task<RepositoryResponse> UpdateTapeAndFabricAsync(TapeAndFabricMasterController.TapeNFabricModel model);
    }
}
