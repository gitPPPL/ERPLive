using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class StoreInventoryTransferListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly IStoreInventoryTransferListRepository _storeInventoryTransferListRepository;

        public StoreInventoryTransferListController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService,  travelexpensemanagement.LogService.LogService logService, IStoreInventoryTransferListRepository storeInventoryTransferListRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
            _storeInventoryTransferListRepository = storeInventoryTransferListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/StoreInventoryTransferList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> LoadListData(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var result = await _storeInventoryTransferListRepository.LoadListDataAsync(searchTerm, pageNo, pageSize);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteData(string docId)
        {
            try
            {
                var result = await _storeInventoryTransferListRepository.DeleteDataAsync(docId);
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
