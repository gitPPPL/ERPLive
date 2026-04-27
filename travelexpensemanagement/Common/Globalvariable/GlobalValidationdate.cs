using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Common.Globalvariable
{
    public class GlobalValidationdate
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly ModuleService.ModuleService _moduleService;

        public GlobalValidationdate(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
        }
        public async Task<ValidationResult> CheckValidDate( string tablename, DateTime vdate, string vtype, string vno)
        {
            try
            {
                var global = _globalVariableService.GetGlobalVariables();
                using (var conn = _dbConnection.GetErpConnection())
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("sp_GlobalCheckValidDate", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@TableName", tablename);
                        cmd.Parameters.AddWithValue("@VDate", vdate);
                        cmd.Parameters.AddWithValue("@VType", vtype);
                        cmd.Parameters.AddWithValue("@VNo", vno);
                        cmd.Parameters.AddWithValue("@CompCode", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", global.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@LoginDate", global.PubLoginDate);
                        cmd.Parameters.AddWithValue("@ServerDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@DateFormat", "dd/MM/yyyy");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new ValidationResult
                                {
                                    Status = Convert.ToBoolean(reader["IsValid"]),
                                    Message = reader["Message"].ToString()
                                };
                            }
                        }
                    }
                }
                return new ValidationResult
                {
                    Status = false,
                    Message = "No response from server"
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }

        public void LogInsertUpdateDelete(string destinationTable, string sourceTable, string transactionType, 
            string codeVNo, string vtype = "", string condition1 = "", string condition2 = "", string condition3 = "")
        {
            var global = _globalVariableService.GetGlobalVariables();
            string imgDatabaseName = "";

            // STEP 1: Get IMGDATABASE_NAME from MAIN DB
            using (var connMain = _dbConnection.GetConDbConnection())
            {
                connMain.Open();
                string imgQuery = "SELECT IMGDATABASE_NAME FROM COMP_MAST WHERE CODE = "+ global.PubCompCode +"";

                using (var cmd = new SqlCommand(imgQuery, connMain))
                {
                    var result = cmd.ExecuteScalar();
                    imgDatabaseName = result?.ToString();
                }
            }
            // STEP 2: ERP DB Operations
            using (var conn = _dbConnection.GetErpConnection())
            {
                conn.Open();
                // Get Column List
                string columnQuery = $@" DECLARE @cols AS NVARCHAR(MAX);
                SELECT @cols = STUFF(( SELECT ',' + name FROM SYS.COLUMNS WHERE OBJECT_ID = OBJECT_ID('{sourceTable}') 
                AND is_computed = 0 FOR XML PATH(''), TYPE).value('.', 'VARCHAR(MAX)'), 1, 1, ''); SELECT @cols; ";
                string columnsList = ExecuteScalar(conn, columnQuery);
                // Check COMP_CODE exists
                bool hasCompCode = CheckExists(conn, $@"SELECT 1  FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{sourceTable}' 
                AND COLUMN_NAME = 'COMP_CODE' ");

                // Build Query
                string mqry = "";

                if (transactionType == "Master")
                {
                    if (hasCompCode)
                    {
                        mqry = $@"INSERT INTO {imgDatabaseName}.dbo.{destinationTable} ({columnsList})
                        SELECT {columnsList} FROM {sourceTable} WHERE Code = {Convert.ToInt32(codeVNo)} AND comp_code = {global.PubCompCode}";
                    }
                    else
                    {
                        mqry = $@" INSERT INTO {imgDatabaseName}.dbo.{destinationTable} ({columnsList})
                        SELECT {columnsList}  FROM {sourceTable} WHERE Code = {Convert.ToInt32(codeVNo)}";
                    }
                }
                else
                {
                    mqry = $@" INSERT INTO {imgDatabaseName}.dbo.{destinationTable} ({columnsList})
                SELECT {columnsList} FROM {sourceTable} WHERE V_No = {Convert.ToInt32(codeVNo)} AND V_Type = '{vtype}'
                AND comp_code = {global.PubCompCode} AND Branch_code = {global.PubBranchCode} AND Year_code = {global.PubFYearCode}";
                }
                // Execute Final Query
                ExecuteNonQuery(conn, mqry);
            }
        }
        private string ExecuteScalar(SqlConnection conn, string query)
        {
            using (var cmd = new SqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
        }
        private bool CheckExists(SqlConnection conn, string query)
        {
            using (var cmd = new SqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result != null;
            }
        }
        private void ExecuteNonQuery(SqlConnection conn, string query)
        {
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
        public class ValidationResult
        {
            public bool Status { get; set; }
            public string Message { get; set; }
        }
        // How to call any page 
        //_globalValidationdate.LogInsertUpdateDelete(destinationTable: "gate1", sourceTable: "gate1",  transactionType: "Transaction",
        //        codeVNo: "262700001", vtype: "INFU");
    }
}