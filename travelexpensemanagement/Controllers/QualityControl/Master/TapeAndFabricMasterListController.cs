using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class TapeAndFabricMasterListController : Controller
    {
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly ModuleService.ModuleService _moduleService;
        private readonly ITapeAndFabricMasterListRepository _repository;
        private readonly GlobalExcelExport _excel;

        public TapeAndFabricMasterListController(DataBaseConnection dbcontext, GlobalVariableService globalValue, 
            GlobalValidationdate globalValidationdate, ModuleService.ModuleService moduleService, ITapeAndFabricMasterListRepository repository, GlobalExcelExport excel)
        {
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _globalValidationdate = globalValidationdate;
            _moduleService = moduleService;
            _repository = repository;
            _excel = excel;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Tape And Fabric Master";
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
            return View("~/Views/QualityControl/Master/TapeAndFabricMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> GetTape_FabricList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _repository.GetTape_FabricListAsync(searchTerm, pageNumber, pageSize);
            if(result.data != null)
            {
                return Json(new { status = result.status, data = result.data, totalCount = result.totalCount });
            }
            return Json(new { status = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult IsTapeFabricDeletable(int docId)
        {
            if (docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = _repository.IsTapeFabricDeletableAsync(docId);
            if (result.data)
            {
                return Json(new { success = result.status, message = result.message, isExists = result.data });
            }
            return Json(new { success = result.status, message = result.message });

        }
        [HttpPost]
        public async Task<IActionResult> DelTape_FabricMast(int docId)
        {
            if (docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = await _repository.DelTape_FabricMastAsync(docId);
            return Json(new { success = result.status, message = result.message });
        }
        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalValue.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@CompanyCd", gv.PubCompCode },
                    { "@AED", "Excel" }
                };

                var fileBytes = _excel.ExportToExcel("sp_TapeNFabricMast_AED", "QC Tape And Fabric Master", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"QCTapeAndFabricMaster_{DateTime.Now:ddMMyyyy}.xlsx"
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

        public class QCStandardMasterDto
        {
            public int? CODE { get; set; }
            public string? NAME { get; set; }
            public string? MESH_NAME { get; set; }

            public decimal? STD_GRAM { get; set; }
            public decimal? MIN_GRAM { get; set; }
            public decimal? MAX_GRAM { get; set; }

            public decimal? GSM { get; set; }
            public decimal? DENIER { get; set; }

            public string? UNIT_NAME { get; set; }
            public string? COLOR_NAME { get; set; }

            public decimal? WIDTH { get; set; }

            public decimal? GPD { get; set; }
            public decimal? MIN_GPD { get; set; }
            public decimal? MAX_GPD { get; set; }

            public decimal? STD_STRENGTH { get; set; }
            public decimal? STRENGTH_MAX { get; set; }
            public decimal? STRENGTH_MIN { get; set; }

            public decimal? STD_ELONG { get; set; }
            public decimal? ELONG_MAX { get; set; }
            public decimal? ELONG_MIN { get; set; }

            public decimal? UNLAM_FAB { get; set; }
            public decimal? LAM_FAB { get; set; }

            public int? ACTIVE { get; set; }
        }
    }
}
