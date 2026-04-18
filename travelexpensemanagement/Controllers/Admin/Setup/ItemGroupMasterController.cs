using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class ItemGroupMasterController : Controller
    {
        public IActionResult Index()
        {
            //return View();
            return View("~/Views/Admin/Setup/ItemGroupMaster/Index.cshtml");
        }
        public IActionResult ItemGroupList()
        {
            //return View();
            return View("~/Views/Admin/Setup/ItemGroupList/Index.cshtml");
        }
    }
}
