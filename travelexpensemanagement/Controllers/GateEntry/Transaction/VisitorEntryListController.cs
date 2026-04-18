using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Gate_Entry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VisitorEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public VisitorEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/GateEntry/Transaction/VisitorEntryList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetAllVisitors(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var visitors = new List<VISITOR>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE",1);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                visitors.Add(new VISITOR
                                {
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                    SLIP_NO = reader["SLIP_NO"]?.ToString(),
                                    NAME = reader["NAME"]?.ToString(),
                                    ORGANIZATION = reader["ORGANIZATION"]?.ToString(),
                                    IN_TIME = reader["IN_TIME"]?.ToString(),
                                    OUT_TIME = reader["OUT_TIME"]?.ToString(),
                                    MEET_NAME = reader["MEET_NAME"]?.ToString(),
                                    PURPOSE = reader["PURPOSE"]?.ToString(),
                                    ADDRESS = reader["ADDRESS"]?.ToString(),
                                    MOBILE_NO = reader["MOBILE_NO"]?.ToString(),
                                    VEHICLE_NO = reader["VEHICLE_NO"]?.ToString(),
                                    MATERIAL = reader["MATERIAL"]?.ToString(),
                                    CARD_NO = reader["CARD_NO"]?.ToString()
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching visitors", error = ex.Message });
            }

            return Json(new { success = true, visitors, totalCount });
        }

        [HttpGet]
        public IActionResult GetVisitorByVno(int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            VISITOR visitor = null;
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn)) // Change SP name accordingly
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "GETBYID");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        //cmd.Parameters.AddWithValue("@V_TYPE", vType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        conn.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                visitor = new VISITOR
                                {
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : (int?)null,
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : (DateTime?)null,
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    SLIP_NO = rdr["SLIP_NO"]?.ToString(),
                                    NAME = rdr["NAME"]?.ToString(),
                                    CARD_NO = rdr["CARD_NO"]?.ToString(),
                                    ORGANIZATION = rdr["ORGANIZATION"]?.ToString(),
                                    ADDRESS = rdr["ADDRESS"]?.ToString(),
                                    MEET_CODE = rdr["MEET_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MEET_CODE"]) : (int?)null,
                                    MEET_NAME = rdr["MEET_NAME"]?.ToString(),
                                    IN_TIME = rdr["IN_TIME"]?.ToString(),
                                    OUT_DATE = rdr["OUT_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["OUT_DATE"]) : (DateTime?)null,
                                    OUT_TIME = rdr["OUT_TIME"]?.ToString(),
                                    MOBILE_NO = rdr["MOBILE_NO"]?.ToString(),
                                    PURPOSE = rdr["PURPOSE"]?.ToString(),
                                    VEHICLE_NO = rdr["VEHICLE_NO"]?.ToString(),
                                    MATERIAL = rdr["MATERIAL"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    IMG_FILE = rdr["IMG_FILE"] != DBNull.Value ? (byte[])rdr["IMG_FILE"] : null,
                                    FILE_NAME = rdr["FILE_NAME"]?.ToString()
                                    // Add other fields as needed
                                };
                            }
                        }
                    }
                }

                if (visitor != null && visitor.IMG_FILE != null)
                {
                    var imageBytes = visitor.IMG_FILE;
                    visitor.IMG_FILE = null;

                    var base64String = Convert.ToBase64String(imageBytes);
                    return Json(new { success = true, data = visitor, base64Image = base64String });
                }

                return Json(new { success = true, data = visitor });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching visitor data", error = ex.Message });
            }
        }

    }
}
 