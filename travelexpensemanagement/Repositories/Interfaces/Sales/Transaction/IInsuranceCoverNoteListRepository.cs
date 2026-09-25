namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IInsuranceCoverNoteListRepository
    {
        Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20);

        Task<object> DeleteDataAsync(int vNo, string vType);
    }
}
