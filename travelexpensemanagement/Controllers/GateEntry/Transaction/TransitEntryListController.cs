using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TransitEntryListController : Controller
    {
        private readonly ITransitEntryListRepository _iTransitEntryListRepository;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TransitEntryListController(ITransitEntryListRepository iTransitEntryListRepository, ModuleService.ModuleService moduleService)
        {
            _moduleService = moduleService;
            _iTransitEntryListRepository = iTransitEntryListRepository;
        } 
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Vehicle Inward";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/GateEntry/Transaction/TransitEntryList/Index.cshtml", model);
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
        public async Task<JsonResult> Delete(int vNo, string docType)
        {
            var result = await _iTransitEntryListRepository.DeleteById(vNo, docType);
            return Json(new { status = result.status, message = result.message });
        }
    }
}
