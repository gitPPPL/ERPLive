using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class BigWeighbridgeListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly IBigWeighbridgeListRepository _bigWeighbridgeListRepository;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public BigWeighbridgeListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, IBigWeighbridgeListRepository bigWeighbridgeListRepository)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _bigWeighbridgeListRepository = bigWeighbridgeListRepository;
        }
        
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Big Weighbridge Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Weighbridge/Transaction/BigWeighbridgeList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetBigWBridgeList(string searchTerm = "",  int pageNumber = 1,int pageSize = 10)
        {
            try
            {
                var result = await _bigWeighbridgeListRepository.GetBigWBridgeListAsync(searchTerm, pageNumber, pageSize);

                return Json(new
                {
                    status = true,
                    data = result.Data,
                    totalCount = result.TotalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckDeleteBigWBridgeEntry(string docId)
        {
            if (string.IsNullOrWhiteSpace(docId))
                return Json(new { success = false, message = "Invalid ID" });

            var result = await _bigWeighbridgeListRepository.CheckDeleteBigWBridgeEntryAsync(docId);

            return Json(new
            {
                success = result.Status,
                message = result.Message,
                data = result.Data
            });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBigWBridgeEntry(string docid)
        {
            if (string.IsNullOrWhiteSpace(docid))
                return Json(new { success = false, message = "Invalid ID" });

            var result = await _bigWeighbridgeListRepository.DeleteBigWBridgeEntryAsync(docid);

            return Json(new
            {
                success = result.Status,
                message = result.Message
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetBigWBridgeEntryDetails(string docid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docid))
                {
                    return Json(new
                    {
                        status = false,
                        message = "Invalid ID"
                    });
                }

                var result = await _bigWeighbridgeListRepository
                    .GetBigWBridgeEntryDetailsAsync(docid);

                return Json(new
                {
                    status = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            try
            {
                var result = await _bigWeighbridgeListRepository.ExportAllDocsAsync();

                return Json(new
                {
                    status = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

    }
}
