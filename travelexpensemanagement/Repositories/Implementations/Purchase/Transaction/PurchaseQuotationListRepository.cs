using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseQuotationListRepository : IPurchaseQuotationListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        public PurchaseQuotationListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, GlobalValidationdate globalValidationdate,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }

        public async Task<(List<QUOTATION1> Quotations, int TotalCount)> GetAllQuotationsAsync(string searchTerm, int pageNumber, int pageSize)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            var quotations = new List<QUOTATION1>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QUOTATION1_MGMT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);

                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            quotations.Add(new QUOTATION1
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                PARTY_CODE = reader["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARTY_CODE"]) : 0,
                                QUOTE_NO = reader["QUOTE_NO"]?.ToString(),
                                QUOTE_DATE = reader["QUOTE_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["QUOTE_DATE"]) : DateTime.MinValue,
                                CONT_PERSON = reader["CONT_PERSON"]?.ToString(),
                                VALID_DATE = reader["VALID_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["VALID_DATE"]) : DateTime.MinValue,
                                REMARKS = reader["REMARKS"]?.ToString(),
                                STATUS_NAME = reader["STATUS_NAME"]?.ToString(),
                                PARTY_NAME = reader["NAME"]?.ToString(),
                            });
                        }

                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }

            return (quotations, totalCount);
        }

        public async Task<QUOTATION1?> GetQuotationByCodeAsync(int vNo, string vType)
        {
            QUOTATION1? quotation = null;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QUOTATION1_MGMT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@V_TYPE", string.IsNullOrWhiteSpace(vType) ? (object)DBNull.Value : vType);

                    await conn.OpenAsync();

                    using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        if (await rdr.ReadAsync())
                        {
                            quotation = new QUOTATION1
                            {
                                V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                V_TYPE = rdr["V_TYPE"]?.ToString(),
                                V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : 0,
                                QUOTE_NO = rdr["QUOTE_NO"]?.ToString(),
                                QUOTE_DATE = rdr["QUOTE_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["QUOTE_DATE"]) : DateTime.MinValue,
                                CONT_PERSON = rdr["CONT_PERSON"]?.ToString(),
                                VALID_DATE = rdr["VALID_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["VALID_DATE"]) : DateTime.MinValue,
                                REMARKS = rdr["REMARKS"]?.ToString(),
                                STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : 0,
                                UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                                UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                                EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                                EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                                AED = rdr["AED"]?.ToString(),
                                WSID = rdr["WSID"]?.ToString(),
                                LIP = rdr["LIP"]?.ToString(),
                                LID = rdr["LID"]?.ToString()
                            };
                        }
                    }
                }
            }

            return quotation;
        }
    }
}

