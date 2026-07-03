using Azure;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
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
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PurchaseReceiptEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
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
            string query = $@"SELECT ADDRESS_ID as Code, Add1 AS Name FROM SUBGROUP_ADDRESS WHERE COMP_CODE = {globalVar.PubCompCode} AND CODE = '{code}'";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetShipDetailsddlAddLine1(string code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT ADDRESS_ID as Code, Add1 AS Name FROM SUBGROUP_ADDRESS WHERE COMP_CODE = {globalVar.PubCompCode} AND CODE = '{code}'";
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
             
            var gv = _globalVariableService.GetGlobalVariables();
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

        public async Task<IActionResult> SaveAllData([FromForm] string Header, [FromForm] List<ItemDetailModel> ItemDetails, [FromForm] List<AttachmentModel> Attachments)
        {
            var headerObj = JsonConvert.DeserializeObject<PurchaseReceiptHeaderModel>(Header);
            var globalVar = _globalVariableService.GetGlobalVariables();

            bool isUpdate = !string.IsNullOrWhiteSpace(headerObj.code) && headerObj.code != "0";
            bool isInsert = !isUpdate;
            string vNo = isInsert ? headerObj.DocNo : headerObj.code;
            string DOC_ID = headerObj.DocType + vNo;

            if (isInsert)
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    string checkQuery = @"SELECT COUNT(*) FROM PURCHASE1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO";

                    using (var cmd = new SqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode); // You can replace 1 with globalVar.PubBranchCode if dynamic
                        cmd.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);

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
                }
            }
                // Check if record already exists
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
                            AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
                            AddParameterSafe(cmdHeader, "@V_NO", vNo);
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

                            AddParameterSafe(cmdHeader, "@Action", isInsert ? "Insert" : "Update");

                            await cmdHeader.ExecuteNonQueryAsync();
                        }

                        if (!isInsert)
                        {
                            string deleteQuery = @"DELETE FROM PURCHASE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE";

                            using (var cmdDelete = new SqlCommand(deleteQuery, con, transaction))
                            {
                                cmdDelete.Parameters.AddWithValue("@V_NO", vNo);
                                cmdDelete.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                cmdDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmdDelete.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                cmdDelete.Parameters.AddWithValue("@BRANCH_CODE", 1);

                                await cmdDelete.ExecuteNonQueryAsync();
                            }
                        }

                        // Insert Items
                        int serialNo = 1;
                        foreach (var item in ItemDetails)
                        {
                            using (var cmdItem = new SqlCommand("InsertPurchaseItemDetail", con, transaction))
                            {
                                cmdItem.CommandType = CommandType.StoredProcedure;

                                AddParameterSafe(cmdItem, "@V_NO", vNo);
                                AddParameterSafe(cmdItem, "@DOC_ID", DOC_ID);
                                AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
                                AddParameterSafe(cmdItem, "@BRANCH_CODE", 1);
                                AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                AddParameterSafe(cmdItem, "@SNO", serialNo++);

                                AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemCode);
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
        
        [HttpGet]
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
        
        //=================Main Method For Gate Fill=====================
        [HttpPost]
        public async Task<IActionResult> GetGatDetailsList(string StrVNo, string StrV_type)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string wbType = "";
            int wbNo = 0;

            Dictionary<string, object> header = null;
            List<Dictionary<string, object>> items = new List<Dictionary<string, object>>();

            try
            {
                if (!int.TryParse(StrVNo, out int gateNo))
                    return BadRequest("Invalid Gate No");

                string gateType = StrV_type.Substring(0, 4);

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // ===========================
                    // 1. WB Query
                    // ===========================

                    string qry = @"
                    SELECT V_TYPE,V_NO
                    FROM WB1
                    WHERE GATE_TYPE=@GateType
                    AND GATE_NO=@GateNo
                    AND COMP_CODE=@CompCode
                    AND BRANCH_CODE=@BranchCode
                    AND YEAR_CODE=@YearCode";

                    using (SqlCommand cmd = new SqlCommand(qry, con))
                    {
                        cmd.Parameters.AddWithValue("@GateType", gateType);
                        cmd.Parameters.AddWithValue("@GateNo", gateNo);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                wbType = dr["V_TYPE"].ToString();
                                wbNo = Convert.ToInt32(dr["V_NO"]);
                            }
                        }
                    }

                    // ===========================
                    // 2. GATE1 Header Query
                    // ===========================

                    string qry1 = @"
                        SELECT
                        a.BILL_NO,
                        a.BILL_DATE,
                        a.CHALL_NO,
                        a.CHALL_DATE,
                        a.WAYBILL_NO,
                        a.TRANSIT_NO,
                        a.PARTY_CODE,
                        a.SHIP_PARTY,
                        b.NAME AS PartyName,
                        a.ADD1,
                        a.ADD2,
                        a.ADD3,
                        sa.ADDRESS_ID AS PARTY_ADDRESSID,
                        a.PARTY_CITY AS CITY_CODE,
                        e.NAME AS CITY,
                        f.CODE AS StateCode,
                        f.NAME AS State,
                        a.PARTY_GST AS GSTIN,
                        a.PARTY_PINCODE,
                        ISNULL(a.TRANSPORT_CODE,0) TRANSPORT_CODE,
                        d.NAME AS Transport,
                        a.TRUCK_NO,
                        a.REMARKS,
                        a.EWB_DATE,
                        a.EWB_EXPDATE,
                        a.EWB_INVNO,
                        a.GR_NO,
                        a.GR_DATE
                        FROM GATE1 a
                        LEFT JOIN SUBGROUP_MAST b
                        ON a.PARTY_CODE=b.CODE
                        AND b.COMP_CODE=@CompCode
                         
                        LEFT JOIN SUBGROUP_ADDRESS sa
                        ON sa.CODE = a.PARTY_CODE
                        AND sa.COMP_CODE = @CompCode
                        AND ISNULL(sa.ADD1,'') = ISNULL(a.ADD1,'')
                        AND ISNULL(sa.ADD2,'') = ISNULL(a.ADD2,'')
                        AND ISNULL(sa.ADD3,'') = ISNULL(a.ADD3,'')
                        
                        LEFT JOIN TRANSPORT_MAST d
                        ON a.TRANSPORT_CODE=d.CODE
                        AND d.COMP_CODE=@CompCode
                        
                        LEFT JOIN CITY_MAST e
                        ON a.PARTY_CITY=e.CODE
                        
                        LEFT JOIN STATE_MAST f
                        ON e.STATE_CODE=f.CODE
                        
                        WHERE
                        a.V_TYPE=@GateType
                        AND a.V_NO=@GateNo
                        AND a.COMP_CODE=@CompCode
                        AND a.BRANCH_CODE=@BranchCode
                        AND a.YEAR_CODE=@YearCode";

                    using (SqlCommand cmd = new SqlCommand(qry1, con))
                    {
                        cmd.Parameters.AddWithValue("@GateType", gateType);
                        cmd.Parameters.AddWithValue("@GateNo", gateNo);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            if (await dr.ReadAsync())
                            {
                                header = new Dictionary<string, object>();

                                for (int i = 0; i < dr.FieldCount; i++)
                                {
                                    header.Add(
                                        dr.GetName(i),
                                        dr.IsDBNull(i) ? null : dr.GetValue(i)
                                    );
                                }
                            }
                        }
                    }

                    //===========================
                    // Container List
                    //===========================

                    if (header != null)
                    {
                        int partyCode = Convert.ToInt32(header["PARTY_CODE"]);
                        string billNo = header["BILL_NO"]?.ToString() ?? "";

                        if (partyCode > 0 && !string.IsNullOrWhiteSpace(billNo))
                        {
                            header["ContainerList"] = await GetContainerList( con, partyCode, billNo);
                        }
                    }

                    // ===========================
                    // 3. Item Query
                    // ===========================

                    string qry2 = "";

                    if (gateType == "INJB" && wbNo > 0)
                    {
                        qry2 = @"
                            Select
                                @GateType v_type,
                                @GateNo v_no,
                                a.ITEM_CODE,
                                b.NAME ITEM_NAME,
                                ISNULL(b.UNIT_NAME,'') Unit,
                                b.HSN_CODE,
                                0 NOS,
                                a.NET_WGT QTY,
                                0 RATE,
                                '' EMPTY,
                                0 PACK_PER,
                                0 DISC_PER,
                                '' TaxType,
                                0 CGST_PER,
                                0 SGST_PER,
                                0 IGST_PER,
                                0 OTH_AMT,
                                a.REF_TYPE,
                                a.REF_NO,
                                '' REQUEST_TYPE,
                                0 REQUEST_NO,
                                '' Make,
                                '' Department,
                                0 DEPT_CODE,
                                0 TAX_CODE,
                                0 MAKE_CODE,
                                b.UNIT_CODE UOM_CODE
                            from WB2 a
                            left join ITEM_MAST b
                                on a.ITEM_CODE=b.CODE
                                and b.COMP_CODE=a.COMP_CODE
                            where
                                ISNULL(a.ITEM_CODE,0)>0
                                and a.V_TYPE=@WBType
                                and a.V_NO=@WBNo
                                and a.COMP_CODE=@CompCode
                                and a.BRANCH_CODE=@BranchCode
                                and a.YEAR_CODE=@YearCode
                            order by a.SNO";
                    }
                    else
                    {
                         qry2 = @"
                            select
                                a.v_type,
                                a.v_no,
                                a.ITEM_CODE,
                                c.NAME ITEM_NAME,
                                ISNULL(a.UOM_NAME,b.NAME) Unit,
                                c.HSN_CODE,
                                a.NOS,
                                a.QTY,
                                d.RATE,
                                a.EMPTY,
                                d.PACK_PER,
                                d.PACK_AMT,
                                d.DISC_PER,
                                d.DISC_AMT,
                                e.NAME TaxType,
                                d.CGST_PER,
                                d.SGST_PER,
                                d.IGST_PER,
                                d.CESS_PER,
                                d.CESS_AMT,
                                d.OTH_AMT,
                                a.REF_TYPE,
                                a.REF_NO,
                                d.REQUEST_TYPE,
                                d.REQUEST_NO,
                                g.NAME Make,
                                f.NAME Department,
                                d.DEPT_CODE,
                                d.TAX_CODE,
                                d.MAKE_CODE,
                                a.UOM_CODE
                            from GATE2 a
                            left join ITEMUNIT_MAST b
                                on a.UOM_CODE=b.CODE
                                and b.COMP_CODE=@CompCode
                            left join ITEM_MAST c
                                on a.ITEM_CODE=c.CODE
                                and c.COMP_CODE=@CompCode
                            left join ORDER2 d
                                on a.REF_TYPE=d.V_TYPE
                                and a.REF_NO=d.V_NO
                                and a.ITEM_CODE=d.ITEM_CODE
                                and d.COMP_CODE=@CompCode
                                and d.BRANCH_CODE=@BranchCode
                            left join TAX_MAST e
                                on d.TAX_CODE=e.CODE
                            left join ITEMDEPT_MAST f
                                on d.DEPT_CODE=f.CODE
                                and f.COMP_CODE=@CompCode
                            left join ITEMMAKE_MAST g
                                on d.MAKE_CODE=g.CODE
                                and g.COMP_CODE=@CompCode
                            where
                                a.V_TYPE=@GateType
                                and a.V_NO=@GateNo
                                and a.COMP_CODE=@CompCode
                                and a.BRANCH_CODE=@BranchCode
                         order by a.SNO";
                    }
                    using (SqlCommand cmd = new SqlCommand(qry2, con))
                    {
                        cmd.Parameters.AddWithValue("@GateType", gateType);
                        cmd.Parameters.AddWithValue("@GateNo", gateNo);

                        cmd.Parameters.AddWithValue("@WBType", wbType);
                        cmd.Parameters.AddWithValue("@WBNo", wbNo);

                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                        List<Dictionary<string, object>> tempItems = new();

                        using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                        {
                            while (await dr.ReadAsync())
                            {
                                Dictionary<string, object> row = new();

                                for (int i = 0; i < dr.FieldCount; i++)
                                {
                                    row.Add(
                                        dr.GetName(i),
                                        dr.IsDBNull(i) ? null : dr.GetValue(i)
                                    );
                                }

                                tempItems.Add(row);
                            }
                        }

                        foreach (var row in tempItems)
                        {
                            int itemCode = Convert.ToInt32(row["ITEM_CODE"]);

                            decimal recQty = await GetRecQty(
                                con,
                                itemCode,
                                gateType,
                                gateNo,
                                wbType
                            );

                            decimal wbQty = 0;

                            if (wbNo > 0)
                            {
                                wbQty = await GetWBQty(
                                    con,
                                    itemCode,
                                    gateType,
                                    gateNo,
                                    wbType
                                );
                            }

                            row["RecQty"] = recQty;
                            row["WBQty"] = wbQty;
                            row["KantaType"] = wbType;
                            row["KantaNo"] = wbNo;

                            await FillDepartmentFromWB( con, row,itemCode, wbType,wbNo);
                            row["WB_YN"] = await GetWBYN(con, itemCode);
                            await FillTCSAndPaymentDetails( con, row, gateType, gateNo);

                            items.Add(row);
                        }
                    }
                   
                }

                return Json(new
                {
                    status = true,
                    wbType,
                    wbNo,
                    header,
                    items
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        //=========GET REC & WN QTY Method==========
        private async Task<decimal> GetRecQty( SqlConnection con, int itemCode, string gateType, int gateNo, string wbType)
        {
            string query;
            var gv = _globalVariableService.GetGlobalVariables();
            if (wbType == "KSIN" || wbType == "KSOT")
            {
                query = @"
                SELECT ISNULL(SUM(a.NET_WGT),0)
                FROM WB2 a
                INNER JOIN WB1 b
                   ON a.V_NO = b.V_NO
                   AND a.V_TYPE = b.V_TYPE
                   AND a.COMP_CODE = b.COMP_CODE
                   AND a.BRANCH_CODE = b.BRANCH_CODE
                   AND a.YEAR_CODE = b.YEAR_CODE
                   WHERE
                    a.ITEM_CODE=@ItemCode
                    AND b.GATE_TYPE=@GateType
                    AND b.GATE_NO=@GateNo
                    AND a.COMP_CODE=@CompCode
                    AND a.BRANCH_CODE=@BranchCode
                    AND a.YEAR_CODE=@YearCode";
            }
            else
            {
                query = @"
                SELECT ISNULL(SUM(a.NET_WGT),0)
                FROM WB2 a
                INNER JOIN WB1 b
                    ON a.V_NO = b.V_NO
                   AND a.V_TYPE = b.V_TYPE
                   AND a.COMP_CODE = b.COMP_CODE
                   AND a.BRANCH_CODE = b.BRANCH_CODE
                   AND a.YEAR_CODE = b.YEAR_CODE
                WHERE
                    b.STATUS=3
                    AND a.ITEM_CODE=@ItemCode
                    AND b.GATE_TYPE=@GateType
                    AND b.GATE_NO=@GateNo
                    AND a.COMP_CODE=@CompCode
                    AND a.BRANCH_CODE=@BranchCode
                    AND a.YEAR_CODE=@YearCode";
            }

            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@ItemCode", itemCode);
            cmd.Parameters.AddWithValue("@GateType", gateType);
            cmd.Parameters.AddWithValue("@GateNo", gateNo);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

            object result = await cmd.ExecuteScalarAsync();

            return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
        }

        private async Task<decimal> GetWBQty( SqlConnection con, int itemCode, string gateType, int gateNo, string kantaType)
        {
            string query;
            var gv = _globalVariableService.GetGlobalVariables();
            if (kantaType == "KSIN" || kantaType == "KSOT")
            {
                query = @"
                SELECT ISNULL(SUM(b.NET_WGT),0)
                FROM WB1 a
                INNER JOIN WB2 b
                   ON a.V_TYPE=b.V_TYPE
                   AND a.V_NO=b.V_NO
                   AND a.COMP_CODE=b.COMP_CODE
                   AND a.BRANCH_CODE=b.BRANCH_CODE
                   AND a.YEAR_CODE=b.YEAR_CODE
                 WHERE
                    b.ITEM_CODE=@ItemCode
                    AND a.GATE_TYPE=@GateType
                    AND a.GATE_NO=@GateNo
                    AND a.COMP_CODE=@CompCode
                    AND a.BRANCH_CODE=@BranchCode
                    AND a.YEAR_CODE=@YearCode";
            }
            else
            {
                query = @"
                SELECT ISNULL(SUM(b.NET_WGT),0)
                FROM WB1 a
                INNER JOIN WB2 b
                    ON a.V_TYPE=b.V_TYPE
                    AND a.V_NO=b.V_NO
                    AND a.COMP_CODE=b.COMP_CODE
                    AND a.BRANCH_CODE=b.BRANCH_CODE
                    AND a.YEAR_CODE=b.YEAR_CODE
                WHERE
                    a.STATUS=3
                    AND b.ITEM_CODE=@ItemCode
                    AND a.GATE_TYPE=@GateType
                    AND a.GATE_NO=@GateNo
                    AND a.COMP_CODE=@CompCode
                    AND a.BRANCH_CODE=@BranchCode
                    AND a.YEAR_CODE=@YearCode";
            }

            using SqlCommand cmd = new(query, con);

            cmd.Parameters.AddWithValue("@ItemCode", itemCode);
            cmd.Parameters.AddWithValue("@GateType", gateType);
            cmd.Parameters.AddWithValue("@GateNo", gateNo);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

            object result = await cmd.ExecuteScalarAsync();

            return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
        }

        //=========TCS & Payment Block ================
        private async Task FillTCSAndPaymentDetails( SqlConnection con, Dictionary<string, object> row, string gateType, int gateNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string poType = row["REF_TYPE"]?.ToString() ?? "";
            int poNo = row["REF_NO"] == null ? 0 : Convert.ToInt32(row["REF_NO"]);

            if (poNo > 0)
            {
                string tcsQry = @"
                SELECT TOP 1 TCS_PER
                FROM ORDER1
                WHERE COMP_CODE=@CompCode
                    AND CONCAT(V_TYPE,V_NO) IN
                    (
                        SELECT CONCAT(REF_TYPE,REF_NO)
                        FROM GATE2 a
                        LEFT JOIN DOCTYPE_MAST b
                            ON a.REF_TYPE=b.CODE
                        WHERE b.DOCTYPE='Purchaseorder'
                        AND a.COMP_CODE=@CompCode
                        AND a.V_TYPE=@GateType
                        AND a.V_NO=@GateNo
                    )";

                using (SqlCommand cmd = new SqlCommand(tcsQry, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@GateType", gateType);
                    cmd.Parameters.AddWithValue("@GateNo", gateNo);

                    object result = await cmd.ExecuteScalarAsync();

                    row["TCS_PER"] = result == DBNull.Value || result == null
                        ? 0
                        : Convert.ToDecimal(result);
                }

                string paymentQry;

                if (poType != "PAUD")
                {
                    paymentQry = @"
                    SELECT HOLD_PAY
                    FROM SAUDA
                    WHERE CONCAT(V_TYPE,V_NO)=
                    (
                        SELECT TOP 1 CONCAT(SAUDA_TYPE,SAUDA_NO)
                        FROM ORDER2
                        WHERE V_TYPE=@POType
                          AND V_NO=@PONo
                          AND COMP_CODE=@CompCode
                          AND BRANCH_CODE=@BranchCode
                    )
                    AND COMP_CODE=@CompCode
                    AND BRANCH_CODE=@BranchCode";
                }
                else
                {
                    paymentQry = @"
                SELECT HOLD_PAY
                FROM SAUDA
                WHERE V_TYPE=@POType
                  AND V_NO=@PONo
                  AND COMP_CODE=@CompCode
                  AND BRANCH_CODE=@BranchCode";
                }

                using (SqlCommand cmd = new SqlCommand(paymentQry, con))
                {
                    cmd.Parameters.AddWithValue("@POType", poType);
                    cmd.Parameters.AddWithValue("@PONo", poNo);
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

                    object result = await cmd.ExecuteScalarAsync();

                    row["Payment"] = result?.ToString() ?? "";
                }

                row["IsHold"] = row["Payment"]?.ToString() == "HOLD";
            }

        }

        //==================Container Method============
        private async Task<List<string>> GetContainerList(SqlConnection con, int partyCode, string billNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            List<string> containers = new();

            string contQry = @"
            SELECT DISTINCT CONTAINER_NO
            FROM
            (
                SELECT CONTAINER_NO
                FROM ORDER4
                WHERE PARTY_CODE=@PartyCode
                  AND INV_NO=@BillNo
                  AND COMP_CODE=@CompCode
                  AND BRANCH_CODE=@BranchCode

                UNION ALL

                SELECT b.CONTAINER_NO
                FROM EXIM1 a
                LEFT JOIN EXIM2 b
                     ON a.V_TYPE=b.V_TYPE
                    AND a.V_NO=b.V_NO
                    AND a.COMP_CODE=b.COMP_CODE
                    AND a.BRANCH_CODE=b.BRANCH_CODE
                    AND a.YEAR_CODE=b.YEAR_CODE

                WHERE a.SUPPLIER=@PartyCode
                  AND a.SUPPLIER_INVNO=@BillNo
                  AND a.COMP_CODE=@CompCode
                  AND a.BRANCH_CODE=@BranchCode
            ) x
            WHERE ISNULL(CONTAINER_NO,'') <> ''";

            using SqlCommand cmd = new(contQry, con);

            cmd.Parameters.AddWithValue("@PartyCode", partyCode);
            cmd.Parameters.AddWithValue("@BillNo", billNo);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

            using SqlDataReader dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                containers.Add(dr["CONTAINER_NO"].ToString());
            }

            return containers;
        }

        //===========Department Override method==================
        private async Task FillDepartmentFromWB(SqlConnection con, Dictionary<string, object> row, int itemCode, string kantaType, int kantaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            if (string.IsNullOrWhiteSpace(kantaType) || kantaNo <= 0)
                return;

            string qry = @"
            SELECT TOP 1
                TO_PLACE,
                TO_NAME
            FROM WB2
            WHERE ITEM_CODE=@ItemCode
              AND V_TYPE=@VType
              AND V_NO=@VNo
              AND COMP_CODE=@CompCode
              AND BRANCH_CODE=@BranchCode
              AND YEAR_CODE=@YearCode";

            using SqlCommand cmd = new(qry, con);

            cmd.Parameters.AddWithValue("@ItemCode", itemCode);
            cmd.Parameters.AddWithValue("@VType", kantaType);
            cmd.Parameters.AddWithValue("@VNo", kantaNo);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
            cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

            using SqlDataReader dr = await cmd.ExecuteReaderAsync();

            if (await dr.ReadAsync())
            {
                row["DEPT_CODE"] = dr["TO_PLACE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TO_PLACE"]);
                row["Department"] = dr["TO_NAME"]?.ToString() ?? "";
            }
        }

        //============Item Check(WB_YN)===========
        private async Task<string> GetWBYN(SqlConnection con,int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = @"
            SELECT ISNULL(WB_YN,'')
            FROM ITEM_MAST
            WHERE CODE=@ItemCode
           AND COMP_CODE=@CompCode";

            using SqlCommand cmd = new(qry, con);

            cmd.Parameters.AddWithValue("@ItemCode", itemCode);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);

            object result = await cmd.ExecuteScalarAsync();

            return result?.ToString() ?? "";
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
