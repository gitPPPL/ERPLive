using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    public class ItemMainGroupListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public ItemMainGroupListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService; 
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Item Main Group Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/ItemMainGroupList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllItemGroups(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var groupList = new List<ITEM_MGROUP>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection()) // Ensure this returns a valid SqlConnection
            {
                using (SqlCommand cmd = new SqlCommand("sp_ItemMGroup", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", 1); 
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    // Add all other required parameters as NULL
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@NAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHORTNAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@MAIN_TYPE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@MGROUP_TYPE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@REPORT_TYPE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PLANNING_METHOD", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PROCUREMENT_METHOD", DBNull.Value);
                    cmd.Parameters.AddWithValue("@VALUATION_METHOD", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            groupList.Add(new ITEM_MGROUP
                            {
                                COMP_CODE = Convert.ToInt32(reader["COMP_CODE"]),
                                CODE = Convert.ToInt32(reader["CODE"]),
                                NAME = reader["NAME"]?.ToString(),
                                SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                MAIN_TYPE = reader["MAIN_TYPE"]?.ToString(),
                                MGROUP_TYPE = reader["MGROUP_TYPE"]?.ToString(),
                                REPORT_TYPE = reader["REPORT_TYPE"]?.ToString(),
                                PLANNING_METHOD = reader["PLANNING_METHOD"]?.ToString(),
                                PROCUREMENT_METHOD = reader["PROCUREMENT_METHOD"]?.ToString(),
                                VALUATION_METHOD = reader["VALUATION_METHOD"]?.ToString(),
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                // You can map more fields if needed
                            });
                        }

                        // Read total count from the second result set
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = Convert.ToInt32(reader["TotalCount"]);
                        }
                    }
                }
            }

            return Json(new { groups = groupList, totalCount });
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<ITEM_MGROUPListExport>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ItemMGroup", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Export");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new ITEM_MGROUPListExport
                            {
                                CODE = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : (int?)null,
                                NAME = reader["Name"]?.ToString(),
                                SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                GROUP_TYPE = reader["MAIN_TYPE"]?.ToString(),
                                PRINT_NAME = reader["PLANNING_METHOD"]?.ToString(),
                                SALE_GROUP = reader["PROCUREMENT_METHOD"]?.ToString(),
                                Sauda_Required = reader["VALUATION_METHOD"]?.ToString(),
                                ACTIVE = reader["ACTIVE"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(docList);
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<ItemMGroupDetailDto> docDetails = new List<ItemMGroupDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ItemMGroup", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new ItemMGroupDetailDto
                            {
                                Code = reader["Code"]?.ToString(),
                                UUser = reader["UUser"]?.ToString(),
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            };
                            docDetails.Add(detail);
                        }
                    }
                }
            }

            return Json(new { success = true, data = docDetails });
        }

    }
}
