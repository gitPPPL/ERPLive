using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IQCDiscMasterRepository
    {
        bool SaveAndUpdateData(QCDISC_MAST model);

        (List<QCDISC_MAST> Data, int TotalCount) GetQcDiscOnChange(int itemCode);

        bool DeleteQcDiscByCode(int itemCode);
    }
}
