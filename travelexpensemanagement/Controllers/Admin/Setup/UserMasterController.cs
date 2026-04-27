using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.EncryptionHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using static iTextSharp.text.pdf.AcroFields;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class UserMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly EncryptionHelper _encryptionHelper;
        public UserMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, EncryptionHelper encryptionHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _encryptionHelper = encryptionHelper;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/Setup/UserMaster/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetCompanyddl()
        {
            List<object> company = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //string query = "SELECT Code, Name FROM COMP_MAST";
                string query = "SELECT Code, Name FROM COMP_MAST order by Code asc";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    company.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }
            return Json(company);
        }
        // GetDepartments start block
        [HttpGet]
        public JsonResult GetDepartmentsddl()
        {
            List<object> departments = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code, Name  From DEPT_MAST order by NAME asc";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    departments.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }
            return Json(departments);
        }
        // GetDepartments End block

        // GetDesignation start block
        [HttpGet]
        public JsonResult GetDesignationddl()
        {
            List<object> designation = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code, Name From DESG_MAST order by NAME asc";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    designation.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }
            return Json(designation);
        }
        [HttpGet]
        public JsonResult GetDashboardNameddl()
        {
            List<object> designation = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code, DISPLAY_NAME From MENU_MAST where MODULE_CODE=17 order by DISPLAY_NAME asc";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    designation.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["DISPLAY_NAME"].ToString()
                    });
                }
            }
            return Json(designation);
        }

        [HttpPost]
        public IActionResult InsertUser([FromBody] UserMaster model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "No data received" });
            }
            // Basic validation for required fields
            if (string.IsNullOrWhiteSpace(model.UserName) ||
                string.IsNullOrWhiteSpace(model.FullName) ||
                string.IsNullOrWhiteSpace(model.Password) ||
                model.AllowDays == null)
            {
                return Json(new { success = false, message = "Required fields are missing" });
            }
            SqlTransaction transaction = null;
            try
            {
                int PasswordNeverExpire = model.PasswordNeverExpire ? 1 : 0;
                int PasswordChangeNextLogin = model.PasswordChangeNextLogin ? 1 : 0;
                //string hashedPassword = Encrypt(model.Password);
                //string hashedPassword = EncryptionHelper.Encrypt(model.Password);
                string hashedPassword = _encryptionHelper.Encrypt(model.Password);
                int newCode;
                using (SqlConnection con = _dbConnection.GetConDbConnection())
                {
                    con.Open();
                    // Begin transaction
                    transaction = con.BeginTransaction();
                    SqlCommand cmdGetCode = new SqlCommand("SELECT MAX(CAST(Code AS INT)) + 1 AS NewCode FROM USER_MAST", con, transaction);
                    var result = cmdGetCode.ExecuteScalar();
                    newCode = result != DBNull.Value ? Convert.ToInt32(result) : 1;

                    var sessionData = _globalVariableService.GetGlobalVariables();

                    // Insert into USER_MAST via stored procedure
                    using (SqlCommand cmd = new SqlCommand("sp_InsertUserMaster", con, transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        //var compCode = model.CompanyIds != null && model.CompanyIds.Any()
                        //    ? Convert.ToInt32(model.CompanyIds.First()) : 0;
                        var compCode = sessionData?.PubCompCode ?? "0";
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@Code", newCode);
                        cmd.Parameters.AddWithValue("@USER_NAME", model.UserName);
                        cmd.Parameters.AddWithValue("@FULL_NAME", model.FullName);
                        cmd.Parameters.AddWithValue("@DESIGNATION", model.Designation ?? "");
                        cmd.Parameters.AddWithValue("@DEPARTMENT", model.Department ?? "");
                        cmd.Parameters.AddWithValue("@EMP_CODE", model.EmpCode);
                        cmd.Parameters.AddWithValue("@WebPASSWD", hashedPassword);
                        cmd.Parameters.AddWithValue("@USER_LEVEL", model.UserLevel);
                        cmd.Parameters.AddWithValue("@PC_NAME", model.PCName1 ?? "");
                        cmd.Parameters.AddWithValue("@PC_NAME2", model.PCName2 ?? "");
                        cmd.Parameters.AddWithValue("@PC_MACADDRESS", model.MACID ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", model.IsActive.ToString());
                        cmd.Parameters.AddWithValue("@ALLOW_DAYS", model.AllowDays);
                        cmd.Parameters.AddWithValue("@PASS_NEXPIRED", PasswordNeverExpire);
                        cmd.Parameters.AddWithValue("@PASS_NEXTLOGIN", PasswordChangeNextLogin);
                        cmd.Parameters.AddWithValue("@EMAIL_ID", model.Email ?? "");
                        cmd.Parameters.AddWithValue("@MOBILE_NO", model.Mobile ?? "");

                        cmd.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE ?? "");
                        cmd.Parameters.AddWithValue("@DESG_CODE", model.DESG_CODE ?? "");


                        cmd.Parameters.AddWithValue("@ISTASK_ALLOWED", model.UserAllowForTask ? 1 : 0);
                        cmd.Parameters.AddWithValue("@APP_DEVICEID1", model.AppDeviceID1 ?? "");
                        cmd.Parameters.AddWithValue("@APP_DEVICEID2", model.AppDeviceID2 ?? "");

                        cmd.Parameters.AddWithValue("@UUSER", sessionData.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", sessionData.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", sessionData.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.UserName);

                        cmd.ExecuteNonQuery();
                    }
                    // Insert into SUBUSER_MAST for each CompanyId
                    foreach (var item in model.CompanyIds)
                    {
                        using (SqlCommand subCmd = new SqlCommand(
                            "INSERT INTO SUBUSER_MAST (USER_CODE, COMP_CODE) VALUES (@USER_CODE, @COMP_CODE)", con, transaction))
                        {
                            subCmd.Parameters.AddWithValue("@USER_CODE", newCode);
                            subCmd.Parameters.AddWithValue("@COMP_CODE", item);
                            subCmd.ExecuteNonQuery();
                        }
                    }

                    // Commit transaction
                    transaction.Commit();
                }

                return Json(new { success = true, message = "User inserted successfully" });
            }
            catch (Exception ex)
            {
                try
                {
                    transaction?.Rollback();
                }
                catch { }

                return Json(new { success = false, message = $"Error occurred: {ex.Message}" });
            }
        }
        [HttpGet]
        public IActionResult EditUserDetails(int id)
        {
            string queryUser = "SELECT * FROM USER_MAST WHERE CODE = @EmpCode";
            UserMaster user = null;

            string queryCompanyIds = "SELECT COMP_CODE FROM SUBUSER_MAST WHERE USER_CODE = @EmpCode";
            using (var con = _dbConnection.GetConDbConnection())
            {
                if (con.State != ConnectionState.Open)
                {
                    con.Open();
                }

                // Fetch user data
                using (var cmd = new SqlCommand(queryUser, con))
                {
                    cmd.Parameters.AddWithValue("@EmpCode", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new UserMaster
                            {
                                // Check each column before accessing it
                                UserName = reader.IsDBNull(reader.GetOrdinal("USER_NAME")) ? string.Empty : reader["USER_NAME"].ToString(),
                                FullName = reader.IsDBNull(reader.GetOrdinal("FULL_NAME")) ? string.Empty : reader["FULL_NAME"].ToString(),
                                EmpCode = reader.IsDBNull(reader.GetOrdinal("EMP_CODE")) ? 0 : Convert.ToInt32(reader["EMP_CODE"]),
                                Password = reader.IsDBNull(reader.GetOrdinal("PASSWD")) ? string.Empty : reader["PASSWD"].ToString(),
                                UserLevel = reader.IsDBNull(reader.GetOrdinal("USER_LEVEL")) ? 0 : Convert.ToInt32(reader["USER_LEVEL"]),
                                AllowDays = reader.IsDBNull(reader.GetOrdinal("ALLOW_DAYS")) ? null : Convert.ToInt32(reader["ALLOW_DAYS"]),
                                PCName1 = reader.IsDBNull(reader.GetOrdinal("PC_NAME")) ? string.Empty : reader["PC_NAME"].ToString(),
                                PCName2 = reader.IsDBNull(reader.GetOrdinal("PC_NAME2")) ? string.Empty : reader["PC_NAME2"].ToString(),
                                MACID = reader.IsDBNull(reader.GetOrdinal("PC_MACADDRESS")) ? string.Empty : reader["PC_MACADDRESS"].ToString(),
                                AppDeviceID1 = reader.IsDBNull(reader.GetOrdinal("APP_DEVICEID1")) ? string.Empty : reader["APP_DEVICEID1"].ToString(),
                                AppDeviceID2 = reader.IsDBNull(reader.GetOrdinal("APP_DEVICEID2")) ? string.Empty : reader["APP_DEVICEID2"].ToString(),
                                IsActive = reader.IsDBNull(reader.GetOrdinal("ACTIVE")) ? string.Empty : reader["ACTIVE"].ToString(),
                                Department = reader.IsDBNull(reader.GetOrdinal("DEPT_CODE")) ? string.Empty : reader["DEPT_CODE"].ToString(),
                                Designation = reader.IsDBNull(reader.GetOrdinal("DESG_CODE")) ? string.Empty : reader["DESG_CODE"].ToString(),
                                Mobile = reader.IsDBNull(reader.GetOrdinal("MOBILE_NO")) ? string.Empty : reader["MOBILE_NO"].ToString(),

                                Dashboard = reader.IsDBNull(reader.GetOrdinal("DBFORM_CODE")) ? default : Convert.ToInt32(reader["DBFORM_CODE"]),
                                Email = reader.IsDBNull(reader.GetOrdinal("EMAIL_ID")) ? string.Empty : reader["EMAIL_ID"].ToString(),
                                PasswordNeverExpire = reader.IsDBNull(reader.GetOrdinal("PASS_NEXPIRED")) ? false : Convert.ToBoolean(reader["PASS_NEXPIRED"]),
                                PasswordChangeNextLogin = reader.IsDBNull(reader.GetOrdinal("PASS_NEXTLOGIN")) ? false : Convert.ToBoolean(reader["PASS_NEXTLOGIN"]),
                                CompanyIds = new List<string>() // Initialize the list for CompanyIds

                            };
                        }
                    }
                }
                // Fetch company IDs from SUBUSER_MAST table
                using (var cmd = new SqlCommand(queryCompanyIds, con))
                {
                    cmd.Parameters.AddWithValue("@EmpCode", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            user.CompanyIds.Add(reader["COMP_CODE"].ToString());
                        }
                    }
                }
            }
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            return Json(new { success = true, data = user });
        }

        //Update start Block
        [HttpPost]
        public IActionResult UpdateUser([FromBody] UserMaster model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "No data received" });
            }
            SqlTransaction transaction = null;
            try
            {
                int PasswordNeverExpire = model.PasswordNeverExpire ? 1 : 0;
                int PasswordChangeNextLogin = model.PasswordChangeNextLogin ? 1 : 0;
                //string hashedPassword = EncryptionHelper.Encrypt(model.Password);
                string hashedPassword = _encryptionHelper.Encrypt(model.Password);
                var sessionData = _globalVariableService.GetGlobalVariables();
                var compCode = sessionData?.PubCompCode ?? "0";
                using (SqlConnection con = _dbConnection.GetConDbConnection())
                {
                    con.Open();
                    // Begin transaction
                    transaction = con.BeginTransaction();
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateUserDetails", con, transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@CODE", model.UserID);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@USER_NAME", model.UserName);
                        cmd.Parameters.AddWithValue("@FULL_NAME", model.FullName);
                        cmd.Parameters.AddWithValue("@DESIGNATION", model.Designation ?? "");
                        cmd.Parameters.AddWithValue("@DEPARTMENT", model.Department ?? "");
                        cmd.Parameters.AddWithValue("@EMP_CODE", model.EmpCode);
                        cmd.Parameters.AddWithValue("@PASSWD", hashedPassword);
                        cmd.Parameters.AddWithValue("@USER_LEVEL", model.UserLevel);
                        cmd.Parameters.AddWithValue("@PC_NAME", model.PCName1 ?? "");
                        cmd.Parameters.AddWithValue("@PC_NAME2", model.PCName2 ?? "");
                        cmd.Parameters.AddWithValue("@PC_MACADDRESS", model.MACID ?? "");
                        cmd.Parameters.AddWithValue("@APP_DEVICEID1", model.AppDeviceID1 ?? "");
                        cmd.Parameters.AddWithValue("@APP_DEVICEID2", model.AppDeviceID2 ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", model.IsActive.ToString());
                        cmd.Parameters.AddWithValue("@ALLOW_DAYS", model.AllowDays);
                        cmd.Parameters.AddWithValue("@PASS_NEXPIRED", PasswordNeverExpire);
                        cmd.Parameters.AddWithValue("@PASS_NEXTLOGIN", PasswordChangeNextLogin);
                        cmd.Parameters.AddWithValue("@MOBILE_NO", model.Mobile ?? "");
                        cmd.Parameters.AddWithValue("@EMAIL_ID", model.Email ?? "");
                        cmd.Parameters.AddWithValue("@DBFORM_CODE", model.Dashboard);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DESG_CODE", model.DESG_CODE ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                    if (model.CompanyIds != null && model.CompanyIds.Any())
                    {
                        using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM SUBUSER_MAST WHERE USER_CODE = @USER_CODE", con, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("@USER_CODE", model.UserID);
                            deleteCmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                            deleteCmd.ExecuteNonQuery();
                        }
                    }
                    // Insert into SUBUSER_MAST for each CompanyId
                    foreach (var item in model.CompanyIds)
                    {
                        using (SqlCommand subCmd = new SqlCommand("INSERT INTO SUBUSER_MAST (USER_CODE, COMP_CODE) VALUES (@USER_CODE, @COMP_CODE)", con, transaction))
                        {
                            subCmd.Parameters.AddWithValue("@USER_CODE", model.UserID);
                            subCmd.Parameters.AddWithValue("@COMP_CODE", item);
                            subCmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
                return Json(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                try
                {
                    transaction?.Rollback();
                }
                catch { }
                return Json(new { success = false, message = $"Error occurred: {ex.Message}" });
            }
        }
        //Update End Block

    }
}
