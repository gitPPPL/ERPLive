using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
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
    public class ItemDepartmentListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public ItemDepartmentListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Item Department Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); 

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/ItemDepartmentList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllDepartment(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var groupList = new List<ItemDeptViewModelList>();
            int totalCount = 0;
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_InsertItemDept", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Required parameters
                    cmd.Parameters.AddWithValue("@Action", "Select");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    //cmd.Parameters.AddWithValue("@NAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@SHORTNAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@TRAN_TYPE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PLACE_TYPE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@REPORT_TYPE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UNIT_TYPE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PLACE_CODE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SORT_ON", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@MPURCH_BUDGET", DBNull.Value);
                    cmd.Parameters.AddWithValue("@MCONSUMP_BUDGET", DBNull.Value);
                    cmd.Parameters.AddWithValue("@COST_CODE", DBNull.Value);

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Read paged data
                        while (reader.Read())
                        {
                            groupList.Add(new ItemDeptViewModelList
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : (int?)null,
                                NAME = reader["NAME"]?.ToString(),
                                SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                TRAN_TYPE = reader["TRAN_TYPE"]?.ToString(),
                                PLACE_TYPE = reader["PLACE_TYPE"]?.ToString(),
                                REPORT_TYPE = reader["REPORT_TYPE"]?.ToString(),
                                UNIT_TYPE = reader["UNIT_TYPE"]?.ToString(),
                                PLACE_CODE = reader["PLACE_CODE"]?.ToString(),
                                SORT_ON = reader["SORT_ON"] != DBNull.Value ? Convert.ToInt32(reader["SORT_ON"]) : (int?)null,
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                MPURCH_BUDGET = reader["MPURCH_BUDGET"] != DBNull.Value ? Convert.ToDecimal(reader["MPURCH_BUDGET"]) : (decimal?)null,
                                MCONSUMP_BUDGET = reader["MCONSUMP_BUDGET"] != DBNull.Value ? Convert.ToDecimal(reader["MCONSUMP_BUDGET"]) : (decimal?)null,
                            });
                        }

                        // Move to next result set for total count
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

            return Json(new { groups = groupList, totalCount });
        }

        
        [HttpPost]
        public JsonResult DeleteDepartment(int id)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertItemDept", con)) // Use your actual SP name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", id);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  category.", error = ex.Message });
            }
        }
        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var itemList = new List<ItemDeptExportDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertItemDept", conn))
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
                            itemList.Add(new ItemDeptExportDto
                            {
                                Code = reader["Code"]?.ToString(),
                                Name = reader["Name"]?.ToString(),
                                TranType = reader["TRAN_TYPE"]?.ToString(),
                                PlaceType = reader["PLACE_TYPE"]?.ToString(),
                                ReportType = reader["REPORT_TYPE"]?.ToString(),
                                UnitType = reader["UNIT_TYPE"]?.ToString(),
                                PlaceCode = reader["PLACE_CODE"]?.ToString(),
                                Active = reader["ACTIVE"] != DBNull.Value && Convert.ToBoolean(reader["ACTIVE"]),
                                Status = reader["Status"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(itemList);
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<ItemDeptDetailDto> docDetails = new List<ItemDeptDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertItemDept", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    // Set required parameters for the stored procedure
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docDetails.Add(new ItemDeptDetailDto
                            {
                                CODE = reader["CODE"]?.ToString(),
                                UUser = reader["UUser"]?.ToString(),
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            });
                        }
                    }
                }
            }
            return Json(new { success = true, data = docDetails });
        }
    }
}
