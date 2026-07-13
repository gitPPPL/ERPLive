using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl
{
    public interface IFlakesQCEntryListRepository
    {

        Task<object> DocDetailsCode(string docCode);

        Task<byte[]> ExportToExcel(string searchTerm = null);

        Task<byte[]> ExportToPdf(string searchTerm = null);

        Task<bool> Delete(int code);
    }
}
