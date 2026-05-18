using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Models.Weighbridge.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Weighbridge
{
    public interface IInHouseWeighbridgeEntryRepository
    {
        Task<IActionResult> SaveOrUpdateInHouseWeighBridgeEntryasync(WBEntryModel model);

    }
}