using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class InsuranceCoverNoteListRepository : IInsuranceCoverNoteListRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public InsuranceCoverNoteListRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
        }

        public async Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                if (pageNo < 1)
                    pageNo = 1;

                if (pageSize < 1)
                    pageSize = 20;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using SqlCommand cmd = new SqlCommand("[sp_Sale3]", con);

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@PageNo", pageNo);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", searchTerm?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Action", "GetList");

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    int totalCount = 0;

                    var list = new List<object>();

                    if (await reader.ReadAsync())
                    {
                        if (reader["TotalRecords"] != DBNull.Value)
                        {
                            totalCount = Convert.ToInt32(reader["TotalRecords"]);
                        }
                    }

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
                                vType = reader["V_TYPE"] == DBNull.Value ? "" : reader["V_TYPE"].ToString(),
                                vDate = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                docId = reader["docId"] == DBNull.Value ? "" : reader["docId"].ToString(),
                                insuranceCode = reader["InsuranceCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InsuranceCode"]),
                                insuranceName = reader["InsuranceName"] == DBNull.Value ? "" : reader["InsuranceName"].ToString(),
                                billName = reader["BillName"] == DBNull.Value ? "" : reader["BillName"].ToString(),
                                billType = reader["BILL_TYPE"] == DBNull.Value ? "" : reader["BILL_TYPE"].ToString(),
                                fromBillNo = reader["F_BILL_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["F_BILL_NO"]),
                                toBillNo = reader["T_BILL_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["T_BILL_NO"]),
                                itemName = reader["ITEM_NAME"] == DBNull.Value ? "" : reader["ITEM_NAME"].ToString(),
                            });
                        }
                    }

                    return new { success = true, data = list, pageNo = pageNo, pageSize = pageSize, totalCount = totalCount, hasMore = (pageNo * pageSize) < totalCount };
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> DeleteDataAsync(int vNo, string vType)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using SqlTransaction transaction =
                    (SqlTransaction)await con.BeginTransactionAsync();

                try
                {
                    string? billType = null;

                    using (SqlCommand billCmd = new SqlCommand(@"
                    SELECT BILL_TYPE
                    FROM SALE3
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con, transaction))
                    {
                        billCmd.Parameters.AddWithValue("@V_TYPE", vType);
                        billCmd.Parameters.AddWithValue("@V_NO", vNo);
                        billCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                        billCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                        billCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);

                        billType = (await billCmd.ExecuteScalarAsync())?.ToString();
                    }

                    // 2. Delete SALE3 using SP
                    using (SqlCommand cmd = new SqlCommand("sp_Sale3", con, transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DeleteData");
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    using (SqlCommand updateCmd = new SqlCommand(@"
                    UPDATE SALE1
                    SET INSU_NO = NULL
                    WHERE COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE
                      AND INSU_NO = @V_NO
                      AND V_TYPE = @BILL_TYPE", con, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                        updateCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                        updateCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                        updateCmd.Parameters.AddWithValue("@V_NO", vNo);
                        updateCmd.Parameters.AddWithValue("@BILL_TYPE", (object?)billType ?? DBNull.Value);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();

                    return new
                    {
                        success = true,
                        message = "Data deleted successfully."
                    };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    return new
                    {
                        success = false,
                        message = ex.Message
                    };
                }
            }
        }

    }
}
