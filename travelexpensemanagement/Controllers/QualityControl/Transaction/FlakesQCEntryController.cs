
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]
    public class FlakesQCEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IFlakesQCEntryRepository _flakesQCEntryRepository;
        public FlakesQCEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
          ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate ,IFlakesQCEntryRepository flakesQCEntryRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _flakesQCEntryRepository = flakesQCEntryRepository;
        }
        public IActionResult Index()
        {
            TempData["LoginDate"] = _globalVariableService.GetGlobalVariables().PubLoginDate;
            TempData["PubUserLevel"] = _globalVariableService.GetGlobalVariables().PubUserLevel;
            return View("~/Views/QualityControl/Transaction/FlakesQCEntry/Index.cshtml");
        }
        public JsonResult GetVNo()
        {
            string newV_NO = "00000";
            try
            {
                newV_NO = _globalValidationdate.GetVNo("SFQC", "PROD1_QC");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }
            return Json(new { V_NO = newV_NO });
        }
        public JsonResult DDLInspBy()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from EMP_MAST where Resign_Date is NULL and COMP_CODE= " + getdata.PubCompCode + "   ORDER BY name asc";
                var DDLInspBylist = _dropdownService.GetDropdownList(query);
                return Json(DDLInspBylist);
            }
        }
        public JsonResult DDLItem()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select a.Code,a.name from item_mast a " +
                    " left join ITEM_GROUP b on a.GROUP_CODE=b.CODE and b.COMP_CODE=" + getdata.PubCompCode + " and b.SALE_GROUP in ('Flakes')" +
                    " where a.Active=1 and a.comp_code= " + getdata.PubCompCode + "  and a.shortname <> '' group by a.NAME,a.CODE order by a.NAME asc";

                var DDLInspBylist = _dropdownService.GetDropdownList(query);

                return Json(DDLInspBylist);
            }

        }
        public JsonResult DDLPordPlace()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select Code,name from ITEMDEPT_MAST where Tran_type='Production' and Place_type='Washline' and COMP_CODE=" + getdata.PubCompCode + "  ";

                var DDLPordPlaceList = _dropdownService.GetDropdownList(query);

                return Json(DDLPordPlaceList);
            }

        }
        public JsonResult DDLChemist()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select code,Name from EMP_MAST WHERE Comp_code=" + getdata.PubCompCode + "  and Resign_date is null and Type in ('Staff','Semi Staff') Order by Name ";

                var DDLChemistList = _dropdownService.GetDropdownList(query);

                return Json(DDLChemistList);
            }

        }
        public JsonResult DDLQCIncharge()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select code,Name from EMP_MAST WHERE Comp_code=" + getdata.PubCompCode + " and Resign_date is null and Type in ('Staff') Order by Name ";

                var DDLQCInchargeList = _dropdownService.GetDropdownList(query);

                return Json(DDLQCInchargeList);
            }

        }
        public JsonResult DDLGridItem()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select a.Code,a.name from item_mast a left join ITEM_GROUP b on a.GROUP_CODE=b.CODE and b.COMP_CODE= " + getdata.PubCompCode + "     where a.Active=1 and b.SALE_GROUP in ('Flakes') " +
                    "and a.comp_code=" + getdata.PubCompCode + "  group by a.name,a.CODE order by a.name";

                var DDLGridItemList = _dropdownService.GetDropdownList(query);

                return Json(DDLGridItemList);
            }

        }
        [HttpPost]
        public IActionResult SavedData([FromBody] FlakesQCEntryLIst_Model request)
        {
            if (request?.Header == null)
            {
                return Json(new  {  success = false,  message = "Input model is null"  });
            }
            var action = request.Header.action == "INSERT"  ? "INSERT"  : "UPDATE";
            var result = _flakesQCEntryRepository.SubmitRequest(  request.Header, request.Deatils,  action);
            return result == "Success"  ? Json(new { success = true }) : Json(new { success = false, message = result });
        }
    }
}
