using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class DocumentEmailingRepository : IDocumentEmailingRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public DocumentEmailingRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        // ======================FILL DATA======================
        public async Task<RepositoryResponseList<Dictionary<string, object>>> FillData(string mailType, string docType, DateTime fromDate,
            DateTime toDate, int[] partyCodes, int agentCode = 0, string itemType = "", int acGroup = 0)
        {
            var response = new RepositoryResponseList<Dictionary<string, object>>();
            var gv = _globalVariableService.GetGlobalVariables();

            mailType = (mailType ?? "").ToLower();
            partyCodes ??= Array.Empty<int>();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                using var cmd = new SqlCommand { Connection = con };
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                cmd.Parameters.AddWithValue("@FROM", fromDate.Date);
                cmd.Parameters.AddWithValue("@TO", toDate.Date);

                // Convert Party codes into parameters: " and <col> in (@P0,@P1..)"
                bool partyParamsAdded = false;
                string PartyFilter(string col)
                {
                    if (partyCodes.Length == 0) return "";
                    var names = new List<string>();
                    for (int i = 0; i < partyCodes.Length; i++)
                    {
                        names.Add("@P" + i);
                        if (!partyParamsAdded) cmd.Parameters.AddWithValue("@P" + i, partyCodes[i]);
                    }
                    partyParamsAdded = true;
                    return $" and {col} in ({string.Join(",", names)})";
                }

                string query = "";
                var sb = new StringBuilder();

                // ===================== Debtors =====================
                if (mailType == "outstanding")
                {
                    if (partyCodes.Length == 0 && acGroup <= 0 && agentCode <= 0)
                    {
                        response.status = false;
                        response.message = "Please select Party or Group or Agent.";
                        return response;
                    }

                    //====================================================================================================
                    // Service has not been implemented yet.
                    // Replace this with the respective service once it is implemented.
                    //====================================================================================================

                    // DebtorsOutstanding("", groupName, actName, osName, toDate)

                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@USERID", gv.PubUserId);

                    query = @"select a.V_TYPE VTYPE, a.V_NO VNO, format(a.V_DATE,'dd/MM/yyyy') VDATE, b.NAME NAME, a.V_NO INVNO,
                      format(a.V_DATE,'dd/MM/yyyy') INVDATE, a.DR_AMT-a.CR_AMT AMT, b.EMAIL EMAIL, a.FOR_CODE PCODE
                      from TEMP_DEBTOROS a left join SUBGROUP_MAST b on a.FOR_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE
                      where a.WSID=@WSID and a.USERID=@USERID and a.COMP_CODE=@COMP_CODE
                      and isnull(a.DR_AMT,0)-isnull(a.CR_AMT,0)>0 and b.CREDIT_DAYS<a.LIMIT_DAYS
                      order by b.NAME, a.V_DATE, a.V_NO";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(docType))
                    {
                        response.status = false;
                        response.message = "Please select Document Type.";
                        return response;
                    }

                    cmd.Parameters.AddWithValue("@V_TYPE", docType);

                    switch (docType)
                    {
                        // ---------- Dr Note/Cr Note ----------
                        case "BFPB":
                        case "RIMP":
                        case "RMPB":
                        case "STJW":
                        case "STPB":
                            {
                                string amtCond = "";

                                if (mailType == "drnote")
                                    amtCond = "(a.QC_DR_AMT>0 or a.QLT_DR_AMT>0 or a.QTY_DR_AMT>0 or a.RDF_DR_AMT>0 or a.OTH_DR_AMT>0)";
                                else if (mailType == "crnote")
                                    amtCond = "(a.QC_CR_AMT>0 or a.QLT_CR_AMT>0 or a.QTY_CR_AMT>0 or a.RDF_CR_AMT>0)";

                                if (amtCond == "") break;

                                sb.Append(@"select a.V_TYPE VTYPE, a.V_NO VNO, format(a.V_DATE,'dd/MM/yyyy') VDATE, b.NAME NAME, a.BILL_NO INVNO,
                                format(a.BILL_DATE,'dd/MM/yyyy') INVDATE, a.NAMOUNT AMT, b.EMAIL EMAIL, a.PARTY_CODE PCODE
                                from PURCHASE1 a left join SUBGROUP_MAST b on a.PARTY_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE
                                where a.V_TYPE=@V_TYPE and " + amtCond + @" and a.COMP_CODE=@COMP_CODE and a.BRANCH_CODE=@BRANCH_CODE
                                and a.YEAR_CODE=@YEAR_CODE and a.V_DATE between @FROM and @TO");
                                sb.Append(PartyFilter("a.PARTY_CODE"));
                                sb.Append(" order by a.V_NO");
                                query = sb.ToString();
                                break;
                            }

                        // ---------- Sales Invoice Customer/Agent ----------
                        case "SAGT":
                            {
                                if (mailType == "salesinvoicecustomer")
                                {
                                    sb.Append(@"select a.V_TYPE VTYPE, a.V_NO VNO, format(a.V_DATE,'dd/MM/yyyy') VDATE, b.NAME NAME, a.V_NO INVNO,
                                    format(a.V_DATE,'dd/MM/yyyy') INVDATE, a.NAMOUNT AMT, b.EMAIL EMAIL, a.BILL_CODE PCODE
                                    from SALE1 a left join SUBGROUP_MAST b on a.BILL_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE
                                    where a.V_TYPE=@V_TYPE and a.COMP_CODE=@COMP_CODE and a.BRANCH_CODE=@BRANCH_CODE
                                    and isnull(a.STATUS,0)<>2 and a.YEAR_CODE=@YEAR_CODE and a.V_DATE between @FROM and @TO");
                                    sb.Append(PartyFilter("a.BILL_CODE"));

                                    if (!string.IsNullOrWhiteSpace(itemType))
                                    {
                                        sb.Append(" and a.ITEM_TYPE=@ITEM_TYPE");
                                        cmd.Parameters.AddWithValue("@ITEM_TYPE", itemType);
                                    }
                                    sb.Append(" order by a.V_NO");
                                    query = sb.ToString();
                                }
                                else if (mailType == "salesinvoiceagent")
                                {
                                    sb.Append(@"select a.V_TYPE VTYPE, a.V_NO VNO, format(a.V_DATE,'dd/MM/yyyy') VDATE, b.NAME NAME, a.V_NO INVNO,
                                    format(a.V_DATE,'dd/MM/yyyy') INVDATE, a.NAMOUNT AMT, b.EMAIL EMAIL, a.AGENT_CODE PCODE
                                    from SALE1 a left join SUBGROUP_MAST b on a.AGENT_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE
                                    where a.V_TYPE=@V_TYPE and a.COMP_CODE=@COMP_CODE and a.BRANCH_CODE=@BRANCH_CODE
                                    and isnull(a.STATUS,0)<>2 and a.YEAR_CODE=@YEAR_CODE and a.V_DATE between @FROM and @TO");
                                    if (agentCode > 0)
                                    {
                                        sb.Append(" and a.AGENT_CODE=@AGENT_CODE");
                                        cmd.Parameters.AddWithValue("@AGENT_CODE", agentCode);
                                    }
                                    sb.Append(" order by a.V_NO");
                                    query = sb.ToString();
                                }
                                break;
                            }

                        // ---------- Packing Slip / Packing Slip in Excel ----------
                        case "FPIS":
                            {
                                sb.Append(@"select a.PACK_TYPE VTYPE, a.PACK_NO VNO, format(a.V_DATE,'dd/MM/yyyy') VDATE, b.NAME NAME, a.V_NO INVNO,
                                format(a.V_DATE,'dd/MM/yyyy') INVDATE, a.NAMOUNT AMT, b.EMAIL EMAIL, a.BILL_CODE PCODE
                                from SALE1 a left join SUBGROUP_MAST b on a.BILL_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE
                                where a.V_TYPE='SAGT' and a.PACK_NO>0 and a.COMP_CODE=@COMP_CODE and a.BRANCH_CODE=@BRANCH_CODE
                                and isnull(a.STATUS,0)<>2 and a.YEAR_CODE=@YEAR_CODE and a.V_DATE between @FROM and @TO
                                and isnull(b.EMAIL,'')<>''");
                                sb.Append(PartyFilter("a.BILL_CODE"));
                                sb.Append(" order by a.V_NO");
                                query = sb.ToString();
                                break;
                            }

                        // ---------- Party Balance ----------
                        case "BPMS":
                            {
                                if (mailType != "partybalance") break;

                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                cmd.Parameters.AddWithValue("@USERID", gv.PubUserId);

                                using (var spCmd = new SqlCommand("sp_DocumentEmailing", con) { CommandType = CommandType.StoredProcedure })
                                {     
                                    spCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    spCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                    spCmd.Parameters.AddWithValue("@TO", toDate.Date);
                                    spCmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                    spCmd.Parameters.AddWithValue("@USERID", gv.PubUserId);
                                    spCmd.Parameters.AddWithValue("@AC_GROUP", acGroup);
                                    spCmd.Parameters.AddWithValue("@PARTY_CODES", string.Join(",", partyCodes));
                                    await spCmd.ExecuteNonQueryAsync();
                                }

                                sb.Append(@"select 'BPMS' VTYPE, a.for_code VNO, format(a.V_DATE,'dd/MM/yyyy') VDATE, b.NAME NAME, 0 INVNO,
                                format(a.V_DATE,'dd/MM/yyyy') INVDATE, a.OP_BAL AMT, b.EMAIL EMAIL, 0 PCODE
                                from temp_ledger a left join SUBGROUP_MAST b on a.FOR_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE
                                where a.COMP_CODE=@COMP_CODE and a.BRANCH_CODE=@BRANCH_CODE and a.wsid=@WSID and a.userid=@USERID");
                                sb.Append(PartyFilter("a.FOR_CODE"));
                                sb.Append(" order by b.NAME");
                                query = sb.ToString();
                                break;
                            }
                    }
                }

                // ===================== Execute the query =====================
                var rows = new List<Dictionary<string, object>>();

                if (query != "")
                {
                    cmd.CommandText = query;

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        rows.Add(row);
                    }
                }

                response.status = true;
                response.data = rows;
                response.totalCount = rows.Count;
                return response;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }

        
        // ======================EXPORT INVOICE======================
        public async Task<RepositoryResponseData<object>> PrepareInvoiceExport(string vType, long vNo)
        {
            var response = new RepositoryResponseData<object>();
            var gv = _globalVariableService.GetGlobalVariables();

            if (string.IsNullOrWhiteSpace(vType) || vNo <= 0)
            {
                response.status = false;
                response.message = "Voucher Type and Voucher No are required.";
                return response;
            }

            vType = vType.Replace("'", "");

            try
            {
                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                using var cmd = new SqlCommand { Connection = con };
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);

                cmd.CommandText = "delete from tempInvoice where comp_code=@COMP_CODE and Branch_code=@BRANCH_CODE";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = @"insert into tempInvoice
                    select a.comp_code,a.Branch_code,a.V_type,a.v_no,1,'Triplicate for Office',b.IRN,b.QR_IMAGE
                    from sale1 a left join QRImage_Path b on a.v_no=b.v_no and a.V_TYPE=b.V_TYPE and a.COMP_CODE=b.COMP_CODE
                    and a.BRANCH_CODE=b.BRANCH_CODE and a.YEAR_CODE=b.YEAR_CODE
                    where a.comp_code=@COMP_CODE and a.branch_code=@BRANCH_CODE and a.v_type=@V_TYPE and a.v_no=@V_NO";
                await cmd.ExecuteNonQueryAsync();

                // Godown address
                cmd.CommandText = @"select Godown_code from Sale1 where V_Type=@V_TYPE and v_no=@V_NO and comp_code=@COMP_CODE
                                    and Branch_Code=@BRANCH_CODE and Year_code=@YEAR_CODE";
                var o = await cmd.ExecuteScalarAsync();
                int gwdn = (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);

                cmd.Parameters.AddWithValue("@GODOWN", gwdn);
                cmd.CommandText = "select isnull(ADDRESS,'') from GODOWN_MAST where CODE=@GODOWN and COMP_CODE=@COMP_CODE";
                o = await cmd.ExecuteScalarAsync();
                string godownAdd = (o == null || o == DBNull.Value) ? "" : Convert.ToString(o);

                // Net amount
                cmd.CommandText = @"select NAMOUNT from SALE1 where V_TYPE=@V_TYPE and V_NO=@V_NO and COMP_CODE=@COMP_CODE
                                    and BRANCH_CODE=@BRANCH_CODE and YEAR_CODE=@YEAR_CODE";
                o = await cmd.ExecuteScalarAsync();
                decimal netAmt = (o == null || o == DBNull.Value) ? 0 : Convert.ToDecimal(o);

                // Report name by company
                string reportName = gv.PubCompCode switch
                {
                    "2" => "INVOICE_GST1PLPLQR_DETAIL",
                    "4" => "INVOICE_GST1PEPLQR_DETAIL",
                    "5" => "INVOICE_GST1SALASARQR_DETAIL",
                    "3" => "INVOICE_GST1",
                    "7" => "INVOICE_GST1KQRNew",
                    _ => "INVOICE_GST1QR"
                };

                string selectionFormula =
                    "{SALE1.V_TYPE}='" + vType + "'" +
                    " AND {SALE1.V_NO}=" + vNo +
                    " AND {SALE1.COMP_CODE}=" + gv.PubCompCode +
                    " AND {SALE1.YEAR_CODE}=" + gv.PubFYearCode +
                    " AND {SALE1.BRANCH_CODE}=" + gv.PubBranchCode;

                var parameters = new Dictionary<string, string>
                {
                    ["RPTNAME"] = "TAX INVOICE",
                    ["comp_name"] = gv.CompanyName,
                    ["comp_add1"] = gv.Address1,
                    ["comp_add2"] = gv.Address2,
                    ["comp_phone"] = "Mobile :" + gv.Phone,
                    ["GST"] = "GSTIN       :" + gv.gstin,
                    ["PAN"] = "PAN NO.   :" + gv.PAN,
                    ["Website"] = "Web  :" + gv.Website,
                    ["EMAIL"] = "Email   :" + gv.Email,
                    ["godownAdd"] = godownAdd != "" ? "Ship From : " + godownAdd : "",
                    ["INWORD"] = NumberToWords(netAmt)
                };

                if (gv.PubCompCode != "3")
                {
                    parameters["comp_name1"] = "An ISO 9001:2015 Certified Company";
                    parameters["Comp_reg"] = "Reg.Office :" + gv.RegAdd1 + ", " + gv.RegAdd2 + "  CIN :" + gv.CINNO;
                }

                response.status = true;
                response.data = new { reportName, selectionFormula, parameters };
                return response;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }

        private static readonly string[] Ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        private static readonly string[] Tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
        private static string WordsBelow1000(int n)
        {
            string s = "";
            if (n >= 100) { s += Ones[n / 100] + " Hundred"; n %= 100; if (n > 0) s += " "; }
            if (n >= 20) { s += Tens[n / 10]; if (n % 10 > 0) s += " " + Ones[n % 10]; }
            else if (n > 0) s += Ones[n];
            return s;
        }
        private static string WordsIndian(long n)
        {
            if (n == 0) return "Zero";

            var parts = new List<string>();
            long crore = n / 10000000; n %= 10000000;
            long lakh = n / 100000; n %= 100000;
            long thousand = n / 1000; n %= 1000;

            if (crore > 0) parts.Add(WordsIndian(crore) + " Crore");
            if (lakh > 0) parts.Add(WordsBelow1000((int)lakh) + " Lakh");
            if (thousand > 0) parts.Add(WordsBelow1000((int)thousand) + " Thousand");
            if (n > 0) parts.Add(WordsBelow1000((int)n));

            return string.Join(" ", parts);
        }
        private static string NumberToWords(decimal amount)
        {
            long rupees = (long)Math.Floor(amount);
            int paise = (int)Math.Round((amount - rupees) * 100);

            string s = "Rs. " + WordsIndian(rupees);
            if (paise > 0) s += " and " + WordsIndian(paise) + " PAISE";

            return s + " Only";
        }

        // ======================SEND MAIL======================
        public async Task<RepositoryResponseList<object>> SendMail(SendMailRequest req)
        {
            var response = new RepositoryResponseList<object>();
            var gv = _globalVariableService.GetGlobalVariables();
            var results = new List<object>();
            void Add(bool ok, string msg) => results.Add(new { success = ok, message = msg });

            try
            {
                // Check Internet Connection
                if (!await CheckForInternetConnection())
                {
                    Add(false, "Internet not accessible. Mail/SMS not send.");
                    response.status = true;
                    response.data = results;
                    response.totalCount = results.Count;
                    return response;
                }

                string mailType = (req.MailType ?? "").ToLower();
                string docType = DocTypeFor(mailType);
                var rows = req.Rows ?? new List<MailRowDto>();

                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                string compName = gv.CompanyName, add1 = gv.Address1, add2 = gv.Address2, compEmail = gv.Email;

                // ===================== Outstanding =====================
                if (mailType == "outstanding")
                {
                    if (rows.Count > 0)
                    {
                        int d = rows.Count - 1;
                        string curparty = (rows[0].Name ?? "").Trim();
                        string mailid = "";

                        double crDays = await GetCrDays(con, rows[0].PCode, gv.PubCompCode);
                        string mailMatter = OsHeader(curparty, crDays, false);

                        for (int i = 0; i < rows.Count; i++)
                        {
                            var r = rows[i];

                            if ((r.Name ?? "").Trim() == curparty)
                            {
                                mailid = (r.Email ?? "").Trim();
                                mailMatter += OsRow(r);
                            }
                            else
                            {
                                mailMatter += OsFooter(compEmail, compName, add1, add2);

                                //====================================================================================================
                                // Service has not been implemented yet.
                                // Replace this with the respective service once it is implemented.
                                //====================================================================================================

                                //bool ok = await MailSendMatterOnly("OS", "SAGT", 1, mailid, mailMatter);
                                //Add(ok, ok ? $"Mail sucessfully sent to Party=> {curparty}." : "Mail not sent.");

                                curparty = (r.Name ?? "").Trim();
                                mailid = (r.Email ?? "").Trim();
                                crDays = await GetCrDays(con, r.PCode, gv.PubCompCode);
                                mailMatter = OsHeader(curparty, crDays, true) + OsRow(r);
                            }

                            if (i == d)
                            {
                                mailMatter += OsFooter(compEmail, compName, add1, add2);

                                //====================================================================================================
                                // Service has not been implemented yet.
                                // Replace this with the respective service once it is implemented.
                                //====================================================================================================

                                //bool ok = await MailSendMatterOnly("OS", "SAGT", 1, mailid, mailMatter);
                                //Add(ok, ok ? $"Mail sucessfully sent to Party=> {curparty}." : "Mail not sent.");
                            }
                        }
                    }
                }
                // ===================== Other mail types =====================
                else
                {
                    string asOnDate = DateTime.TryParse(req.ToDate, out var td) ? td.ToString("dd.MM.yyyy") : "";

                    foreach (var r in rows)
                    {
                        string vtype = (r.VType ?? "").Trim();
                        string vno = (r.VNo ?? "").Trim();
                        string mailid = (r.Email ?? "").Trim();
                        string label = vtype + vno;

                        if (vtype == "FPIS")
                        {
                            if (await IsExist(con,
                                "select 1 from Ledger2 where V_type='SAGT' and V_NO=@N and COMP_CODE=@C and BRANCH_CODE=@B and Year_Code=@Y",
                                ("@N", ToLong(r.InvNo)), ("@C", gv.PubCompCode), ("@B", gv.PubBranchCode), ("@Y", gv.PubFYearCode)))
                            {
                                bool inExcel = mailType == "packingslipexcel";

                                //====================================================================================================
                                // Service has not been implemented yet.
                                // Replace this with the respective service once it is implemented.
                                //====================================================================================================

                                //bool ok = await MailSend(docType, vtype, vno, mailid, req.Message, inExcel);
                                //Add(ok, ok ? $"Mail sent sucessfully of {label}." : "Mail not sent.");
                            }
                        }
                        else if (vtype == "BPMS")
                        {
                            if (await IsExist(con,
                                "select 1 from temp_ledger where FOR_CODE=@F and COMP_CODE=@C and WSID=@W and USERID=@U",
                                ("@F", ToLong(vno)), ("@C", gv.PubCompCode), ("@W", gv.PubWorkStationID), ("@U", gv.PubUserId)))
                            {

                                //====================================================================================================
                                // Service has not been implemented yet.
                                // Replace this with the respective service once it is implemented.
                                //====================================================================================================

                                //bool ok = await MailSend(r.Name, vtype, vno, mailid, req.Message, false, r.Amt.ToString(CultureInfo.InvariantCulture), asOnDate);
                                //Add(ok, ok ? $"Mail sent sucessfully of {label}." : "Mail not sent.");
                            }
                        }
                        else
                        {
                            if (await IsExist(con,
                                "select 1 from Ledger2 where V_type=@T and V_NO=@N and COMP_CODE=@C and BRANCH_CODE=@B and Year_Code=@Y",
                                ("@T", vtype), ("@N", ToLong(vno)), ("@C", gv.PubCompCode), ("@B", gv.PubBranchCode), ("@Y", gv.PubFYearCode)))
                            {
                                //====================================================================================================
                                // Service has not been implemented yet.
                                // Replace this with the respective service once it is implemented.
                                //====================================================================================================

                                //bool ok = await MailSend(docType, vtype, vno, mailid, req.Message);
                                //Add(ok, ok ? $"Mail sent sucessfully of {label}." : "Mail not sent.");
                            }
                        }
                    }
                }

                response.status = true;
                response.data = results;
                response.totalCount = results.Count;
                return response;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }

        // ---------- Helpers ----------
        private static async Task<bool> CheckForInternetConnection()
        {
            try
            {
                using (var client = new HttpClient())
                using (var response = await client.GetAsync("http://www.google.co.in", HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        private static async Task<bool> IsExist(SqlConnection con, string sql, params (string Name, object Value)[] ps)
        {
            using var cmd = new SqlCommand(sql, con);
            foreach (var p in ps) cmd.Parameters.AddWithValue(p.Name, p.Value);
            return (await cmd.ExecuteScalarAsync()) != null;
        }
        private static async Task<double> GetCrDays(SqlConnection con, string partyCode, object compCode)
        {
            using var cmd = new SqlCommand("select isnull(CR_DAYS,0) from CRLIMIT_MAST where Party_Code=@P and Comp_Code=@C", con);
            cmd.Parameters.AddWithValue("@P", ToLong(partyCode));
            cmd.Parameters.AddWithValue("@C", compCode);
            var o = await cmd.ExecuteScalarAsync();
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToDouble(o);
        }
        private static long ToLong(string s) => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? (long)n : 0;
        private static string Num(double d) => d.ToString(CultureInfo.InvariantCulture);
        private static string DocTypeFor(string mailType) => (mailType ?? "").ToLower() switch
        {
            "salesinvoicecustomer" or "salesinvoiceagent" => "SaleInvoice",
            "drnote" => "DrNote",
            "crnote" => "CrNote",
            "packingslip" or "packingslipexcel" => "SaleInvoice",
            "partybalance" => "Approval",
            _ => ""
        };
        private static string OsHeader(string party, double crDays, bool border)
        {
            string s = "Dear Sir, " + party + "<BR>";
            s += "We hope this message finds you well." + "<BR>";
            s += "This is to inform you that your credit period is <b>" + Num(crDays) + " days from Invoice date</b>. We kindly request you to ensure that payment for the invoice is made <b>before the expiry of the credit period</b> to avoid any inconvenience." + "<BR><BR>";
            s += " This is request you to Please arrange to make payment of our following due Invoice(s):";
            s += "<BR><BR> <HTML><HEAD><style>table, th, td { border: 1px solid;}table {width: 100%;border-collapse: collapse;}</style></HEAD>"
                 + (border ? "<TABLE  BORDER=1>" : "<TABLE>")
                 + "<TH>Reference No</TH><TH>Reference Date</TH><TH>Amount</TH><TH>Due Amount</TH>";
            return s;
        }
        private static string OsRow(MailRowDto r)
        {
            return "<TR>"
                 + "<TD>" + (r.VType ?? "").Trim() + (r.VNo ?? "").Trim() + "</TD>"
                 + "<TD>" + (r.VDate ?? "").Trim() + "</TD>"
                 + "<TD>" + Num(r.DrAmt) + "</TD>"
                 + "<TD>" + Num(r.Amt) + "</TD>"
                 + "</TR>";
        }
        private static string OsFooter(string compEmail, string compName, string add1, string add2)
        {
            string s = "</TABLE></HTML> <BR><BR>";
            s += "Should you have any questions or require further clarification, please feel free to contact us at <b>" + compEmail + "</b>.<BR>";
            s += "Thank you for your cooperation.<BR><BR>";
            s += " Best Regards,<BR>" + compName + "<BR>" + add1 + "<BR>" + add2;
            return s;
        }
    }
}