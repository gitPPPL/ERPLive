using AngleSharp.Text;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Layout.Element;
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
               
                if(GlobalData.PubCompCode == "1"  && header.V_TYPE == "PSF")
                {
                    chkval = true;
                }
                else if ((GlobalData.PubCompCode == "2" || GlobalData.PubCompCode == "5"  &&  (header.V_TYPE == "Fabric"  ||  header.V_TYPE == "Sacks") ))
                {
                    chkval = true;
                }
                else if (GlobalData.PubCompCode == "4" && header.V_TYPE == "Finish")
                {
                    chkval = true;
                }

                if (header.V_TYPE != "SAJI")
                {

                    decimal clbl = 0;
                    decimal CrLimit = 0;
                    decimal LastCrLimit = 0;

                    string query5 = $@"select isnull(sum(Amt),0) as Amt from ledger2 where CR_CODE= {header.BILL_CODE}  and COMP_CODE={GlobalData.PubCompCode} and
                    concat(V_type,V_no)<> '{header.V_TYPE} {header.V_NO}'";
                    clbl -= Convert.ToDecimal(GetText(query5));

                    string query6 = $@"select isnull( sum(Amt),0) as Amt  from ledger2 where DR_CODE= {header.BILL_CODE}  and COMP_CODE= {GlobalData.PubCompCode} and 
                    concat(V_type,V_no)<>'{header.V_TYPE}{header.V_NO}'";
                    clbl += Convert.ToDecimal(GetText(query6));


                    string query7 = $@"select ISNULL(b.Credit_type,'')Credit_type from SUBGROUP_MAST a left join Payterm_mast b on a.Payterm_code=b.code and a.comp_code=b.comp_code
                    where a.CODE={header.BILL_CODE} and a.comp_Code={GlobalData.PubCompCode}";

                    string Credit_type = GetText(query7);

                    if (Credit_type == "Against LC" && header.LC_NO == "")
                    {
                        return ("Validation", "LC Number Required. Otherwise invoice will not Approve."); fappstatus = ""; fappRemark = "";
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

                if (header.SUPPLY_TYPE == "EXPWOP" || header.SUPPLY_TYPE == "EXPWP")
                {
                    using (SqlConnection con = _dbConnection.GetErpConnection())
                    {
                        string sql = @"  SELECT COUNT(*) FROM Subgroup_Address  WHERE comp_code = @CompCode AND Code = @PartyCode AND ISNULL(Add1, '') <> ''";

                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("@CompCode", GlobalData.PubCompCode);
                            cmd.Parameters.AddWithValue("@PartyCode", header.BILL_CODE);

                            con.Open();

                            int addctr = Convert.ToInt32(cmd.ExecuteScalar());

                            if (addctr > 1)
                            {
                                return ( "Validation", $"{header.BILL_NAME} has multiple address, and for Export Multiple Address not allowed in same Ledger. " +
                                    "So, Please create Separate Party Ledger for each address."
                                );
                            }
                        }
                    }
                }
                else
                {
                    if (GlobalData.PubCompCode != "3" && (header.V_TYPE == "SAGT" || header.V_TYPE == "SABS"))
                    {
                        using (SqlConnection con = _dbConnection.GetErpConnection())
                        {
                            con.Open();

                            // Check party exists in CRLIMIT_MAST
                            string checkPartySql = @" SELECT 1 FROM CRLIMIT_MAST  WHERE PARTY_CODE = @PartyCode  AND COMP_CODE = @CompCode";

                            using (SqlCommand cmd = new SqlCommand(checkPartySql, con))
                            {
                                cmd.Parameters.AddWithValue("@PartyCode", header.BILL_CODE);
                                cmd.Parameters.AddWithValue("@CompCode", GlobalData.PubCompCode);

                                object partyExists = cmd.ExecuteScalar();

                                if (partyExists == null)
                                {
                                    return ( "Validation", "Party not exist in CR Limit Master. Invoice can not generated." );
                                }
                            }

                            // Previous Debit
                                string debitSql = @"  SELECT ISNULL(SUM(AMT), 0) FROM LEDGER2  WHERE V_TYPE = @VType AND V_NO <> @VNo
                                AND DR_CODE = @PartyCode AND Comp_Code = @CompCode";

                            decimal pDrAmt;

                            using (SqlCommand cmd = new SqlCommand(debitSql, con))
                            {
                                cmd.Parameters.AddWithValue("@VType", header.V_TYPE);
                                cmd.Parameters.AddWithValue("@VNo", header.V_NO);
                                cmd.Parameters.AddWithValue("@PartyCode", header.BILL_CODE);
                                cmd.Parameters.AddWithValue("@CompCode", GlobalData.PubCompCode);

                                pDrAmt = Convert.ToDecimal(cmd.ExecuteScalar());
                            }

                            // Previous Credit
                            string creditSql = @"SELECT ISNULL(SUM(AMT), 0) FROM LEDGER2  WHERE CR_CODE = @PartyCode AND Comp_Code = @CompCode";

                            decimal pCrAmt;

                            using (SqlCommand cmd = new SqlCommand(creditSql, con))
                            {
                                cmd.Parameters.AddWithValue("@PartyCode", header.BILL_CODE);
                                cmd.Parameters.AddWithValue("@CompCode", GlobalData.PubCompCode);

                                pCrAmt = Convert.ToDecimal(cmd.ExecuteScalar());
                            }

                            decimal totDrAmt = pDrAmt + Convert.ToDecimal(header.NAMOUNT ?? 0);

                            decimal balAmt = totDrAmt - pCrAmt;

                            string limitSql = @" SELECT ISNULL(CR_LIMIT, 0)  FROM CRLIMIT_MAST WHERE PARTY_CODE = @PartyCode  AND COMP_CODE = @CompCode";

                            decimal crLimitAmt;

                            using (SqlCommand cmd = new SqlCommand(limitSql, con))
                            {
                                cmd.Parameters.AddWithValue("@PartyCode", header.BILL_CODE);
                                cmd.Parameters.AddWithValue("@CompCode", GlobalData.PubCompCode);

                                crLimitAmt = Convert.ToDecimal(cmd.ExecuteScalar());
                            }
                                 
                            if (crLimitAmt <= 0)
                            {
                                return ( "Validation", "Credit Limit is <=0. Invoice can not generated." );
                            }

                            if (balAmt - crLimitAmt > 1)
                            {
                                return ( "Validation", "Total Sale Amount exceeds Credit Limit (Incl. this invoice). Invoice can not generated.");
                            }
                        }
                    }
                }

                if (GeneralSetting.pubDefSOINSI == "Yes")
                {
                    if (details.Count > 0)
                    {
                        if (details[0].ITEM_NAME == null)
                        {
                            if (header.V_TYPE == "SAGT" && details[0].ORD_NO > 0)
                            {
                                if (details[0].ORD_TYPE == "DOGT")
                                {
                                    string query = $@" SELECT 1 FROM DO1 WHERE BILL_CODE = {header.BILL_CODE}  AND V_TYPE = '{details[0].ORD_TYPE}' AND V_NO = {details[0].ORD_NO}
                                        AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}";

                                    if (!IsExist(query))
                                    {
                                        return (  "Validation", "Party in Sale Invoice not mathced with Party in Delivery Order. Please check it." );
                                    }
                                }

                                else
                                {
                                    string query = $@" SELECT 1 FROM ORDER1 WHERE PARTY_CODE = {header.BILL_CODE} AND V_TYPE = '{details[0].ORD_TYPE}'
                                        AND V_NO = {details[0].ORD_NO} AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}";

                                    if (!IsExist(query))
                                    {
                                        return ( "Validation", "Party in Sale Invoice not mathced with Party in Sale Order. Please check it." );
                                    }
                                }
                                string shipQuery = $@" SELECT 1 FROM ORDER1 WHERE SHIP_CODE = {header.SHIP_CODE} AND V_TYPE = '{details[0].ORD_TYPE}'
                                    AND V_NO = {details[0].ORD_NO} AND COMP_CODE = {GlobalData.PubCompCode}  AND BRANCH_CODE = {GlobalData.PubBranchCode}";

                                if (!IsExist(shipQuery))
                                {
                                    return ( "Validation", "Shipping Party in Sale Invoice not mathced with Shipping Party in Sale Order. Please check it." );
                                }
                            }
                        }
                    }
                }

                if (GeneralSetting.pubDefPACKINSI == "Yes")
                {
                    if (GlobalData.PubCompCode != "3" &&  GlobalData.PubCompCode != "2" &&  GlobalData.PubCompCode != "5")
                    {
                        if (header.PACK_NO > 0)
                        {
                            int pubRes1Int = Convert.ToInt32(GetText($@" SELECT V_NO FROM SALE1 WHERE PACK_TYPE = '{header.PACK_TYPE}' AND PACK_NO = {header.PACK_NO}
                            AND ISNULL(Status, 0) <> 2 AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}  AND YEAR_CODE = {GlobalData.PubFYearCode}
                            AND DOC_ID <> '{header.DOC_ID}'"));

                            if (pubRes1Int > 0)
                            {
                                return ( "Validation",  $"Packing Slip No. already Exist in Sale, Serial No. {pubRes1Int}");
                            }
                        }
                    }
                }

                if (IsExist($@"  SELECT 1  FROM GATE2  WHERE V_TYPE = 'OUSL'  AND REF_TYPE = '{header.V_TYPE}' AND REF_NO = {header.V_NO}  AND COMP_CODE = {GlobalData.PubCompCode}
                AND BRANCH_CODE = {GlobalData.PubBranchCode}"))
                {  
                    return (  "Validation",  "Gate Pass created, modification not allowed." );
                }

                string discPerMaster = GetText($@" SELECT ISNULL(DISC_PER, 0)  FROM SUBGROUP_MAST WHERE CODE = {header.BILL_CODE} AND COMP_CODE = {GlobalData.PubCompCode}");

                decimal masterDisc = decimal.TryParse(discPerMaster, out var masterValue) ? masterValue  : 0;
                           
                if (header.DISC_PER != masterDisc)
                {
                    return ("Validation", $"Discount in master=>{masterDisc} % not matched with Discount in invoice=>{header.DISC_PER}%, Please check it.");
                }

                if (details.Count > 0)
                {
                    for (int i = 0; i < details.Count; i++)
                    {
                        if (details[i].ITEM_CODE != null)
                        {
                            if (Convert.ToDecimal(details[i].DCN_NO) > 0)
                            {
                                string wbNoText = GetText($@" SELECT ISNULL(WB_NO, 0)  FROM DC_NOTE2  WHERE ITEM_CODE = {details[i].ITEM_CODE}
                                AND V_TYPE = '{details[i].DCN_TYPE}' AND V_NO = {details[i].DCN_NO}  AND COMP_CODE = {GlobalData.PubCompCode}
                                AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                decimal wbNo = 0;
                                decimal.TryParse(wbNoText, out wbNo);

                                decimal headerWbNo = header.WB_NO ?? 0;

                                if (headerWbNo == 0 && wbNo == 0)
                                {
                                    WBReqCN = false;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (header.SUPPLY_TYPE == "EXPWOP" || header.SUPPLY_TYPE == "EXPWP" || header.SUPPLY_TYPE == "SEZWOP")
                {
                }
                else
                {
                    if (GeneralSetting.pubDefWBINSI == "Yes")
                    {
                        if (header.PORT_CODE != "Other" &&  Convert.ToDecimal(header.TOT_GROSS) > 500 &&  header.V_TYPE == "SAGT")
                        {
                            if (WBReqCN == true)
                            {
                                // Weighbridge No. validation
                                if ((header.WB_NO == 0))
                                {
                                    return ("Validation", "Weighbridge No. is required.");
                                }

                                // Truck No. validation
                                if (header.VEHICLE_NO == "")
                                {
                                    return ("Validation", "Truck No. is required.");
                                }

                                // Get Party Code
                                int pubRes1Int = Convert.ToInt32(GetText($@"  SELECT PARTY_CODE FROM WB1  WHERE V_TYPE = '{header.WB_TYPE}'
                                AND V_NO = {header.WB_NO} AND COMP_CODE = {GlobalData.PubCompCode}  AND BRANCH_CODE = {GlobalData.PubBranchCode}"));

                                // Get Weighbridge Truck No.
                                string pubRes1Str = GetText($@" SELECT VEHICLE_NO FROM WB1 WHERE V_TYPE = '{header.WB_TYPE}' AND V_NO = {header.WB_NO}  AND 
                                 COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                // Get Weighbridge Net Weight
                                decimal pubRes1Dbl = Convert.ToDecimal(
                                    GetText($@" SELECT SUM(NET_WGT) FROM WB2  WHERE V_TYPE = '{header.WB_TYPE}'  AND V_NO = {header.WB_NO} AND COMP_CODE = {GlobalData.PubCompCode}
                                    AND BRANCH_CODE = {GlobalData.PubBranchCode}")
                                );

                                // Truck No. validation
                                if (header.VEHICLE_NO?.Trim() != pubRes1Str.Trim())
                                {
                                    return ( "Validation",  $"Invoice Truck No. {header.VEHICLE_NO} not match with Weighbridge Truck No., Please Check Truck No. {pubRes1Str}" );
                                }

                                // Waste quantity validation
                                if (header.PORT_CODE == "Waste" &&
                                    Convert.ToDecimal(header.TOT_NET) != pubRes1Dbl)
                                {
                                    return ( "Validation", $"Invoice Quantity {Convert.ToDecimal(header.TOT_NET)} not match with Weighbridge Quantity, Please Check Quantity {pubRes1Dbl}" );
                                }
                            }
                        }
                    }
                }

                if(chkval == true)
                {
                    if(header.V_TYPE == "SAGT" && header.PACK_NO == null)
                    {
                        return ("Validation", $"Packing No. can not be Blank, Please Check it." );
                    }

                    if(header.V_TYPE == "SAGT" || header.V_TYPE == "SABS")
                    {
                        if(header.V_TYPE == "Flakes")
                        {
                            packtyp = "'SFIS','SFEI'";
                        }
                        else
                        {
                            packtyp = "'FPIS'";
                        }

                    }
                    else if (header.V_TYPE == "SACH")
                    {
                        packtyp = Convert.ToString(header.PACK_NO);             
                    }


                    string packdate = GetText($@"
                    SELECT FORMAT(V_DATE, 'dd/MM/yyyy')
                    FROM PRODUCTION1
                    WHERE V_NO = {header.PACK_NO}
                    AND V_TYPE IN ({packtyp})
                    AND COMP_CODE = {GlobalData.PubCompCode}
                    AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                    if (!string.IsNullOrWhiteSpace(packdate))
                    {
                        if (header.V_DATE.HasValue &&
                            header.V_DATE.Value.ToString("dd/MM/yyyy") != packdate)
                        {
                            return ("Validation", $"Packing Date " + packdate + " not matched with Invoice date, Please Check it.");

                        }
                    }


                }

                if(GeneralSetting.pubDefISSUEINSI == "Yes")
                {
                    if(header.ITEM_TYPE == "Store" || header.ITEM_TYPE == "Other" || header.ITEM_TYPE == "Scrap" || header.ITEM_TYPE == "SACH"  )
                    {
                        checkIssueNo = false;
                    }
                    else
                    {
                        if(GlobalData.PubCompCode == "1" ||GlobalData.PubCompCode == "3" ||GlobalData.PubCompCode == "7" ||GlobalData.PubCompCode == "8")
                        {
                            issuevtype = "RAID";
                        }
                        else
                        {
                            issuevtype = "RAIS";
                        }
                    }
                }

                if(header.V_TYPE == "SAJI" && GlobalData.PubCompCode == "3")
                {
                    if(GeneralSetting.pubDefPACKINSI == "Yes")
                    {
                        if(header.PACK_NO == 0)
                        {
                            if(checkIssueNo == true && header.ISSUE_TYPE == "")
                            {
                                return (
                                       "Validation",
                                       $"either Packing no. or Issue no. required."
                                   );
                            }
                        }
                    }
                }

                if (GeneralSetting.pubDefPACKINSI == "Yes")
                {
                    if (GlobalData.PubCompCode != "3")
                    {
                        if (chkval == true && !string.IsNullOrEmpty(header.PACK_TYPE))
                        {
                            string pslip = "";
                            int ps = 0;

                            for (int p = 0; p < details.Count; p++)
                            {
                                if (details[p].ITEM_CODE != null &&
                                    ps != details[p].PACK_NO)
                                {
                                    ps = Convert.ToInt32(details[p].PACK_NO);
                                    pslip += ps + ",";
                                }
                            }

                            pslip = pslip.TrimEnd(',');

                            if (!string.IsNullOrWhiteSpace(pslip))
                            {
                                string query = $@"
                                    SELECT ITEM_CODE, TENACITY_CODE FROM PRODUCTION2  WHERE V_TYPE = 'FPIS' AND V_NO IN ({pslip}) AND COMP_CODE = {GlobalData.PubCompCode}
                                    AND BRANCH_CODE = {GlobalData.PubBranchCode} AND YEAR_CODE = {GlobalData.PubFYearCode} GROUP BY ITEM_CODE, TENACITY_CODE";

                                using var cmd = new SqlCommand(query, conn);

                                using var reader = cmd.ExecuteReader();

                                while (reader.Read())
                                {
                                    int tenacityCode = Convert.ToInt32(reader["TENACITY_CODE"]);

                                    string TType1 = GetText($@"
                                        SELECT ISNULL(TENACITY_TYPE, '')  FROM TENACITY_MAST WHERE CODE = {tenacityCode} AND COMP_CODE = {GlobalData.PubCompCode}");

                                        string TType2 = GetText($@"  SELECT ISNULL(TENACITY_TYPE, '') FROM SUBGROUP_MAST  WHERE CODE = {header.BILL_CODE}  AND COMP_CODE = {GlobalData.PubCompCode}");

                                    if (!string.IsNullOrWhiteSpace(TType1) &&
                                        !string.IsNullOrWhiteSpace(TType2) &&
                                        !TType1.Trim().Equals(
                                            TType2.Trim(),
                                            StringComparison.OrdinalIgnoreCase))
                                    {
                                        return ( "Validation", $"Please check Tenacity Type in packing Slip is '{TType1}' and Party Master allow is '{TType2}'."  );
                                    }
                                }
                            }
                        }
                    }
                }

                if (GeneralSetting.pubDefPACKINSI == "Yes")
                {
                    if (GlobalData.PubCompCode == "1" ||
                        GlobalData.PubCompCode == "7")
                    {
                        if (chkval == true && !string.IsNullOrEmpty(header.PACK_TYPE))
                        {
                            string pslip = "";
                            int ps = 0;

                            for (int p = 0; p < details.Count; p++)
                            {
                                if (details[p].ITEM_CODE != null &&
                                    ps != details[p].PACK_NO)
                                {
                                    ps = Convert.ToInt32(details[p].PACK_NO);
                                    pslip += ps + ",";
                                }
                            }

                            pslip = pslip.TrimEnd(',');

                            if (!string.IsNullOrWhiteSpace(pslip))
                            {
                                string query = $@"
                                    SELECT DISTINCT CAL_ON FROM PRODUCTION1  WHERE V_TYPE = '{header.PACK_TYPE}' AND V_NO IN ({pslip}) AND COMP_CODE = {GlobalData.PubCompCode}
                                    AND BRANCH_CODE = {GlobalData.PubBranchCode}";

                                using var cmd = new SqlCommand(query, conn);                         

                                using var reader = cmd.ExecuteReader();

                                int count = 0;

                                while (reader.Read())
                                {
                                    count++;

                                    if (count > 1)
                                        break;
                                }

                                if (count > 1)
                                {
                                    return ( "Validation",  "Please check Packing Slip, all related packing slip must have same Weighment Type either 'Gross' or 'Net'." );
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(header.TRANSPORT_NAME) && !string.IsNullOrWhiteSpace(header.GR_NO))
                {
                    string currentDocId = $"{header.V_TYPE}{header.V_NO}";

                    // Check Purchase
                    string purdocid = GetText($@"
                        SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM PURCHASE1  WHERE CONCAT(V_TYPE, V_NO) <> '{currentDocId}'  AND TRANSPORT_NAME = '{header.TRANSPORT_NAME.Trim()}'
                        AND GR_NO = '{header.GR_NO.Trim()}'  AND CONCAT(V_TYPE, V_NO) <> '{header.V_TYPE}{header.V_NO}'   AND V_TYPE NOT IN  (SELECT CODE FROM DOCTYPE_MAST
                        WHERE DOCTYPE = 'MaterialReceipt') AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}  AND YEAR_CODE = {GlobalData.PubFYearCode}");

                    if (!string.IsNullOrWhiteSpace(purdocid))
                    {
                        return ( "Warning",
                        $"Transport Name '{header.TRANSPORT_NAME.Trim()}' with GRNo='{header.GR_NO.Trim()}' already exist in Purchase Bill/Direct Exps/JW/Imported Exps/Return No:{purdocid}" );
                    }

                    // Check Sale
                    string saledocid = GetText($@"
                        SELECT TOP 1 CONCAT(V_TYPE, V_NO)  FROM SALE1 WHERE ISNULL(Status, 0) <> 2  AND CONCAT(V_TYPE, V_NO) <> '{currentDocId}'  AND TRANSPORT_NAME = '{header.TRANSPORT_NAME.Trim()}'
                        AND GR_NO = '{header.GR_NO.Trim()}' AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}  AND YEAR_CODE = {GlobalData.PubFYearCode}");

                    if (!string.IsNullOrWhiteSpace(saledocid))
                    {
                        return ( "Warning", $"Transport Name '{header.TRANSPORT_NAME.Trim()}' with GRNo='{header.GR_NO.Trim()}' already exist in Sale/JW Issue/Sale Return invoice No:{saledocid}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(header.TRANSPORT_NAME) && header.TRANSPORT_NAME.Trim().ToUpper() != "SELF")
                {
                    string tptCode = GetText($@"
                    SELECT PARTY_CODE  FROM TRANSPORT_MAST  WHERE CODE = {header.TRANSPORT_CODE} AND COMP_CODE = {GlobalData.PubCompCode}   AND ACTIVE = 1");

                    if (string.IsNullOrWhiteSpace(tptCode) || tptCode == "0")
                    {
                        return (  "Validation",  $"Party Name not Linked with Transport => {header.TRANSPORT_NAME}, Please update first." );
                    }
                    else if (!IsExist($@" SELECT 1 FROM SUBGROUP_MAST WHERE CODE = {tptCode}  AND COMP_CODE = {GlobalData.PubCompCode} AND ACTIVE = 1"))
                    {
                        return ( "Validation", $"Party not linked in Tranport Master OR not Active/Exist in BP Master which is Linked with Transport=>{header.TRANSPORT_NAME}" );
                    }
                }

                if (GlobalData.PubCompCode != "8" && header.VEHICLE_NO.Length >= 4 && Convert.ToInt32(header.VEHICLE_NO.Substring(header.VEHICLE_NO.Length - 4)) >= 1)
                {
                    // Transport Quotation Approval validation
                    string query = $@" SELECT   T1.BILL_CODE,  T2.TRANSPORT_CODE,   T2.TRUCK_NO, T2.OUR_RATE  FROM TRANSPORT_QT1 T1
                        INNER JOIN TRANSPORT_QT2 T2  ON T1.COMP_CODE = T2.COMP_CODE AND T1.YEAR_CODE = T2.YEAR_CODE  AND T1.BRANCH_CODE = T2.BRANCH_CODE
                        AND T1.V_TYPE = T2.V_TYPE AND T1.V_NO = T2.V_NO  WHERE T2.TRUCK_NO = '{header.VEHICLE_NO}'  AND T2.TRANSPORT_CODE = {header.TRANSPORT_CODE}
                        AND T2.V_DATE BETWEEN '{header.V_DATE.Value.AddDays(-2):yyyy-MM-dd}'  AND '{header.V_DATE:yyyy-MM-dd}'  AND T2.COMP_CODE = {GlobalData.PubCompCode}
                        AND T2.YEAR_CODE = {GlobalData.PubFYearCode}";
                                 

                    using (var cmd = new SqlCommand(query, conn))
            
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            decimal ourRate = Convert.ToDecimal(reader["OUR_RATE"] ?? 0);
                            decimal freight = Convert.ToDecimal(header.FRT_AMT ?? 0);

                            if (freight != ourRate)
                            {
                                // Warning only - same as original VB
                                // Message:
                                // The freight amount you have entered does not match...
                            }
                        }
                        else
                        {
                            return ("Validation",  "The transport name or truck number does not match in the Transport Quotation Approval entry." +
                                "  Alternatively, it appears that the quotation for this truck has not been approved. Kindly check and confirm."  );
                        }
                    }

                    // Vehicle Gate Inward validation
                    string gateQuery = $@" SELECT 1 FROM GATE1  WHERE V_TYPE = 'TRGI' AND TRUCK_NO = '{header.VEHICLE_NO}' AND V_DATE BETWEEN '{header.V_DATE.Value.AddDays(-2):yyyy-MM-dd}'
                        AND '{header.V_DATE.Value:yyyy-MM-dd}' AND COMP_CODE = {GlobalData.PubCompCode}";

                    string Dataexit = GetText(gateQuery);

                    if(Dataexit != "")
                    {
                        if (GlobalData.PubUserLevel != "1")
                        {
                            return ("Validation", "The truck number does not match in the Vehicle Gate Inward entry. Kindly check and confirm.");
                        }
                    }
                                    
                }

                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;

                if (header.V_TYPE == "SAGT" &&  header.V_DATE.HasValue && header.V_DATE.Value >= new DateTime(2020, 10, 1))
                {
                    string panNo = GetText($@" SELECT LTRIM(RTRIM(ISNULL(PAN, ''))) FROM SUBGROUP_MAST WHERE COMP_CODE = {GlobalData.PubCompCode}  AND CODE = {header.BILL_CODE}");

                    if (!string.IsNullOrWhiteSpace(panNo))
                    {
                        string tcsApply = GetText($@" SELECT ISNULL(TCS_APPLY, '')  FROM SUBGROUP_MAST  WHERE COMP_CODE = {GlobalData.PubCompCode} AND CODE = {header.BILL_CODE}");

                        decimal tcsPer = Convert.ToDecimal(header.TCS_PER ?? 0);

                        if (tcsApply == "Yes")
                        {
                            if (tcsPer == 0)
                            {                         
                                return ("Confirmation",  $"Please Check, TCS applicable @ {GeneralSetting.pubBPTCSPer}% for {header.BILL_NAME}. Do you want to Continue ?");
                            }
                        }
                        else
                        {
                            if (tcsPer > 0)
                            {                             
                                return ("Confirmation", $"Please Check, TCS not applicable for Party => {header.BILL_NAME}. Do you want to Continue ?");
                            }
                        }
                    }
                    else
                    {                      
                        decimal tcsPer = Convert.ToDecimal(header.TCS_PER ?? 0);
                        if (tcsPer < 2)
                        {
                            return ("Confirmation",  $"Please Check, PAN No. not found in Party Master of {header.BILL_NAME}.\n" +  $"So, TCS applicable @ 2%. Do you want to Continue ?");
                        }
                    }
                }

                if(header.BILL_CODE > 0)

                {
                    string  StateCode = GetText(@$"select State_Code from CITY_MAST where code={header.BILL_CITY}");

                    string StateType = "";

                    if(GlobalData.STATE_CODE == StateCode)
                    {
                        StateType = "Local";
                    }
                    else
                    {
                        StateType = "Central/Other";
                    }

                    if (GlobalData.STATE_CODE == StateCode && header.IGST_AMT > 0)
                    {
                        return ( "Validation",  $"IGST Not applicable as per Party State type is {StateType}" );
                    }
                    else if (GlobalData.STATE_CODE != StateCode && (header.CGST_AMT + header.SGST_AMT) > 0)
                    {
                        return ("Validation", $"Both GST Tax Rate is not Applicable in One Sales Invoice (CGST+SGST & IGST)");
                    }

                    if(header.CGST_AMT != header.SGST_AMT)
                    {
                        return ("Validation", $"CGST & SGST Amount Should Be Same.");
                    }

                }


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
                        cmd.Parameters.AddWithValue("@LICENCE_NO", header.LICENCE_NO);
                        cmd.Parameters.AddWithValue("@LICENCE_TYPE", header.LICENCE_TYPE);
                        cmd.Parameters.AddWithValue("@LICENCE_DATE", header.LICENCE_DATE);
                        cmd.Parameters.AddWithValue("@SHIPMENT_TYPE", header.SHIPMENT_TYPE);
                        cmd.Parameters.AddWithValue("@TRAN_TYPE", header.TRAN_TYPE);
                        cmd.Parameters.AddWithValue("@BANK_CODE", header.BANK_CODE);                   
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

                        string hsncode = GetText($@"select isnull(HSN_CODE,'') from ITEM_MAST 
                        where code= {detail.ITEM_CODE} and Comp_code={GlobalData.PubCompCode} ");


                        if(hsncode.Length < 6)
                        {
                            return ("Validation",  $"HSN Code must be 6 digit in Item Master of Item = {detail.ITEM_NAME} " );
                        }

                        if (hsncode.Length < 4)
                        {
                        if (hsncode.Length < 4)
                            return ("Validation", $"Invalid HSN Code in Item mast of Item = {detail.ITEM_NAME} ");
                        }

                        if (GeneralSetting.pubDefSOINSI == "Yes")
                        {
                            if (header.V_TYPE == "SAGT" && detail.ORD_NO > 0)
                            {
                                if (detail.ORD_TYPE == "DOGT")
                                {
                                    bool itemExists = IsExist($@"
                                        SELECT 1 FROM DO2  WHERE ITEM_CODE = {detail.ITEM_CODE}  AND V_TYPE = '{detail.ORD_TYPE}'
                                        AND V_NO = {detail.ORD_NO} AND COMP_CODE = {GlobalData.PubCompCode}  AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                    if (!itemExists)
                                    {
                                        return ("Validation", $"Item Code : {detail.ITEM_CODE} not exist in Sale Order. Please check it.");
                                    }
                                }
                                else
                                {
                                    bool itemExists = IsExist($@"
                                        SELECT 1  FROM ORDER2  WHERE ITEM_CODE = {detail.ITEM_CODE}  AND V_TYPE = '{detail.ORD_TYPE}' AND V_NO = {detail.ORD_NO}
                                        AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                    if (!itemExists)
                                    {
                                        return ("Validation", $"Item Code : {detail.ITEM_CODE} not exist in Sale Order. Please check it.");
                                    }
                                }
                            }
                        }

                        decimal ordRate = 0;

                        if(header.EXRATE > 0)
                        {
                            ordRate = Convert.ToDecimal(detail.FOR_RATE);
                        }
                        else
                        {
                            ordRate = Convert.ToDecimal(detail.RATE);
                        }

                        if (detail.ORD_TYPE == "DOGT")
                        {
                            bool rateExists = IsExist($@"
                                SELECT 1
                                FROM DO2
                                WHERE ITEM_CODE = {detail.ITEM_CODE}
                                AND RATE = {ordRate}
                                AND V_TYPE = '{detail.ORD_TYPE}'
                                AND V_NO = {detail.ORD_NO}
                                AND COMP_CODE = {GlobalData.PubCompCode}
                                AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                            if (!rateExists)
                            {
                                return ("Validation", $"RATE of Item: {detail.ITEM_NAME} not matched with Sale Order Item. Please check it.");
                            }
                        }
                        else
                        {
                            bool rateExists = IsExist($@"
                                SELECT 1  FROM ORDER2 WHERE ITEM_CODE = {detail.ITEM_CODE}  AND RATE = {ordRate} AND V_TYPE = '{detail.ORD_TYPE}'
                                AND V_NO = {detail.ORD_NO} AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                            if (!rateExists)
                            {
                                return ("Validation", $"RATE of Item: {detail.ITEM_NAME} not matched with Sale Order Item. Please check it.");
                            }
                        }

                        if(header.V_TYPE != "SASI" && header.V_TYPE != "SAST")
                        {
                            string mgroup = GetText($@"Select SALE_GROUP from ITEM_MAST a LEFT JOIN item_group b ON a.GROUP_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE 
                            where a.CODE={detail.ITEM_CODE} and a.COMP_CODE={GlobalData.PubCompCode}");


                            if(mgroup != header.V_TYPE)
                            {
                                return ("Validation", $"Item Name={detail.ITEM_NAME} (Group:{mgroup}), but you selected Product Type : {header.ITEM_TYPE} in header.");

                            }                       

                        }

                        if(header.CAL_ONPCS == 0 && detail.QTY == 0)
                        {
                            return ("Validation", $"Quantity Should not be Blank of Item Name={detail.ITEM_NAME}");

                        }


                        if ((header.ITEM_TYPE == "Scrap" || header.ITEM_TYPE == "Store") && Convert.ToInt32(detail.ITEM_CODE) != 0)
                        {
                            decimal curStock = 0;

                            // MaterialReceipt + JobReceived
                            string stock1 = GetText($@"
                                SELECT ISNULL(SUM(purchase2.recd_qty), 0)
                                FROM purchase2
                                LEFT JOIN doctype_mast
                                ON doctype_mast.code = purchase2.v_type
                                WHERE purchase2.comp_code = {GlobalData.PubCompCode}
                                AND purchase2.branch_code = {GlobalData.PubBranchCode}
                                AND purchase2.v_date <= '{header.V_DATE:yyyyMMdd}'
                                AND item_code = {detail.ITEM_CODE}
                                AND doctype_mast.doctype IN ('MaterialReceipt','JobReceived')");

                            curStock += decimal.TryParse(stock1, out var s1) ? s1 : 0;


                            // PurchaseReturn + JobIssue
                            string stock2 = GetText($@"
                                SELECT ISNULL(-SUM(purchase2.recd_qty), 0)
                                FROM purchase2
                                LEFT JOIN doctype_mast
                                ON doctype_mast.code = purchase2.v_type
                                WHERE purchase2.comp_code = {GlobalData.PubCompCode}
                                AND purchase2.branch_code = {GlobalData.PubBranchCode}
                                AND purchase2.v_date <= '{header.V_DATE:yyyyMMdd}'
                                AND item_code = {detail.ITEM_CODE}
                                AND doctype_mast.doctype IN ('PurchaseReturn','JobIssue')");

                            curStock += decimal.TryParse(stock2, out var s2) ? s2 : 0;


                            // PlantReturn + OpeningStock + ProductionReceived + AdjustmentReceived
                            string stock3 = GetText($@"
                                SELECT ISNULL(SUM(issue2.qty), 0)
                                FROM issue2
                                LEFT JOIN doctype_mast
                                ON doctype_mast.code = issue2.v_type
                                WHERE issue2.comp_code = {GlobalData.PubCompCode}
                                AND issue2.branch_code = {GlobalData.PubBranchCode}
                                AND issue2.v_date <= '{header.V_DATE:yyyyMMdd}'
                                AND item_code = {detail.ITEM_CODE}
                                AND doctype_mast.doctype IN
                                ('PlantReturn','OpeningStock','ProductionReceived','AdjustmentReceived')");

                            curStock += decimal.TryParse(stock3, out var s3) ? s3 : 0;


                            // GoodsIssue + ProductionIssue + AdjustmentIssue + DispatchIssue + MoistureIssue
                            // Exclude current voucher
                            string stock4 = GetText($@"
                                SELECT ISNULL(-SUM(Issue2.qty), 0)
                                FROM Issue2
                                LEFT JOIN doctype_mast
                                ON doctype_mast.code = Issue2.v_type
                                WHERE Issue2.comp_code = {GlobalData.PubCompCode}
                                AND Issue2.branch_code = {GlobalData.PubBranchCode}
                                AND Issue2.v_date <= '{header.V_DATE:yyyyMMdd}'
                                AND item_code = {detail.ITEM_CODE}
                                AND doctype_mast.doctype IN
                                ('GoodsIssue','ProductionIssue','AdjustmentIssue','DispatchIssue','MoistureIssue')
                                AND Issue2.v_no NOT IN ({header.V_NO})");

                            curStock += decimal.TryParse(stock4, out var s4) ? s4 : 0;


                            // SalesInvoice + JobIssue
                            // Exclude current voucher
                            string stock5 = GetText($@"
                                SELECT ISNULL(-SUM(sale2.qty), 0)
                                FROM sale2
                                LEFT JOIN doctype_mast
                                ON doctype_mast.code = sale2.v_type
                                WHERE sale2.comp_code = {GlobalData.PubCompCode}
                                AND sale2.branch_code = {GlobalData.PubBranchCode}
                                AND sale2.v_date <= '{header.V_DATE:yyyyMMdd}'
                                AND item_code = {detail.ITEM_CODE}
                                AND doctype_mast.doctype IN ('SalesInvoice','JobIssue')
                                AND status <> 2
                                AND sale2.v_no NOT IN ({header.V_NO})");

                            curStock += decimal.TryParse(stock5, out var s5) ? s5 : 0;


                            // SalesReturn
                            string stock6 = GetText($@"
                                    SELECT ISNULL(SUM(sale2.qty), 0)
                                    FROM sale2
                                    LEFT JOIN doctype_mast
                                    ON doctype_mast.code = sale2.v_type
                                    WHERE sale2.comp_code = {GlobalData.PubCompCode}
                                    AND sale2.branch_code = {GlobalData.PubBranchCode}
                                    AND sale2.v_date <= '{header.V_DATE:yyyyMMdd}'
                                    AND item_code = {detail.ITEM_CODE}
                                    AND doctype_mast.doctype IN ('SalesReturn')");

                            curStock += decimal.TryParse(stock6, out var s6) ? s6 : 0;


                            // Round stock to 2 decimals
                            curStock = Math.Round(curStock, 2);

                            decimal issueQty = Convert.ToDecimal(detail.QTY ?? 0);

                            if ((curStock - issueQty) < 0)
                            {
                                return ("Validation",
                                    $"Current Stock = ({curStock}) is less than issue qty. Please check for item name {detail.ITEM_NAME} ({detail.ITEM_CODE})");
                            }
                        }

                        string csgText = GetText($@"
                        SELECT ISNULL(CGST_PER, 0)
                        FROM ITEM_MAST
                        WHERE Code = {detail.ITEM_CODE}
                        AND COMP_CODE = {GlobalData.PubCompCode}");

                        string igText = GetText($@"
                        SELECT ISNULL(IGST_PER, 0)
                        FROM ITEM_MAST
                        WHERE Code = {detail.ITEM_CODE}
                        AND COMP_CODE = {GlobalData.PubCompCode}");

                        decimal csg = decimal.TryParse(csgText, out var csgValue) ? csgValue : 0;
                        decimal ig = decimal.TryParse(igText, out var igValue) ? igValue : 0;

                        if(detail.CGST_PER > 0)
                        {
                            if(detail.CGST_PER != csg)
                            {
                                return ("Validation", $"Tax Percentage of Item =>{detail.ITEM_NAME} not matched as per Item Master.");

                            }
                        }
                        else if(detail.IGST_PER > 0)
                        {
                            if (detail.IGST_PER != ig)
                            {
                                return ("Validation", $"Tax Percentage of Item =>{detail.ITEM_NAME} not matched as per Item Master.");

                            }
                        }

                        if(header.V_TYPE == "SAGT")
                        {
                            if(detail.ITEM_CODE > 0 )
                            {
                                if(detail.HSN_CODE == "")
                                {
                                    return ("Validation", $"Tax Percentage of Item =>{detail.ITEM_NAME} not matched as per Item Master.");

                                }
                            }

                        }
           
                        string getWBYN = GetText($@"Select isnull(WB_YN,'') from GODOWN_MAST where comp_code={GlobalData.PubCompCode} and CODE={header.GODOWN_CODE}");

                        if (getWBYN == "Yes")
                        {
                            if(GlobalData.PubCompCode == "1")
                            {

                                if (detail.ITEM_CODE != 0)
                                {
                                    if(detail.QTY != detail.WBQTY)
                                    {
                                        return ("Validation", $"Net Qty not Matched with WB Qty, Please Check it of =>{detail.ITEM_NAME}, Approval Required");
                                    }
                                }
                            }


                        }

                        if(GeneralSetting.pubDefSOINSI == "Yes")
                        {
                            if(detail.ITEM_CODE > 0 &&  detail.PACK_NO > 0)
                            {
                                if(detail.ORD_NO == 0)
                                {
                                    return ("Validation", $"Order No. can not be Blank, Please Check it of =>{detail.ITEM_NAME}");

                                }
                            }



                            if (detail.ITEM_CODE > 0 && detail.ORD_NO > 0)
                            {
                                if (detail.ORD_TYPE == "DOGT")
                                {
                                    // Delivery Order Approval
                                    bool doApproved = IsExist($@"
                                        SELECT 1
                                        FROM DO1
                                        WHERE V_TYPE = '{detail.ORD_TYPE}'
                                        AND V_NO = {detail.ORD_NO}
                                        AND FAPROV_STATUS = 'Approved'
                                        AND COMP_CODE = {GlobalData.PubCompCode}
                                        AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                    if (!doApproved)
                                    {
                                        return ("Validation",
                                            $"Delivery Order No {detail.ORD_NO} not approved, Please Check it of = {detail.ITEM_NAME}");
                                    }

                                    // Delivery Order Billing Party Check
                                    bool doPartyMatched = IsExist($@"
                                        SELECT 1
                                        FROM DO1
                                        WHERE V_TYPE = '{detail.ORD_TYPE}'
                                        AND V_NO = {detail.ORD_NO}
                                        AND BILL_CODE = {header.BILL_CODE}
                                        AND COMP_CODE = {GlobalData.PubCompCode}
                                        AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                    if (!doPartyMatched)
                                    {
                                        return ("Validation",
                                            $"Party in Delivery Order No. {detail.ORD_NO} not matched with Billing Party, Please Check it of = {detail.ITEM_NAME}");
                                    }
                                }
                                else
                                {
                                    // Sale Order Approval
                                    bool orderApproved = IsExist($@"
                                    SELECT 1
                                    FROM ORDER1
                                    WHERE V_TYPE = 'SORD'
                                    AND V_NO = {detail.ORD_NO}
                                    AND FAPROV_STATUS = 'Approved'
                                    AND COMP_CODE = {GlobalData.PubCompCode}
                                    AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                    if (!orderApproved)
                                    {
                                        return ("Validation",
                                            $"Order No {detail.ORD_NO} not approved, Please Check it of = {detail.ITEM_NAME}");
                                    }

                                    // Sale Order Billing Party Check
                                    bool orderPartyMatched = IsExist($@"
                                        SELECT 1
                                        FROM ORDER1
                                        WHERE V_TYPE = '{detail.ORD_TYPE}'
                                        AND V_NO = {detail.ORD_NO}
                                        AND PARTY_CODE = {header.BILL_CODE}
                                        AND COMP_CODE = {GlobalData.PubCompCode}
                                        AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                    if (!orderPartyMatched)
                                    {
                                        return ("Validation",
                                            $"Party in Order No. {detail.ORD_NO} not matched with Billing Party, Please Check it of = {detail.ITEM_NAME}");
                                    }
                                }
                            }


                        }

                        if (GeneralSetting.pubDefSSINSI == "Yes")
                        {
                            if (detail.ITEM_CODE > 0 && detail.SAUDA_NO > 0)
                            {
                                // Sauda approval check
                                bool saudaApproved = IsExist($@"
                                    SELECT 1
                                    FROM SAUDA
                                    WHERE V_TYPE = 'SAUD'
                                    AND V_NO = {detail.SAUDA_NO}
                                    AND FAPROV_STATUS = 'Approved'
                                    AND COMP_CODE = {GlobalData.PubCompCode}
                                    AND BRANCH_CODE = {GlobalData.PubBranchCode}");

                                if (!saudaApproved)
                                {
                                    return ("Validation",
                                        $"Sauda No {detail.SAUDA_NO} not approved, Please Check it of = {detail.ITEM_NAME}");
                                }

                                // Freight validation
                                if (header.FRT_AMT > 0)
                                {
                                    bool freightAllowed = IsExist($@"
                                        SELECT 1
                                        FROM SAUDA
                                        WHERE V_TYPE = 'SAUD'
                                        AND V_NO = {detail.SAUDA_NO}
                                        AND COMP_CODE = {GlobalData.PubCompCode}
                                        AND BRANCH_CODE = {GlobalData.PubBranchCode}
                                        AND FRT_TERM IN ('FOR', 'FOR-TPT')");

                                    if (!freightAllowed)
                                    {
                                        return ("Validation",
                                            $"Freight payable by customer in Sauda No {detail.SAUDA_NO}, so freight can not be charged.");
                                    }
                                }
                            }
                        }

                        if(detail.ITEM_CODE != 0)
                        {
                            if(chkval == true && header.V_TYPE == "SAGT")
                            {
                                if (GeneralSetting.pubDefPACKINSI == "Yes")
                                {
                                    if (header.PACK_NO > 0)
                                    {
                                        int PubRes1Int = Convert.ToInt32(GetText($@"select v_no from sale2 where pack_type = '{header.PACK_TYPE}' and pack_no = {detail.PACK_NO} and isnull(Status, 0) <> 2 and ITEM_CODE = {detail.ITEM_CODE} and comp_code = {GlobalData.PubCompCode}
                                        and branch_code= {GlobalData.PubCompCode} and year_code= {GlobalData.PubFYearCode} and v_type='{header.V_TYPE}' and  v_no <> '{header.V_TYPE}' "));


                                        if (PubRes1Int > 0)
                                        {
                                            return ("Validation",
                                            $"Packing Slip No. already Exist in Sale, Serial No.  {PubRes1Int}");

                                        }

                                        if (detail.PACK_NO > 0)
                                        {

                                            return ("Validation",
                                            $"Packing No. can not be Blank, Please Check it of =  {detail.ITEM_NAME}");

                                        }

                                        packtyp = "";

                                        if (header.V_TYPE == "SAGT" || header.V_TYPE == "SABS")
                                        {

                                            if (header.V_TYPE == "Flakes")
                                            {
                                                packtyp = "'SFIS','SFEI'";
                                            }
                                            else
                                            {
                                                packtyp = "'FPIS'";
                                            }

                                        }
                                        else if (header.V_TYPE == "SACH")
                                        {
                                            packtyp = "'FGIS','FGRC'";
                                        }

                                        if (GlobalData.PubCompCode == "1" && (packtyp == "FPIS" || packtyp == "FGIS"))
                                        {
                                            string tenCatOrder = GetText($@"select TENACITY_CODE from ORDER2 where v_type='SORD' and v_no={detail.ORD_NO} and ITEM_CODE={detail.ITEM_CODE} and COMP_CODE={GlobalData.PubCompCode} and Branch_code={GlobalData.PubBranchCode}");
                                            string tenCatPack = GetText($@"Select distinct TENACITY_CODE from PRODUCTION2 where v_type in ({packtyp}) and v_no={header.PACK_NO} and ITEM_CODE={detail.ITEM_CODE} and COMP_CODE={GlobalData.PubCompCode} and Branch_code={GlobalData.PubBranchCode}");

                                            if (tenCatOrder != "" && tenCatPack != "")
                                            {
                                                if (tenCatOrder != tenCatPack)
                                                {
                                                    return ("Validation",
                                                    $"Tenacity not matched in Sales Order and in Packing Slip, Please check it for Item =  {detail.ITEM_NAME}");
                                                }
                                            }
                                        }

                                        if (GlobalData.PubCompCode == "2" || GlobalData.PubCompCode == "4" || GlobalData.PubCompCode == "5")
                                        {

                                            decimal packQty = Convert.ToDecimal(GetText($@"SELECT sum(QTY) FROM PRODUCTION2 WHERE ITEM_CODE={detail.ITEM_CODE} and V_NO={detail.PACK_NO} and v_type in ({packtyp}) and COMP_CODE= {GlobalData.PubCompCode} and BRANCH_CODE={GlobalData.PubBranchCode}"));

                                            if (packQty < detail.QTY)
                                            {
                                                return ("Validation",
                                                $"Packing Qty is less than Sale Qty, Please check it for Item =  {detail.ITEM_NAME}");

                                            }
                                        }

                                        else
                                        {
                                            decimal packQty = Convert.ToDecimal(GetText($@"SELECT sum(QTY) FROM PRODUCTION2 WHERE ITEM_CODE={detail.ITEM_CODE} and V_NO={detail.PACK_NO} and v_type in ({packtyp}) and COMP_CODE={GlobalData.PubCompCode} and BRANCH_CODE={GlobalData.PubBranchCode}"));

                                            if(packQty != detail.QTY)
                                            {
                                                return ("Validation",
                                                $"Packing Qty ({packQty}) and Invoice Qty ({detail.QTY}) not matched of Item {detail.ITEM_NAME}, Please Check it.");
                                            }

                                        }

                                    }
                                }
                            }
                        }

                        if(header.V_TYPE == "SAJI"  || header.V_TYPE == "SASI")
                        {
                            
                        }
                        else
                        {
                            if(header.ISSUE_NO > 0 && checkIssueNo == true)
                            {
                                if (IsExist($@"  SELECT 1  FROM Issue2 WHERE V_TYPE = '{header.ISSUE_TYPE}' AND V_NO = {header.ISSUE_NO}
                                    AND COMP_CODE = {GlobalData.PubCompCode}  AND BRANCH_CODE = {GlobalData.PubBranchCode}"))
                                {
                                    // Check whether Sale Item exists in Issue
                                    if (IsExist($@"
                                    SELECT 1 FROM Issue2 WHERE ITEM_CODE = {detail.ITEM_CODE} AND V_TYPE = '{header.ISSUE_TYPE}'
                                    AND V_NO = {header.ISSUE_NO}  AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}"))
                                    {
                                        // Get Issue Quantity
                                        decimal issueQty = Convert.ToDecimal(GetText($@"
                                            SELECT ISNULL(SUM(QTY), 0) FROM Issue2  WHERE ITEM_CODE = {detail.ITEM_CODE} AND LOT_NO = '{detail.LOT_No}'
                                            AND V_TYPE = '{header.ISSUE_TYPE}' AND V_NO = {header.ISSUE_NO} AND COMP_CODE = {GlobalData.PubCompCode}
                                            AND BRANCH_CODE = {GlobalData.PubBranchCode}"));

                                        decimal saleQty = Convert.ToDecimal(detail.QTY ?? 0);

                                        if (issueQty != saleQty)
                                        {
                                            return ("Validation",
                                            $"Issue Qty not matched with Sale Qty of Item: ({detail.ITEM_CODE}) {detail.ITEM_NAME}, " +
                                            $"Issue No: {header.ISSUE_NO} and LotNo = {detail.LOT_No}.");
                                        }
                                    }
                                    else
                                    {
                                        return ("Validation",
                                        $"Sale Item and Issue Item not matched, please check for Item: ({detail.ITEM_CODE}) " +
                                        $"{detail.ITEM_NAME}, Issue No: {header.ISSUE_NO}.");
                                    }
                                }
                                else
                                {
                                    // Original VB had Return False commented out, so only warning/message.
                                    // Do not return here if you want exactly the same behavior.
                                }
                            }
                        }

                        if (header.V_TYPE != "SASI" && header.V_TYPE != "SAST")
                        {
                            decimal stkqty = Convert.ToDecimal(GetText($@"
                                SELECT ISNULL(QTY, 0) FROM tmpStockBalance WHERE ITEM_CODE = {detail.ITEM_CODE} AND COMP_CODE = {GlobalData.PubCompCode}"));

                            if (action == "UPDATE")
                            {
                                decimal saleQty = Convert.ToDecimal(GetText($@"
                                SELECT ISNULL(SUM(Qty), 0) FROM SALE2 WHERE ITEM_CODE = {detail.ITEM_CODE} AND ISNULL(Status, 0) <> 2 AND V_TYPE = '{header.V_TYPE}'
                                AND V_NO = {header.V_NO} AND COMP_CODE = {GlobalData.PubCompCode}   AND BRANCH_CODE = {GlobalData.PubBranchCode} AND YEAR_CODE = {GlobalData.PubFYearCode}"));

                                stkqty += saleQty;
                            }


                            if (stkqty <= 0)
                            {
                                return ("Validation", $"Stock not available, Please Check it of = {detail.ITEM_NAME}");
                            }
                        }



                        if(header.V_TYPE == "SAGT" && header.ITEM_TYPE != "Other")
                        {
                            if(GeneralSetting.pubDefSSINSI == "Yes")
                            {
                                if(detail.SAUDA_TYPE == "")
                                {
                                    return ("Validation", $"Sauda Type can not be Blank, Please Check it of = {detail.ITEM_NAME}");
                                }


                                if(detail.SAUDA_NO == 0)
                                {
                                    return ("Validation",  $"Sauda No can not be Blank, Please Check it of = {detail.ITEM_NAME}");
                                }

                                if(detail.SAUDA_RATE == 0)
                                {
                                    return ("Validation", $"Sauda Rate can not be Blank, Please Check it of = {detail.ITEM_NAME}");
                                }

                                decimal pubRes1Dbl = Convert.ToDecimal(GetText($@"
                                    SELECT ISNULL(SUM(QTY), 0) FROM SAUDA WHERE V_TYPE = '{detail.SAUDA_TYPE}'
                                    AND V_NO = {detail.SAUDA_NO} AND COMP_CODE = {GlobalData.PubCompCode} AND BRANCH_CODE = {GlobalData.PubBranchCode}"));

                                    decimal pubRes2Dbl = Convert.ToDecimal(GetText($@"
                                        SELECT ISNULL(SUM(QTY), 0) FROM SALE2 WHERE SAUDA_TYPE = '{detail.SAUDA_TYPE}'  AND SAUDA_NO = {detail.SAUDA_NO}
                                        AND ISNULL(Status, 0) <> 2  AND COMP_CODE = {GlobalData.PubCompCode}  AND BRANCH_CODE = {GlobalData.PubBranchCode}
                                        AND V_TYPE = '{header.V_TYPE}'  AND V_NO <> {header.V_NO}"));

                                pubRes2Dbl += Convert.ToDecimal(detail.QTY ?? 0);

                                if (pubRes2Dbl > (pubRes1Dbl + 3000))
                                {
                                    decimal totalNet = Convert.ToDecimal(header.TOT_NET ?? 0);

                                    return ("Validation",
                                        $"Sauda Pending Quantity is = {pubRes1Dbl - pubRes2Dbl + totalNet:0.00}, " +
                                        $"Your Invoice is = {totalNet:0.00}, Please Check it.");
                                }
                            }
                        }



                        if(GeneralSetting.pubDefSOINSI == "Yes")
                        {
                            if(detail.ORD_TYPE == "")
                            {
                                return ("Validation", $"Order Type can not be Blank, Please Check it of = {detail.ITEM_NAME}");
                            }


                            if(detail.ORD_NO == 0)
                            {
                                return ("Validation", $"Order No can not be Blank, Please Check it of = {detail.ITEM_NAME}");
                            }

                            if(detail.ORD_RATE == 0)
                            {
                                return ("Validation", $"Order Rate can not be Blank, Please Check it of = {detail.ITEM_NAME}");
                            }


                            decimal pubRes1Dbl = 0;
                            decimal pubRes2Dbl = 0;

                            if (detail.ORD_TYPE == "DOGT")
                            {
                                pubRes1Dbl = Convert.ToDecimal(GetText($@" SELECT ISNULL(SUM(QTY), 0) FROM DO2 WHERE V_TYPE = '{detail.ORD_TYPE}'
                                    AND V_NO = {detail.ORD_NO} AND COMP_CODE = {GlobalData.PubCompCode}  AND BRANCH_CODE = {GlobalData.PubBranchCode}
                                    AND ITEM_CODE = {detail.ITEM_CODE}"));
                            }
                            else
                            {
                                pubRes1Dbl = Convert.ToDecimal(GetText($@"
                                    SELECT ISNULL(SUM(QTY), 0)  FROM ORDER2  WHERE V_TYPE = '{detail.ORD_TYPE}'
                                    AND V_NO = {detail.ORD_NO}
                                    AND COMP_CODE = {GlobalData.PubCompCode}
                                    AND BRANCH_CODE = {GlobalData.PubBranchCode}
                                    AND ITEM_CODE = {detail.ITEM_CODE}"));
                            }

                            pubRes2Dbl = Convert.ToDecimal(GetText($@" SELECT ISNULL(SUM(QTY), 0)  FROM SALE2  WHERE ORD_TYPE = '{detail.ORD_TYPE}'
                                AND ORD_NO = {detail.ORD_NO}  AND ISNULL(Status, 0) <> 2   AND COMP_CODE = {GlobalData.PubCompCode}  AND BRANCH_CODE = {GlobalData.PubBranchCode}
                                AND YEAR_CODE = {GlobalData.PubFYearCode} AND ITEM_CODE = {detail.ITEM_CODE} AND V_TYPE = '{header.V_TYPE}' AND V_NO <> {header.V_NO}"));

                            decimal invoiceQty = Convert.ToDecimal(detail.QTY ?? 0);

                            pubRes2Dbl += invoiceQty;

                            if (pubRes2Dbl > (pubRes1Dbl + 3000))
                            {
                                decimal pendingQty = pubRes1Dbl - pubRes2Dbl + invoiceQty;

                                return ("Validation",
                                    $"Order Pending Quantity is = {pendingQty:0.00} " +
                                    $"and Your Invoice Qty is = {invoiceQty:0.00}, " +
                                    $"Please Check it of Item Name {detail.ITEM_NAME}");
                            }
                        }

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
