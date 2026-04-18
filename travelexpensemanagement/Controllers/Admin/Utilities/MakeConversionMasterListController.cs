using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class MakeConversionMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public MakeConversionMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/Utilities/MakeConversionMasterList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetMakeConversionMasterList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            List<dynamic> list = new List<dynamic>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("USP_PAY_LOOMINCENTIVERATE_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    // unused params
                    cmd.Parameters.AddWithValue("@MAKE_TYPE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@RUN_NO", DBNull.Value);
                    cmd.Parameters.AddWithValue("@BASE_PRODUCTION", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PRODUCTION", DBNull.Value);
                    cmd.Parameters.AddWithValue("@PER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@REPORT_FLG", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SNO", DBNull.Value);
                    cmd.Parameters.AddWithValue("@FLG", DBNull.Value);

                    conn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        // First result set - data
                        while (dr.Read())
                        {
                            list.Add(new
                            {
                                SNO = Convert.ToInt32(dr["SNO"]),
                                MAKE_TYPE = dr["MAKE_TYPE"]?.ToString(),
                                RUN_NO = Convert.ToInt32(dr["RUN_NO"])
                            });
                        }

                        // Move to the second result set - Total count
                        if (dr.NextResult() && dr.Read())
                        {
                            totalCount = dr["TotalRecords"] != DBNull.Value ? Convert.ToInt32(dr["TotalRecords"]) : 0;
                        }
                    }
                }
            }

            return Json(new { items = list, totalCount });
        }
        [HttpPost]
        public IActionResult Delete(int sno, string maketype, int Runno)
        {
            try
            {
                var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("USP_PAY_LOOMINCENTIVERATE_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@SNO", sno);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@MAKE_TYPE", maketype);
                    cmd.Parameters.AddWithValue("@RUN_NO", Runno);

                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        return Json(new { success = true, message = "Record Deleted Successfully" });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Record Not Found" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }





    }
}
