using AngleSharp.Text;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sale.Transaction
{
    public class SalesProformaInvoice : ISalesProformaInvoice
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;

        public SalesProformaInvoice(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
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



        public async Task<(string Status, string Message)> SubmitRequest(SalesProformaInvoice_Header header, List<SalesProformaInvoice_Detail> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();

                    DataTable dt = new DataTable();

                if (header.SUPPLY_TYPE != "LOCAL")
                {
                    string sql = $@"SELECT * FROM SAUDA_EXPORT  
                     WHERE Ref_type = '" + header.V_TYPE + "' AND" +
                    " Ref_no = " + header.V_NO + "  AND Comp_code = " + g.PubCompCode + " " +
                    "AND Branch_code = " + g.PubBranchCode + " AND Year_Code = " + g.PubFYearCode + "";

                    using var da = new SqlDataAdapter(sql, conn);
                    da.Fill(dt);
                }

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];

                    int FINAL_COUNTRY = row["FINAL_COUNTRY"] == DBNull.Value ? 0 : Convert.ToInt32(row["FINAL_COUNTRY"]);
                    int NOTIFY_PERSON = row["NOTIFY_PERSON"] == DBNull.Value ? 0 : Convert.ToInt32(row["NOTIFY_PERSON"]);
                    string POL_CODE = row["POL_CODE"]?.ToString() ?? "";
                    string FPOD = row["FPOD"]?.ToString() ?? "";
                    string SEAPORT_CODE = row["SEAPORT_CODE"]?.ToString() ?? "";


                    if (header.INCOTERM == "FOB")
                    {
                        if (FINAL_COUNTRY == 0 && NOTIFY_PERSON == 0)
                        {

                            return ("Validation", "Please fill 'Final Country and Notify Party-1' both in Export Details Form.");
                        }

                        else
                        {
                            if (POL_CODE == "" && FPOD == "" && SEAPORT_CODE == "" && FINAL_COUNTRY == 0 && NOTIFY_PERSON == 0)
                            {
                                return ("Validation", "Please fill all the necessary Export Details in Export detail Form.");
                            }
                        }
                    }

                    else
                    {
                        return ("Validation", "Please fill all the necessary Export Details.");
                    }

                }

                await conn.OpenAsync();

                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;

                using (var cmd = new SqlCommand("sp_SalesProformaInvoice", conn))
                {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "HEADER");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                        cmd.Parameters.AddWithValue("@SUPPLY_TYPE", header.SUPPLY_TYPE);
                        cmd.Parameters.AddWithValue("@SHIP_TYPE", header.SHIP_TYPE);
                        cmd.Parameters.AddWithValue("@BILL_ADDRESSID", header.BILL_ADDRESSID);
                        cmd.Parameters.AddWithValue("@SHIP_ADDRESSID", header.SHIP_ADDRESSID);
                        cmd.Parameters.AddWithValue("@BILL_CODE", header.BILL_CODE);
                        cmd.Parameters.AddWithValue("@BILL_NAME", header.BILL_NAME);
                        cmd.Parameters.AddWithValue("@BILL_ADD1", header.BILL_ADD1);
                        cmd.Parameters.AddWithValue("@BILL_ADD2", header.BILL_ADD2);
                        cmd.Parameters.AddWithValue("@BILL_ADD3", header.BILL_ADD3);
                        cmd.Parameters.AddWithValue("@BILL_CITY", header.BILL_CITY);
                        cmd.Parameters.AddWithValue("@BILL_STATE", header.BILL_STATE);
                        cmd.Parameters.AddWithValue("@BILL_COUNTRY", header.BILL_COUNTRY);
                        cmd.Parameters.AddWithValue("@BILL_PINCODE", header.BILL_PINCODE);
                        cmd.Parameters.AddWithValue("@BILL_CITYName", header.BILL_CITYName);
                        cmd.Parameters.AddWithValue("@BILL_STATENAME", header.BILL_STATENAME);
                        cmd.Parameters.AddWithValue("@BILL_COUNTRYNAME", header.BILL_COUNTRYNAME);
                        cmd.Parameters.AddWithValue("@BILL_GST", header.BILL_GST);
                        cmd.Parameters.AddWithValue("@SHIP_CODE", header.SHIP_CODE);
                        cmd.Parameters.AddWithValue("@SHIP_NAME", header.SHIP_NAME);
                        cmd.Parameters.AddWithValue("@SHIP_ADD1", header.SHIP_ADD1);
                        cmd.Parameters.AddWithValue("@SHIP_ADD2", header.SHIP_ADD2);
                        cmd.Parameters.AddWithValue("@SHIP_ADD3", header.SHIP_ADD3);
                        cmd.Parameters.AddWithValue("@SHIP_CITY", header.SHIP_CITY);
                        cmd.Parameters.AddWithValue("@SHIP_STATE", header.SHIP_STATE);
                        cmd.Parameters.AddWithValue("@SHIP_COUNTRY", header.SHIP_COUNTRY);
                        cmd.Parameters.AddWithValue("@SHIP_PINCODE", header.SHIP_PINCODE);
                        cmd.Parameters.AddWithValue("@SHIP_CITYNAME", header.SHIP_CITYNAME);
                        cmd.Parameters.AddWithValue("@SHIP_STATENAME", header.SHIP_STATENAME);
                        cmd.Parameters.AddWithValue("@SHIP_COUNTRYNAME", header.SHIP_COUNTRYNAME);
                        cmd.Parameters.AddWithValue("@SHIP_GST", header.SHIP_GST);
                        cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", header.IMPORT_CURRENCY);
                        cmd.Parameters.AddWithValue("@EXRATE", header.EXRATE);
                        cmd.Parameters.AddWithValue("@PACK_NO", header.PACK_NO);
                        cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE);
                        cmd.Parameters.AddWithValue("@WB_NO", header.WB_NO);
                        cmd.Parameters.AddWithValue("@AMOUNT", header.AMOUNT);
                        cmd.Parameters.AddWithValue("@PACK_AMT", header.PACK_AMT);
                        cmd.Parameters.AddWithValue("@TCS_PER", header.TCS_PER);
                        cmd.Parameters.AddWithValue("@TCS_AMT", header.TCS_AMT);
                        cmd.Parameters.AddWithValue("@CGST_AMT", header.CGST_AMT);
                        cmd.Parameters.AddWithValue("@SGST_AMT", header.SGST_AMT);
                        cmd.Parameters.AddWithValue("@IGST_AMT", header.IGST_AMT);
                        cmd.Parameters.AddWithValue("@CESS_AMT", header.CESS_AMT);
                        cmd.Parameters.AddWithValue("@TOT_NOS", header.TOT_NOS);
                        cmd.Parameters.AddWithValue("@TOT_NET", header.TOT_NET);
                        cmd.Parameters.AddWithValue("@FRT_AMT", header.FRT_AMT);
                        cmd.Parameters.AddWithValue("@ROUND_OFF", header.ROUND_OFF);
                        cmd.Parameters.AddWithValue("@NAMOUNT", header.NAMOUNT);
                        cmd.Parameters.AddWithValue("@INSU_PER", header.INSU_PER);
                        cmd.Parameters.AddWithValue("@INSU_AMT", header.INSU_AMT);
                        cmd.Parameters.AddWithValue("@TOT_GROSS", header.TOT_GROSS);
                        cmd.Parameters.AddWithValue("@GR_NO", header.GR_NO);
                        cmd.Parameters.Add("@GR_DATE", SqlDbType.SmallDateTime).Value = header.GR_DATE;                                
                        cmd.Parameters.AddWithValue("@VEHICLE_NO", header.VEHICLE_NO);
                        cmd.Parameters.AddWithValue("@TRANSPORT_CODE", header.TRANSPORT_CODE);
                        cmd.Parameters.AddWithValue("@TRANSPORT_NAME", header.TRANSPORT_NAME);
                        cmd.Parameters.AddWithValue("@PORT_LOADING", header.PORT_LOADING);
                        cmd.Parameters.AddWithValue("@PORT_DISCHARGE", header.PORT_DISCHARGE);
                        cmd.Parameters.AddWithValue("@SOLD_BY", header.SOLD_BY);
                        cmd.Parameters.AddWithValue("@BUYER_ORDNO", header.BUYER_ORDNO);
                        cmd.Parameters.AddWithValue("@REMARK", header.REMARK);
                        cmd.Parameters.AddWithValue("@WB_QTY", header.WB_QTY);
                        cmd.Parameters.AddWithValue("@disc_per", header.DISC_PER);
                        cmd.Parameters.AddWithValue("@disc_amt", header.DISC_AMT);
                        cmd.Parameters.AddWithValue("@AGENT_CODE", header.AGENT_CODE);
                        cmd.Parameters.AddWithValue("@FINAL_DEST", header.FINAL_DEST);
                        cmd.Parameters.AddWithValue("@FINAL_DEST_COUNTRY", header.FINAL_DEST_COUNTRY);
                        cmd.Parameters.AddWithValue("@DELIVERY_TERMS", header.DELIVERY_TERMS);
                        cmd.Parameters.AddWithValue("@PAY_TERM", header.PAY_TERM);
                        cmd.Parameters.AddWithValue("@LUT_DETAIL", header.LUT_DETAIL);
                        cmd.Parameters.AddWithValue("@INSU_DETAIL", header.INSU_DETAIL);
                        cmd.Parameters.AddWithValue("@TRADE_TERM", header.TRADE_TERM);
                        cmd.Parameters.AddWithValue("@INCOTERM", header.INCOTERM);
                        cmd.Parameters.AddWithValue("@SHIPMENT_TYPE", header.SHIPMENT_TYPE);
                        cmd.Parameters.AddWithValue("@MODEOF_PAYMENT", header.MODEOF_PAYMENT);
                        cmd.Parameters.AddWithValue("@DEL_SCH", header.DEL_SCH);
                        cmd.Parameters.AddWithValue("@CONTAINER_SIZE", header.CONTAINER_SIZE);
                        cmd.Parameters.AddWithValue("@STATUS", header.STATUS);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        using var cmd = new SqlCommand("sp_SalesProformaInvoice", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "DETAILS");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                        cmd.Parameters.AddWithValue("@ITEM_CODE", detail.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@PROD_DESC", detail.PROD_DESC);
                        cmd.Parameters.AddWithValue("@HSN_CODE", detail.HSN_CODE);
                        cmd.Parameters.AddWithValue("@NOS", detail.NOS);
                        cmd.Parameters.AddWithValue("@GROSS_QTY", detail.GROSS_QTY);
                        cmd.Parameters.AddWithValue("@QTY", detail.QTY);
                        cmd.Parameters.AddWithValue("@RATE", detail.RATE);
                        cmd.Parameters.AddWithValue("@AMOUNT", detail.AMOUNT);
                        cmd.Parameters.AddWithValue("@PACK_PER", detail.PACK_PER);
                        cmd.Parameters.AddWithValue("@PACK_AMT", detail.PACK_AMT);
                        cmd.Parameters.AddWithValue("@DISC_PER", detail.DISC_PER);
                        cmd.Parameters.AddWithValue("@DISC_AMT", detail.DISC_AMT);
                        cmd.Parameters.AddWithValue("@FREIGHT_AMT", detail.FREIGHT_AMT);
                        cmd.Parameters.AddWithValue("@CGST_PER", detail.CGST_PER);
                        cmd.Parameters.AddWithValue("@CGST_AMT", detail.CGST_AMT);
                        cmd.Parameters.AddWithValue("@SGST_PER", detail.SGST_PER);
                        cmd.Parameters.AddWithValue("@SGST_AMT", detail.SGST_AMT);
                        cmd.Parameters.AddWithValue("@IGST_PER", detail.IGST_PER);
                        cmd.Parameters.AddWithValue("@IGST_AMT", detail.IGST_AMT);
                        cmd.Parameters.AddWithValue("@CESS_PER", detail.CESS_PER);
                        cmd.Parameters.AddWithValue("@CESS_AMT", detail.CESS_AMT);
                        cmd.Parameters.AddWithValue("@REMARK", detail.REMARK);
                        cmd.Parameters.AddWithValue("@PACK_NO", detail.PACK_NO);
                        cmd.Parameters.AddWithValue("@LOT_NO", detail.LOT_No);
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE", detail.SAUDA_TYPE);
                        cmd.Parameters.AddWithValue("@SAUDA_NO", detail.SAUDA_NO);
                        cmd.Parameters.AddWithValue("@SAUDA_RATE", detail.SAUDA_RATE);
                        cmd.Parameters.AddWithValue("@ORD_TYPE", detail.ORD_TYPE);
                        cmd.Parameters.AddWithValue("@ORD_NO", detail.ORD_NO);
                        cmd.Parameters.AddWithValue("@ORD_RATE", detail.ORD_RATE);
                        cmd.Parameters.AddWithValue("@TAX_CODE", detail.TAX_CODE);
                         cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return ("Success", "Data Save Successfully");

            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

    }
}
