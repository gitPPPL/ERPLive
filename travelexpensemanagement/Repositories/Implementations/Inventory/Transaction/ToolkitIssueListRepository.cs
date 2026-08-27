using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class ToolkitIssueListRepository : IToolkitIssueListRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalExcelExport _excel;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        public ToolkitIssueListRepository(GlobalVariableService globalVariableService, DataBaseConnection dbConnection
            , GlobalExcelExport excel, DbHelper dbHelper, GlobalValidationdate globalValidationdate)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _excel = excel;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
        }
        public RepositoryResponseList<ToolKitIssueModel> GetAllToolkitIssue(string docType, string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<ToolKitIssueModel>();
            int totalcount = 0;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_ToolKitIssueEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", docType);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                data.Add(new ToolKitIssueModel
                                {
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_TYPE_NAME = reader["vtypeName"]?.ToString(),
                                    V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]),
                                    V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]),
                                    PLACE_CODE = reader["PLACE_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["PLACE_CODE"]),
                                    PLACE_NAME = reader["PLACENAME"]?.ToString(),
                                    ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["ITEM_CODE"]),
                                    ITEM_NAME = reader["ITEMNAME"]?.ToString(),
                                    QTY = reader["QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["QTY"]),
                                    RATE = reader["RATE"] == DBNull.Value ? null : Convert.ToDecimal(reader["RATE"]),
                                    AMOUNT = reader["AMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["AMOUNT"]),
                                    EMP_CODE = reader["EMP_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["EMP_CODE"]),
                                    EMP_NAME = reader["EMPNAME"]?.ToString(),
                                    FROM_DEPT = reader["FROM_DEPT"] == DBNull.Value ? null : Convert.ToInt32(reader["FROM_DEPT"]),
                                    FROM_DEPT_NAME = reader["FROMDEPTNAME"]?.ToString(),
                                    TO_DEPT = reader["TO_DEPT"] == DBNull.Value ? null : Convert.ToInt32(reader["TO_DEPT"]),
                                    TO_DEPT_NAME = reader["TODEPTNAME"]?.ToString(),
                                    RECD_QTY = reader["RECD_QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["RECD_QTY"]),
                                    DR_AMOUNT = reader["DR_AMOUNT"] == DBNull.Value ? null : Convert.ToDecimal(reader["DR_AMOUNT"]),
                                    UUSER = reader["UUSER"]?.ToString(),
                                    UDATE = reader["UDATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["UDATE"]),
                                    REMARK = reader["REMARK"]?.ToString()
                                });
                            }

                            if (reader.NextResult())
                            {
                                if (reader.Read())
                                {
                                    totalcount = reader["TotalCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalCount"]);
                                }
                            }
                        }
                    }
                }
                return new RepositoryResponseList<ToolKitIssueModel> { status = true, data = data, totalCount = totalcount};
            }
            catch (Exception ex)
            {
                return new RepositoryResponseList<ToolKitIssueModel> { status = false, message = ex.Message };
            }
        }

        [HttpPost]
        public RepositoryResponse Delete(int vNo, string docType)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_ToolKitIssueEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", docType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        cmd.ExecuteNonQuery();
                    }
                }

                return new RepositoryResponse { status = true, message = "Deleted Successfully!" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = true, message = ex.Message };
            }
        }

    }
}
