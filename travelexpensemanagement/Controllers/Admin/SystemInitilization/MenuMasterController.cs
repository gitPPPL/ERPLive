using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.Admin.SystemInitilization;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Admin.Master
{
    [SessionAuthorize]
    public class MenuMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public MenuMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Menu Structure";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/SystemInitilization/MenuMaster/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult MenuMasterAddOrEditForm()
        {
            //return View();
            return View("~/Views/Admin/SystemInitilization/MenuMaster/MenuMasterAddOrEditForm.cshtml");
        }

        [HttpGet]
        public JsonResult GetModuleMasterDdl()
        {
            string query = "SELECT CODE,DISPLAY_NAME FROM MODULE_MAST ORDER BY CODE ASC";
            var moduelList = _dropdownService.GetDropdownListERP(query);
            return Json(moduelList);
        }

        //Replace Parent module code with name on change of Module
        [HttpGet]
        public JsonResult GetParentModuleDdl(int ModuleCode)
        {
            string query = "SELECT CODE,DISPLAY_NAME NAME from MENU_MAST where MODULE_CODE='" + ModuleCode + "' and ACTIVE=1 Order by NAME";
            var parnetModuleList = _dropdownService.GetDropdownListERP(query);
            return Json(parnetModuleList);
        }
        //To Replace the code with name while show the data on grid.
        [HttpGet]
        public JsonResult GetParentFullListDdl(int ModuleCode)
        {
            string query = "SELECT CODE,DISPLAY_NAME NAME from MENU_MAST where ACTIVE=1 Order by NAME";
            var parnetModuleList = _dropdownService.GetDropdownListERP(query);
            return Json(parnetModuleList);
        }

        [HttpGet]
        public IActionResult GetAllMenus(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var menuList = new List<MENU_MAST>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_MenuMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            menuList.Add(new MENU_MAST
                            {
                                CODE = Convert.ToInt32(reader["CODE"]),
                                MODULE_CODE = reader["MODULE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MODULE_CODE"]) : (int?)null,
                                MAINMENU_CODE = reader["MAINMENU_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MAINMENU_CODE"]) : (int?)null,
                                MENU_OPTION = reader["MENU_OPTION"] != DBNull.Value ? Convert.ToInt32(reader["MENU_OPTION"]) : (int?)null,
                                NAME = reader["NAME"] as string,
                                DISPLAY_NAME = reader["DISPLAY_NAME"] as string,
                                WebFORM_NAME = reader["WebFORM_NAME"] as string,
                                TAG_NAME = reader["TAG_NAME"] as string,
                                MENU_TYPE = reader["MENU_TYPE"] as string,
                                SECURITY_TYPE = reader["SECURITY_TYPE"] != DBNull.Value ? Convert.ToInt32(reader["SECURITY_TYPE"]) : (int?)null,
                                APPROVAL = reader["APPROVAL"] as string,
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : (int?)null,
                            });
                        }

                        // Read second result set (total count)
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = Convert.ToInt32(reader["TotalCount"]);
                        }
                    }
                }
            }

            return Json(new { menus = menuList, totalCount });
        }


        [HttpPost]
        public IActionResult SaveMenu([FromBody] MENU_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            var result = SaveOrUpdateMenu(model, action);

            TempData["Message"] = result;
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var menu = GetMenuByCode(id);
            return View(menu);
        }

        public string SaveOrUpdateMenu(MENU_MAST menu, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_MenuMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Common Parameters
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@CODE", menu.CODE);
                    cmd.Parameters.AddWithValue("@MODULE_CODE", menu.MODULE_CODE);
                    cmd.Parameters.AddWithValue("@MAINMENU_CODE", menu.MAINMENU_CODE);
                    cmd.Parameters.AddWithValue("@MENU_OPTION", menu.MENU_OPTION);
                    cmd.Parameters.AddWithValue("@NAME", menu.NAME);
                    cmd.Parameters.AddWithValue("@DISPLAY_NAME", menu.DISPLAY_NAME);
                    cmd.Parameters.AddWithValue("@FORM_NAME", menu.WebFORM_NAME);
                    cmd.Parameters.AddWithValue("@TAG_NAME", menu.TAG_NAME);
                    cmd.Parameters.AddWithValue("@MENU_TYPE", menu.MENU_TYPE);
                    cmd.Parameters.AddWithValue("@SECURITY_TYPE", menu.SECURITY_TYPE);
                    cmd.Parameters.AddWithValue("@APPROVAL", menu.APPROVAL);
                    cmd.Parameters.AddWithValue("@ACTIVE", menu.ACTIVE);
                    cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", menu.AED ?? "A");
                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");
                    cmd.Parameters.AddWithValue("@LOCK_EDIT", menu.LOCK_EDIT);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    return "Success";
                }
            }
        }

        public MENU_MAST GetMenuByCode(int code)
        {
            MENU_MAST menu = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_MenuMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@CODE", code);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            menu = new MENU_MAST
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                MODULE_CODE = rdr["MODULE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MODULE_CODE"]) : 0,
                                MAINMENU_CODE = rdr["MAINMENU_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MAINMENU_CODE"]) : 0,
                                MENU_OPTION = rdr["MENU_OPTION"] != DBNull.Value ? Convert.ToInt32(rdr["MENU_OPTION"]) : 0,
                                NAME = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : string.Empty,
                                DISPLAY_NAME = rdr["DISPLAY_NAME"] != DBNull.Value ? rdr["DISPLAY_NAME"].ToString() : string.Empty,
                                WebFORM_NAME = rdr["WebFORM_NAME"] != DBNull.Value ? rdr["WebFORM_NAME"].ToString() : string.Empty,
                                TAG_NAME = rdr["TAG_NAME"] != DBNull.Value ? rdr["TAG_NAME"].ToString() : string.Empty,
                                MENU_TYPE = rdr["MENU_TYPE"] != DBNull.Value ? rdr["MENU_TYPE"].ToString() : string.Empty,
                                SECURITY_TYPE = rdr["SECURITY_TYPE"] != DBNull.Value ? Convert.ToInt32(rdr["SECURITY_TYPE"]) : 0,
                                APPROVAL = rdr["APPROVAL"] != DBNull.Value ? rdr["APPROVAL"].ToString() : "No",
                                ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,

                                // Add additional fields with the same null-safe pattern
                            };
                        }
                    }
                }
            }

            return menu;
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<MENU_MASTEport>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_MenuMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Export");
                    //cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new MENU_MASTEport
                            {
                                CODE = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : (int?)null,
                                MENU_OPTION = reader["MENU_OPTION"]?.ToString(),
                                NAME = reader["Name"]?.ToString(),
                                DISPLAY_NAME = reader["DISPLAY_NAME"]?.ToString(),
                                FORM_NAME = reader["FORM_NAME"]?.ToString(),
                                MENU_TYPE = reader["MENU_TYPE"]?.ToString(),
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
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_MenuMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
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
