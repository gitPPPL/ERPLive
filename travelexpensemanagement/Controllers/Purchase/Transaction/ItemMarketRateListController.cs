using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ItemMarketRateListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly IItemMarketRateListRepository _itemMarketRateListRepository;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly GlobalExcelExport _globalExcelExport;
        public ItemMarketRateListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService, DbHelper dbHelper,ModuleService.ModuleService moduleService, IItemMarketRateListRepository itemMarketRateList, GlobalValidationdate globalValidationdate, GlobalExcelExport globalExcelExport)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _itemMarketRateListRepository = itemMarketRateList;
            _globalValidationdate = globalValidationdate;
            _globalExcelExport = globalExcelExport;
        }

        public IActionResult Index()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = globalVar.PubBranchCode;
            ViewBag.YearCode = globalVar.PubFYearCode;
            ViewBag.CurrentMenu = "Purchase Price List";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Purchase/Transaction/ItemMarketRateList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllItemRateList(string searchTerm = "",int pageNumber = 1,int pageSize = 10)
        {
            try
            {
                var result = _itemMarketRateListRepository.GetAllItemRateList(searchTerm, pageNumber, pageSize);

                return Json(new
                {
                    success = true,
                    itemRates = result.itemRates,
                    totalCount = result.totalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching Item Market Rate list.",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult DeleteItemMarketRateByCode(int code, string vType, int compCode, int branchCode,int yearCode)
        {
            try
            {
                bool isDeleted = _itemMarketRateListRepository.DeleteItemMarketRateByCode(code, vType, compCode, branchCode,yearCode);

                if (isDeleted)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Item Market Rate deleted successfully."
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Record not found or could not be deleted."
                });
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

        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@YEAR_CODE", gv.PubFYearCode },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    { "@Action", "Excel" }
                };

                var fileBytes = _globalExcelExport.ExportToExcel("sp_MARKET_RATE", "Item Market Rate", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"ItemMarketRate_{DateTime.Now:ddMMyyyy}.xlsx"
                );
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
 