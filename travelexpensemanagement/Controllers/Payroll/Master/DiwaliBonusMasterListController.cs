using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;


namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class DiwaliBonusMasterListController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        private int? userLevel;
        public DiwaliBonusMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          DropdownService dropdownService, DbHelper dbHelper,
          ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }


        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/DiwaliBonusMasterList/Index.cshtml");
        }

        public IActionResult Getlist(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var DiwaliBonusModel = new List<DiwaliBonusModel>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_DewaliBonusMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DiwaliBonusModel.Add(new DiwaliBonusModel
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                TYPE = reader["TYPE"] != DBNull.Value ? reader["TYPE"].ToString() : null,
                                JOIN_DATE = reader["JOIN_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["JOIN_DATE"]) : DateTime.MinValue,
                                PERC = reader["PERC"] != DBNull.Value ? Convert.ToDecimal(reader["PERC"]) : 0,
                                AMT = reader["AMT"] != DBNull.Value ? Convert.ToDecimal(reader["AMT"]) : 0,
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching Qualification Master", error = ex.Message });
            }

            return Json(new { success = true, lists = DiwaliBonusModel, totalCount });
        }

        [HttpGet]
        public IActionResult GetdataByCode(int code)
        {
            var getvariable = _globalVariableService.GetGlobalVariables();


            DiwaliBonusModel DiwaliBonusModel = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DewaliBonusMaster", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("COMP_CODE", getvariable.PubCompCode);


                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                DiwaliBonusModel = new DiwaliBonusModel
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    TYPE = rdr["TYPE"] != DBNull.Value ? rdr["TYPE"].ToString() : null,
                                    JOIN_DATE = rdr["JOIN_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["JOIN_DATE"]) : DateTime.MinValue,
                                    PERC = rdr["PERC"] != DBNull.Value ? Convert.ToDecimal(rdr["PERC"]) : 0,
                                    AMT = rdr["AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMT"]) : 0
                                    };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = DiwaliBonusModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }



        [HttpPost]
        public JsonResult Delete(int code)
        {
            var getvariable = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DewaliBonusMaster", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getvariable.PubCompCode);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Diwali Bonus Master deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Diwali Bonus Master.", error = ex.Message });
            }
        }

    }

}
