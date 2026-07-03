using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net.Mail;
using System.Reflection.Emit;
using System.Text.Json;
using System.Threading.Tasks;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Master;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Master;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class QuotationRateApprovalController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly DropdownService _dropdownService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IQuotationRateApprovalRepository _quotationRateApprovalRepository;
        public QuotationRateApprovalController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, DropdownService dropdownService, GlobalValidationdate globalValidationdate, IQuotationRateApprovalRepository quotationRateApprovalRepository)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _dropdownService = dropdownService;
            _globalValidationdate = globalValidationdate;
            _quotationRateApprovalRepository = quotationRateApprovalRepository;
        }
        
        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbcontext.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVar = _globalValue.GetGlobalVariables();
            ViewBag.GlobalVariables = globalVar;
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = globalVar.PubBranchCode;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/Purchase/Transaction/QuotationRateApproval/Index.cshtml");
        }

        public async Task<IActionResult> GetMaxVNo()
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = userSession.PubBranchCode;
                var vType = "STAP";
                var tableName = "QUOTATION2";

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
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='RateApproved' ");
                return Json(new { status = true, data = Doctype });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        //[HttpGet]
        //public IActionResult GetItemName(string search = "", int page = 1)
        //{
        //    int pageSize = 500;

        //    var compCode = _globalValue.GetGlobalVariables().PubCompCode;

        //    string query = $@"
        //    SELECT CODE, NAME
        //    FROM ITEM_MAST
        //    WHERE COMP_CODE = {compCode}
        //    AND ACTIVE = 1
        //    AND ('{search}' = '' OR NAME LIKE '%{search}%')
        //    ORDER BY NAME
        //    OFFSET {(page - 1) * pageSize} ROWS
        //    FETCH NEXT {pageSize} ROWS ONLY";

        //    var list = _dropdownService.GetDropdownList(query);

        //    return Json(new
        //    {
        //        status = true,
        //        data = list,
        //        pagination = new
        //        {
        //            more = list.Count == pageSize
        //        }
        //    });
        //}

        [HttpGet]
        public IActionResult GetItemName()
        {
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;

            string query= $@"  SELECT CODE, NAME
            FROM ITEM_MAST
            WHERE COMP_CODE = {compCode}
            AND ACTIVE = 1 order by name ";

            var list  = _dropdownService.GetDropdownList(query);

            return Json(new { status = true, data = list });
        }

        [HttpGet]
        public async Task<IActionResult> GetVendorName()
        {
            try
            {
                var vendorList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from SUBGROUP_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode}  order by NAME ");
                return Json(new { status = true, data = vendorList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetQuotRtApvrlDetailsById(string id)
        {
            var result = await _quotationRateApprovalRepository.GetQuotRtApvrlDetailsById(id);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> GetFilterItemdetails([FromBody] FilterItemload filtrItmModel)
        {
            var result = await _quotationRateApprovalRepository.GetFilterItemdetails(filtrItmModel);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateQuotRateApproval([FromBody] QuotationRateApproval equotmodel)
        {
            var result = await _quotationRateApprovalRepository.SaveOrUpdateQuotRateApproval(equotmodel);

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalValue.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("QUOTATION2", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string vType, int vNo)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("SP_QUOTATIONRATEAPPROVAL", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "COPYTOEXCEL");
                        cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);

                        DataTable dt = new DataTable();

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }

                        using (XLWorkbook workbook = new XLWorkbook())
                        {
                            workbook.Worksheets.Add(dt, "Quotation");

                            using (MemoryStream stream = new MemoryStream())
                            {
                                workbook.SaveAs(stream);

                                return File(
                                    stream.ToArray(),
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    $"Quotation_{vNo}.xlsx");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
