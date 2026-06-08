using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;


namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class ParameterMasterListController : Controller
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IParameterMasterListRepository _repository;

        public ParameterMasterListController(DataBaseConnection dbcontext, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, 
            GlobalValidationdate globalValidationdate, IParameterMasterListRepository repository)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _repository = repository;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "QC Parameter Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel,
            };
            var globalVariables = _globalValue.GetGlobalVariables();

            string databaseName;
            using (var connection = _dbcontext.GetErpConnection())
            {
                databaseName = connection.Database; // Get the database name
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/QualityControl/Master/ParameterMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetQualityParamList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _repository.GetQualityParamListAsync(searchTerm, pageNumber, pageSize);
            if(result.data != null)
            {
                return Json(new { status = true, data = result.data, totalCount = result.totalCount });
            }
            return Json(new { status = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult IsQcParamDeletable(int docId)
        {
            if (docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = _repository.IsQcParamDeletableAsync(docId);
            return Json(new { success = result.status, message = result.message, isExists = result .data});
        }

        [HttpPost]
        public async Task<IActionResult> DelQParamMast(int docId)
        {
            if (docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = await _repository.DelQParamMastAsync(docId);
            return Json(new { success = result.status, message = result.message});
        }

        public class QCprameterDto
        {
            public int? CODE { get; set; }
            public string? NAME { get; set; }
            public string? SHORTNAME { get; set; }
            public string? QUNIT { get; set; }
            public int? qty { get; set; }
            public int? ACTIVE { get; set; }
        }

        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalValue.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@companyCd", gv.PubCompCode },
                    { "@AED", "Excel" }
                };

                var fileBytes = _globalValidationdate.ExportToExcel("sp_QualityParameterMast_AED", "Qc Parameter Master", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"QcParameterMaster_{DateTime.Now:ddMMyyyy}.xlsx"
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
