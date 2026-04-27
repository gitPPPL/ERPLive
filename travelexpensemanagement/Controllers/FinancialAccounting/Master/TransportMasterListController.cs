using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class TransportMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public TransportMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
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
            ViewBag.CurrentMenu = "Transport Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/TransportMasterList/Index.cshtml",model);
        }

        [HttpGet]
        public IActionResult GetTransportList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var transportList = new List<TRANSPORT_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TRANSPORT_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value); // Not filtering by CODE

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                transportList.Add(new TRANSPORT_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    //PARTY_CODE = reader["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARTY_CODE"]) : 0,
                                    PartyName = reader["NAME"]?.ToString(),
                                    OWNER_NAME = reader["OWNER_NAME"]?.ToString(),
                                    ADDRESS = reader["ADDRESS"]?.ToString(),
                                    GSTIN = reader["GSTIN"]?.ToString(),
                                    PAN = reader["PAN"]?.ToString(),
                                    TDS_PER = reader["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["TDS_PER"]) : 0,
                                    DECL_NO = reader["DECL_NO"]?.ToString(),
                                    DECL_DATE = reader["DECL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["DECL_DATE"]) : DateTime.MinValue,
                                    EXPIRY_DATE = reader["EXPIRY_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EXPIRY_DATE"]) : DateTime.MinValue,
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
                return Json(new { success = true, lists = transportList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching transport list", error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetTransportByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            TRANSPORT_MAST transport = null;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TRANSPORT_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@CODE", code);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                transport = new TRANSPORT_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    PARTY_CODE = reader["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARTY_CODE"]) : 0,
                                    OWNER_NAME = reader["OWNER_NAME"]?.ToString(),
                                    ADDRESS = reader["ADDRESS"]?.ToString(),
                                    GSTIN = reader["GSTIN"]?.ToString(),
                                    PAN = reader["PAN"]?.ToString(),
                                    TDS_PER = reader["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["TDS_PER"]) : 0,
                                    DECL_NO = reader["DECL_NO"]?.ToString(),
                                    DECL_DATE = reader["DECL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["DECL_DATE"]) : DateTime.MinValue,
                                    EXPIRY_DATE = reader["EXPIRY_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EXPIRY_DATE"]) : DateTime.MinValue,
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    SALE_GROUP = reader["SALE_GROUP"]?.ToString()
                                };
                            }
                        }
                    }
                }
                return Json(new { success = true, data = transport });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching transport data", error = ex.Message });
            }
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<TRANSPORT_MAST>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_TRANSPORT_MAST", conn))
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
                            docList.Add(new TRANSPORT_MAST
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"]?.ToString(),
                                PARTY_CODE = reader["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARTY_CODE"]) : 0,
                                OWNER_NAME = reader["OWNER_NAME"]?.ToString(),
                                ADDRESS = reader["ADDRESS"]?.ToString(),
                                GSTIN = reader["GSTIN"]?.ToString(),
                                PAN = reader["PAN"]?.ToString(),
                                TDS_PER = reader["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["TDS_PER"]) : 0,
                                DECL_NO = reader["DECL_NO"]?.ToString(),
                                DECL_DATE = reader["DECL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["DECL_DATE"]) : DateTime.MinValue,
                                EXPIRY_DATE = reader["EXPIRY_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EXPIRY_DATE"]) : DateTime.MinValue,
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
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
                using (SqlCommand cmd = new SqlCommand("sp_TRANSPORT_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
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
