using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{

    [SessionAuthorize]
    public class InventoryOpeningEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public InventoryOpeningEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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

            return View("~/Views/Inventory/Transaction/InventoryOpeningEntry/Index.cshtml");
        }

        public JsonResult GetVNo(string Vtype, string Tablename = "ISSUE1")
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
                string query = "select code,name from DOCTYPE_MAST  where doctype='OpeningStock' order by  name ";
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

        public JsonResult DDlUnit()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select distinct unit_code,unit_name from ITEM_MAST where comp_code=" + getdata.PubCompCode + " and active=1 order by unit_name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDLItemmake()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEMMAKE_MAST where comp_code=" + getdata.PubCompCode + " and active=1  order by name ";
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
        public JsonResult GetDataByItemcode(int ItemCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var data = new object();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string sql = @" SELECT unit_code,  unit_name  FROM ITEM_MAST WHERE comp_code = @CompCode AND code = @ItemCode  AND active = 1";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@ItemCode", ItemCode);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            data = new
                            {
                                unit_code = reader["unit_code"],
                                unit_name = reader["unit_name"]
                            };
                        }
                    }
                }
            }

            return Json(new
            {
                Data = data,
                Status = true
            });
        }

        [HttpPost]
        public async Task<JsonResult> SavedData( [FromBody] InventoryOpeningEntry_Model request)
        {
            if (request?.Header == null)
            {
                return Json(new  {  success = false,  status = "Error",  message = "Input model is null" });
            }

            var action = string.Equals( request.Header.action, "INSERT", StringComparison.OrdinalIgnoreCase)  ? "INSERT" : "UPDATE";

            var result = await SubmitRequest( request.Header, request.Details,action);

            return Json(new  {  success = result.Status == "Success",   status = result.Status,  message = result.Message });
        }

        private async Task<(string Status, string Message)> SubmitRequest( InventoryOpeningEntry_Header header, List<InventoryOpeningEntry_Details> details,string action)
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
                        return ( "Error", "Unable to generate voucher number." );
                    }

                    header.V_NO = Convert.ToInt32(data.V_NO);
                }

                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;
          
                using (var cmd = new SqlCommand( "sp_InventoryOpening", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");

                    cmd.Parameters.AddWithValue(  "@YEAR_CODE",  g.PubFYearCode);
                    cmd.Parameters.AddWithValue( "@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue(  "@BRANCH_CODE",  g.PubBranchCode);
                    cmd.Parameters.AddWithValue(  "@V_TYPE",  (object?)header.V_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue( "@V_NO", header.V_NO);
                    cmd.Parameters.Add( "@V_DATE",  SqlDbType.SmallDateTime).Value = header.V_DATE;
                    cmd.Parameters.AddWithValue( "@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@REMARKS",  (object?)header.REMARKS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue( "@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue( "@UDATE",  DateTime.Now);
                    cmd.Parameters.AddWithValue( "@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue( "@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue( "@AED",  "A");
                    cmd.Parameters.AddWithValue( "@WSID",  g.PubWorkStationID);
                    cmd.Parameters.AddWithValue( "@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue(  "@LID",  Environment.MachineName);
                    await cmd.ExecuteNonQueryAsync();
                }

                // ---------------------------------------------------------
                // DETAILS
                // ---------------------------------------------------------
                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
            
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        using var cmd = new SqlCommand(  "sp_InventoryOpening",  conn);

                        cmd.CommandType =  CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue( "@Action",  action);
                        cmd.Parameters.AddWithValue(  "@SaveAction",  "Details");
                        cmd.Parameters.AddWithValue(  "@YEAR_CODE",  g.PubFYearCode);
                        cmd.Parameters.AddWithValue(  "@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue( "@BRANCH_CODE",  g.PubBranchCode);
                        cmd.Parameters.AddWithValue( "@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue(  "@V_NO",  header.V_NO);
                        cmd.Parameters.Add( "@V_DATE",  SqlDbType.SmallDateTime).Value =  header.V_DATE;
                        cmd.Parameters.AddWithValue( "@DOC_ID", docId);
                        cmd.Parameters.AddWithValue( "@SNO",  detail.SNO);
                        cmd.Parameters.AddWithValue(  "@ITEM_CODE",  detail.ITEM_CODE);
                        cmd.Parameters.AddWithValue(  "@ITEM_NAME", (object?)detail.ITEM_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@MAKE_CODE", (object?)detail.MAKE_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@UOM_CODE",  (object?)detail.UOM_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@UOM_NAME", (object?)detail.UOM_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue(  "@FROM_DEPT", (object?)detail.FROM_DEPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue(  "@TO_DEPT", (object?)detail.TO_DEPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue(  "@MAC_CODE",  (object?)detail.MAC_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@NOS",  (object?)detail.NOS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@QTY",  (object?)detail.QTY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@RATE", (object?)detail.RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@AMOUNT",   (object?)detail.AMOUNT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@LAND_AMT", (object?)detail.LAND_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue(  "@REMARKS",  (object?)detail.REMARKS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue( "@UUSER",  g.PubUserId);
                        cmd.Parameters.AddWithValue( "@UDATE",  DateTime.Now);
                        cmd.Parameters.AddWithValue(  "@EUSER",  g.PubUserId);
                        cmd.Parameters.AddWithValue( "@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue(  "@AED",  "A");
                        cmd.Parameters.AddWithValue(  "@WSID",  g.PubWorkStationID);
                        cmd.Parameters.AddWithValue(  "@LIP",  g.PubLocalId);
                        cmd.Parameters.AddWithValue(  "@LID",  Environment.MachineName);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return ( "Success",  "Data Save Successfully");
            }
            catch (Exception ex)
            {
                return ( "Error",  ex.Message);
            }
        }


    }
}
