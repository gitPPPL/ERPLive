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

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class ProductionPlaceMasterListController : Controller
    { 
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public ProductionPlaceMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Production Place Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/ProductionPlaceMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllProdPlaceMast(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var itemList = new List<PRODPLACE_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_ProdPlaceMast", conn)) // 🔁 Replace with your actual stored procedure name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Required parameters
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", 1); // Replace if dynamic
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        // Fill remaining SP parameters with DBNull
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHORTNAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", DBNull.Value);
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
                            try
                            {
                                while (reader.Read())
                                {
                                    itemList.Add(new PRODPLACE_MAST
                                    {
                                        CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                        NAME = reader["NAME"]?.ToString() ?? string.Empty,
                                        SHORTNAME = reader["SHORTNAME"]?.ToString() ?? string.Empty,
                                        PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : 0

                                        //ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                return Json(new { error = "Error reading data", message = ex.Message });
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
                return Json(new { error = "Database error", message = ex.Message });
            }

            return Json(new { items = itemList, totalCount });
        }


        [HttpGet]
        public IActionResult GetProdPlaceMastByCode(int code)
        {
            PRODPLACE_MAST prodPlace = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ProdPlaceMast", con))
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
                            prodPlace = new PRODPLACE_MAST
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                NAME = rdr["NAME"]?.ToString(),
                                SHORTNAME = rdr["SHORTNAME"]?.ToString(),
                                PLACE_CODE = rdr["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PLACE_CODE"]) : 0,
                                //ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0
                            };
                        }
                    }
                }
            }

            if (prodPlace == null)
                return Json(null); // or return NotFound();

            return Json(new
            {
                code = prodPlace.CODE,
                name = prodPlace.NAME,
                shortname = prodPlace.SHORTNAME,
                place_Code = prodPlace.PLACE_CODE,
                //active = prodPlace.ACTIVE
            });
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<ProdPlaceExportModel>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_ProdPlaceMast", conn))
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
                            docList.Add(new ProdPlaceExportModel
                            {
                                CODE = reader["CODE"]?.ToString(),
                                NAME = reader["NAME"]?.ToString(),
                                SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                PLACE_CODE = reader["PLACE_CODE"]?.ToString()
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
                using (SqlCommand cmd = new SqlCommand("sp_ProdPlaceMast", conn))
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
