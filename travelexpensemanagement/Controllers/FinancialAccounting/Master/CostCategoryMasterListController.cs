using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class CostCategoryMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel; 
        public CostCategoryMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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

            ViewBag.CurrentMenu = "Cost Category Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/CostCategoryMasterList/Index.cshtml", model);
        }


        [HttpGet]
        public IActionResult GetCostCategories(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var costCategories = new List<COSTCAT_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_COSTCAT_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value); // For bulk fetch

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            costCategories.Add(new COSTCAT_MAST
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"]?.ToString(),
                                COSTCODE = reader["COSTCODE"]?.ToString(),
                                COSTTYPE = reader["COSTTYPE"]?.ToString(),
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching cost categories", error = ex.Message });
            }

            return Json(new { success = true, lists = costCategories, totalCount });
        }

        [HttpGet]
        public IActionResult GetCostCategoryByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            COSTCAT_MAST category = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_COSTCAT_MAST", con))
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
                            category = new COSTCAT_MAST
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                NAME = rdr["NAME"]?.ToString(),
                                COSTCODE = rdr["COSTCODE"]?.ToString(),
                                COSTTYPE = rdr["COSTTYPE"]?.ToString(),
                                ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
                                UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                                UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.Now,
                                EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                                EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.Now,
                                AED = rdr["AED"]?.ToString(),
                                WSID = rdr["WSID"]?.ToString(),
                                LIP = rdr["LIP"]?.ToString(),
                                LID = rdr["LID"]?.ToString()
                            };
                        }
                    }
                }

                return Json(new { success = true, data = category });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching cost category", error = ex.Message });
            }
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var costCatList = new List<CostCatExpert>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_COSTCAT_MAST", conn))
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
                            costCatList.Add(new CostCatExpert
                            {
                                Code = reader["CODE"]?.ToString(),
                                Name = reader["Name"]?.ToString(),
                                CostCode = reader["COSTCODE"]?.ToString(),
                                CostType = reader["COSTTYPE"]?.ToString(),
                                Status = reader["STATUS"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(costCatList);
        }


        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_COSTCAT_MAST", conn))
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
 