using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Metadata;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

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
        public RepositoryResponse SaveOutwardEntry(OutWordEntry_Header header, List<DetailsOutwardEntry> details, string action)
        {
            try
            {
                var validation = Validdata(header, details);
                if (validation.status== false)
                {
                    return new RepositoryResponse {  status = false,  message = validation.message  };
                }
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

                    return new RepositoryResponse
                    {
                        status = true,
                        message = validation.message
                    };
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse
                {
                    status = false,
                    message = ex.Message
                }; ;
            }
        }          
        public RepositoryResponse Validdata(OutWordEntry_Header header, List<DetailsOutwardEntry> details)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                foreach (var d in details)
                {
                    if (string.IsNullOrWhiteSpace(d.ITEM_NAME))
                        continue;        

                    if (d.REF_TYPE == "SAGT" && d.REF_NO != 0)
                    {
                        string sql = @"
                        SELECT bill_gst, einvoice_flg, namount
                        FROM sale1
                        WHERE v_type = @v_type
                        AND v_no = @v_no
                        AND comp_code = @comp_code
                        AND branch_code = @branch_code
                        AND year_code = @year_code";

                        using (var cmd1 = new SqlCommand(sql, conn))
                        {
                            cmd1.Parameters.AddWithValue("@v_type", d.REF_TYPE);
                            cmd1.Parameters.AddWithValue("@v_no", d.REF_NO);
                            cmd1.Parameters.AddWithValue("@comp_code", g.PubCompCode);
                            cmd1.Parameters.AddWithValue("@branch_code", g.PubBranchCode);
                            cmd1.Parameters.AddWithValue("@year_code", g.PubFYearCode);

                            using var reader = cmd1.ExecuteReader();

                            if (reader.Read())
                            {
                                decimal bill_gst = Convert.ToDecimal(reader["bill_gst"]);
                                string einvoice_flg = Convert.ToString(reader["einvoice_flg"]);
                                decimal namount = Convert.ToDecimal(reader["namount"]);

                                // ✅ FIXED CONDITION
                                if (namount >= 100000 &&
                                    bill_gst > 16 &&
                                    namount != 0 &&
                                    einvoice_flg != "Y")
                                {
                                    return new RepositoryResponse
                                    {
                                        status = false,
                                        message = "Please Generate GST E Invoice Before Creating GatePass."
                                    };
                                }
                            }
                        }

                        // =========================================
                        // GST TAX VALIDATION
                        // =========================================

                        string sql2 = @"
                            SELECT 1
                            FROM Sale2
                            WHERE Tax_Code IN
                            (
                            SELECT Code
                            FROM TAX_MAST
                            WHERE TAX_TYPE = 'GST'
                            AND T_TYPE NOT IN ('Import')
                            )
                            AND (CGST_AMT + SGST_AMT + IGST_AMT) = 0
                            AND V_type = @v_type
                            AND V_no = @v_no
                            AND Comp_code = @comp_code
                            AND Branch_code = @branch_code
                            AND Year_Code = @year_code";

                        using (var cmd1 = new SqlCommand(sql2, conn))
                        {
                            cmd1.Parameters.AddWithValue("@v_type", d.REF_TYPE);
                            cmd1.Parameters.AddWithValue("@v_no", d.REF_NO);
                            cmd1.Parameters.AddWithValue("@comp_code", g.PubCompCode);
                            cmd1.Parameters.AddWithValue("@branch_code", g.PubBranchCode);
                            cmd1.Parameters.AddWithValue("@year_code", g.PubFYearCode);

                            using var reader = cmd1.ExecuteReader();

                            if (reader.Read())
                            {
                                return new RepositoryResponse
                                {
                                    status = false,
                                    message = $"ERROR! Tax not calculated in Invoice No => {d.REF_NO} & {d.REF_TYPE}"
                                };
                            }
                        }
                    }

                    // =========================================
                    // EWAY BILL VALIDATION
                    // =========================================

                    if (d.REF_TYPE == "DCHL" &&
                        string.IsNullOrWhiteSpace(header.WAYBILL_NO))
                    {
                        string sql3 = @"
                        SELECT state_mast.state_type
                        FROM state_mast
                        LEFT JOIN city_mast
                        ON city_mast.state_code = state_mast.code
                        WHERE city_mast.code = @code";

                        using (var cmd1 = new SqlCommand(sql3, conn))
                        {
                            cmd1.Parameters.AddWithValue("@code", header.PARTY_CITY);

                            using var reader = cmd1.ExecuteReader();

                            if (reader.Read())
                            {
                                string state_type = Convert.ToString(reader["state_type"]);

                                if (state_type != "Local")
                                {
                                    return new RepositoryResponse
                                    {
                                        status = false,
                                        message = "EWayBill is mandatory for Interstate Material Movement."
                                    };
                                }
                            }
                        }
                    }

                    // =========================================
                    // QUANTITY VALIDATION
                    // =========================================

                    decimal PubRes1Dbl = 0;
                    decimal PubRes2Dbl = 0;
                    string PubRes1Str = "";



                    if (header.ITEM_TYPE == "Sale")
                    {
                        PubRes1Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from sale2  where V_TYPE ='" + d.REF_TYPE + "' and V_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and YEAR_CODE =" + g.PubFYearCode + " and item_code= " + d.ITEM_CODE + ""));
                        PubRes2Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from gate2  where ref_TYPE ='" + d.REF_TYPE + "'and ref_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + "  and YEAR_CODE =" + g.PubFYearCode + " and item_code= " + d.ITEM_CODE + " and v_type<> '" + header.V_TYPE + "' and v_no <>" + header.V_NO + ""));
                        PubRes1Str = "Sale";

                    }
                    else if (header.ITEM_TYPE == "Order")
                    {
                        PubRes1Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from Order2  where V_TYPE ='" + d.REF_TYPE + "' and V_NO = " + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and item_code=" + d.ITEM_CODE + " "));
                        PubRes2Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from gate2  where ref_TYPE ='" + d.REF_TYPE + "' and ref_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and YEAR_CODE =" + g.PubFYearCode + "  and item_code= " + d.ITEM_CODE + " and v_type<>'" + header.V_TYPE + "' and v_no <> " + header.V_NO + ""));
                        PubRes1Str = "Order";
                    }
                    else if (header.ITEM_TYPE == "Misc")
                    {
                        PubRes1Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from gate2  where V_TYPE = '" + d.REF_TYPE + "' and V_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and YEAR_CODE =" + g.PubFYearCode + " and item_code=" + d.ITEM_CODE + "  and remarks= '" + d.REMARKS + "'"));
                        PubRes2Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from gate2  where ref_TYPE ='" + d.REF_TYPE + "' and ref_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and YEAR_CODE =" + g.PubFYearCode + " and item_code=" + d.ITEM_CODE + "   and remarks=  '" + d.REMARKS + "' and v_type<> '" + header.V_TYPE + "' and v_no <>" + header.V_NO + ""));
                        PubRes1Str = "Misc";
                    }
                    else if (header.ITEM_TYPE == "Empty")
                    {
                        PubRes1Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from gate2 where empty='Yes' and  V_TYPE ='" + d.REF_TYPE + "' and V_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and YEAR_CODE =" + g.PubFYearCode + " and item_code=" + d.ITEM_CODE + " "));
                        PubRes2Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from gate2 where ref_TYPE ='" + d.REF_TYPE + "' and ref_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and YEAR_CODE =" + g.PubFYearCode + " and item_code=" + d.ITEM_CODE + "  and v_type<>'" + header.V_TYPE + "' and v_no <>" + header.V_NO + ""));
                        PubRes1Str = "Empty";
                    }
                    else if (header.ITEM_TYPE == "Purchase Return")
                    {
                        PubRes1Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(bill_qty),0) from purchase2 where V_TYPE ='" + d.REF_TYPE + "' and V_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and item_code=" + d.ITEM_CODE + ""));
                        PubRes2Dbl = Convert.ToDecimal(GetText("select  isnull(Sum(qty),0) from gate2 where ref_TYPE ='" + d.REF_TYPE + "' and ref_NO =" + d.REF_NO + " and COMP_CODE =" + g.PubCompCode + " and BRANCH_CODE =" + g.PubBranchCode + " and item_code= " + d.ITEM_CODE + " and v_type= '" + header.V_TYPE + "' and v_no <>" + header.V_NO + ""));

                    }

                    // Add current qty
                    PubRes2Dbl += (d.QTY ?? 0);

                    if (header.ITEM_TYPE != "Others")
                    {
                        if (PubRes1Dbl > PubRes2Dbl)
                        {
                            string message = $"{PubRes1Str} Pending Quantity is = {PubRes1Dbl - PubRes2Dbl + (d.QTY ?? 0)} " +
                                             $"& Your Quantity is = {(d.QTY ?? 0)}, " +
                                             $"Please Check it of Item Name {d.ITEM_CODE}";
                            return new RepositoryResponse
                            {
                                status = true,
                                message = message
                            };
                        }
                    }

                }
                return new RepositoryResponse { status = true, message = "Success"  };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse  {  status = false,  message = ex.Message  };
            }
        }
        public string GetText(string query)
        {
            try
            {
                using var con = _dbConnection.GetErpConnection();
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader[0].ToString();
                            }
                            else
                            {
                                return string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetText() Error: " + ex.Message);
                return string.Empty;
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
