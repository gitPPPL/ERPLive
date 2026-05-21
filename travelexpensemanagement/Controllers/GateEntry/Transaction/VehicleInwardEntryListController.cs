using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VehicleInwardEntryListController : Controller
    {
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IVehicleInwardListRepository _VehicleInwardListRepository;
        public VehicleInwardEntryListController(ModuleService.ModuleService moduleService, IVehicleInwardListRepository VehicleInwardListRepository)
        {
            _moduleService = moduleService;
            _VehicleInwardListRepository = VehicleInwardListRepository;
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
            return View("~/Views/GateEntry/Transaction/VehicleInwardEntryList/Index.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> GetTransportInwardList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _VehicleInwardListRepository.GetTransportInwardList(searchTerm, pageNumber, pageSize);
            return Json(new { success = result.status, data = result.data, totalCount = result.totalCount });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteVehicleInwardEntry(string docId)
        {
            var result = await _VehicleInwardListRepository.DeleteTransportInward(docId);
            return Json(new { success = result.status, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GetVehicleInwardEntryDetails(string docid)
        {
            var result = await _VehicleInwardListRepository.VehicleInwardEntryDetails(docid);
            return Json(new { status = result.status, data = result.data, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            var result = await _VehicleInwardListRepository.ExportVehicleInwardAsExcel();
            return Json(new { status = result.status, data = result.data, message = result.message });
        }
    }
}
