using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.PayRoll;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class HolidayMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public HolidayMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
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
            return View("~/Views/Payroll/Master/HolidayMaster/Index.cshtml", model);
        }

        [HttpPost]
        public IActionResult SaveTempMaster([FromBody] HolidayModel data)
        {
            if (data == null)
                return Json(new { success = false, message = "Invalid or empty data." });

            string action = data.action == "INSERT" ? "Insert" : "Update";

            string result = ProcessHoliday(data, action);  // ✅ renamed

            if (result == "Success")
                return Json(new { success = true, message = "Saved successfully!" });

            return Json(new { success = false, message = result });
        }

        private string ProcessHoliday(HolidayModel data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                   if (action == "Insert")
                    {
                        string duplicateData = @"
                            SELECT NAME
                            FROM HOLIDAY_MAST
                            WHERE UPPER(NAME) = UPPER(@Name)
                              AND COMP_CODE = @CompCode
                              AND BRANCH_CODE = 1
                              AND YEAR_CODE = @YearCode;
                            ";

                        string name = null;

                        using (SqlCommand cmdYear = new SqlCommand(duplicateData, conn))
                        {
                            cmdYear.Parameters.AddWithValue("@Name", data.Name);
                            cmdYear.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                            cmdYear.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);

                            using (SqlDataReader reader = cmdYear.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    name = reader["NAME"]?.ToString();
                                }
                            }
                        }

                        // ✅ Check if record exists
                        if (!string.IsNullOrEmpty(name))
                        {
                            return "Record Already Exist, Please Check! " + data.Name + "";
                        }

                    }                             


                    string sql = "SELECT START_DATE, END_DATE FROM YEAR_MAST WHERE CODE = " + globalVar.PubFYearCode + "";

                    DateTime? yearStart = null;
                    DateTime? yearEnd = null;

                    using (SqlCommand cmdYear = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmdYear.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            yearStart = reader["START_DATE"] as DateTime?;
                            yearEnd = reader["END_DATE"] as DateTime?;
                        }
                    }

                    if (!yearStart.HasValue || !yearEnd.HasValue)
                        return "Financial year start/end date not found.";

                    if (data.HolidayDate < yearStart.Value || data.HolidayDate > yearEnd.Value)
                    {
                        return $"Holiday Date must be between {yearStart.Value:dd-MM-yyyy} and {yearEnd.Value:dd-MM-yyyy}.";
                    }


                    if (data.BeforeDate < yearStart.Value || data.BeforeDate > yearEnd.Value)
                    {
                        return $"Before Date must be between {yearStart.Value:dd-MM-yyyy} and {yearEnd.Value:dd-MM-yyyy}.";
                    }

                    if (data.AfterDate < yearStart.Value || data.AfterDate > yearEnd.Value)
                    {
                        return $" After Date must be between {yearStart.Value:dd-MM-yyyy} and {yearEnd.Value:dd-MM-yyyy}.";
                    }


                    using (SqlCommand cmd = new SqlCommand("sp_HolidayMaster", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", data.Code);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@NAME", data.Name);
                        cmd.Parameters.AddWithValue("@HOLIDAY_DATE", data.HolidayDate);
                        cmd.Parameters.AddWithValue("@BEFORE_DATE", data.BeforeDate);
                        cmd.Parameters.AddWithValue("@AFTER_DATE", data.AfterDate);
                        cmd.Parameters.AddWithValue("@NATIONAL_HOLIDAY", data.NationalHoliday);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ACTIVE", data.Active);

                        cmd.ExecuteNonQuery();
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        public JsonResult Getcode()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            int? code = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                 select max(code) + 1  from HOLIDAY_MAST   WHERE COMP_CODE = @CompCode   and BRANCH_CODE = @BranchCode  and YEAR_CODE = @YEAR_CODE    ; ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);

                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    { 
                        code = Convert.ToInt32(result);
                    }
                    else
                    {
                        code = 1;
                    }
                }
            }

            return Json(new { code = code });
        }

        [HttpGet]
        public JsonResult SearchHolidayNames(string term)
        {
            List<string> suggestions = new List<string>();
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = @"
                    select NAME from HOLIDAY_MAST
                    WHERE NAME LIKE @SearchText and  COMP_CODE =@COMP_CODE and BRANCH_CODE =@BRANCH_CODE  and YEAR_CODE  =@YEAR_CODE
                    ORDER BY NAME ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SearchText", "%" + term + "%");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);


                        conn.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                suggestions.Add(dr["Name"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }

            return Json(suggestions);
        }

    }
}
