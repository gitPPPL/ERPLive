using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class DispatchPlanningListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IDispatchPlanningListRepository _dispatchPlanningListRepository;

        public DispatchPlanningListController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IDispatchPlanningListRepository dispatchPlanningListRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _dispatchPlanningListRepository = dispatchPlanningListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/DispatchPlanningList/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> LoadListData(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var result = await _dispatchPlanningListRepository.LoadListDataAsync(searchTerm, pageNo, pageSize);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteData(int vNo, string vType)
        {
            var result = await _dispatchPlanningListRepository.DeleteDataAsync(vNo, vType);
            return Json(result);
        }



    }
}
