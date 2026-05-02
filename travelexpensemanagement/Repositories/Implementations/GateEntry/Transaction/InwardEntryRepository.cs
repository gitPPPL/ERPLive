
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
        private readonly HttpClient _client;
        public InwardEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, HttpClient client)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _client = client;
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

        public async Task<RcRequest> GetVehicleInfoAsync(string rcNumber)
        {
            string url = "https://kyc-api.surepass.io/api/v1/rc/rc-full";
            string token = "YOUR_TOKEN_HERE";

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var payload = new JObject
            {
                ["id_number"] = rcNumber
            };

            var content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(url, content);
            var responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(responseData);

            var json = JObject.Parse(responseData);
            var data = json["data"];

            if (data == null)
                return null;

            return new RcRequest
            {
                RcNumber = data["rc_number"]?.ToString(),
                ClientId = data["client_id"]?.ToString(),
                OwnerName = data["owner_name"]?.ToString(),
                FatherName = data["father_name"]?.ToString(),
                PresentAddress = data["present_address"]?.ToString(),
                PermanentAddress = data["permanent_address"]?.ToString(),
                VehicleChasiNumber = data["vehicle_chasi_number"]?.ToString(),
                VehicleEngineNumber = data["vehicle_engine_number"]?.ToString(),
                RegistrationDate = data["registration_date"]?.ToObject<DateTime?>(),
                FuelType = data["fuel_type"]?.ToString(),
                Color = data["color"]?.ToString(),
                InsurancePolicyNumber = data["insurance_policy_number"]?.ToString(),
                InsuranceUpto = data["insurance_upto"]?.ToObject<DateTime?>(),
                RcStatus = data["rc_status"]?.ToString()
            };
        }

        // ===========================
        // 2. SAVE VEHICLE INFO
        // ===========================
        public async Task<ApiResponse> SaveVehicleInfoAsync(RcRequest vehicleInfo, string VType, int VNo)
        {
            var g = _globalVariableService.GetGlobalVariables();

            using var conn = _dbConnection.GetErpConnection();
            conn.Open();

            // DELETE OLD
            string deleteSql = @"DELETE FROM GATE_VAHAN
                             WHERE V_TYPE=@VType AND V_NO=@VNo
                             AND COMP_CODE=@Comp AND BRANCH_CODE=@Branch AND YEAR_CODE=@Year";

            using (var cmd = new SqlCommand(deleteSql, conn))
            {
                cmd.Parameters.AddWithValue("@VType", VType);
                cmd.Parameters.AddWithValue("@VNo", VNo);
                cmd.Parameters.AddWithValue("@Comp", g.PubCompCode);
                cmd.Parameters.AddWithValue("@Branch", g.PubBranchCode);
                cmd.Parameters.AddWithValue("@Year", g.PubFYearCode);
                cmd.ExecuteNonQuery();
            }

            // INSERT
                string sql = @"INSERT INTO GATE_VAHAN
                (COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, rc_number, owner_name, father_name,
                present_address, permanent_address, vehicle_chasi_number, vehicle_engine_number,
                registration_date, fuel_type, color, insurance_policy_number, insurance_upto,
                rc_status, UUSER, UDATE)
                VALUES
                (@COMP, @BRANCH, @YEAR, @VTYPE, @VNO, @rc, @owner, @father,
                @paddr, @peraddr, @chasi, @engine,
                @regdate, @fuel, @color, @policy, @insup,
                @status, @user, GETDATE())";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@COMP", g.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH", g.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR", g.PubFYearCode);
                cmd.Parameters.AddWithValue("@VTYPE", VType);
                cmd.Parameters.AddWithValue("@VNO", VNo);

                cmd.Parameters.AddWithValue("@rc", vehicleInfo.RcNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@owner", vehicleInfo.OwnerName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@father", vehicleInfo.FatherName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@paddr", vehicleInfo.PresentAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@peraddr", vehicleInfo.PermanentAddress ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@chasi", vehicleInfo.VehicleChasiNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@engine", vehicleInfo.VehicleEngineNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@regdate", vehicleInfo.RegistrationDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@fuel", vehicleInfo.FuelType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@color", vehicleInfo.Color ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@policy", vehicleInfo.InsurancePolicyNumber ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@insup", vehicleInfo.InsuranceUpto ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@status", vehicleInfo.RcStatus ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@user", g.PubUserId);

                cmd.ExecuteNonQuery();
            }

            return new ApiResponse
            {
                Status = "Success",
                Message = "Vehicle saved successfully"
            };
        }

        // ===========================
        // 3. GET SAVED VEHICLE
        // ===========================
        public async Task<RcRequest?> GetVehicleDetailAsync(int vNo, string vType)
        {
            var g = _globalVariableService.GetGlobalVariables();

            using var conn = _dbConnection.GetErpConnection();
            conn.Open();

            string sql = @"SELECT * FROM GATE_VAHAN
                       WHERE COMP_CODE=@Comp AND YEAR_CODE=@Year
                       AND V_NO=@VNo AND V_TYPE=@VType";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Comp", g.PubCompCode);
            cmd.Parameters.AddWithValue("@Year", g.PubFYearCode);
            cmd.Parameters.AddWithValue("@VNo", vNo);
            cmd.Parameters.AddWithValue("@VType", vType);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new RcRequest
            {
                RcNumber = reader["rc_number"]?.ToString(),
                OwnerName = reader["owner_name"]?.ToString(),
                FatherName = reader["father_name"]?.ToString(),
                VehicleChasiNumber = reader["vehicle_chasi_number"]?.ToString(),
                VehicleEngineNumber = reader["vehicle_engine_number"]?.ToString(),
                FuelType = reader["fuel_type"]?.ToString(),
                Color = reader["color"]?.ToString(),
                RcStatus = reader["rc_status"]?.ToString()
            };
        }
    }

}

