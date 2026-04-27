using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.MonthlyTransaction;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class AdvanceListCreationController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public AdvanceListCreationController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/MonthlyTransaction/AdvanceListCreation/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            var vtypeList = await _masterDataService.GetDocTypeAsync("PayIntrim");
            return Json(vtypeList);
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string vtype)
        {
            var vnoNew = await _masterDataService.GetMaxVNoAsync(vtype, "PAY_INTRIM");
            return Json(vnoNew);
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentMast()
        {
            var dataList = await _masterDataService.GetEmployeeDepartMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetDesignationList()
        {
            var dataList = await _masterDataService.GetDesignationMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeList()
        {
            var dataList = await _masterDataService.GetEmployeeMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetBankList()
        {
            var dataList = await _masterDataService.GetBankMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetFilteredData(string vdate, int departmentId, int cbDeptId, int cbDesignId, int cbEmplId)
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var parameters = new Dictionary<string, object>
                {
                {"@COMP_CODE", companyCd},
                {"@V_DATE", vdate},
                {"@Department", departmentId},
                {"@cbDept", cbDeptId},
                {"@cbDesg", cbDesignId},
                {"@cbEmp", cbEmplId},
                {"@Action",  "GetFilterData"}
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("dbo.sp_AdvanceListCreation", parameters);
                return Json(new { status = true, data = dataList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetAdvanceListCreationForUpdate(string id)
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var yearCode = _globalValue.GetGlobalVariables().PubFYearCode;
                var vtype = id.Substring(0, 4);
                var vno = id.Substring(4);
                var parameters = new Dictionary<string, object>
                {
                {"@COMP_CODE", companyCd},
                {"@YEAR_CODE",  yearCode},
                {"@BRANCH_CODE", 1 },
                {"@V_TYPE", vtype},
                {"@V_NO", vno},
                {"@Action",  "GateInOutDataForUpdate"}
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_AdvanceListCreation]", parameters);
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateAdvanceListCreationEntry([FromBody] PayAdvanceList model)
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
                            var DOC_ID = model.VType + Convert.ToString(model.VNo);
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_AdvanceListCreation]", con))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Add ");
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@DOC_ID", DOC_ID);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                cmd.Parameters.AddWithValue("@V_DATE", model.VDate);
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                var tvp = new SqlParameter("@AdvanceList", SqlDbType.Structured)
                                {
                                    TypeName = "Type_AdvanceListCreation",
                                    Value = FillDataTable(model.payAdvanceLists)
                                };
                                cmd.Parameters.Add(tvp);
                                await cmd.ExecuteNonQueryAsync();

                            }
                        return Json(new { status = true, message = "Data save/update successfully" });
                       
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

        private DataTable FillDataTable(List<PayAdvanceListDT> items)
        {
            var table = new DataTable();
            table.Columns.Add("EMP_CODE", typeof(int));
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("WDAY", typeof(decimal));
            table.Columns.Add("GROSS", typeof(decimal));
            table.Columns.Add("WAGES", typeof(decimal));
            table.Columns.Add("ADVNAMT", typeof(decimal));
            table.Columns.Add("SANCAMT", typeof(decimal));
            table.Columns.Add("REMARK", typeof(string));
            table.Columns.Add("BANK_CH", typeof(decimal));
            table.Columns.Add("RATE", typeof(decimal));
            table.Columns.Add("PER_FLG", typeof(int));

            foreach (var item in items ?? new List<PayAdvanceListDT>())
            {
                table.Rows.Add(
                    item.EmpCode ?? (object)DBNull.Value,
                    item.SNo ?? (object)DBNull.Value,
                    item.WDay ?? (object)DBNull.Value,
                    item.Gross ?? (object)DBNull.Value,
                    item.Wages ?? (object)DBNull.Value,
                    item.AdvNamT ?? (object)DBNull.Value,
                    item.SancAmt ?? (object)DBNull.Value,
                    item.Remark ?? (object)DBNull.Value,
                    item.BankCh ?? (object)DBNull.Value,
                    item.Rate ?? (object)DBNull.Value,
                    item.PerFlg ?? (object)DBNull.Value
                );
            }
            return table;
        }

    }
}
