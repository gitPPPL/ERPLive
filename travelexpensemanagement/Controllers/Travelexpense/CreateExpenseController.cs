using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers.Travelexpense
{
    public class CreateExpenseController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }
    }
}
