using iTextSharp.text.pdf.parser.clipper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Runtime.InteropServices.JavaScript;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Utilities;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class PostingParameterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public PostingParameterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/PostingParameter/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetVTypeList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("select CODE,NAME from DOCTYPE_MAST order by NAME");
                return Json(new { status = true, data = dataList });

            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync(" select CODE,NAME from SUBGROUP_MAST where COMP_CODE="+ companyCd + " and ACTIVE = 1 and isnull(NATURE, '')='Others' order by NAME ");
                return Json(new { status = true, data = dataList });
            }
            catch(Exception ex)
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
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPostingParameterForUpdate(string Vtype, string docType)
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCd},
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", Vtype},
                    {"@POST_TYPE", ""},
                    {"@FORM_CODE", ""},
                    {"@Action", "PostingParameterEntryForUpdate"}
                };
               
                var dataList =await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PostingParameterEntry]", parameter);
                return Json(new { status = true, data = dataList });
            }
            catch(Exception ex)
            {
                return Json(new { status = true, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdatePostingParameterEntry([FromBody] PostingParameter model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request: Model is null." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PostingParameterEntry]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;                         
                        string action = model.SaveOrUpdate == "Save" ? "Add" : "Edit";
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@DOC_TYPE", model.DOC_TYPE);
                        cmd.Parameters.AddWithValue("@POST_TYPE", model.POST_TYPE);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                        cmd.Parameters.AddWithValue("@FORM_CODE", model.FORM_CODE);
                        cmd.Parameters.AddWithValue("@SALE_AC", model.SALE_AC);
                        cmd.Parameters.AddWithValue("@DISC_AC", model.DISC_AC);
                        cmd.Parameters.AddWithValue("@CGST_AC", model.CGST_AC);
                        cmd.Parameters.AddWithValue("@SGST_AC", model.SGST_AC);
                        cmd.Parameters.AddWithValue("@IGST_AC", model.IGST_AC);
                        cmd.Parameters.AddWithValue("@FREIGHT_AC", model.FREIGHT_AC);
                        cmd.Parameters.AddWithValue("@ROUND_AC", model.ROUND_AC);
                        cmd.Parameters.AddWithValue("@INSU_AC", model.INSU_AC);
                        cmd.Parameters.AddWithValue("@TDS_AC", model.TDS_AC);
                        cmd.Parameters.AddWithValue("@TCS_AC", model.TCS_AC);
                        cmd.Parameters.AddWithValue("@QLTDR_AC", model.QLTDR_AC);
                        cmd.Parameters.AddWithValue("@QCDR_AC", model.QCDR_AC);
                        cmd.Parameters.AddWithValue("@QTYDR_AC", model.QTYDR_AC);
                        cmd.Parameters.AddWithValue("@RDDR_AC", model.RDDR_AC);
                        cmd.Parameters.AddWithValue("@CGST_AC_RCM", model.CGST_AC_RCM);
                        cmd.Parameters.AddWithValue("@SGST_AC_RCM", model.SGST_AC_RCM);
                        cmd.Parameters.AddWithValue("@IGST_AC_RCM", model.IGST_AC_RCM);
                        cmd.Parameters.AddWithValue("@CESS_AC", model.CESS_AC);
                        cmd.Parameters.AddWithValue("@PACK_AC", model.PACK_AC);
                        cmd.Parameters.AddWithValue("@IMPORT_PL", model.IMPORT_PL);
                        cmd.Parameters.AddWithValue("@GSTHOLD_AC", model.GSTHOLD_AC);
                        cmd.Parameters.AddWithValue("@BILLHOLD_AC", model.BILLHOLD_AC);
                        cmd.Parameters.AddWithValue("@TDS_194QAC", model.TDS_194QAC);
                        cmd.Parameters.AddWithValue("@LOADING_AC", model.LOADING_AC);
                        cmd.Parameters.AddWithValue("@WB_AC", model.WB_AC);
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
