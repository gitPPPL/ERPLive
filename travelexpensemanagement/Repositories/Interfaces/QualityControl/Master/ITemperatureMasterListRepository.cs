using travelexpensemanagement.Models.QualityMaster;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface ITemperatureMasterListRepository
    {
        (List<TempratureMasterModel> Data, int TotalCount) GetTemperatureList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);

        TempratureMasterModel GetCategoryCode(int code);

        bool Delete(int code);

        byte[] ExportAllDocs();
    }
}
