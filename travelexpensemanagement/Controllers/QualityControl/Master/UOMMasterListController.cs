using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class UOMMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        private int? userLevel;
        public UOMMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/UOMMasterList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAllUOMs(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            //var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var uoms = new List<QCPUNIT_MAST>(); 
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCPUNIT_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        //cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                uoms.Add(new QCPUNIT_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, lists = uoms, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching UOM records", error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetUOMByCode(int code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            QCPUNIT_MAST uom = null;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCPUNIT_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                uom = new QCPUNIT_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = uom });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching UOM data", error = ex.Message });
            }
        }

    }
}
 