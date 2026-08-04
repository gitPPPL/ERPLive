using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class QCGroupMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IQCGroupMasterListRepository _repository;
        private readonly GlobalExcelExport _excel;

        public QCGroupMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate,
    IQCGroupMasterListRepository repository, GlobalExcelExport excel)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _repository = repository;
            _excel = excel;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "QC Group Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel,
            };
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database; // Get the database name
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            return View("~/Views/QualityControl/Master/QCGroupMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetAllQCGroups(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = _repository.GetAllQCGroupsAsync(searchTerm, pageNumber, pageSize);
            if (result.data != null)
            {
                return Json(new { success = result.status, lists = result.data , totalCount  = result.totalCount});
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public IActionResult GetQCGroupByCode(int code)
        {
            if(code <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = _repository.GetQCGroupByCodeAsync(code);
            if(result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }
        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
        {
            { "@Action", "Excel" }
        };

                var fileBytes = _excel.ExportToExcel("sp_QCG_MAST", "QC Group Master", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"QCGroupMaster_{DateTime.Now:ddMMyyyy}.xlsx"
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
 