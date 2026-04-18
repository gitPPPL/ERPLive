using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesInvoiceController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesInvoice/Index.cshtml");
        }
    }
}
