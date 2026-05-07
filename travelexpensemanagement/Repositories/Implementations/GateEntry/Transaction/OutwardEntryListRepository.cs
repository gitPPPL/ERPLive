using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class OutwardEntryListRepository : IOutwardEntryListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;

        public OutwardEntryListRepository(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
        }

        public object GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            int totalCount = 0;
            var headerList = new List<OutWordEntry_Header>();

            try
            {
                using var conn = _dbConnection.GetErpConnection();

                using var cmd = new SqlCommand("sp_OutwardEntry", conn);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@SearchTerm",
                    string.IsNullOrWhiteSpace(searchTerm)
                    ? DBNull.Value
                    : searchTerm);

                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@COMP_CODE", getvariabledata.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", getvariabledata.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", getvariabledata.PubBranchCode);

                conn.Open();

                using var reader = cmd.ExecuteReader();

                // First Result Set
                while (reader.Read())
                {
                    headerList.Add(new OutWordEntry_Header
                    {
                        DOC_ID = reader["DOC_ID"]?.ToString(),

                        V_NO = reader["V_NO"] != DBNull.Value
                            ? Convert.ToInt32(reader["V_NO"])
                            : 0,

                        REF_NO = reader["Ref_no"] != DBNull.Value
                            ? Convert.ToInt32(reader["Ref_no"])
                            : 0,

                        V_DATE = reader["V_DATE"] != DBNull.Value
                            ? Convert.ToDateTime(reader["V_DATE"])
                            : DateTime.MinValue,

                        TRUCK_NO = reader["Truck_no"]?.ToString(),

                        BILL_NO = reader["BILL_NO"]?.ToString(),

                        BILL_DATE = reader["BILL_DATE"] != DBNull.Value
                            ? Convert.ToDateTime(reader["BILL_DATE"])
                            : DateTime.MinValue,

                        PARTY_NAME = reader["NAME"]?.ToString(),

                        REF_TYPE = reader["Ref_type"]?.ToString(),

                        V_TYPE = reader["V_TYPE"]?.ToString()
                    });
                }

                // Second Result Set for Total Count
                if (reader.NextResult() && reader.Read())
                {
                    totalCount = reader["TotalCount"] != DBNull.Value
                        ? Convert.ToInt32(reader["TotalCount"])
                        : 0;
                }

                return new
                {
                    success = true,
                    lists = headerList,
                    totalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error fetching data.",
                    error = ex.Message
                };
            }
        }
    }
}