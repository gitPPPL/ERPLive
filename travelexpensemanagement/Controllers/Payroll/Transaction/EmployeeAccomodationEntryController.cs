
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.Models.Payroll.Transaction;
using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class EmployeeAccomodationEntryController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public EmployeeAccomodationEntryController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/EmployeeAccomodationEntry/Index.cshtml");
        }


        [HttpGet]
        public async Task<IActionResult> GetMaxVNo()
        {
            var maxVnoData = await _masterDataService.GetMaxVNoAsync("EMPA", "PAY_EMPACCOMODATION");
            return Json(maxVnoData);

        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeMast()
        {
            var employeeList = await _masterDataService.GetEmployeeMastAsync();
            return Json(employeeList);
        }
        [HttpGet]
        public async Task<IActionResult> GetDesignMastList()
        {
            var designList = await _masterDataService.GetDesignationMastAsync();
            return Json(designList);
        }
        [HttpGet]
        public async Task<IActionResult> GetDepartmentMast()
        {
            var dataList = await _masterDataService.GetEmployeeDepartMastAsync();
            return Json(dataList);
        }


        [HttpGet]
        public async Task<IActionResult> GetEmpAccomodationById(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@DOC_ID", id},
                    {"@Action", "EmpAccomodationUpdateById"}
                };
                var detaillist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_EmpAccomodationEntry]", parameter1);
                return Json(new { status = true, data = detaillist });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

       
        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateEmpAccomodationEntry([FromBody] PayEmpAccommodation model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request: Model is null." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_EmpAccomodationEntry]", con))
                    {
                        var Vtype = (model.DOC_ID).Substring(0, 4);

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@V_TYPE", Vtype);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@APPL_FROM", model.APPL_FROM ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ROOM_NO", model.ROOM_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REFERENCE_BY", model.REFERENCE_BY ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", model.FAPROV_STATUS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", model.FAPROV_REMARKS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ValidUpto", model.ValidUpto ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LIVING_STATUS", model.LIVING_STATUS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                        await cmd.ExecuteNonQueryAsync();
                        return Json(new { status = true, message = "Data saved/updated successfully." });

                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Unexpected error: " + ex.Message });
            }
        }



    }
}
