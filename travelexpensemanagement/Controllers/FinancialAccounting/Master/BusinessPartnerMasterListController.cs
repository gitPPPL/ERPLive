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
    public class BusinessPartnerMasterListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public BusinessPartnerMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Business Partner Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/BusinessPartnerMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetBusinessPartnerMasterList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var list = new List<GeneralDetailsModelList>();
            int totalCount = 0;
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_InsertBusinessPartner", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "Select");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new GeneralDetailsModelList
                            {
                                ACCODE = reader["ACCODE"]?.ToString(),
                                NAME = reader["NAME"]?.ToString(),
                                GROUPNAME = reader["GROUPNAME"]?.ToString(),
                                CURRENCY = reader["CURRENCY"]?.ToString(),
                                NATURE = reader["NATURE"]?.ToString(),
                                MOBILE = reader["MOBILE"]?.ToString(),
                                EMAILID = reader["EMAILID"]?.ToString(),    
                                PARTYTYPE = reader["PARTYTYPE"]?.ToString(),
                                BANKNAME = reader["BANKNAME"]?.ToString(),
                                IFSCCODE = reader["IFSCCODE"]?.ToString(),
                                ACNO = reader["ACNO"]?.ToString(),
                                BANKBRANCH = reader["BANKBRANCH"]?.ToString()
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
                return Json(new { error = true, message = ex.Message });
            }

            return Json(new
            {
                data = list,
                totalCount = totalCount,
                currentPage = pageNumber,
                pageSize = pageSize
            });
        }

        public JsonResult DeleteDocByCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_DeleteBusinessPartnerMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", docCode);

                    conn.Open();
                    cmd.ExecuteNonQuery(); // Execute the DELETE command
                }
            }
            return Json(new { success = true, message = "Record deleted successfully." });
        }

        // Downlod File in Excel
        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<GeneralDetailsModelList>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertBusinessPartner", conn))
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
                            docList.Add(new GeneralDetailsModelList
                            {
                                ACCODE = reader["ACCODE"]?.ToString(),
                                NAME = reader["NAME"]?.ToString(),
                                GROUPNAME = reader["GROUPNAME"]?.ToString(),
                                CURRENCY = reader["CURRENCY"]?.ToString(),
                                NATURE = reader["NATURE"]?.ToString(),
                                MOBILE = reader["MOBILE"]?.ToString(),
                                EMAILID = reader["EMAILID"]?.ToString(),
                                PARTYTYPE = reader["PARTYTYPE"]?.ToString(),
                                BANKNAME = reader["BANKNAME"]?.ToString(),
                                IFSCCODE = reader["IFSCCODE"]?.ToString(),
                                ACNO = reader["ACNO"]?.ToString(),
                                BANKBRANCH = reader["BANKBRANCH"]?.ToString()
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
                using (SqlCommand cmd = new SqlCommand("sp_InsertBusinessPartner", conn))
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
