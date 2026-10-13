using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace travelexpensemanagement.Repositories.Implementations.Sale.Transaction
{
    public class SalesInvoiceDirect : ISalesInvoiceDirect
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;

        public SalesInvoiceDirect(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService)
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

        public async Task<(string Status, string Message)> validation(SalesInvoiceDirectModel_Header header, List<SalesInvoiceDirectModel_Detail> details, string action)
        {
            try
            {
                var GlobalData = _globalVariableService.GetGlobalVariables();
                var GeneralSetting = await _globalVariableService.LoadGeneralSetting();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                DataTable dt = new DataTable();
                   
                string issuevtype = "";
                Boolean chkval = false;
                Boolean WBReqCN = false;
                Boolean checkIssueNo = false;
                string packtyp = "";
                 
                if (GlobalData.PubCompCode == "1" && header.V_TYPE == "PSF")
                {
                    chkval = true;
                }
                else if ((GlobalData.PubCompCode == "2" || GlobalData.PubCompCode == "5" && (header.V_TYPE == "Fabric" || header.V_TYPE == "Sacks")))
                {
                    chkval = true;
                }
                else if (GlobalData.PubCompCode == "4" && header.V_TYPE == "Finish")
                {
                    chkval = true;
                }



                if(header.SUPPLY_TYPE== "EXPWOP" || header.SUPPLY_TYPE == "EXPWP")
                {
                }
                else
                {
                    if(header.V_TYPE  == "SATD")
                    {
                        string query = $@"select 1 from CRLIMIT_MAST where PARTY_CODE= {header.BILL_CODE} and COMP_CODE= {GlobalData.PubCompCode}";

                        if (IsExist(query))
                        {
                            String aptype = GetText($@"select APPROVAL_TYPE from CRLIMIT_MAST where PARTY_CODE={header.BILL_CODE}  and COMP_CODE= {GlobalData.PubCompCode}");

                            if(aptype.ToUpper() == "APPROVED" || aptype.ToUpper() == "NOT REPORTED")
                            {

                                Decimal pDrAmt = Convert.ToDecimal(GetText($@"select isnull(sum(AMT),0) from LEDGER2 where V_type='{header.V_TYPE}' and V_NO<>{header.V_NO} and DR_CODE={header.BILL_CODE} And Comp_Code={GlobalData.PubCompCode}"));
                                Decimal pCrAmt = Convert.ToDecimal(GetText($@"select isnull(sum(AMT),0) from LEDGER2 where CR_CODE={header.BILL_CODE} And Comp_Code={GlobalData.PubCompCode}"));

                                decimal totDrAmt = pDrAmt + Convert.ToDecimal(header.NAMOUNT ?? 0);

                                decimal BalAmt = totDrAmt - pCrAmt;

                                decimal crLimitAmt = Convert.ToDecimal(GetText($@"select isnull(CR_LIMIT,0) from CRLIMIT_MAST where PARTY_CODE={header.BILL_CODE} and COMP_CODE={GlobalData.PubCompCode}"));

                                decimal AMOUNT = BalAmt - crLimitAmt;


                                if (AMOUNT > 1)
                                {
                                    return ("Validation", "Total Sale Amount exceeds Credit Limit (Incl. this invoice). Invoice can not generated.");
                                }
                            }

                            else
                            {
                                String ourAppType = GetText(@$"select isnull(OURAPPROVAL_TYPE,'') from CRLIMIT_MAST where PARTY_CODE={header.BILL_CODE} and COMP_CODE={GlobalData.PubCompCode}");

                                if(ourAppType =="" )
                                {
                                    return ("Validation", "CR Limit Refused by Insurance company. Invoice can not generated.");

                                }

                            }                        

                        }
                        else
                        {
                            return ("Validation", "Party not exist in CR Limit Master. Invoice can not generated.");
                        }
                    }
                }

                if(header.TRANSPORT_NAME != "" || header.GR_NO != "")
                {
                    string purdocid = GetText($@"Select top 1 concat(V_TYPE,V_NO) from purchase1 where concat(v_type,v_no)<>'{header.V_TYPE}{header.V_NO}' and Transport_Name='{header.TRANSPORT_NAME}' and GR_NO='{header.GR_NO}' and CONCAT(v_type,v_no)<>'{header.V_TYPE}{header.V_NO} 
                    and v_type not in (Select code from Doctype_mast where doctype='MaterialReceipt') and comp_code={GlobalData.PubCompCode} and branch_code= {GlobalData.PubBranchCode} and Year_Code={GlobalData.PubFYearCode}");

                    if(purdocid != "")
                    {
                        return ("Validation", $"Transport Name {header.TRANSPORT_NAME} with GRNo={header.GR_NO} already exist in Purchase Bill/Direct Exps/JW/Imported Exps/Return No:{purdocid}");
                    }

                    string saledocid = GetText($@"Select top 1 concat(V_TYPE,V_NO) from Sale1 where isnull(Status,0)<>2 and concat(v_type,v_no)<> '{header.V_TYPE}{header.V_NO}' and Transport_Name='{header.TRANSPORT_NAME}' and GR_NO='{header.GR_NO}' and comp_code={GlobalData.PubCompCode} and branch_code={GlobalData.PubBranchCode} and Year_Code={GlobalData.PubFYearCode}");

                    if(saledocid != "")
                    {
                        return ("Validation", $"Transport Name {header.TRANSPORT_NAME} with GRNo={header.GR_NO} already exist in Sale/JW Issue/Sale Return invoice No :{purdocid}");

                    }
                }


                if(header.TRANSPORT_NAME != "" )
                {
                    int tptCode = Convert.ToInt16(GetText($@"SELECT PARTY_CODE FROM TRANSPORT_MAST WHERE CODE={header.TRANSPORT_CODE} and COMP_CODE={GlobalData.PubCompCode} and ACTIVE=1"));

                    if (tptCode == 0)
                    {
                        return ( "Validation", $"Party Name not Linked with Transport => {header.TRANSPORT_NAME}, Please update first." );
                    }
                    else if (!IsExist($@" SELECT 1 FROM SUBGROUP_MAST  WHERE CODE = {tptCode}  AND COMP_CODE = {GlobalData.PubCompCode} AND ACTIVE = 1 "))
                    {
                        return ( "Validation", $"Party not linked in Transport Master OR not Active/Exist in BP Master which is Linked with Transport => {header.TRANSPORT_NAME}" );
                    }
                }


                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        string hsncode = GetText($@"select isnull(HSN_CODE,'') from ITEM_MAST 
                        where code= {detail.ITEM_CODE} and Comp_code={GlobalData.PubCompCode} ");

                        if (hsncode.Length < 6)
                        {
                            return ("Validation", $"HSN Code must be 6 digit in Item Master of Item = {detail.ITEM_NAME} ");
                        }

                        if (hsncode.Length < 4)
                        {
                            if (hsncode.Length < 4)
                                return ("Validation", $"Invalid HSN Code in Item mast of Item = {detail.ITEM_NAME} ");
                        }
                                     
       
                  
                        if(header.ISSUE_NO > 0)
                        {
                            if(!IsExist(@$"select 1 from purchase2 where V_type='{header.ISSUE_TYPE}' and V_no={header.ISSUE_NO} and comp_code={GlobalData.PubCompCode} and year_code={GlobalData.PubFYearCode} and Branch_code={GlobalData.PubBranchCode}"))
                            {
                                if(!IsExist($@"select 1 from purchase2 where Item_code="" & Val(Datagridview2.Rows(i).Cells(1).Value) & "" and V_type='"" & txtIssueNo.Tag & ""' and V_no="" & Val(txtIssueNo.Text) & "" and comp_code="" & pubCompCode & "" and year_code="" & pubFYearCode & "" and Branch_code="" & pubBranchCode"))
                                {
                                    decimal issueQty = Convert.ToDecimal(@$"select sum(BILL_Qty) from purchase2 where Item_code={detail.ITEM_CODE} and V_type='{header.ISSUE_TYPE}' and V_no={header.ISSUE_NO} and comp_code={GlobalData.PubCompCode} and Branch_code={GlobalData.PubBranchCode}");

                                    if(issueQty != detail.QTY)
                                    {
                                        return ("Validation", $"Purchase Qty not matched with Sale Qty of Item:({detail.ITEM_CODE}) {detail.ITEM_NAME} Purchase No: {header.ISSUE_NO} ");
                                    }
                                }
                                else
                                {
                                    return ("Validation", $"Sale Item and Issue Item not matched, please check for Item:({detail.ITEM_CODE}) {detail.ITEM_NAME}, Purchase No:{header.ISSUE_NO}");

                                }
                            }
                            else
                            {
                                return ("Validation", $"Purchase No: {header.ISSUE_NO} not found.");

                            }

                        }


                        if(header.V_TYPE  != "SATD")
                        {
                            string mgroup = GetText(@$"Select SALE_GROUP from ITEM_MAST a LEFT JOIN item_group b ON a.GROUP_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE where a.CODE={detail.ITEM_CODE} and a.COMP_CODE={GlobalData.PubCompCode}");

                            if(mgroup != header.ITEM_TYPE)
                            {
                                return ("Validation", $"Item Name={detail.ITEM_NAME} (Group:{mgroup}), but you selected Product Type : {header.ITEM_TYPE} in header.");

                            }
                        }



                        Decimal csg = Convert.ToDecimal(GetText(@$"Select isnull(CGST_PER,0) from ITEM_MAST where Code={detail.ITEM_CODE} and COMP_CODE={GlobalData.PubCompCode}"));
                        Decimal ig = Convert.ToDecimal(GetText(@$"Select isnull(IGST_PER,0) from ITEM_MAST where Code={detail.ITEM_CODE} and COMP_CODE={GlobalData.PubCompCode}"));

                        decimal Total = csg + ig;

                        if(Total > 0)
                        {
                            if(detail.CGST_PER > 0)
                            {
                                return ("Validation", $"Tax Percentage of Item =>{detail.ITEM_NAME} not matched as per Item Master.");

                            }
                        }
                        else if (detail.IGST_PER > 0 )
                        {
                            if(detail.IGST_PER != ig)
                            {
                                return ("Validation", $"Tax Percentage of Item =>{detail.ITEM_NAME} not matched as per Item Master.");
                            }
                        }


                        if(header.V_TYPE == "SATD")
                        {
                            if(GeneralSetting.pubDefSSINSI == "Yes")
                            {
                                if(detail.ITEM_CODE > 0 && detail.SAUDA_NO > 0)
                                {
                                    if(header.FRT_AMT > 0)
                                    {
                                        string FRT_TERM = GetText(@$"Select isnull(FRT_TERM,'') from SAUDA where v_type='SAUD' and v_NO={detail.SAUDA_NO} and COMP_CODE= {GlobalData.PubCompCode} and BRANCH_CODE={GlobalData.PubBranchCode}");
                                      
                                        string FRT_TERM1 = GetText(@$"Select isnull(FRT_TERM,'') from SAUDA where v_type='SAUD' and v_NO={detail.SAUDA_NO} and COMP_CODE={GlobalData.PubCompCode} and BRANCH_CODE={GlobalData.PubBranchCode}");


                                        if(FRT_TERM == "FOR" || FRT_TERM1 == "FOR-TPT")
                                        {

                                        }
                                        else
                                        {
                                            return ("Validation", $"Freight payable by customer in Sauda No { detail.SAUDA_NO}, so freight can not be charged.");

                                        }


                                    }
                                }
                            }
                        }








                    }

                    string StateCode = GetText(@$"select State_Code from CITY_MAST where code={header.BILL_CITY}");
                    string StateType = "";

                    if (GlobalData.STATE_CODE == StateCode)
                    {
                        StateType = "Local";
                    }
                    else
                    {
                        StateType = "Central/Other";
                    }

                    if (GlobalData.STATE_CODE == StateCode && header.IGST_AMT > 0)
                    {
                        return ("Validation", $"IGST Not applicable as per Party State type is {StateType}");
                    }

                    else if (GlobalData.STATE_CODE != StateCode && (header.CGST_AMT + header.SGST_AMT) > 0)
                    {
                        return ("Validation", $"CGST/SGST not applicable as per Party State type is {StateType}");
                    }

                    if ((header.CGST_AMT + header.SGST_AMT) > 0 && header.IGST_AMT > 0)
                    {
                        return ("Validation", $"Both GST Tax Rate is not Applicable in One Sales Invoice (CGST+SGST & IGST)");
                    }

                    if(header.CGST_AMT != header.SGST_AMT)
                    {
                     return ("Validation", $"CGST & SGST Amount Should Be Same.");
                    }
                }

                return ("Success", "Data Save Successfully");

            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

        public async Task<(string Status, string Message)> SubmitRequest(SalesInvoiceDirectModel_Header header, List<SalesInvoiceDirectModel_Detail> details, string action)
        {
            try
            {

                var validationResult = await validation(header, details, action);

                if (validationResult.Status != "Success")
                {
                    return (validationResult.Status, validationResult.Message);
                }

                var GlobalData = _globalVariableService.GetGlobalVariables();
                var GeneralSetting = await  _globalVariableService.LoadGeneralSetting();

                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                DataTable dt = new DataTable();

                string fappstatus = "";
                string fappRemark = "";
                string fappUserCode = "";
                Boolean isApprovalBody = false;
                Boolean isFinalApprovalBody = false;
                Boolean isFinalApprovalBodyCS = false;
                Boolean isFinalApprovalBodyLCS = false;
                string issuevtype = "";
                Boolean chkval = false;
                Boolean WBReqCN = false;
                Boolean checkIssueNo  = false;
                string packtyp = "";

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
                string query4 = $@"select APPROV_USER from DOC_APPROSTAGE where FLAG_B='LCS' and USER_CODE={GlobalData.PubUserId} and DOC_CODE='{header.V_TYPE}'
                and comp_code={GlobalData.PubCompCode}";

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
                  
                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;

                using (var cmd = new SqlCommand("sp_SalesInvoiceDirect", conn))
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
                        cmd.Parameters.AddWithValue("@BILL_CODE", header.BILL_CODE);
                        cmd.Parameters.AddWithValue("@BILL_NAME", header.BILL_NAME);
                        cmd.Parameters.AddWithValue("@BILL_ADD1", header.BILL_ADD1);
                        cmd.Parameters.AddWithValue("@BILL_ADD2", header.BILL_ADD2);
                        cmd.Parameters.AddWithValue("@BILL_ADD3", header.BILL_ADD3);
                        cmd.Parameters.AddWithValue("@BILL_CITY", header.BILL_CITY);
                        cmd.Parameters.AddWithValue("@BILL_GST", header.BILL_GST);
                        cmd.Parameters.AddWithValue("@BILL_PINCODE", header.BILL_PINCODE);
                        cmd.Parameters.AddWithValue("@INSUCR_DAYS", header.INSUCR_DAYS);
                        cmd.Parameters.AddWithValue("@SHIP_CODE", header.SHIP_CODE);
                        cmd.Parameters.AddWithValue("@SHIP_NAME", header.SHIP_NAME);
                        cmd.Parameters.AddWithValue("@SHIP_ADD1", header.SHIP_ADD1);
                        cmd.Parameters.AddWithValue("@SHIP_ADD2", header.SHIP_ADD2);
                        cmd.Parameters.AddWithValue("@SHIP_ADD3", header.SHIP_ADD3);
                        cmd.Parameters.AddWithValue("@SHIP_CITY", header.SHIP_CITY);
                        cmd.Parameters.AddWithValue("@SHIP_GST", header.SHIP_GST);
                        cmd.Parameters.AddWithValue("@SHIP_PINCODE", header.SHIP_PINCODE);
                        cmd.Parameters.AddWithValue("@TAX_CODE", header.TAX_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE);
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
                        cmd.Parameters.AddWithValue("@GR_DATE", (object?)header.GR_DATE ?? DBNull.Value);
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
                        cmd.Parameters.AddWithValue("@disc_per", header.DISC_PER);
                        cmd.Parameters.AddWithValue("@disc_amt", header.DISC_AMT);
                        cmd.Parameters.AddWithValue("@cdisc_per", header.CDISC_PER);
                        cmd.Parameters.AddWithValue("@cdisc_amt", header.CDISC_AMT);
                        cmd.Parameters.AddWithValue("@AGENT_CODE", header.AGENT_CODE);
                        cmd.Parameters.AddWithValue("@FORM_CODE", header.FORM_CODE);
                        cmd.Parameters.AddWithValue("@DELIVERY_TERMS", header.DELIVERY_TERMS);
                        cmd.Parameters.AddWithValue("@Supply_type", header.SUPPLY_TYPE);
                        cmd.Parameters.AddWithValue("@LC_NO", header.LC_NO);
                        cmd.Parameters.AddWithValue("@PAY_TERM", header.PAY_TERM);
                        cmd.Parameters.AddWithValue("@SHIPMENT_TYPE", header.SHIPMENT_TYPE);
                        cmd.Parameters.AddWithValue("@TRAN_TYPE", header.TRAN_TYPE);
                        cmd.Parameters.AddWithValue("@STATUS", header.STATUS);
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fappRemark);
                        cmd.Parameters.AddWithValue("@APPROVAL_USER", fappUserCode);         
                        cmd.Parameters.AddWithValue("@UUSER", GlobalData.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", GlobalData.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", GlobalData.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", GlobalData.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    await cmd.ExecuteNonQueryAsync();
                }
                                                
                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;               

                        using var cmd = new SqlCommand("sp_SalesInvoiceDirect", conn)
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
                        cmd.Parameters.AddWithValue("@SNO", detail.SNO);
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

                if (isFinalApprovalBody == true)
                {
                    if (fappstatus != "")
                    {
                        string checkQuery = @" SELECT 1  FROM approval_status  WHERE user_Code = @USER_CODE AND V_Type = @V_TYPE
                            AND V_No = @V_NO AND COMP_CODE = @COMP_CODE  AND Branch_Code = @BRANCH_CODE  AND Year_Code = @YEAR_CODE";

                        using (var cmd = new SqlCommand(checkQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@USER_CODE", GlobalData.PubUserId);
                            cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);

                            var exists = await cmd.ExecuteScalarAsync();

                            if (exists != null)
                            {
                                string updateQuery = @"
                                    UPDATE approval_status SET STATUS = 'CLOSE',  CLOSE_DATE = FORMAT(GETDATE(), 'yyyy-MM-dd HH:mm'),
                                    Approval_code = 8, Approval_remark = 'Approved', remarks = 'Document Approved'
                                    WHERE V_Type = @V_TYPE AND V_No = @V_NO AND COMP_CODE = @COMP_CODE  AND Branch_Code = @BRANCH_CODE
                                    AND Year_Code = @YEAR_CODE";

                                using (var updateCmd = new SqlCommand(updateQuery, conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                                    updateCmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                                    updateCmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                                    updateCmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                                    updateCmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);

                                    await updateCmd.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }
                }

                return ("Success", "Data Save Successfully");

            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

        public bool IsExist(string query)
        {
            try
            {
                using var conn = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand(query, conn);

                conn.Open();

                using var reader = cmd.ExecuteReader();

                return reader.Read();
            }
            catch (Exception ex)
            {        

                return false;
            }
        }
             
    }
}
