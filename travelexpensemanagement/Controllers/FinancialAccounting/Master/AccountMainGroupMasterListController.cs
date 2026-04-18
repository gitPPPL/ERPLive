using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class AccountMainGroupMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public AccountMainGroupMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/AccountMainGroupMasterList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAccountMainGroupMasterList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var groupList = new List<AccountMainGroup>();
            int totalCount = 0;
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_InsertGRMast", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "Select");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", 1);
                    cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    // Other unused parameters
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHORTNAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@TYPE", DBNull.Value);
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
                            groupList.Add(new AccountMainGroup
                            {
                                code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                group_name = reader["NAME"]?.ToString(),
                                short_name = reader["SHORTNAME"]?.ToString(),
                                type = reader["TYPE"]?.ToString(),
                                active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
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
                    string delete = "DELETE FROM GR_MAST WHERE COMP_CODE=@COMP_CODE AND CODE=@CODE";
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
