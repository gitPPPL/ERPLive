using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportExportDocAttachmentListController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportExportDocAttachmentList/Index.cshtml");
        }
    }
}
