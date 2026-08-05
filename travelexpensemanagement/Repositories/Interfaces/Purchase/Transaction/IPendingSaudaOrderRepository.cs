using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Repositories.Implementations.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPendingSaudaOrderRepository
    {
        JsonResult GetddlDocType();
        JsonResult GetdocNumber(string vType);
        JsonResult GetfilterType(string vType);
        JsonResult GetStatus();
        JsonResult GetPendingData(string vType,string refType,string status,string source, DateTime fromDate, DateTime toDate, string itemSearch);
        IActionResult SaveData(PendingSaudaOrderRepository.PendingSaudaOrderSaveModel request);
    }
}
