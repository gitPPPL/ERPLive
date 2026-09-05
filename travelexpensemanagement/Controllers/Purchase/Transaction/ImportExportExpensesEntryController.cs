using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportExportExpensesEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IImportExportExpensesEntryRepository _importExportPurchase;

        public ImportExportExpensesEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DbHelper dbHelper, GlobalValidationdate globalValidationdate, IImportExportExpensesEntryRepository importExportPurchase)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _importExportPurchase = importExportPurchase;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportExportExpensesEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetList(string type, string? vType = null, int shipFromCode = 0, int cCode = 0, int itemCode = 0)
        {
            var query = BuildListQuery(type, vType, shipFromCode, cCode, itemCode);

            if (query == null)
                return BadRequest("Invalid list type");

            var data = await _dbHelper.GetJsonDataAsync(query);

            return Json(new { success = true, data });
        }
        private string BuildListQuery(string type, string? vType, int shipFromCode, int cCode, int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            return type switch
            {
                "doctype" => "SELECT CODE as Value, NAME as Text FROM DOCTYPE_MAST WHERE DOCTYPE in ('PurchaseExpenses') ORDER BY NAME",

                "party" => $@"select Code as Value, Name as Text, ADD1, ADD2, CITY_CODE, GSTIN, PINCODE 
                                from SUBGROUP_MAST where NATURE in ('Supplier') and COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by name",

                "drcrbyvtype" => $@"select SUBGROUP_MAST.code as Value, SUBGROUP_MAST.name as Text from SUBGROUP_MAST 
                                    LEFT JOIN DOC_GLMAST ON DOC_GLMAST.COMP_CODE=SUBGROUP_MAST.COMP_CODE AND DOC_GLMAST.AC_CODE=SUBGROUP_MAST.CODE
                                    where SUBGROUP_MAST.NATURE='Others' and 
                                    SUBGROUP_MAST.COMP_CODE={gv.PubCompCode} and SUBGROUP_MAST.ACTIVE=1 AND DOC_GLMAST.DOC_CODE='{vType}'
                                    order by Name",

                "drcr" => $@"select a.code as Value, a.name as Text, a.ADD1, a.ADD2, a.CITY_CODE, a.GSTIN 
                            from SUBGROUP_MAST a where a.COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by name",

                "item" => $@"Select a.name as Text, a.CODE as Value, c.NAME as unit, c.CODE as ucode, a.HSN_CODE as hsncode
                            from item_mast a 
                            left join ITEM_MAKE b on a.code=b.ITEM_CODE and a.comp_code=b.COMP_CODE
                            left join ITEMUNIT_MAST c on a.UNIT_CODE=c.CODE and a.comp_code=c.comp_code
                            where a.comp_code={gv.PubCompCode} group by a.name ,a.CODE , c.NAME ,c.CODE, a.HSN_CODE
                            order by a.name",

                "address" => $@"select address_id Value, add1 Text from SUBGROUP_ADDRESS 
                                where code={shipFromCode} and COMP_CODE={gv.PubCompCode} order by ADDRESS_ID",

                "department" => $@"select name as Text,code as Value from ITEMDEPT_MAST where COMP_CODE={gv.PubCompCode} order by name",

                "city" => $@"select code as Value, NAME as Text from CITY_MAST where ACTIVE=1 order by Name",

                "state" => $@"select Top 1 b.CODE as Value, b.NAME as Text from CITY_MAST a
                            left join STATE_MAST b on a.STATE_CODE = b.CODE
                            where a.code = {cCode}",

                "tax" => $@"select name as Text, code as Value, CGST_PER,SGST_PER,IGST_PER,isnull(VAT_PER,0)VAT_PER,TDS_PER,TCS_PER,OTH_PER,
                            isnull(OTH_PER2,0)OTH_PER2 from TAX_MAST
                            where ACTIVE = 1 order by name",

                "status" => $@"SELECT CODE as Value, NAME as Text FROM DOCSTATUS_MAST WHERE V_TYPE = 'Document' ORDER BY CODE",

                "transport" => $@"select code as Value, ltrim(name) as Text from TRANSPORT_MAST 
                                where COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by ltrim(name)",

                "employee" => $@"Select code as Value, ltrim(rtrim(CODE))+ space(7- LEN (ltrim(rtrim(CODE))))+'|'+SPACE(1)+CAST (NAME as varchar ) as Text 
                                from EMP_MAST where  Resign_date is null and Join_date is not null and COMP_CODE = {gv.PubCompCode} and ACTIVE =1 order by name",

                "make" => $@"Select b.name as Text, a.MAKE_CODE as Value from ITEM_MAKE a left join ITEMMAKE_MAST b on a.MAKE_CODE =b.CODE and b.COMP_CODE=1
                            where a.ITEM_CODE={itemCode} and a.COMP_CODE={gv.PubCompCode} order by b.name",

                _ => ""
            };
        }

        //============VNO========================
        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "PURCHASE1");
            return Json(new { status = true, V_NO = result });
        }

        //============MRN List========================
        [HttpGet]
        public async Task<IActionResult> GetMrnNoList(string vType)
        {
            // Determine the MRN type based on vType
            string mrntype = "";
            var gv = _globalVariableService.GetGlobalVariables();

            string query = "";
            if (vType.Equals("SADP", StringComparison.OrdinalIgnoreCase))
            {
                mrntype = "'SAGT'";
                query += $@"Select a.V_TYPE as vType, DOC_ID as Text, a.V_no as Value from SALE1 a 
				where COMP_CODE={gv.PubCompCode} and YEAR_CODE>=5 and BRANCH_CODE={gv.PubBranchCode} and a.V_TYPE in ({mrntype})
				order by a.V_no";
            }
            else if (vType.Equals("STDP", StringComparison.OrdinalIgnoreCase))
            {
                query += $@"Select a.V_TYPE as vType, DOC_ID as Text, a.V_no as Value from Order1 a 
				where a.COMP_CODE={gv.PubCompCode} and a.YEAR_CODE={gv.PubFYearCode} and a.BRANCH_CODE={gv.PubBranchCode} and a.V_TYPE='DORD' 
                and a.Status=1 and a.Faprov_status='Approved'
				order by a.V_no";
            }
            else
            {
                if (vType.Equals("RMDP", StringComparison.OrdinalIgnoreCase))
                {
                    mrntype = "'RIMP','RMPB'";
                }
                else if (vType.Equals("SIDP", StringComparison.OrdinalIgnoreCase))
                {
                    mrntype = "'STPB','STJW'";
                }
                query += $@"Select a.V_TYPE as vType, a.DOC_ID as Text, a.V_no as Value from PURCHASE1 a 
                        left join CITY_MAST  b on a.BILL_CITY=b.CODE left join STATE_MAST c on b.STATE_CODE=c.CODE
                        where COMP_CODE={gv.PubCompCode} and YEAR_CODE={gv.PubFYearCode} and BRANCH_CODE={gv.PubBranchCode} and 
                        a.V_TYPE in ({mrntype})
                        order by a.V_type,a.V_no";
            }
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        public IActionResult GetAddressByBillToParty(int code, int addressId)
        {
            try
            {
                var addressDetails = _importExportPurchase.GetAddByParty(code, addressId);
                return Json(new { success = true, addressDetails });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error retrieving the address by specfic address id" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFullQuotationByVno(int vNo, string vtype)
        {
            try
            {
                var result = await _importExportPurchase.GetFullQuotationByVno(vNo, vtype);
                if (result.data != null)
                {
                    return Json(new
                    {
                        success = result.status,
                        header = result.data.Header,
                        items = result.data.Items,
                        attachments = result.data.Attachments,
                        eprAttachments = result.data.EprAttachments,
                        existingTDS = result.data.existingTDS
                    });
                }
                else
                {
                    return Json(new { success = false, message = "Data not found!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching quotation", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePurchaseBillPassEntry([FromBody] PurchaseWrapper data)
        {
            if (data == null)
            {
                return Json(new { success = false, message = "Invalid data!" });
            }
            try
            {
                var result = await _importExportPurchase.SavePurchaseBillPassEntry(data);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPackOnBasic(int code)
        {
            try
            {
                var result = await _importExportPurchase.GetPackOnBasic(code);
                return Json(new { success = result.status, packOnBasic = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //--------------- Get Existing TDS ---------------------
        [HttpPost]
        public async Task<IActionResult> CheckExistingTDS(string billNo, int drCode)
        {
            try
            {
                var result = await _importExportPurchase.CheckExistingTDS(billNo, drCode);
                return Json(new { totTDS = result });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFrtCrAcByTransCode(int transportCode)
        {

            try
            {
                var result = await _importExportPurchase.GetFrtCrAcByTransCodeAsync(transportCode);
                int partyCode = result.PartyCode;
                string partyName = result.PartyName;
                return Json(new { success = true, partyCode, partyName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //=================================Validate Date===============
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("PURCHASE1", vdate, vtype, vno);
            return Ok(result);
        }

        //================================= Validation Helpers ===============

        [HttpGet]
        public async Task<IActionResult> GetPartyPurchaseAmount(int partyCode, string vType, int? vNo = null, decimal? currentAmount = null)
        {
            try
            {
                var result = await _importExportPurchase.GetPartyPurchaseAmount(partyCode, vType, vNo, currentAmount);
                return Json(new { success = result.status, totalAmount = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTDS206Apply(int partyCode)
        {
            try
            {
                var result = await _importExportPurchase.GetTDS206Apply(partyCode);
                return Json(new { success = result.status, tds206Apply = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPurchaseOrSaleVoucherNo(string transportName, string grNo, string currentVoucher, string purchaseOrSale)
        {
            try
            {
                var result = await _importExportPurchase.GetPurchaseOrSaleVoucherNo(transportName, grNo, currentVoucher, purchaseOrSale);
                return Json(new { success = result.status, message = result.message, voucherNo = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> CheckPaymentExists(string docType, int docNo)
        {
            try
            {
                var result = await _importExportPurchase.CheckPaymentExists(docType, docNo);
                return Json(new { success = result.status, exists = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<JsonResult> CheckDuplicateBill(int partyCode, string billNo, int currentVNo)
        {
            try
            {
                var result = await _importExportPurchase.CheckDuplicateBill(partyCode, billNo, currentVNo);
                return Json(new { success = true, exists = result.Exists, docId = result.DocId, vDate = result.VDate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ValidateTaxType(int billToCode, decimal totalIGST, decimal totalCGST, decimal totalSGST)
        {
            try
            {
                var result = await _importExportPurchase.ValidateTaxType(billToCode, totalIGST, totalCGST, totalSGST);
                return Json(new { success = result.status, isValid = result.data, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> getGlobalValues()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();
                using var erpCon = _dbConnection.GetErpConnection();

                string databaseName;
                using (var connection = _dbConnection.GetErpConnection())
                {
                    databaseName = connection.Database; // Get the database name
                }

                var response = new
                {
                    userLevel = gv.PubUserLevel,
                    compCode = gv.PubCompCode,
                    yearCode = gv.PubFYearCode,
                    branchCode = gv.PubBranchCode,
                    pubDefPOInMRN = gs.pubDefPOInMRN,
                    dataSource = erpCon.DataSource,
                    add1 = gv.Address1,
                    add2 = gv.Address2,
                    companyName = gv.CompanyName,
                    companyGst = gv.gstin,
                    db = databaseName
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ValidatePartyGst(string gstType, string partyCode, string gstNo)
        {
            try
            {
                var result = await _importExportPurchase.ValidatePartyGst(gstType, partyCode, gstNo);
                return Json(new { success = true, result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CalculateTDS([FromBody] PURCHASE1 model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid request." });
            }
            try
            {
                var result = await _importExportPurchase.CalculateTDS(model);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCopyFromMenu()
        {
            var result = _importExportPurchase.GetCopyFromMenu();

            if (!result.status)
                return Json(new { success = result.status, message = result.message });

            return Json(new { success = result.status, data = result.data });
        }

        [HttpPost]
        public IActionResult GetCopyFromData([FromBody] CopyFromRequest request)
        {
            var result = _importExportPurchase.GetCopyFromData(request);

            return Json(new { success = result.status, message = result.message, data = result.data });
        }

        [HttpGet]
        public IActionResult GetPendingApprovalList()
        {
            var result = _importExportPurchase.GetPendingApprovalList();

            return Json(new { success = result.status, message = result.message, data = result.data });
        }


        //===========RMDP MRN Details===========
        [HttpGet]
        public async Task<IActionResult> loadRMDPMRNData(string mrnType, int mrnNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string qry = $@"select CHALL_NO, CHALL_DATE, BL_NO, BL_DT from PURCHASE1 where V_TYPE=@V_TYPE and v_no=@v_no and 
                            COMP_CODE=@COMP_CODE and BRANCH_CODE=@BRANCH_CODE and YEAR_CODE=@YEAR_CODE";
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();
                using SqlCommand cmd = new SqlCommand(qry, con);
                cmd.Parameters.AddWithValue("@V_TYPE", mrnType);
                cmd.Parameters.AddWithValue("@v_no", mrnNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                await con.OpenAsync();
                using SqlDataReader dr = await cmd.ExecuteReaderAsync();
                object data = null;

                if (await dr.ReadAsync())
                {
                    data = new
                    {
                        challNo = dr["CHALL_NO"]?.ToString(),
                        challDate = dr["CHALL_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["CHALL_DATE"]),
                        blNo = dr["BL_NO"]?.ToString(),
                        blDate = dr["BL_DT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["BL_DT"])
                    };
                }
                return Json(new { success = true, mrnData = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ValidateFreightExpense(string refType, string refVNo, string expsType)
        {
            try
            {
                var result = await _importExportPurchase.ValidateFreightExpense(refType, refVNo, expsType);

                return Json(new { success = result.status, data = result.data, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ValidateImportTracking(string refVType, string refVNo, string billFromCode, string billNo, string billToName)
        {
            try
            {
                var result = await _importExportPurchase.ValidateImportTracking(refVType, refVNo, billFromCode, billNo, billToName);

                return Json(new { success = result.status, data = result.data, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ValidateCostAllocation(ValidateCostAllocationRequest model)
        {
            try
            {
                var result = await _importExportPurchase.ValidateCostAllocation(model);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new RepositoryResponseData<bool> { status = false, data = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetImportInvoiceList(int partyCode)
        {
            var result = _importExportPurchase.GetImportInvoiceList(partyCode);

            return Json(new { success = result.status, message = result.message, data = result.data });
        }

        // ---------------------------------------------------------
        // TDS ADJUSTMENT
        // ---------------------------------------------------------

        [HttpPost]
        public async Task<JsonResult> LoadTDSAdjustmentData([FromBody] TDSAdjustmentRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Invalid request." });

                if (request.PartyCode <= 0)
                    return Json(new { success = false, message = "Party is required." });

                DateTime voucherDate;

                if (!DateTime.TryParse(request.VDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out voucherDate))
                {
                    return Json(new { success = false, message = "Invalid voucher date." });
                }

                var gv = _globalVariableService.GetGlobalVariables();
                var rows = new List<TDSAdjustmentRow>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"SELECT x.V_type, x.V_No, CONVERT(varchar(10), x.V_date, 103) AS V_Date, x.AMT, CAST(x.NARRATION AS nvarchar(max)) 
                                    AS NARRATION, SUM(DISTINCT ISNULL(x.Adj_amt, 0)) AS Adj_Amt FROM (
                                    SELECT a.V_type, a.V_No, a.V_date, a.AMT, a.NARRATION, c.Adj_amt FROM LEDGER2 a
                                    LEFT JOIN TDSLedger_OS c ON a.V_TYPE = c.V_type AND a.V_no = c.V_no AND a.DR_CODE = c.AC_CODE AND a.COMP_CODE = c.Comp_code 
                                    AND a.BRANCH_CODE = c.Branch_code
                                    LEFT JOIN SUBGROUP_MAST b ON a.DR_CODE = b.Code AND a.COMP_CODE = b.COMP_CODE
                                    WHERE a.V_TYPE = 'JRNL' AND a.DR_CODE = @DR_CODE AND a.v_date <= @v_date AND ISNULL(a.AMT, 0) - (
                                            SELECT ISNULL(SUM(TDSLEDGER_OS.ADJ_AMT), 0) FROM TDSLEDGER_OS WHERE a.DR_CODE = TDSLEDGER_OS.AC_CODE AND a.V_TYPE = 
                                    TDSLEDGER_OS.V_TYPE AND a.V_NO = TDSLEDGER_OS.V_NO AND a.COMP_CODE = TDSLEDGER_OS.COMP_CODE) > 0 AND a.Comp_code = @Comp_code
                                    UNION ALL
                                    SELECT V_type, V_No, V_date, AMT, NARRATION, Adj_amt FROM TDSLedger_OS WHERE CONCAT(doc_type, Doc_no) = CONCAT(@V_Type, @V_No)) x
                                GROUP BY x.V_type, x.V_No, CONVERT(varchar(10), x.V_date, 103), x.AMT, CAST(x.NARRATION AS nvarchar(max)) 
                                ORDER BY x.V_no";


                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DR_CODE", request.PartyCode);
                        cmd.Parameters.AddWithValue("@v_date", voucherDate);
                        cmd.Parameters.AddWithValue("@Comp_code", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@V_Type", request.VType ?? "");
                        cmd.Parameters.AddWithValue("@V_No", request.VNo);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                var row = new TDSAdjustmentRow();
                                row.VType = Convert.ToString(dr["V_type"]);
                                row.VNo = Convert.ToInt32(dr["V_No"]);
                                row.VDate = dr["V_Date"] == DBNull.Value ? null : Convert.ToDateTime(dr["V_Date"]);
                                row.Amount = dr["AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["AMT"]);
                                row.Narration = Convert.ToString(dr["NARRATION"]);
                                row.AdjustedAmount = dr["Adj_Amt"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Adj_Amt"]);
                                row.BalanceAdjustment = null;
                                rows.Add(row);
                            }
                        }
                    }


                    // Get CR_CODE and CR_NAME
                    foreach (var row in rows)
                    {
                        string crQuery = @"SELECT TOP 1 CR_CODE FROM LEDGER2 WHERE V_TYPE = 'JRNL' AND V_NO = @VNo AND AMT = @Amount AND CR_CODE > 0 AND Comp_code = @CompCode";

                        using (SqlCommand cmd = new SqlCommand(crQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@VNo", row.VNo);
                            cmd.Parameters.AddWithValue("@Amount", row.Amount);
                            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                            object result = cmd.ExecuteScalar();

                            if (result != null && result != DBNull.Value)
                            {
                                row.CRCode = Convert.ToInt32(result);
                                string nameQuery = @"SELECT Name FROM Subgroup_mast WHERE Code = @Code AND Comp_code = @CompCode";
                                using (SqlCommand nameCmd = new SqlCommand(nameQuery, con))
                                {
                                    nameCmd.Parameters.AddWithValue("@Code", row.CRCode);
                                    nameCmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                                    object nameResult = nameCmd.ExecuteScalar();
                                    row.CRName = nameResult == null || nameResult == DBNull.Value ? "" : Convert.ToString(nameResult);
                                }
                            }
                        }
                    }
                }

                string qry = $@"Select Sum(isnull(ADJ_AMT,0)) From TDSLedger_OS Where DOC_TYPE='{request.VType}' and DOC_NO={request.VNo} and 
                                Comp_code={gv.PubCompCode} and Branch_code={gv.PubBranchCode}";

                decimal totalExistingAdjustment = await _dbHelper.GetExecuteScalarAsync<decimal>(qry);

                return Json(new
                {
                    success = true,
                    data = rows,
                    totalRecords = rows.Count,
                    totalExistingAdjustment = totalExistingAdjustment
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveTDSAdjustment([FromBody] TDSAdjustmentSaveRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            if (request.Rows == null || request.Rows.Count == 0)
            {
                return Json(new { success = false, message = "No records found." });
            }

            SqlTransaction transaction = null;
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {

                DateTime voucherDate;

                if (!DateTime.TryParse(request.VDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out voucherDate))
                {
                    return Json(new { success = false, message = "Invalid voucher date." });
                }

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    transaction = con.BeginTransaction();

                    // -------------------------------------------------
                    // DELETE OLD RECORDS
                    // -------------------------------------------------

                    string deleteQuery = @"DELETE FROM TDSLEDGER_OS WHERE Comp_code = @Comp_code AND DOC_TYPE = @DOC_TYPE AND DOC_NO = @DOC_NO AND AC_CODE = @AC_CODE";
                    using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, con, transaction))
                    {
                        deleteCmd.Parameters.AddWithValue("@Comp_code", gv.PubCompCode);
                        deleteCmd.Parameters.AddWithValue("@DOC_TYPE", request.VType.Trim());
                        deleteCmd.Parameters.AddWithValue("@DOC_NO", request.VNo);
                        deleteCmd.Parameters.AddWithValue("@AC_CODE", request.PartyCode);

                        deleteCmd.ExecuteNonQuery();
                    }


                    // -------------------------------------------------
                    // INSERT
                    // -------------------------------------------------

                    string insertQuery = @"INSERT INTO TDSLEDGER_OS (COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, V_DATE, AC_CODE, DOC_TYPE, DOC_NO, DOC_DATE, AMT,
                                            ADJ_AMT, NARRATION, SNO, UUSER, UDATE, AED, WSID, LIP, LID)
                                            VALUES (@COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @V_DATE, @AC_CODE, @DOC_TYPE, @DOC_NO, @DOC_DATE, @AMT, @ADJ_AMT,
                                            @NARRATION, @SNO, @UUSER, GETDATE(), 'A', @WSID, @LIP, @LID)";


                    for (int i = 0; i < request.Rows.Count; i++)
                    {
                        TDSAdjustmentRow row = request.Rows[i];

                        decimal adjusted = row.AdjustedAmount;
                        decimal? balanceAdjustment = row.BalanceAdjustment;
                        decimal? totalAdjustment = adjusted + balanceAdjustment;

                        if (balanceAdjustment > 0)
                        {
                            if (totalAdjustment > row.Amount)
                            {
                                transaction.Rollback();
                                return Json(new { success = false, message = $"Adjusted + Bal. Adj. Amount greater than Amount of VNo => {row.VNo} at Row Number => {i + 1}" });
                            }


                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, con, transaction))
                            {
                                insertCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                insertCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                insertCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                insertCmd.Parameters.AddWithValue("@V_TYPE", row.VType ?? "");
                                insertCmd.Parameters.AddWithValue("@V_NO", row.VNo);
                                insertCmd.Parameters.AddWithValue("@V_DATE", row.VDate ?? (object)DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@AC_CODE", request.PartyCode);
                                insertCmd.Parameters.AddWithValue("@DOC_TYPE", request.VType.Trim() ?? (object)DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@DOC_NO", request.VNo);
                                insertCmd.Parameters.AddWithValue("@DOC_DATE", voucherDate);
                                insertCmd.Parameters.AddWithValue("@AMT", row.Amount);
                                insertCmd.Parameters.AddWithValue("@ADJ_AMT", balanceAdjustment ?? (object)DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@NARRATION", row.Narration);
                                insertCmd.Parameters.AddWithValue("@SNO", i + 1);
                                insertCmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                insertCmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                insertCmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                insertCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    transaction.Commit();
                }

                return Json(new { success = true, message = "Data Updated." });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
