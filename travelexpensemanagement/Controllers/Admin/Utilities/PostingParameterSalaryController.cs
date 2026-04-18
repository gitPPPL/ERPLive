using Microsoft.AspNetCore.Mvc;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Utilities;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class PostingParameterSalaryController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public PostingParameterSalaryController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/PostingParameterSalary/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetVTypeList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("select CODE,NAME from DOCTYPE_MAST where isnull(DOCTYPE, '') = 'SalesInvoice' order by NAME");
                return Json(new { status = true, data = dataList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            //var dataList = await _masterDataService.GetPartyListAsync();
            //return Json(dataList);
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync(" select CODE,NAME from SUBGROUP_MAST where COMP_CODE=" + companyCd + " and ACTIVE = 1 and isnull(NATURE, '')='Others' order by NAME ");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocList()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"
                select distinct DOC_TYPE as Name from POSTING_MAST a left join Doctype_mast c on a.v_type=c.code 
                where c.doctype not in ('salesinvoice','SalesReturn','JobworkIssue') and a.comp_code={companyCd} AND a.BRANCH_CODE= 1 Order by a.Doc_Type
                ");

                return Json(new { status = true, data = dataList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPostingParameterSalaryForUpdate(string Vtype)
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCd},
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", Vtype},
                    {"@Action", "PostingSalaryEntryForUpdate"}
                };
                var dataList =await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PostingSalaryEntry]", parameter);
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdatePostingParameterSalaryEntry([FromBody] PostingSalary model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request: Model is null." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PostingSalaryEntry]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;                       
                        cmd.Parameters.AddWithValue("@Action", "Add");
                        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@DOC_TYPE", "SALARY");
                        cmd.Parameters.AddWithValue("@POST_TYPE", "SALARY");
                        cmd.Parameters.AddWithValue("@V_TYPE", "SALP");
                        cmd.Parameters.AddWithValue("@SALARY_AC", model.SalaryAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SALARY_PAYAC", model.SalaryPayAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@HRA_AC", model.HraAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CONV_AC", model.ConvAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@OTHER_AC", model.OtherAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WASHING_AC", model.WashingAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SPECIAL_AC", model.SpecialAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMPLOYEE_PFAC", model.EmployeePFac ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMPLOYEE_ESIAC", model.EmployeeEsiAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMPLOYER_PFAC", model.EmployerPFac ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMPLOYER_ESIAC", model.EmployerEsiAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ADVANCE_AC", model.AdvanceAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TDS_PAYAC", model.TdsPayAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ADMIN_CHAC", model.AdminChAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PENSION_AC", model.PensionAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EPF_PAYAC", model.EpFPayAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ESI_PAYAC", model.EsiPayAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WAGES_AC", model.WagesAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WAGES_PAYAC", model.WagesPayAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PRODINC_AC", model.ProdIncAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PRODINC_PAYAC", model.ProdIncPayAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EXGRATIA_AC", model.ExGratiaAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EXGRATIA_PAYAC", model.ExGratiaPayAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@OTHER_MTHDEDAC", model.OtherMthDedAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LOAN_AC", model.LoanAc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                        await cmd.ExecuteNonQueryAsync();
                        return Json(new { status = true, message = "Data saved/updated successfully." });
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                var errorMessage = $"Error Number: {sqlEx.Number}, Message: {sqlEx.Message}, Line: {sqlEx.LineNumber}, Procedure: {sqlEx.Procedure}";
                return Json(new { status = false, message = "SQL Error: " + errorMessage });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Unexpected error: " + ex.Message });
            }
        }


    }
}
