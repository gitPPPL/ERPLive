using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class QCMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IQCMasterListRepository _qcMasterListRepository;

        public QCMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, IQCMasterListRepository qcMasterListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _qcMasterListRepository = qcMasterListRepository;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "QC Master";
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

            return View("~/Views/QualityControl/Master/QCMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetQCMasterLList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var response = await _qcMasterListRepository.GetQCMasterListAsync(searchTerm, pageNumber, pageSize);
            if (response.status)
            {
                return Json(new { success = true, groups = response.data, totalCount = response.totalCount });
            }
            return Json(new { success = false, message = response.message });
        }

        [HttpPost]
        public async Task<JsonResult> DeleteQcMaster(int docId)
        {
            if(docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var response = await _qcMasterListRepository.DeleteQcMasterAsync(docId);
            return Json(new { success = response.status, message = response.message });
        }

        [HttpGet]
        public async Task<JsonResult> IsQcDeletable(int docId)
        {
            if (docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var response = await _qcMasterListRepository.IsQcDeletableAsync(docId);
            return Json(new { success = response.status, message = response.message, isExists = response.data });
        }

        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@Action", "Excel" }
                };

                var fileBytes = _globalValidationdate.ExportToExcel("Insert_QC_MAST", "QC Master", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"QCMaster_{DateTime.Now:ddMMyyyy}.xlsx"
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
