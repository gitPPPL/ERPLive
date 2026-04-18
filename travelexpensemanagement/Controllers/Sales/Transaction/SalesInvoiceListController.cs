using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesInvoiceListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesInvoiceList/Index.cshtml");
        }
    }
}
