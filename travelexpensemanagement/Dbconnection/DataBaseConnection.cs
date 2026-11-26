using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Runtime.InteropServices;
using System.Text;

namespace travelexpensemanagement.Dbconnection
{
    public class DataBaseConnection
    {
        private readonly string _conDbConnectionString;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public DataBaseConnection(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _conDbConnectionString = configuration.GetConnectionString("CONDATABASE");
            _httpContextAccessor = httpContextAccessor;
        }

        // CONDATABASE Connection
        public SqlConnection GetConDbConnection()
        {
            return new SqlConnection(_conDbConnectionString);
        }

        // Dynamic ERP Connection
        public SqlConnection GetErpConnection()
        {
            var compCode = _httpContextAccessor.HttpContext?.Session?.GetString("COMP_CODE");

            if (string.IsNullOrEmpty(compCode))
            {
                throw new Exception("COMP_CODE is not available in session.");
            }

            try
            {
                using var con = new SqlConnection(_conDbConnectionString);
                con.Open();

                string query = @"SELECT SERVER_IP, DATABASE_NAME 
                                 FROM Condatabase.dbo.COMP_MAST 
                                 WHERE Code = @Code";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Code", compCode);

                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                {
                    throw new Exception($"No server info found for company code: {compCode}");
                }

                var serverIp = reader["SERVER_IP"]?.ToString();
                var dbName = reader["DATABASE_NAME"]?.ToString();

                if (string.IsNullOrWhiteSpace(serverIp) ||
                    string.IsNullOrWhiteSpace(dbName))
                {
                    throw new Exception("Incomplete server info retrieved.");
                }

                var credentials = GetDbCredentials(serverIp);

                var connectionString =
                    $"Data Source={credentials.ServerName};" +
                    $"Initial Catalog={dbName};" +
                    $"Persist Security Info=True;" +
                    $"User ID={credentials.User};" +
                    $"Password={credentials.Password};" +
                    $"TrustServerCertificate=True;";

                return new SqlConnection(connectionString);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while creating ERP connection: " + ex.Message, ex);
            }
        }

        private (string ServerName, string User, string Password)
            GetDbCredentials(string serverIp)
        {
            return serverIp switch
            {
                "192.168.1.218" => ("192.168.1.218", "sa", "deepak123"),
                //"192.168.20.51" => ("192.168.20.51", "sa", "Pass@123"),
                "192.168.20.52" => ("192.168.20.52", "sa", "Pass@123"),

                _ => throw new Exception("Unknown server IP: " + serverIp)
            };
        }




        // ==================ODBC configuration for ERP connection=================================
        [DllImport("ODBCCP32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "SQLConfigDataSourceW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SQLConfigDataSource(
     IntPtr hwndParent,
     ushort fRequest,
     string lpszDriver,
     string lpszAttributes);

        public void ConfigureErpDsn(string compCode)
        {
            string serverIp, databaseName;

            // Get company server and database
            using (SqlConnection con = GetConDbConnection())
            {
                con.Open();

                string query = @"SELECT SERVER_IP, DATABASE_NAME FROM Condatabase.dbo.COMP_MAST WHERE CODE = @Code";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Code", compCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            throw new Exception("Company details not found.");

                        serverIp = reader["SERVER_IP"].ToString();
                        databaseName = reader["DATABASE_NAME"].ToString();
                    }
                }
            }

            // Get existing server credentials
            var credentials = GetDbCredentials(serverIp);

            string attributes =
    $"DSN=ERP\0" +
    $"SERVER={serverIp}\0" +
    $"DATABASE={databaseName}\0\0";

            bool result = SQLConfigDataSource(
                IntPtr.Zero,
                2, // ODBC_CONFIG_DSN
                "SQL Server",
                attributes);

            if (!result)
            {
                result = SQLConfigDataSource(
                    IntPtr.Zero,
                    1, // ODBC_ADD_DSN
                    "SQL Server",
                    attributes);
            }


            if (!result)
            {
                throw new Exception("ERP DSN Configuration Failed.");
            }
        }

    }
}

//using Microsoft.AspNetCore.Http;
//using Microsoft.Data.SqlClient;
//using Microsoft.Extensions.Configuration;
//using System.Data;
//using travelexpensemanagement.Common.Globalvariable;

//namespace travelexpensemanagement.Dbconnection
//{
//    public class DataBaseConnection
//    {
//        private readonly string _erpConnectionString;
//        private readonly string _conDbConnectionString;
//        private readonly GlobalVariableService _globalValue;
//        private readonly IHttpContextAccessor _httpContextAccessor;
//        public DataBaseConnection(IConfiguration configuration, GlobalVariableService globalValue, IHttpContextAccessor httpContextAccessor)
//        {
//            //_erpConnectionString = configuration.GetConnectionString("ERPDB");
//            _conDbConnectionString = configuration.GetConnectionString("CONDATABASE");
//            _globalValue = globalValue;
//            _httpContextAccessor = httpContextAccessor;
//        }
//        // For ERPDB
//        public SqlConnection GetConDbConnection()
//        {
//            return new SqlConnection(_conDbConnectionString);
//        }
//        public SqlConnection GetErpConnection()
//        {

//            var compCode = _httpContextAccessor.HttpContext?.Session?.GetString("COMP_CODE");
//            //var compCode = _httpContextAccessor.HttpContext?.Session?.GetString("COMP_CODE") ?? "1";

//            if (string.IsNullOrEmpty(compCode))
//            {
//                throw new Exception("COMP_CODE is not available in session.");
//            }
//            try
//            {
//                using var con = new SqlConnection(_conDbConnectionString);
//                con.Open();

//                //string query = @"SELECT SERVER_IP, DATABASE_NAME FROM COMP_MAST WHERE Code = @Code";
//                string query = @"SELECT SERVER_IP, DATABASE_NAME FROM Condatabase.dbo.COMP_MAST WHERE Code = @Code";

//                using var cmd = new SqlCommand(query, con);
//                cmd.Parameters.AddWithValue("@Code", compCode);

//                using var reader = cmd.ExecuteReader();
//                if (!reader.Read())
//                {
//                    throw new Exception($"No server info found for company code: {compCode}");
//                }

//                var serverIp = reader["SERVER_IP"]?.ToString();
//                var dbName = reader["DATABASE_NAME"]?.ToString();

//                if (string.IsNullOrWhiteSpace(serverIp) || string.IsNullOrWhiteSpace(dbName))
//                {
//                    throw new Exception("Incomplete server info retrieved.");
//                }
//                var credentials = GetDbCredentials(serverIp);

//                var connectionString = $"Data Source={credentials.ServerName};Initial Catalog={dbName};" +
//                                       $"Persist Security Info=True;User ID={credentials.User};Password={credentials.Password};" +
//                                       $"TrustServerCertificate=True;";

//                return new SqlConnection(connectionString);
//            }
//            catch (Exception ex)
//            {
//                throw new Exception("Error while creating dynamic ERP connection: " + ex.Message, ex);
//            }
//        }
//        private (string ServerName, string User, string Password) GetDbCredentials(string serverIp)
//        {
//            return serverIp switch
//            {
//                "192.168.1.218" => ("192.168.1.218", "sa", "deepak123"),
//                "192.168.20.51" => ("192.168.20.51", "sa", "Pass@123"),
//                //"118.139.164.161" => ("118.139.164.161", "noida", "Kwalityy@214#"),

//                _ => throw new Exception("Unknown server IP: " + serverIp)
//            };
//        }


//    }
//}
