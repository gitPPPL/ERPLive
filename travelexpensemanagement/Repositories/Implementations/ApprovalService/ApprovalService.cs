using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces;

namespace travelexpensemanagement.Repositories.Implementations
{
    public class ApprovalService : IApprovalService
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public ApprovalService(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public async Task<string> GetApprovalStatus(string vType, int vNo, string tableName)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using var conn = _dbConnection.GetErpConnection();

            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand(
                "sp_CheckApprovalStatus",
                conn);

            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@V_NO", vNo);
            cmd.Parameters.AddWithValue("@V_TYPE", vType);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);

            var result = await cmd.ExecuteScalarAsync();

            //return result?.ToString() ?? "SendForApproval";
            return result?.ToString() ?? "NullData";
        }

    }
}


//using Microsoft.Data.SqlClient;
//using System.Data;
//using travelexpensemanagement.Common.Globalvariable;
//using travelexpensemanagement.Dbconnection;
//using travelexpensemanagement.Repositories.Interfaces;

//namespace travelexpensemanagement.Repositories.Implementations
//{
//    public class ApprovalService : IApprovalService
//    {
//        private readonly DataBaseConnection _dbConnection;
//        private readonly GlobalVariableService _globalVariableService;

//        public ApprovalService(
//            DataBaseConnection dbConnection,
//            GlobalVariableService globalVariableService)
//        {
//            _dbConnection = dbConnection;
//            _globalVariableService = globalVariableService;
//        }

//        public async Task<string> GetApprovalStatus(string vType, int vNo, string tableName)
//        {
//            var gv = _globalVariableService.GetGlobalVariables();

//            using var conn = _dbConnection.GetErpConnection();
//            await conn.OpenAsync();

//            using SqlCommand cmd = new SqlCommand("sp_CheckApprovalStatus", conn);
//            cmd.CommandType = CommandType.StoredProcedure;

//            cmd.Parameters.AddWithValue("@V_NO", vNo);
//            cmd.Parameters.AddWithValue("@V_TYPE", vType);
//            cmd.Parameters.AddWithValue("@TableName", tableName);
//            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
//            cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
//            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);

//            var result = await cmd.ExecuteScalarAsync();

//            return result?.ToString() ?? "NoAction";
//        }
//    }
//}