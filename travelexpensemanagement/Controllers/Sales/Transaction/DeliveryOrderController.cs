using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class DeliveryOrderController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/DeliveryOrder/Index.cshtml");
        }
    }
}
