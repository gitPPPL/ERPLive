using DocumentFormat.OpenXml.Office2021.Drawing.Livefeed;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Updatetable
{
    public class UpdatetablesController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly string _connectionString = "Data Source=118.139.164.161;Initial Catalog=Hrms_db;Persist Security Info=True;User ID=noida;Password=Kwalityy@214#;Trust Server Certificate=True";

        public UpdatetablesController(IWebHostEnvironment env, DataBaseConnection db, DataBaseConnection dbConnectionlocal, GlobalVariableService globalVariableService)
        {
            _env = env;
            _dbConnection = db;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            return View();
        }
        //Live to Local
        //[HttpPost]
        //public IActionResult UpdateTablesData()
        //{
        //    try
        //    {
        //        using (SqlConnection conLive = new SqlConnection(_connectionString))
        //        using (SqlConnection conLocal = _dbConnection.GetErpConnection())
        //        {
        //            conLive.Open();
        //            conLocal.Open();

        //            // Get Live Tables
        //            List<string> liveTables = new List<string>();

        //            using (SqlCommand cmdLiveTables = new SqlCommand("SELECT name FROM sys.tables", conLive))
        //            using (SqlDataReader reader = cmdLiveTables.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    liveTables.Add(reader["name"].ToString());
        //                }
        //            }

        //            foreach (var tableName in liveTables)
        //            {
        //                // Check table exists in Local
        //                using (SqlCommand checkCmd = new SqlCommand(
        //                    "SELECT COUNT(*) FROM sys.tables WHERE name = @TableName",
        //                    conLocal))
        //                {
        //                    checkCmd.Parameters.AddWithValue("@TableName", tableName);

        //                    int exists = (int)checkCmd.ExecuteScalar();

        //                    if (exists > 0)
        //                    {
        //                        using (SqlTransaction transaction = conLocal.BeginTransaction())
        //                        {
        //                            try
        //                            {
        //                                // Disable Constraints
        //                                SqlCommand disableCmd = new SqlCommand(
        //                                    $"ALTER TABLE [{tableName}] NOCHECK CONSTRAINT ALL",
        //                                    conLocal, transaction);
        //                                disableCmd.ExecuteNonQuery();

        //                                // Try TRUNCATE first
        //                                try
        //                                {
        //                                    SqlCommand truncateCmd = new SqlCommand(
        //                                        $"TRUNCATE TABLE [{tableName}]",
        //                                        conLocal, transaction);
        //                                    truncateCmd.ExecuteNonQuery();
        //                                }
        //                                catch
        //                                {
        //                                    // If TRUNCATE fails, use DELETE
        //                                    SqlCommand deleteCmd = new SqlCommand(
        //                                        $"DELETE FROM [{tableName}]",
        //                                        conLocal, transaction);
        //                                    deleteCmd.ExecuteNonQuery();
        //                                }

        //                                // Get data from Live
        //                                DataTable dt = new DataTable();
        //                                using (SqlDataAdapter da = new SqlDataAdapter(
        //                                    $"SELECT * FROM [{tableName}]",
        //                                    conLive))
        //                                {
        //                                    da.Fill(dt);
        //                                }

        //                                if (dt.Rows.Count > 0)
        //                                {
        //                                    using (SqlBulkCopy bulk = new SqlBulkCopy(
        //                                        conLocal,
        //                                        SqlBulkCopyOptions.KeepIdentity,
        //                                        transaction))
        //                                    {
        //                                        bulk.DestinationTableName = tableName;

        //                                        // Column Mapping
                                                  // bulk.bulkcopytimeout = 0;   
        //                                        foreach (DataColumn col in dt.Columns)
        //                                        {
        //                                            bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        //                                        }

        //                                        bulk.WriteToServer(dt);
        //                                    }
        //                                }

        //                                // Enable Constraints
        //                                SqlCommand enableCmd = new SqlCommand(
        //                                    $"ALTER TABLE [{tableName}] CHECK CONSTRAINT ALL",
        //                                    conLocal, transaction);
        //                                enableCmd.ExecuteNonQuery();

        //                                transaction.Commit();
        //                            }
        //                            catch
        //                            {
        //                                transaction.Rollback();
        //                                throw;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        return Json(new { message = "Live Data Successfully Synced to Local" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = ex.Message });
        //    }
        //}

        // local to live

        [HttpPost]
        public IActionResult UpdateTablesData()
        {
            try
            {
                using (SqlConnection conLive = new SqlConnection(_connectionString))
                using (SqlConnection conLocal = _dbConnection.GetErpConnection())
                {
                    conLive.Open();
                    conLocal.Open();

                    // Get Local Tables
                    List<string> localTables = new List<string>();

                    //using (SqlCommand cmdLocalTables = new SqlCommand("SELECT name FROM sys.tables", conLocal))
                    using (SqlCommand cmdLocalTables = new SqlCommand("SELECT name FROM sys.tables where name in('EmpPortalLogin','PAY_ATTEN', 'PAY_TIMEDATA' ,'EMP_MAST' ,'DESG_MAST' ,'DEPT_MAST' ,'HOLIDAY_MAST' ,'PAY_SALARY' ,'PAY_LEAVE' ,'approval_status')", conLocal))
                    using (SqlDataReader reader = cmdLocalTables.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            localTables.Add(reader["name"].ToString());
                        }
                    }
                    foreach (var tableName in localTables)
                    {
                        // Check table exists in Live
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = @TableName", conLive))
                        {
                            checkCmd.Parameters.AddWithValue("@TableName", tableName);
                            int exists = (int)checkCmd.ExecuteScalar();
                            if (exists > 0)
                            {
                                using (SqlTransaction transaction = conLive.BeginTransaction())
                                {
                                    try
                                    {
                                        // Disable Constraints in Live
                                        SqlCommand disableCmd = new SqlCommand(
                                            $"ALTER TABLE [{tableName}] NOCHECK CONSTRAINT ALL",
                                            conLive, transaction);
                                        disableCmd.ExecuteNonQuery();
                                        // Try TRUNCATE first
                                        try
                                        {
                                            SqlCommand truncateCmd = new SqlCommand(
                                                $"TRUNCATE TABLE [{tableName}]",
                                                conLive, transaction);
                                            truncateCmd.ExecuteNonQuery();
                                        }
                                        catch
                                        {
                                            SqlCommand deleteCmd = new SqlCommand(
                                                $"DELETE FROM [{tableName}]",
                                                conLive, transaction);
                                            deleteCmd.ExecuteNonQuery();
                                        }
                                        // Get data from Local
                                        DataTable dt = new DataTable();
                                        using (SqlDataAdapter da = new SqlDataAdapter(
                                            $"SELECT * FROM [{tableName}]",
                                            conLocal))
                                        {
                                            da.Fill(dt);
                                        }
                                        if (dt.Rows.Count > 0)
                                        {
                                            using (SqlBulkCopy bulk = new SqlBulkCopy(
                                                conLive,
                                                SqlBulkCopyOptions.KeepIdentity,
                                                transaction))
                                            {
                                                bulk.DestinationTableName = tableName;
                                                bulk.BulkCopyTimeout = 0;   // ✅ Unlimited wait
                                                foreach (DataColumn col in dt.Columns)
                                                {
                                                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                                }
                                                bulk.WriteToServer(dt);
                                            }
                                        }
                                        // Enable Constraints
                                        SqlCommand enableCmd = new SqlCommand(
                                            $"ALTER TABLE [{tableName}] CHECK CONSTRAINT ALL",
                                            conLive, transaction);
                                        enableCmd.ExecuteNonQuery();

                                        transaction.Commit();
                                    }
                                    catch
                                    {
                                        transaction.Rollback();
                                        throw;
                                    }
                                }
                            }
                        }
                    }
                }
                return Json(new { message = "Local Data Successfully Synced to Live" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

    }
}
