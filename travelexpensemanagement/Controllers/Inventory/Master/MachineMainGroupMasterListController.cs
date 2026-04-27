using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Master;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    [SessionAuthorize]
    public class MachineMainGroupMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        private int? userLevel;
        public MachineMainGroupMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Machine Main Group Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Inventory/Master/MachineMainGroupMasterList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetMachineMainGroups(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var machineGroups = new List<MACHINE_GRP_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_GRP_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value); // fetch all if no specific CODE

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                machineGroups.Add(new MACHINE_GRP_MAST
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, lists = machineGroups, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching machine groups", error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetMachineGroupByCode(int code)
        {
            MACHINE_GRP_MAST group = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;


            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_GRP_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@CODE", code);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                group = new MACHINE_GRP_MAST
                                {
                                    COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    NAME = rdr["NAME"]?.ToString(),
                                    SHORTNAME = rdr["SHORTNAME"]?.ToString(),
                                    COST_DEPT = rdr["COST_DEPT"]?.ToString(),
                                    COST_FL = rdr["COST_FL"] != DBNull.Value ? Convert.ToInt32(rdr["COST_FL"]) : 0,
                                    COST_WL = rdr["COST_WL"] != DBNull.Value ? Convert.ToInt32(rdr["COST_WL"]) : 0,
                                    ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
                                    UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                                    UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                                    EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                                    EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                                    AED = rdr["AED"]?.ToString(),
                                    WSID = rdr["WSID"]?.ToString(),
                                    LIP = rdr["LIP"]?.ToString(),
                                    LID = rdr["LID"]?.ToString()
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = group });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching machine group", error = ex.Message });
            }
        }


    }
}
