//using Microsoft.Data.SqlClient;
//using travelexpensemanagement.Dbconnection;

//namespace travelexpensemanagement.Middleware.GlobalErrorHandlingMiddleware
//{
//    public class ErrorLoggerService
//    {
//        private readonly DataBaseConnection _dbConnection;

//        public ErrorLoggerService(DataBaseConnection dbConnection)
//        {
//            _dbConnection = dbConnection;
//        }
//        public void LogError(Exception ex, string source)
//        {
//            using var con = _dbConnection.GetErpConnection();
//            var cmd = new SqlCommand(@"
//            INSERT INTO ErrorLog (ErrorMessage, StackTrace, Source, LogDate)
//            VALUES (@ErrorMessage, @StackTrace, @Source, @LogDate)", con);

//            cmd.Parameters.AddWithValue("@ErrorMessage", ex.Message);
//            cmd.Parameters.AddWithValue("@StackTrace", ex.StackTrace ?? "");
//            cmd.Parameters.AddWithValue("@Source", source);
//            cmd.Parameters.AddWithValue("@LogDate", DateTime.Now);

//            con.Open();
//            cmd.ExecuteNonQuery();
//        }
//    }

//}


using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Middleware.GlobalErrorHandlingMiddleware
{
    public class ErrorLoggerService
    {
        private readonly DataBaseConnection _dbConnection;

        public ErrorLoggerService(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public string LogError(Exception ex, HttpContext context, int statusCode, string errorCategory)
        {
            // Generate unique error reference
            string errorReference = $"ERR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20);
            using var con = _dbConnection.GetErpConnection();
            const string query = @"INSERT INTO ErrorLog( ErrorMessage, StackTrace, Source, LogDate, ErrorReference, ErrorType, ErrorCategory, StatusCode, RequestPath)
                VALUES (@ErrorMessage, @StackTrace, @Source, @LogDate, @ErrorReference, @ErrorType, @ErrorCategory, @StatusCode, @RequestPath)";

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@ErrorMessage", ex.Message);
            cmd.Parameters.AddWithValue("@StackTrace", ex.StackTrace ?? string.Empty);
            cmd.Parameters.AddWithValue("@Source", ex.Source ?? string.Empty);
            cmd.Parameters.AddWithValue("@LogDate", DateTime.Now);
            cmd.Parameters.AddWithValue("@ErrorReference", errorReference);
            cmd.Parameters.AddWithValue("@ErrorType", ex.GetType().Name);
            cmd.Parameters.AddWithValue("@ErrorCategory", errorCategory);
            cmd.Parameters.AddWithValue("@StatusCode", statusCode);
            cmd.Parameters.AddWithValue("@RequestPath", context.Request.Path.ToString());
            con.Open();
            cmd.ExecuteNonQuery();
            return errorReference;
        }
    }
}