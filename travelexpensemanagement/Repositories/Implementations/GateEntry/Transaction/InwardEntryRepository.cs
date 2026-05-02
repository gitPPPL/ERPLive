
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

        public async Task<List<object>> GetDataByPartyCodeAsync(int partyId, int addressId)
        {
            var dataList = new List<object>();
            var globalData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                string query = @"SELECT a.Add1, a.Add2, a.Add3, a.GSTIN, a.City_Code,
                                b.Name AS State, a.Pincode, c.NAME as cityName,
                                a.PAN, a.STATE_CODE
                         FROM Subgroup_Address a
                         LEFT JOIN STATE_MAST b ON a.STATE_CODE = b.code
                         LEFT JOIN CITY_MAST c ON a.CITY_CODE = c.code
                         WHERE a.comp_code = @CompCode 
                         AND a.Code = @PartyId 
                         AND a.Address_Id = @Address_Id";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", globalData.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", partyId);
                    cmd.Parameters.AddWithValue("@Address_Id", addressId);

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

                string query = @"SELECT a.Add1, a.Add2, a.Add3, a.GSTIN, a.City_Code,
                                b.Name AS State, a.Pincode, c.NAME as cityName,
                                a.PAN, a.STATE_CODE
                         FROM Subgroup_Address a
                         LEFT JOIN STATE_MAST b ON a.STATE_CODE = b.code
                         LEFT JOIN CITY_MAST c ON a.CITY_CODE = c.code
                         WHERE a.comp_code = @CompCode 
                         AND a.Code = @PartyId
                         ORDER BY a.ADDRESS_ID ASC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", globalData.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", partyId);

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

        public async Task<List<object>> FetchShipFromAddressAsync(int shipFromId)
        {
            var dataList = new List<object>();
            var globalData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                string query = @"SELECT CONCAT(A.ADD1, ' ', A.ADD2, ' ', A.ADD3) AS FullAddress
                         FROM SUBGROUP_MAST A
                         WHERE Nature IN ('Customer','Supplier','Broker','Staff')
                         AND COMP_CODE = @CompCode
                         AND A.ACTIVE = 1
                         AND A.code = @ShipFromID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", globalData.PubCompCode);
                    cmd.Parameters.AddWithValue("@ShipFromID", shipFromId);

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

        public async Task<RepositoryResponse> ValidateBillNoAsync(int partyCode, string billNo, int vNo)
        {
            try
            {
                if (partyCode <= 0 || string.IsNullOrWhiteSpace(billNo))
                {
                   return new RepositoryResponse { status = false, message = "Invalid input" };
                }

                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                string sql = @"SELECT TOP 1 doc_id, V_date FROM GATE1
                       WHERE PARTY_CODE = @PartyCode AND BILL_NO = @BillNo
                       AND V_TYPE IN('INST','INRM','INFU','INJB','INMS','INSR','INRT')
                       AND V_NO <> @VNo
                       AND COMP_CODE = @CompCode
                       AND Branch_Code = @BranchCode
                       AND Year_Code = @YearCode";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PartyCode", partyCode);
                cmd.Parameters.AddWithValue("@BillNo", billNo);
                cmd.Parameters.AddWithValue("@VNo", vNo);
                cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                cmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var docId = reader["doc_id"]?.ToString();
                    var vDate = reader["V_date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["V_date"]).ToString("dd-MMM-yyyy")
                        : "";

                    return new RepositoryResponse
                    {
                        status = false,
                        message = $"Bill No '{billNo}' already exists at Serial No: {docId} dated: {vDate}"
                    };
                }

                return new RepositoryResponse { status = true, message = "Valid" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponse> ValidateGateNoAsync(string vType, int vNo)
        {
            try
            {
                if (vNo <= 0)
                {
                    return new RepositoryResponse { status = false, message = "Invalid Gate No" };
                }

                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                string sql = @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) AS V_NO
                       FROM Purchase1
                       WHERE V_TYPE IN (SELECT code FROM doctype_mast WHERE doctype = 'MaterialReceipt')
                       AND GATE_TYPE = @GATE_TYPE
                       AND GATE_No = @GATE_NO
                       AND Comp_Code = @Comp_Code
                       AND Branch_Code = @Branch_Code";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@GATE_TYPE", vType);
                cmd.Parameters.AddWithValue("@GATE_NO", vNo);
                cmd.Parameters.AddWithValue("@Comp_Code", g.PubCompCode);
                cmd.Parameters.AddWithValue("@Branch_Code", g.PubBranchCode);

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var existing = reader["V_NO"]?.ToString();

                    return new RepositoryResponse
                    {
                        status = false,
                        message = $"Gate no. {existing} exists. Modification not allowed."
                    };
                }

                return new RepositoryResponse { status = true, message = "Valid" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }


        public async Task<RepositoryResponseData<int>> GetSEARCHCONTAINERAsync(string Container_No)
        {
            var response = new RepositoryResponseData<int>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                string SQL = @"SELECT TOP 1 SUPPLIER  
                       FROM EXIM1 a  
                       LEFT JOIN EXIM2 b ON a.V_TYPE = b.V_TYPE  
                       AND a.V_NO = b.V_NO  
                       AND a.COMP_CODE = b.COMP_CODE 
                       AND a.BRANCH_CODE = b.BRANCH_CODE  
                       AND a.YEAR_CODE = b.YEAR_CODE  
                       WHERE b.Container_No = @Container_No";

                using (SqlCommand cmd = new SqlCommand(SQL, con))
                {
                    cmd.Parameters.Add("@Container_No", SqlDbType.VarChar).Value = Container_No;

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


        public async Task<RepositoryResponseList<int>> DDlTransitNoAsync(
        string v_type, int v_no, int partycode, DateTime ExpiryDate)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<int>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                string query = @"SELECT V_No FROM WAYBILL1 WHERE V_TYPE = 'TRIN'
                                AND V_No NOT IN (
                                SELECT TRANSIT_NO FROM GATE1 
                                WHERE V_TYPE = @V_Type AND V_No = @V_No 
                                AND TRANSIT_NO <> 0 
                                AND COMP_CODE = @CompCode 
                                AND BRANCH_CODE = @BRANCH_CODE
                                )
                                AND PARTY_CODE = @PartyCode 
                                AND Status = 1 
                                AND COMP_CODE = @CompCode 
                                AND BRANCH_CODE = @BRANCH_CODE 
                                AND EXPIRY_DATE IS NOT NULL  
                                AND EXPIRY_DATE >= @ExpiryDate  
                                ORDER BY V_No;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = getdata.PubCompCode;
                    cmd.Parameters.Add("@V_Type", SqlDbType.VarChar).Value = (object)v_type ?? DBNull.Value;
                    cmd.Parameters.Add("@V_No", SqlDbType.Int).Value = v_no;
                    cmd.Parameters.Add("@PartyCode", SqlDbType.Int).Value = partycode;
                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = getdata.PubBranchCode;

                    DateTime expiryDate = ExpiryDate.AddMonths(-1);
                    cmd.Parameters.Add("@ExpiryDate", SqlDbType.DateTime).Value = expiryDate;

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(Convert.ToInt32(reader["V_No"]));
                        }
                    }
                }
            }

            return new RepositoryResponseList<int>
            {
                status = true,
                message = "Success",
                totalCount = dataList.Count,
                data = dataList
            };
        }
    }

}

