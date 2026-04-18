using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class MeshConversionMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public MeshConversionMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/Utilities/MeshConversionMasterList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetMeshConversionMasterList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            List<dynamic> list = new List<dynamic>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("USP_PAY_MESHCONV_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Adding parameters
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    conn.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new
                            {
                                SNO = Convert.ToInt32(dr["SNO"]),
                                MESH_NAME = dr["MESH_NAME"]?.ToString(),
                                MESH_CODE = dr["MESH_CODE"]?.ToString(),
                                RUN_NO = Convert.ToInt32(dr["RUN_NO"]),
                                BASE_PRODUCTION = Convert.ToDecimal(dr["BASE_PRODUCTION"]),
                                PRODUCTION = Convert.ToDecimal(dr["PRODUCTION"]),
                                PER = Convert.ToDecimal(dr["PER"]),
                                REPORT_FLG = dr["REPORT_FLG"]?.ToString()
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
                using (SqlCommand cmd = new SqlCommand("USP_PAY_MESHCONV_MAST", conn))
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

