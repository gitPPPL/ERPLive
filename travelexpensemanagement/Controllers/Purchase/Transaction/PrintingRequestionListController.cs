using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PrintingRequestionListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PrintingRequestionList/Index.cshtml");
        }
    }
}
