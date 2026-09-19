using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesSaudaListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly ISaleSaudaEntryListRepository _itSaleSaudaEntryEntryListRepository;

        public SalesSaudaListController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService, ISaleSaudaEntryListRepository saleSaudaEntryListRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
            _itSaleSaudaEntryEntryListRepository = saleSaudaEntryListRepository;
        }
        
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesSaudaList/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> LoadListData(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var result = await _itSaleSaudaEntryEntryListRepository.LoadListDataAsync(searchTerm, pageNo, pageSize);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}
