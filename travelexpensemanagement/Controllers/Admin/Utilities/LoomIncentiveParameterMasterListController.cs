using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Utilities;


namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class LoomIncentiveParameterMasterListController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public LoomIncentiveParameterMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/LoomIncentiveParameterMasterList/Index.cshtml");
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var LoomIncentiveParameterMaster_Model = new List<LoomIncentiveParameterMaster_Model>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_PAY_LOOMINCENPARM_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                             
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            LoomIncentiveParameterMaster_Model.Add(new LoomIncentiveParameterMaster_Model
                            {
                                Code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                ConvCode = reader["CONV_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CONV_CODE"]) : 0,
                                Name = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
                                LoomType = reader["LOOM_TYPE"] != DBNull.Value ? reader["LOOM_TYPE"].ToString() : string.Empty,
                                ConvName = reader["CONV_NAME"] != DBNull.Value ? reader["CONV_NAME"].ToString() : string.Empty,
                                Per = reader["PER"] != DBNull.Value ? Convert.ToDecimal(reader["PER"]) : 0,
                                FixAmt = reader["FIX_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["FIX_AMT"]) : 0,
                                Active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
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

            return Json(new { success = true, lists = LoomIncentiveParameterMaster_Model, totalCount });
        }

        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            LoomIncentiveParameterMaster_Model LoomIncentiveParameterMaster_Model = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_LOOMINCENPARM_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                
                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                LoomIncentiveParameterMaster_Model = new LoomIncentiveParameterMaster_Model
                                {
                                    Code = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    Name = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : null,
                                    V_Type = rdr["V_Type"] != DBNull.Value ? rdr["V_Type"].ToString() : null,
                                    LoomType = rdr["LOOM_TYPE"] != DBNull.Value ? rdr["LOOM_TYPE"].ToString() : null,
                                    ConvCode = rdr["CONV_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CONV_CODE"]) : 0,
                                    ConvName = rdr["CONV_NAME"] != DBNull.Value ? rdr["CONV_NAME"].ToString() : null,
                                    Per = rdr["PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PER"]) : 0,
                                    FixAmt = rdr["FIX_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["FIX_AMT"]) : 0,
                                    Active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0

                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = LoomIncentiveParameterMaster_Model });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Delete(int code)
        {
            var globalvariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_LOOMINCENPARM_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);
                   
                      
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = " Loom Incentive Parameter Master deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Loom Incentive Parameter Master.", error = ex.Message });
            }
        }

    }
}
