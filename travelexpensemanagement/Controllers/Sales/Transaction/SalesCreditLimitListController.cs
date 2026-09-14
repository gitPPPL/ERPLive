using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;


namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesCreditLimitListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalExcelExport _excel;
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly ISalesCreditLimitListRepository _repo;

        public SalesCreditLimitListController(GlobalVariableService globalVariableService, GlobalExcelExport excel,
            DbHelper dbHelper, ISalesCreditLimitListRepository repo, DataBaseConnection dbConnection)
        {
            _globalVariableService = globalVariableService;
            _excel = excel;
            _dbHelper = dbHelper;
            _repo = repo;
            _dbConnection = dbConnection;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesCreditLimitList/Index.cshtml");
        }
        const string doctype = "CLMT";

        [HttpGet]
        public IActionResult GetAllSalesCreditLimitList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = _repo.GetAllSalesCreditLimitList(searchTerm, pageNumber, pageSize);
            if (result.data == null || !result.status)
            {
                return Json(new { status = false, message = result.message });
            }
            return Json(new { status = true, data = result.data, totalCount = result.totalCount });
        }

        [HttpPost]
        public IActionResult Delete(int vNo)
        {
            var result = _repo.Delete(doctype, vNo);
            return Json(new { status = result.status, message = result.message });
        }

        [HttpGet]
        public IActionResult CheckApprovalStatus(int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string lastUser = null;
            bool isBlocked = false;

            const string checkSql = @"SELECT TOP 1 status FROM APPROVAL_STATUS WHERE v_type = @v_type AND v_NO = @v_NO AND comp_code = @comp_code AND 
                            branch_code = @branch_code AND year_code = @year_code AND status = 'OPEN' AND USER_CODE <> @USER_CODE";

            const string lastUserSql = @"SELECT TOP 1 user_name FROM APPROVAL_STATUS WHERE v_type = @v_type AND v_NO = @v_NO AND comp_code = @comp_code
                                AND branch_code = @branch_code AND year_code = @year_code AND status = 'OPEN' AND user_code <> @user_code 
                                ORDER BY srno DESC";

            using (var conn = _dbConnection.GetErpConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@v_type", doctype);
                    cmd.Parameters.AddWithValue("@v_NO", vNo);
                    cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);

                    var result = cmd.ExecuteScalar();
                    isBlocked = result != null;
                }

                if (isBlocked)
                {
                    using var cmd2 = new SqlCommand(lastUserSql, conn);
                    cmd2.Parameters.AddWithValue("@v_type", doctype);
                    cmd2.Parameters.AddWithValue("@v_NO", vNo);
                    cmd2.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                    cmd2.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                    cmd2.Parameters.AddWithValue("@year_code", gv.PubFYearCode);
                    cmd2.Parameters.AddWithValue("@user_code", gv.PubUserId);

                    lastUser = cmd2.ExecuteScalar() as string;
                }
            }

            if (isBlocked)
            {
                return Ok(new { isBlocked = true, message = $"This Document Approval is in process at User: {lastUser}, Edit not allowed." });
            }

            return Ok(new { isBlocked = false, message = (string)null });
        }

        [HttpGet]
        public async Task<JsonResult> PBPEntryDetails(string vNo)
        {
            try
            {
                var usersession = _globalVariableService.GetGlobalVariables();
                if (string.IsNullOrEmpty(vNo))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", doctype},
                    {"@V_NO", vNo },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_SalesCreditLimit]", parameter);
                return Json(new { status = true, data = entryDetailList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@YEAR_CODE", gv.PubFYearCode },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    { "@Action", "Excel" }
                };

                var fileBytes = _excel.ExportToExcel("sp_SalesCreditLimit", "Sales Credit Limit", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"SalesCreditLimit{DateTime.Now:ddMMyyyy}.xlsx"
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
    }
}
