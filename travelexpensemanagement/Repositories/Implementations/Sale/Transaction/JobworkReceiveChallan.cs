using AngleSharp.Text;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using StackExchange.Redis;
using System.Data;
using System.Reflection.Metadata;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sale.Transaction
{
    public class JobworkReceiveChallan : IJobworkReceiveChallan
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        public JobworkReceiveChallan(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService)
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

        public async Task<(string Status, string Message)> validation( JobworkReceiveChallanModel_Header header, List<JobworkReceiveChallanModel_Detail> details,string action)
        {
            try
            {
                var GlobalData = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                await conn.OpenAsync();

                bool chkval = false;
                string packtyp = "";
                string sql = "";

                // 1. Gate Entry Party Validation
                if (header.GATE_NO != 0)
                {
                    sql = $@" SELECT PARTY_CODE FROM GATE1  WHERE V_TYPE = '{header.V_TYPE}'  AND V_NO = {header.V_NO}
                    AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}";

                    string partycode = GetText(sql);

                    if (!string.IsNullOrWhiteSpace(partycode))
                    {
                        if (!int.TryParse(partycode.Trim(), out int code))
                        {
                            return ("Validation",  $"Invalid party code found for Gate Entry No. {header.GATE_NO}.");
                        }

                        if (header.BILL_CODE != code)
                        {
                            return ("Validation",  $"Party not matched with Gate Entry no.={header.GATE_NO}");
                        }
                    }
                }

                // 2. Company-wise Item Type Validation
                if (GlobalData.PubCompCode == "1" && header.ITEM_TYPE == "PSF")
                {
                    chkval = true;
                }
                else if ((GlobalData.PubCompCode == "2" ||  GlobalData.PubCompCode == "5") && (header.ITEM_TYPE == "Fabric" ||  header.ITEM_TYPE == "Sacks"))
                {
                    chkval = true;
                }
                else if (GlobalData.PubCompCode == "4" && header.ITEM_TYPE == "Finish")
                {
                    chkval = true;
                }

                // 3. Packing Number Validation
                if (chkval && header.V_TYPE == "SAJR")
                {
                    // Assumes PACK_NO = 0 means no packing number.
                    if (header.PACK_NO == 0)
                    {
                        return ("Validation", "Packing No. cannot be blank, Please Check it.");
                    }
                }

                // 4. Packing Type
                if (header.V_TYPE == "SAJR")
                {
                    if (GlobalData.PubCompCode == "2" ||  GlobalData.PubCompCode == "5")
                    {
                        packtyp = "FPJR";
                    }
                    else
                    {
                        packtyp = "FFRC";
                    }
                }

                // 5. Packing Date Validation
                // Return the date as dd/MM/yyyy to avoid culture-related parsing issues.
                sql = $@"
                SELECT CONVERT(VARCHAR(10), V_DATE, 103) FROM PRODUCTION1
                WHERE V_NO = {header.V_NO}  AND V_TYPE = '{header.V_TYPE}'  AND COMP_CODE = {GlobalData.PubCompCode} AND 
                BRANCH_CODE = {GlobalData.PubBranchCode}";

                string packDateText = GetText(sql);

                if (string.IsNullOrWhiteSpace(packDateText))
                {
                    return ("Validation", $"Packing date not found for Document Type: {header.V_TYPE}, " + $"Document No: {header.V_NO}. Please check the Production Entry.");
                }

                if (!DateTime.TryParseExact(  packDateText.Trim(),  "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,  System.Globalization.DateTimeStyles.None, out DateTime packdate))
                {
                    return ("Validation", $"Invalid packing date '{packDateText}'. Please check the Production Entry.");
                }

                if (header.V_DATE != packdate.Date)
                {
                    return ("Validation", $"Packing Date {packdate:dd/MM/yyyy} does not match with Invoice date, Please Check it.");
                }

                // 6. Duplicate Transport Name and GR Number Validation
                if (header.TRANSPORT_CODE != 0 &&
                    !string.IsNullOrWhiteSpace(header.GR_NO))
                {
                    sql = $@"
                        SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM purchase1  WHERE CONCAT(V_TYPE, V_NO) <> '{header.V_TYPE}{header.V_NO}'
                        AND Transport_Name = '{header.TRANSPORT_NAME}' AND GR_NO = '{header.GR_NO}' AND V_TYPE NOT IN
                        (
                        SELECT CODE  FROM Doctype_mast WHERE DOCTYPE = 'MaterialReceipt'
                        )
                        AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode} AND Year_Code = {GlobalData.PubFYearCode}";

                    string purdocid = GetText(sql);

                    if (!string.IsNullOrWhiteSpace(purdocid))
                    {
                        return ("Validation",
                            $"Transport Name '{header.TRANSPORT_NAME}' with GRNo='{header.GR_NO}' already exists in Purchase Bill/Direct Exps/JW/Imported Exps/Return No: {purdocid}");
                    }

                    sql = $@"
                        SELECT TOP 1 CONCAT(V_TYPE, V_NO)
                        FROM Sale1
                        WHERE CONCAT(V_TYPE, V_NO) <> '{header.V_TYPE}{header.V_NO}'
                        AND Transport_Name = '{header.TRANSPORT_NAME}'
                        AND GR_NO = '{header.GR_NO}'
                        AND COMP_CODE = {GlobalData.PubCompCode}
                        AND BRANCH_CODE = {GlobalData.PubBranchCode}";

                    string saledocid = GetText(sql);

                    if (!string.IsNullOrWhiteSpace(saledocid))
                    {
                        return ("Validation",
                            $"Transport Name '{header.TRANSPORT_NAME}' with GRNo='{header.GR_NO}' already exists in Sales documents. Document No: {saledocid}");
                    }
                }

                // 7. Detail Item Group Validation
                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        sql = $@"
                        SELECT b.SALE_GROUP  FROM ITEM_MAST a  LEFT JOIN ITEM_GROUP b  ON a.GROUP_CODE = b.CODE  AND a.COMP_CODE = b.COMP_CODE
                        WHERE a.CODE = {detail.ITEM_CODE}  AND a.COMP_CODE = {GlobalData.PubCompCode}";

                        string mgroup = GetText(sql);

                        if (string.IsNullOrWhiteSpace(mgroup) ||
                            mgroup.Trim() != (header.ITEM_TYPE ?? "").Trim())
                        {
                            return ("Validation", $"Item '{detail.ITEM_NAME}' Group: {mgroup}, but you selected Product Type: {header.ITEM_TYPE}");
                        }
                    }
                }

                // 8. GST Validation for PSF
                if (header.ITEM_TYPE == "PSF")
                {
                    if (header.BILL_CODE > 0)
                    {
                        sql = $@" SELECT STATE_CODE FROM SUBGROUP_MAST WHERE CODE = {header.BILL_CODE}  AND COMP_CODE = {GlobalData.PubCompCode}";

                        string StateCode = GetText(sql);

                        if (string.IsNullOrWhiteSpace(StateCode))
                        {
                            return ("Validation", "Party State Code not found. Please check the selected party.");
                        }

                        string StateType = GlobalData.STATE_CODE == StateCode ? "Local" : "Central/Other";

                        if (GlobalData.STATE_CODE == StateCode && header.IGST_AMT > 0)
                        {
                            return ("Validation", $"IGST not applicable as per Party State type: {StateType}");
                        }
                        else if (GlobalData.STATE_CODE != StateCode && (header.CGST_AMT + header.SGST_AMT) > 0)
                        {
                            return ("Validation", $"CGST/SGST not applicable as per Party State type: {StateType}");
                        }
                        else if (header.IGST_AMT > 0 && (header.CGST_AMT + header.SGST_AMT) > 0)
                        {
                            return ("Validation", "CGST+SGST+IGST all three types of tax are not applicable together.");
                        }
                    }
                }
                else
                {
                    // 9. GST Validation for Other Item Types
                    if (header.BILL_CODE > 0)
                    {
                        if ((header.CGST_AMT + header.SGST_AMT) > 0 &&
                            header.IGST_AMT > 0)
                        {
                            return ("Validation", "Both GST tax types are not applicable in one invoice (CGST+SGST & IGST).");
                        }

                        if (header.CGST_AMT != header.SGST_AMT)
                        {
                            return ("Validation",  "CGST & SGST Amount Should Be Same");
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

        public async Task<(string Status, string Message)> SubmitRequest(JobworkReceiveChallanModel_Header header, List<JobworkReceiveChallanModel_Detail> details, string action)
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

                string qyery = $@"select 1 from DOC_APPROSTAGE  where USER_CODE = { GlobalData.PubUserId } and DOC_CODE = '{header.V_TYPE}'
                and comp_code = { GlobalData.PubCompCode }";

                 string Approval = GetText(qyery);

                if(Approval == "1")
                {
                    isApprovalBody = true;
                }

                string query2 = $@"select APPROV_USER from DOC_APPROSTAGE where USER_CODE={GlobalData.PubUserId} and DOC_CODE='{header.V_TYPE}' 
                and comp_code={GlobalData.PubCompCode}";

                string APPROV_USER = GetText(query2);

                if(APPROV_USER == "FINAL")
                {
                    isFinalApprovalBody = true;
                }

                string query3 = $@"select APPROV_USER from DOC_APPROSTAGE where FLAG_B='CS' and USER_CODE={GlobalData.PubUserId} and 
                DOC_CODE='{header.V_TYPE}' and comp_code={GlobalData.PubCompCode} ";

                APPROV_USER = GetText(query3);

                if(APPROV_USER == "FINAL")
                {
                    isFinalApprovalBodyCS = true;
                }
                string query4 = $@"select APPROV_USER from DOC_APPROSTAGE where FLAG_B='LCS' and USER_CODE={GlobalData.PubUserId} 
                and DOC_CODE='{header.V_TYPE}' and comp_code={GlobalData.PubCompCode}";

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

                using (var cmd = new SqlCommand("sp_JobworkReceiveChallan", conn))
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
                    cmd.Parameters.AddWithValue("@BILL_CITYName", header.BILL_CITYName);
                    cmd.Parameters.AddWithValue("@BILL_STATE", header.BILL_STATE);
                    cmd.Parameters.AddWithValue("@BILL_STATENAME", header.BILL_STATENAME);
                    cmd.Parameters.AddWithValue("@BILL_COUNTRY", header.BILL_COUNTRY);
                    cmd.Parameters.AddWithValue("@BILL_COUNTRYNAME", header.BILL_COUNTRYNAME);
                    cmd.Parameters.AddWithValue("@BILL_GST", header.BILL_GST);
                    cmd.Parameters.AddWithValue("@BILL_PINCODE", header.BILL_PINCODE);
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
                    cmd.Parameters.AddWithValue("@AMOUNT", header.AMOUNT);
                    cmd.Parameters.AddWithValue("@PACK_PER", header.PACK_PER);
                    cmd.Parameters.AddWithValue("@PACK_AMT", header.PACK_AMT);
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
                    cmd.Parameters.AddWithValue("@BUYER_ORDNO", header.BUYER_ORDNO);
                    cmd.Parameters.AddWithValue("@TOT_NOS", header.TOT_NOS);
                    cmd.Parameters.AddWithValue("@FRT_AMT", header.FRT_AMT);
                    cmd.Parameters.AddWithValue("@ROUND_OFF", header.ROUND_OFF);
                    cmd.Parameters.AddWithValue("@NAMOUNT", header.NAMOUNT);
                    cmd.Parameters.AddWithValue("@INSU_PER", header.INSU_PER);
                    cmd.Parameters.AddWithValue("@INSU_AMT", header.INSU_AMT);
                    cmd.Parameters.AddWithValue("@TCS_PER", header.TCS_PER);
                    cmd.Parameters.AddWithValue("@TCS_AMT", header.TCS_AMT);
                    cmd.Parameters.AddWithValue("@TDS_PER", header.TDS_PER);
                    cmd.Parameters.AddWithValue("@TDS_AMT", header.TDS_AMT);
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
                    cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO);
                    cmd.Parameters.AddWithValue("@FRT_TOPAY", header.FRT_TOPAY);
                    cmd.Parameters.AddWithValue("@REMARK", header.REMARK);
                    cmd.Parameters.AddWithValue("@WB_QTY", header.WB_QTY);
                    cmd.Parameters.AddWithValue("@disc_per", header.DISC_PER);
                    cmd.Parameters.AddWithValue("@disc_amt", header.DISC_AMT);
                    cmd.Parameters.AddWithValue("@AGENT_CODE", header.AGENT_CODE);
                    cmd.Parameters.AddWithValue("@FORM_CODE", header.FORM_CODE);
                    cmd.Parameters.AddWithValue("@SAUDA_TYPE", header.SAUDA_TYPE);
                    cmd.Parameters.AddWithValue("@SAUDA_NO", header.SAUDA_NO);
                    cmd.Parameters.AddWithValue("@SAUDA_RATE", header.SAUDA_RATE);
                    cmd.Parameters.AddWithValue("@CAL_ONPCS", header.CAL_ONPCS);
                    cmd.Parameters.AddWithValue("@GATE_TYPE", header.GATE_TYPE);
                    cmd.Parameters.AddWithValue("@GATE_NO", header.GATE_NO);
                    cmd.Parameters.AddWithValue("@REF_TYPE", header.REF_TYPE);
                    cmd.Parameters.AddWithValue("@REF_NO", header.REF_NO);
                    cmd.Parameters.AddWithValue("@REF_DATE", header.REF_DATE);
                    cmd.Parameters.AddWithValue("@INSU_DETAIL", header.INSU_DETAIL);
                    cmd.Parameters.AddWithValue("@shipment_type", header.SHIPMENT_TYPE);
                    cmd.Parameters.AddWithValue("@TRAN_TYPE", header.TRAN_TYPE);
                    cmd.Parameters.AddWithValue("@SUPPLY_TYPE", header.SUPPLY_TYPE);
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

                        using var cmd = new SqlCommand("sp_JobworkReceiveChallan", conn)
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
                        cmd.Parameters.AddWithValue("@STATUS", detail.STATUS);                     
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
                    if (!string.IsNullOrWhiteSpace(fappstatus))
                    {
                        string checkQuery = @"
                            SELECT 1 FROM approval_status WHERE user_Code = @USER_CODE AND V_Type = @V_TYPE
                            AND V_No = @V_NO AND COMP_CODE = @COMP_CODE AND Branch_Code = @BRANCH_CODE AND Year_Code = @YEAR_CODE";

                        using (var checkCmd = new SqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue( "@USER_CODE", GlobalData.PubUserId);
                            checkCmd.Parameters.AddWithValue(  "@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                            checkCmd.Parameters.AddWithValue( "@V_NO", header.V_NO);
                            checkCmd.Parameters.AddWithValue(  "@COMP_CODE", GlobalData.PubCompCode);
                            checkCmd.Parameters.AddWithValue( "@BRANCH_CODE", GlobalData.PubBranchCode);
                            checkCmd.Parameters.AddWithValue( "@YEAR_CODE", GlobalData.PubFYearCode);
                            object? exists = await checkCmd.ExecuteScalarAsync();

                            if (exists != null && exists != DBNull.Value)
                            {
                                string updateQuery = @"
                                    UPDATE approval_status SET STATUS = 'CLOSE', CLOSE_DATE = GETDATE(), Approval_code = 8,
                                    Approval_remark = 'Approved', remarks = 'Document Approved'
                                    WHERE V_Type = @V_TYPE AND V_No = @V_NO AND COMP_CODE = @COMP_CODE AND Branch_Code = @BRANCH_CODE AND Year_Code = @YEAR_CODE";

                                using (var updateCmd = new SqlCommand(updateQuery, conn))
                                {
                                    updateCmd.Parameters.AddWithValue( "@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                                    updateCmd.Parameters.AddWithValue( "@V_NO", header.V_NO);
                                    updateCmd.Parameters.AddWithValue( "@COMP_CODE", GlobalData.PubCompCode);
                                    updateCmd.Parameters.AddWithValue( "@BRANCH_CODE", GlobalData.PubBranchCode);
                                    updateCmd.Parameters.AddWithValue( "@YEAR_CODE", GlobalData.PubFYearCode);
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