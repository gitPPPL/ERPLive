using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Admin.Setup
{ 
    [SessionAuthorize]
    public class ItemGroupListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public ItemGroupListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Item Group Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/ItemGroupList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetAllItemGroups(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var groupList = new List<ITEM_GROUPList>();
            int totalCount = 0;
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ItemGroup", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        // Fill other unused params with DBNull
                        foreach (string param in new[]
                        {
                    "@CODE", "@NAME", "@SHORTNAME", "@MGROUP_CODE", "@GROUP_TYPE", "@PRINT_NAME", "@SALE_GROUP",
                    "@ACT_CODE", "@UUSER", "@UDATE", "@EUSER", "@EDATE", "@AED", "@WSID", "@LIP", "@LID",
                    "@SRNO", "@ACTIVE", "@SAUDA_REQ"
                })
                        {
                            cmd.Parameters.AddWithValue(param, DBNull.Value);
                        }

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                groupList.Add(new ITEM_GROUPList
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString() ?? string.Empty,
                                    SHORTNAME = reader["SHORTNAME"]?.ToString() ?? string.Empty,
                                    MGROUP_CODE = reader["MGROUP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MGROUP_CODE"]) : null,
                                    MGROUP_NAME = reader["MGROUP_NAME"]?.ToString(),
                                    GROUP_TYPE = reader["GROUP_TYPE"]?.ToString(),
                                    PRINT_NAME = reader["PRINT_NAME"]?.ToString(),
                                    SALE_GROUP = reader["SALE_GROUP"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : null,
                                    Sauda_Required = reader["SAUDA_REQ"]?.ToString(),
                                    ACT_CODE = reader["ACT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ACT_CODE"]) : null,
                                    ACT_NAME = reader["ACT_NAME"]?.ToString()
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = Convert.ToInt32(reader["TotalCount"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while processing your request.", message = ex.Message });
            }

            return Json(new { groups = groupList, totalCount });
        }
        public ITEM_GROUP GetItemGroupByCode(int code)
        {
            ITEM_GROUP itemGroup = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ItemGroup", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            itemGroup = new ITEM_GROUP
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                NAME = rdr["NAME"]?.ToString(),
                                SHORTNAME = rdr["SHORTNAME"]?.ToString(),
                                PRINT_NAME = rdr["PRINT_NAME"]?.ToString(),
                                MGROUP_CODE = rdr["MGROUP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MGROUP_CODE"]) : 0,
                                GROUP_TYPE = rdr["GROUP_TYPE"]?.ToString(),
                                Accounting_Name = rdr["ACT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ACT_CODE"]) : 0,
                                Sauda_Required = rdr["SAUDA_REQ"]?.ToString(),
                                SALE_GROUP = rdr["SALE_GROUP"]?.ToString(),
                                ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0
                            };
                        }
                        else
                        {
                            // Log or handle case when no rows are returned.
                            Console.WriteLine("No rows found for the given code.");
                        }
                    }
                }
            }
            return itemGroup;
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var itemGroupList = new List<ITEM_GROUPListExport>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ItemGroup", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Export");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            itemGroupList.Add(new ITEM_GROUPListExport
                            {
                                CODE = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : null,
                                NAME = reader["Name"]?.ToString(),
                                SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                PRINT_NAME = reader["PRINT_NAME"]?.ToString(),
                                MGROUP_NAME = reader["MGROUP_CODE"]?.ToString(),
                                GROUP_TYPE = reader["GROUP_TYPE"]?.ToString(),
                                ACT_NAME = reader["ACT_CODE"]?.ToString(),
                                Sauda_Required = reader["SAUDA_REQ"]?.ToString(),
                                SALE_GROUP = reader["SALE_GROUP"]?.ToString(),
                                ACTIVE = reader["ACTIVE"]?.ToString(),
                            });
                        }
                    }
                }
            }

            return Json(itemGroupList);
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ItemGroup", conn))
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
                            var detail = new ItemGroupDetailDto
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
