using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{   
    public class AccountGroupMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public AccountGroupMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/AccountGroupMasterList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAccountGroupMasterList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var groupList = new List<AccountGroupMasterList>();
            int totalCount = 0;
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_InsertMGROUPMast", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Select");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    //cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHORTNAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SRNO", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            groupList.Add(new AccountGroupMasterList
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                GROUP_NAME = reader["GROUP_NAME"]?.ToString(),
                                SHORT_NAME = reader["SHORT_NAME"]?.ToString(),
                                MAIN_GROUP_NAME = reader["MAIN_GROUP_NAME"]?.ToString(),
                                NATURE = reader["NATURE"]?.ToString(),
                                SCHEDULE_GROUPING = reader["SCHEDULE_GROUPING"]?.ToString(),
                                SUB_SCHEDULE_NAME = reader["SUB_SCHEDULE_NAME"]?.ToString(),
                                MAIN_SCHEDULE_NAME = reader["MAIN_SCHEDULE_NAME"]?.ToString(),
                                GROUPING_ON_TRAIL = reader["GROUPING_ON_TRAIL"] != DBNull.Value && Convert.ToBoolean(reader["GROUPING_ON_TRAIL"]),
                                ACTIVE = reader["ACTIVE"] != DBNull.Value && Convert.ToBoolean(reader["ACTIVE"])
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

            return Json(new { groups = groupList, totalCount });
        }

        [HttpPost]
        public IActionResult DeleteID(int ID)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    string delete = "DELETE FROM MGROUP_MAST WHERE COMP_CODE=@COMP_CODE AND CODE=@CODE";
                    using (SqlCommand cmd2 = new SqlCommand(delete, conn))
                    {
                        cmd2.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd2.Parameters.AddWithValue("@CODE", ID);
                        cmd2.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Record deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
