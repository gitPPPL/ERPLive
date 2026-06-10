using travelexpensemanagement.Models.QualityMaster;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface ITempratureMasterRepository
    {
        string SaveTempMaster(TempratureMasterModel data);
    }
}
