using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class BranchMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel; 

        public BranchMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Branch Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); 

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/BranchMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllBranches(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var branchList = new List<BRANCH_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BranchMast", conn)) // This should be your new SP
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        // Optional: dummy params if needed by the SP
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@LOCATION", DBNull.Value);
                        cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                        cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                        cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                        cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SRNO", DBNull.Value);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                branchList.Add(new BRANCH_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    LOCATION = reader["LOCATION"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
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
                return Json(new { error = "Error loading branches", message = ex.Message });
            }

            return Json(new { branches = branchList, totalCount });
        }

        public BRANCH_MAST GetBranchByCode(int code)
        {
            BRANCH_MAST branch = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_BranchMast", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@CODE", code);

                    // Optional: dummy params
                    cmd.Parameters.AddWithValue("@SearchTerm", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageNumber", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageSize", DBNull.Value);
                    cmd.Parameters.AddWithValue("@NAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LOCATION", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SRNO", DBNull.Value);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            branch = new BRANCH_MAST
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                NAME = rdr["NAME"]?.ToString(),
                                LOCATION = rdr["LOCATION"]?.ToString(),
                                ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0
                            };
                        }
                    }
                }
            }

            return branch;
        }


    }
}
