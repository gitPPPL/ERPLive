using Microsoft.AspNetCore.Mvc;
using System.Data;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class EmployeeGateOutEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public EmployeeGateOutEntryController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/EmployeeGateOutEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var doctypeList = await _dbHelper.GetJsonDataAsync("select distinct CODE , NAME from DOCTYPE_MAST where code in ('IN','OUT') ");
                return Json(new { status = true, data = doctypeList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message="data load failed" });

            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string vtype)
        {
            var response = await _masterDataService.GetMaxVNoAsync(vtype, "PAY_INOUT");
            return Json(response);
        }
        [HttpGet]
        public async Task<IActionResult> GetShiftList()
        {
            var shifList = await _masterDataService.GetShiftMastAsync();
            return Json(shifList);
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeList()
        {
            var dataList = await _masterDataService.GetEmployeeMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetHodList()
        {
            var dataList = await _masterDataService.GetHodMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentList()
        {
            var dataList = await _masterDataService.GetEmployeeDepartMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetGateInOutDataForUpdate(string id)
        {           
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object> {
                {"@COMP_CODE", usersession.PubCompCode},
                {"@YEAR_CODE", usersession.PubFYearCode},
                {"@BRANCH_CODE", 1},
                {"@DOC_ID",  id},
                {"@Action", "GateInOutDataForUpdate"}
            };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GateInOutEntry]", parameter);
                return Json(new { status = true, data = dataList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });

            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateGateInOutEntry([FromBody] PayGateInOut model)
        {
            if (model == null)
                return Json(new { status = false, message = "Data save failed." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    bool success = true;

                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("[dbo].[sp_GateInOutEntry]", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                            cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.VType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                            cmd.Parameters.AddWithValue("@V_DATE", model.VDate);
                            cmd.Parameters.AddWithValue("@DOC_ID", model.DocId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHIFT", model.Shift ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMP_CODE", model.EmpCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@EMP_NAME", model.EmpName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEPT_CODE", model.DeptCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEPT_NAME", model.DeptName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARKS", model.Remarks ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@E_DATE", model.EDate);
                            cmd.Parameters.AddWithValue("@E_TIME", model.ETime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IN_TIME", model.InTime ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GP_NO", model.GpNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEDU_HRS", model.DeduHrs);
                            cmd.Parameters.AddWithValue("@HOD_CODE", model.HodCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@HOD_NAME", model.HodName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REASON_CODE", model.ReasonCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GP_TYPE", model.GpType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@GP_HRS", model.GpHrs);
                            cmd.Parameters.AddWithValue("@LATE_HRS", model.LateHrs);
                            cmd.Parameters.AddWithValue("@SLEEP_HRS", model.SleepHrs);
                            cmd.Parameters.AddWithValue("@WORKPLACE_PLACE", model.WorkplacePlace ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WORKPLACE_CODE", model.WorkplaceCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@APPROVE", model.Approve);
                            cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId ?? (object)DBNull.Value);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        return Json(new
                        {
                            status = success,
                            message = success ? "Data save/update successfully." : "Failed to save or update some entry details."
                        });
                    }
                    catch (Exception ex)
                    {
                        return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }



    }
}
