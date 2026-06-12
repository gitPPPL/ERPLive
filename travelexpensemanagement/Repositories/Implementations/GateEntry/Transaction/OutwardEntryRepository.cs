using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using StackExchange.Redis;
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

        public RepositoryResponse SaveOutwardEntry( OutWordEntry_Header header, List<DetailsOutwardEntry> details, string action)
        {
            try
            {
                var validation = Validdata(header, details);

                if (validation.status == true)
                {
                    return new RepositoryResponse  {  status = true,  message = validation.message };
                }
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();

                conn.Open();
                using var transaction = conn.BeginTransaction();
                try
                {               
            
                    using (var cmd = new SqlCommand("sp_OutwardEntry", conn, transaction))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "Header");
                        cmd.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "") + header.V_NO);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        // DATE PARAMETERS
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value =
                            header.V_DATE == null
                            ? DBNull.Value
                            : Convert.ToDateTime(header.V_DATE);

                        cmd.Parameters.Add("@RETURN_DATE", SqlDbType.SmallDateTime).Value =
                            header.RETURN_DATE == null
                            ? DBNull.Value
                            : Convert.ToDateTime(header.RETURN_DATE);

                        cmd.Parameters.AddWithValue("@V_TIME", header.V_TIME ?? "");                
                        cmd.Parameters.AddWithValue("@RESPONSIBLE_PERSON", header.RESPONSIBLE_PERSONB ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@PARTY_NAME", header.PARTY_NAME ?? "");
                        cmd.Parameters.AddWithValue("@TRUCK_NO", header.TRUCK_NO ?? "");
                        cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO ?? "");
                        cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS ?? "");
                        cmd.Parameters.AddWithValue("@ADD1", header.Add1 ?? "");
                        cmd.Parameters.AddWithValue("@ADD2", header.Add2 ?? "");
                        cmd.Parameters.AddWithValue("@ADD3", header.Add3 ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_CITY", header.PARTY_CITY);
                        cmd.Parameters.AddWithValue("@PARTY_GST", header.PARTY_GST ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_PINCODE", header.PARTY_PINCODE ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", header.PARTY_ADDRESSID);
                        cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID ?? "");
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId ?? "");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.ExecuteNonQuery();
                    }
               
                    string deleteSql = @"DELETE FROM GATE2
                                 WHERE COMP_CODE = @CompCode
                                 AND V_NO = @VNo
                                 AND BRANCH_CODE = @BranchCode
                                 AND YEAR_CODE = @YearCode";

                    using (var deleteCmd = new SqlCommand(deleteSql, conn, transaction))
                    {
                        deleteCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        deleteCmd.Parameters.AddWithValue("@VNo", header.V_NO);
                        deleteCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                        deleteCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);

                        deleteCmd.ExecuteNonQuery();
                    }

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
                        cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE ?? "");
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE == null ? DBNull.Value : Convert.ToDateTime(header.V_DATE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", d.ITEM_NAME ?? "");
                        cmd.Parameters.AddWithValue("@DEPT_CODE", d.DEPT_CODE);
                        cmd.Parameters.AddWithValue("@UOM_CODE", d.UOM_CODE);
                        cmd.Parameters.AddWithValue("@UOM_NAME", d.UOM_NAME ?? "");
                        cmd.Parameters.AddWithValue("@NOS", d.NOS);
                        cmd.Parameters.AddWithValue("@QTY", d.QTY);
                        cmd.Parameters.AddWithValue("@REMARKS", d.REMARKS ?? "");
                        cmd.Parameters.AddWithValue("@REF_TYPE", d.REF_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@REF_NO", d.REF_NO);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID ?? "");
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId ?? "");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit();
                    return new RepositoryResponse { status = true, message = "Save Successfully" };
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }
        public RepositoryResponse Validdata(OutWordEntry_Header header,  List<DetailsOutwardEntry> details)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                decimal mainQty = 0;
                decimal gateQty = 0;

                foreach (var d in details)
                {
                    if (string.IsNullOrWhiteSpace(d.ITEM_NAME))
                        continue;
                    if (d.REF_TYPE == "SAGT" && d.REF_NO != 0)
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", "Validdata");
                            cmd.Parameters.AddWithValue("@ShowActionOption", "SAGT");
                            cmd.Parameters.AddWithValue("@REF_TYPE", d.REF_TYPE);
                            cmd.Parameters.AddWithValue("@REF_NO", d.REF_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                            using var reader = cmd.ExecuteReader();
                            if (reader.Read())
                            {
                                string bill_gst = Convert.ToString(reader["bill_gst"]);
                                string einvoice_flg = Convert.ToString(reader["einvoice_flg"]);
                                decimal namount = Convert.ToDecimal(reader["namount"]);
                                decimal billGstValue = 0;
                                decimal.TryParse(bill_gst, out billGstValue);

                                if (namount >= 100000 && billGstValue > 16 && namount != 0 &&  einvoice_flg != "Y")
                                {
                                    return new RepositoryResponse { status = true, message = "Please Generate GST E Invoice Before Creating GatePass." };
                                }
                            }
                        }
                        using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", conn))          
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", "Validdata");
                            cmd.Parameters.AddWithValue("@ShowActionOption", "GSTVALID");
                            cmd.Parameters.AddWithValue("@REF_TYPE", d.REF_TYPE);
                            cmd.Parameters.AddWithValue("@REF_NO", d.REF_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                            using var reader = cmd.ExecuteReader();
                            if (reader.Read())
                            {
                                return new RepositoryResponse { status = true, message = $"ERROR! Tax not calculated in Invoice No => {d.REF_NO} & {d.REF_TYPE}"  };
                            }
                        }
                    }

                    if (d.REF_TYPE == "DCHL" && string.IsNullOrWhiteSpace(header.WAYBILL_NO))
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", "Validdata");
                            cmd.Parameters.AddWithValue("@ShowActionOption", "STATECHECK");
                            cmd.Parameters.AddWithValue("@PARTY_CITY", header.PARTY_CITY);
                            using var reader = cmd.ExecuteReader();
                            if (reader.Read())
                            {
                                string state_type =  Convert.ToString(reader["state_type"]);
                                if (state_type != "Local")
                                {
                                    return new RepositoryResponse { status = true, message = "EWayBill is mandatory for Interstate Material Movement."  };
                                }
                            }
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Validdata");
                        cmd.Parameters.AddWithValue("@ShowActionOption", "PendingQuantitycheack");
                        cmd.Parameters.AddWithValue("@PARTY_CITY", header.PARTY_CITY);
                        using var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            return new RepositoryResponse { status = true, message = "EWayBill is mandatory for Interstate Material Movement." };
                        }
                    }


                    if (header.ITEM_TYPE != "Others")
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", "Validdata");
                            cmd.Parameters.AddWithValue("@ShowActionOption", "QTYVALID");
                            cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE);
                            cmd.Parameters.AddWithValue("@REF_TYPE", d.REF_TYPE);
                            cmd.Parameters.AddWithValue("@REF_NO", d.REF_NO);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                            cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                            cmd.Parameters.AddWithValue("@DOC_ID", header.V_TYPE + header.V_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                            cmd.Parameters.AddWithValue("@REMARKS", d.REMARKS);
                            using var reader = cmd.ExecuteReader();
                            if (reader.Read())
                            {
                                mainQty = Convert.ToDecimal(reader["MainQty"]);
                                gateQty =  Convert.ToDecimal(reader["GateQty"]);
                            }
                        }
                        gateQty = gateQty + (d.QTY ?? 0);


                        if (gateQty > mainQty)
                        {
                            decimal pendingQty = (decimal)((mainQty - gateQty) + d.QTY);
                            return new RepositoryResponse
                            {
                                status = true,
                                message = $"{header.ITEM_TYPE} Pending Quantity is = {pendingQty} " +
                                          $"& Your Quantity is = {(d.QTY ?? 0)}, " +
                                          $"Please Check Item Name {d.ITEM_NAME}"
                            };
                        }

                    }

                }
                return new RepositoryResponse { status = false, message = "Success" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse  { status = false,  message = ex.Message };
            }
        }
        
        public decimal GetDecimal(string query)
        {
            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToDecimal(result);
                    }

                    return 0m;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetDecimal() Error: " + ex.Message);
                return 0m;
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
                using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetDataByPartyCodeAsync");
                    cmd.Parameters.AddWithValue( "@COMP_CODE",  getdata.PubCompCode);
                    cmd.Parameters.AddWithValue( "@PARTY_CODE",  partyId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                ADDRESS_ID = reader["ADDRESS_ID"]?.ToString(),
                                Add1 = reader["Add1"]?.ToString(),
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




        public List<object> GetDataByPartyandAddressidCodeAsync(  int partyId, int addressid)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue(  "@Action",  "GetByPartyandAddressid");
                    cmd.Parameters.AddWithValue(  "@COMP_CODE",   getdata.PubCompCode);
                    cmd.Parameters.AddWithValue(  "@PARTY_CODE", partyId);
                    cmd.Parameters.AddWithValue(  "@PARTY_ADDRESSID", addressid );
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                ADDRESS_ID =  reader["ADDRESS_ID"]?.ToString(),
                                Add1 = reader["Add1"]?.ToString(),
                                Add2 =  reader["Add2"]?.ToString(),
                                Add3 = reader["Add3"]?.ToString(),
                                GSTIN =  reader["GSTIN"]?.ToString(),
                                City_Code = reader["City_Code"]?.ToString(),
                                State = reader["State"]?.ToString(),
                                Pincode =  reader["Pincode"]?.ToString(),
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
