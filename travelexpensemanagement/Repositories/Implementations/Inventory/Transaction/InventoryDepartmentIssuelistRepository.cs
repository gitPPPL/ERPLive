using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.GateEntry.Transaction;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;


namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class InventoryDepartmentIssuelistRepository : IInventoryDepartmentIssueListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;

        public InventoryDepartmentIssuelistRepository(  DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
        }

        public async Task<(List<InventryDepartmentIssue_Header> Lists, int TotalCount)> GetListAsync(string searchTerm = "", int pageNumber = 1, int pageSize = 10 , string FormName = "")
        {
            var globalData = _globalVariableService.GetGlobalVariables();

            if (globalData == null)
            {
                throw new Exception("Global variable data is null.");
            }

            var headerList = new List<InventryDepartmentIssue_Header>();
            int totalCount = 0;

            using var conn = _dbConnection.GetErpConnection();

            using var cmd = new SqlCommand("sp_InventoryDepartmentIssue", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@Action", SqlDbType.VarChar).Value = "SELECT";
            cmd.Parameters.Add("@SearchTerm", SqlDbType.VarChar).Value = string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm;
            cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
            cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalData.PubCompCode;
            cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = globalData.PubFYearCode;
            cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = globalData.PubBranchCode;
            cmd.Parameters.Add("@FormName", SqlDbType.NVarChar).Value = FormName;

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                headerList.Add(new InventryDepartmentIssue_Header
                {
                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                    PORD_NO = reader["PORD_NO"] != DBNull.Value ? Convert.ToInt32(reader["PORD_NO"]) : 0,
                    DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                    V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                    SLIP_NO = reader["SLIP_NO"] != DBNull.Value ? reader["SLIP_NO"].ToString() : string.Empty,
                    PORD_TYPE = reader["PORD_TYPE"] != DBNull.Value ? reader["PORD_TYPE"].ToString() : string.Empty,
                    REMARKS = reader["REMARKS"] != DBNull.Value ? reader["REMARKS"].ToString() : string.Empty,
                    SHIFT = reader["SHIFT"] != DBNull.Value ? reader["SHIFT"].ToString() : string.Empty,
                    StatusText = reader["StatusText"] != DBNull.Value ? reader["StatusText"].ToString() : string.Empty,
                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null
                });
            }

            // Second result set contains TotalCount
            if (await reader.NextResultAsync())
            {
                if (await reader.ReadAsync())
                {
                    totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                }
            }

            return (headerList, totalCount);
        }
               
        
        public async Task<bool> DeleteAsync(string docId, int V_NO, string V_TYPE)
        {
            if (string.IsNullOrWhiteSpace(docId))
            {
                return false;
            }

            var getGlobalCode = _globalVariableService.GetGlobalVariables();

            if (getGlobalCode == null)
            {
                throw new Exception("Global variable data is not available.");
            }

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand( "sp_InventoryDepartmentIssue", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@Action", SqlDbType.VarChar).Value = "DELETE";
                    cmd.Parameters.Add("@DOC_ID", SqlDbType.VarChar).Value = docId;
                    cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = V_NO;
                    cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = V_TYPE;
                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = getGlobalCode.PubCompCode;
                    cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = getGlobalCode.PubFYearCode;
                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = getGlobalCode.PubBranchCode;

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return true;
        }


        public async Task<List<InwardEntryDetailDto>> DocDetailsCodeAsync(string docCode)
        {
            var docDetails = new List<InwardEntryDetailDto>();

            if (string.IsNullOrWhiteSpace(docCode))
            {
                return docDetails;
            }

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InventoryOpening", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@Action", SqlDbType.VarChar).Value = "DocDetailID";
                    cmd.Parameters.Add("@DOC_ID", SqlDbType.VarChar).Value = docCode;

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var detail = new InwardEntryDetailDto
                            {
                                Code = reader["Code"] != DBNull.Value
                                    ? reader["Code"].ToString()
                                    : null,

                                UUser = reader["UUser"] != DBNull.Value
                                    ? reader["UUser"].ToString()
                                    : null,

                                UDATE = reader["UDATE"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["UDATE"])
                                    : (DateTime?)null,

                                EUSER = reader["EUSER"] != DBNull.Value
                                    ? reader["EUSER"].ToString()
                                    : null,

                                EDATE = reader["EDATE"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["EDATE"])
                                    : (DateTime?)null,

                                WSID = reader["WSID"] != DBNull.Value
                                    ? reader["WSID"].ToString()
                                    : null,

                                LIP = reader["LIP"] != DBNull.Value
                                    ? reader["LIP"].ToString()
                                    : null,

                                LID = reader["LID"] != DBNull.Value
                                    ? reader["LID"].ToString()
                                    : null
                            };

                            docDetails.Add(detail);
                        }
                    }
                }
            }

            return docDetails;
        }


        public class InwardEntryDetailDto
        {
            public string? Code { get; set; }
            public string? UUser { get; set; }
            public DateTime? UDATE { get; set; }
            public string? EUSER { get; set; }
            public DateTime? EDATE { get; set; }
            public string? WSID { get; set; }
            public string? LIP { get; set; }
            public string? LID { get; set; }
        }

    }
}