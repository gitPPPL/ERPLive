using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class OutwardEntryRepository  : IOutwardEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        public OutwardEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
        }

        public async Task<string> GetVNoAsync(string vType, string tableName = "")
        {
            string newV_NO = "00000";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    string prefixYR;

                    using (SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con))
                    {
                        prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        prefixYR = (await prefixCmd.ExecuteScalarAsync())?.ToString() ?? "0000";
                    }
                             

                    string lastV_NO_Query = $@"
                        SELECT MAX(CAST(V_NO AS INT)) 
                        FROM {tableName}
                        WHERE COMP_CODE = @CompCode 
                        AND YEAR_CODE = @YearCode 
                        AND BRANCH_CODE = @BranchCode 
                        AND V_TYPE = @Vtype";

                    using (SqlCommand cmd = new SqlCommand(lastV_NO_Query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BranchCode", 1);
                        cmd.Parameters.AddWithValue("@Vtype", vType);

                        object result = await cmd.ExecuteScalarAsync();

                        if (result != DBNull.Value && result != null)
                        {
                            int lastV_NO = Convert.ToInt32(result);
                            newV_NO = (lastV_NO + 1).ToString("D5");
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
        public string SaveOutwardEntry(OutWordEntry_Header header, List<DetailsOutwardEntry> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();         
                using var transaction = conn.BeginTransaction();
                try
                {
                    // 🔴 DELETE OLD DATA
                    string deleteSql = @" DELETE FROM GATE2   WHERE COMP_CODE = @CompCode   AND V_NO = @VNo 
                    AND BRANCH_CODE = @BranchCode   AND YEAR_CODE = @YearCode;";

                    using (var deleteCmd = new SqlCommand(deleteSql, conn, transaction))
                    {
                        deleteCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        deleteCmd.Parameters.AddWithValue("@VNo", header.V_NO);
                        deleteCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                        deleteCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                        deleteCmd.ExecuteNonQuery();
                    }

                    // 🔵 HEADER SAVE
                    using (var cmd = new SqlCommand("sp_OutwardEntry", conn, transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "Header");
                        cmd.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "") + header.V_NO);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                        cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                        cmd.Parameters.AddWithValue("@V_TIME", header.V_TIME);
                        cmd.Parameters.AddWithValue("@RETURN_DATE", header.RETURN_DATE);
                        cmd.Parameters.AddWithValue("@RESPONSIBLE_PERSON", header.RESPONSIBLE_PERSONB);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@PARTY_NAME", header.PARTY_NAME);
                        cmd.Parameters.AddWithValue("@TRUCK_NO", header.TRUCK_NO);
                        cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO);
                        cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS);
                        cmd.Parameters.AddWithValue("@ADD1", header.Add1);
                        cmd.Parameters.AddWithValue("@ADD2", header.Add2);
                        cmd.Parameters.AddWithValue("@ADD3", header.Add3);
                        cmd.Parameters.AddWithValue("@PARTY_CITY", header.PARTY_CITY);
                        cmd.Parameters.AddWithValue("@PARTY_GST", header.PARTY_GST);
                        cmd.Parameters.AddWithValue("@PARTY_PINCODE", header.PARTY_PINCODE);
                        cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", header.PARTY_ADDRESSID);
                        cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.ExecuteNonQuery();
                    }

                    // 🟢 DETAILS SAVE
                    foreach (var d in details)
                    {
                        if (string.IsNullOrWhiteSpace(d.ITEM_NAME))
                            continue;

                        using var cmd = new SqlCommand("sp_OutwardEntry", conn, transaction);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "INSERT");
                        cmd.Parameters.AddWithValue("@SaveAction", "Details");
                        cmd.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "") + header.V_NO);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                        cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", d.ITEM_NAME);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", d.DEPT_CODE);
                        cmd.Parameters.AddWithValue("@UOM_CODE", d.UOM_CODE);
                        cmd.Parameters.AddWithValue("@UOM_NAME", d.UOM_NAME);
                        cmd.Parameters.AddWithValue("@NOS", d.NOS);
                        cmd.Parameters.AddWithValue("@QTY", d.QTY);
                        cmd.Parameters.AddWithValue("@REMARKS", d.REMARKS);
                        cmd.Parameters.AddWithValue("@REF_TYPE", d.REF_TYPE);
                        cmd.Parameters.AddWithValue("@REF_NO", d.REF_NO);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();


                    if (action == "UPDATE")
                    {
                        _globalValidationdate.LogInsertUpdateDelete(destinationTable: "gate1", sourceTable: "gate1", transactionType: "Transaction",
                        codeVNo: header.V_NO.ToString(), vtype: header.V_TYPE);
                    }

                    return "Success";
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        public List<object> GetDataByPartyCodeAsync(int partyId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"
                SELECT  top 1 a.ADDRESS_ID ,   a.Add1, a.Add2, a.Add3, a.GSTIN, 
                a.City_Code, b.Name AS State, 
                c.Name AS City, a.Pincode
                FROM Subgroup_Address AS a 
                LEFT JOIN STATE_MAST AS b ON a.STATE_CODE = b.Code
                LEFT JOIN CITY_MAST AS c ON a.CITY_CODE = c.Code
                WHERE a.Comp_Code = @CompCode  
                AND  a.Code = @PartyId  order by a.ADDRESS_ID asc ;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", partyId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                Add1 = reader["Add1"]?.ToString(),
                                ADDRESS_ID = reader["ADDRESS_ID"]?.ToString(),
                                Add2 = reader["Add2"]?.ToString(),
                                Add3 = reader["Add3"]?.ToString(),
                                GSTIN = reader["GSTIN"]?.ToString(),
                                City_Code = reader["City_Code"]?.ToString(),
                                State = reader["State"]?.ToString(),
                                Pincode = reader["Pincode"]?.ToString(),
                                cityName = reader["City"]?.ToString()
                            });
                        }
                    }
                }
            }

            return dataList;
        }
        public List<object> GetDataByPartyandAddressidCodeAsync(int partyId , int  addressid)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"
                SELECT top 1 a.Add1, a.Add2, a.Add3, a.GSTIN, a.ADDRESS_ID ,
                a.City_Code, b.Name AS State, 
                c.Name AS City, a.Pincode
                FROM Subgroup_Address AS a 
                LEFT JOIN STATE_MAST AS b ON a.STATE_CODE = b.Code
                LEFT JOIN CITY_MAST AS c ON a.CITY_CODE = c.Code
                WHERE a.Comp_Code = @CompCode  
                AND a.Code = @PartyId   and  a.ADDRESS_ID = @addressid ;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", partyId);
                    cmd.Parameters.AddWithValue("@addressid", addressid);


                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                Add1 = reader["Add1"]?.ToString(),
                                ADDRESS_ID = reader["ADDRESS_ID"]?.ToString(),
                                Add2 = reader["Add2"]?.ToString(),
                                Add3 = reader["Add3"]?.ToString(),
                                GSTIN = reader["GSTIN"]?.ToString(),
                                City_Code = reader["City_Code"]?.ToString(),
                                State = reader["State"]?.ToString(),
                                Pincode = reader["Pincode"]?.ToString(),
                                cityName = reader["City"]?.ToString()
                            });
                        }
                    }
                }
            }

            return dataList;
        }
    }
}
