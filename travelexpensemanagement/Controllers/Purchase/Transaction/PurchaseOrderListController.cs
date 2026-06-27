using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseOrderListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public PurchaseOrderListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Purchase Order";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var userlevel = ViewBag.UserLevel;
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Purchase/Transaction/PurchaseOrderList/Index.cshtml", model);
        }
         
        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", UsersessionDt.PubCompCode },
                    {"@YEAR_CODE", UsersessionDt.PubFYearCode },
                    {"@BRANCH_CODE",  UsersessionDt.PubBranchCode},
                    {"@Action", "PurchaseOrderList" }
                };

                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parameter);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "DOC_ID" };
                            return searchableKeys.Any(key =>
                                dict.ContainsKey(key) &&
                                dict[key]?.ToString().ToLower().Contains(searchTerm) == true
                            );
                        })
                        .ToList();
                }

                var totalCount = fullList.Count;
                var pagedList = fullList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new { status = true, data = pagedList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePurchaseOrderEntry(string docid)
        {
            try
            {
                if (string.IsNullOrEmpty(docid))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }

                var userSession = _globalValue.GetGlobalVariables();
                string VType = docid.Substring(0, 4);
                string VNo = docid.Substring(4);

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {

                        string[] deleteQueries = {
                        "DELETE FROM ORDER1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        "DELETE FROM ORDER2 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        "DELETE FROM ORDER3 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO"
                        };
                            foreach (var query in deleteQueries)
                            {
                                using (var cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                    cmd.Parameters.AddWithValue("@V_NO", VNo);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                            return Json(new { status = true, data = "Data deleted successfully" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { status = false, message = $"Delete failed: {ex.Message}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        public JsonResult GetPurchaseOrderEntryDetails(string docid)
        {
            var globalVar = _globalValue.GetGlobalVariables();
            List<InwardEntryDetailDto> docDetails = new List<InwardEntryDetailDto>();

            using (SqlConnection conn = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseOrder", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "EntryDetail");
                    cmd.Parameters.AddWithValue("@DOC_ID", docid);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var InwardEntryDetailDto = new InwardEntryDetailDto
                            {
                                Code = reader["Doc_code"]?.ToString(),
                                UUser = reader["UUser"]?.ToString(),
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            };
                            docDetails.Add(InwardEntryDetailDto);
                        }
                    }
                }
            }

            return Json(new { success = true, data = docDetails });
        }

        public class InwardEntryDetailDto
        {
            public string? Code { get; set; }
            public string? UUser { get; set; }
            public DateTime? UDATE { get; set; }
            public string? EUSER { get; set; }
            public DateTime? EDATE { get; set; }
            public string? WSID { get; set; }
            public string? LIP { get; set; }
            public string? LID { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {

            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public JsonResult DeleteValidation(int v_no, string v_type)
        {
            var globalVaraible = _globalValue.GetGlobalVariables();

            using var con = _dbcontext.GetErpConnection();
            con.Open();

            object validation1 = null;
            object validation2 = null;
            object validation3 = null;
            object validation4 = null;

            // ===================== QUERY 1 =====================
            string query1 = @"
            SELECT   v_no, v_date  
            FROM GATE2  ";
            //WHERE REF_TYPE = @v_type  
            //AND REF_NO = @v_no 
            //AND COMP_CODE = @COMP_CODE 
            //AND BRANCH_CODE = @BRANCH_CODE 
            //AND YEAR_CODE = @YEAR_CODE";

            using (var cmd1 = new SqlCommand(query1, con))
            {
                cmd1.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                cmd1.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                cmd1.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                cmd1.Parameters.AddWithValue("@v_no", v_no);
                cmd1.Parameters.AddWithValue("@v_type", v_type);

                using var reader1 = cmd1.ExecuteReader();
                if (reader1.Read())
                {
                    int gateNo = Convert.ToInt32(reader1["v_no"]);
                    string gateDate = Convert.ToDateTime(reader1["v_date"]).ToString("dd/MM/yyyy");

                    validation1 = new
                    {
                        success = true,
                        Message = $"This document exists in Gate Serial No: {gateNo} dated: {gateDate}"
                    };
                }
            }

            // ===================== QUERY 2 =====================
            string query2 = @"
                    SELECT   v_no, v_date 
                    FROM PURCHASE2 
                    WHERE PO_TYPE = @PO_TYPE
                    AND PO_NO = @PO_NO
                    AND COMP_CODE = @COMP_CODE
                    AND BRANCH_CODE = @BRANCH_CODE
                    AND YEAR_CODE = @YEAR_CODE";

            using (var cmd2 = new SqlCommand(query2, con))
            {
                cmd2.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                cmd2.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                cmd2.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                cmd2.Parameters.AddWithValue("@PO_NO", v_no);
                cmd2.Parameters.AddWithValue("@PO_TYPE", v_type);

                using var reader2 = cmd2.ExecuteReader();
                if (reader2.Read())
                {
                    int gateNo = Convert.ToInt32(reader2["v_no"]);
                    string gateDate = Convert.ToDateTime(reader2["v_date"]).ToString("dd/MM/yyyy");

                    validation2 = new
                    {
                        success = true,
                        Message = $"This document exists in Purchase Invoice Serial No {gateNo} dated: {gateDate}"
                    };
                }
            }

            // ===================== QUERY 3 =====================
            string query3 = @"
                SELECT   v_no, v_date 
                FROM PURCHASE2 
                WHERE PO_TYPE = @PO_TYPE
                AND PO_NO = @PO_NO
                AND COMP_CODE = @COMP_CODE
                AND BRANCH_CODE = @BRANCH_CODE
                AND YEAR_CODE = @YEAR_CODE";

            using (var cmd3 = new SqlCommand(query3, con))
            {
                cmd3.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                cmd3.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                cmd3.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                cmd3.Parameters.AddWithValue("@PO_NO", v_no);
                cmd3.Parameters.AddWithValue("@PO_TYPE", v_type);

                using var reader3 = cmd3.ExecuteReader();
                if (reader3.Read())
                {
                    int gateNo = Convert.ToInt32(reader3["v_no"]);
                    string gateDate = Convert.ToDateTime(reader3["v_date"]).ToString("dd/MM/yyyy");

                    validation3 = new
                    {
                        success = true,
                        Message = $"This document exists in Purchase Receipt Serial No : {gateNo} dated: {gateDate}"
                    };
                }
            }

            // ===================== QUERY 4 =====================
            string query4 = @"
                    SELECT   status   FROM APPROVAL_STATUS 
                    WHERE v_type = @v_type  AND v_NO = @v_NO
                    AND comp_code = @COMP_CODE
                    AND branch_code = @BRANCH_CODE
                    AND year_code = @YEAR_CODE
                    AND status = 'OPEN'
                    AND USER_CODE<> @USER_CODE";

            using (var cmd4 = new SqlCommand(query4, con))
            {
                cmd4.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                cmd4.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                cmd4.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                cmd4.Parameters.AddWithValue("@USER_CODE", globalVaraible.PubUserId);
                cmd4.Parameters.AddWithValue("@v_NO", v_no);
                cmd4.Parameters.AddWithValue("@v_type", v_type);

                using var reader4 = cmd4.ExecuteReader();
                if (reader4.Read())
                {
                    validation4 = new
                    {
                        success = true,
                        Approval = Convert.ToString(reader4["status"])
                    };
                }
            }

            return Json(new  { validation1, validation2, validation3,  validation4 });
        }

        public JsonResult EditValidation(int v_no, string v_type)
        {
            var globalVaraible = _globalValue.GetGlobalVariables();

            using var con = _dbcontext.GetErpConnection();
            con.Open();

            object validation1 = null;
            object validation2 = null;
            object validation3 = null;

            string sql = "select  top 1  status from APPROVAL_STATUS " +
            " where v_type=@v_type  and v_NO=@v_no and comp_code=@COMP_CODE   " +
            "  and branch_code=@BRANCH_CODE  and year_code=@YEAR_CODE  and status='OPEN' and USER_CODE<> @USER_CODE";

            using (var cmd1 = new SqlCommand(sql, con))
            {
                cmd1.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                cmd1.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                cmd1.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                cmd1.Parameters.AddWithValue("@USER_CODE", globalVaraible.PubUserId);
                cmd1.Parameters.AddWithValue("@v_no", v_no);
                cmd1.Parameters.AddWithValue("@v_type", v_type);

                using var reader1 = cmd1.ExecuteReader();
                if (reader1.Read())
                {

                    string lastuser = GetText("select top 1 user_name from APPROVAL_STATUS " +
                    "where v_type=@v_type and v_NO= @v_no and comp_code= @COMP_CODE   " +
                    "  and branch_code= @BRANCH_CODE and year_code= @YEAR_CODE and status='OPEN' and user_code<> @USER_CODE  order by srno desc");

                    validation1 = new
                    {
                        success = true,
                        Message = $"This Document Approval is in process at User:{lastuser} "
                    };
                }
            }


            // ===================== QUERY 1 =====================
            string query1 = @"select v_no,v_date from GATE2
            where REF_TYPE = @v_type and REF_NO = @v_no
            and COMP_CODE = @COMP_CODE and BRANCH_CODE = @BRANCH_CODE and YEAR_CODE = @YEAR_CODE";

            using (var cmd1 = new SqlCommand(query1, con))
            {
                cmd1.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                cmd1.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                cmd1.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                cmd1.Parameters.AddWithValue("@v_no", v_no);
                cmd1.Parameters.AddWithValue("@v_type", v_type);

                using var reader1 = cmd1.ExecuteReader();
                if (reader1.Read())
                {
                    int gateNo = Convert.ToInt32(reader1["v_no"]);
                    string gateDate = Convert.ToDateTime(reader1["v_date"]).ToString("dd/MM/yyyy");

                    validation2 = new
                    {
                        success = true,
                        Message = $"This document exists in Gate Serial No: {gateNo} dated: {gateDate}"
                    };
                }
            }

            // ===================== QUERY 2 =====================
            string query2 = @" select top 1 v_no,v_date from ORDER2
                where SAUDA_TYPE = @SAUDA_TYPE and sauda_NO = @sauda_NO and COMP_CODE = @COMP_CODE and 
                BRANCH_CODE = @BRANCH_CODE and YEAR_CODE = @YEAR_CODE";

            using (var cmd2 = new SqlCommand(query2, con))
            {
                cmd2.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                cmd2.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                cmd2.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                cmd2.Parameters.AddWithValue("@sauda_NO", v_no);
                cmd2.Parameters.AddWithValue("@SAUDA_TYPE", v_type);

                using var reader2 = cmd2.ExecuteReader();
                if (reader2.Read())
                {
                    int gateNo = Convert.ToInt32(reader2["v_no"]);
                    string gateDate = Convert.ToDateTime(reader2["v_date"]).ToString("dd/MM/yyyy");

                    validation3 = new
                    {
                        success = true,
                        Message = $"This document exists in ORDER Serial No : {gateNo} dated: {gateDate}"
                    };
                }
            }          

            return Json(new { validation1, validation2, validation3 });
        }

        public string GetText(string query)
        {
            try
            {
                using var con = _dbcontext.GetErpConnection();
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {

                                return reader[0].ToString();

                            }
                            else
                            {

                                return string.Empty;

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetText() Error: " + ex.Message);
                return string.Empty;
            }
        }

    }
}
