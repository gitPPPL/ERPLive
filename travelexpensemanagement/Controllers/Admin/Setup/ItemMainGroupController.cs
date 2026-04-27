using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class ItemMainGroupController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;

        public ItemMainGroupController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/Setup/ItemMainGroup/Index.cshtml");
        }
        public IActionResult ItemMainGroupForm()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetMainGroupTypesList()
        {
            var types = new List<string>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT MGROUP_TYPE FROM ITEM_MGROUP ORDER BY MGROUP_TYPE", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["MGROUP_TYPE"].ToString());
                        }
                    }
                }
            }
            return Json(types);
        }
        [HttpGet]
        public IActionResult GetPlanningMethodList()
        {
            var types = new List<string>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT PLANNING_METHOD FROM ITEM_MGROUP ORDER BY PLANNING_METHOD ASC\r\n", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["PLANNING_METHOD"].ToString());
                        }
                    }
                }
            }

            return Json(types);
        }

        [HttpGet]
        public IActionResult GetProcedurmentMethodList()
        {
            var types = new List<string>();
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT PROCUREMENT_METHOD FROM ITEM_MGROUP WHERE PROCUREMENT_METHOD <>''ORDER BY PROCUREMENT_METHOD ASC", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["PROCUREMENT_METHOD"].ToString());
                        }
                    }
                }
            }

            return Json(types);
        }

        [HttpGet]
        public IActionResult GetValuationMethodList()
        {
            var types = new List<string>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT VALUATION_METHOD FROM ITEM_MGROUP ORDER BY VALUATION_METHOD ASC", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["VALUATION_METHOD"].ToString());
                        }
                    }
                }
            }

            return Json(types);
        }

        [HttpGet]
        public ITEM_MGROUP GetItemMasterGroupByCode(int code)
        {
            ITEM_MGROUP itemGroup = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ItemMGroup", con)) // Adjust stored procedure name as needed
                {
                    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            itemGroup = new ITEM_MGROUP
                            {
                                CODE = Convert.ToInt32(rdr["CODE"]),
                                NAME = rdr["Name"].ToString(),
                                SHORTNAME = rdr["SHORTNAME"].ToString(),
                                MGROUP_TYPE = rdr["MGROUP_TYPE"].ToString(),
                                PLANNING_METHOD = rdr["PLANNING_METHOD"].ToString(),
                                PROCUREMENT_METHOD = rdr["PROCUREMENT_METHOD"].ToString(),
                                VALUATION_METHOD = rdr["VALUATION_METHOD"].ToString(),
                                ACTIVE = Convert.ToInt32(rdr["ACTIVE"]),
                            };
                        }
                    }
                }
            }

            return itemGroup;
        }


        [HttpPost]
        public IActionResult SaveItemMainGroup([FromBody] ITEM_MGROUP model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            // Check for duplicate name before insert
            if (action == "INSERT" && IsDuplicateItemMainGroupName(model.NAME))
            {
                return Json(new { success = false, message = "Item Main Group name already exists." });
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
        public string SaveOrUpdateItemGroup(ITEM_MGROUP group, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();


            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ItemMGroup", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Core parameters
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@COMP_CODE", group.COMP_CODE);
                    cmd.Parameters.AddWithValue("@CODE", group.CODE);
                    cmd.Parameters.AddWithValue("@NAME", group.NAME ?? "");
                    cmd.Parameters.AddWithValue("@SHORTNAME", group.SHORTNAME ?? "");
                    cmd.Parameters.AddWithValue("@MAIN_TYPE", group.MAIN_TYPE ?? "");
                    cmd.Parameters.AddWithValue("@MGROUP_TYPE", group.MGROUP_TYPE ?? "");
                    cmd.Parameters.AddWithValue("@REPORT_TYPE", group.REPORT_TYPE ?? "");
                    cmd.Parameters.AddWithValue("@PLANNING_METHOD", group.PLANNING_METHOD ?? "");
                    cmd.Parameters.AddWithValue("@PROCUREMENT_METHOD", group.PROCUREMENT_METHOD ?? "");
                    cmd.Parameters.AddWithValue("@VALUATION_METHOD", group.VALUATION_METHOD ?? "");
                    cmd.Parameters.AddWithValue("@ACTIVE", group.ACTIVE);
                    cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", group.AED ?? "A");
                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "");
                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "");
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "");

                    con.Open();
                    cmd.ExecuteNonQuery();

                    return "Success";
                }
            }
        }

        [HttpPost]
        public JsonResult DeleteItemMainGroup(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ItemMGroup", con))
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


        private bool IsDuplicateItemMainGroupName(string MainGroupName)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM ITEM_MGROUP WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", MainGroupName ?? "");

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }


    }
}
