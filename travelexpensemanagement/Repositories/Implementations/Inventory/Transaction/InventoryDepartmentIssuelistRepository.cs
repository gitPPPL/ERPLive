using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.GateEntry.Transaction;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
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

       public async Task<List<InwardEntryDetailDto_Model>> DocDetailsCodeAsync(string docCode)
        {
            var docDetails = new List<InwardEntryDetailDto_Model>();

            if (string.IsNullOrWhiteSpace(docCode))
            {
                return docDetails;
            }

            var globalVar = _globalVariableService.GetGlobalVariables();

            if (globalVar == null)
            {
                throw new Exception("Global variable data is not available.");
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
                            var detail = new InwardEntryDetailDto_Model
                            {
                                Code = reader["Code"] != DBNull.Value ? reader["Code"].ToString() : string.Empty,
                                UUser = reader["UUser"] != DBNull.Value ? reader["UUser"].ToString()  : string.Empty,
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"])  : (DateTime?)null,
                                EUSER = reader["EUSER"] != DBNull.Value ? reader["EUSER"].ToString()  : string.Empty,
                                EDATE = reader["EDATE"] != DBNull.Value  ? Convert.ToDateTime(reader["EDATE"])  : (DateTime?)null,
                                WSID = reader["WSID"] != DBNull.Value  ? reader["WSID"].ToString()  : string.Empty,
                                LIP = reader["LIP"] != DBNull.Value  ? reader["LIP"].ToString() : string.Empty,
                                LID = reader["LID"] != DBNull.Value ? reader["LID"].ToString()  : string.Empty
                            };

                            docDetails.Add(detail);
                        }
                    }
                }
            }

            return docDetails;
        }




        public InventryDepartmentIssue_Model GetDataByCode(string DocID)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();

            InventryDepartmentIssue_Model wrapper = new InventryDepartmentIssue_Model
            {
                Header = new InventryDepartmentIssue_Header(),
                Details = new List<InventryDepartmentIssue_Details>()
            };

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    #region Fetch Header Data
                    using (SqlCommand cmd = new SqlCommand("sp_InventoryDepartmentIssue", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@SaveAction", "Header");
                        cmd.Parameters.AddWithValue("@DOC_ID", DocID);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                wrapper.Header = new InventryDepartmentIssue_Header
                                {
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    V_NO = rdr["V_no"] != DBNull.Value ? Convert.ToInt32(rdr["V_no"]) : 0,
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_DATE = rdr["V_date"] != DBNull.Value ? Convert.ToDateTime(rdr["V_date"]) : DateTime.MinValue,
                                    REMARKS = rdr["REMARKS"]?.ToString()

                                };
                            }
                        }
                    }
                    #endregion

                    #region Fetch Dispatch Data
                    using (SqlCommand cmd4 = new SqlCommand("sp_InventoryDepartmentIssue", con))
                    {
                        cmd4.CommandType = CommandType.StoredProcedure;
                        cmd4.Parameters.AddWithValue("@Action", "ShowData");
                        cmd4.Parameters.AddWithValue("@SaveAction", "Details");
                        cmd4.Parameters.AddWithValue("@DOC_ID", DocID);
                        cmd4.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd4.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd4.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);


                        using (SqlDataReader rdr = cmd4.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Details.Add(new InventryDepartmentIssue_Details
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    MAKE_CODE = rdr["MAKE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MAKE_CODE"]) : 0,
                                    UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                                    NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                                    QTY = rdr["QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY"]) : 0,
                                    RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                                    AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                                    TO_DEPT = rdr["TO_DEPT"] != DBNull.Value ? Convert.ToInt32(rdr["TO_DEPT"]) : 0,
                                    REMARKS = rdr["REMARKS"]?.ToString()
                                });
                            }
                        }
                    }
                    #endregion.
                }

                return wrapper;
            }
            catch (Exception)
            {
                throw;
            }
        }





    }
}