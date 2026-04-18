using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.Travelexpense
{
    public class EmployeeDetailsListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        public EmployeeDetailsListController(DataBaseConnection dbConnection)
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
        public JsonResult GetPagedTravelRequests(int page = 1, int pageSize = 10)
            {
            try
            {
                List<object> EmployeeList = new List<object>();
                int totalCount = 0;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string countQuery = "SELECT COUNT(*) FROM TravelRequest";
                    using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                    {
                        totalCount = (int)countCmd.ExecuteScalar();
                    }
                    string query = $@"
    SELECT EmpID, Name, MobileNo, Email, Address, Department, Designation, CreatedAt AS Date
    FROM (
        SELECT *, ROW_NUMBER() OVER (ORDER BY EmpID DESC) AS RowNum
        FROM Employee
    ) AS RowConstrainedResult
    WHERE RowNum >= {(page - 1) * pageSize + 1} AND RowNum <= {page * pageSize}
    ORDER BY EmpID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                EmployeeList.Add(new
                                {
                                    EmpID = reader["EmpID"],
                                    Name = reader["Name"]?.ToString(),
                                    MobileNo = reader["MobileNo"]?.ToString(),
                                    Email = reader["Email"]?.ToString(),
                                    Address = reader["Address"]?.ToString(),
                                    Department = reader["Department"]?.ToString(),
                                    Designation = reader["Designation"]?.ToString(),
                                    Date = reader["Date"] != DBNull.Value? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"): ""
                                });
                            }
                        }
                    }
                }
                return Json(new { success = true, EmployeeList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public JsonResult GetDepartmentsList()
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
        public JsonResult GetDesignationList()
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

        //Update start Block
        [HttpPost]
        public JsonResult UpdateEmployeeDetailsList([FromBody] EmployeeDetailsList model)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string updateQuery = @"
            UPDATE Employee SET
                Name = @Name,
                MobileNo = @MobileNo,
                Email = @Email,
                Address = @Address,
                Department = @Department,
                Designation = @Designation
            WHERE EmpID = @EmpID"; // Removed the extra comma here

                    using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                    {
                        // Corrected the parameter names to match the SQL query
                        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MobileNo", model.MobileNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", model.Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Address", model.Address ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Department", model.Department ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Designation", model.Designation ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EmpID", model.EmpID); // Assuming EmpID is in the model

                        int rows = cmd.ExecuteNonQuery();
                        return Json(new { success = rows > 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //Update End Block
    }
}
