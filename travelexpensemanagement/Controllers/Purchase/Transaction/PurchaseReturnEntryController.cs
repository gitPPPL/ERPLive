using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseReturnEntry;


namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseReturnEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PurchaseReturnEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
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
            return View("~/Views/Purchase/Transaction/PurchaseReturnEntry/Index.cshtml");
        }

        public JsonResult GetddlDocType()
        {
            string query = $@" Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('PurchaseReturn')";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlRefType()
        {
            string query = $@" Select Code,Name from DOCTYPE_MAST where Code in ('BFRC','RCPI','RCPT','SRPU')";
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
                return Json(new { success = true, nextVNo = nextVNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //-------------------------------Ref No Drop down List Banding data------------------------------------
        public JsonResult GetddlRefNo(string Vtype)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT a.V_NO, DOC_ID  FROM PURCHASE1 a  WHERE a.COMP_CODE = {globalVar.PubCompCode}
            AND a.BRANCH_CODE = 1  AND a.V_TYPE = '{Vtype}' and a.YEAR_CODE = '{globalVar.PubFYearCode}' ORDER BY a.V_NO";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //-------------------------------Ref No Drop down List Banding data------------------------------------
        public JsonResult GetddlDocStatus()
        {
            string query = $@" Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetMakeListByItem()
        {
            var CCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $@"SELECT DISTINCT IMM.CODE,IMM.NAME FROM ITEM_MAKE IM LEFT JOIN ITEMMAKE_MAST IMM ON IM.MAKE_CODE = IMM.CODE 
            WHERE IM.COMP_CODE = '" + CCode + "' and imm.name<>''";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetDepartmentList()
        {
            var CCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $@"Select code, name from DEPT_MAST";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlReturnTo()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT  a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetddlCreditAC()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT  a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }


        public JsonResult GetddlDebitAC()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT  a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
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
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //Transport Name ddl banding
        public JsonResult GetddlTransportName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select Code, NAME From TRANSPORT_MAST where COMP_CODE={globalVar.PubCompCode} order by name asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        //Banding Tab1 Item Name List

        public JsonResult GetddlTransportAc()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select Code, NAME From TRANSPORT_MAST where COMP_CODE={globalVar.PubCompCode} order by name asc";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpGet]
        public JsonResult GetItemList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            //string sql = @"SELECT a.CODE AS Code, a.NAME AS Name FROM ITEM_MAST a LEFT JOIN ITEM_MAKE b ON a.code = b.ITEM_CODE AND b.COMP_CODE = @Comp
            //LEFT JOIN ITEMUNIT_MAST c ON a.UNIT_CODE = c.CODE AND c.COMP_CODE = @Comp
            //LEFT JOIN ITEM_MGROUP d ON a.MGROUP_CODE = d.CODE AND d.COMP_CODE = @Comp
            //WHERE a.COMP_CODE = @Comp AND d.MGROUP_TYPE = 'Store' GROUP BY a.NAME, a.CODE ORDER BY a.NAME";
            string sql = @"SELECT a.CODE AS Code, a.NAME AS Name FROM ITEM_MAST a LEFT JOIN ITEM_MAKE b ON a.code = b.ITEM_CODE AND b.COMP_CODE = @Comp
            LEFT JOIN ITEMUNIT_MAST c ON a.UNIT_CODE = c.CODE AND c.COMP_CODE = @Comp
            LEFT JOIN ITEM_MGROUP d ON a.MGROUP_CODE = d.CODE AND d.COMP_CODE = @Comp
            WHERE a.COMP_CODE = @Comp GROUP BY a.NAME, a.CODE ORDER BY a.NAME";
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

        public async Task<IActionResult> SaveAllData(
        [FromForm] string Header,
        [FromForm] List<ItemDetailModel> ItemDetails,
        [FromForm] List<AttachmentModel> Attachments)
        {
            var headerObj = JsonConvert.DeserializeObject<PurchaseReturnHeaderModel>(Header);
            var globalVar = _globalVariableService.GetGlobalVariables();
            string V_NO = "";
            string DOC_ID = "";
            DOC_ID = headerObj.DocType + headerObj.Vno;
            if (headerObj.ACTION == "INSERT")
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {

                            // Insert Header
                            using (var cmdHeader = new SqlCommand("InsertPurchaseReturnEntryHeader", con, transaction))
                            {
                                cmdHeader.CommandType = CommandType.StoredProcedure;

                                AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
                                AddParameterSafe(cmdHeader, "@BRANCH_CODE", globalVar.PubBranchCode);
                                AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
                                // Document Header
                                AddParameterSafe(cmdHeader, "@V_NO", headerObj.Vno);
                                AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
                                AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                //AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WbNo);
                                AddParameterSafe(cmdHeader, "@REF_TYPE", headerObj.RefType);
                                AddParameterSafe(cmdHeader, "@REF_NO", headerObj.RefNo);

                                // Return To Details
                                AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.ReturnTo);
                                AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.ReturnAddLine1);
                                AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.ReturnAddLine2);
                                AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.ReturnAddLine3);
                                AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.ReturnCity);
                                AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.ReturnCity);
                                AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.ReturnGST);

                                // Ship To Details
                                AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipTo);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
                                AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
                                AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);
                                AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.ShipCity);

                                // Accounting
                                AddParameterSafe(cmdHeader, "@CREDIT_AC", headerObj.CreditAC);
                                AddParameterSafe(cmdHeader, "@DEBIT_AC", headerObj.DebitAC);

                                // Document Details 
                                AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
                                AddParameterSafe(cmdHeader, "@BILL_DATE", string.IsNullOrWhiteSpace(headerObj.BillDate) ? DBNull.Value : DateTime.Parse(headerObj.BillDate));
                                AddParameterSafe(cmdHeader, "@BL_NO", headerObj.BLNo);
                                AddParameterSafe(cmdHeader, "@BL_DATE", string.IsNullOrWhiteSpace(headerObj.BLDate) ? DBNull.Value : DateTime.Parse(headerObj.BLDate));
                                AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);
                                AddParameterSafe(cmdHeader, "@INPUT_TYPE", headerObj.InputType);
                                AddParameterSafe(cmdHeader, "@EXPS_TYPE", headerObj.ExpensesType);
                                AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);
                                AddParameterSafe(cmdHeader, "@STATUS", 1);

                                // Transport
                                AddParameterSafe(cmdHeader, "@TRANSPORT_CODE", headerObj.TransportName);
                                AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
                                AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
                                AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
                                AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
                                AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo ?? "");
                                AddParameterSafe(cmdHeader, "@GR_DATE", string.IsNullOrWhiteSpace(headerObj.GRDate) ? DBNull.Value : DateTime.Parse(headerObj.GRDate));
                                AddParameterSafe(cmdHeader, "@TRANSPORT_AC", headerObj.TransportAC);
                                //AddParameterSafe(cmdHeader, "@DEBIT_AC", headerObj.FreightDebit);
                                //AddParameterSafe(cmdHeader, "@CREDIT_AC", headerObj.FreightCredit);
                                AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);

                                // Amount Breakdown
                                AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty ?? 0);
                                AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty ?? 0);
                                AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount ?? 0);
                                AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount ?? 0);
                                AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking ?? (object)DBNull.Value);
                                AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST ?? 0);
                                AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST ?? 0);
                                AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST ?? 0);
                                AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS ?? 0);
                                AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT ?? 0);
                                AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt ?? 0);
                                AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1 ?? 0);
                                AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2 ?? 0);
                                AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff ?? 0);

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
                                using (var cmdItem = new SqlCommand("InsertPurchaseReturnEntryItemDetail", con, transaction))
                                {
                                    cmdItem.CommandType = CommandType.StoredProcedure;

                                    AddParameterSafe(cmdItem, "@V_NO", headerObj.Vno);
                                    AddParameterSafe(cmdItem, "@DOC_ID", headerObj.DocType + headerObj.Vno ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                    AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdItem, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                    AddParameterSafe(cmdItem, "@SNO", serialNo++);
                                    AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemCode);
                                    AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@UOM_NAME", item.Unit ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@NOS", item.Nos ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RECD_QTY", item.ReturnQty ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RATE", item.Rate ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@AMOUNT", item.Amount ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RCM_YN", item.RCMYN ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@INPUT_YN", item.InputYN ?? (object)DBNull.Value);

                                    // Parse string to int or pass DBNull
                                    if (int.TryParse(item.TaxType, out int taxCode))
                                        AddParameterSafe(cmdItem, "@TAX_CODE", taxCode);
                                    else
                                        AddParameterSafe(cmdItem, "@TAX_CODE", DBNull.Value);

                                    AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt ?? (object)DBNull.Value);

                                    // Handle MAKE_CODE (string to int or DBNull)
                                    if (int.TryParse(item.Make, out int makeCode))
                                        AddParameterSafe(cmdItem, "@MAKE_CODE", makeCode);
                                    else
                                        AddParameterSafe(cmdItem, "@MAKE_CODE", DBNull.Value);

                                    // Handle DEPT_CODE (string to int or DBNull)
                                    if (int.TryParse(item.Department, out int deptCode))
                                        AddParameterSafe(cmdItem, "@DEPT_CODE", deptCode);
                                    else
                                        AddParameterSafe(cmdItem, "@DEPT_CODE", DBNull.Value);

                                    AddParameterSafe(cmdItem, "@REMARKS", item.Remarks ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt ?? (object)DBNull.Value);
                                    // WBType/WBNo are not being sent, so omitted
                                    

                                    AddParameterSafe(cmdItem, "@KANTA_TYPE", item.WBType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@KANTA_NO", item.WBNo ?? (object)DBNull.Value);

                                    AddParameterSafe(cmdItem, "@REF_TYPE", item.RefType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@REF_NO", item.RefNo);

                                    AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
                                    AddParameterSafe(cmdItem, "@EUSER", DBNull.Value);
                                    AddParameterSafe(cmdItem, "@EDATE", DBNull.Value);
                                    AddParameterSafe(cmdItem, "@AED", "A");
                                    AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdItem, "@LID", Environment.MachineName);
                                    AddParameterSafe(cmdItem, "@Action", "Insert");

                                    await cmdItem.ExecuteNonQueryAsync();
                                }
                            }
                            transaction.Commit();
                            return Ok(new { status = "success", message = "Saved successfully" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return BadRequest(new { status = "error", message = ex.Message });
                        }
                    }
                }
            }
            else if (headerObj.ACTION == "UPDATE")
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Insert Header
                            using (var cmdHeader = new SqlCommand("InsertPurchaseReturnEntryHeader", con, transaction))
                            {
                                cmdHeader.CommandType = CommandType.StoredProcedure;

                                AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
                                AddParameterSafe(cmdHeader, "@BRANCH_CODE", globalVar.PubBranchCode);
                                AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
                                // Document Header
                                AddParameterSafe(cmdHeader, "@V_NO", headerObj.Vno);
                                AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
                                AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                //AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WbNo);
                                AddParameterSafe(cmdHeader, "@REF_TYPE", headerObj.RefType);
                                AddParameterSafe(cmdHeader, "@REF_NO", headerObj.RefNo);

                                // Return To Details
                                AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.ReturnTo);
                                AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.ReturnAddLine1);
                                AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.ReturnAddLine2);
                                AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.ReturnAddLine3);
                                AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.ReturnCity);
                                AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.ReturnCity);
                                AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.ReturnGST);

                                // Ship To Details
                                AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipTo);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
                                AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
                                AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);
                                AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.ShipCity);

                                // Accounting
                                AddParameterSafe(cmdHeader, "@CREDIT_AC", headerObj.CreditAC);
                                AddParameterSafe(cmdHeader, "@DEBIT_AC", headerObj.DebitAC);

                                // Document Details 
                                AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
                                AddParameterSafe(cmdHeader, "@BILL_DATE", string.IsNullOrWhiteSpace(headerObj.BillDate) ? DBNull.Value : DateTime.Parse(headerObj.BillDate));
                                AddParameterSafe(cmdHeader, "@BL_NO", headerObj.BLNo);
                                AddParameterSafe(cmdHeader, "@BL_DATE", string.IsNullOrWhiteSpace(headerObj.BLDate) ? DBNull.Value : DateTime.Parse(headerObj.BLDate));
                                AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);
                                AddParameterSafe(cmdHeader, "@INPUT_TYPE", headerObj.InputType);
                                AddParameterSafe(cmdHeader, "@EXPS_TYPE", headerObj.ExpensesType);
                                AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);
                                AddParameterSafe(cmdHeader, "@STATUS", 1);

                                // Transport
                                AddParameterSafe(cmdHeader, "@TRANSPORT_CODE", headerObj.TransportName);
                                AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
                                AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
                                AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
                                AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
                                AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo ?? "");
                                AddParameterSafe(cmdHeader, "@GR_DATE", string.IsNullOrWhiteSpace(headerObj.GRDate) ? DBNull.Value : DateTime.Parse(headerObj.GRDate));
                                AddParameterSafe(cmdHeader, "@TRANSPORT_AC", headerObj.TransportAC);
                                //AddParameterSafe(cmdHeader, "@DEBIT_AC", headerObj.FreightDebit);
                                //AddParameterSafe(cmdHeader, "@CREDIT_AC", headerObj.FreightCredit);
                                AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);

                                // Amount Breakdown
                                AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty ?? 0);
                                AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty ?? 0);
                                AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount ?? 0);
                                AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount ?? 0);
                                AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking ?? (object)DBNull.Value);
                                AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST ?? 0);
                                AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST ?? 0);
                                AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST ?? 0);
                                AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS ?? 0);
                                AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT ?? 0);
                                AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt ?? 0);
                                AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1 ?? 0);
                                AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2 ?? 0);
                                AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff ?? 0);

                                AddParameterSafe(cmdHeader, "@UUSER", globalVar.PubUserId);
                                AddParameterSafe(cmdHeader, "@UDATE", DateTime.Now);
                                AddParameterSafe(cmdHeader, "@EUSER", "");
                                AddParameterSafe(cmdHeader, "@EDATE", "");
                                AddParameterSafe(cmdHeader, "@AED", "A");
                                AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
                                AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
                                AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);
                                AddParameterSafe(cmdHeader, "@Action", "Update");
                                await cmdHeader.ExecuteNonQueryAsync();
                            }
                            // Insert Items

                            using (SqlCommand ItemDetailDelete = new SqlCommand("DELETE FROM PURCHASE2 WHERE COMP_CODE = @COMP_CODE AND V_NO = @V_NO and V_TYPE= @V_TYPE and YEAR_CODE= @YEAR_CODE ", con, transaction))
                            {
                                ItemDetailDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                ItemDetailDelete.Parameters.AddWithValue("@V_NO", headerObj.Vno);
                                ItemDetailDelete.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                ItemDetailDelete.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                ItemDetailDelete.ExecuteNonQuery();
                            }

                            int serialNo = 1;

                            foreach (var item in ItemDetails)
                            {
                                using (var cmdItem = new SqlCommand("InsertPurchaseReturnEntryItemDetail", con, transaction))
                                {
                                    cmdItem.CommandType = CommandType.StoredProcedure;

                                    AddParameterSafe(cmdItem, "@V_NO", headerObj.Vno);
                                    AddParameterSafe(cmdItem, "@DOC_ID", headerObj.DocType + headerObj.Vno ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                    AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdItem, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                    AddParameterSafe(cmdItem, "@SNO", serialNo++);
                                    AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemCode);
                                    AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@UOM_NAME", item.Unit ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@NOS", item.Nos ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RECD_QTY", item.ReturnQty ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RATE", item.Rate ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@AMOUNT", item.Amount ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RCM_YN", item.RCMYN ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@INPUT_YN", item.InputYN ?? (object)DBNull.Value);

                                    // Parse string to int or pass DBNull
                                    if (int.TryParse(item.TaxType, out int taxCode))
                                        AddParameterSafe(cmdItem, "@TAX_CODE", taxCode);
                                    else
                                        AddParameterSafe(cmdItem, "@TAX_CODE", DBNull.Value);

                                    AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt ?? (object)DBNull.Value);

                                    // Handle MAKE_CODE (string to int or DBNull)
                                    if (int.TryParse(item.Make, out int makeCode))
                                        AddParameterSafe(cmdItem, "@MAKE_CODE", makeCode);
                                    else
                                        AddParameterSafe(cmdItem, "@MAKE_CODE", DBNull.Value);

                                    // Handle DEPT_CODE (string to int or DBNull)
                                    if (int.TryParse(item.Department, out int deptCode))
                                        AddParameterSafe(cmdItem, "@DEPT_CODE", deptCode);
                                    else
                                        AddParameterSafe(cmdItem, "@DEPT_CODE", DBNull.Value);

                                    AddParameterSafe(cmdItem, "@REMARKS", item.Remarks ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt ?? (object)DBNull.Value);
                                    // WBType/WBNo are not being sent, so omitted


                                    AddParameterSafe(cmdItem, "@KANTA_TYPE", item.WBType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@KANTA_NO", item.WBNo ?? (object)DBNull.Value);

                                    AddParameterSafe(cmdItem, "@REF_TYPE", item.RefType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@REF_NO", item.RefNo);

                                    AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
                                    AddParameterSafe(cmdItem, "@EUSER", DBNull.Value);
                                    AddParameterSafe(cmdItem, "@EDATE", DBNull.Value);
                                    AddParameterSafe(cmdItem, "@AED", "A");
                                    AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdItem, "@LID", Environment.MachineName);
                                    AddParameterSafe(cmdItem, "@Action", "Insert");

                                    await cmdItem.ExecuteNonQueryAsync();
                                }
                            }
                            // Insert Image
                            using (SqlCommand ItemDetailDelete = new SqlCommand("DELETE FROM PURCHASE3 WHERE COMP_CODE = @COMP_CODE AND V_NO = @V_NO and V_TYPE= @V_TYPE and YEAR_CODE= @YEAR_CODE ", con, transaction))
                            {
                                ItemDetailDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                ItemDetailDelete.Parameters.AddWithValue("@V_NO", headerObj.Vno);
                                ItemDetailDelete.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                ItemDetailDelete.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                ItemDetailDelete.ExecuteNonQuery();
                            }

                            foreach (var file in Attachments)
                            {
                                if (file.File != null && file.File.Length > 0)
                                {
                                    // Save file to disk
                                    var fileName = Path.GetFileName(file.File.FileName);
                                    var saveFolder = Path.Combine("wwwroot", "attachments", "Purchase");
                                    var filePath = Path.Combine(saveFolder, fileName);

                                    if (!Directory.Exists(saveFolder))
                                    {
                                        Directory.CreateDirectory(saveFolder);
                                    }
                                    using (var stream = new FileStream(filePath, FileMode.Create))
                                    {
                                        await file.File.CopyToAsync(stream);
                                    }
                                    // Save record to database
                                    using (var cmdAttach = new SqlCommand("InsertPURCHASEReturnEntryAttachment", con, transaction))
                                    {
                                        cmdAttach.CommandType = CommandType.StoredProcedure;

                                        AddParameterSafe(cmdAttach, "@COMP_CODE", globalVar.PubCompCode);
                                        AddParameterSafe(cmdAttach, "@BRANCH_CODE", 1);
                                        AddParameterSafe(cmdAttach, "@YEAR_CODE", globalVar.PubFYearCode);
                                        AddParameterSafe(cmdAttach, "@DOC_ID", headerObj.DocNo);
                                        AddParameterSafe(cmdAttach, "@V_NO", headerObj.Vno);
                                        AddParameterSafe(cmdAttach, "@V_TYPE", headerObj.DocType);
                                        AddParameterSafe(cmdAttach, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                        AddParameterSafe(cmdAttach, "@UUSER", globalVar.PubUserId);
                                        AddParameterSafe(cmdAttach, "@UDATE", DateTime.Now);
                                        AddParameterSafe(cmdAttach, "@AED", "A");
                                        AddParameterSafe(cmdAttach, "@WSID", globalVar.PubWorkStationID);
                                        AddParameterSafe(cmdAttach, "@LIP", globalVar.PubLocalId);
                                        AddParameterSafe(cmdAttach, "@LID", Environment.MachineName);
                                        AddParameterSafe(cmdAttach, "@ATTACHMENT", "/attachments/Purchase/" + fileName);
                                        //AddParameterSafe(cmdAttach, "@Action", "Insert");
                                        await cmdAttach.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            transaction.Commit();
                            return Ok(new { status = "success", message = "Update successfully" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return BadRequest(new { status = "error", message = ex.Message });
                        }
                    }
                }
            }
            else
            {
                return Json(new { success = false, message = "Invalid action specified." });
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
        //-------------------------------------GetRefNoList----------------------------------
        public async Task<IActionResult> GetRefNoList(string StrVNo, string StrV_type)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new GatePurchaseDetailsResponse();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand command = new SqlCommand("usp_GetRefNoPurchaseReturnEntry", con))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@V_TYPE", StrV_type);
                    command.Parameters.AddWithValue("@V_NO", Convert.ToInt32(StrVNo));
                    command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    command.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    await con.OpenAsync();
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        // Header
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine("SHIP_GST = " + reader["SHIP_GST"].ToString());
                            var header = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount;  i++)
                            {
                                header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            response.Header.Add(header);
                        }
                        // Items
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    item[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                                response.Items.Add(item);
                            }
                        }
                        // Weight Summary
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
                                        prop.SetValue(obj, ChangeType(reader.GetValue(i), prop.PropertyType));
                                    }
                                }
                                response.WeightSummary.Add(obj);
                            }
                        }
                    }
                }
                return Json(new
                {
                    success = true,
                    data = response
                });
            }
            catch (SqlException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
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
        //-------------------------------------GetRefNoList----------------------------------

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
                using (var cmd = new SqlCommand("sp_GetPurchaseReturnAllDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VNO", request.VNO);
                    //cmd.Parameters.AddWithValue("@VNO", 252660013);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@V_TYPE", request.vType);
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
