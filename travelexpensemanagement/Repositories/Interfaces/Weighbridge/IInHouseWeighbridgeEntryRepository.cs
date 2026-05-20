using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Models.Weighbridge.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Weighbridge
{
    public interface IInHouseWeighbridgeEntryRepository
    {
        Task<IActionResult> SaveOrUpdateInHouseWeighBridgeEntryasync(WBEntryModel model);
        Task<byte[]> ExportToExcel(string searchTerm = null);
        Task<byte[]> ExportToPdf(string searchTerm = null);
    }
}