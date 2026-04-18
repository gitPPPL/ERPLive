using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;  // Use Microsoft.Data.SqlClient instead of System.Data.SqlClient
using System;
using System.Data;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using TravelExpenseManagement.Models.Admin.Utilities;
namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class GramSizeConversionMasterController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public GramSizeConversionMasterController(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService,
            travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/Admin/Utilities/GramSizeConversionMaster/Index.cshtml");
        }

        public JsonResult DDLItemType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select Code,Name from ITEMCAT_MAST where COMP_CODE=" + getdata.PubCompCode + " order by name ";

                var DDLItemType = _dropdownService.GetDropdownList(query);

                return Json(DDLItemType);
            }

        }

        [HttpPost]
        public IActionResult SaveTempMaster([FromBody] SaveTempMasterRequest request)
        {
            if (request == null || request.tableData == null || request.tableData.Count == 0)
            {
                return Json(new { success = false, message = "No valid rows provided." });
            }

            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    if (request.CODE == null || request.CODE == 0)
                    {
                       
                        string sql = @"
                        SELECT 1 
                        FROM PAY_GRAMSIZECONV 
                        WHERE ITEM_TYPE = @ITEM_TYPE 
                        AND COMP_CODE = @COMP_CODE
                        AND CODE <> ISNULL(@CODE, 0)";

                        using (SqlCommand checkCmd = new SqlCommand(sql, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@ITEM_TYPE", request.ItemType ?? (object)DBNull.Value);
                            checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            checkCmd.Parameters.AddWithValue("@CODE", request.CODE ?? 0);

                            using (SqlDataReader reader = checkCmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    return Json(new { success = false, message = "Duplicate record found." });
                                }
                            }
                        }
                    }


                    if (request.CODE != null && request.CODE != 0)
                    {
                        string deleteQuery = @"
                            DELETE FROM PAY_GRAMSIZECONV  
                            WHERE CODE = @CODE AND ITEM_TYPE = @ITEM_TYPE AND COMP_CODE = @COMP_CODE";

                        using (var deleteCmd = new SqlCommand(deleteQuery, conn))
                        {
                            deleteCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            deleteCmd.Parameters.AddWithValue("@ITEM_TYPE", request.ItemType ?? (object)DBNull.Value);
                            deleteCmd.Parameters.AddWithValue("@CODE", request.CODE);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }


                    foreach (var item in request.tableData)
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("sp_PAY_GRAMSIZECONV", conn))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 20).Value = "INSERT";
                                cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalVar.PubCompCode;
                                cmd.Parameters.Add("@CODE", SqlDbType.Int).Value = request.CODE;
                                cmd.Parameters.Add("@CAT_CODE", SqlDbType.Int).Value = request.CAT_CODE;
                                cmd.Parameters.Add("@ITEM_TYPE", SqlDbType.NVarChar, 30).Value = (object)request.ItemType ?? DBNull.Value;
                                cmd.Parameters.Add("@FROM_SIZE", SqlDbType.Decimal).Value = item.FromSize;
                                cmd.Parameters.Add("@TO_SIZE", SqlDbType.Decimal).Value = item.ToSize;
                                cmd.Parameters.Add("@PER", SqlDbType.Decimal).Value = item.Per;
                                cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                                cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                                cmd.Parameters.Add("@EUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                                cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                                cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = "A";
                                cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = (object)globalVar.PubWorkStationID ?? DBNull.Value;
                                cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = (object)globalVar.PubLocalId ?? DBNull.Value;
                                cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value = Environment.MachineName;

                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch
                        {
                        }
                    }

                    return Json(new { success = true, message = "Data saved successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }



    }
}
