using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class DeliveryOrderListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/DeliveryOrderList/Index.cshtml");
        }
    }
}
