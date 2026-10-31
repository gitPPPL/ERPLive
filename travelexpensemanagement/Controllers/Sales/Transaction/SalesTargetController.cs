using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.Sale.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesTargetController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public SalesTargetController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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

        public async Task<IActionResult> Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            var loadGeneralSetting = await _globalVariableService.LoadGeneralSetting();
            ViewBag.LoadGeneralSetting = loadGeneralSetting;
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            return View("~/Views/Sales/Transaction/SalesTarget/Index.cshtml");
        }

        public JsonResult GetVNo(string Vtype, string Tablename = "SALES_TARGET")
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
                string query = "Select Code,Name from DOCTYPE_MAST where code='STGT' order by Name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbPartyName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"Select CODE,NAME From Subgroup_Mast where Comp_Code={getdata.PubCompCode} and nature='Customer' and active=1 order by Name";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbProductName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@" select  a.code, a.Shortname  from ITEM_MAST a 
                Left join Item_Mgroup b on a.Mgroup_code=b.code and a.Comp_code=b.comp_code where isnull(a.Shortname,'')<>''
                and a.COMP_CODE = {getdata.PubCompCode} order by a.Shortname ; ";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);

            }
        }
        public JsonResult cmbSALESThough()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@" select CODE,NAME from SALESEXECUTIVE_MAST where ACTIVE = 1  order by NAME ; ";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);

            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCopyData(DateTime V_date)
        {
            var globalData = _globalVariableService.GetGlobalVariables();

            var partyList = new List<object>();

            await using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await using (SqlCommand cmd = new SqlCommand("sp_SalesTarget", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;
                    cmd.Parameters.Add("@Action", SqlDbType.VarChar, 50).Value = "copyData";
                    cmd.Parameters.Add("@V_DATE", SqlDbType.VarChar, 50).Value = V_date;
                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.VarChar, 50).Value = globalData.PubCompCode;
                    cmd.Parameters.Add("@Branch_code", SqlDbType.VarChar, 50).Value = globalData.PubBranchCode;

                    await con.OpenAsync();
                    await using (SqlDataReader reader =
                        await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        int itemCodeOrdinal = reader.GetOrdinal("ITEM_CODE");
                        int qtyOrdinal = reader.GetOrdinal("QTY");

                        while (await reader.ReadAsync())
                        {
                            partyList.Add(new
                            {
                                iteM_CODE = reader.IsDBNull(itemCodeOrdinal) ? (object)null! : reader.GetValue(itemCodeOrdinal),
                                qty = reader.IsDBNull(qtyOrdinal) ? 0 : Convert.ToDecimal(reader.GetValue(qtyOrdinal))
                            });
                        }
                    }
                }
            }

            return Json(partyList);
        }

        [HttpPost]
        public async Task<JsonResult> SavedData([FromBody] SalesTargetmodel request)
        {
            if (request == null || request.Details == null || !request.Details.Any())
            {
                return Json(new
                {
                    success = false,
                    status = "Error",
                    message = "No sales target details received."
                });
            }

            var result = await SubmitRequest(request.Details);

            return Json(new  { success = result.Status == "Success", status = result.Status, message = result.Message  });
        }
        public async Task<(string Status, string Message)> SubmitRequest(List<SalesTargetmodel_Detail> details)
        {
            try
            {

                var GlobalData = _globalVariableService.GetGlobalVariables();
                var GeneralSetting = await _globalVariableService.LoadGeneralSetting();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                DataTable dt = new DataTable();
                var header = details[0];



                string query = @" DELETE FROM SALES_TARGET WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND DOC_ID = @DOC_ID";
                using var deleteCmd = new SqlCommand(query, conn);
                deleteCmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                deleteCmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                deleteCmd.Parameters.AddWithValue("@DOC_ID", header.V_TYPE + header.V_NO);
                int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                
                
                string sql = @"  SELECT 1 FROM Sales_Target  WHERE V_NO <> @V_NO AND PARTY_CODE = @PARTY_CODE AND 
                FORMAT(PLAN_MONTH, 'yyyyMM') = FORMAT(@PLAN_MONTH, 'yyyyMM')
                AND COMP_CODE = @COMP_CODE";
                using var cmdCheck = new SqlCommand(sql, conn);
                cmdCheck.Parameters.AddWithValue("@V_NO", header.V_NO);
                cmdCheck.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                cmdCheck.Parameters.AddWithValue("@PLAN_MONTH", header.PLAN_MONTH);
                cmdCheck.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                var result = await cmdCheck.ExecuteScalarAsync();
                if (result != null)
                {
                    return ("Validation", "Sales Target already exists for this Party and Planning Month.");
                }
                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        using var cmd = new SqlCommand("sp_SalesTarget", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@Action", "INSERT");
                        cmd.Parameters.AddWithValue("@SaveAction", "Details");
                        cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", detail.V_TYPE);
                        cmd.Parameters.AddWithValue("@V_NO", detail.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", detail.V_DATE);
                        cmd.Parameters.AddWithValue("@DOC_ID", detail.V_TYPE + detail.V_NO );
                        cmd.Parameters.AddWithValue("@PLAN_MONTH", detail.PLAN_MONTH);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", detail.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", detail.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@SOLD_QTY", detail.SOLD_QTY);
                        cmd.Parameters.AddWithValue("@TARGET_QTY", detail.TARGET_QTY);
                        cmd.Parameters.AddWithValue("@MIN_PRICE", detail.MIN_PRICE);
                        cmd.Parameters.AddWithValue("@MAX_PRICE", detail.MAX_PRICE);
                        cmd.Parameters.AddWithValue("@APP_PRICE", detail.APP_PRICE);
                        cmd.Parameters.AddWithValue("@VIR_PRICE", detail.VIR_PRICE);
                        cmd.Parameters.AddWithValue("@REMARKS", detail.VIR_PRICE);
                        cmd.Parameters.AddWithValue("@salesThrough", detail.salesThrough);                        
                        cmd.Parameters.AddWithValue("@UUSER", GlobalData.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", GlobalData.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", GlobalData.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", GlobalData.PubLocalId);
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

    }

}
