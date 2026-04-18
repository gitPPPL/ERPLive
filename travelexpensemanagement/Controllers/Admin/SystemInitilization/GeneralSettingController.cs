using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System;
using System.Collections.Generic;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    public class GeneralSettingController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public GeneralSettingController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService,
            travelexpensemanagement.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Admin/SystemInitilization/GeneralSetting/Index.cshtml");
        }
        // Main action to update SQL schema
        [HttpPost]
        public IActionResult UpdateSQLSchema()
        {
            var serverCredentials = new List<Tuple<string, string, string>>()
            {
                new Tuple<string, string, string>("192.168.20.51", "PASHUPATI_E", "Pass@123"),
                new Tuple<string, string, string>("192.168.20.51", "PASHUPATI_L", "Pass@123"),
                new Tuple<string, string, string>("192.168.20.53", "PASHUPATI_SR", "Pass@123"),
                new Tuple<string, string, string>("192.168.20.53", "PASHUPATI_ST", "Pass@123")
            };

            bool credentialsMatched = false;

            foreach (var credential in serverCredentials)
            {
                if (CheckCredentials(credential.Item1, credential.Item2, credential.Item3))
                {
                    credentialsMatched = true;
                    break; 
                }
            }
            if (!credentialsMatched)
            {
                return BadRequest(new { success = false, message = "Credentials do not match." });
            }

            // If credentials match, proceed to copy stored procedures
            bool updateSuccessful = true;

            var targetDatabases = new List<string> { "PASHUPATI_E", "PASHUPATI_L", "PASHUPATI_SR", "PASHUPATI_ST" };
            foreach (var targetDatabase in targetDatabases)
            {
                if (!CopyStoredProcedures("ERPDB", targetDatabase))
                {
                    updateSuccessful = false;
                    break; 
                }
            }
            if (updateSuccessful)
            {
                return Ok(new { success = true, message = "Stored Procedures updated successfully!" });
            }
            else
            {
                return BadRequest(new { success = false, message = "Error occurred while updating stored procedures." });
            }
        }
        // Method to check credentials in COMP_MAST table
        private bool CheckCredentials(string ServerIP, string DatabaseName, string DatabasePassword)
        {
            bool credentialsMatched = false;
            using (SqlConnection conn = new SqlConnection(_dbConnection.GetConDbConnection().ConnectionString))
            {
                conn.Open();

                string checkQuery = @"
                    SELECT COUNT(1)
                    FROM COMP_MAST
                    WHERE Server_IP = @ServerIP
                    AND DATABASE_NAME = @DatabaseName
                    AND DATABASE_PASS = @DatabasePassword";

                SqlCommand checkCmd = new SqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@ServerIP", ServerIP);
                checkCmd.Parameters.AddWithValue("@DatabaseName", DatabaseName);
                checkCmd.Parameters.AddWithValue("@DatabasePassword", DatabasePassword);
                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (count > 0)
                {
                    credentialsMatched = true;
                }
            }
            return credentialsMatched;
        }
        // Method to copy stored procedures from ERPDB to PASHUPATI_E
        private bool CopyStoredProcedures(string sourceDatabase, string targetDatabase)
        {
            bool result = false;
            using (SqlConnection sourceConn = new SqlConnection(_dbConnection.GetErpConnection().ConnectionString))
            {
                sourceConn.Open();

                string query = "USE " + sourceDatabase + "; SELECT name FROM sys.procedures;";
                SqlCommand cmd = new SqlCommand(query, sourceConn);

                try
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string procedureName = reader.GetString(0);
                        string procedureText = GetProcedureText(procedureName, sourceDatabase);

                        if (!string.IsNullOrEmpty(procedureText))
                        {
                            // Create the procedure in the target database
                            CreateProcedureInTargetDatabase(procedureName, procedureText, targetDatabase);
                            result = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error fetching procedures from ERPDB: " + ex.Message);
                }
            }

            return result;
        }
        private string GetProcedureText(string procedureName, string sourceDatabase)
        {
            string procedureText = string.Empty;

            using (SqlConnection conn = new SqlConnection(_dbConnection.GetErpConnection().ConnectionString))
            {
                conn.Open();

                string query = "USE " + sourceDatabase + "; EXEC sp_helptext @ProcedureName;";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ProcedureName", procedureName);

                try
                {
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        procedureText += reader["Text"].ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error fetching procedure text: " + ex.Message);
                }
            }
            return procedureText;
        }
        private void CreateProcedureInTargetDatabase(string procedureName, string procedureText, string targetDatabase)
        {
            using (SqlConnection targetConn = new SqlConnection(_dbConnection.GetErpConnection().ConnectionString))
            {
                targetConn.Open();

                // Check if the procedure exists, only drop if it does
                string checkProcedureQuery = @" IF OBJECT_ID('" + targetDatabase + ".dbo." + procedureName + "', 'P') IS NOT NULL BEGIN  DROP PROCEDURE " + procedureName + @" END";
        
                SqlCommand dropCmd = new SqlCommand(checkProcedureQuery, targetConn);
                dropCmd.ExecuteNonQuery();

                string useDatabaseQuery = "USE " + targetDatabase + ";";
                SqlCommand useDbCmd = new SqlCommand(useDatabaseQuery, targetConn);
                useDbCmd.ExecuteNonQuery();

                // Create the stored procedure in the target database
                string createProcedureQuery = @"
                  CREATE PROCEDURE " + procedureName + @"
                  @code NVARCHAR(50)
                  AS
                  BEGIN
                      DELETE FROM DOC_NUMBER 
                      WHERE CAST(COMP_CODE AS VARCHAR) + CAST(YEAR_CODE AS VARCHAR) + CAST(V_TYPE AS VARCHAR) = @code;
                  END";

                  SqlCommand createCmd = new SqlCommand(createProcedureQuery, targetConn);
                  createCmd.ExecuteNonQuery();
            }
        }
    }
}
