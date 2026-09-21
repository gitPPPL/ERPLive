using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class ITInventoryListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly IITInventroyEntryListRepository _itInventoryEntryListRepository;

        public ITInventoryListController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService, IITInventroyEntryListRepository itInventoryEntryListRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
            _itInventoryEntryListRepository = itInventoryEntryListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/ITInventoryList/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> LoadListData(string searchTerm = "", int pageNo = 1,int pageSize = 20)
        {
            try
            {
                var result = await _itInventoryEntryListRepository.LoadListDataAsync(searchTerm, pageNo, pageSize);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = ex.Message});
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteData(string docId)
        {
            try
            {
                var result = await _itInventoryEntryListRepository.DeleteDataAsync(docId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}
