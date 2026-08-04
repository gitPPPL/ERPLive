using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TransitEntryListController : Controller
    {
        private readonly ITransitEntryListRepository _iTransitEntryListRepository;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DataBaseConnection _dataBaseConnection;
        private readonly GlobalExcelExport _excel;
        public TransitEntryListController(ITransitEntryListRepository iTransitEntryListRepository, ModuleService.ModuleService moduleService, GlobalVariableService globalVariableService, GlobalValidationdate globalValidationdate
            , DataBaseConnection dataBaseConnection, GlobalExcelExport excel)
        {
            _moduleService = moduleService;
            _iTransitEntryListRepository = iTransitEntryListRepository;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dataBaseConnection = dataBaseConnection;
            _excel = excel;
        } 
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Transit EWwaybill";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string databaseName;
            using (var connection = _dataBaseConnection.GetErpConnection())
            {
                databaseName = connection.Database; // Get the database name
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel,
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

                var fileBytes = _excel.ExportToExcel("sp_TransitEntry", "Transit EWayBill", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"TransitEWayBill_{DateTime.Now:ddMMyyyy}.xlsx"
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
