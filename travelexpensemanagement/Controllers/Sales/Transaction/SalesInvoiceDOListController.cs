using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesInvoiceDOListController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "DeliveryOrderList",
                new { mode = "DInv" }
            );
        }
    }
}