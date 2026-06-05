using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;
using static travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction.ILaminationQCEntryRepository;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]
    public class LaminationQCEntryController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly DropdownService _dropdownService;
        private readonly ILaminationQCEntryRepository _ILamQC;
        private readonly GlobalValidationdate _globalValidationdate;

        public LaminationQCEntryController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, 
            GlobalVariableService globalValue, ModuleService.ModuleService moduleService, DropdownService dropdownService, ILaminationQCEntryRepository ILamQC
            , GlobalValidationdate globalValidationdate)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _dropdownService = dropdownService;
            _ILamQC = ILamQC;
            _globalValidationdate = globalValidationdate;
        }
        public IActionResult Index()
        {
            var globalVariables = _globalValue.GetGlobalVariables();

            string databaseName;
            using (var connection = _dbcontext.GetErpConnection())
            {
                databaseName = connection.Database; // Get the database name
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/QualityControl/Transaction/LaminationQCEntry/Index.cshtml");
        }

        public JsonResult GetDropdown(string type, string VTypeId = "")
        {
            var gv = _globalValue.GetGlobalVariables();
            string query = "";
            switch (type)
            {
                case "Place":
                    query = $@"SELECT CODE, NAME FROM PLACE_MAST WHERE COMP_CODE = {gv.PubCompCode} ORDER BY NAME";
                    break;
                case "Supervisor":
                    query = $@"Select CODE, NAME from EMP_MAST where comp_Code={gv.PubCompCode} and Type in ('Staff','Semi Staff') and 
                            Resign_date is null and Join_Date is not null ORDER BY NAME";
                    break;
                case "Operator":
                    query = $@"Select  CODE, NAME from EMP_MAST where comp_Code={gv.PubCompCode} and Resign_date is null and 
                            Join_Date is not null ORDER BY NAME";
                    break;
                case "Strength":
                    query = $@"select CODE, NAME from TENACITY_MAST where COMP_CODE ={gv.PubCompCode} order by NAME";
                    break;
                case "Status":
                    query = $@"Select CODE, NAME from STATUS_MAST where comp_Code={gv.PubCompCode} Order by NAME";
                    break;
                case "Shift":
                    query = $@"SELECT DISTINCT SHIFT AS CODE, SHIFT AS NAME FROM SHIFT_MAST WHERE COMP_CODE = {gv.PubCompCode} ORDER BY NAME";
                    break;
                case "Plant":
                    query = $@"SELECT CODE, NAME FROM MACHINE_MAST WHERE TYPE = 'Lamination' AND COMP_CODE = {gv.PubCompCode}";
                    break;

            }
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }

        //===Check Modification Days
        [HttpGet]
        public JsonResult checkModificationDays(DateTime? vDate)
        {
            if (!vDate.HasValue)
            {
                return Json(new { success = false, message = "Doc Date is empty!!" });
            }
            var (allowed, message) = _globalValidationdate.CheckModificationDays(vDate.Value);
            return Json(new { success = true, isAllowed = allowed, message = message });
        }

        public async Task<IActionResult> GetQCDataList(string QcAllOrPending, int placeCode, string date, int plantCode)
        {

            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", userSession.PubCompCode },
                    {"@YEAR_CODE", userSession.PubFYearCode },
                    {"@BRANCH_CODE", userSession.PubBranchCode},
                    {"@PLACE_CODE", placeCode},
                    {"@V_DATE", date},
                    {"@PLANT_CODE", plantCode},
                    {"@QCAllOrPending",  QcAllOrPending},
                    {"@Action", "QC_LIST" }
                };

                var itemList = await _dbHelper.GetJsonFromProcedureAsync("sp_UpdateLamination", parameter);
                return Json(new { status = true, data = itemList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });

            }
        }
        
        [HttpPost]
        public async Task<IActionResult> UpdateLamination([FromBody] LaminationUpdateModel model)
        {
            if(model == null)
            {
                return Json(new { status = false, message = "Invalid data." });
            }      
            var result = await _ILamQC.UpdateLaminationAsync(model);
            return Json(new { status = result.status, message = result.message });
        }

        [HttpPost]
        public IActionResult ProcessTenacityData([FromBody] TenacityRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }
            var result = _ILamQC.ProcessTenacityDataAsync(request);
            if(result.data >= 0)
            {
                return Json(new { success = result.status, tenaMaxcode = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

    }
}
