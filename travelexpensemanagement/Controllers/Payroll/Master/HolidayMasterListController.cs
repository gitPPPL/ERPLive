using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.PayRoll;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class HolidayMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public HolidayMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbcontext;
            _globalVariableService = globalValue;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Holiday Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Payroll/Master/HolidayMasterList/Index.cshtml", model);

        }

        public IActionResult GetHolidayList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var HolidayModelList = new List<HolidayModel>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_HolidayMaster", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            HolidayModelList.Add(new HolidayModel
                            {
                                Code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                Name = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
                                HolidayDate = reader["HOLIDAY_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["HOLIDAY_DATE"]) : (DateTime?)null,
                                BeforeDate = reader["BEFORE_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["BEFORE_DATE"]) : (DateTime?)null,
                                AfterDate = reader["AFTER_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["AFTER_DATE"]) : (DateTime?)null,
                                NationalHoliday = reader["NATIONAL_HOLIDAY"] != DBNull.Value ? Convert.ToInt32(reader["NATIONAL_HOLIDAY"]) : 0,
                                                          
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

            return Json(new { success = true, lists = HolidayModelList, totalCount });
        }


        [HttpGet]
        public IActionResult GetHolidayCode(int code)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            HolidayModel HolidayModel = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_HolidayMaster", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
              
                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                HolidayModel = new HolidayModel
                                {
                                Code = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                Name = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : null,
                                HolidayDate = rdr["HOLIDAY_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["HOLIDAY_DATE"]) : DateTime.MinValue,
                                BeforeDate = rdr["BEFORE_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["BEFORE_DATE"]) : DateTime.MinValue,
                                AfterDate = rdr["AFTER_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["AFTER_DATE"]) : DateTime.MinValue,
                                NationalHoliday = rdr["NATIONAL_HOLIDAY"] != DBNull.Value ? Convert.ToInt32(rdr["NATIONAL_HOLIDAY"]) : 0,
                                Active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0

                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = HolidayModel });
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
                    using (SqlCommand cmd = new SqlCommand("sp_HolidayMaster", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalvariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = " Holiday  Master deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Holiday  Master.", error = ex.Message });
            }
        }

    }
}
