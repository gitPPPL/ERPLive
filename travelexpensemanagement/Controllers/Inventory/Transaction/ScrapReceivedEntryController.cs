using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    [SessionAuthorize]
    public class ScrapReceivedEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public ScrapReceivedEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
            ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/Inventory/Transaction/ScrapReceivedEntry/Index.cshtml");
        }

        public JsonResult GetVNo(string Vtype, string Tablename = "SCRAP1")
        {
            string newV_NO = "00000";
            try
            {
                newV_NO = _globalValidationdate.GetVNo(Vtype, Tablename);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }

        public JsonResult DDlVType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from DOCTYPE_MAST  where doctype   IN ('SCRAP')     order by  name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlHDept()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = " SELECT CODE,NAME FROM ITEMDEPT_MAST where comp_code=" + getdata.PubCompCode + " and TRAN_TYPE='Store'  ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlPlace()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = " SELECT CODE,NAME FROM PLACE_MAST where comp_code=" + getdata.PubCompCode  +"   ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlItemName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEM_MAST where comp_code=" + getdata.PubCompCode + " and active=1 order by name  ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDLItemDapt()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEMDEPT_MAST where comp_code=" + getdata.PubCompCode + " and active=1  order by name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDLScrapName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select a.code , a.name from ITEM_MAST a left join Item_group b on a.group_code=b.code and a.comp_code=b.comp_code where b.sale_group='Scrap' and a.COMP_CODE=" + getdata.PubCompCode + " and a.ACTIVE = 1 ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDLUnitName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE ,NAME FROM ITEMUNIT_MAST WHERE COMP_CODE=" + getdata.PubCompCode + "  order by name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        [HttpGet]
        public JsonResult GetPendingData(DateTime v_date)   
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var result = new List<Dictionary<string, object>>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_ScrapReceived", con)) 
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "PendingList");
                cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = v_date;
                cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                cmd.Parameters.AddWithValue("@WSID", getdata.PubWorkStationID);
                cmd.Parameters.AddWithValue("@UUSER", getdata.PubUserId);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new Dictionary<string, object>
                        {
                            ["ITEM_CODE"] = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ITEM_CODE"]),
                            ["ItemName"] = reader["ItemName"] == DBNull.Value ? "" : reader["ItemName"].ToString(),
                            ["Unit_Name"] = reader["Unit_Name"] == DBNull.Value ? "" : reader["Unit_Name"].ToString(),
                            ["open_qty"] = reader["open_qty"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["open_qty"]),
                            ["dept_name"] = reader["dept_name"] == DBNull.Value ? "" : reader["dept_name"].ToString(),
                            ["TO_DEPT"] = reader["TO_DEPT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TO_DEPT"]),
                            ["scrapname"] = reader["scrapname"] == DBNull.Value ? "" : reader["scrapname"].ToString(),
                            ["PARTY_CODE"] = reader["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARTY_CODE"]),
            
                        });
                    }
                }
            }
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("SCRAP1", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpPost]
        public async Task<JsonResult> SavedData([FromBody] ScrapReceivedEntry_Model request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, status = "Error", message = "Input model is null" });
            }

            var action = string.Equals(request.Header.action, "INSERT", StringComparison.OrdinalIgnoreCase) ? "INSERT" : "UPDATE";
                   

            var result = await SubmitRequest(request.Header, request.Details, action);

            return Json(new { success = result.Status == "Success", status = result.Status, message = result.Message });
        }

        private async Task<(string Status, string Message)> SubmitRequest(ScrapReceivedEntry_Header header, List<ScrapReceivedEntry_Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
 

                await conn.OpenAsync();

                if (action == "INSERT")
                {
                    var jsonResult = GetVNo(header.V_TYPE) as JsonResult;

                    dynamic data = jsonResult?.Value;

                    if (data == null || data.V_NO == null)
                    {
                        return ("Error", "Unable to generate voucher number.");
                    }

                    header.V_NO = Convert.ToInt32(data.V_NO);
                }            


                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;

                using (var cmd = new SqlCommand("sp_ScrapReceived", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                    cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                    cmd.Parameters.AddWithValue("@PLACE_CODE", header.PLACE_CODE);
                    cmd.Parameters.AddWithValue("@PARTY", header.PARTY);
                    cmd.Parameters.AddWithValue("@remark", header.REMARK);
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);           
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        using var cmd = new SqlCommand("sp_ScrapReceived", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "Details");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                        cmd.Parameters.AddWithValue("@ITEM_CODE", detail.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@QTY", detail.QTY);
                        cmd.Parameters.AddWithValue("@WEIGHT", detail.WEIGHT);
                        cmd.Parameters.AddWithValue("@remark", detail.REMARK);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", detail.DEPT_CODE);
                        cmd.Parameters.AddWithValue("@SCRAP_NAME", detail.SCRAP_NAME);
                        cmd.Parameters.AddWithValue("@SCRAP_CODE", detail.SCRAP_CODE);                 
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return ("Success", "Data Save Successfully");
            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

        public JsonResult DailyReport(DateTime From_DATE, DateTime To_DATE ,int itemcode, int DEPT_CODE,int UnitCode )
        {
            try
            {
                var GlobalData = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand("sp_ScrapReceived", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "DailyReport");
                cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = From_DATE;
                cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = To_DATE;
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemcode);
                cmd.Parameters.AddWithValue("@DEPT_CODE", DEPT_CODE);
                cmd.Parameters.AddWithValue("@UnitCode", UnitCode);
                cmd.Parameters.AddWithValue("@WSID", GlobalData.PubWorkStationID);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Json(new { success = true, message = "View created successfully" });
            }
            catch (Exception error)
            {
                Console.WriteLine(error);

                return Json(new { success = false, message = error.Message });
            }
        }

        public JsonResult PendingDept(DateTime From_DATE, DateTime To_DATE, int itemcode, int DEPT_CODE, int UnitCode)
        {
            try
            {
                var GlobalData = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand("sp_ScrapReceived", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "PendingDept");
                cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = From_DATE;
                cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = To_DATE;
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemcode);
                cmd.Parameters.AddWithValue("@DEPT_CODE", DEPT_CODE);
                cmd.Parameters.AddWithValue("@UnitCode", UnitCode);
                cmd.Parameters.AddWithValue("@WSID", GlobalData.PubWorkStationID);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Json(new { success = true, message = "View created successfully" });
            }
            catch (Exception error)
            {
                Console.WriteLine(error);

                return Json(new { success = false, message = error.Message });
            }
        }

        public JsonResult ScrapStocREPORT(DateTime From_DATE, DateTime To_DATE, int itemcode, int DEPT_CODE, int UnitCode)
        {
            try
            {
                var GlobalData = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand("sp_ScrapReceived", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "PendingDept");
                cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = From_DATE;
                cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = To_DATE;
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemcode);
                cmd.Parameters.AddWithValue("@DEPT_CODE", DEPT_CODE);
                cmd.Parameters.AddWithValue("@UnitCode", UnitCode);
                cmd.Parameters.AddWithValue("@WSID", GlobalData.PubWorkStationID);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Json(new { success = true, message = "View created successfully" });
            }
            catch (Exception error)
            {
                Console.WriteLine(error);

                return Json(new { success = false, message = error.Message });
            }
        }

    }
}
