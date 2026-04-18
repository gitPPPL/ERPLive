using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Inventry;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    public class CategoryMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CategoryMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Inventory/Master/CategoryMasterList/Index.cshtml");
        }
        public IActionResult GetCategaryList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode; 
            var CategaryMast = new List<CategaryMast>();
            int totalCount = 0;
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_Categary_Mast", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);  
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            CategaryMast.Add(new CategaryMast
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                NAME = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
                                SHORTNAME = reader["SHORTNAME"] != DBNull.Value ? reader["SHORTNAME"].ToString() : string.Empty,
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching categories", error = ex.Message });
            }

            return Json(new { success = true, lists = CategaryMast, totalCount });
        }
        [HttpGet]
        public IActionResult GetCategoryCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            CategaryMast CategaryMast = null;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_Categary_Mast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                CategaryMast = new CategaryMast
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    NAME = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : null,
                                    SHORTNAME = rdr["SHORTNAME"] != DBNull.Value ? rdr["SHORTNAME"].ToString() : null,
                                    UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                                    UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                                    EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                                    EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                                    AED = rdr["AED"] != DBNull.Value ? rdr["AED"].ToString() : null,
                                    WSID = rdr["WSID"] != DBNull.Value ? rdr["WSID"].ToString() : null,
                                    LIP = rdr["LIP"] != DBNull.Value ? rdr["LIP"].ToString() : null,
                                    LID = rdr["LID"] != DBNull.Value ? rdr["LID"].ToString() : null,
                                    ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0

                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = CategaryMast });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult Delete(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_Categary_Mast", con)) // Use your actual SP name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = " category deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  category.", error = ex.Message });
            }
        }
    }
}
