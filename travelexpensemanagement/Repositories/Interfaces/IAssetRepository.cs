using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Repositories.Interfaces
{
    public interface IAssetRepository
    {
        bool InsertAsset(AssetModel model);
        AssetModel GetAssetBySrno(int srno);
        bool UpdateAsset(AssetModel model);
        bool IsDuplicate(int yearCode, int compCode, int acCode);
    }
}