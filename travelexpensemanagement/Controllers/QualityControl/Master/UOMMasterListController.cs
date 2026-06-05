using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class UOMMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;

        private int? userLevel;
        public UOMMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "QCP UOM Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel,
            };
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database; // Get the database name
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/QualityControl/Master/UOMMasterList/Index.cshtml", model);
        }
        
        [HttpGet]
        public IActionResult GetAllUOMs(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var uoms = new List<QCPUNIT_MAST>(); 
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCPUNIT_MAST", conn))
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
                                uoms.Add(new QCPUNIT_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, lists = uoms, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching UOM records", error = ex.Message });
            }
        }
        
        [HttpGet]
        public IActionResult GetUOMByCode(int code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            QCPUNIT_MAST uom = null;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCPUNIT_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                uom = new QCPUNIT_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = uom });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching UOM data", error = ex.Message });
            }
        }
        
        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@Action", "Excel" }
                };

                var fileBytes = _globalValidationdate.ExportToExcel("sp_QCPUNIT_MAST", "QC UOM Master", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"QCUOMMaster_{DateTime.Now:ddMMyyyy}.xlsx"
                );
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}
 