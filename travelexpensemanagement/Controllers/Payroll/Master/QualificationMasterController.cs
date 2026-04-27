using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class QualificationMasterController : Controller
    {



        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public QualificationMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbcontext;
            _globalVariableService = globalValue;
            _moduleService = moduleService;
        }



        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Employee Category Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Payroll/Master/QualificationMaster/Index.cshtml", model);
        }


        [HttpPost]
        public IActionResult SaveMaster([FromBody] Qualification_MasterModel data)
        {
            if (data == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            string action = data.Action == "INSERT" ? "Insert" : "Update";

            var result = Submitbtn(data, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }


        [HttpPost]
        private string Submitbtn(Qualification_MasterModel data, string action)
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
                            FROM QUALIFICATION_MAST
                            WHERE UPPER(NAME) = UPPER(@Name);  ";

                         string name = null;

                        using (SqlCommand cmdYear = new SqlCommand(duplicateData, conn))
                        {
                            cmdYear.Parameters.AddWithValue("@Name", data.NAME);
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
                            return "Record Already Exist, Please Check! " + data.NAME + "";
                        }

                    }

                        using (SqlCommand cmd = new SqlCommand("sp_Qualification_mast", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", data.CODE);
                        cmd.Parameters.AddWithValue("@NAME", data.NAME);
                        cmd.Parameters.AddWithValue("@SHORTNAME", data.SHORTNAME);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.Add("@ACTIVE", SqlDbType.Int).Value = data.ACTIVE;

                        int rowsInserted = cmd.ExecuteNonQuery();

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {

                return $"Error: {ex.Message}";
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
        public JsonResult SearchNames(string term)
        {
            List<string> suggestions = new List<string>();
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = @"
                    select NAME from QUALIFICATION_MAST
                    WHERE NAME LIKE @SearchText 
                    ORDER BY NAME ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SearchText", "%" + term + "%");
         
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
