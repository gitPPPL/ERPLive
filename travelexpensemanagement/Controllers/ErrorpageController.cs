using Microsoft.AspNetCore.Mvc;

namespace travelexpensemanagement.Controllers
{
    public class ErrorpageController : Controller
    {
        // Testing: 10 seconds
        // Change this to your required timeout later
        private readonly TimeSpan _timeout =
            TimeSpan.FromMinutes(30);


        // ==========================================
        // ERROR PAGE
        // ==========================================

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Errorpage/Index.cshtml");
        }


        // ==========================================
        // UPDATE USER ACTIVITY
        // ==========================================

        [HttpPost]
        public IActionResult UpdateActivity()
        {
            if (!HttpContext.Session.IsAvailable)
            {
                return Unauthorized();
            }

            var lastActivityStr = HttpContext.Session.GetString("LastActivity");

            // Check existing activity
            if (!string.IsNullOrEmpty(lastActivityStr) &&
                DateTimeOffset.TryParse(
                    lastActivityStr,
                    out var lastActivity))
            {
                var inactiveTime =
                    DateTimeOffset.UtcNow - lastActivity;


                // Session already expired
                if (inactiveTime > _timeout)
                {
                    HttpContext.Session.Clear();

                    return Unauthorized();
                }
            }


            // User is active
            HttpContext.Session.SetString(
                "LastActivity",
                DateTimeOffset.UtcNow.ToString("O"));


            return Ok();
        }


        // ==========================================
        // EXPIRE SESSION
        // ==========================================

        [HttpPost]
        public IActionResult ExpireSession()
        {
            HttpContext.Session.Clear();

            return Ok();
        }
    }
}