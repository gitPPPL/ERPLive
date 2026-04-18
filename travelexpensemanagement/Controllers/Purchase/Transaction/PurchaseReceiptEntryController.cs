using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseReceiptEntry;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseReceiptEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PurchaseReceiptEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PurchaseReceiptEntry/Index.cshtml");
        }
        public JsonResult GetddlDocType()
        {
            string query = $@" Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('MaterialReceipt','ServiceReceipt')";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpPost]
        public JsonResult GetDocNo(string docType, string docName)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = @"SELECT ISNULL(MAX(V_no), 0) + 1 AS NextVNo FROM PURCHASE1 WHERE V_TYPE = @V_TYPE 
                AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                var parameters = new[]
                {
                    //new SqlParameter("@DocType", docType),
                    new SqlParameter("@CompCode", globalVar.PubCompCode),
                    new SqlParameter("@BranchCode", 1),
                    new SqlParameter("@YearCode", globalVar.PubFYearCode),
                    new SqlParameter("@V_TYPE", docType)
                };
                int nextVNo = 1;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddRange(parameters);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            nextVNo = Convert.ToInt32(result);
                        }
                    }
                }
                return Json(new { success = true, nextVNo = nextVNo, docType= docType });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public JsonResult GetddlGateNo(string VNo, string Vtype)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string gType = Vtype switch
            {
                "RCPT" or "RCPI" => "INRM",
                "SRPU" => "INST",
                "SRJW" => "INST",
                "BFRC" => "INFU",
                _ => "INST" 
            };
            string query = $@" SELECT V_NO, DOC_ID FROM Gate1 G WHERE G.v_type =  '{gType}'  AND G.COMP_CODE = {globalVar.PubCompCode}  AND G.BRANCH_CODE = 1
            AND G.YEAR_CODE = {globalVar.PubFYearCode} AND (MRN_NO IS NULL OR MRN_NO = 0 OR MRN_NO='{VNo}') ORDER BY G.V_NO";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlDocStatus()
        {
            string query = $@" Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlBillFrom()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by name asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetBillDetailsddlAddLine1(string code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT ADDRESS_ID, Add1 AS Name FROM SUBGROUP_ADDRESS WHERE COMP_CODE = {globalVar.PubCompCode} AND CODE = '{code}'";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetShipDetailsddlAddLine1(string code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT ADDRESS_ID, Add1 AS Name FROM SUBGROUP_ADDRESS WHERE COMP_CODE = {globalVar.PubCompCode} AND CODE = '{code}'";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpPost]
        public JsonResult GetBillDetails(int code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
                string query = @" SELECT TOP 1 a.code, a.name, a.ADD1, a.ADD2, a.Add3, a.CITY_CODE, b.name AS City, c.code AS StateCode, c.name AS State, a.GSTIN, a.PINCODE 
                FROM SUBGROUP_MAST a LEFT JOIN CITY_MAST b ON a.CITY_CODE = b.CODE LEFT JOIN STATE_MAST c ON b.STATE_CODE = c.CODE WHERE a.NATURE = 'Supplier' 
                AND a.COMP_CODE = @CompCode AND a.ACTIVE = 1 AND a.CODE = @Code";

            object billDetails = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);

                    con.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            billDetails = new
                            {
                                Code = rdr["code"].ToString(),
                                Name = rdr["name"].ToString(),
                                Address1 = rdr["ADD1"].ToString(),
                                Address2 = rdr["ADD2"].ToString(),
                                Address3 = rdr["Add3"].ToString(),
                                CityCode = rdr["CITY_CODE"].ToString(),
                                City = rdr["City"].ToString(),
                                StateCode = rdr["StateCode"].ToString(),
                                State = rdr["State"].ToString(),
                                GSTIN = rdr["GSTIN"].ToString(),
                                Pincode = rdr["PINCODE"].ToString()
                            };
                        }
                    }
                }
            }
            return Json(billDetails); 
        }
        [HttpPost]
        public JsonResult GetBillDetailsAddLine1(int code, int AddressID)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query = @"
        SELECT ADD1, ADD2, Add3, CITY_CODE, STATE_CODE, GSTIN, PINCODE 
        FROM SUBGROUP_ADDRESS 
        WHERE COMP_CODE = @CompCode AND CODE = @Code AND ADDRESS_ID = @AddressID";

            object billDetails = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@AddressID", AddressID);

                    con.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            billDetails = new
                            {
                                Address1 = rdr["ADD1"].ToString(),
                                Address2 = rdr["ADD2"].ToString(),
                                Address3 = rdr["Add3"].ToString(),
                                CityCode = rdr["CITY_CODE"].ToString(),
                                StateCode = rdr["STATE_CODE"].ToString(),
                                GSTIN = rdr["GSTIN"].ToString(),
                                Pincode = rdr["PINCODE"].ToString()
                            };
                        }
                    }
                }
            }

            return Json(billDetails);
        }


        [HttpPost]
        public JsonResult GetShipDetailsAddLine1(int code, int AddressID)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query = @"
        SELECT ADD1, ADD2, Add3, CITY_CODE, STATE_CODE, GSTIN, PINCODE 
        FROM SUBGROUP_ADDRESS 
        WHERE COMP_CODE = @CompCode AND CODE = @Code AND ADDRESS_ID = @AddressID";

            object billDetails = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@AddressID", AddressID);

                    con.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            billDetails = new
                            {
                                Address1 = rdr["ADD1"].ToString(),
                                Address2 = rdr["ADD2"].ToString(),
                                Address3 = rdr["Add3"].ToString(),
                                CityCode = rdr["CITY_CODE"].ToString(),
                                StateCode = rdr["STATE_CODE"].ToString(),
                                GSTIN = rdr["GSTIN"].ToString(),
                                Pincode = rdr["PINCODE"].ToString()
                            };
                        }
                    }
                }
            }

            return Json(billDetails);
        }


        [HttpGet]
        public JsonResult GetddlCityBillDetails()
        {
            string query = $@" Select a.CODE, a.NAME from CITY_MAST a left join STATE_MAST b on a.STATE_CODE=b.CODE 
            left join COUNTRY_MAST c on a.COUNTRY_CODE=c.CODE where a.ACTIVE=1 and b.ACTIVE=1 and c.ACTIVE=1 Order by a.NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlstateBillDetails()
        {
            string query = $@" select a.CODE, a.NAME from STATE_MAST a left join COUNTRY_MAST b on a.COUNTRY_CODE=b.CODE where a.ACTIVE=1 and b.ACTIVE=1  Order by a.NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlCityShipDetails()
        {
            string query = $@" Select a.CODE, a.NAME from CITY_MAST a left join STATE_MAST b on a.STATE_CODE=b.CODE 
            left join COUNTRY_MAST c on a.COUNTRY_CODE=c.CODE where a.ACTIVE=1 and b.ACTIVE=1 and c.ACTIVE=1 Order by a.NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetddlstateShipDetails()
        {
            string query = $@" select a.CODE, a.NAME from STATE_MAST a left join COUNTRY_MAST b on a.COUNTRY_CODE=b.CODE where a.ACTIVE=1 and b.ACTIVE=1  Order by a.NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlShipDetails()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by name asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //Transport Name ddl banding
        public JsonResult GetddlTransportName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select Code, NAME From TRANSPORT_MAST where COMP_CODE={globalVar.PubCompCode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //Banding Tab1 Item Name List

        public JsonResult GetddlOrdertype()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" Select Code,NAME from DOCTYPE_MAST where DOCTYPE='PurchaseOrder'";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetItemList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string sql = @"SELECT a.CODE AS Code, a.NAME AS Name FROM ITEM_MAST a LEFT JOIN ITEM_MAKE b ON a.code = b.ITEM_CODE AND b.COMP_CODE = @Comp
            LEFT JOIN ITEMUNIT_MAST c ON a.UNIT_CODE = c.CODE AND c.COMP_CODE = @Comp LEFT JOIN ITEM_MGROUP d ON a.MGROUP_CODE = d.CODE AND d.COMP_CODE = @Comp
            WHERE a.NAME <> '' AND a.NAME <> '.' AND a.COMP_CODE = 1 GROUP BY a.NAME, a.CODE ORDER BY a.NAME ASC;";
            //string sql = @"SELECT a.CODE AS Code, a.NAME AS Name FROM ITEM_MAST a LEFT JOIN ITEM_MAKE b ON a.code = b.ITEM_CODE AND b.COMP_CODE = @Comp
            //LEFT JOIN ITEMUNIT_MAST c ON a.UNIT_CODE = c.CODE AND c.COMP_CODE = @Comp
            //LEFT JOIN ITEM_MGROUP d ON a.MGROUP_CODE = d.CODE AND d.COMP_CODE = @Comp
            //WHERE a.COMP_CODE = @Comp";
            var list = new List<object>();
            using (var con = _dbConnection.GetErpConnection())
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Comp", gv.PubCompCode);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new
                        {
                            Code = rdr["Code"].ToString(),
                            Name = rdr["Name"].ToString()
                        });
                    }
                }
            }
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetHSNCode(int code)
        {
            var result = new { hsnCode = "", unit = "" };
            string sql = @"SELECT a.HSN_CODE, b.NAME AS UNIT_NAME
        FROM ITEM_MAST a LEFT JOIN ITEMUNIT_MAST b ON a.UNIT_CODE = b.CODE AND b.COMP_CODE = a.COMP_CODE
        WHERE a.CODE = @Code AND a.COMP_CODE = @CompCode
        ";

            var gv = _globalVariableService.GetGlobalVariables(); // for COMP_CODE
            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);

                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        result = new
                        {
                            hsnCode = rdr["HSN_CODE"]?.ToString() ?? "",
                            unit = rdr["UNIT_NAME"]?.ToString() ?? ""
                        };
                    }
                }
            }
            return Json(result);
        }
        public JsonResult GetTaxTypeList()
        {
            string sql = @"Select Code, NAME From TAX_MAST";
            var list = new List<object>();
            using (var con = _dbConnection.GetErpConnection())
            using (var cmd = new SqlCommand(sql, con))
            {
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new
                        {
                            Code = rdr["Code"].ToString(),
                            Name = rdr["Name"].ToString()
                        });
                    }
                }
            }
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetTaxTypeDetails(string code)
        {
            bool isNumeric = int.TryParse(code, out int codeValue);
            string sql;
            SqlCommand cmd;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                if (isNumeric)
                {
                sql = @" SELECT CODE, CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 
                FROM TAX_MAST WHERE CODE = @Code";

                    cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@Code", codeValue);
                }
                else
                {
                sql = @" SELECT CODE, CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 
                FROM TAX_MAST WHERE NAME = @Name";

                    cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@Name", code);
                }

                con.Open();

                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        var result = new
                        {
                            Code = rdr["CODE"],
                            CGST_PER = rdr["CGST_PER"],
                            SGST_PER = rdr["SGST_PER"],
                            IGST_PER = rdr["IGST_PER"],
                            TDS_PER = rdr["TDS_PER"],
                            TCS_PER = rdr["TCS_PER"],
                            VAT_PER = rdr["VAT_PER"],
                            OTH_PER = rdr["OTH_PER"],
                            OTH_PER2 = rdr["OTH_PER2"]
                        };

                        return Json(result);
                    }
                    else
                    {
                        return Json(new { success = false, message = "No record found" });
                    }
                }
            }
        }


        //public async Task<IActionResult> SaveAllData(
        //[FromForm] string Header,
        //[FromForm] List<ItemDetailModel> ItemDetails,
        //[FromForm] List<AttachmentModel> Attachments)
        //{
        //    var headerObj = JsonConvert.DeserializeObject<PurchaseReceiptHeaderModel>(Header);
        //    var globalVar = _globalVariableService.GetGlobalVariables();
        //    if (headerObj.ACTION == "INSERT")
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            await con.OpenAsync();

        //            using (var transaction = con.BeginTransaction())
        //            {
        //                try
        //                {
        //                    string V_NO = "";
        //                    string DOC_ID = "";
        //                    //using (var cmdGetVNo = new SqlCommand("sp_GetPURCHASEVNo", con, transaction))
        //                    //{
        //                    //    cmdGetVNo.CommandType = CommandType.StoredProcedure;
        //                    //    cmdGetVNo.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                    //    cmdGetVNo.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

        //                    //    var outParam = new SqlParameter("@NewVNo", SqlDbType.VarChar, 50)
        //                    //    {
        //                    //        Direction = ParameterDirection.Output
        //                    //    };
        //                    //    cmdGetVNo.Parameters.Add(outParam);

        //                    //    await cmdGetVNo.ExecuteNonQueryAsync();
        //                    //    V_NO = outParam.Value.ToString();
        //                    //    DOC_ID = headerObj.DocType + V_NO;
        //                    //}
        //                    DOC_ID = headerObj.DocType + headerObj.DocNo;


        //                    // Insert Header
        //                    using (var cmdHeader = new SqlCommand("InsertPurchaseReceiptHeader", con, transaction))
        //                    {
        //                        cmdHeader.CommandType = CommandType.StoredProcedure;

        //                        AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
        //                        AddParameterSafe(cmdHeader, "@BRANCH_CODE", 1);
        //                        AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
        //                        AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
        //                        AddParameterSafe(cmdHeader, "@V_NO", headerObj.DocNo);
        //                        AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
        //                        AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
        //                        AddParameterSafe(cmdHeader, "@EXCH_RATE", headerObj.ExchangeRate);
        //                        // add new Line
        //                        AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.BillFrom);

        //                        AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.AddLine1);
        //                        AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.AddLine2);
        //                        AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.AddLine3);
        //                        AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.City);
        //                        AddParameterSafe(cmdHeader, "@BILL_PINCODE", headerObj.Pincode);
        //                        AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.State);
        //                        AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.GST);
        //                        AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);

        //                        AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipFrom);
        //                        AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
        //                        AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
        //                        AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
        //                        AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
        //                        AddParameterSafe(cmdHeader, "@SHIP_PINCODE", headerObj.ShipPincode);
        //                        AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.ShipState);

        //                        // add new Line

        //                        AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
        //                        AddParameterSafe(cmdHeader, "@BILL_DATE", DateTime.Parse(headerObj.BillDate));
        //                        AddParameterSafe(cmdHeader, "@CHALL_NO", headerObj.ChallanNo);
        //                        AddParameterSafe(cmdHeader, "@CHALL_DATE", DateTime.Parse(headerObj.ChallanDate));
        //                        AddParameterSafe(cmdHeader, "@GATE_NO", headerObj.GateNo);

        //                        AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);

        //                        AddParameterSafe(cmdHeader, "@TRANSPORT_NAME", headerObj.TransportName);
        //                        AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo);
        //                        AddParameterSafe(cmdHeader, "@GR_DATE", headerObj.GRDate);

        //                        AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
        //                        AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
        //                        AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
        //                        AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
        //                        AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
        //                        AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
        //                        AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);
        //                        AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);

        //                        // Default values
        //                        AddParameterSafe(cmdHeader, "@STATUS", headerObj.DocStatus);
        //                        AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty);
        //                        AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty);
        //                        AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount);
        //                        AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount);
        //                        AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking);
        //                        AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST);
        //                        AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST);
        //                        AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST);
        //                        AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS);
        //                        AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT);
        //                        AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt);
        //                        AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1);
        //                        AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2);
        //                        AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff);

        //                        AddParameterSafe(cmdHeader, "@UUSER", globalVar.PubUserId);
        //                        AddParameterSafe(cmdHeader, "@UDATE", DateTime.Now);
        //                        AddParameterSafe(cmdHeader, "@EUSER", "");
        //                        AddParameterSafe(cmdHeader, "@EDATE", "");
        //                        AddParameterSafe(cmdHeader, "@AED", "A");
        //                        AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
        //                        AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
        //                        AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);

        //                        AddParameterSafe(cmdHeader, "@Action", "Insert");

        //                        await cmdHeader.ExecuteNonQueryAsync();
        //                    }
        //                    // Insert Items
        //                    int serialNo = 1;
        //                    foreach (var item in ItemDetails)
        //                    {
        //                        using (var cmdItem = new SqlCommand("InsertPurchaseItemDetail", con, transaction))
        //                        {
        //                            cmdItem.CommandType = CommandType.StoredProcedure;

        //                            AddParameterSafe(cmdItem, "@V_NO", V_NO);
        //                            AddParameterSafe(cmdItem, "@DOC_ID", DOC_ID);
        //                            AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType);
        //                            AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));
        //                            AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
        //                            AddParameterSafe(cmdItem, "@BRANCH_CODE", 1);
        //                            AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
        //                            AddParameterSafe(cmdItem, "@SNO", serialNo++);

        //                            AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemName);
        //                            AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName);
        //                            AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode);
        //                            AddParameterSafe(cmdItem, "@UOM_NAME", item.UOMName);
        //                            AddParameterSafe(cmdItem, "@NOS", item.Nos);
        //                            AddParameterSafe(cmdItem, "@PLUS_MINUSQTY", item.PlusMinusQty);
        //                            AddParameterSafe(cmdItem, "@RECD_QTY", item.RecQty);
        //                            AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty);
        //                            AddParameterSafe(cmdItem, "@USD_RATE", item.USDRate);
        //                            AddParameterSafe(cmdItem, "@EXCH_RATE", item.ExRate);
        //                            AddParameterSafe(cmdItem, "@RATE", item.Rate);
        //                            AddParameterSafe(cmdItem, "@AMOUNT", item.Amount);
        //                            AddParameterSafe(cmdItem, "@EMPTY_YN", item.EmptyYN);
        //                            AddParameterSafe(cmdItem, "@WB_QTY", item.WBQty);
        //                            AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer);
        //                            AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt);
        //                            AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer);
        //                            AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt);
        //                            AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer);
        //                            AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt);
        //                            AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer);
        //                            AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt);
        //                            AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer);
        //                            AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt);
        //                            AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer);
        //                            AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt);
        //                            AddParameterSafe(cmdItem, "@VAT_PER", item.VATPer);
        //                            AddParameterSafe(cmdItem, "@VAT_AMT", item.VATAmt);
        //                            AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt);
        //                            AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt);
        //                            AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate);
        //                            AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt);
        //                            AddParameterSafe(cmdItem, "@BIN_LOCATION", item.BinLocation);
        //                            AddParameterSafe(cmdItem, "@PO_TYPE", item.POType);
        //                            AddParameterSafe(cmdItem, "@PO_NO", item.PONo);
        //                            AddParameterSafe(cmdItem, "@KANTA_TYPE", item.KantaType);
        //                            AddParameterSafe(cmdItem, "@KANTA_NO", item.KantaNo);
        //                            AddParameterSafe(cmdItem, "@REQ_TYPE", item.ReqType);
        //                            AddParameterSafe(cmdItem, "@REQ_NO", item.ReqNo);
        //                            AddParameterSafe(cmdItem, "@GATE_TYPE", headerObj.DocType);
        //                            AddParameterSafe(cmdItem, "@GATE_NO", headerObj.GateNo);
        //                            AddParameterSafe(cmdItem, "@BIN_CODE", item.BinCode);
        //                            AddParameterSafe(cmdItem, "@MAKE_CODE", item.MakeCode);
        //                            AddParameterSafe(cmdItem, "@TAX_CODE", item.TaxCode);
        //                            AddParameterSafe(cmdItem, "@UOM_CODE", item.UOMCode);
        //                            AddParameterSafe(cmdItem, "@DEPT_CODE", item.DeptCode);
        //                            AddParameterSafe(cmdItem, "@REMARKS", item.Remarks);

        //                            AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
        //                            AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
        //                            AddParameterSafe(cmdItem, "@EUSER", "");
        //                            AddParameterSafe(cmdItem, "@EDATE", "");
        //                            AddParameterSafe(cmdItem, "@AED", "A");
        //                            AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
        //                            AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
        //                            AddParameterSafe(cmdItem, "@LID", Environment.MachineName);

        //                            AddParameterSafe(cmdItem, "@Action", "Insert");

        //                            await cmdItem.ExecuteNonQueryAsync();
        //                        }
        //                    }
        //                    // Insert Image
        //                    foreach (var file in Attachments)
        //                    {
        //                        if (file.File != null && file.File.Length > 0)
        //                        {
        //                            // Save file to disk
        //                            var fileName = Path.GetFileName(file.File.FileName);
        //                            var saveFolder = Path.Combine("wwwroot", "attachments", "Purchase");
        //                            var filePath = Path.Combine(saveFolder, fileName);

        //                            if (!Directory.Exists(saveFolder))
        //                            {
        //                                Directory.CreateDirectory(saveFolder);
        //                            }
        //                            using (var stream = new FileStream(filePath, FileMode.Create))
        //                            {
        //                                await file.File.CopyToAsync(stream);
        //                            }
        //                            // Save record to database
        //                            using (var cmdAttach = new SqlCommand("InsertPURCHASEAttachment", con, transaction))
        //                            {
        //                                cmdAttach.CommandType = CommandType.StoredProcedure;

        //                                AddParameterSafe(cmdAttach, "@COMP_CODE", globalVar.PubCompCode);
        //                                AddParameterSafe(cmdAttach, "@BRANCH_CODE", 1);
        //                                AddParameterSafe(cmdAttach, "@YEAR_CODE", globalVar.PubFYearCode);
        //                                AddParameterSafe(cmdAttach, "@DOC_ID", DOC_ID);
        //                                AddParameterSafe(cmdAttach, "@V_NO", V_NO);
        //                                AddParameterSafe(cmdAttach, "@V_TYPE", headerObj.DocType);
        //                                AddParameterSafe(cmdAttach, "@V_DATE", DateTime.Parse(headerObj.DocDate));
        //                                AddParameterSafe(cmdAttach, "@UUSER", globalVar.PubUserId);
        //                                AddParameterSafe(cmdAttach, "@UDATE", DateTime.Now);
        //                                AddParameterSafe(cmdAttach, "@AED", "A");
        //                                AddParameterSafe(cmdAttach, "@WSID", globalVar.PubWorkStationID);
        //                                AddParameterSafe(cmdAttach, "@LIP", globalVar.PubLocalId);
        //                                AddParameterSafe(cmdAttach, "@LID", Environment.MachineName);
        //                                AddParameterSafe(cmdAttach, "@ATTACHMENT", "/attachments/Purchase/" + fileName);
        //                                //AddParameterSafe(cmdAttach, "@Action", "Insert");
        //                                await cmdAttach.ExecuteNonQueryAsync();
        //                            }
        //                        }
        //                    }
        //                    transaction.Commit();
        //                    return Ok(new { status = "success", message = "Saved successfully" });
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();
        //                    return BadRequest(new { status = "error", message = ex.Message });
        //                }
        //            }
        //        }
        //    }
        //    else if (headerObj.ACTION == "UPDATE")
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            await con.OpenAsync();
        //            using (var transaction = con.BeginTransaction())
        //            {
        //                try
        //                {
        //                   // Insert Header
        //                    using (var cmdHeader = new SqlCommand("InsertPurchaseReceiptHeader", con, transaction))
        //                    {
        //                        cmdHeader.CommandType = CommandType.StoredProcedure;

        //                        AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
        //                        AddParameterSafe(cmdHeader, "@BRANCH_CODE", 1);
        //                        AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
        //                        AddParameterSafe(cmdHeader, "@DOC_ID", headerObj.DocNo);
        //                        AddParameterSafe(cmdHeader, "@V_NO", headerObj.code);
        //                        AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
        //                        AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
        //                        AddParameterSafe(cmdHeader, "@EXCH_RATE", headerObj.ExchangeRate);
        //                        // add new Line
        //                        AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.AddLine1);
        //                        AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.AddLine2);
        //                        AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.AddLine3);
        //                        AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.City);
        //                        AddParameterSafe(cmdHeader, "@BILL_PINCODE", headerObj.Pincode);
        //                        AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.State);
        //                        AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.GST);
        //                        AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);

        //                        AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipFrom);
        //                        AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
        //                        AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
        //                        AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
        //                        AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
        //                        AddParameterSafe(cmdHeader, "@SHIP_PINCODE", headerObj.ShipPincode);
        //                        AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.ShipState);

        //                        // add new Line

        //                        AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
        //                        AddParameterSafe(cmdHeader, "@BILL_DATE", DateTime.Parse(headerObj.BillDate));
        //                        AddParameterSafe(cmdHeader, "@CHALL_NO", headerObj.ChallanNo);
        //                        AddParameterSafe(cmdHeader, "@CHALL_DATE", DateTime.Parse(headerObj.ChallanDate));
        //                        AddParameterSafe(cmdHeader, "@GATE_NO", headerObj.GateNo);

        //                        AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);

        //                        AddParameterSafe(cmdHeader, "@TRANSPORT_NAME", headerObj.TransportName);
        //                        AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo);
        //                        AddParameterSafe(cmdHeader, "@GR_DATE", headerObj.GRDate);

        //                        AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
        //                        AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
        //                        AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
        //                        AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
        //                        AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
        //                        AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
        //                        AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);
        //                        AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);

        //                        // Default values
        //                        AddParameterSafe(cmdHeader, "@STATUS", 0);
        //                        AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty);
        //                        AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty);
        //                        AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount);
        //                        AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount);
        //                        AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking);
        //                        AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST);
        //                        AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST);
        //                        AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST);
        //                        AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS);
        //                        AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT);
        //                        AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt);
        //                        AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1);
        //                        AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2);
        //                        AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff);

        //                        AddParameterSafe(cmdHeader, "@UUSER", globalVar.PubUserId);
        //                        AddParameterSafe(cmdHeader, "@UDATE", DateTime.Now);
        //                        AddParameterSafe(cmdHeader, "@EUSER", "");
        //                        AddParameterSafe(cmdHeader, "@EDATE", "");
        //                        AddParameterSafe(cmdHeader, "@AED", "A");
        //                        AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
        //                        AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
        //                        AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);

        //                        AddParameterSafe(cmdHeader, "@Action", "Update");

        //                        await cmdHeader.ExecuteNonQueryAsync();
        //                    }
        //                    // Insert Items

        //                    using (SqlCommand ItemDetailDelete = new SqlCommand("DELETE FROM PURCHASE2 WHERE COMP_CODE = @COMP_CODE AND V_NO = @V_NO and DOC_ID= @DOC_ID and YEAR_CODE= @YEAR_CODE ", con, transaction))
        //                    {
        //                        ItemDetailDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                        ItemDetailDelete.Parameters.AddWithValue("@V_NO", headerObj.code);
        //                        ItemDetailDelete.Parameters.AddWithValue("@DOC_ID", headerObj.DocNo);
        //                        ItemDetailDelete.Parameters.AddWithValue( "@YEAR_CODE", globalVar.PubFYearCode);
        //                        ItemDetailDelete.ExecuteNonQuery();
        //                    }

        //                    int serialNo = 1;
        //                    foreach (var item in ItemDetails)
        //                    {
        //                        using (var cmdItem = new SqlCommand("InsertPurchaseItemDetail", con, transaction))
        //                        {
        //                            cmdItem.CommandType = CommandType.StoredProcedure;

        //                            AddParameterSafe(cmdItem, "@V_NO", headerObj.code);
        //                            AddParameterSafe(cmdItem, "@DOC_ID", headerObj.DocNo);
        //                            AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType);
        //                            AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));
        //                            AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
        //                            AddParameterSafe(cmdItem, "@BRANCH_CODE", 1);
        //                            AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
        //                            AddParameterSafe(cmdItem, "@SNO", serialNo++);

        //                            AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemName);
        //                            AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName);
        //                            AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode);
        //                            AddParameterSafe(cmdItem, "@UOM_NAME", item.UOMName);
        //                            AddParameterSafe(cmdItem, "@NOS", item.Nos);
        //                            AddParameterSafe(cmdItem, "@PLUS_MINUSQTY", item.PlusMinusQty);
        //                            AddParameterSafe(cmdItem, "@RECD_QTY", item.RecQty);
        //                            AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty);
        //                            AddParameterSafe(cmdItem, "@USD_RATE", item.USDRate);
        //                            AddParameterSafe(cmdItem, "@EXCH_RATE", item.ExRate);
        //                            AddParameterSafe(cmdItem, "@RATE", item.Rate);
        //                            AddParameterSafe(cmdItem, "@AMOUNT", item.Amount);
        //                            AddParameterSafe(cmdItem, "@EMPTY_YN", item.EmptyYN);
        //                            AddParameterSafe(cmdItem, "@WB_QTY", item.WBQty);
        //                            AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer);
        //                            AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt);
        //                            AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer);
        //                            AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt);
        //                            AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer);
        //                            AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt);
        //                            AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer);
        //                            AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt);
        //                            AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer);
        //                            AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt);
        //                            AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer);
        //                            AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt);
        //                            AddParameterSafe(cmdItem, "@VAT_PER", item.VATPer);
        //                            AddParameterSafe(cmdItem, "@VAT_AMT", item.VATAmt);
        //                            AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt);
        //                            AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt);
        //                            AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate);
        //                            AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt);
        //                            AddParameterSafe(cmdItem, "@BIN_LOCATION", item.BinLocation);
        //                            AddParameterSafe(cmdItem, "@PO_TYPE", item.POType);
        //                            AddParameterSafe(cmdItem, "@PO_NO", item.PONo);
        //                            AddParameterSafe(cmdItem, "@KANTA_TYPE", item.KantaType);
        //                            AddParameterSafe(cmdItem, "@KANTA_NO", item.KantaNo);
        //                            AddParameterSafe(cmdItem, "@REQ_TYPE", item.ReqType);
        //                            AddParameterSafe(cmdItem, "@REQ_NO", item.ReqNo);
        //                            AddParameterSafe(cmdItem, "@GATE_TYPE", item.GateType);
        //                            AddParameterSafe(cmdItem, "@GATE_NO", item.GateNo);
        //                            AddParameterSafe(cmdItem, "@BIN_CODE", item.BinCode);
        //                            AddParameterSafe(cmdItem, "@MAKE_CODE", item.MakeCode);
        //                            AddParameterSafe(cmdItem, "@TAX_CODE", item.TaxCode);
        //                            AddParameterSafe(cmdItem, "@UOM_CODE", item.UOMCode);
        //                            AddParameterSafe(cmdItem, "@DEPT_CODE", item.DeptCode);
        //                            AddParameterSafe(cmdItem, "@REMARKS", item.Remarks);

        //                            AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
        //                            AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
        //                            AddParameterSafe(cmdItem, "@EUSER", "");
        //                            AddParameterSafe(cmdItem, "@EDATE", "");
        //                            AddParameterSafe(cmdItem, "@AED", "A");
        //                            AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
        //                            AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
        //                            AddParameterSafe(cmdItem, "@LID", Environment.MachineName);
        //                            AddParameterSafe(cmdItem, "@Action", "Insert");
        //                            await cmdItem.ExecuteNonQueryAsync();
        //                        }
        //                    }
        //                    // Insert Image

        //                    using (SqlCommand deleteAttachments = new SqlCommand("DELETE FROM PURCHASE3 WHERE COMP_CODE = @COMP_CODE AND V_NO = @V_NO and DOC_ID= @DOC_ID and YEAR_CODE= @YEAR_CODE ", con, transaction))
        //                    {
        //                        deleteAttachments.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                        deleteAttachments.Parameters.AddWithValue("@V_NO", headerObj.code);
        //                        deleteAttachments.Parameters.AddWithValue("@DOC_ID", headerObj.DocNo);
        //                        deleteAttachments.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
        //                        deleteAttachments.ExecuteNonQuery();
        //                    }

        //                    foreach (var file in Attachments)
        //                    {
        //                        if (file.File != null && file.File.Length > 0)
        //                        {
        //                            // Save file to disk
        //                            var fileName = Path.GetFileName(file.File.FileName);
        //                            var saveFolder = Path.Combine("wwwroot", "attachments", "Purchase");
        //                            var filePath = Path.Combine(saveFolder, fileName);

        //                            if (!Directory.Exists(saveFolder))
        //                            {
        //                                Directory.CreateDirectory(saveFolder);
        //                            }
        //                            using (var stream = new FileStream(filePath, FileMode.Create))
        //                            {
        //                                await file.File.CopyToAsync(stream);
        //                            }
        //                            // Save record to database
        //                            using (var cmdAttach = new SqlCommand("InsertPURCHASEAttachment", con, transaction))
        //                            {
        //                                cmdAttach.CommandType = CommandType.StoredProcedure;

        //                                AddParameterSafe(cmdAttach, "@COMP_CODE", globalVar.PubCompCode);
        //                                AddParameterSafe(cmdAttach, "@BRANCH_CODE", 1);
        //                                AddParameterSafe(cmdAttach, "@YEAR_CODE", globalVar.PubFYearCode);
        //                                AddParameterSafe(cmdAttach, "@DOC_ID", headerObj.DocNo);
        //                                AddParameterSafe(cmdAttach, "@V_NO", headerObj.code);
        //                                AddParameterSafe(cmdAttach, "@V_TYPE", headerObj.DocType);
        //                                AddParameterSafe(cmdAttach, "@V_DATE", DateTime.Parse(headerObj.DocDate));
        //                                AddParameterSafe(cmdAttach, "@UUSER", globalVar.PubUserId);
        //                                AddParameterSafe(cmdAttach, "@UDATE", DateTime.Now);
        //                                AddParameterSafe(cmdAttach, "@AED", "A");
        //                                AddParameterSafe(cmdAttach, "@WSID", globalVar.PubWorkStationID);
        //                                AddParameterSafe(cmdAttach, "@LIP", globalVar.PubLocalId);
        //                                AddParameterSafe(cmdAttach, "@LID", Environment.MachineName);
        //                                AddParameterSafe(cmdAttach, "@ATTACHMENT", "/attachments/Purchase/" + fileName);
        //                                //AddParameterSafe(cmdAttach, "@Action", "Insert");
        //                                await cmdAttach.ExecuteNonQueryAsync();
        //                            }
        //                        }
        //                    }
        //                    transaction.Commit();
        //                    return Ok(new { status = "success", message = "Update successfully" });
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();
        //                    return BadRequest(new { status = "error", message = ex.Message });
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        return Json(new { success = false, message = "Invalid action specified." });
        //    }
        //}


        public async Task<IActionResult> SaveAllData(
    [FromForm] string Header,
    [FromForm] List<ItemDetailModel> ItemDetails,
    [FromForm] List<AttachmentModel> Attachments)
        {
            var headerObj = JsonConvert.DeserializeObject<PurchaseReceiptHeaderModel>(Header);
            var globalVar = _globalVariableService.GetGlobalVariables();

            if (headerObj.ACTION == "INSERT")
            {
                // Check if record already exists
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string checkQuery = @"
                SELECT COUNT(*) 
                FROM PURCHASE1 
                WHERE COMP_CODE = @COMP_CODE 
                  AND YEAR_CODE = @YEAR_CODE 
                  AND BRANCH_CODE = @BRANCH_CODE 
                  AND V_TYPE = @V_TYPE 
                  AND V_NO = @V_NO";

                    using (var cmd = new SqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1); // You can replace 1 with globalVar.PubBranchCode if dynamic
                        cmd.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                        cmd.Parameters.AddWithValue("@V_NO", headerObj.DocNo);

                        int count = (int)await cmd.ExecuteScalarAsync();

                        if (count > 0)
                        {
                            return BadRequest(new
                            {
                                status = "exists",
                                message = "Record already exists in PURCHASE1."
                            });
                        }
                    }

                    // Begin Insert Transaction
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            string V_NO = headerObj.DocNo;
                            string DOC_ID = headerObj.DocType + headerObj.DocNo;

                            // Insert Header
                            using (var cmdHeader = new SqlCommand("InsertPurchaseReceiptHeader", con, transaction))
                            {
                                cmdHeader.CommandType = CommandType.StoredProcedure;
                                AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
                                AddParameterSafe(cmdHeader, "@BRANCH_CODE", 1);
                                AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
                                AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
                                AddParameterSafe(cmdHeader, "@V_NO", headerObj.DocNo);
                                AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                AddParameterSafe(cmdHeader, "@EXCH_RATE", headerObj.ExchangeRate);
                                // add new Line
                                AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.BillFrom);

                                AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.AddLine1);
                                AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.AddLine2);
                                AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.AddLine3);
                                AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.City);
                                AddParameterSafe(cmdHeader, "@BILL_PINCODE", headerObj.Pincode);
                                AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.State);
                                AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.GST);
                                AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);

                                AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipFrom);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
                                AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
                                AddParameterSafe(cmdHeader, "@SHIP_PINCODE", headerObj.ShipPincode);
                                AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.ShipState);

                                // add new Line

                                AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
                                AddParameterSafe(cmdHeader, "@BILL_DATE", DateTime.Parse(headerObj.BillDate));
                                AddParameterSafe(cmdHeader, "@CHALL_NO", headerObj.ChallanNo);
                                //AddParameterSafe(cmdHeader, "@CHALL_DATE", DateTime.Parse(headerObj.ChallanDate));
                                AddParameterSafe(cmdHeader, "@CHALL_DATE", string.IsNullOrWhiteSpace(headerObj.ChallanDate) ? (object)DBNull.Value : DateTime.Parse(headerObj.ChallanDate));
                                AddParameterSafe(cmdHeader, "@GATE_NO", headerObj.GateNo);

                                AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);

                                AddParameterSafe(cmdHeader, "@TRANSPORT_NAME", headerObj.TransportName);
                                AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo);
                                AddParameterSafe(cmdHeader, "@GR_DATE", headerObj.GRDate);

                                AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
                                AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
                                AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
                                AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
                                AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);
                                AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);

                                // Default values
                                AddParameterSafe(cmdHeader, "@STATUS", headerObj.DocStatus);
                                AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty);
                                AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty);
                                AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount);
                                AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount);
                                AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking);
                                AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST);
                                AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST);
                                AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST);
                                AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS);
                                AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT);
                                AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt);
                                AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1);
                                AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2);
                                AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff);

                                AddParameterSafe(cmdHeader, "@UUSER", globalVar.PubUserId);
                                AddParameterSafe(cmdHeader, "@UDATE", DateTime.Now);
                                AddParameterSafe(cmdHeader, "@EUSER", "");
                                AddParameterSafe(cmdHeader, "@EDATE", "");
                                AddParameterSafe(cmdHeader, "@AED", "A");
                                AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
                                AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
                                AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);

                                AddParameterSafe(cmdHeader, "@Action", "Insert");

                                await cmdHeader.ExecuteNonQueryAsync();
                            }

                            // Insert Items
                            int serialNo = 1;
                            foreach (var item in ItemDetails)
                            {
                                using (var cmdItem = new SqlCommand("InsertPurchaseItemDetail", con, transaction))
                                {
                                    cmdItem.CommandType = CommandType.StoredProcedure;

                                    AddParameterSafe(cmdItem, "@V_NO", V_NO);
                                    AddParameterSafe(cmdItem, "@DOC_ID", DOC_ID);
                                    AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                    AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdItem, "@BRANCH_CODE", 1);
                                    AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                    AddParameterSafe(cmdItem, "@SNO", serialNo++);

                                    AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemName);
                                    AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName);
                                    AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode);
                                    AddParameterSafe(cmdItem, "@UOM_NAME", item.UOMName);
                                    AddParameterSafe(cmdItem, "@NOS", item.Nos);
                                    AddParameterSafe(cmdItem, "@PLUS_MINUSQTY", item.PlusMinusQty);
                                    AddParameterSafe(cmdItem, "@RECD_QTY", item.RecQty);
                                    AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty);
                                    AddParameterSafe(cmdItem, "@USD_RATE", item.USDRate);
                                    AddParameterSafe(cmdItem, "@EXCH_RATE", item.ExRate);
                                    AddParameterSafe(cmdItem, "@RATE", item.Rate);
                                    AddParameterSafe(cmdItem, "@AMOUNT", item.Amount);
                                    AddParameterSafe(cmdItem, "@EMPTY_YN", item.EmptyYN);
                                    AddParameterSafe(cmdItem, "@WB_QTY", item.WBQty);
                                    AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer);
                                    AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt);
                                    AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer);
                                    AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt);
                                    AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer);
                                    AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt);
                                    AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer);
                                    AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt);
                                    AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer);
                                    AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt);
                                    AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer);
                                    AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt);
                                    AddParameterSafe(cmdItem, "@VAT_PER", item.VATPer);
                                    AddParameterSafe(cmdItem, "@VAT_AMT", item.VATAmt);
                                    AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt);
                                    AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt);
                                    AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate);
                                    AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt);
                                    AddParameterSafe(cmdItem, "@BIN_LOCATION", item.BinLocation);
                                    AddParameterSafe(cmdItem, "@PO_TYPE", item.POType);
                                    AddParameterSafe(cmdItem, "@PO_NO", item.PONo);
                                    AddParameterSafe(cmdItem, "@KANTA_TYPE", item.KantaType);
                                    AddParameterSafe(cmdItem, "@KANTA_NO", item.KantaNo);
                                    AddParameterSafe(cmdItem, "@REQ_TYPE", item.ReqType);
                                    AddParameterSafe(cmdItem, "@REQ_NO", item.ReqNo);
                                    AddParameterSafe(cmdItem, "@GATE_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdItem, "@GATE_NO", headerObj.GateNo);
                                    AddParameterSafe(cmdItem, "@BIN_CODE", item.BinCode);
                                    AddParameterSafe(cmdItem, "@MAKE_CODE", item.MakeCode);
                                    AddParameterSafe(cmdItem, "@TAX_CODE", item.TaxCode);
                                    AddParameterSafe(cmdItem, "@UOM_CODE", item.UOMCode);
                                    AddParameterSafe(cmdItem, "@DEPT_CODE", item.DeptCode);
                                    AddParameterSafe(cmdItem, "@REMARKS", item.Remarks);

                                    AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
                                    AddParameterSafe(cmdItem, "@EUSER", "");
                                    AddParameterSafe(cmdItem, "@EDATE", "");
                                    AddParameterSafe(cmdItem, "@AED", "A");
                                    AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdItem, "@LID", Environment.MachineName);

                                    AddParameterSafe(cmdItem, "@Action", "Insert");

                                    await cmdItem.ExecuteNonQueryAsync();
                                }
                            }

                            // TODO: Insert Attachment Logic here (if needed)

                            // Commit Transaction
                            transaction.Commit();

                            return Ok(new
                            {
                                status = "success",
                                message = "Purchase Receipt saved successfully."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(500, new
                            {
                                status = "error",
                                message = "An error occurred while saving the data.",
                                error = ex.Message
                            });
                        }
                    }
                }
            }
            else if (headerObj.ACTION == "UPDATE")
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // Begin Insert Transaction
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Insert Header
                            using (var cmdHeader = new SqlCommand("InsertPurchaseReceiptHeader", con, transaction))
                            {
                                cmdHeader.CommandType = CommandType.StoredProcedure;
                                AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
                                AddParameterSafe(cmdHeader, "@BRANCH_CODE", 1);
                                AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
                                AddParameterSafe(cmdHeader, "@DOC_ID", headerObj.DocNo);
                                AddParameterSafe(cmdHeader, "@V_NO", headerObj.code);
                                AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                AddParameterSafe(cmdHeader, "@EXCH_RATE", headerObj.ExchangeRate);
                                // add new Line
                                AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.BillFrom);

                                AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.AddLine1);
                                AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.AddLine2);
                                AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.AddLine3);
                                AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.City);
                                AddParameterSafe(cmdHeader, "@BILL_PINCODE", headerObj.Pincode);
                                AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.State);
                                AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.GST);
                                AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);

                                AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipFrom);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
                                AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
                                AddParameterSafe(cmdHeader, "@SHIP_PINCODE", headerObj.ShipPincode);
                                AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.ShipState);

                                // add new Line

                                AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
                                AddParameterSafe(cmdHeader, "@BILL_DATE", DateTime.Parse(headerObj.BillDate));
                                AddParameterSafe(cmdHeader, "@CHALL_NO", headerObj.ChallanNo);
                                //AddParameterSafe(cmdHeader, "@CHALL_DATE", DateTime.Parse(headerObj.ChallanDate));
                                AddParameterSafe(cmdHeader, "@CHALL_DATE", string.IsNullOrWhiteSpace(headerObj.ChallanDate) ? (object)DBNull.Value : DateTime.Parse(headerObj.ChallanDate));
                                AddParameterSafe(cmdHeader, "@GATE_NO", headerObj.GateNo);
                                AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);
                                AddParameterSafe(cmdHeader, "@TRANSPORT_NAME", headerObj.TransportName);
                                AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo);
                                AddParameterSafe(cmdHeader, "@GR_DATE", headerObj.GRDate);

                                AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
                                AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
                                AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
                                AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
                                AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);
                                AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);

                                // Default values
                                AddParameterSafe(cmdHeader, "@STATUS", headerObj.DocStatus);
                                AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty);
                                AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty);
                                AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount);
                                AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount);
                                AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking);
                                AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST);
                                AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST);
                                AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST);
                                AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS);
                                AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT);
                                AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt);
                                AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1);
                                AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2);
                                AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff);

                                AddParameterSafe(cmdHeader, "@UUSER", globalVar.PubUserId);
                                AddParameterSafe(cmdHeader, "@UDATE", DateTime.Now);
                                AddParameterSafe(cmdHeader, "@EUSER", "");
                                AddParameterSafe(cmdHeader, "@EDATE", "");
                                AddParameterSafe(cmdHeader, "@AED", "A");
                                AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
                                AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
                                AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);

                                AddParameterSafe(cmdHeader, "@Action", "UPDATE");

                                await cmdHeader.ExecuteNonQueryAsync();
                            }

                            // Insert Items

                            string deleteQuery = @" DELETE FROM PURCHASE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE   AND COMP_CODE = @COMP_CODE
                            AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE";

                            using (var cmdDelete = new SqlCommand(deleteQuery, con, transaction))
                            {
                                cmdDelete.Parameters.AddWithValue("@V_NO", headerObj.code);
                                cmdDelete.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                cmdDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmdDelete.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                cmdDelete.Parameters.AddWithValue("@BRANCH_CODE", 1);

                                await cmdDelete.ExecuteNonQueryAsync();
                            }

                            int serialNo = 1;
                            foreach (var item in ItemDetails)
                            {
                                using (var cmdItem = new SqlCommand("InsertPurchaseItemDetail", con, transaction))
                                {
                                    cmdItem.CommandType = CommandType.StoredProcedure;

                                    AddParameterSafe(cmdItem, "@V_NO", headerObj.code);
                                    AddParameterSafe(cmdItem, "@DOC_ID", headerObj.DocNo);
                                    AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                    AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdItem, "@BRANCH_CODE", 1);
                                    AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                    AddParameterSafe(cmdItem, "@SNO", serialNo++);

                                    AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemName);
                                    AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName);
                                    AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode);
                                    AddParameterSafe(cmdItem, "@UOM_NAME", item.UOMName);
                                    AddParameterSafe(cmdItem, "@NOS", item.Nos);
                                    AddParameterSafe(cmdItem, "@PLUS_MINUSQTY", item.PlusMinusQty);
                                    AddParameterSafe(cmdItem, "@RECD_QTY", item.RecQty);
                                    AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty);
                                    AddParameterSafe(cmdItem, "@USD_RATE", item.USDRate);
                                    AddParameterSafe(cmdItem, "@EXCH_RATE", item.ExRate);
                                    AddParameterSafe(cmdItem, "@RATE", item.Rate);
                                    AddParameterSafe(cmdItem, "@AMOUNT", item.Amount);
                                    AddParameterSafe(cmdItem, "@EMPTY_YN", item.EmptyYN);
                                    AddParameterSafe(cmdItem, "@WB_QTY", item.WBQty);
                                    AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer);
                                    AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt);
                                    AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer);
                                    AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt);
                                    AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer);
                                    AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt);
                                    AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer);
                                    AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt);
                                    AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer);
                                    AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt);
                                    AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer);
                                    AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt);
                                    AddParameterSafe(cmdItem, "@VAT_PER", item.VATPer);
                                    AddParameterSafe(cmdItem, "@VAT_AMT", item.VATAmt);
                                    AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt);
                                    AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt);
                                    AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate);
                                    AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt);
                                    AddParameterSafe(cmdItem, "@BIN_LOCATION", item.BinLocation);
                                    AddParameterSafe(cmdItem, "@PO_TYPE", item.POType);
                                    AddParameterSafe(cmdItem, "@PO_NO", item.PONo);
                                    AddParameterSafe(cmdItem, "@KANTA_TYPE", item.KantaType);
                                    AddParameterSafe(cmdItem, "@KANTA_NO", item.KantaNo);
                                    AddParameterSafe(cmdItem, "@REQ_TYPE", item.ReqType);
                                    AddParameterSafe(cmdItem, "@REQ_NO", item.ReqNo);
                                    AddParameterSafe(cmdItem, "@GATE_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdItem, "@GATE_NO", headerObj.GateNo);
                                    AddParameterSafe(cmdItem, "@BIN_CODE", item.BinCode);
                                    AddParameterSafe(cmdItem, "@MAKE_CODE", item.MakeCode);
                                    AddParameterSafe(cmdItem, "@TAX_CODE", item.TaxCode);
                                    AddParameterSafe(cmdItem, "@UOM_CODE", item.UOMCode);
                                    AddParameterSafe(cmdItem, "@DEPT_CODE", item.DeptCode);
                                    AddParameterSafe(cmdItem, "@REMARKS", item.Remarks);

                                    AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
                                    AddParameterSafe(cmdItem, "@EUSER", "");
                                    AddParameterSafe(cmdItem, "@EDATE", "");
                                    AddParameterSafe(cmdItem, "@AED", "A");
                                    AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdItem, "@LID", Environment.MachineName);

                                    AddParameterSafe(cmdItem, "@Action", "Insert");

                                    await cmdItem.ExecuteNonQueryAsync();
                                }
                            }

                            // TODO: Insert Attachment Logic here (if needed)

                            // Commit Transaction
                            transaction.Commit();

                            return Ok(new
                            {
                                status = "success",
                                message = "Purchase Receipt saved successfully."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(500, new
                            {
                                status = "error",
                                message = "An error occurred while saving the data.",
                                error = ex.Message
                            });
                        }
                    }
                }
            }

            return BadRequest(new
            {
                status = "invalid_action",
                message = "Unsupported action provided."
            });
        }
        
        
        [HttpGet]
        //public async Task<IActionResult> GetOrderDetailsList(string StrID, string ItemCode)
        public async Task<IActionResult> GetOrderDetailsList(string StrID, [FromQuery] List<string> ItemCodes)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var results = new List<Dictionary<string, object>>();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (var command = new SqlCommand("usp_GetGatePurchaseEntryDetailsCopyFrom", con))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@V_TYPE", StrID);
                        command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        command.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        command.Parameters.AddWithValue("@PartyCode", ItemCodes.FirstOrDefault());

                        await con.OpenAsync();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                                results.Add(row);
                            }
                        }
                    }
                }
                return Json(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while fetching order details.", error = ex.Message });
            }
        }
        public static void AddParameterSafe(SqlCommand cmd, string paramName, object value)
        {
             try
             {
                 cmd.Parameters.AddWithValue(paramName, value ?? DBNull.Value);
             }
             catch (Exception ex)
             {
                 throw new Exception($"{ex.Message} | Parameter: {paramName}", ex);
             }
        }
        public async Task<IActionResult> GetGatDetailsList(string StrVNo, string StrV_type)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new GatePurchaseDetailsResponse(); 

            string strVType = StrV_type.Substring(0, 4);

            try
            {
                // Validate and parse V_NO
                if (!int.TryParse(StrVNo, out int vNo))
                    return BadRequest("Invalid gate number format.");

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (var command = new SqlCommand("usp_GetGatePurchaseEntryDetails", con))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@V_TYPE", strVType);
                    command.Parameters.AddWithValue("@V_NO", vNo);
                    command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    command.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    await con.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // ----------- Header List -----------
                        while (await reader.ReadAsync())
                        {
                            var header = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                                header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            response.Header.Add(header);
                        }

                        // ----------- Items List -----------
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                    item[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                response.Items.Add(item);
                            }
                        }

                        // ----------- Weight Summary -----------
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var obj = new WeightSummary();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    var prop = typeof(WeightSummary).GetProperty(reader.GetName(i));
                                    if (prop != null && !reader.IsDBNull(i))
                                    {
                                        var value = reader.GetValue(i);
                                        var converted = ChangeType(value, prop.PropertyType);
                                        prop.SetValue(obj, converted);
                                    }
                                }
                                response.WeightSummary.Add(obj);
                            }
                        }
                    }
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpPost]
        public IActionResult GetAllDatadetails([FromBody] GetDetailsRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request");

            var gv = _globalVariableService.GetGlobalVariables();
            var response = new PurchaseAllDetailsResponse();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_GetPurchaseAllDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VNO", request.VNO);
                    //cmd.Parameters.AddWithValue("@VNO", 252660013);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@V_TYPE", request.vType);

                    //cmd.Parameters.AddWithValue("@VNO", 202100001);
                    //cmd.Parameters.AddWithValue("@YEAR_CODE", 3);
                    //cmd.Parameters.AddWithValue("@COMP_CODE", 5);
                    //cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    //cmd.Parameters.AddWithValue("@V_TYPE", vType);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        // ----------- PURCHASE1 -----------
                        while (reader.Read())
                        {
                            var obj = new Purchase1List();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var prop = typeof(Purchase1List).GetProperty(reader.GetName(i));
                                if (prop != null && !reader.IsDBNull(i))
                                {
                                    var value = reader.GetValue(i);
                                    var converted = ChangeType(value, prop.PropertyType);
                                    prop.SetValue(obj, converted);
                                }
                            }
                            response.Purchase1.Add(obj);
                        }

                        // ----------- PURCHASE2 -----------
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var obj = new Purchase2List();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    var prop = typeof(Purchase2List).GetProperty(reader.GetName(i));
                                    if (prop != null && !reader.IsDBNull(i))
                                    {
                                        var value = reader.GetValue(i);
                                        var converted = ChangeType(value, prop.PropertyType);
                                        prop.SetValue(obj, converted);
                                    }
                                }
                                response.Purchase2.Add(obj);
                            }
                        }

                        // ----------- PURCHASE3 -----------
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var obj = new Purchase3List();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    var prop = typeof(Purchase3List).GetProperty(reader.GetName(i));
                                    if (prop != null && !reader.IsDBNull(i))
                                    {
                                        var value = reader.GetValue(i);
                                        var converted = ChangeType(value, prop.PropertyType);
                                        prop.SetValue(obj, converted);
                                    }
                                }
                                response.Purchase3.Add(obj);
                            }
                        }
                    }
                }

                return Json(response);
            }
            catch (SqlException ex)
            {
                return StatusCode(500, "Database error: " + ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An unexpected error occurred: " + ex.Message);
            }
        }
        private object ChangeType(object value, Type targetType)
        {
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null || value == DBNull.Value) return null;
                targetType = Nullable.GetUnderlyingType(targetType);
            }
            if (targetType.IsEnum)
            {
                return Enum.ToObject(targetType, value);
            }

            return Convert.ChangeType(value, targetType);
        }
}
}
