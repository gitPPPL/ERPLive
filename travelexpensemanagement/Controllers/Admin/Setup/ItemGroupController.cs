using System.Data;
using System.Net.Sockets;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class ItemGroupController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;

        public ItemGroupController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     DropdownService dropdownService, DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            //return View();
            return View("~/Views/Admin/Setup/ItemGroup/Index.cshtml");
        }
        public IActionResult GetGroupTypeList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var types = new List<string>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT GROUP_TYPE FROM ITEM_GROUP WHERE COMP_CODE='"+ compCode + "' AND ACTIVE=1 ORDER BY GROUP_TYPE ASC", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["GROUP_TYPE"].ToString());
                        }
                    }
                }
            }

            return Json(types);
        }

        public IActionResult GetSaleGroupList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            var types = new List<string>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT SALE_GROUP FROM ITEM_GROUP WHERE COMP_CODE='"+ compCode + "' AND ACTIVE=1  ORDER BY SALE_GROUP ASC", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["SALE_GROUP"].ToString());
                        }
                    }
                }
            }

            return Json(types);
        }

        [HttpGet]
        public JsonResult GetMainGrpDdl()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM ITEM_MGROUP WHERE COMP_CODE='"+ compCode + "' AND ACTIVE=1 AND NAME <>'' ORDER BY CODE ASC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public JsonResult GetActNameDdl()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM SUBGROUP_MAST WHERE COMP_CODE='"+ compCode + "' ORDER BY CODE ASC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }


        [HttpPost]
        public IActionResult SaveItemGroup([FromBody] ITEM_GROUP model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            // Check for duplicate name before insert
            if (action == "INSERT" && IsDuplicateItemGroupName(model.NAME))
            {
                return Json(new { success = false, message = "Item Group name exists." });
            }

            var result = SaveOrUpdateItemGroup(model, action);

            TempData["Message"] = result;
            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }
        [HttpPost]
        public string SaveOrUpdateItemGroup(ITEM_GROUP group, string action)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ItemGroup", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var globalVar = _globalVariableService.GetGlobalVariables();

                        cmd.Parameters.Add("@Action", SqlDbType.NVarChar).Value = action;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalVar.PubCompCode;
                        cmd.Parameters.Add("@CODE", SqlDbType.Int).Value = group.CODE;
                        cmd.Parameters.Add("@NAME", SqlDbType.NVarChar, 100).Value = group.NAME ?? "";
                        cmd.Parameters.Add("@SHORTNAME", SqlDbType.NVarChar, 30).Value = group.SHORTNAME ?? "";
                        cmd.Parameters.Add("@PRINT_NAME", SqlDbType.NVarChar, 100).Value = group.PRINT_NAME ?? "";
                        cmd.Parameters.Add("@MGROUP_CODE", SqlDbType.Int).Value = (object?)group.MGROUP_CODE ?? DBNull.Value;
                        cmd.Parameters.Add("@GROUP_TYPE", SqlDbType.NVarChar, 50).Value = group.GROUP_TYPE ?? "";
                        cmd.Parameters.Add("@ACT_CODE", SqlDbType.Int).Value = (object?)group.Accounting_Name ?? DBNull.Value;
                        cmd.Parameters.Add("@SAUDA_REQ", SqlDbType.NVarChar, 5).Value = group.Sauda_Required ?? "";
                        cmd.Parameters.Add("@SALE_GROUP", SqlDbType.NVarChar, 50).Value = group.SALE_GROUP ?? "";

                        // Audit fields
                        cmd.Parameters.Add("@ACTIVE", SqlDbType.Int).Value = group.ACTIVE; // Should be int (0 or 1)
                        cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                        cmd.Parameters.Add("@EUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                        cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = "A";
                        cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = globalVar.PubWorkStationID ?? "";
                        cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = globalVar.PubLocalId;
                        cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value = Environment.MachineName ?? "";

                        con.Open();
                        cmd.ExecuteNonQuery();

                        return "Success";

                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return $"SQL Error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [HttpPost]
        public JsonResult DeleteItemGroup(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ItemGroup", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting this record.", error = ex.Message });
            }
        }

        private bool IsDuplicateItemGroupName(string branchName)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM ITEM_GROUP WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", branchName ?? "");

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

    }
}
