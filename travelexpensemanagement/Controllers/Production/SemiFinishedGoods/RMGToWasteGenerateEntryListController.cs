using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Production.SemiFinishedGoods
{
    public class RMGToWasteGenerateEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public RMGToWasteGenerateEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
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
            ViewBag.CurrentMenu = "RMG To Waste Generate Entry";

            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Production/SemiFinishedGoods/RMGToWasteGenerateEntryList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult loadListData(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            int totalCount = 0;
            List<object> list = new List<object>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_RMGToWaste_GenerateEntry", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "RAIT,RAIV");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@Action", "List");

                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            docId = reader["DOC_ID"].ToString(),
                            VNo = reader["VNo"] != DBNull.Value ? Convert.ToInt32(reader["VNo"]) : 0,
                            VType = reader["VType"]?.ToString(),
                            VDate = Convert.ToDateTime(reader["VDate"]).ToString("yyyy-MM-dd"),
                            Shift = reader["Shift"]?.ToString(),
                            Slip_No = reader["Slip_No"]?.ToString(),
                            Pord_Type = reader["Pord_Type"]?.ToString(),
                            Pord_No = reader["Pord_No"] != DBNull.Value ? Convert.ToInt32(reader["Pord_No"]) : 0,
                            Remark = reader["Remark"]?.ToString(),
                            Status = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0
                        });
                    }
                    if (reader.NextResult() && reader.Read())
                    {
                        totalCount = Convert.ToInt32(reader["TotalCount"]);
                    }
                    return Json(new { success = true, data = list, totalCount });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteData(string docId)
        {
            var globalVarible = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_RMGToWaste_GenerateEntry", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVarible.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVarible.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVarible.PubFYearCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@Action", "Delete");
                    cmd.ExecuteNonQuery();
                }
                return Json(new { success = true, message = "Data Deleted Successfully !!" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
