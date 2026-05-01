
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Repositories.Interfaces;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class InwardEntryRepository : IInwardEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public InwardEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public async Task<string> GetVNoAsync(string vType, string tableName = "GATE1")
        {
            string newV_NO = "00000";

            try
            {
                var globalData = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // 🔹 1. Get Prefix Year
                    string prefixQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                    string prefixYR = "00";
                    using (SqlCommand prefixCmd = new SqlCommand(prefixQuery, con))
                    {
                        prefixCmd.Parameters.AddWithValue("@YearCode", globalData.PubFYearCode);

                        var result = await prefixCmd.ExecuteScalarAsync();
                        if (result != null)
                            prefixYR = result.ToString();
                    }

                    if (tableName != "GATE1")
                        throw new Exception("Invalid table name");

                    string lastVNoQuery = $@"
                    SELECT MAX(CAST(V_NO AS INT)) 
                    FROM {tableName}
                    WHERE COMP_CODE = @CompCode
                    AND YEAR_CODE = @YearCode
                    AND BRANCH_CODE = @BranchCode
                    AND V_TYPE = @Vtype";

                    using (SqlCommand cmd = new SqlCommand(lastVNoQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", globalData.PubCompCode);
                        cmd.Parameters.AddWithValue("@YearCode", globalData.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BranchCode", globalData.PubBranchCode);
                        cmd.Parameters.AddWithValue("@Vtype", vType);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            int lastNo = Convert.ToInt32(result);
                            newV_NO = (lastNo + 1).ToString("D5");
                        }
                        else
                        {
                            newV_NO = prefixYR + "00001";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating V_NO", ex);
            }

            return newV_NO;
        }


    }
}
