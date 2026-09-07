using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesExportCostingController : Controller
    {

        public SalesExportCostingController()
        {
            
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesExportCosting/Index.cshtml");
        }
    }
}
