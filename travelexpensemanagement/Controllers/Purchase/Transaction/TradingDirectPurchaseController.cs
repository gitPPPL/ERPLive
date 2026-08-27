using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class TradingDirectPurchaseController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly ITradingDirectPurchaseRepository _tradingDirectPurchase;

        public TradingDirectPurchaseController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DbHelper dbHelper, GlobalValidationdate globalValidationdate, ITradingDirectPurchaseRepository tradingDirectPurchase)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _tradingDirectPurchase = tradingDirectPurchase;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/TradingDirectPurchase/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetList(string type, string? vType = null, int shipFromCode = 0, int cCode = 0)
        {
            var query = BuildListQuery(type, vType, shipFromCode, cCode);

            if (query == null)
                return BadRequest("Invalid list type");

            var data = await _dbHelper.GetJsonDataAsync(query);

            return Json(new { success = true, data });
        }
        private string BuildListQuery(string type, string vType, int shipFromCode, int cCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            return type switch
            {
                "doctype" => "SELECT CODE as Value, NAME as Text FROM DOCTYPE_MAST WHERE DOCTYPE in ('TradingPurchase') ORDER BY NAME",

                "party" => $@"select Code as Value, Name as Text, ADD1, ADD2, CITY_CODE, GSTIN, PINCODE 
                                from SUBGROUP_MAST where NATURE in ('Supplier') and COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by name",

                "drcrbyvtype" => $@"select code as Value, name as Text ,ADD1, ADD2, ADD3, CITY_CODE, GSTIN from SUBGROUP_MAST where NATURE='Others' and COMP_CODE={gv.PubCompCode}
                                    and ACTIVE=1 order by name",

                "drcr" => $@"select a.code as Value, a.name as Text, a.ADD1, a.ADD2, a.CITY_CODE, a.GSTIN 
                            from SUBGROUP_MAST a where a.COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by name",

                "item" => $@"Select a.CODE as Value, a.name as Text, c.NAME as unit, c.CODE as ucode from item_mast a 
                            left join ITEM_MAKE b on a.code=b.ITEM_CODE and b.COMP_CODE=a.COMP_CODE
                            left join ITEMUNIT_MAST c on a.UNIT_CODE=c.CODE and c.comp_code=a.COMP_CODE
                            left join item_group d on a.GROUP_CODE=d.CODE and d.COMP_CODE=a.COMP_CODE
                            left join ITEM_MGROUP e on d.MGROUP_CODE=e.CODE and e.COMP_CODE=a.COMP_CODE
                            where a.comp_code={gv.PubCompCode} 
                            --and e.MGROUP_TYPE in('Store','Raw')
                            group by a.name ,a.CODE , c.NAME ,c.CODE order by a.name",

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
        public async Task<IActionResult> GetMrnNoList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"Select a.V_type as vType, a.v_no as Value, concat(a.V_TYPE,a.V_NO) as Text from Order1 a where a.V_type='RORD' and COMP_CODE={gv.PubCompCode} and YEAR_CODE={gv.PubFYearCode}
                            and BRANCH_CODE={gv.PubBranchCode}";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }


        public IActionResult GetAddressByBillToParty(int code, int addressId)
        {
            try
            {
                var addressDetails = _tradingDirectPurchase.GetAddByParty(code, addressId);
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
                var vtypeFromQuery = Request.Query["vtype"].ToString();

                var result = await _tradingDirectPurchase.GetFullQuotationByVno(vNo, vtype);
                if (result.data != null)
                {
                    return Json(new
                    {
                        success = result.status,
                        header = result.data.Header,
                        items = result.data.Items,
                        attachments = result.data.Attachments,
                        eprAttachments = result.data.EprAttachments
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
                var result = await _tradingDirectPurchase.SavePurchaseBillPassEntry(data);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //==============================================================MRN Change=======================================================
        [HttpPost]
        public IActionResult ValidateMRN(string mrnTypeNo, string vType, int vNo)
        {
            try
            {
                var result = _tradingDirectPurchase.ValidateMRN(mrnTypeNo, vType, vNo);
                if (!result.status)
                {
                    return Json(new { success = false, message = result.message });
                }
                return Json(new { success = true, mrnNo = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseDetailsByMRN(string vType, int vNo)
        {
            try
            {
                var model = await _tradingDirectPurchase.GetPurchaseDetailsByMRN(vType, vNo);
                if (model == null)
                {
                    return Json(new { success = true, message = "Purchase details not found.", });
                }

                return Json(new { success = true, message = "Purchase details retrieved successfully.", data = model });

            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = "A database error occurred while retrieving purchase details.", error = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An unexpected error occurred.", error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetPurchaseItemsByMRN(string vType, int vNo)
        {
            try
            {
                var result = _tradingDirectPurchase.GetPurchaseItemsByMRN(vType, vNo);
                return new JsonResult(new { success = result.status, message = result.message, data = result.data });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message, data = new List<PurchaseItemDto>() });
            }
        }

        [HttpGet]
        public JsonResult GetItemOrderRatesByPO(string poType, int poNo, int itemCode)
        {
            try
            {
                bool exists = false;
                decimal landRate = 0;
                decimal rate = 0;
                var result = _tradingDirectPurchase.GetItemOrderRatesByPO(poType, poNo, itemCode);
                if (result.LandRate >= 0 && result.Rate >= 0)
                {
                    return Json(new { success = true, exists, landRate, rate });
                }
                return Json(new { success = true, message = "Rates not found!" });
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
                var result = await _tradingDirectPurchase.GetPackOnBasic(code);
                return Json(new { success = result.status, packOnBasic = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //--------------- Calc Frieght ---------------------
        [HttpPost]
        public async Task<IActionResult> CalculateFrieght([FromBody] DebitNoteRequest request)
        {
            try
            {
                var result = await _tradingDirectPurchase.CalculateFrieghtPay(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
        //--------------- DR/CR NOTE ---------------------
        [HttpPost]
        public async Task<IActionResult> CalculateDebitNote([FromBody] DebitNoteRequest request)
        {
            try
            {
                var result =
                await _tradingDirectPurchase.CalculateDebitNote(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        //--------------- Get Existing TDS ---------------------
        [HttpPost]
        public async Task<IActionResult> CheckExistingTDS(string billNo, int drCode)
        {
            try
            {
                var result = await _tradingDirectPurchase.CheckExistingTDS(billNo, drCode);
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
                var result = await _tradingDirectPurchase.GetFrtCrAcByTransCodeAsync(transportCode);
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
        public async Task<JsonResult> GetPurchaseOrSaleVoucherNo(string transportName, string grNo, string currentVoucher, string purchaseOrSale)
        {
            try
            {
                var result = await _tradingDirectPurchase.GetPurchaseOrSaleVoucherNo(transportName, grNo, currentVoucher, purchaseOrSale);
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
                var result = await _tradingDirectPurchase.CheckPaymentExists(docType, docNo);
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
                var result = await _tradingDirectPurchase.CheckDuplicateBill(partyCode, billNo, currentVNo);
                return Json(new { success = true, exists = result.Exists, docId = result.DocId, vDate = result.VDate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ValidateTaxType(int cityCode, decimal totalIGST, decimal totalCGST, decimal totalSGST)
        {
            try
            {
                var result = await _tradingDirectPurchase.ValidateTaxType(cityCode, totalIGST, totalCGST, totalSGST);
                return Json(new { success = result.status, isValid = result.data, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ValidatePurchaseRow(string vType, int itemCode, string itemName, string billHsnCode, decimal qty,
        decimal freightAmount, string poType, int poNo, string mrnType, int mrnNo)
        {
            try
            {
                var result = await _tradingDirectPurchase.ValidatePurchaseRow(vType, itemCode, itemName, billHsnCode, qty,
                    freightAmount, poType, poNo, mrnType, mrnNo);

                return Json(new { success = true, result });
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
                var result = await _tradingDirectPurchase.ValidatePartyGst(gstType, partyCode, gstNo);
                return Json(new { success = true, result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}
