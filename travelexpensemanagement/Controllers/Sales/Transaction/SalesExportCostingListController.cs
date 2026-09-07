using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesExportCostingListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesExportCostingList/Index.cshtml");
        }
    }
}
