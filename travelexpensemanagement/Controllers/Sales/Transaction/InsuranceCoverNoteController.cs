using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Implementations.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class InsuranceCoverNoteController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IInsuranceCoverNoteRepository _insuranceCoverNoteRepository;

        public InsuranceCoverNoteController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IInsuranceCoverNoteRepository insuranceCoverNoteRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _insuranceCoverNoteRepository = insuranceCoverNoteRepository;
        }

        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.GlobalVariables = globalVar;
            return View("~/Views/Sales/Transaction/InsuranceCoverNote/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GenerateVNo()
        {
            string newV_NO = "00001";
            string vType = "INSU";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = @"SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string query = @"
                    SELECT ISNULL(MAX(CAST(RIGHT(CAST(V_NO AS VARCHAR(20)), 5) AS INT)), 0) + 1 FROM sale3 WHERE V_TYPE = @VType AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@VType", vType);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    int nextNo = Convert.ToInt32(cmd.ExecuteScalar());

                    newV_NO = prefixYR + nextNo.ToString("D5");
                }

                return Json(new { v_NO = newV_NO,  v_TYPE = vType });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult InsuranceTypeDDL()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT 
                CODE,
                NAME,
                POLICY_AMT,
                EFF_DATE,
                EXP_DATE
            FROM INSU_MAST
            WHERE COMP_CODE = @COMP_CODE
              AND ACTIVE = 1
            ORDER BY NAME";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    value = reader["CODE"],
                    text = reader["NAME"],
                    policyAmt = reader["POLICY_AMT"],
                    effDate = reader["EFF_DATE"],
                    expDate = reader["EXP_DATE"]
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult BillTypeDDL()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"Select CODE,NAME from DOCTYPE_MAST where DOCTYPE in ('SalesInvoice','Jobworkissue','SaleChallan') order by SNO";
            var dropdownList = _dropdownService.GetDropdownList(query);
            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult FromBillNoDDL(string vType)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = $@"
            SELECT V_NO, V_NO [VNO]
            FROM SALE1
            WHERE V_TYPE = '{vType}'
              AND ISNULL(INSU_NO, 0) = 0
              AND COMP_CODE = {globalVariables.PubCompCode}
              AND BRANCH_CODE = {globalVariables.PubBranchCode}
              AND YEAR_CODE = {globalVariables.PubFYearCode}
            ORDER BY V_NO";

            var dropdownList = _dropdownService.GetDropdownList(query);

            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult GetPolicyBalanceAmount(int insuCode)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                string query = @"
                SELECT
                    ISNULL((
                        SELECT POLICY_AMT
                        FROM INSU_MAST
                        WHERE CODE = @INSU_CODE
                          AND COMP_CODE = @COMP_CODE
                    ), 0)
                    -
                    ISNULL((
                        SELECT SUM(ISNULL(NAMOUNT, 0))
                        FROM SALE1
                        LEFT JOIN SALE3
                            ON SALE3.V_NO = SALE1.INSU_NO
                            AND SALE3.COMP_CODE = SALE1.COMP_CODE
                            AND SALE3.BRANCH_CODE = SALE1.BRANCH_CODE
                            AND SALE3.YEAR_CODE = SALE1.YEAR_CODE
                        WHERE SALE3.INSU_CODE = @INSU_CODE
                          AND SALE3.COMP_CODE = @COMP_CODE
                          AND SALE3.BRANCH_CODE = @BRANCH_CODE
                    ), 0) AS PolicyBalance";

                using var con = _dbConnection.GetErpConnection();
                con.Open();

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@INSU_CODE", insuCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);

                var result = cmd.ExecuteScalar();

                decimal balance = result == null || result == DBNull.Value ? 0 : Convert.ToDecimal(result);

                return Json(new { policyBalance = balance.ToString("0.00")});
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateData([FromBody] InsuranceCoverNoteModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                var result = await _insuranceCoverNoteRepository.SaveAndUpdateDataAsync(model);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadEditData(int vNo, string vType)
        {
            var result = await _insuranceCoverNoteRepository.LoadEditDataAsync(vNo, vType);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("SALE3", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetInsuranceItemName(int fBillNo, int tBillNo, string billType)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                string query = @"
                SELECT TOP 1 ITEM_NAME
                FROM SALE2
                WHERE COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE
                  AND V_NO >= @F_BILL_NO
                  AND V_NO <= @T_BILL_NO
                  AND V_TYPE = @BILL_TYPE
                ORDER BY AMOUNT DESC";

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                cmd.Parameters.AddWithValue("@F_BILL_NO", fBillNo);
                cmd.Parameters.AddWithValue("@T_BILL_NO", tBillNo);
                cmd.Parameters.AddWithValue("@BILL_TYPE", billType);

                var itemName = cmd.ExecuteScalar()?.ToString() ?? "";

                return Json(new
                {
                    success = true,
                    itemName = itemName
                });
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
