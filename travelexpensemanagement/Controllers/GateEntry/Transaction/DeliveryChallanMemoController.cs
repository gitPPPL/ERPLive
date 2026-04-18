using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class DeliveryChallanMemoController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public DeliveryChallanMemoController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService; 
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/GateEntry/Transaction/DeliveryChallanMemo/Index.cshtml");
        }
        public int GetNextV_NO(string yearCode)
        {
            string newV_NO = "00000";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // Execute query to get PREFIXYR
                string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = '" + yearCode + "'";
                SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                // Execute query to get last V_NO
                string lastV_NO_Query = "SELECT TOP 1 V_NO FROM GATE_MEMO1 ORDER BY V_NO DESC";
                SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                string lastV_NO = lastVnoCmd.ExecuteScalar()?.ToString();

                int lastNumber = 0;
                if (!string.IsNullOrEmpty(lastV_NO) && lastV_NO.Length >= 9)
                {
                    string numericPart = lastV_NO.Substring(lastV_NO.Length - 5);
                    int.TryParse(numericPart, out lastNumber);
                }

                // Increment and format the new V_NO
                string newRunningNo = (lastNumber + 1).ToString("D5");
                newV_NO = prefixYR + newRunningNo;
            }

            return Convert.ToInt32(newV_NO);
        }

        public IActionResult GetEmpList(int cCode)
        {
            string query = "SELECT CODE,NAME FROM EMP_MAST WHERE COMP_CODE='" + cCode + "' AND ACTIVE=1 ORDER BY NAME ";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetUOMList()
        {
            string query = "SELECT CODE,NAME FROM QCPUNIT_MAST WHERE ACTIVE=1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public IActionResult GetVendorList(int cCode, int bCode, int yCode)
        {
            List<object> vendorList = new List<object>();

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection()) // replace with your actual connection getter
                {
                    string query = @"SELECT NAME 
                             FROM VISITOR 
                             WHERE COMP_CODE = @CompCode 
                               AND BRANCH_CODE = @BranchCode 
                               AND YEAR_CODE = @YearCode 
                             ORDER BY NAME";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", cCode);
                        cmd.Parameters.AddWithValue("@BranchCode", bCode);
                        cmd.Parameters.AddWithValue("@YearCode", yCode);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                vendorList.Add(new
                                {
                                    value = reader["NAME"].ToString(),
                                    text = reader["NAME"].ToString()
                                });
                            }
                        }
                    }
                }

                return Json(vendorList);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching vendor list", error = ex.Message });
            }
        }

        public IActionResult GetItemList(int cCode)
        {
            string query = "SELECT CODE,NAME FROM ITEM_MAST WHERE COMP_CODE = '" + cCode + "' AND ACTIVE=1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpPost]
        public async Task<IActionResult> SaveDeliveryChallanMemo([FromBody] DeliveryChallanMemoWrapper data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string action = "INSERTANDUPDATE";
            string subAction = IsDuplicateDeliveryChallanMemoEntry(data.Header.V_NO, Convert.ToInt32(globalVar.PubCompCode), Convert.ToInt32(globalVar.PubFYearCode))
                ? "UPDATE" : "INSERT";
            string docId = data.Header.V_TYPE + data.Header.V_NO;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_DELIVERY_CHALLAN_MEMO_MGMT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        //  Basic Header Parameters
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SubAction", subAction);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", data.Header.BRANCH_CODE);
                        cmd.Parameters.AddWithValue("@V_TYPE", data.Header.V_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@V_NO", data.Header.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", data.Header.V_DATE);
                        cmd.Parameters.AddWithValue("@EMP_CODE", data.Header.EMP_CODE);
                        cmd.Parameters.AddWithValue("@VENDOR_CODE", data.Header.VENDOR_CODE);
                        cmd.Parameters.AddWithValue("@TRANSPORT_NAME", data.Header.TRANSPORT_NAME ?? "");
                        cmd.Parameters.AddWithValue("@THROUGH", data.Header.THROUGH ?? "");
                        cmd.Parameters.AddWithValue("@RETURN_DATE", data.Header.RETURN_DATE);
                        cmd.Parameters.AddWithValue("@REMARKS", data.Header.REMARKS ?? "");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@STATUS", data.Header.STATUS);

                        // System Audit Fields
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", data.Header.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        //  Convert Items to TVP
                        DataTable dtItems = ConvertToGateMemo2TVP(data.Items, docId,  globalVar);
                        var tvpParam = cmd.Parameters.AddWithValue("@TVP_GateMemo2", dtItems);
                        tvpParam.SqlDbType = SqlDbType.Structured;
                        tvpParam.TypeName = "dbo.TVP_GateMemo2";

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true });
            }
            catch (SqlException sqlEx)
            {
                return Json(new { success = false, message = "SQL Error: " + sqlEx.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private DataTable ConvertToGateMemo2TVP(List<GATE_MEMO2> items, string docId, UserSessionData globalVar)
        {
             
            DataTable dt = new DataTable("TVP_GateMemo2");
            dt.Columns.Add("COMP_CODE", typeof(int));
            dt.Columns.Add("BRANCH_CODE", typeof(int));
            dt.Columns.Add("YEAR_CODE", typeof(int));
            dt.Columns.Add("DOC_ID", typeof(string));
            dt.Columns.Add("SRNO", typeof(int));
            dt.Columns.Add("ITEM_CODE", typeof(int));   
            dt.Columns.Add("QUANTITY", typeof(decimal));
            dt.Columns.Add("APPROX_AMOUNT", typeof(decimal));

            foreach (var item in items)
            {
                dt.Rows.Add(
                    globalVar.PubCompCode,
                    1,
                    globalVar.PubFYearCode,
                    docId,
                    item.SRNO,
                    item.ITEM_CODE,
                    item.QTY,
                    item.APPROX_AMT
                );
            }
            return dt;
        }

        private bool IsDuplicateDeliveryChallanMemoEntry(int? vno, int? cCode, int? yCode)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM QUOTATION1 WHERE V_NO = @vno AND COMP_CODE = @cCode AND YEAR_CODE = @yCode", con))
                {
                    cmd.Parameters.AddWithValue("@vno", vno);
                    cmd.Parameters.AddWithValue("@cCode", cCode);
                    cmd.Parameters.AddWithValue("@yCode", yCode);

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

    }
}
 