using DocumentFormat.OpenXml.Drawing;
using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using System.Text;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.AddAttachmentService;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Controllers.Master.CityMasterController;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseBillPassEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IPurchaseBillPassEntryRepository _purchaseBillPassEntry;

        public PurchaseBillPassEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, IPurchaseBillPassEntryRepository purchaseBillPassEntry)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _purchaseBillPassEntry = purchaseBillPassEntry;
        }
        public IActionResult Index()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
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

        //public IActionResult GetPoList(int cCode, int yCode, int bCode)
        //{
        //    string query = "SELECT V_TYPE,DOC_ID FROM PO_MAST WHERE COMP_CODE = '" + cCode + "' AND YEAR_CODE='" + yCode + "' AND BRANCH_CODE='" + bCode + "'";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}

        //public IActionResult GetPartyListNatureOther(int cCode)
        //{
        //    string query = "SELECT CODE,NAME FROM SUBGROUP_MAST WHERE COMP_CODE='" + cCode + "' AND NATURE='Others' AND ACTIVE=1 ORDER BY NAME ";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}
        public IActionResult GetTransportList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"select code, ltrim(name) from TRANSPORT_MAST 
                                where COMP_CODE={gv.PubCompCode} and ACTIVE=1 order by ltrim(name)";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        //public JsonResult GetCityList()
        //{
        //    var city = new List<object>();

        //    using (SqlConnection conn = _dbConnection.GetErpConnection())
        //    {
        //        string query = "SELECT CODE, NAME FROM CITY_MAST";
        //        SqlCommand cmd = new SqlCommand(query, conn);
        //        conn.Open();
        //        SqlDataReader reader = cmd.ExecuteReader();
        //        while (reader.Read())
        //        {
        //            city.Add(new
        //            {
        //                Value = reader["Code"].ToString(),
        //                Text = reader["Name"].ToString()
        //            });
        //        }

        //    }
        //    return Json(city);
        //}
        //public IActionResult GetTransitNoByParty(int cCode, int bCode, int yCode, int pCode)
        //{
        //    //var query = "SELECT V_NO,DOC_ID FROM WAYBILL1 WHERE  COMP_CODE='" + cCode + "' AND YEAR_CODE='" + yCode + "' AND BRANCH_CODE='" + bCode + "' AND PARTY_CODE='" + pCode + "' ";
        //    var queryBuilder = new StringBuilder();
        //    queryBuilder.Append("SELECT V_NO,DOC_ID FROM WAYBILL1 ");
        //    queryBuilder.Append("WHERE COMP_CODE='").Append(cCode).Append("' ");
        //    queryBuilder.Append("AND YEAR_CODE='").Append(yCode).Append("' ");
        //    queryBuilder.Append("AND BRANCH_CODE='").Append(bCode).Append("' ");
        //    queryBuilder.Append("AND PARTY_CODE='").Append(pCode).Append("'");
        //    string query = queryBuilder.ToString();

        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}
        //public IActionResult GetAddressListByBillToParty(int pCode)
        //{
        //    var cCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        //    var query = "SELECT ADDRESS_ID,ADD1 FROM [SUBGROUP_ADDRESS] WHERE  COMP_CODE='" + cCode + "' AND CODE='" + pCode + "'";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}

        //[HttpGet]
        //public IActionResult GetAddressByDocId(string docCODE)
        //{
        //    var cCode = _globalVariableService.GetGlobalVariables().PubCompCode;

        //    var addressDetails = new
        //    {
        //        pcode = "",
        //        add1 = "",
        //        add2 = "",
        //        add3 = "",
        //        pincode = "",
        //        gstin = "",
        //        cityCode = ""
        //    };

        //    try
        //    {
        //        string partyCode = null;

        //        using (SqlConnection conn = _dbConnection.GetErpConnection())
        //        {
        //            // Step 1: Get PARTY_CODE from PURCHASE1 using DOC_ID
        //            using (SqlCommand cmd = new SqlCommand("SELECT PARTY_CODE FROM PURCHASE1 WHERE DOC_ID = @docId", conn))
        //            {
        //                cmd.Parameters.AddWithValue("@docId", docCODE);

        //                conn.Open();
        //                var result = cmd.ExecuteScalar();
        //                if (result != null && result != DBNull.Value)
        //                {
        //                    partyCode = result.ToString();
        //                }
        //                conn.Close();
        //            }

        //            if (string.IsNullOrEmpty(partyCode))
        //            {
        //                return Json(new { success = false, message = "No PARTY_CODE found for the provided DOC_ID." });
        //            }

        //            // Step 2: Get Address List  from SUBGROUP_ADDRESS using PARTY_CODE
        //            GetAddressListByBillToParty(Convert.ToInt32(partyCode));


        //            // Step 3: Get Address from SUBGROUP_ADDRESS using PARTY_CODE
        //            using (SqlCommand cmd = new SqlCommand("SELECT TOP(1) CODE,ADD1, ADD2, ADD3, PINCODE, GSTIN, CITY_CODE FROM SUBGROUP_ADDRESS WHERE COMP_CODE = @COMP_CODE AND CODE = @PCODE", conn))
        //            {
        //                cmd.Parameters.AddWithValue("@COMP_CODE", cCode);
        //                cmd.Parameters.AddWithValue("@PCODE", partyCode);

        //                conn.Open();
        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        addressDetails = new
        //                        {
        //                            pcode = reader["CODE"].ToString(),
        //                            add1 = reader["ADD1"].ToString(),
        //                            add2 = reader["ADD2"].ToString(),
        //                            add3 = reader["ADD3"].ToString(),
        //                            pincode = reader["PINCODE"].ToString(),
        //                            gstin = reader["GSTIN"].ToString(),
        //                            cityCode = reader["CITY_CODE"].ToString()
        //                        };
        //                    }
        //                }
        //                conn.Close();
        //            }

        //        }

        //        return Json(new { success = true, addressDetails });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error retrieving address details", error = ex.Message });
        //    }
        //}
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

        //public IActionResult GetDocDetailsByTransitNo(int cCode, int yCode, int bCode, string docId)
        //{
        //    var docDetails = new
        //    {
        //        formNoWB = "",
        //        formDateWB = "",
        //        expDateWB = "",
        //        billNO = "",
        //        billDate = ""
        //    };
        //    try
        //    {
        //        using (SqlConnection connection = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("SELECT FORM_NO,FORM_DATE,EXPIRY_DATE, BILL_NO,BILL_DATE FROM WAYBILL1 WHERE COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE AND BRANCH_CODE=@BRANCH_CODE AND V_NO = @DOC_ID", connection))
        //            {
        //                cmd.Parameters.AddWithValue("@COMP_CODE", cCode);
        //                cmd.Parameters.AddWithValue("@YEAR_CODE", yCode);
        //                cmd.Parameters.AddWithValue("@BRANCH_CODE", bCode);
        //                cmd.Parameters.AddWithValue("@DOC_ID", docId);
        //                connection.Open();
        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        docDetails = new
        //                        {
        //                            formNoWB = reader["FORM_NO"].ToString(),
        //                            formDateWB = reader["FORM_DATE"].ToString(),
        //                            expDateWB = reader["EXPIRY_DATE"].ToString(),
        //                            billNO = reader["BILL_NO"].ToString(),
        //                            billDate = reader["BILL_DATE"].ToString()
        //                        };

        //                    }
        //                }
        //            }
        //        }
        //        return Json(new { success = true, docDetails });
        //    }
        //    catch (Exception)
        //    {
        //        return Json(new { success = false, message = "Error retrieving the address by specfic address id" });
        //    }
        //}

        //public IActionResult GetTexType()
        //{
        //    var query = "SELECT CODE,NAME FROM TAX_MAST";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}

        //public IActionResult GetItemList(int cCode)
        //{
        //    string query = "SELECT CODE,NAME FROM ITEM_MAST WHERE COMP_CODE = '" + cCode + "' AND ACTIVE=1 ORDER BY NAME";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}
        //public IActionResult GetUOMList()
        //{
        //    string query = "SELECT CODE,NAME FROM QCPUNIT_MAST WHERE ACTIVE=1 ORDER BY NAME";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}

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


        //[HttpGet]
        //public IActionResult GetHsnAndUnitAndMakeByItemCode(int iCode)
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        //    object makeItem = null;

        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            string query = @"
        //        SELECT TOP 1 IM.HSN_CODE, IM.UNIT_CODE, IM.ITEM_TYPE, IMK.MAKE_CODE, IMM.NAME 
        //        FROM ITEM_MAKE IMK
        //        LEFT JOIN ITEMMAKE_MAST IMM ON IMM.CODE = IMK.MAKE_CODE
        //        LEFT JOIN ITEM_MAST IM ON IMK.ITEM_CODE = IM.CODE
        //        WHERE IMM.COMP_CODE = @COMP_CODE  
        //          AND IMK.ITEM_CODE = @ITEM_CODE
        //        ORDER BY IMM.NAME";

        //            using (SqlCommand cmd = new SqlCommand(query, con))
        //            {
        //                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
        //                cmd.Parameters.AddWithValue("@ITEM_CODE", iCode);

        //                con.Open();
        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        makeItem = new
        //                        {
        //                            HsnCode = reader["HSN_CODE"],
        //                            UnitCode = reader["UNIT_CODE"],
        //                            ItemType = reader["ITEM_TYPE"],
        //                            MakeCode = reader["MAKE_CODE"],
        //                            Name = reader["NAME"]
        //                        };
        //                    }
        //                }
        //            }
        //        }

        //        if (makeItem != null)
        //        {
        //            return Json(new { success = true, data = makeItem });
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = "No record found." });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error retrieving make item", error = ex.Message });
        //    }
        //}

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
        public IActionResult GetFullQuotationByVno(int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            PURCHASE1 header = null;
            List<PURCHASE2> items = new();
            List<PURCHASE3> attachments = new();

            try
            {
                using SqlConnection conn = _dbConnection.GetErpConnection();
                using SqlCommand cmd = new("sp_PurchaseBillPassEntryDirect", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                //cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                conn.Open();
                using SqlDataReader rdr = cmd.ExecuteReader();

                // Header (QUOTATION1)
                if (rdr.Read())
                {
                    header = new PURCHASE1
                    {
                        YEAR_CODE = rdr["YEAR_CODE"] as int? ?? 0,
                        COMP_CODE = rdr["COMP_CODE"] as int? ?? 0,
                        BRANCH_CODE = rdr["BRANCH_CODE"] as int? ?? 0,
                        V_TYPE = rdr["V_TYPE"]?.ToString(),
                        V_NO = rdr["V_NO"] as int? ?? 0,
                        V_DATE = rdr["V_DATE"] as DateTime? ?? DateTime.MinValue,
                        DOC_ID = rdr["DOC_ID"]?.ToString(),
                        PLACE_CODE = rdr["PLACE_CODE"] as int? ?? 0,
                        EMP_CODE = rdr["EMP_CODE"] as int? ?? 0,
                        PARTY_CODE = rdr["PARTY_CODE"] as int? ?? 0,
                        //PARTY_NAME = rdr["PARTY_NAME"]?.ToString(),
                        EXCH_RATE = rdr["EXCH_RATE"] as decimal? ?? 0,
                        CREDIT_AC = rdr["CREDIT_AC"] as int? ?? 0,
                        DEBIT_AC = rdr["DEBIT_AC"] as int? ?? 0,
                        BILL_ADD1 = rdr["BILL_ADD1"]?.ToString(),
                        BILL_ADD2 = rdr["BILL_ADD2"]?.ToString(),
                        BILL_ADD3 = rdr["BILL_ADD3"]?.ToString(),
                        BILL_CITY = rdr["BILL_CITY"] as int? ?? 0,
                        BILL_PINCODE = rdr["BILL_PINCODE"]?.ToString(),
                        BILL_ADDRESSID = rdr["BILL_ADDRESSID"] as int? ?? 0,
                        BILL_GST = rdr["BILL_GST"]?.ToString(),
                        SHIP_CODE = rdr["SHIP_CODE"] as int? ?? 0,
                        SHIP_ADD1 = rdr["SHIP_ADD1"]?.ToString(),
                        SHIP_ADD2 = rdr["SHIP_ADD2"]?.ToString(),
                        SHIP_ADD3 = rdr["SHIP_ADD3"]?.ToString(),
                        SHIP_CITY = rdr["SHIP_CITY"] as int? ?? 0,
                        SHIP_PINCODE = rdr["SHIP_PINCODE"]?.ToString(),
                        SHIP_ADDRESSID = rdr["SHIP_ADDRESSID"] as int? ?? 0,
                        SHIP_GST = rdr["SHIP_GST"]?.ToString(),
                        BILL_NO = rdr["BILL_NO"]?.ToString(),
                        BILL_DATE = rdr["BILL_DATE"] as DateTime? ?? DateTime.MinValue,
                        CHALL_NO = rdr["CHALL_NO"]?.ToString(),
                        CHALL_DATE = rdr["CHALL_DATE"] as DateTime? ?? DateTime.MinValue,
                        UOM_CODE = rdr["UOM_CODE"] as int? ?? 0,
                        GATE_TYPE = rdr["GATE_TYPE"]?.ToString(),
                        GATE_NO = rdr["GATE_NO"] as int? ?? 0,
                        REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                        REF_NO = rdr["REF_NO"] as int? ?? 0,
                        PASS_TYPE = rdr["PASS_TYPE"]?.ToString(),
                        PASS_NO = rdr["PASS_NO"] as int? ?? 0,
                        TRANSIT_NO = rdr["TRANSIT_NO"] as int? ?? 0,
                        WAYBILL_NO = rdr["WAYBILL_NO"]?.ToString(),
                        TRANSPORT_CODE = rdr["TRANSPORT_CODE"] as int? ?? 0,
                        TRANSPORT_NAME = rdr["TRANSPORT_NAME"]?.ToString(),
                        TRANSPORT_AC = rdr["TRANSPORT_AC"] as int? ?? 0,
                        GR_NO = rdr["GR_NO"]?.ToString(),
                        GR_DATE = rdr["GR_DATE"] as DateTime? ?? DateTime.MinValue,
                        TRUCK_NO = rdr["TRUCK_NO"]?.ToString(),
                        CONTAINER_NO = rdr["CONTAINER_NO"]?.ToString(),
                        SEALED_VEHICLE = rdr["SEALED_VEHICLE"] as int? ?? 0,
                        INPUT_TYPE = rdr["INPUT_TYPE"]?.ToString(),
                        EXPS_TYPE = rdr["EXPS_TYPE"]?.ToString(),
                        REMARKS = rdr["REMARKS"]?.ToString(),
                        STATUS = rdr["STATUS"] as int? ?? 0,
                        RECD_QTY = rdr["RECD_QTY"] as decimal? ?? 0,
                        BILL_QTY = rdr["BILL_QTY"] as decimal? ?? 0,
                        AMOUNT = rdr["AMOUNT"] as decimal? ?? 0,
                        DISC_PER = rdr["DISC_PER"] as decimal? ?? 0,
                        DISC_AMT = rdr["DISC_AMT"] as decimal? ?? 0,
                        PACK_PER = rdr["PACK_PER"] as decimal? ?? 0,
                        PACK_AMT = rdr["PACK_AMT"] as decimal? ?? 0,
                        CGST_PER = rdr["CGST_PER"] as decimal? ?? 0,
                        CGST_AMT = rdr["CGST_AMT"] as decimal? ?? 0,
                        SGST_PER = rdr["SGST_PER"] as decimal? ?? 0,
                        SGST_AMT = rdr["SGST_AMT"] as decimal? ?? 0,
                        IGST_PER = rdr["IGST_PER"] as decimal? ?? 0,
                        IGST_AMT = rdr["IGST_AMT"] as decimal? ?? 0,
                        CESS_PER = rdr["CESS_PER"] as decimal? ?? 0,
                        CESS_AMT = rdr["CESS_AMT"] as decimal? ?? 0,
                        VAT_PER = rdr["VAT_PER"] as decimal? ?? 0,
                        VAT_AMT = rdr["VAT_AMT"] as decimal? ?? 0,
                        OTH_AMT = rdr["OTH_AMT"] as decimal? ?? 0,
                        TCS_PER = rdr["TCS_PER"] as decimal? ?? 0,
                        TCS_AMT = rdr["TCS_AMT"] as decimal? ?? 0,
                        ROUND_OFF = rdr["ROUND_OFF"] as decimal? ?? 0,
                        NAMOUNT = rdr["NAMOUNT"] as decimal? ?? 0,
                        DIFF_AMT = rdr["DIFF_AMT"] as decimal? ?? 0,
                        BANK_AMT = rdr["BANK_AMT"] as decimal? ?? 0,
                        BANK_RATE = rdr["BANK_RATE"] as decimal? ?? 0,
                        PL_NO = rdr["PL_NO"] as int? ?? 0,
                        PL_DATE = rdr["PL_DATE"] as DateTime? ?? DateTime.MinValue,
                        BILLAMT_USD = rdr["BILLAMT_USD"] as decimal? ?? 0,
                        FRTPAY_AMT = rdr["FRTPAY_AMT"] as decimal? ?? 0,
                        FRTPAY_TAXPER = rdr["FRTPAY_TAXPER"] as decimal? ?? 0,
                        FRTPAY_TAX = rdr["FRTPAY_TAX"] as decimal? ?? 0,
                        FRTPAY_NAR = rdr["FRTPAY_NAR"]?.ToString(),
                        FRTPAY_DRAC = rdr["FRTPAY_DRAC"] as int? ?? 0,
                        FRTPAY_CRAC = rdr["FRTPAY_CRAC"] as int? ?? 0,
                        FRT_TDSPER = rdr["FRT_TDSPER"] as decimal? ?? 0,
                        FRT_TDS = rdr["FRT_TDS"] as decimal? ?? 0,
                        DR_FROM_TPT = rdr["DR_FROM_TPT"]?.ToString(),
                        TDS_ACT = rdr["TDS_ACT"] as int? ?? 0,
                        TDS_PER = rdr["TDS_PER"] as decimal? ?? 0,
                        TDS_AMT = rdr["TDS_AMT"] as decimal? ?? 0,
                        WB_AMT = rdr["WB_AMT"] as decimal? ?? 0,
                        WB_TDSPER = rdr["WB_TDSPER"] as decimal? ?? 0,
                        WB_TDS = rdr["WB_TDS"] as decimal? ?? 0,
                        WB_DRACT = rdr["WB_DRACT"] as int? ?? 0,
                        WB_CRACT = rdr["WB_CRACT"] as int? ?? 0,
                        WB_NARR = rdr["WB_NARR"]?.ToString(),
                        UL_AMT = rdr["UL_AMT"] as decimal? ?? 0,
                        UL_TDSPER = rdr["UL_TDSPER"] as decimal? ?? 0,
                        UL_TDS = rdr["UL_TDS"] as decimal? ?? 0,
                        UL_DRACT = rdr["UL_DRACT"] as int? ?? 0,
                        UL_CRACT = rdr["UL_CRACT"] as int? ?? 0,
                        UL_NARR = rdr["UL_NARR"]?.ToString(),
                        QLT_DR_AMT = rdr["QLT_DR_AMT"] as decimal? ?? 0,
                        QLT_DR_TAX = rdr["QLT_DR_TAX"] as decimal? ?? 0,
                        QLT_DR_NAR = rdr["QLT_DR_NAR"]?.ToString(),
                        QLT_CR_AMT = rdr["QLT_CR_AMT"] as decimal? ?? 0,
                        QLT_CR_TAX = rdr["QLT_CR_TAX"] as decimal? ?? 0,
                        QLT_CR_NAR = rdr["QLT_CR_NAR"]?.ToString(),
                        RDF_DR_AMT = rdr["RDF_DR_AMT"] as decimal? ?? 0,
                        RDF_DR_TAX = rdr["RDF_DR_TAX"] as decimal? ?? 0,
                        RDF_DR_NAR = rdr["RDF_DR_NAR"]?.ToString(),
                        RDF_CR_AMT = rdr["RDF_CR_AMT"] as decimal? ?? 0,
                        RDF_CR_TAX = rdr["RDF_CR_TAX"] as decimal? ?? 0,
                        RDF_CR_NAR = rdr["RDF_CR_NAR"]?.ToString(),
                        QTY_DR_AMT = rdr["QTY_DR_AMT"] as decimal? ?? 0,
                        QTY_DR_TAX = rdr["QTY_DR_TAX"] as decimal? ?? 0,
                        QTY_DR_NAR = rdr["QTY_DR_NAR"]?.ToString(),
                        QTY_CR_AMT = rdr["QTY_CR_AMT"] as decimal? ?? 0,
                        QTY_CR_TAX = rdr["QTY_CR_TAX"] as decimal? ?? 0,
                        QTY_CR_NAR = rdr["QTY_CR_NAR"]?.ToString(),
                        QC_DR_AMT = rdr["QC_DR_AMT"] as decimal? ?? 0,
                        QC_DR_TAX = rdr["QC_DR_TAX"] as decimal? ?? 0,
                        QC_DR_NAR = rdr["QC_DR_NAR"]?.ToString(),
                        QC_CR_AMT = rdr["QC_CR_AMT"] as decimal? ?? 0,
                        QC_CR_TAX = rdr["QC_CR_TAX"] as decimal? ?? 0,
                        QC_CR_NAR = rdr["QC_CR_NAR"]?.ToString(),
                        OTH_DR_AMT = rdr["OTH_DR_AMT"] as decimal? ?? 0,
                        OTH_DR_TAX = rdr["OTH_DR_TAX"] as decimal? ?? 0,
                        OTH_DR_NAR = rdr["OTH_DR_NAR"]?.ToString(),
                        QC_TYPE = rdr["QC_TYPE"]?.ToString(),
                        QC_NO = rdr["QC_NO"] as int? ?? 0,
                        DEPT_CODE = rdr["DEPT_CODE"] as int? ?? 0,
                        TAX_HOLD = rdr["TAX_HOLD"]?.ToString(),
                        PRICE_TYPE = rdr["PRICE_TYPE"]?.ToString(),
                        FAPROV_STATUS = rdr["FAPROV_STATUS"]?.ToString(),
                        FAPROV_REMARKS = rdr["FAPROV_REMARKS"]?.ToString(),
                        HOLD_PAY = rdr["HOLD_PAY"]?.ToString(),
                        HOLD_REASON = rdr["HOLD_REASON"]?.ToString(),
                        HOLD_DATE = rdr["HOLD_DATE"] as DateTime? ?? DateTime.MinValue,
                        IMPORT_AMT = rdr["IMPORT_AMT"] as decimal? ?? 0,
                        IMPORT_TAX = rdr["IMPORT_TAX"] as decimal? ?? 0,
                        INVLAND_AMT = rdr["INVLAND_AMT"] as decimal? ?? 0,
                        RCM_NO = rdr["RCM_NO"]?.ToString(),
                        DRNOTE_MAILSEND = rdr["DRNOTE_MAILSEND"] as int? ?? 0,
                        FRT_BILLNO = rdr["FRT_BILLNO"] as int? ?? 0,
                        FRT_BILLDT = rdr["FRT_BILLDT"] as DateTime? ?? DateTime.MinValue,
                        FRT_PASSDT = rdr["FRT_PASSDT"] as DateTime? ?? DateTime.MinValue,
                        FRT_CHQ = rdr["FRT_CHQ"]?.ToString(),
                        FRT_REMARK = rdr["FRT_REMARK"]?.ToString(),
                        GSTRMAIL_PARTYCNTR = rdr["GSTRMAIL_PARTYCNTR"] as int? ?? 0,
                        GSTRMAIL_BILLCNTR = rdr["GSTRMAIL_BILLCNTR"] as int? ?? 0,
                        UUSER = rdr["UUSER"] as int? ?? 0,
                        UDATE = rdr["UDATE"] as DateTime? ?? DateTime.MinValue,
                        EUSER = rdr["EUSER"] as int? ?? 0,
                        EDATE = rdr["EDATE"] as DateTime? ?? DateTime.MinValue,
                        AED = rdr["AED"]?.ToString(),
                        WSID = rdr["WSID"]?.ToString(),
                        LIP = rdr["LIP"]?.ToString(),
                        LID = rdr["LID"]?.ToString(),
                        TDS_PER194Q = rdr["TDS_PER194Q"] as decimal? ?? 0,
                        TDS_AMT194Q = rdr["TDS_AMT194Q"] as decimal? ?? 0,
                        DISP_ADDRESS = rdr["DISP_ADDRESS"]?.ToString(),
                        DISP_CITY = rdr["DISP_CITY"] as int? ?? 0,
                        GSTRECO_REFTYPE = rdr["GSTRECO_REFTYPE"]?.ToString(),
                        GSTRECO_REFNO = rdr["GSTRECO_REFNO"] as int? ?? 0,
                        STOREIMG_FLG = rdr["STOREIMG_FLG"] as int? ?? 0,
                        RET_TYPE = rdr["RET_TYPE"]?.ToString(),
                        FEXCH_USD = rdr["FEXCH_USD"] as decimal? ?? 0,
                        TRP_GSTNO = rdr["TRP_GSTNO"]?.ToString(),
                        TRP_BILLNO = rdr["TRP_BILLNO"]?.ToString(),
                        TRP_BILLDATE = rdr["TRP_BILLDATE"] as DateTime? ?? DateTime.MinValue,
                        TRP_TAXTYPE = rdr["TRP_TAXTYPE"]?.ToString(),
                        MONTH_3B = rdr["MONTH_3B"] as DateTime? ?? DateTime.MinValue,
                        MONTH_3BN = rdr["MONTH_3BN"] as DateTime? ?? DateTime.MinValue,
                        TRP_MONTH3B = rdr["TRP_MONTH3B"] as DateTime? ?? DateTime.MinValue,
                        MTH_REVYN3B = rdr["MTH_REVYN3B"]?.ToString(),
                        TRP_MTHREVYN3B = rdr["TRP_MTHREVYN3B"]?.ToString(),
                        MONTH_2B = rdr["MONTH_2B"] as DateTime? ?? DateTime.MinValue,
                        EWB_DATE = rdr["EWB_DATE"] as DateTime? ?? DateTime.MinValue,
                        EWB_EXPDATE = rdr["EWB_EXPDATE"] as DateTime? ?? DateTime.MinValue,
                        EWB_INVNO = rdr["EWB_INVNO"]?.ToString(),
                        PL_AMT = rdr["PL_AMT"] as decimal? ?? 0
                    };
                }

                //  Items (QUOTATION2)
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        items.Add(new PURCHASE2
                        {
                            DOC_ID = rdr["DOC_ID"]?.ToString(),
                            YEAR_CODE = rdr["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["YEAR_CODE"]) : 0,
                            COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                            BRANCH_CODE = rdr["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BRANCH_CODE"]) : 0,
                            V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                            V_TYPE = rdr["V_TYPE"]?.ToString(),
                            V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                            SNO = rdr["SNO"] != DBNull.Value ? Convert.ToInt32(rdr["SNO"]) : 0,
                            ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                            ITEM_NAME = rdr["ITEM_NAME"]?.ToString(),
                            MAKE_CODE = rdr["MAKE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MAKE_CODE"]) : 0,
                            HSN_CODE = rdr["HSN_CODE"]?.ToString(),
                            RCM_YN = rdr["RCM_YN"]?.ToString(),
                            INPUT_YN = rdr["INPUT_YN"]?.ToString(),
                            UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                            UOM_NAME = rdr["UOM_NAME"]?.ToString(),
                            DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                            NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                            PLUS_MINUSQTY = rdr["PLUS_MINUSQTY"] != DBNull.Value ? Convert.ToDecimal(rdr["PLUS_MINUSQTY"]) : 0,
                            WB_QTY = rdr["WB_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_QTY"]) : 0,
                            RECD_QTY = rdr["RECD_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["RECD_QTY"]) : 0,
                            BILL_QTY = rdr["BILL_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["BILL_QTY"]) : 0,
                            USD_RATE = rdr["USD_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["USD_RATE"]) : 0,
                            EXCH_RATE = rdr["EXCH_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["EXCH_RATE"]) : 0,
                            RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                            AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                            DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : 0,
                            DISC_AMT = rdr["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_AMT"]) : 0,
                            PACK_PER = rdr["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_PER"]) : 0,
                            PACK_AMT = rdr["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_AMT"]) : 0,
                            TAX_CODE = rdr["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TAX_CODE"]) : 0,
                            CGST_PER = rdr["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_PER"]) : 0,
                            CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_AMT"]) : 0,
                            SGST_PER = rdr["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_PER"]) : 0,
                            SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_AMT"]) : 0,
                            IGST_PER = rdr["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_PER"]) : 0,
                            IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_AMT"]) : 0,
                            CESS_PER = rdr["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_PER"]) : 0,
                            CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_AMT"]) : 0,
                            VAT_PER = rdr["VAT_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_PER"]) : 0,
                            VAT_AMT = rdr["VAT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["VAT_AMT"]) : 0,
                            OTH_AMT = rdr["OTH_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["OTH_AMT"]) : 0,
                            NET_AMT = rdr["NET_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["NET_AMT"]) : 0,
                            LAND_RATE = rdr["LAND_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["LAND_RATE"]) : 0,
                            LAND_AMT = rdr["LAND_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["LAND_AMT"]) : 0,
                            POLAND_RATE = rdr["POLAND_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["POLAND_RATE"]) : 0,
                            PO_RATE = rdr["PO_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["PO_RATE"]) : 0,
                            BIN_LOCATION = rdr["BIN_LOCATION"]?.ToString(),
                            BIN_CODE = rdr["BIN_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BIN_CODE"]) : 0,
                            PO_TYPE = rdr["PO_TYPE"]?.ToString(),
                            PO_NO = rdr["PO_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PO_NO"]) : 0,
                            SAUDA_TYPE = rdr["SAUDA_TYPE"]?.ToString(),
                            SAUDA_NO = rdr["SAUDA_NO"] != DBNull.Value ? Convert.ToInt32(rdr["SAUDA_NO"]) : 0,
                            KANTA_TYPE = rdr["KANTA_TYPE"]?.ToString(),
                            KANTA_NO = rdr["KANTA_NO"] != DBNull.Value ? Convert.ToInt32(rdr["KANTA_NO"]) : 0,
                            REQ_TYPE = rdr["REQ_TYPE"]?.ToString(),
                            REQ_NO = rdr["REQ_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REQ_NO"]) : 0,
                            GATE_TYPE = rdr["GATE_TYPE"]?.ToString(),
                            GATE_NO = rdr["GATE_NO"] != DBNull.Value ? Convert.ToInt32(rdr["GATE_NO"]) : 0,
                            REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                            REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0,
                            QC_TYPE = rdr["QC_TYPE"]?.ToString(),
                            QC_NO = rdr["QC_NO"] != DBNull.Value ? Convert.ToInt32(rdr["QC_NO"]) : 0,
                            PASS_TYPE = rdr["PASS_TYPE"]?.ToString(),
                            PASS_NO = rdr["PASS_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PASS_NO"]) : 0,
                            EMPTY_YN = rdr["EMPTY_YN"]?.ToString(),
                            MACH_CODE = rdr["MACH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MACH_CODE"]) : 0,
                            REMARKS = rdr["REMARKS"]?.ToString(),
                            RATE_MONTHLY = rdr["RATE_MONTHLY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_MONTHLY"]) : 0,
                            RATE_QUARTERLY = rdr["RATE_QUARTERLY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_QUARTERLY"]) : 0,
                            RATE_ANNUALY = rdr["RATE_ANNUALY"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_ANNUALY"]) : 0,
                            RATE_SPECIAL = rdr["RATE_SPECIAL"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE_SPECIAL"]) : 0,
                            FINAL_LOCK = rdr["FINAL_LOCK"]?.ToString(),
                            UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                            UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                            EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                            EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                            AED = rdr["AED"]?.ToString(),
                            WSID = rdr["WSID"]?.ToString(),
                            LIP = rdr["LIP"]?.ToString(),
                            LID = rdr["LID"]?.ToString()
                        });
                    }
                }

                // Attachments (QUOTATION3 as list)
                if (rdr.NextResult())
                {
                    while (rdr.Read())
                    {
                        attachments.Add(new PURCHASE3
                        {
                            DOC_ID = rdr["DOC_ID"]?.ToString(),
                            YEAR_CODE = rdr["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["YEAR_CODE"]) : 0,
                            COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                            BRANCH_CODE = rdr["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BRANCH_CODE"]) : 0,
                            V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                            V_TYPE = rdr["V_TYPE"]?.ToString(),
                            ATTACHMENT = rdr["ATTACHMENT"]?.ToString() ?? string.Empty,
                            UUSER = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                            UDATE = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                            EUSER = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                            EDATE = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                            AED = rdr["AED"]?.ToString(),
                            WSID = rdr["WSID"]?.ToString(),
                            LIP = rdr["LIP"]?.ToString(),
                            LID = rdr["LID"]?.ToString(),
                            SRNO = rdr["SRNO"] != DBNull.Value ? Convert.ToInt32(rdr["SRNO"]) : 0,
                            FILE_NAME = rdr["FILE_NAME"]?.ToString() ?? string.Empty
                        });
                    }
                }
                return Json(new
                {
                    success = true,
                    header,
                    items,
                    attachments
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching quotation", error = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SavePurchaseBillPassEntry([FromBody] PurchaseWrapper data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //int vNo = GetNextV_NO(globalVar.PubFYearCode);
            var model = data.header;
            string action = "INSERTANDUPDATE";
            string subAction = "";

            // Duplicate Check
            if (IsDuplicatePurchaseEntry(model.V_NO, Convert.ToInt32(globalVar.PubCompCode), Convert.ToInt32(globalVar.PubFYearCode)))
            {
                subAction = "UPDATE";
            }
            else
            {
                subAction = "INSERT";
            }

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseBillPassEntryDirect", con))
                    {
                        var docID = model.V_TYPE + model.V_NO;

                        cmd.CommandType = CommandType.StoredProcedure;

                        // Core Parameters for PURCHASE1
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SubAction", subAction);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE);
                        //cmd.Parameters.AddWithValue("@NET_AMT", model.AMOUNT);
                        cmd.Parameters.AddWithValue("@STATUS", model.STATUS);
                        cmd.Parameters.AddWithValue("@DOC_ID", docID ?? "");


                        // ➕ Start Adding All Additional Header Parameters

                        cmd.Parameters.AddWithValue("@BILL_ADD1", model.BILL_ADD1 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_ADD2", model.BILL_ADD2 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_ADD3", model.BILL_ADD3 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_CITY", model.BILL_CITY ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_GST", model.BILL_GST ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DISP_ADDRESS", model.DISP_ADDRESS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DISP_CITY", model.DISP_CITY ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_ADD1", model.SHIP_ADD1 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_ADD2", model.SHIP_ADD2 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_ADD3", model.SHIP_ADD3 ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_CITY", model.SHIP_CITY ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIP_GST", model.SHIP_GST ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_NO", model.BILL_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BILL_DATE", model.BILL_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CHALL_NO", model.CHALL_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CHALL_DATE", model.CHALL_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WAYBILL_NO", model.WAYBILL_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GR_DATE", model.GR_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EWB_INVNO", model.EWB_INVNO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EWB_EXPDATE", model.EWB_EXPDATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EXCH_RATE", model.EXCH_RATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@INPUT_TYPE", model.INPUT_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEBIT_AC", model.DEBIT_AC ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CREDIT_AC", model.CREDIT_AC ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NAMOUNT", model.NAMOUNT ?? (object)DBNull.Value);

                        // 🚚 Transport Information
                        cmd.Parameters.AddWithValue("@TRANSPORT_NAME", model.TRANSPORT_NAME ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TRUCK_NO", model.TRUCK_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CONTAINER_NO", model.CONTAINER_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GR_NO", model.GR_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SEALED_VEHICLE", model.SEALED_VEHICLE);

                        // 🚛 Freight Details
                        cmd.Parameters.AddWithValue("@FRTPAY_AMT", model.FRTPAY_AMT);
                        cmd.Parameters.AddWithValue("@FRTPAY_TAXPER", model.FRTPAY_TAXPER);
                        cmd.Parameters.AddWithValue("@FRTPAY_TAX", model.FRTPAY_TAX);
                        cmd.Parameters.AddWithValue("@FRTPAY_DRAC", model.FRTPAY_DRAC ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRTPAY_CRAC", model.FRTPAY_CRAC ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRTPAY_NAR", model.FRTPAY_NAR ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FRT_TDSPER", model.FRT_TDSPER);
                        cmd.Parameters.AddWithValue("@FRT_TDS", model.FRT_TDS);

                        // 🧾 Billing Info
                        cmd.Parameters.AddWithValue("@TRP_GSTNO", model.TRP_GSTNO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TRP_TAXTYPE", model.TRP_TAXTYPE ?? (object)DBNull.Value);

                        // 📦 WB Details
                        cmd.Parameters.AddWithValue("@WB_AMT", model.WB_AMT);
                        cmd.Parameters.AddWithValue("@WB_TDSPER", model.WB_TDSPER);
                        cmd.Parameters.AddWithValue("@WB_TDS", model.WB_TDS);
                        cmd.Parameters.AddWithValue("@WB_DRACT", model.WB_DRACT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WB_CRACT", model.WB_CRACT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@WB_NARR", model.WB_NARR ?? (object)DBNull.Value);

                        // 🏗️ Unloading Details
                        cmd.Parameters.AddWithValue("@UL_AMT", model.UL_AMT);
                        cmd.Parameters.AddWithValue("@UL_TDSPER", model.UL_TDSPER);
                        cmd.Parameters.AddWithValue("@UL_TDS", model.UL_TDS);
                        cmd.Parameters.AddWithValue("@UL_DRACT", model.UL_DRACT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UL_CRACT", model.UL_CRACT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UL_NARR", model.UL_NARR ?? (object)DBNull.Value);




                        // Audit Fields
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", model.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        // TVP for PURCHASE2
                        DataTable dtPurchase2 = ConvertToPurchase2TVP(data.lineRows, docID);
                        SqlParameter tvpParam = cmd.Parameters.AddWithValue("@PURCHASE2_TYPE", dtPurchase2);
                        tvpParam.SqlDbType = SqlDbType.Structured;
                        tvpParam.TypeName = "dbo.PURCHASE2_TYPE";

                        // Convert QUOT3 to DataTable
                        DataTable dtQuotation3 = await ConvertToPurchase3TVP(model.V_NO, model.V_TYPE, data.Attachement, docID);
                        var tvpParam3 = cmd.Parameters.AddWithValue("@PURCHASE3_TYPE", dtQuotation3);
                        tvpParam3.SqlDbType = SqlDbType.Structured;
                        tvpParam3.TypeName = "dbo.PURCHASE3_TYPE";

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Purchase saved successfully." });
            }
            catch (SqlException ex)
            {
                return Json(new { success = false, message = "SQL Error: " + ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        public DataTable ConvertToPurchase2TVP(List<PURCHASE2> list, string docID)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            DataTable dt = new DataTable("PURCHASE2_TYPE");

            // Add columns exactly as in PURCHASE2_TYPE (order and types must match)
            dt.Columns.Add("SNO", typeof(int));
            dt.Columns.Add("ITEM_CODE", typeof(int));
            dt.Columns.Add("ITEM_NAME", typeof(string));
            dt.Columns.Add("MAKE_CODE", typeof(int));
            dt.Columns.Add("HSN_CODE", typeof(string));
            dt.Columns.Add("RCM_YN", typeof(string));
            dt.Columns.Add("INPUT_YN", typeof(string));
            dt.Columns.Add("UOM_CODE", typeof(int));
            dt.Columns.Add("UOM_NAME", typeof(string));
            dt.Columns.Add("DEPT_CODE", typeof(int));
            dt.Columns.Add("NOS", typeof(int));
            dt.Columns.Add("PLUS_MINUSQTY", typeof(decimal));
            dt.Columns.Add("WB_QTY", typeof(decimal));
            dt.Columns.Add("RECD_QTY", typeof(decimal));
            dt.Columns.Add("BILL_QTY", typeof(decimal));
            dt.Columns.Add("USD_RATE", typeof(decimal));
            dt.Columns.Add("EXCH_RATE", typeof(decimal));
            dt.Columns.Add("RATE", typeof(decimal));
            dt.Columns.Add("AMOUNT", typeof(decimal));
            dt.Columns.Add("DISC_PER", typeof(decimal));
            dt.Columns.Add("DISC_AMT", typeof(decimal));
            dt.Columns.Add("PACK_PER", typeof(decimal));
            dt.Columns.Add("PACK_AMT", typeof(decimal));
            dt.Columns.Add("TAX_CODE", typeof(int));
            dt.Columns.Add("CGST_PER", typeof(decimal));
            dt.Columns.Add("CGST_AMT", typeof(decimal));
            dt.Columns.Add("SGST_PER", typeof(decimal));
            dt.Columns.Add("SGST_AMT", typeof(decimal));
            dt.Columns.Add("IGST_PER", typeof(decimal));
            dt.Columns.Add("IGST_AMT", typeof(decimal));
            dt.Columns.Add("CESS_PER", typeof(decimal));
            dt.Columns.Add("CESS_AMT", typeof(decimal));
            dt.Columns.Add("VAT_PER", typeof(decimal));
            dt.Columns.Add("VAT_AMT", typeof(decimal));
            dt.Columns.Add("OTH_AMT", typeof(decimal));
            dt.Columns.Add("NET_AMT", typeof(decimal));
            dt.Columns.Add("LAND_RATE", typeof(decimal));
            dt.Columns.Add("LAND_AMT", typeof(decimal));
            dt.Columns.Add("POLAND_RATE", typeof(decimal));
            dt.Columns.Add("PO_RATE", typeof(decimal));
            dt.Columns.Add("BIN_LOCATION", typeof(string));
            dt.Columns.Add("BIN_CODE", typeof(int));
            dt.Columns.Add("PO_TYPE", typeof(string));
            dt.Columns.Add("PO_NO", typeof(int));
            dt.Columns.Add("SAUDA_TYPE", typeof(string));
            dt.Columns.Add("SAUDA_NO", typeof(int));
            dt.Columns.Add("KANTA_TYPE", typeof(string));
            dt.Columns.Add("KANTA_NO", typeof(int));
            dt.Columns.Add("REQ_TYPE", typeof(string));
            dt.Columns.Add("REQ_NO", typeof(int));
            dt.Columns.Add("GATE_TYPE", typeof(string));
            dt.Columns.Add("GATE_NO", typeof(int));
            dt.Columns.Add("REF_TYPE", typeof(string));
            dt.Columns.Add("REF_NO", typeof(int));
            dt.Columns.Add("QC_TYPE", typeof(string));
            dt.Columns.Add("QC_NO", typeof(int));
            dt.Columns.Add("PASS_TYPE", typeof(string));
            dt.Columns.Add("PASS_NO", typeof(int));
            dt.Columns.Add("EMPTY_YN", typeof(string));
            dt.Columns.Add("MACH_CODE", typeof(int));
            dt.Columns.Add("REMARKS", typeof(string));
            dt.Columns.Add("RATE_MONTHLY", typeof(decimal));
            dt.Columns.Add("RATE_QUARTERLY", typeof(decimal));
            dt.Columns.Add("RATE_ANNUALY", typeof(decimal));
            dt.Columns.Add("RATE_SPECIAL", typeof(decimal));
            dt.Columns.Add("FINAL_LOCK", typeof(string));

            int sno = 1;

            foreach (var item in list)
            {
                dt.Rows.Add(
                    sno++,
                    item.ITEM_NAME ?? (object)DBNull.Value,
                    item.ITEM_NAME ?? (object)DBNull.Value,
                    item.MAKE_CODE ?? (object)DBNull.Value,
                    item.HSN_CODE ?? (object)DBNull.Value,
                    item.RCM_YN ?? (object)DBNull.Value,
                    item.INPUT_YN ?? (object)DBNull.Value,
                    item.UOM_CODE ?? (object)DBNull.Value,
                    item.UOM_NAME ?? (object)DBNull.Value,
                    item.DEPT_CODE ?? (object)DBNull.Value,
                    item.NOS ?? (object)DBNull.Value,
                    item.PLUS_MINUSQTY ?? (object)DBNull.Value,
                    item.WB_QTY ?? (object)DBNull.Value,
                    item.RECD_QTY ?? (object)DBNull.Value,
                    item.BILL_QTY ?? (object)DBNull.Value,
                    item.USD_RATE ?? (object)DBNull.Value,
                    item.EXCH_RATE ?? (object)DBNull.Value,
                    item.RATE ?? (object)DBNull.Value,
                    item.AMOUNT ?? (object)DBNull.Value,
                    item.DISC_PER ?? (object)DBNull.Value,
                    item.DISC_AMT ?? (object)DBNull.Value,
                    item.PACK_PER ?? (object)DBNull.Value,
                    item.PACK_AMT ?? (object)DBNull.Value,
                    item.TAX_CODE ?? (object)DBNull.Value,
                    item.CGST_PER ?? (object)DBNull.Value,
                    item.CGST_AMT ?? (object)DBNull.Value,
                    item.SGST_PER ?? (object)DBNull.Value,
                    item.SGST_AMT ?? (object)DBNull.Value,
                    item.IGST_PER ?? (object)DBNull.Value,
                    item.IGST_AMT ?? (object)DBNull.Value,
                    item.CESS_PER ?? (object)DBNull.Value,
                    item.CESS_AMT ?? (object)DBNull.Value,
                    item.VAT_PER ?? (object)DBNull.Value,
                    item.VAT_AMT ?? (object)DBNull.Value,
                    item.OTH_AMT ?? (object)DBNull.Value,
                    item.NET_AMT ?? (object)DBNull.Value,
                    item.LAND_RATE ?? (object)DBNull.Value,
                    item.LAND_AMT ?? (object)DBNull.Value,
                    item.POLAND_RATE ?? (object)DBNull.Value,
                    item.PO_RATE ?? (object)DBNull.Value,
                    item.BIN_LOCATION ?? (object)DBNull.Value,
                    item.BIN_CODE ?? (object)DBNull.Value,
                    item.PO_TYPE ?? (object)DBNull.Value,
                    item.PO_NO ?? (object)DBNull.Value,
                    item.SAUDA_TYPE ?? (object)DBNull.Value,
                    item.SAUDA_NO ?? (object)DBNull.Value,
                    item.KANTA_TYPE ?? (object)DBNull.Value,
                    item.KANTA_NO,
                    item.REQ_TYPE ?? (object)DBNull.Value,
                    item.REQ_NO ?? (object)DBNull.Value,
                    item.GATE_TYPE ?? (object)DBNull.Value,
                    item.GATE_NO ?? (object)DBNull.Value,
                    item.REF_TYPE ?? (object)DBNull.Value,
                    item.REF_NO ?? (object)DBNull.Value,
                    item.QC_TYPE ?? (object)DBNull.Value,
                    item.QC_NO ?? (object)DBNull.Value,
                    item.PASS_TYPE ?? (object)DBNull.Value,
                    item.PASS_NO ?? (object)DBNull.Value,
                    item.EMPTY_YN ?? (object)DBNull.Value,
                    item.MACH_CODE ?? (object)DBNull.Value,
                    item.REMARKS ?? (object)DBNull.Value,
                    item.RATE_MONTHLY ?? (object)DBNull.Value,
                    item.RATE_QUARTERLY ?? (object)DBNull.Value,
                    item.RATE_ANNUALY ?? (object)DBNull.Value,
                    item.RATE_SPECIAL ?? (object)DBNull.Value,
                    item.FINAL_LOCK ?? (object)DBNull.Value
                );
            }

            return dt;
        }

        private bool IsDuplicatePurchaseEntry(int? vno, int? cCode, int? yCode)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM PURCHASE1 WHERE V_NO = @vno AND COMP_CODE = @cCode AND YEAR_CODE = @yCode", con))
                {
                    cmd.Parameters.AddWithValue("@vno", vno);
                    cmd.Parameters.AddWithValue("@cCode", cCode);
                    cmd.Parameters.AddWithValue("@yCode", yCode);

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public async Task<DataTable> ConvertToPurchase3TVP(int? vNO, string? vType, List<PURCHASE3> list, string docId)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            DataTable dt = new DataTable("PURCHASE3_TYPE");

            dt.Columns.Add("ATTACHMENT", typeof(string));
            dt.Columns.Add("FILE_NAME", typeof(string));
            dt.Columns.Add("SRNO", typeof(int));

            var filesToSave = list
                .Where(x => !string.IsNullOrWhiteSpace(x.FILE_NAME))
                .Select(x => (
                    FileName: x.ATTACHMENT,
                    Base64Content: x.FILE_NAME
                ))
                .ToList();

            string folderName = "PurchaseBillPassEntry";
            var savedFiles = await FileHelper.SaveBase64FilesAsync(filesToSave, folderName);

            foreach (var item in list)
            {
                string attachment = item.ATTACHMENT ?? "";
                if (attachment.Length > 400)
                    attachment = attachment.Substring(0, 400);

                string fileName = item.FILE_NAME ?? "";

                dt.Rows.Add(attachment, fileName, item.SRNO ?? 0);
            }

            return dt;
        }

        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
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

                //--------------------Commented below for testing -----------
                //string duplicateQuery = @"
                //    Select distinct V_TYPE+cast(V_NO as varchar) from PURCHASE2 
                //    where V_TYPE=@V_TYPE and V_NO<>@V_NO
                //    and REF_TYPE+cast(REF_NO as varchar)=@REF_TYPE_REF_NO 
                //    and COMP_CODE=@COMP_CODE and BRANCH_CODE=@BRANCH_CODE and YEAR_CODE=@YEAR_CODE";

                //SqlCommand cmd = new SqlCommand(duplicateQuery, con);

                //cmd.Parameters.AddWithValue("@V_TYPE", vType);
                //cmd.Parameters.AddWithValue("@V_NO", vNo);
                //cmd.Parameters.AddWithValue("@REF_TYPE_REF_NO", mrnTypeNo);
                //cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                //cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                //cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                //var billPassNo = cmd.ExecuteScalar();

                //if (billPassNo != null)
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        message = "MRN No exists in Purchase Bill Pass Entry No : " + billPassNo
                //    });
                //}

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
                                    ON s1.CODE = a.BILL_CITY
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
                            a.MAKE_CODE AS Make_Code,
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
                                    Make_Code = dr["Make_Code"].ToString(),
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
        public async Task<JsonResult> GetPubDefPOInMRN()
        {
            var lgs = await _globalVariableService.LoadGeneralSetting();
            var gv = _globalVariableService.GetGlobalVariables();
            
            var pubDefPOInMRN = lgs.pubDefPOInMRN;
            var compCode = gv.PubCompCode;

            pubDefPOInMRN = "YES"; // For Testing
            return Json(new { pubDefPOInMRN, compCode });
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
            catch(Exception ex)
            {
                return Json(ex.Message);
            }
        }
    }
}

