using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{

    [SessionAuthorize]

    public class InventoryDepartmentIssueListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IInventoryDepartmentIssueListRepository _inventoryDepartmentIssueListRepository;
        public string Fromname = "AdjustmentIssue";

        public InventoryDepartmentIssueListController(DataBaseConnection dbConnection,
         GlobalVariableService globalVariableService,  DropdownService dropdownService,  travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
         travelexpensemanagement.ModuleService.ModuleService moduleService,  GlobalValidationdate globalValidationdate,IInventoryDepartmentIssueListRepository inventoryDepartmentIssueListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _inventoryDepartmentIssueListRepository = inventoryDepartmentIssueListRepository;
        }


        public IActionResult Index()
        {

            var globalVariables = _globalVariableService.GetGlobalVariables();
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/Inventory/Transaction/InventoryDepartmentIssueList/Index.cshtml");
        }



        [HttpGet]
        public async Task<IActionResult> GetList(  string searchTerm = "", int pageNumber = 1,  int pageSize = 10)
        {
            try
            {
                var result = await _inventoryDepartmentIssueListRepository.GetListAsync(searchTerm, pageNumber, pageSize, "AdjustmentIssue");

                return Json(new  { success = true,  data = result.Lists,  totalCount = result.TotalCount, pageNumber = pageNumber,  pageSize = pageSize });
            }
            catch (Exception ex)
            {
                return Json(new {  success = false, message = ex.Message });
            }
        }














    }
}
