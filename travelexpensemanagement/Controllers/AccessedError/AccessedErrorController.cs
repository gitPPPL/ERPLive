//using Microsoft.AspNetCore.Mvc;

//namespace travelexpensemanagement.Controllers.AccessedError
//{
//    public class AccessedErrorController : Controller
//    {
//        public IActionResult Index(string message, int? code)
//        {
//            ViewBag.ErrorMessage = message ?? "Something went wrong.";
//            ViewBag.StatusCode = code ?? 500;
//            return View();
//        }


//    }
//}

using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Spire.Doc.AI.Model;

namespace travelexpensemanagement.Controllers.AccessedError
{
    public class AccessedErrorController : Controller
    {
        public IActionResult Index(string? message, int? code, string? errorReference , string? errorType, string? errorCategory, string? requestPath)
        {
            ViewBag.ErrorMessage = message ?? "Something went wrong.";
            ViewBag.StatusCode = code ?? 500;
            ViewBag.ErrorReference = errorReference ?? "N/A";
            ViewBag.ErrorType = errorType ?? "Unknown";
            ViewBag.ErrorCategory = errorCategory ?? "Unknown";
            ViewBag.RequestPath = requestPath ?? "N/A";
            return View();
        }
    }
}