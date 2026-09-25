using AngleSharp.Text;
using DocumentFormat.OpenXml.Math;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using StackExchange.Redis;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sale.Transaction
{
    public class SalesInVoice : ISalesInVoice
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;

        public SalesInVoice(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService)
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

        public async Task<(string Status, string Message)> SubmitRequest(SalesInvoiceModel_Header header, List<SalesInvoiceModel_Detail> details, string action)
        {
            try
            {
                var GlobalData = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                DataTable dt = new DataTable();

                string fappstatus = "";
                string fappRemark = "";
                string fappUserCode = "";

                Boolean isApprovalBody = false;
                Boolean isFinalApprovalBody = false;
                Boolean isFinalApprovalBodyCS = false;
                Boolean isFinalApprovalBodyLCS = false;



                string qyery = $@"select 1 from DOC_APPROSTAGE where USER_CODE={GlobalData.PubUserId} and DOC_CODE='{header.V_TYPE}' and comp_code={GlobalData.PubCompCode}";

                 string Approval = GetText(qyery);


                if(Approval == "1")
                {
                    isApprovalBody = true;
                }

                string query2 = $@"select APPROV_USER from DOC_APPROSTAGE where USER_CODE={GlobalData.PubUserId}   and DOC_CODE='{header.V_TYPE}' and comp_code={GlobalData.PubCompCode}";

                string APPROV_USER = GetText(query2);


                if(APPROV_USER == "FINAL")
                {
                    isFinalApprovalBody = true;
                }

                string query3 = $@"select APPROV_USER from DOC_APPROSTAGE where FLAG_B='CS' and USER_CODE={GlobalData.PubUserId} and DOC_CODE='{header.V_TYPE}' and comp_code={GlobalData.PubCompCode} ";


                APPROV_USER = GetText(query3);

                if(APPROV_USER == "FINAL")
                {
                    isFinalApprovalBodyCS = true;
                }
                string query4 = $@"select APPROV_USER from DOC_APPROSTAGE where FLAG_B='LCS' and USER_CODE={GlobalData.PubUserId} and DOC_CODE='{header.V_TYPE}' and comp_code={GlobalData.PubCompCode}";

                APPROV_USER = GetText(query4);


                if(APPROV_USER == "FINAL")
                {
                    isFinalApprovalBodyLCS = true;
                }
                if (isFinalApprovalBody = true)
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                    fappUserCode = GlobalData.PubUserId.ToString();
                }
               
                if(header.V_TYPE != "SAJI")
                {

                    decimal clbl = 0;
                    decimal CrLimit = 0;
                    decimal LastCrLimit = 0;

                    string query5 = $@"select isnull(sum(Amt),0) as Amt from ledger2 where CR_CODE= {header.BILL_CODE}  and COMP_CODE={GlobalData.PubCompCode} and concat(V_type,V_no)<> '{header.V_TYPE} {header.V_NO}'";
                    clbl  -= Convert.ToDecimal(GetText(query5));
                 
                    string query6 = $@"select isnull( sum(Amt),0) as Amt  from ledger2 where DR_CODE= {header.BILL_CODE}  and COMP_CODE= {GlobalData.PubCompCode} and concat(V_type,V_no)<>'{header.V_TYPE}{header.V_NO}'";
                    clbl += Convert.ToDecimal(GetText(query6));


                    string query7 = $@"select ISNULL(b.Credit_type,'')Credit_type from SUBGROUP_MAST a left join Payterm_mast b on a.Payterm_code=b.code and a.comp_code=b.comp_code
                                    where a.CODE={header.BILL_CODE} and a.comp_Code={GlobalData.PubCompCode}";

                    string Credit_type = GetText(query7);

                    if(Credit_type == "Against LC" && header.LC_NO  == "")
                    {
                        return ("Validation", "LC Number Required. Otherwise invoice will not Approve.");
                        fappstatus = "";
                        fappRemark = "";
                    }
                }

                if(GlobalData.PubUserId == "1")
                {
                    if(isFinalApprovalBodyLCS == false)
                    {

                        if (header.V_TYPE == "SAGT" && header.PACK_NO <= 0)
                        {
                            foreach (var detail in details)
                            {

                                if(detail.ITEM_CODE != null)
                                {
                                    if(detail.QTY != detail.WBQTY)
                                    {
                                        fappstatus = "";
                                        fappRemark = "";

                                        return ("Validation", "WB Qty and Net Qty not matched, invoice will not Approve.");

                                    }
                                }
                            }

                        }
                    }
                }

                await conn.OpenAsync();

                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;

                using (var cmd = new SqlCommand("sp_SalesInvoice", conn))
                {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "HEADER");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;

                        cmd.Parameters.AddWithValue("@GODOWN_CODE", header.GODOWN_CODE);
                        cmd.Parameters.AddWithValue("@BILL_CODE", header.BILL_CODE);
                        cmd.Parameters.AddWithValue("@BILL_NAME", header.BILL_NAME);
                        cmd.Parameters.AddWithValue("@BILL_ADD1", header.BILL_ADD1);
                        cmd.Parameters.AddWithValue("@BILL_ADD2", header.BILL_ADD2);
                        cmd.Parameters.AddWithValue("@BILL_ADD3", header.BILL_ADD3);
                        cmd.Parameters.AddWithValue("@BILL_CITY", header.BILL_CITY);
                        cmd.Parameters.AddWithValue("@BILL_CITYName", header.BILL_CITYName);

                        cmd.Parameters.AddWithValue("@BILL_STATE", header.BILL_STATE);
                        cmd.Parameters.AddWithValue("@BILL_STATENAME", header.BILL_STATENAME);
                        cmd.Parameters.AddWithValue("@BILL_COUNTRY", header.BILL_COUNTRY);
                        cmd.Parameters.AddWithValue("@BILL_COUNTRYNAME", header.BILL_COUNTRYNAME);
                        cmd.Parameters.AddWithValue("@BILL_GST", header.BILL_GST);
                        cmd.Parameters.AddWithValue("@BILL_PINCODE", header.BILL_PINCODE);
                        cmd.Parameters.AddWithValue("@INSUCR_DAYS", header.INSUCR_DAYS);

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
                     
                       cmd.Parameters.AddWithValue("@TAX_CODE", header.TAX_CODE);
                        cmd.Parameters.AddWithValue("@PACK_TYPE", header.PACK_TYPE);
                        cmd.Parameters.AddWithValue("@PACK_NO", header.PACK_NO);
                        cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE);
                        cmd.Parameters.AddWithValue("@WB_NO", header.WB_NO);
                        cmd.Parameters.AddWithValue("@WB_TYPE", header.WB_TYPE);
                        cmd.Parameters.AddWithValue("@ISSUE_NO", header.ISSUE_NO);
                        cmd.Parameters.AddWithValue("@ISSUE_TYPE", header.ISSUE_TYPE);
                        cmd.Parameters.AddWithValue("@AMOUNT", header.AMOUNT);
                        cmd.Parameters.AddWithValue("@PACK_PER", header.PACK_PER);
                        cmd.Parameters.AddWithValue("@PACK_AMT", header.PACK_AMT);
                        cmd.Parameters.AddWithValue("@TCS_PER", header.TCS_PER);
                        cmd.Parameters.AddWithValue("@TCS_AMT", header.TCS_AMT);
                        cmd.Parameters.AddWithValue("@CGST_PER", header.CGST_PER);
                        cmd.Parameters.AddWithValue("@CGST_AMT", header.CGST_AMT);
                        cmd.Parameters.AddWithValue("@SGST_PER", header.SGST_PER);
                        cmd.Parameters.AddWithValue("@SGST_AMT", header.SGST_AMT);
                        cmd.Parameters.AddWithValue("@IGST_PER", header.IGST_PER);
                        cmd.Parameters.AddWithValue("@IGST_AMT", header.IGST_AMT);
                        cmd.Parameters.AddWithValue("@CESS_PER", header.CESS_PER);
                        cmd.Parameters.AddWithValue("@CESS_AMT", header.CESS_AMT);
                        cmd.Parameters.AddWithValue("@LOAD_PER", header.LOAD_PER);
                        cmd.Parameters.AddWithValue("@LOAD_AMT", header.LOAD_AMT);
                        cmd.Parameters.AddWithValue("@LOAD_AC", header.LOAD_AC);
                        cmd.Parameters.AddWithValue("@LOAD_REM", header.LOAD_REM);
                        cmd.Parameters.AddWithValue("@WB_AMT", header.WB_AMT);
                        cmd.Parameters.AddWithValue("@WB_AC", header.WB_AC);
                        cmd.Parameters.AddWithValue("@PAYMENT_TERM", header.PAYMENT_TERM);
                        cmd.Parameters.AddWithValue("@TOT_NOS", header.TOT_NOS);
                        cmd.Parameters.AddWithValue("@FRT_AMT", header.FRT_AMT);
                        cmd.Parameters.AddWithValue("@ROUND_OFF", header.ROUND_OFF);
                        cmd.Parameters.AddWithValue("@NAMOUNT", header.NAMOUNT);
                        cmd.Parameters.AddWithValue("@INSU_PER", header.INSU_PER);
                        cmd.Parameters.AddWithValue("@INSU_AMT", header.INSU_AMT);
                        cmd.Parameters.AddWithValue("@TDS_PER", header.TDS_PER);
                        cmd.Parameters.AddWithValue("@TDS_AMT", header.TDS_AMT);
                        cmd.Parameters.AddWithValue("@FRT_TAXPER", header.FRT_TAXPER);
                        cmd.Parameters.AddWithValue("@FRT_TAXAMT", header.FRT_TAXAMT);
                        cmd.Parameters.AddWithValue("@WB_REM", header.WB_REM);
                        cmd.Parameters.AddWithValue("@TOT_GROSS", header.TOT_GROSS);
                        cmd.Parameters.AddWithValue("@TOT_NET", header.TOT_NET);
                        cmd.Parameters.AddWithValue("@GR_NO", header.GR_NO);
                        cmd.Parameters.Add("@GR_DATE", SqlDbType.SmallDateTime).Value = header.GR_DATE;               
                        cmd.Parameters.AddWithValue("@VEHICLE_NO", header.VEHICLE_NO);
                        cmd.Parameters.AddWithValue("@TRANSPORT_CODE", header.TRANSPORT_CODE);
                        cmd.Parameters.AddWithValue("@TRANSPORT_NAME", header.TRANSPORT_NAME);
                        cmd.Parameters.AddWithValue("@DRIVER_NAME", header.DRIVER_NAME);
                        cmd.Parameters.AddWithValue("@DRIVER_NO", header.DRIVER_NO);
                        cmd.Parameters.AddWithValue("@TPT_MODE", header.TPT_MODE);
                        cmd.Parameters.AddWithValue("@TPT_DISTANCE", header.TPT_DISTANCE);
                        cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO);
                        cmd.Parameters.AddWithValue("@FRT_TOPAY", header.FRT_TOPAY);
                        cmd.Parameters.AddWithValue("@REMARK", header.REMARK);
                        cmd.Parameters.AddWithValue("@WB_QTY", header.WB_QTY);
                        cmd.Parameters.AddWithValue("@disc_per", header.DISC_PER);
                        cmd.Parameters.AddWithValue("@disc_amt", header.DISC_AMT);
                        cmd.Parameters.AddWithValue("@cdisc_per", header.CDISC_PER);
                        cmd.Parameters.AddWithValue("@CDISC_AMT", header.CDISC_AMT);
                        cmd.Parameters.AddWithValue("@AGENT_CODE", header.AGENT_CODE);
                        cmd.Parameters.AddWithValue("@FORM_CODE", header.FORM_CODE);
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE", header.SAUDA_TYPE);
                        cmd.Parameters.AddWithValue("@SAUDA_NO", header.SAUDA_NO);
                        cmd.Parameters.AddWithValue("@SAUDA_RATE", header.SAUDA_RATE);
                        cmd.Parameters.AddWithValue("@DEFECTIVE_GOODS", header.DEFECTIVE_GOODS);
                        cmd.Parameters.AddWithValue("@CAL_ONPCS", header.CAL_ONPCS);
                        cmd.Parameters.AddWithValue("@PRINT_DETAIL", header.PRINT_DETAIL);
                        cmd.Parameters.AddWithValue("@EXRATE", header.EXRATE);
                        cmd.Parameters.AddWithValue("@BUYER_ORDNO", header.BUYER_ORDNO);
                        cmd.Parameters.AddWithValue("@PLACE_RECEIPT", header.PLACE_RECEIPT);
                        cmd.Parameters.AddWithValue("@PORT_LOADING", header.PORT_LOADING);
                        cmd.Parameters.AddWithValue("@PORT_DISCHARGE", header.PORT_DISCHARGE);
                        cmd.Parameters.AddWithValue("@FINAL_DEST", header.FINAL_DEST);
                        cmd.Parameters.AddWithValue("@FINAL_DEST_COUNTRY", header.FINAL_DEST_COUNTRY);
                        cmd.Parameters.AddWithValue("@DELIVERY_TERMS", header.DELIVERY_TERMS);
                        cmd.Parameters.AddWithValue("@SB_NO", header.SB_NO);
                        cmd.Parameters.AddWithValue("@SB_DATE", header.SB_DATE);
                        cmd.Parameters.AddWithValue("@PORT_CODE", header.PORT_CODE);
                        cmd.Parameters.AddWithValue("@FOB_VALUE", header.FOB_VALUE);
            
                        cmd.Parameters.AddWithValue("@FOB_FRT", header.FOB_FRT);
                        cmd.Parameters.AddWithValue("@FOB_INSU", header.FOB_INSU);
                        cmd.Parameters.AddWithValue("@FOB_OTHER", header.FOB_OTHER);
                        cmd.Parameters.AddWithValue("@LUT_DETAIL", header.LUT_DETAIL);
                        cmd.Parameters.AddWithValue("@LUT_NO", header.LUT_NO);
                        cmd.Parameters.AddWithValue("@LUT_DATE", header.LUT_DATE);
                        cmd.Parameters.AddWithValue("@INSU_DETAIL", header.INSU_DETAIL);
                        cmd.Parameters.AddWithValue("@INCOTERM", header.INCOTERM);
                        cmd.Parameters.AddWithValue("@BILLOF_LADING", header.BILLOF_LADING);
                        cmd.Parameters.AddWithValue("@Supply_type", header.SUPPLY_TYPE);
                        cmd.Parameters.AddWithValue("@LC_NO", header.LC_NO);
                        //cmd.Parameters.AddWithValue("@LICENCE_NO", header.);
                        //cmd.Parameters.AddWithValue("@LICENCE_TYPE", header.);
                        //cmd.Parameters.AddWithValue("@LICENCE_DATE", header.);
                        cmd.Parameters.AddWithValue("@SHIPMENT_TYPE", header.SHIPMENT_TYPE);
                        cmd.Parameters.AddWithValue("@TRAN_TYPE", header.TRAN_TYPE);
                   
                        cmd.Parameters.AddWithValue("@CURRENCY", header.CURRENCY);
                    
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fappRemark);
                        cmd.Parameters.AddWithValue("@APPROVAL_USER", fappUserCode);
         
                        cmd.Parameters.AddWithValue("@STATUS", header.STATUS);
                        cmd.Parameters.AddWithValue("@UUSER", GlobalData.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", GlobalData.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", GlobalData.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", GlobalData.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    await cmd.ExecuteNonQueryAsync();
                }



                if(header.Do_NO != "")
                {
                    using (var cmd = new SqlCommand("sp_SalesInvoice", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DONO");

                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.AddWithValue("@DoType", (object?)header.DoType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Do_NO", header.Do_NO);


                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                

                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        using var cmd = new SqlCommand("sp_SalesInvoice", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "DETAILS");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);                 
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);

                        cmd.Parameters.AddWithValue("@ITEM_CODE", detail.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", detail.ITEM_NAME);
                        cmd.Parameters.AddWithValue("@UNIT_CODE", detail.UNIT_CODE);
                        cmd.Parameters.AddWithValue("@UNIT_NAME", detail.UNIT_NAME);                 
                        
                        cmd.Parameters.AddWithValue("@HSN_CODE", detail.HSN_CODE);
                        cmd.Parameters.AddWithValue("@NOS", detail.NOS);
                        cmd.Parameters.AddWithValue("@GROSS_QTY", detail.GROSS_QTY);
                        cmd.Parameters.AddWithValue("@QTY", detail.QTY);
                        cmd.Parameters.AddWithValue("@FOR_RATE", detail.FOR_RATE);
                        cmd.Parameters.AddWithValue("@RATE", detail.RATE);
                        cmd.Parameters.AddWithValue("@AMOUNT", detail.AMOUNT);
                        cmd.Parameters.AddWithValue("@PACK_PER", detail.PACK_PER);
                        cmd.Parameters.AddWithValue("@PACK_AMT", detail.PACK_AMT);
                        cmd.Parameters.AddWithValue("@DISC_PER", detail.DISC_PER);
                        cmd.Parameters.AddWithValue("@DISC_AMT", detail.DISC_AMT);

                        cmd.Parameters.AddWithValue("@CGST_PER", detail.CGST_PER);
                        cmd.Parameters.AddWithValue("@CGST_AMT", detail.CGST_AMT);
                        cmd.Parameters.AddWithValue("@SGST_PER", detail.SGST_PER);
                        cmd.Parameters.AddWithValue("@SGST_AMT", detail.SGST_AMT);
                        cmd.Parameters.AddWithValue("@IGST_PER", detail.IGST_PER);
                        cmd.Parameters.AddWithValue("@IGST_AMT", detail.IGST_AMT);
                        cmd.Parameters.AddWithValue("@CESS_PER", detail.CESS_PER);
                        cmd.Parameters.AddWithValue("@CESS_AMT", detail.CESS_AMT);
                        cmd.Parameters.AddWithValue("@REMARK", detail.REMARK);
                        cmd.Parameters.AddWithValue("@STATUS", detail.STATUS);

                        cmd.Parameters.AddWithValue("@PACK_NO", detail.PACK_NO);
                        cmd.Parameters.AddWithValue("@PACK_TYPE", detail.PACK_TYPE);
                        cmd.Parameters.AddWithValue("@lot_no", detail.LOT_No);
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE", detail.SAUDA_TYPE);
                        cmd.Parameters.AddWithValue("@SAUDA_NO", detail.SAUDA_NO);
                        cmd.Parameters.AddWithValue("@SAUDA_RATE", detail.SAUDA_RATE);
                        cmd.Parameters.AddWithValue("@ORD_TYPE", detail.ORD_TYPE);
                        cmd.Parameters.AddWithValue("@ORD_NO", detail.ORD_NO);                  
                        cmd.Parameters.AddWithValue("@ORD_RATE", detail.ORD_RATE);
                        cmd.Parameters.AddWithValue("@DCN_TYPE", detail.DCN_TYPE);
                        cmd.Parameters.AddWithValue("@DCN_NO", detail.DCN_NO);
                        cmd.Parameters.AddWithValue("@TAX_CODE", detail.TAX_CODE);
                        cmd.Parameters.AddWithValue("@FRT_AMT", detail.FRT_AMT);
                        cmd.Parameters.AddWithValue("@INSU_AMT", detail.INSU_AMT);
                        cmd.Parameters.AddWithValue("@CDISC_AMT", detail.CDISC_AMT);
                        cmd.Parameters.AddWithValue("@WBQTY", detail.WBQTY);
                         cmd.Parameters.AddWithValue("@UUSER", GlobalData.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", GlobalData.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", GlobalData.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", GlobalData.PubLocalId);
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
