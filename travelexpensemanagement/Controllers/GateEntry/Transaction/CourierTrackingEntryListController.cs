using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class CourierTrackingEntryListController : Controller
    {
        private readonly IApprovalService _repository;

        public CourierTrackingEntryListController(IApprovalService repository)
        {
            _repository = repository;
        }
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/CourierTrackingEntryList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetCourierTrackingEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var result = _repository.GetCourierTrackingEntryList(searchTerm, pageNumber, pageSize);
            return Json(new
            {
                status = result.status,
                items = result.data,      
                totalCount = result.totalCount,
                message = result.message
            });
        }
        [HttpPost]
        public async Task<IActionResult> Delete(string vNo, string docType)
        {
            var result = await _repository.DeleteCourierTrackingEntry(vNo, docType);
            return Json(new
            {
                status = result.status,
                message = result.message
            });
        }
    }
}