using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseBillPassEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IPurchaseBillPassEntryRepository _purchaseBillPassEntry;

        public PurchaseBillPassEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService
            dropdownService, DbHelper dbHelper, GlobalValidationdate globalValidationdate, IPurchaseBillPassEntryRepository purchaseBillPassEntry)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _purchaseBillPassEntry = purchaseBillPassEntry;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PurchaseBillPassEntry/Index.cshtml");
        }

        //============VType========================
        public IActionResult GetDocTypeList()
        {
            string query = "SELECT CODE,NAME FROM DOCTYPE_MAST WHERE DOCTYPE in ('PurchaseInvoice','Service Order') ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        //============VNO========================
        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "PURCHASE1");
            return Json(new { status = true, V_NO = result });
        }

        //============Latest Debit Acount ========================
        [HttpGet]
        public async Task<IActionResult> GetLatestDebitAccount(string vType)
        {
            try
            {
                var result = await _purchaseBillPassEntry.GetLatestDebitAccount(vType);
                return Json(new { success = true, debitAc = result.DebitAc, debitAcName = result.DebitAcName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        //============MRN List========================
        [HttpGet]
        public async Task<IActionResult> GetMrnNoList(string vType)
        {
            // Determine the MRN type based on vType
            string mrntype = "";

            switch (vType)
            {
                case "BFPB":
                    mrntype = "BFRC";
                    break;
                case "RIMP":
                    mrntype = "RCPI";
                    break;
                case "RMPB":
                    mrntype = "RCPT";
                    break;
                case "STPB":
                    mrntype = "SRPU";
                    break;
                case "STJW":
                    mrntype = "SRJW";
                    break;
                default:
                    return Json(new List<object>());
            }

            var gv = _globalVariableService.GetGlobalVariables();

            string query = $@"SELECT V_NO as Value, DOC_ID as Text, V_TYPE as vType FROM PURCHASE1 WHERE COMP_CODE= {gv.PubCompCode} AND BRANCH_CODE = {gv.PubBranchCode} AND YEAR_CODE ={gv.PubFYearCode} 
                            AND V_TYPE = '{mrntype}' order by V_NO";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //============Party List With Supplier Nature========================
        [HttpGet]
        public async Task<IActionResult> GetPartyListNatureSupplier(string vType)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = $@"select Code as Value, Name as Text, ADD1, ADD2, CITY_CODE, GSTIN, PINCODE 
                                from SUBGROUP_MAST where NATURE in ('Supplier') and COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by name";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //============Dr Ac List By Vtype========================
        [HttpGet]
        public async Task<IActionResult> GetDrAcListByVtype(string vType)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = $@"select SUBGROUP_MAST.code as Value, SUBGROUP_MAST.name as Text, SUBGROUP_MAST.ADD1, SUBGROUP_MAST.ADD2, 
                                SUBGROUP_MAST.CITY_CODE, SUBGROUP_MAST.GSTIN 
                                from SUBGROUP_MAST 
                                LEFT JOIN DOC_GLMAST ON DOC_GLMAST.COMP_CODE = SUBGROUP_MAST.COMP_CODE 
                                AND DOC_GLMAST.AC_CODE=SUBGROUP_MAST.CODE 
                                where SUBGROUP_MAST.NATURE='Others' and SUBGROUP_MAST.COMP_CODE={gv.PubCompCode} and SUBGROUP_MAST.ACTIVE=1
                                AND DOC_GLMAST.DOC_CODE='{vType}' order by name";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //============Party Dr Cr Ac List========================
        [HttpGet]
        public async Task<IActionResult> GetPartyDrCrAcList()
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = $@"select a.code as Value, a.name as Text, a.ADD1, a.ADD2, a.CITY_CODE, a.GSTIN 
                            from SUBGROUP_MAST a where a.COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by name";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //============Transport GST List========================
        [HttpGet]
        public async Task<IActionResult> GetTranGSTByFrtCrAc(int? frtCrAcCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = "";
            if (frtCrAcCode != null && frtCrAcCode > 0)
            {
                query += $@"Select a.GSTIN as Value, a.GSTIN as Text from SUBGROUP_ADDRESS a 
                left join TRANSPORT_MAST b on a.CODE=b.PARTY_CODE and a.COMP_CODE=b.COMP_CODE 
                where a.COMP_CODE={gv.PubCompCode} and a.CODE={frtCrAcCode} group by a.GSTIN order by a.GSTIN";
            }
            else
            {
                query += $@"Select 'URP' as Value, 'URP' as Text";
            }
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //============Item List========================
        [HttpGet]
        public async Task<IActionResult> GetItemList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"Select a.CODE as Value, a.name as Text, c.NAME as unit, c.CODE as ucode from item_mast a 
                            left join ITEM_MAKE b on a.code=b.ITEM_CODE and b.COMP_CODE=a.COMP_CODE
                            left join ITEMUNIT_MAST c on a.UNIT_CODE=c.CODE and c.comp_code=a.COMP_CODE
                            left join item_group d on a.GROUP_CODE=d.CODE and d.COMP_CODE=a.COMP_CODE
                            left join ITEM_MGROUP e on d.MGROUP_CODE=e.CODE and e.COMP_CODE=a.COMP_CODE
                            where a.comp_code={gv.PubCompCode} 
                            --and e.MGROUP_TYPE in('Store','Raw')
                            group by a.name ,a.CODE , c.NAME ,c.CODE order by a.name";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //============Add List========================
        [HttpGet]
        public async Task<IActionResult> GetAddList(int shipFromCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"select address_id Value, add1 Text from SUBGROUP_ADDRESS 
                                where code={shipFromCode} and COMP_CODE={gv.PubCompCode} order by ADDRESS_ID";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //================Department List=================
        [HttpGet]
        public async Task<IActionResult> GetDepartmentList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"select name as Text,code as Value from ITEMDEPT_MAST where COMP_CODE={gv.PubCompCode} order by name";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //================City List=================
        [HttpGet]
        public async Task<IActionResult> GetCityList()
        {
            string query = $@"select code as Value, NAME as Text from CITY_MAST where ACTIVE=1 order by Name";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //================State List=================
        [HttpGet]
        public async Task<IActionResult> GetStateList(int cCode)
        {
            string query = $@"select Top 1 b.CODE as Value, b.NAME as Text from CITY_MAST a
                            left join STATE_MAST b on a.STATE_CODE = b.CODE
                            where a.code = {cCode}";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //================Currency List=================
        [HttpGet]
        public async Task<IActionResult> GetCurrencyList()
        {
            string query = $@"SELECT CODE as Value, SHORTNAME as Text FROM CURRENCY_MAST ORDER BY code";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //==================Tax Type List==================
        [HttpGet]
        public async Task<IActionResult> GetTaxList()
        {
            string query = $@"select name as Text, code as Value, CGST_PER,SGST_PER,IGST_PER,isnull(VAT_PER,0)VAT_PER,TDS_PER,TCS_PER,OTH_PER,
                            isnull(OTH_PER2,0)OTH_PER2 from TAX_MAST
                            where ACTIVE = 1 order by name";
            var moduelList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = moduelList });
        }

        //================Status List=================
        public IActionResult GetStatusList()
        {
            string query = "SELECT CODE,NAME FROM DOCSTATUS_MAST WHERE V_TYPE = 'Document' ORDER BY CODE";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult GetTransportList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"select code, ltrim(name) from TRANSPORT_MAST 
                                where COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by ltrim(name)";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetAddressByBillToParty(int code, int addressId)
        {
            var addressDetails = new
            {
                add1 = "",
                add2 = "",
                add3 = "",
                pincode = "",
                gstin = "",
                cityCode = ""
            };

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection connection = _dbConnection.GetErpConnection())
                {
                    //using (SqlCommand cmd = new SqlCommand("Select top(1) ADD1,ADD2,ADD3,PINCODE,GSTIN,CITY_CODE from SUBGROUP_ADDRESS where COMP_CODE = @COMP_CODE AND Code = @PCODE", connection))
                    using (SqlCommand cmd = new SqlCommand(
                        $@"Select a.Add1,a.Add2,a.Add3,a.GSTIN,a.City_Code,b.Name State,c.name City,a.Pincode,a.Distance 
                            from Subgroup_Address a 
                            left join STATE_MAST b on a.STATE_CODE=b.code 
                            left join CITY_MAST c on a.CITY_CODE=c.code 
                            where a.comp_code=@COMP_CODE and a.Code=@CODE and a.Address_Id=@Address_Id",
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@Address_Id", addressId);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                addressDetails = new
                                {
                                    add1 = reader["ADD1"].ToString(),
                                    add2 = reader["ADD2"].ToString(),
                                    add3 = reader["ADD3"].ToString(),
                                    pincode = reader["PINCODE"].ToString(),
                                    gstin = reader["GSTIN"].ToString(),
                                    cityCode = reader["CITY_CODE"].ToString()
                                };

                            }
                        }
                    }
                }
                return Json(new { success = true, addressDetails });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error retrieving the address by specfic address id" });
            }
        }

        public IActionResult GetDepartmentList(int cCode)
        {
            string query = "SELECT CODE,NAME FROM DEPT_MAST WHERE ACTIVE=1 AND COMP_CODE = '" + cCode + "'  ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult GetItemMakeList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM ITEMMAKE_MAST WHERE COMP_CODE = '" + compCode + "' AND ACTIVE=1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);

            return Json(moduelList);
        }

        [HttpGet]
        public IActionResult GetMakeItemsByItemCode(int itemCode)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var makeItems = new List<object>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"
                SELECT DISTINCT IMK.MAKE_CODE, IMM.NAME 
                FROM ITEM_MAKE IMK
                LEFT JOIN ITEMMAKE_MAST IMM ON IMM.CODE = IMK.MAKE_CODE
                WHERE IMM.COMP_CODE = @COMP_CODE  
                AND IMK.ITEM_CODE = @ITEM_CODE
                ORDER BY IMM.NAME";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                makeItems.Add(new
                                {
                                    MakeCode = reader["MAKE_CODE"],
                                    Name = reader["NAME"]
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, data = makeItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving make items", error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetHsnAndUnitByItemCode(int iCode)
        {
            string hsn = "";
            string unitCode = "";

            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = "SELECT HSN_CODE, UNIT_CODE,ITEM_TYPE FROM ITEM_MAST WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@CODE", iCode);

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                hsn = reader["HSN_CODE"]?.ToString();
                                unitCode = reader["UNIT_CODE"]?.ToString();
                            }
                        }
                    }
                }

                return Json(new { success = true, hsn, unitCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error getting HSN and UNIT_CODE", error = ex.Message });
            }
        }

        public IActionResult GetTextRelatedDetailsTaxCode(int taxCode)
        {
            var taxDetails = new
            {
                TaxType = "",
                CgstPer = "",
                SgstPer = "",
                IgstPer = "",
                TdsPer = "",
                TcsPer = "",
                VatPer = "",
                OthPer = ""
            };

            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("Select TAX_TYPE,CGST_PER,SGST_PER,IGST_PER,TDS_PER,TCS_PER,VAT_PER,OTH_PER from TAX_MAST WHERE CODE = @taxCode", con))
                    {
                        cmd.Parameters.AddWithValue("@taxCode", taxCode);

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                taxDetails = new
                                {
                                    TaxType = reader["TAX_TYPE"]?.ToString(),
                                    CgstPer = reader["CGST_PER"]?.ToString(),
                                    SgstPer = reader["SGST_PER"]?.ToString(),
                                    IgstPer = reader["IGST_PER"]?.ToString(),
                                    TdsPer = reader["TDS_PER"]?.ToString(),
                                    TcsPer = reader["TCS_PER"]?.ToString(),
                                    VatPer = reader["VAT_PER"]?.ToString(),
                                    OthPer = reader["OTH_PER"]?.ToString()
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, taxDetails });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving next ID", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFullQuotationByVno(int vNo, string vType)
        {
            try
            {
                var result = await _purchaseBillPassEntry.GetFullQuotationByVno(vNo, vType);
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
                var result = await _purchaseBillPassEntry.SavePurchaseBillPassEntry(data);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeletePurchaseBillPassByCode(int code, string vType, int compCode, int branchCode, int yearCode)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETEALL");
                        cmd.Parameters.AddWithValue("@SubAction", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", branchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", yearCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Purchase bill pass entry deleted successfully." });
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
            var gv = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                //-------------------------------------------------------
                //Check Duplicate
                //-------------------------------------------------------

                string duplicateQuery = @"
                    Select distinct V_TYPE+cast(V_NO as varchar) from PURCHASE2 
                    where V_TYPE=@V_TYPE and V_NO<>@V_NO
                    and REF_TYPE+cast(REF_NO as varchar)=@REF_TYPE_REF_NO 
                    and COMP_CODE=@COMP_CODE and BRANCH_CODE=@BRANCH_CODE and YEAR_CODE=@YEAR_CODE";

                SqlCommand cmd = new SqlCommand(duplicateQuery, con);

                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@REF_TYPE_REF_NO", mrnTypeNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                var billPassNo = cmd.ExecuteScalar();

                if (billPassNo != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "MRN No exists in Purchase Bill Pass Entry No : " + billPassNo
                    });
                }

                //-------------------------------------------------------
                //Check Approval
                //-------------------------------------------------------
                string MRNtype = mrnTypeNo.Substring(0, 4);
                string MRNo = mrnTypeNo.Substring(4);
                string approveQuery = @"
                    SELECT FAPROV_STATUS
                    FROM Purchase1
                    WHERE V_TYPE=@MRNType
                    AND V_NO=@MRNNo
                    AND COMP_CODE=@COMP_CODE
                    AND BRANCH_CODE=@BRANCH_CODE";

                SqlCommand cmd2 = new SqlCommand(approveQuery, con);

                cmd2.Parameters.AddWithValue("@MRNType", MRNtype);
                cmd2.Parameters.AddWithValue("@MRNNo", MRNo);
                cmd2.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd2.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                var status = Convert.ToString(cmd2.ExecuteScalar());

                if (status != null && status.ToUpper() != "APPROVED")
                {
                    return Json(new
                    {
                        success = false,
                        message = "MRN No " + mrnTypeNo + " not approved."
                    });
                }

                return Json(new
                {
                    success = true,
                    mrnNo = MRNo,
                });
            }
        }

        [HttpGet]
        public IActionResult GetPurchaseDetailsByMRN(string vType, int vNo)
        {
            try
            {
                PurchaseDetailsDto model = new PurchaseDetailsDto();

                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // 1. Check Purchase Exists
                    string purchaseSql = @"SELECT V_No
                                   FROM PURCHASE1
                                   WHERE V_TYPE=@V_TYPE
                                   AND V_NO=@V_NO
                                   AND COMP_CODE=@COMP_CODE
                                   AND BRANCH_CODE=@BRANCH_CODE
                                   AND YEAR_CODE=@YEAR_CODE";

                    using (SqlCommand cmd = new SqlCommand(purchaseSql, con))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        object obj = cmd.ExecuteScalar();

                        if (obj == null)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Purchase not found."
                            });
                        }

                        model.V_No = Convert.ToInt32(obj);
                    }

                    // 2. Deduction Details
                    string deductionSql = @"SELECT DEDUCT_AMT, DEDUCT_NARR
                                    FROM QC1
                                    WHERE MRN_TYPE=@V_TYPE
                                    AND MRN_NO=@V_NO";

                    using (SqlCommand cmd = new SqlCommand(deductionSql, con))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                model.DeductAmt = dr["DEDUCT_AMT"] == DBNull.Value
                                    ? 0
                                    : Convert.ToDecimal(dr["DEDUCT_AMT"]);

                                model.DeductNarr = dr["DEDUCT_NARR"]?.ToString();
                            }
                        }
                    }

                    // 3. Purchase Header Details
                    string headerSql = @"SELECT
                                    a.BILL_NO,a.BILL_DATE,a.CHALL_NO,a.CHALL_DATE,
                                    a.WAYBILL_NO,a.TRANSIT_NO,a.EXCH_RATE,
                                    a.PARTY_CODE,b.NAME Party,
                                    a.BILL_ADD1,a.BILL_ADD2,a.BILL_ADD3,
                                    a.BILL_CITY,a.BILL_GST,a.BILL_PINCODE,
                                    b1.STATE_CODE as BILL_STATE,
                                    a.SHIP_CODE,c.NAME ShipTo,
                                    a.SHIP_ADD1,a.SHIP_ADD2,a.SHIP_ADD3,
                                    a.SHIP_CITY,a.SHIP_GST,a.SHIP_PINCODE,
                                    s1.STATE_CODE as SHIP_STATE,
                                    a.REMARKS,
                                    ISNULL(a.TRANSPORT_CODE,0) TRANSPORT_CODE,
                                    d.NAME Transport,
                                    a.TRANSPORT_NAME,a.TRUCK_NO,a.CONTAINER_NO,
                                    a.GR_NO,a.GR_DATE,
                                    a.FRTPAY_AMT,a.FRTPAY_TAXPER,a.FRTPAY_TAX,
                                    a.FRTPAY_NAR,
                                    a.HOLD_PAY,a.HOLD_REASON,a.HOLD_DATE,
                                    a.TCS_PER,a.TCS_AMT,
                                    a.EWB_DATE,a.EWB_EXPDATE,
                                    a.EWB_INVNO
                                    --,b.Einv_Party
                                FROM PURCHASE1 a
                                LEFT JOIN SUBGROUP_MAST b
                                    ON a.PARTY_CODE=b.CODE
                                    AND b.COMP_CODE=@COMP_CODE
                                LEFT JOIN SUBGROUP_MAST c
                                    ON a.SHIP_CODE=c.CODE
                                    AND c.COMP_CODE=@COMP_CODE
                                LEFT JOIN TRANSPORT_MAST d
                                    ON a.TRANSPORT_CODE=d.CODE
                                    AND d.COMP_CODE=@COMP_CODE
                                LEFT JOIN CITY_MAST b1 
                                    ON b1.CODE = a.BILL_CITY
                                LEFT JOIN CITY_MAST s1 
                                    ON s1.CODE = a.SHIP_CITY
                                WHERE a.V_TYPE=@V_TYPE
                                    AND a.V_NO=@V_NO
                                    AND a.COMP_CODE=@COMP_CODE
                                    AND a.BRANCH_CODE=@BRANCH_CODE
                                    AND a.YEAR_CODE=@YEAR_CODE";

                    using (SqlCommand cmd = new SqlCommand(headerSql, con))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                model.BILL_NO = dr["BILL_NO"]?.ToString();
                                model.BILL_DATE = dr["BILL_DATE"] as DateTime?;
                                model.CHALL_NO = dr["CHALL_NO"]?.ToString();
                                model.CHALL_DATE = dr["CHALL_DATE"] as DateTime?;
                                model.WAYBILL_NO = dr["WAYBILL_NO"]?.ToString();
                                model.TRANSIT_NO = dr["TRANSIT_NO"]?.ToString();
                                model.EXCH_RATE = dr["EXCH_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["EXCH_RATE"]);

                                model.PARTY_CODE = dr["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PARTY_CODE"]);
                                model.Party = dr["Party"]?.ToString();

                                model.BILL_ADD1 = dr["BILL_ADD1"]?.ToString();
                                model.BILL_ADD2 = dr["BILL_ADD2"]?.ToString();
                                model.BILL_ADD3 = dr["BILL_ADD3"]?.ToString();
                                model.BILL_CITY = dr["BILL_CITY"]?.ToString();
                                model.BILL_GST = dr["BILL_GST"]?.ToString();
                                model.BILL_PINCODE = dr["BILL_PINCODE"]?.ToString();
                                model.BILL_STATE = dr["BILL_STATE"]?.ToString();

                                model.SHIP_CODE = dr["SHIP_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["SHIP_CODE"]);
                                model.ShipTo = dr["ShipTo"]?.ToString();

                                model.SHIP_ADD1 = dr["SHIP_ADD1"]?.ToString();
                                model.SHIP_ADD2 = dr["SHIP_ADD2"]?.ToString();
                                model.SHIP_ADD3 = dr["SHIP_ADD3"]?.ToString();
                                model.SHIP_CITY = dr["SHIP_CITY"]?.ToString();
                                model.SHIP_GST = dr["SHIP_GST"]?.ToString();
                                model.SHIP_PINCODE = dr["SHIP_PINCODE"]?.ToString();
                                model.SHIP_STATE = dr["SHIP_STATE"]?.ToString();

                                model.REMARKS = dr["REMARKS"]?.ToString();
                                model.TRANSPORT_CODE = Convert.ToInt32(dr["TRANSPORT_CODE"]);
                                model.Transport = dr["Transport"]?.ToString();
                                model.TRANSPORT_NAME = dr["TRANSPORT_NAME"]?.ToString();
                                model.TRUCK_NO = dr["TRUCK_NO"]?.ToString();
                                model.CONTAINER_NO = dr["CONTAINER_NO"]?.ToString();
                                model.GR_NO = dr["GR_NO"]?.ToString();
                                model.GR_DATE = dr["GR_DATE"] as DateTime?;
                                model.FRTPAY_AMT = dr["FRTPAY_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["FRTPAY_AMT"]);
                                model.FRTPAY_TAXPER = dr["FRTPAY_TAXPER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["FRTPAY_TAXPER"]);
                                model.FRTPAY_TAX = dr["FRTPAY_TAX"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["FRTPAY_TAX"]);
                                model.FRTPAY_NAR = dr["FRTPAY_NAR"]?.ToString();
                                model.HOLD_REASON = dr["HOLD_REASON"]?.ToString();
                                model.TCS_PER = dr["TCS_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["TCS_PER"]);
                                model.TCS_AMT = dr["TCS_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["TCS_AMT"]);
                                model.EWB_INVNO = dr["EWB_INVNO"]?.ToString();
                                model.EWB_DATE = dr["EWB_DATE"] as DateTime?;
                                model.EWB_EXPDATE = dr["EWB_EXPDATE"] as DateTime?;
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Purchase details retrieved successfully.",
                    data = model
                });

            }
            catch (SqlException ex)
            {
                return Json(new
                {
                    success = false,
                    message = "A database error occurred while retrieving purchase details.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetPurchaseItemsByMRN(string vType, int vNo)
        {
            List<PurchaseItemDto> items = new List<PurchaseItemDto>();
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"SELECT
                            a.ITEM_CODE,
                            a.ITEM_NAME,
                            ISNULL(a.Uom_name,b.NAME) AS Unit,
                            ISNULL(a.HSN_CODE,'') AS HSN_CODE,
                            a.NOS,
                            a.RECD_QTY,
                            a.BILL_QTY,
                            a.USD_RATE,
                            a.EXCH_RATE,
                            a.RATE,
                            a.PACK_PER,
                            a.PACK_AMT,
                            a.DISC_PER,
                            a.DISC_AMT,
                            ISNULL(d.NAME,'') AS TaxType,
                            a.CGST_PER,
                            a.SGST_PER,
                            a.IGST_PER,
                            a.VAT_PER,
                            a.OTH_AMT,
                            a.PO_TYPE,
                            a.PO_NO,
                            a.V_TYPE as REF_TYPE, a.v_no as REF_NO,
                            ISNULL(a.REQ_TYPE,'') AS REQ_TYPE,
                            a.REQ_NO,
                            ISNULL(a.KANTA_TYPE,'') AS KANTA_TYPE,
                            a.KANTA_NO,
                            ISNULL(f.NAME,'') AS Make,
                            ISNULL(e.NAME,'') AS Department,
                            a.DEPT_CODE,
                            a.TAX_CODE,
                            a.MAKE_CODE AS MAKE_CODE,
                            a.UOM_CODE
                        FROM PURCHASE2 a
                        LEFT JOIN ITEMUNIT_MAST b
                            ON a.UOM_CODE=b.CODE AND b.COMP_CODE=@COMP_CODE
                        LEFT JOIN TAX_MAST d
                            ON a.TAX_CODE=d.CODE
                        LEFT JOIN ITEMDEPT_MAST e
                            ON a.DEPT_CODE=e.CODE AND e.COMP_CODE=@COMP_CODE
                        LEFT JOIN ITEMMAKE_MAST f
                            ON a.MAKE_CODE=f.CODE AND f.COMP_CODE=@COMP_CODE
                        WHERE
                            a.V_TYPE=@V_TYPE
                            AND a.V_NO=@V_NO
                            AND a.COMP_CODE=@COMP_CODE
                            AND a.BRANCH_CODE=@BRANCH_CODE
                            AND a.YEAR_CODE=@YEAR_CODE
                        ORDER BY a.SNO";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                items.Add(new PurchaseItemDto
                                {
                                    ITEM_CODE = dr["ITEM_CODE"].ToString(),
                                    ITEM_NAME = dr["ITEM_NAME"].ToString(),
                                    Unit = dr["Unit"].ToString(),
                                    HSN_CODE = dr["HSN_CODE"].ToString(),
                                    NOS = dr["NOS"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["NOS"]),
                                    RECD_QTY = dr["RECD_QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["RECD_QTY"]),
                                    BILL_QTY = dr["BILL_QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["BILL_QTY"]),
                                    USD_RATE = dr["USD_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["USD_RATE"]),
                                    EXCH_RATE = dr["EXCH_RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["EXCH_RATE"]),
                                    RATE = dr["RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["RATE"]),
                                    PACK_PER = dr["PACK_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["PACK_PER"]),
                                    PACK_AMT = dr["PACK_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["PACK_AMT"]),
                                    DISC_PER = dr["DISC_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DISC_PER"]),
                                    DISC_AMT = dr["DISC_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DISC_AMT"]),
                                    TaxType = dr["TaxType"].ToString(),
                                    CGST_PER = dr["CGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["CGST_PER"]),
                                    SGST_PER = dr["SGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["SGST_PER"]),
                                    IGST_PER = dr["IGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["IGST_PER"]),
                                    VAT_PER = dr["VAT_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["VAT_PER"]),
                                    OTH_AMT = dr["OTH_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["OTH_AMT"]),
                                    PO_TYPE = dr["PO_TYPE"].ToString(),
                                    PO_NO = dr["PO_NO"].ToString(),
                                    REF_TYPE = dr["REF_TYPE"].ToString(),
                                    REF_NO = dr["REF_NO"] == DBNull.Value ? 0 : Convert.ToInt32(dr["REF_NO"]),
                                    REQ_TYPE = dr["REQ_TYPE"].ToString(),
                                    REQ_NO = dr["REQ_NO"].ToString(),
                                    KANTA_TYPE = dr["KANTA_TYPE"].ToString(),
                                    KANTA_NO = dr["KANTA_NO"].ToString(),
                                    Make = dr["Make"].ToString(),
                                    Department = dr["Department"].ToString(),
                                    DEPT_CODE = dr["DEPT_CODE"].ToString(),
                                    TAX_CODE = dr["TAX_CODE"].ToString(),
                                    MAKE_CODE = dr["MAKE_CODE"].ToString(),
                                    UOM_CODE = dr["UOM_CODE"].ToString()
                                });
                            }
                        }
                    }
                }

                return new JsonResult(new
                {
                    success = true,
                    message = "Data fetched successfully.",
                    data = items
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message,
                    data = new List<PurchaseItemDto>()
                });
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
                var gv = _globalVariableService.GetGlobalVariables();

                string query = @"
                SELECT LAND_RATE, RATE
                FROM ORDER2
                WHERE V_TYPE = @V_TYPE
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND ITEM_CODE = @ITEM_CODE";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", poType);
                        cmd.Parameters.AddWithValue("@V_NO", poNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                exists = true;
                                landRate = dr["LAND_RATE"] != DBNull.Value ? Convert.ToDecimal(dr["LAND_RATE"]) : 0;
                                rate = dr["RATE"] != DBNull.Value ? Convert.ToDecimal(dr["RATE"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, exists, landRate, rate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetPackOnBasic(int code)
        {
            try
            {
                int packOnBasic = 0;

                string query = @"
                    SELECT PACK_ONBASIC 
                    FROM TAX_MAST 
                    WHERE code = @code AND Active = 1";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@code", code);

                        con.Open();

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            packOnBasic = Convert.ToInt32(result);
                        }
                    }
                }

                return Json(new { success = true, data = packOnBasic });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetHsnCodeAndQty(int itemCode, string poType, int poNo)
        {
            try
            {
                int hsnCode = 0;
                decimal qty = 0;
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Get HSN_CODE
                    string hsnQuery = @"
                    SELECT ISNULL(HSN_CODE, 0)
                    FROM ITEM_MAST
                    WHERE Code = @ItemCode
                      AND Comp_code = @Comp_code";

                    using (SqlCommand cmd = new SqlCommand(hsnQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                        cmd.Parameters.AddWithValue("@Comp_code", gv.PubCompCode);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            hsnCode = Convert.ToInt32(result);
                        }
                    }

                    // Get QTY
                    string qtyQuery = @"
                    SELECT ISNULL(QTY, 0)
                    FROM ORDER2
                    WHERE Item_Code = @ItemCode
                      AND V_type = @V_type
                      AND V_No = @V_No
                      AND Comp_code = @Comp_code
                      AND Branch_code = @Branch_code";

                    using (SqlCommand cmd = new SqlCommand(qtyQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                        cmd.Parameters.AddWithValue("@V_type", poType);
                        cmd.Parameters.AddWithValue("@V_No", poNo);
                        cmd.Parameters.AddWithValue("@Comp_code", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@Branch_code", gv.PubBranchCode);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            qty = Convert.ToDecimal(result);
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    data = new { hsnCode, qty }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetSaudaDetails(string poType, int poNo)
        {
            try
            {
                decimal rate = 0;
                int itemCode = 0;

                var gv = _globalVariableService.GetGlobalVariables();

                string query = @"
                SELECT TOP 1
                    b.RATE,
                    b.ITEM_CODE
                FROM ORDER2 a
                LEFT JOIN SAUDA b
                    ON a.SAUDA_TYPE = b.V_TYPE
                   AND a.SAUDA_NO = b.V_NO
                   AND a.COMP_CODE = b.COMP_CODE
                   AND a.BRANCH_CODE = b.BRANCH_CODE
                WHERE a.V_TYPE = @V_TYPE
                  AND a.V_NO = @V_NO
                  AND a.COMP_CODE = @COMP_CODE
                  AND a.BRANCH_CODE = @BRANCH_CODE";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@V_TYPE", poType);
                    cmd.Parameters.AddWithValue("@V_NO", poNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            rate = dr["RATE"] == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(dr["RATE"]);

                            itemCode = dr["ITEM_CODE"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(dr["ITEM_CODE"]);
                        }
                    }
                }

                return Json(new { success = true, rate, itemCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetSaudaReqByItem(int itemCode)
        {
            try
            {
                string saudaReq = string.Empty;

                var gv = _globalVariableService.GetGlobalVariables();

                string query = @"
                SELECT ISNULL(SAUDA_REQ, '')
                FROM ITEM_GROUP
                WHERE CODE = (
                    SELECT GROUP_CODE
                    FROM ITEM_MAST
                    WHERE CODE = @ITEM_CODE
                      AND COMP_CODE = @COMP_CODE
                )
                AND COMP_CODE = @COMP_CODE";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        con.Open();

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            saudaReq = result.ToString();
                        }
                    }
                }

                return Json(new { success = true, saudaReq });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetRMDiscountDetails(int saudaItemCode, int itemCode)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                bool saudaExists = false;

                decimal rate = 0;
                decimal discRate = 0;
                decimal abovePer = 0;
                decimal aboveAmt = 0;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    //=====================================================
                    // Query 1 : Check SAUDA_ITEM exists
                    //=====================================================
                    string query1 = @"
                    SELECT TOP 1 1
                    FROM RMDISC_MAST
                    WHERE SAUDA_ITEM = @SAUDA_ITEM
                      AND COMP_CODE = @COMP_CODE";

                    using (SqlCommand cmd = new SqlCommand(query1, con))
                    {
                        cmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        saudaExists = cmd.ExecuteScalar() != null;
                    }
                    if (saudaExists)
                    {
                        //=====================================================
                        // Query 2 : Get SAUDA_ITEM discount rate
                        //=====================================================
                        string query2 = @"
                        select top 1 isnull(RATE,0) from RMDISC_MAST where SAUDA_ITEM=@SAUDA_ITEM and item_code=@ITEM_CODE and COMP_CODE=@COMP_CODE";

                        using (SqlCommand cmd = new SqlCommand(query2, con))
                        {
                            cmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaItemCode);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                            var result = cmd.ExecuteScalar();
                            discRate = Convert.ToDecimal(result);
                        }

                        //=====================================================
                        // Query 3 : Get Discount Details
                        //=====================================================
                        string query3 = @"
                        SELECT TOP 1
                            ISNULL(RATE,0) AS RATE,
                            ISNULL(ABOVE_PER,0) AS ABOVE_PER,
                            ISNULL(ABOVE_AMT,0) AS ABOVE_AMT
                        FROM RMDISC_MAST
                        WHERE ITEM_CODE = @ITEM_CODE
                          AND SAUDA_ITEM = @SAUDA_ITEM
                          AND COMP_CODE = @COMP_CODE
                          AND EFF_DATE < GETDATE()
                        ORDER BY EFF_DATE DESC";

                        using (SqlCommand cmd = new SqlCommand(query3, con))
                        {
                            cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                            cmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaItemCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    rate = dr["RATE"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["RATE"])
                                        : 0;

                                    abovePer = dr["ABOVE_PER"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["ABOVE_PER"])
                                        : 0;

                                    aboveAmt = dr["ABOVE_AMT"] != DBNull.Value
                                        ? Convert.ToDecimal(dr["ABOVE_AMT"])
                                        : 0;
                                }
                            }
                        }
                    }

                }

                return Json(new
                {
                    success = true,
                    saudaExists,
                    discRate,
                    rate,
                    abovePer,
                    aboveAmt
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //--------------- Calc Frieght ---------------------
        [HttpPost]
        public async Task<IActionResult> CalculateFrieght([FromBody] DebitNoteRequest request)
        {
            try
            {
                var result = await _purchaseBillPassEntry.CalculateFrieghtPay(request);
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
                await _purchaseBillPassEntry.CalculateDebitNote(request);
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
                var result = await _purchaseBillPassEntry.CheckExistingTDS(billNo, drCode);
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
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                // Get Party Code & Party Name
                string transportQuery = @"
                SELECT
                    ISNULL(T.PARTY_CODE,0) AS PARTY_CODE,
                    ISNULL(S.NAME,'') AS PARTY_NAME
                FROM TRANSPORT_MAST T
                LEFT JOIN SUBGROUP_MAST S
                    ON T.PARTY_CODE = S.CODE
                   AND T.COMP_CODE = S.COMP_CODE
                WHERE T.COMP_CODE = @COMP_CODE
                  AND T.CODE = @CODE";

                int partyCode = 0;
                string partyName = "";

                using (SqlCommand cmd = new SqlCommand(transportQuery, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", transportCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            partyCode = reader["PARTY_CODE"] != DBNull.Value
                                ? Convert.ToInt32(reader["PARTY_CODE"])
                                : 0;

                            partyName = reader["PARTY_NAME"]?.ToString() ?? "";
                        }
                    }
                }

                return Json(new { success = true, partyCode, partyName });
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
        public async Task<IActionResult> GetPurchaseDate(string vType, int vNo)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = @"SELECT V_DATE FROM PURCHASE1 WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
                var parameter = new Dictionary<string, object>
                                {
                                    { "@V_TYPE", vType },
                                    { "@V_NO", vNo },
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@BRANCH_CODE", globalVar.PubBranchCode }
                                };
                DateTime purchaseDate = await _dbHelper.GetExecuteScalarAsync<DateTime>(query, parameter);
                return Json(new { success = true, purchaseDate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyPurchaseAmount(int partyCode, string vType, int? vNo = null, decimal? currentAmount = null)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                string query;
                var parameters = new Dictionary<string, object>
                                {
                                    { "@PARTY_CODE", partyCode },
                                    { "@V_TYPE", vType },
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@YEAR_CODE", globalVar.PubFYearCode }
                                };

                if (vNo.HasValue && currentAmount.HasValue)
                {
                    query = @"SELECT ISNULL(SUM(NAMOUNT), 0) + @CURRENT_AMOUNT FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE AND V_TYPE = @V_TYPE AND V_NO <> @V_NO
                      AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE";

                    parameters.Add("@V_NO", vNo.Value);
                    parameters.Add("@CURRENT_AMOUNT", currentAmount.Value);
                }
                else
                {
                    query = @"SELECT ISNULL(SUM(NAMOUNT), 0) FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE
                      AND YEAR_CODE = @YEAR_CODE";
                }

                decimal totalAmount = await _dbHelper.GetExecuteScalarAsync<decimal>(query, parameters);

                return Json(new { success = true, totalAmount });
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
                var globalVar = _globalVariableService.GetGlobalVariables();

                string query = @"SELECT ISNULL(TDS_206APPLY, '') FROM SUBGROUP_MAST WHERE COMP_CODE = @COMP_CODE AND CODE = @PARTY_CODE";

                var parameters = new Dictionary<string, object>
                                {
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@PARTY_CODE", partyCode }
                                };

                string tds206Apply = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

                return Json(new { success = true, tds206Apply });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> IsPostingExist(string vType)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = @"SELECT TOP 1 1 FROM POSTING_MAST WHERE V_TYPE = @V_TYPE AND DOC_TYPE = 'PURCHASE' AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";
                var parameters = new Dictionary<string, object>
                                {
                                    { "@V_TYPE", vType },
                                    { "@COMP_CODE", globalVar.PubCompCode },
                                    { "@BRANCH_CODE", globalVar.PubBranchCode }
                                };

                int result = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);

                return Json(new { success = true, isPostingExist = result == 1 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPLDocId(int vNo, int plNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string query = @" SELECT TOP (1) Doc_Id FROM Purchase1 WHERE V_Type IN ( SELECT Code FROM Doctype_Mast WHERE Doctype = @DocType)
                        AND V_No <> @V_No AND PL_No = @PL_No AND Comp_Code = @COMP_CODE AND Branch_Code = @BRANCH_CODE AND Year_Code = @Year_Code";

                var parameters = new Dictionary<string, object>
                                 {
                                     { "@DocType", "Purchaseinvoice" },
                                     { "@V_No", vNo },
                                     { "@PL_No", plNo },
                                     { "@COMP_CODE", gv.PubCompCode },
                                     { "@BRANCH_CODE", gv.PubBranchCode },
                                     { "@Year_Code", gv.PubFYearCode }
                                 };

                int docId = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);

                return Json(new { success = true, docId = docId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetDebitAcType(int code)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string query = @"SELECT isnull(gm.type,'') FROM GR_MAST gm 
                                    INNER JOIN MGROUP_MAST mg ON gm.code = mg.gr_code AND gm.comp_code = mg.comp_code 
                                    INNER JOIN SUBGROUP_MAST sg ON mg.code = sg.group_code AND mg.comp_code = sg.comp_code 
                                    WHERE sg.code =@code AND sg.comp_code = @COMP_CODE";
                var parameters = new Dictionary<string, object>
                                 {
                                     { "@COMP_CODE", gv.PubCompCode },
                                     { "@Code", code }
                                 };

                string type = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

                return Json(new { success = true, type = type });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPOType(string poType, int poNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string query = @"Select isnull(POTYPE,'') from ORDER1 where V_TYPE=@V_TYPE and  V_NO=@V_NO and COMP_CODE=@COMP_CODE and Branch_Code=@Branch_Code";
                var parameters = new Dictionary<string, object>
                                 {
                                     { "@V_TYPE", poType },
                                     { "@V_NO", poNo },
                                     { "@COMP_CODE", gv.PubCompCode },
                                     { "@Branch_Code", gv.PubBranchCode }
                                 };

                string type = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

                return Json(new { success = true, type = type });
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
                var gv = _globalVariableService.GetGlobalVariables();

                string query = "";

                if (purchaseOrSale.Equals("PURCHASE", StringComparison.OrdinalIgnoreCase))
                {
                    query += @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM Purchase1 WHERE CONCAT(V_TYPE, V_NO) <> @DOCID
                      AND Transport_Name = @Transport_Name AND GR_NO = @GR_NO AND V_Type NOT IN (
                      SELECT Code FROM Doctype_Mast WHERE Doctype = 'MaterialReceipt' )
                      AND Comp_Code = @Comp_Code AND Branch_Code = @Branch_Code AND Year_Code = @Year_Code";
                }
                else if (purchaseOrSale.Equals("SALE", StringComparison.OrdinalIgnoreCase))
                {
                    query = @"SELECT TOP 1 CONCAT(V_TYPE, V_NO) FROM Sale1 WHERE CONCAT(V_TYPE, V_NO) <> @DOCID AND Transport_Name = @Transport_Name
                      AND GR_NO = @GR_NO AND Comp_Code = @Comp_Code AND Branch_Code = @Branch_Code AND Year_Code = @Year_Code";
                }
                else
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                var parameters = new Dictionary<string, object>
                {
                    { "@DOCID", currentVoucher },
                    { "@Transport_Name", transportName },
                    { "@GR_NO", grNo },
                    { "@Comp_Code", gv.PubCompCode },
                    { "@Branch_Code", gv.PubBranchCode },
                    { "@Year_Code", gv.PubFYearCode }
                };

                string voucherNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);

                return Json(new { success = true, voucherNo = voucherNo });
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
                var gv = _globalVariableService.GetGlobalVariables();

                string query = @"
                    SELECT TOP 1 1
                    FROM LEDGER_OS
                    WHERE V_TYPE = 'BPMT'
                      AND DOC_TYPE = @V_TYPE
                      AND DOC_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE";

                var parameters = new Dictionary<string, object>
                {
                    { "@V_TYPE", docType },
                    { "@V_NO", docNo },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode }
                };

                int result = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);

                return Json(new { success = true, exists = result == 1 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetPurchaseQtyExcess(int vNo, decimal currentRecQty)
        {
            try
            {
                var result = await _purchaseBillPassEntry.CheckPurchaseQtyExcess(vNo, currentRecQty);
                return Json(new { success = true, result });
            }
            catch (Exception ex)
            {
                return Json(new { success = true, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> CheckDuplicateBill(int partyCode, string billNo, int currentVNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string query = @"SELECT TOP 1 DOC_ID AS DocId, format(V_date, 'dd/MM/yyyy') AS VDate FROM PURCHASE1 WHERE PARTY_CODE = @PARTY_CODE
                                AND BILL_NO = @BILL_NO AND V_TYPE IN ('STPB','STDP','STJW','RMPB','BFPB','RIMP','RMDP','SIDP','SADP')
                                AND V_NO <> @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@PARTY_CODE", partyCode),
                    new SqlParameter("@BILL_NO", billNo),
                    new SqlParameter("@V_NO", currentVNo),
                    new SqlParameter("@COMP_CODE", gv.PubCompCode),
                    new SqlParameter("@BRANCH_CODE", gv.PubBranchCode),
                    new SqlParameter("@YEAR_CODE", gv.PubFYearCode)
                };

                DataTable dt = await _dbHelper.ExecuteQueryAsync(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    string docId = dt.Rows[0]["DOC_ID"].ToString();
                    DateTime? vDate = dt.Rows[0]["V_DATE"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(dt.Rows[0]["V_DATE"]);

                    return Json(new { success = true, exists = true, docId, vDate });
                }

                return Json(new { success = true, exists = false });
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
                var gv = _globalVariableService.GetGlobalVariables();

                string query = @"SELECT State_Code FROM CITY_MAST WHERE Code = @CITY_CODE";

                var parameters = new Dictionary<string, object>
                {
                    { "@CITY_CODE", cityCode }
                };

                int stateCode = await _dbHelper.GetExecuteScalarAsync<int>(query, parameters);
                string stateType = gv.STATE_CODE == stateCode.ToString() ? "Local" : "Central/Other";

                if (gv.STATE_CODE == stateCode.ToString() && totalIGST > 0)
                {
                    return Json(new { success = true, isValid = false, message = $"IGST not applicable as Party State type is {stateType}." });
                }
                if (gv.STATE_CODE != stateCode.ToString() && (totalCGST + totalSGST) > 0)
                {
                    return Json(new { success = true, isValid = false, message = $"CGST/SGST not applicable as Party State type is {stateType}." });
                }
                if (totalIGST > 0 && (totalCGST + totalSGST) > 0)
                {
                    return Json(new { success = true, isValid = false, message = "CGST + SGST + IGST all three types of tax are not applicable." });
                }

                return Json(new { success = true, isValid = true });
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
                var result = await _purchaseBillPassEntry.ValidatePurchaseRow(vType, itemCode, itemName, billHsnCode, qty,
                    freightAmount, poType, poNo, mrnType, mrnNo);

                return Json(new { success = true, result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ValidatePoSaudaApproval(int itemCode, string itemName, string poType, int poNo)
        {
            try
            {
                var result = await _purchaseBillPassEntry.ValidatePoSaudaApproval(itemCode, itemName, poType, poNo);
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


                var response = new
                {
                    userLevel = "2",//gv.PubUserLevel,
                    compCode = gv.PubCompCode,
                    pubDefPOInMRN = gs.pubDefPOInMRN,
                    dataSource = erpCon.DataSource
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
                var result = await _purchaseBillPassEntry.ValidatePartyGst(gstType, partyCode, gstNo);
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
                var result = await _purchaseBillPassEntry.CalculateTDS(model);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCopyFromMenu(string docType)
        {
            var result = _purchaseBillPassEntry.GetCopyFromMenu(docType);

            if (!result.status)
                return Json(new { success = result.status, message = result.message });

            return Json(new { success = result.status, data = result.data });
        }

        [HttpPost]
        public IActionResult GetCopyFromData([FromBody] CopyFromRequest request)
        {
            var result = _purchaseBillPassEntry.GetCopyFromData(request);

            return Json(new {success = result.status, message = result.message, data = result.data});
        }
    }
}

