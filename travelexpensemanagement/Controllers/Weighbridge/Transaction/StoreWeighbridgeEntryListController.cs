using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class StoreWeighbridgeEntryListController : Controller
    {

        private readonly ModuleService.ModuleService _moduleService;

        private readonly IStoreWeighbridgeEntryListRepository _storeWbListRepository;

        public StoreWeighbridgeEntryListController(ModuleService.ModuleService moduleService, IStoreWeighbridgeEntryListRepository storeWbListRepository)
        {
            _moduleService = moduleService;
            _storeWbListRepository = storeWbListRepository;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Store Weighbridge Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Weighbridge/Transaction/StoreWeighbridgeEntryList/Index.cshtml", model);

        }

        [HttpGet]
        public async Task<IActionResult> GetStoreWBridgeList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _storeWbListRepository.GetList(searchTerm, pageNumber, pageSize);
            if (!result.status)
            {
                return Json(new { status = result.status, message = result.message });
            }
            return Json(new { status = result.status, data = result.data, totalCount = result.totalCount});
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStoreWBridgeEntry(string docId)
        {
            if (string.IsNullOrEmpty(docId))
            {
                return Json(new { status = false, message = "Invalid ID" });
            }
            var result = await _storeWbListRepository.DeleteStoreWb(docId);
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public async Task<IActionResult> GetStoreWBridgeEntryDetails(string docid)
        {
            if (string.IsNullOrEmpty(docid))
            {
                return Json(new { status = false, message = "Invalid ID" });
            }
            var result = await _storeWbListRepository.StoreWBDetails(docid);
            if (!result.status)
            {
                return Json(new { status = result.status, message = result.message });
            }
            return Json(new { status = result.status, data = result.data});
        }

        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            var result = await _storeWbListRepository.ExportAllDocs();
            if (!result.status)
            {
                return Json(new { status = result.status, message = result.message });
            }
            return Json(new { status = result.status, data = result.data });
        }

    }
}
