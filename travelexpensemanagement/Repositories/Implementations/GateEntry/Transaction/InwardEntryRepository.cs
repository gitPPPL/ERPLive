
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Common;
using System.Net.Http.Headers;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Interfaces;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
using static travelexpensemanagement.Controllers.GateEntry.Transaction.InwardEntryController;
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
        public async Task<List<object>> GetDataByPartyCodeAsync(int partyId, int addressId)
        {
            var dataList = new List<object>();
            var globalData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalData.PubCompCode);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", partyId);
                    cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", addressId);
                    cmd.Parameters.AddWithValue("@Action", "GetDataByPartyCodeAsync");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(new
                            {
                                Add1 = reader["Add1"]?.ToString(),
                                Add2 = reader["Add2"]?.ToString(),
                                Add3 = reader["Add3"]?.ToString(),
                                GSTIN = reader["GSTIN"]?.ToString(),
                                City_Code = reader["City_Code"]?.ToString(),
                                STATE_CODE = reader["STATE_CODE"]?.ToString(),
                                State = reader["State"]?.ToString(),
                                Pincode = reader["Pincode"]?.ToString(),
                                cityName = reader["cityName"]?.ToString(),
                                PAN = reader["PAN"]?.ToString()
                            });
                        }
                    }
                }
            }

            return dataList;
        }
        public async Task<List<object>> GetPartyAddressByCodeAsync(int partyId)
        {
            var dataList = new List<object>();
            var globalData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalData.PubCompCode);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", partyId);
                    cmd.Parameters.AddWithValue("@Action", "GetPartyAddressByCodeAsync");


                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(new
                            {
                                ADDRESS_ID = reader["ADDRESS_ID"]?.ToString(),
                                Add1 = reader["Add1"]?.ToString(),
                                Add2 = reader["Add2"]?.ToString(),
                                Add3 = reader["Add3"]?.ToString(),
                                GSTIN = reader["GSTIN"]?.ToString(),
                                City_Code = reader["City_Code"]?.ToString(),
                                STATE_CODE = reader["STATE_CODE"]?.ToString(),
                                State = reader["State"]?.ToString(),
                                Pincode = reader["Pincode"]?.ToString(),
                                cityName = reader["cityName"]?.ToString(),
                                PAN = reader["PAN"]?.ToString()
                            });
                        }
                    }
                }
            }

            return dataList;
        }
        public async Task<List<object>> FetchShipFromAddressAsync(int shipFromId)
        {
            var dataList = new List<object>();
            var globalData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalData.PubCompCode);
                    cmd.Parameters.AddWithValue("@SHIP_PARTY", shipFromId);
                    cmd.Parameters.AddWithValue("@Action", "FetchShipFromAddressAsync");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(new
                            {
                                Address = reader["FullAddress"]?.ToString()
                            });
                        }
                    }
                }
            }

            return dataList;
        }
        public async Task<RepositoryResponse> ValidateBillNoAsync(  int partyCode, string billNo,  int vNo)
        {
            try
            {
                if (partyCode <= 0 || string.IsNullOrWhiteSpace(billNo))
                {
                    return new RepositoryResponse
                    {
                        status = false,
                        message = "Invalid input"
                    };
                }

                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                using SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn);

                // IMPORTANT
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@PARTY_CODE", SqlDbType.Int).Value = partyCode;

                cmd.Parameters.Add("@BILL_NO", SqlDbType.NVarChar, 30).Value =
                    billNo;

                cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = vNo;

                cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value =
                    g.PubCompCode;

                cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value =
                    g.PubBranchCode;

                cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value =
                    g.PubFYearCode;

                // EXACT ACTION NAME FROM SP
                cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 50).Value =
                    "ValidateBillNoAsync";

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    string docId = reader["doc_id"]?.ToString() ?? "";

                    string vDate = reader["V_date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["V_date"])
                            .ToString("dd-MMM-yyyy")
                        : "";

                    return new RepositoryResponse
                    {
                        status = false,
                        message = $"Bill No '{billNo}' already exists at Serial No: {docId} dated: {vDate}"
                    };
                }

                return new RepositoryResponse
                {
                    status = true,
                    message = ""
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse
                {
                    status = false,
                    message = ex.Message
                };
            }
        }
        public async Task<RepositoryResponse> ValidateGateNoAsync( string vType,  int vNo)
        {
            try
            {
                if (vNo <= 0)
                {
                    return new RepositoryResponse
                    {
                        status = false,
                        message = "Invalid Gate No"
                    };
                }

                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();

                await conn.OpenAsync();

                using SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn);

                // IMPORTANT
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 50)
                    .Value = "ValidateGateNoAsync";

                cmd.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 4)
                    .Value = vType;

                cmd.Parameters.Add("@V_NO", SqlDbType.Int)
                    .Value = vNo;

                cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int)
                    .Value = g.PubCompCode;

                cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int)
                    .Value = g.PubBranchCode;

                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    string existing = reader["V_NO"]?.ToString() ?? "";

                    return new RepositoryResponse
                    {
                        status = false,
                        message = $"Gate no. {existing} exists. Modification not allowed."
                    };
                }

                return new RepositoryResponse
                {
                    status = true,
                    message = ""
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse
                {
                    status = false,
                    message = ex.Message
                };
            }
        }
        public async Task<RepositoryResponseData<int>> GetSEARCHCONTAINERAsync(string Container_No)
        {
            var response = new RepositoryResponseData<int>();
            var g = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();


                using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", con))
                {
                   
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@Year_Code", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@Container_No", Container_No);
                    cmd.Parameters.AddWithValue("@Action", "GetSEARCHCONTAINERAsync");

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.status = true;
                            response.message = "Successfully fetched";
                            response.data = Convert.ToInt32(reader["SUPPLIER"]);
                        }
                        else
                        {
                            response.status = false;
                            response.message = "Container Detail not found in Import Tracking.";
                        }
                    }
                }
            }

            return response;
        }
        public async Task<RepositoryResponseList<int>> DDlTransitNoAsync(  string v_type,  int v_no,  int partycode, DateTime ExpiryDate)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<int>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = getdata.PubCompCode;
                    cmd.Parameters.Add("@V_Type", SqlDbType.NVarChar, 10).Value = (object)v_type ?? DBNull.Value;
                    cmd.Parameters.Add("@V_No", SqlDbType.Int).Value = v_no;
                    cmd.Parameters.Add("@PARTY_CODE", SqlDbType.Int).Value = partycode;
                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = getdata.PubBranchCode;
                    cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 50).Value = "DDlTransitNo";
                    cmd.Parameters.Add("@EWB_EXPDATE", SqlDbType.Date).Value = Convert.ToDateTime(ExpiryDate).AddMonths(-1);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(Convert.ToInt32(reader["V_No"]));
                        }
                    }
                }
            }
            return new RepositoryResponseList<int>  {  status = true, message = "Success",  totalCount = dataList.Count, data = dataList  };
        }
    }
}