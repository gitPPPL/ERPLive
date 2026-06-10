namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Master
{
    public interface IQCDiscMasterListRepository
    {
        (List<object> Data, int TotalCount) GetAllListData(string searchTerm = "", int pageNumber = 1, int pageSize = 10);

        byte[] ExportAllDocs();
    }
}
