using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using System.Text.Json.Nodes;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;
using travelexpensemanagement.Models.Weighbridge.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class MissedPunchEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public MissedPunchEntryController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/MissedPunchEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = "1";
                var vType = V_type;
                var tableName = "PAY_PUNCH_MISS";

                var yearParams = new Dictionary<string, object> { { "@YearCd", yearCode } };
                var vnoParams = new Dictionary<string, object>
            {
            { "@COMP_CODE", companyCode },
            { "@BRANCH_CODE", branchCode },
            { "@YEAR_CODE", yearCode },
            { "@V_TYPE", vType },
            { "@TableName", tableName }
            };

                string nextVNo = await _dbHelper.GetExecuteScalarAsync<string>("sp_GetMaxVNo", vnoParams, isStoredProc: true);
                string year = await _dbHelper.GetExecuteScalarAsync<string>("SELECT dbo.fn_GetCurrentYear(@YearCd)", yearParams);
                var docId = (vType) + (year) + (nextVNo);
                var newVno = year + nextVNo;
                var docIdNoList = new { DocId = docId, VNo = newVno };
                return Json(new { status = true, data = docIdNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='PunchMissed' ");
                return Json(new { status = true, data = Doctype });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeList()
        {
            var employeeList= await _masterDataService.GetEmployeeMastAsync();
            return Json(employeeList);
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeDepartmentList()
        {
            var departList = await _masterDataService.GetEmployeeDepartMastAsync();
            return Json(departList);
        }
        [HttpGet]
        public async Task<IActionResult> GetShiftList()
        {
            var shiftlist = await _masterDataService.GetShiftMastAsync();
            return Json(shiftlist);
        }

        [HttpGet]
        public async Task<IActionResult> GetMissedPunchEntryDataById(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();

                    var parameters = new Dictionary<string, object>
                    {
                       { "@COMP_CODE", usersession.PubCompCode },
                       { "@BRANCH_CODE", 1 },
                       { "@YEAR_CODE", usersession.PubFYearCode },
                       { "@DOC_ID", id },
                       { "@Action", "MissedPunchDataForUpdate" }
                    };
                var punchMissedList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetMissedPunchEntry]", parameters);
                return Json(new
                {
                    status=true,
                    data=punchMissedList
                });
            }
            catch(Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateMissedPunchEntry([FromBody] PayMissedPunch model)
        {
            if (model == null)
                return Json(new { status = false, message = "Data save failed." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;
                        try
                        {
                            
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_MissedPunchEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO); 
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                var tvp = new SqlParameter("@MissedPunchData", SqlDbType.Structured)
                                {
                                    TypeName = "Type_PayPunchMissed",
                                    Value = ToWB2DataTable(model.PayMissedPunchDetails)
                                };
                                cmd.Parameters.Add(tvp);
                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.ReturnValue
                                };
                                cmd.Parameters.Add(returnParam);
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);
                                await cmd.ExecuteNonQueryAsync();

                                string errorMessage = errorParam.Value?.ToString();
                                if ((int)returnParam.Value <= 0)
                                    success = false;
                            }

                            if (success)
                                transaction.Commit();
                            else
                                transaction.Rollback();

                            return Json(new
                            {
                                status = success,
                                message = success ? "Data save/update successfully." : "Failed to save or update some entry details."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction?.Rollback();
                            return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        private DataTable ToWB2DataTable(List<PayMissedPunchDetail> items)
        {
            var table = new DataTable();
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("EMP_CODE", typeof(string));
            table.Columns.Add("EMP_NAME", typeof(string));
            table.Columns.Add("DEPT_CODE", typeof(string));
            table.Columns.Add("DEPT_NAME", typeof(string));
            table.Columns.Add("OUT_TYPE", typeof(string));
            table.Columns.Add("V_DATE", typeof(DateTime));
            table.Columns.Add("SHIFT", typeof(string));
            table.Columns.Add("IN_TIME", typeof(TimeSpan));
            table.Columns.Add("OUT_TIME", typeof(TimeSpan));
            table.Columns.Add("REMARKS", typeof(string));
            table.Columns.Add("FAPROV_STATUS", typeof(string));
            table.Columns.Add("FAPROV_REMARKS", typeof(string));

            foreach (var item in items ?? new List<PayMissedPunchDetail>())
            {
                table.Rows.Add(
                    item.SNO ?? (object)DBNull.Value,
                    item.EMP_CODE ?? (object)DBNull.Value,
                    item.EMP_NAME ?? (object)DBNull.Value,
                    item.DEPT_CODE ?? (object)DBNull.Value,
                    item.DEPT_NAME ?? (object)DBNull.Value,
                    item.OUT_TYPE ?? (object)DBNull.Value,
                    item.V_DATE ?? (object)DBNull.Value,
                    item.SHIFT ?? (object)DBNull.Value,
                    item.IN_TIME ?? (object)DBNull.Value,
                    item.OUT_TIME ?? (object)DBNull.Value,
                    item.REMARKS ?? (object)DBNull.Value,
                    item.FAPROV_STATUS ?? (object)DBNull.Value,
                    item.FAPROV_REMARKS ?? (object)DBNull.Value
                );
            }

            return table;
        }


    }
}
