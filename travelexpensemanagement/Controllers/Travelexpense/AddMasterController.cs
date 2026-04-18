using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;
using Microsoft.AspNetCore.Authorization;

namespace travelexpensemanagement.Controllers.Travelexpense
{
    [Route("AddMaster")]
    public class AddMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        public AddMasterController(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        [HttpGet("")]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            //return View();
            return View("Index");
        }

        //[HttpGet("GetAllTables")]
        //public JsonResult GetAllTables()
        //{
        //    List<string> tableNames = new List<string>();
        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        con.Open();
        //        string query = "SELECT name FROM AllMasters";
        //        using (SqlCommand cmd = new SqlCommand(query, con))
        //        {
        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    tableNames.Add(reader["name"].ToString());
        //                }
        //            }
        //        }
        //    }

        //    return Json(new { success = true, tables = tableNames });
        //}
        // This code is correct 
        //[HttpGet("GetColumns")]
        //public JsonResult GetColumns(string tableName)
        //{
        //    List<string> columns = new List<string>();

        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        string query = $@"
        //    SELECT COLUMN_NAME 
        //    FROM INFORMATION_SCHEMA.COLUMNS 
        //    WHERE TABLE_NAME = @TableName 
        //    AND COLUMNPROPERTY(OBJECT_ID(@TableName), COLUMN_NAME, 'IsIdentity') = 0";

        //        using (SqlCommand cmd = new SqlCommand(query, con))
        //        {
        //            cmd.Parameters.AddWithValue("@TableName", tableName);
        //            con.Open();
        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    columns.Add(reader["COLUMN_NAME"].ToString());
        //                }
        //            }
        //        }
        //    }
        //    return Json(new { success = true, columns = columns });
        //}
        //[HttpPost("SaveData")]
        //public IActionResult SaveData()
        //{
        //    string tableName = Request.Form["TableName"];
        //    var formKeys = Request.Form.Keys.Where(k => k != "TableName");

        //    var columns = new List<string>();
        //    var values = new List<string>();

        //    foreach (var key in formKeys)
        //    {
        //        string value = Request.Form[key];
        //        columns.Add($"[{key}]");
        //        values.Add($"'{value.Replace("'", "''")}'"); // SQL Injection prevention
        //    }
        //    string insertQuery = $"INSERT INTO {tableName} ({string.Join(",", columns)}) VALUES ({string.Join(",", values)})";
        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        con.Open();
        //        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
        //        {
        //            try
        //            {
        //                cmd.ExecuteNonQuery();
        //                return Json(new { success = true });
        //            }
        //            catch (Exception ex)
        //            {
        //                return Json(new { success = false, message = ex.Message });
        //            }
        //        }
        //    }
        //}
    }
}

