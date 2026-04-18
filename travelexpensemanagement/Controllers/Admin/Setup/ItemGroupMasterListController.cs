using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class ItemGroupMasterListController : Controller
    {
        public IActionResult Index()    
        {
            //return View();
            return View("~/Views/Admin/Setup/ItemGroupMasterList/Index.cshtml");
        }

        public IActionResult ItemGroupMasterList()
        {
            //return View();
            return View("~/Views/Admin/Setup/ItemGroupMasterList/Index.cshtml");
        }
    }
}
