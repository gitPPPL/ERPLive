using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Travelexpense
{
    public class EmployeeController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        public EmployeeController(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }
        [HttpGet]
        public JsonResult GetUserNameEmail()
        {
            string userName = HttpContext.Session.GetString("UserName");
            string email = "";
            if (string.IsNullOrEmpty(userName))
            {
                return Json(new { success = false, message = "User is not logged in." });
            }
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
            SELECT us.Username, us.Email 
            FROM Users us
            JOIN Employee em ON us.UserID = em.UserID 
            WHERE us.Username = @Username AND em.Name = @Name";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Username", userName);
                    cmd.Parameters.AddWithValue("@Name", userName);
                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        userName = reader["Username"].ToString();
                        email = reader["Email"].ToString();
                    }
                }
            }
            return Json(new { success = true, userName, email });
        }
        // GetDepartments start block
        [HttpGet]
        public JsonResult GetDepartments()
        {
            List<object> departments = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT DepartmentID, DepartmentName FROM DepartmentMaster ORDER BY DepartmentID ASC";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    departments.Add(new
                    {
                        Value = reader["DepartmentID"].ToString(),
                        Text = reader["DepartmentName"].ToString()
                    });
                }
            }
            return Json(departments);
        }
        // GetDepartments End block

        // GetDesignation start block
        [HttpGet]
        public JsonResult GetDesignation()
        {
            List<object> designation = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select DesignationID, DesignationName from DesignationMaster order by DesignationID asc";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    designation.Add(new
                    {
                        Value = reader["DesignationID"].ToString(),
                        Text = reader["DesignationName"].ToString()
                    });
                }
            }
            return Json(designation);
        }
        // GetDesignation End block
        [HttpPost]
        public JsonResult Submitbtn(string Name, string MobileNo, string Email, string Address, string Department, string Designation)
        {
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    string checkMobileQuery = @"SELECT COUNT(*) FROM Employee 
                                        WHERE Name = @Name AND MobileNo = @MobileNo";

                    using (SqlCommand checkMobileCmd = new SqlCommand(checkMobileQuery, conn))
                    {
                        checkMobileCmd.Parameters.AddWithValue("@Name", Name);
                        checkMobileCmd.Parameters.AddWithValue("@MobileNo", MobileNo);

                        int mobileExists = (int)checkMobileCmd.ExecuteScalar();
                        if (mobileExists > 0)
                        {
                            return Json(new { success = false, message = "Employee with same Name and Mobile No already exists!" });
                        }
                    }
                    // Now check if same Name AND Email exists
                    string checkEmailQuery = @"SELECT COUNT(*) FROM Employee 
                                       WHERE Name = @Name AND Email = @Email";

                    using (SqlCommand checkEmailCmd = new SqlCommand(checkEmailQuery, conn))
                    {
                        checkEmailCmd.Parameters.AddWithValue("@Name", Name);
                        checkEmailCmd.Parameters.AddWithValue("@Email", Email);

                        int emailExists = (int)checkEmailCmd.ExecuteScalar();

                        if (emailExists > 0)
                        {
                            // UPDATE
                            string updateQuery = @"UPDATE Employee SET 
                                            MobileNo = @MobileNo,
                                            Address = @Address,
                                            Department = @Department,
                                            Designation = @Designation,
                                            Active = @Active,
                                            CreatedAt = @CreatedAt
                                            WHERE Name = @Name AND Email = @Email";

                            using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@MobileNo", MobileNo);
                                cmd.Parameters.AddWithValue("@Address", Address);
                                cmd.Parameters.AddWithValue("@Department", Department);
                                cmd.Parameters.AddWithValue("@Designation", Designation);
                                cmd.Parameters.AddWithValue("@Active", true);
                                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                                cmd.Parameters.AddWithValue("@Name", Name);
                                cmd.Parameters.AddWithValue("@Email", Email);

                                int rowsUpdated = cmd.ExecuteNonQuery();
                                return Json(new { success = true, message = "Employee updated successfully!" });
                            }
                        }
                        else
                        {
                            // INSERT
                        string insertQuery = @"INSERT INTO Employee 
                        (Name, MobileNo, Email, Address, Department, Designation, Active, CreatedAt)
                        VALUES 
                        (@Name, @MobileNo, @Email, @Address, @Department, @Designation, @Active, @CreatedAt)";

                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@Name", Name);
                                cmd.Parameters.AddWithValue("@MobileNo", MobileNo);
                                cmd.Parameters.AddWithValue("@Email", Email);
                                cmd.Parameters.AddWithValue("@Address", Address);
                                cmd.Parameters.AddWithValue("@Department", Department);
                                cmd.Parameters.AddWithValue("@Designation", Designation);
                                cmd.Parameters.AddWithValue("@Active", true);
                                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                                int rowsInserted = cmd.ExecuteNonQuery();
                                return Json(new { success = true, message = "Employee added successfully!" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
