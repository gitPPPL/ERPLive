using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TransitEntryListController : Controller
    {
        private readonly ITransitEntryListRepository _iTransitEntryListRepository;
        public TransitEntryListController(ITransitEntryListRepository iTransitEntryListRepository)
        {
            _iTransitEntryListRepository = iTransitEntryListRepository;
        } 
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/TransitEntryList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _iTransitEntryListRepository.GetList(searchTerm, pageNumber, pageSize);
            return Json(new { success = result.status, message = result.message, lists = result.data, result.totalCount });
        }
        public async Task<IActionResult> GetDataByID(int code , string vtype)
        {
            var result = await _iTransitEntryListRepository.GetById(code, vtype);
            return Json(new { success = result.status, data = result.data, message = result.message });
        }
        [HttpPost]
        public async Task<JsonResult> Delete(int code, string VType)
        {
            var result = await _iTransitEntryListRepository.DeleteById(code, VType);
            return Json(new { success = result.status, message = result.message });
        }
    }
}
