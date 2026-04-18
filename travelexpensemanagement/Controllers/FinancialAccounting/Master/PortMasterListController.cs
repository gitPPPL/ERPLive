using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    [SessionAuthorize]
    public class PortMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PortMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            ViewBag.CurrentMenu = "Port Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/PortMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public IActionResult GetPorts(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var ports = new List<PORT_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PORT_MAST", conn)) // Replace with your actual SP name
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
                                ports.Add(new PORT_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    PORTCODE = reader["PORTCODE"]?.ToString(),
                                    NAME = reader["NAME"]?.ToString(),
                                    STATE = reader["STATE"]?.ToString(),
                                    PORT_TYPE = reader["PORT_TYPE"]?.ToString(),
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
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching ports", error = ex.Message });
            }

            return Json(new { success = true, lists = ports, totalCount });
        }

        [HttpGet]
        public IActionResult GetPortByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            PORT_MAST port = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PORT_MAST", con)) // Your stored procedure
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                port = new PORT_MAST
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    PORTCODE = rdr["PORTCODE"]?.ToString(),
                                    NAME = rdr["NAME"]?.ToString(),
                                    STATE = rdr["STATE"]?.ToString(),
                                    PORT_TYPE = rdr["PORT_TYPE"]?.ToString(),
                                    //REMARKS = rdr["REMARKS"]?.ToString(),
                                    ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = port });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching port details", error = ex.Message });
            }
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<PortExport>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PORT_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Export");
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new PortExport
                            {
                                CODE = reader["Code"]?.ToString(),
                                NAME = reader["Name"]?.ToString(),
                                PORTCODE = reader["PORTCODE"]?.ToString(),
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
                using (SqlCommand cmd = new SqlCommand("sp_PORT_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@CODE", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new ItemGroupDetailDto
                            {
                                Code = reader["CODE"]?.ToString(),
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
 