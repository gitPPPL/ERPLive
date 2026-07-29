using Azure;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.Data;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Purchase.Transiction;
using static travelexpensemanagement.Models.Purchase.Transaction.ImportExportExpensesEntry;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseReceiptEntry;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportExportExpensesEntry : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        public ImportExportExpensesEntry(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }

        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.GlobalVariables = globalVar;
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = globalVar.PubBranchCode;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/Purchase/Transaction/ImportExportExpensesEntry/Index.cshtml");
        }

        public JsonResult GetddlDocType()
        {
            string query = $@"Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('MaterialReceipt','ServiceReceipt')";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetDocTypeCopyFrom()
        {
            string query = $@"SELECT CODE, NAME FROM DOCTYPE_MAST WHERE DOCTYPE='PurchaseOrder' AND CODE<>'PORD' ORDER BY NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public JsonResult GetDepartment()
        {
            var globalVaribale = _globalVariableService.GetGlobalVariables();
            string query = $@"select name,code from ITEMDEPT_MAST where COMP_CODE= {globalVaribale.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public JsonResult GetBINMAST()
        {
            var globalVaribale = _globalVariableService.GetGlobalVariables();
            string query = $@"select name,code from ITEM_BIN_MAST where COMP_CODE= {globalVaribale.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public JsonResult GetUnitMast()
        {
            var globalVaribale = _globalVariableService.GetGlobalVariables();
            string query = $@"select code , name from ITEMUNIT_MAST where COMP_CODE = {globalVaribale.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public JsonResult GetMakeMast()
        {
            var globalVaribale = _globalVariableService.GetGlobalVariables();
            string query = $@"select code, name from ITEMMAKE_MAST where COMP_CODE = {globalVaribale.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        //==========For text box dropdown search==================
        [HttpGet]
        public JsonResult GetDropdown(string type, string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var data = type switch
            {
                "TransportName" => _dropdownService.GetTransportName(gv.PubCompCode, term),
                _ => new List<DropdownService.DropdownModel>()
            };

            return Json(data);
        }

        [HttpGet]
        public JsonResult SearchTransportName(string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var data = _dropdownService.GetTransportName(gv.PubCompCode, term);

            return Json(data);
        }

        //========================End================
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
                    new SqlParameter("@BranchCode", globalVar.PubBranchCode),
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
                return Json(new { success = true, nextVNo = nextVNo, docType = docType });
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

        public JsonResult GetddlTransportName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select Code, NAME From TRANSPORT_MAST where COMP_CODE={globalVar.PubCompCode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

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

            string sql = @"
            SELECT 
                a.CODE AS Code,
                a.NAME AS Name
            FROM ITEM_MAST a
            LEFT JOIN ITEM_MAKE b
                ON a.CODE = b.ITEM_CODE
                AND b.COMP_CODE = @COMP_CODE
            LEFT JOIN ITEMUNIT_MAST c
                ON a.UNIT_CODE = c.CODE
                AND c.COMP_CODE = @COMP_CODE
            LEFT JOIN ITEM_MGROUP d
                ON a.MGROUP_CODE = d.CODE
                AND d.COMP_CODE = @COMP_CODE
            WHERE a.NAME <> ''
              AND a.NAME <> '.'
              AND a.COMP_CODE = @COMP_CODE
            GROUP BY a.NAME, a.CODE
            ORDER BY a.NAME ASC";

            var list = new List<object>();

            using (var con = _dbConnection.GetErpConnection())
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

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
                    sql = @" SELECT CODE, CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 ,PACK_ONBASIC
                FROM TAX_MAST WHERE CODE = @Code";

                    cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@Code", codeValue);
                }
                else
                {
                    sql = @" SELECT CODE, CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 ,PACK_ONBASIC
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
                            OTH_PER2 = rdr["OTH_PER2"],
                            PACK_ONBASIC = rdr["PACK_ONBASIC"]
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

        [HttpGet]
        public IActionResult GetStateByCity(int cityCode)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"SELECT c.code As CityCode,
                                       c.name As CityName,
                                       s.CODE AS StateCode,
                                       s.NAME AS StateName
                                    FROM CITY_MAST c
                                    INNER JOIN STATE_MAST s
                                        ON c.STATE_CODE = s.CODE
                                    WHERE c.CODE = @CITY_CODE";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@CITY_CODE", cityCode);

                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        return Json(new
                        {
                            stateCode = reader["StateCode"],
                            stateName = reader["StateName"]
                        });
                    }
                    return Json(null);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAllData([FromForm] string Header, [FromForm] List<ItemDetailImportExportExpensesEntryModel> ItemDetails, [FromForm] List<ImportExportExpensesEntryAttachmentModel> Attachments)
        {
            var headerObj = JsonConvert.DeserializeObject<ImportExportExpensesEntryHeaderModel>(Header);
            var globalVar = _globalVariableService.GetGlobalVariables();

            bool isUpdate = !string.IsNullOrWhiteSpace(headerObj.code) && headerObj.code != "0";
            bool isInsert = !isUpdate;
            string vNo = isInsert ? headerObj.DocNo : headerObj.code;
            string DOC_ID = headerObj.DocType + vNo;

            // ================= Validation =================
            var validationResult = await ValidatePurchaseReceiptAsync(headerObj, ItemDetails, isInsert, vNo);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    status = "validation",
                    message = validationResult.Message
                });
            }

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
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
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

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        using (var cmdHeader = new SqlCommand("InsertPurchaseReceiptHeader", con, transaction))
                        {
                            cmdHeader.CommandType = CommandType.StoredProcedure;
                            AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
                            AddParameterSafe(cmdHeader, "@BRANCH_CODE", globalVar.PubBranchCode);
                            AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
                            AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
                            AddParameterSafe(cmdHeader, "@V_NO", vNo);
                            AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
                            AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                            AddParameterSafe(cmdHeader, "@EXCH_RATE", headerObj.ExchangeRate);
                            AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.BillFrom);
                            AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.AddLine1);
                            AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.AddLine2);
                            AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.AddLine3);
                            AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.City);
                            AddParameterSafe(cmdHeader, "@BILL_PINCODE", headerObj.Pincode);
                            AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.BILL_ADDRESSID);
                            AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.GST);
                            AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);

                            AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipFrom);
                            AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
                            AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
                            AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
                            AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
                            AddParameterSafe(cmdHeader, "@SHIP_PINCODE", headerObj.ShipPincode);
                            AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.SHIP_ADDRESSID);

                            AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
                            AddParameterSafe(cmdHeader, "@BILL_DATE", DateTime.Parse(headerObj.BillDate));
                            AddParameterSafe(cmdHeader, "@CHALL_NO", headerObj.ChallanNo);
                            AddParameterSafe(cmdHeader, "@CHALL_DATE", string.IsNullOrWhiteSpace(headerObj.ChallanDate) ? (object)DBNull.Value : DateTime.Parse(headerObj.ChallanDate));
                            AddParameterSafe(cmdHeader, "@GATE_NO", headerObj.GateNo);
                            AddParameterSafe(cmdHeader, "@GATE_TYPE", headerObj.GATE_TYPE);
                            AddParameterSafe(cmdHeader, "@TRANSIT_NO", headerObj.TRANSIT_NO);

                            AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);

                            AddParameterSafe(cmdHeader, "@TRANSPORT_NAME", headerObj.TransportName);
                            AddParameterSafe(cmdHeader, "@TRANSPORT_CODE", headerObj.TRANSPORT_CODE);
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

                            AddParameterSafe(cmdHeader, "@EWB_DATE", headerObj.EWB_DATE);
                            AddParameterSafe(cmdHeader, "@EWB_EXPDATE", headerObj.EWB_EXPDATE);
                            AddParameterSafe(cmdHeader, "@EWB_INVNO", headerObj.EWB_INVNO);
                            AddParameterSafe(cmdHeader, "@HOLD_PAY", headerObj.HOLD_PAY);
                            AddParameterSafe(cmdHeader, "@HOLD_REASON", headerObj.HOLD_REASON);
                            AddParameterSafe(cmdHeader, "@HOLD_DATE", headerObj.HOLD_DATE);

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
                            AddParameterSafe(cmdHeader, "@EUSER", globalVar.PubUserId);
                            AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
                            AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
                            AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);

                            AddParameterSafe(cmdHeader, "@Action", isInsert ? "Insert" : "Update");
                            await cmdHeader.ExecuteNonQueryAsync();
                        }

                        if (!isInsert)
                        {
                            string deleteQuery = @"DELETE FROM PURCHASE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE
                                                   DELETE FROM IMG_TABLE WHERE V_NO=@V_NO AND V_TYPE=@V_TYPE AND COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE AND BRANCH_CODE=@BRANCH_CODE
                                                   DELETE FROM PROD_BATCH WHERE V_NO=@V_NO AND V_TYPE=@V_TYPE AND COMP_CODE=@COMP_CODE AND YEAR_CODE=@YEAR_CODE AND BRANCH_CODE=@BRANCH_CODE";

                            using (var cmdDelete = new SqlCommand(deleteQuery, con, transaction))
                            {
                                cmdDelete.Parameters.AddWithValue("@V_NO", vNo);
                                cmdDelete.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                cmdDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmdDelete.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                cmdDelete.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                await cmdDelete.ExecuteNonQueryAsync();
                            }
                        }

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
                                AddParameterSafe(cmdItem, "@BRANCH_CODE", globalVar.PubBranchCode);
                                AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                AddParameterSafe(cmdItem, "@SNO", serialNo++);
                                AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemCode);
                                AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName);
                                AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode);
                                AddParameterSafe(cmdItem, "@UOM_NAME", item.UOMName);
                                AddParameterSafe(cmdItem, "@UOM_CODE", item.UOMCode);
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
                                AddParameterSafe(cmdItem, "@DEPT_CODE", item.DeptCode);
                                AddParameterSafe(cmdItem, "@REMARKS", item.Remarks);
                                AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
                                AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
                                AddParameterSafe(cmdItem, "@AED", "A");
                                AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
                                AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
                                AddParameterSafe(cmdItem, "@LID", Environment.MachineName);
                                AddParameterSafe(cmdItem, "@Action", "Insert");

                                await cmdItem.ExecuteNonQueryAsync();
                            }

                            //====================== Insert Into PROD_BATCH ======================
                            if (headerObj.DocType == "RCPT" || headerObj.DocType == "RCPI")
                            {
                                using (var cmdBatch = new SqlCommand(@"
                                    INSERT INTO PROD_BATCH
                                    ( COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, V_DATE, BATCH_NO, BAG_NO, ITEM_CODE, GROSS_QTY, QTY, REMARKS, SNO,
                                      UUSER, UDATE, AED, WSID, LIP, LID )

                                    VALUES
                                    ( @COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @V_DATE, @BATCH_NO, @BAG_NO, @ITEM_CODE, @GROSS_QTY, @QTY,
                                      @REMARKS, @SNO, @UUSER, GETDATE(), @AED, @WSID, @LIP, @LID )", con, transaction))
                                {
                                    AddParameterSafe(cmdBatch, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdBatch, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdBatch, "@YEAR_CODE", globalVar.PubFYearCode);

                                    AddParameterSafe(cmdBatch, "@V_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdBatch, "@V_NO", vNo);
                                    AddParameterSafe(cmdBatch, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                    AddParameterSafe(cmdBatch, "@BATCH_NO", vNo);
                                    AddParameterSafe(cmdBatch, "@BAG_NO", $"{vNo}{serialNo - 1}");

                                    AddParameterSafe(cmdBatch, "@ITEM_CODE", item.ItemCode);
                                    AddParameterSafe(cmdBatch, "@GROSS_QTY", item.RecQty);
                                    AddParameterSafe(cmdBatch, "@QTY", item.RecQty);

                                    AddParameterSafe(cmdBatch, "@REMARKS", item.Remarks);
                                    AddParameterSafe(cmdBatch, "@SNO", serialNo - 1);

                                    AddParameterSafe(cmdBatch, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdBatch, "@AED", isInsert ? "A" : "E");
                                    AddParameterSafe(cmdBatch, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdBatch, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdBatch, "@LID", Environment.MachineName);

                                    await cmdBatch.ExecuteNonQueryAsync();
                                }
                            }
                            //====================== End PROD_BATCH ======================

                        }

                        //=================For Image===========================
                        int rowId = 1;

                        if (Attachments != null && Attachments.Any())
                        {
                            foreach (var attachment in Attachments)
                            {
                                byte[] fileBytes;
                                string fileName;
                                string fileType;

                                if (attachment.File != null && attachment.File.Length > 0)
                                {
                                    // New uploaded image
                                    using (var ms = new MemoryStream())
                                    {
                                        await attachment.File.CopyToAsync(ms);
                                        fileBytes = ms.ToArray();
                                    }

                                    fileName = attachment.File.FileName;
                                    fileType = Path.GetExtension(attachment.File.FileName);
                                }
                                else if (attachment.IMG_FILE != null && attachment.IMG_FILE.Length > 0)
                                {
                                    // Existing image
                                    fileBytes = Convert.FromBase64String(attachment.IMG_FILE);
                                    fileName = attachment.FILE_NAME;
                                    fileType = attachment.FILE_TYPE;
                                }
                                else
                                {
                                    continue;
                                }

                                using (var cmdImage = new SqlCommand("InsertPurchaseReceiptHeader", con, transaction))
                                {
                                    cmdImage.CommandType = CommandType.StoredProcedure;

                                    AddParameterSafe(cmdImage, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdImage, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdImage, "@YEAR_CODE", globalVar.PubFYearCode);

                                    AddParameterSafe(cmdImage, "@DOC_ID", DOC_ID);
                                    AddParameterSafe(cmdImage, "@V_NO", vNo);
                                    AddParameterSafe(cmdImage, "@V_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdImage, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                    AddParameterSafe(cmdImage, "@ROWID", rowId++);
                                    AddParameterSafe(cmdImage, "@IMG_FILE", fileBytes);
                                    AddParameterSafe(cmdImage, "@FILE_NAME", fileName);
                                    AddParameterSafe(cmdImage, "@FILE_TYPE", fileType);

                                    AddParameterSafe(cmdImage, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdImage, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdImage, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdImage, "@LID", Environment.MachineName);

                                    AddParameterSafe(cmdImage, "@Action", "ImageInsert");

                                    await cmdImage.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        //==========Both are commented in  old code ==================

                        //========Update Gate1 ==============
                        //using (var cmdGate = new SqlCommand(@"UPDATE GATE1 SET BILL_NO = @BILL_NO, BILL_DATE = @BILL_DATE, MRN_TYPE = @MRN_TYPE, MRN_NO = @MRN_NO
                        //                                   WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE", con, transaction))
                        //{
                        //    AddParameterSafe(cmdGate, "@BILL_NO", headerObj.BillNo);
                        //    AddParameterSafe(cmdGate, "@BILL_DATE", string.IsNullOrWhiteSpace(headerObj.BillDate) ? (object)DBNull.Value : DateTime.Parse(headerObj.BillDate));

                        //    AddParameterSafe(cmdGate, "@V_TYPE", headerObj.GATE_TYPE);
                        //    AddParameterSafe(cmdGate, "@V_NO", headerObj.GateNo);

                        //    AddParameterSafe(cmdGate, "@MRN_TYPE", headerObj.DocType);
                        //    AddParameterSafe(cmdGate, "@MRN_NO", vNo);

                        //    AddParameterSafe(cmdGate, "@COMP_CODE", globalVar.PubCompCode);
                        //    AddParameterSafe(cmdGate, "@BRANCH_CODE", globalVar.PubBranchCode);
                        //    AddParameterSafe(cmdGate, "@YEAR_CODE", globalVar.PubFYearCode);

                        //    await cmdGate.ExecuteNonQueryAsync();
                        //}

                        //========== Update Qc1 ============
                        //using (var cmdQC = new SqlCommand(@"UPDATE QC1 SET BILL_NO = @BILL_NO, BILL_DATE = @BILL_DATE, CONTAINER_NO = @CONTAINER_NO WHERE MRN_TYPE = @MRN_TYPE
                        //                                 AND MRN_NO = @MRN_NO AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE", con, transaction))
                        //{
                        //    AddParameterSafe(cmdQC, "@BILL_NO", headerObj.BillNo);
                        //    AddParameterSafe(cmdQC, "@BILL_DATE", string.IsNullOrWhiteSpace(headerObj.BillDate) ? (object)DBNull.Value : DateTime.Parse(headerObj.BillDate));

                        //    AddParameterSafe(cmdQC, "@CONTAINER_NO", headerObj.ContainerNo);

                        //    AddParameterSafe(cmdQC, "@MRN_TYPE", headerObj.DocType);
                        //    AddParameterSafe(cmdQC, "@MRN_NO", vNo);

                        //    AddParameterSafe(cmdQC, "@COMP_CODE", globalVar.PubCompCode);
                        //    AddParameterSafe(cmdQC, "@BRANCH_CODE", globalVar.PubBranchCode);
                        //    AddParameterSafe(cmdQC, "@YEAR_CODE", globalVar.PubFYearCode);

                        //    await cmdQC.ExecuteNonQueryAsync();
                        //}

                        transaction.Commit();

                        foreach (var item in ItemDetails)
                        {
                            if (item.ReqNo > 0)
                            {
                                //==================== Update PREQUEST2 ====================
                                using (var cmdReq = new SqlCommand(@"
                                  UPDATE PREQUEST2
                                   SET Adj_Qty = @Adj_Qty,
                                   Status = 3,
                                   PO_TYPE = IIF(ISNULL(PO_TYPE,'')='', @PO_TYPE, PO_TYPE),
                                   PO_NO = IIF(ISNULL(PO_NO,0)=0, @PO_NO, PO_NO),
                                   MRN_TYPE = @MRN_TYPE,
                                   MRN_NO = @MRN_NO
                                  WHERE V_TYPE = 'STPI'
                                   AND V_NO = @REQ_NO
                                   AND ITEM_CODE = @ITEM_CODE
                                   AND COMP_CODE = @COMP_CODE
                                   AND BRANCH_CODE = @BRANCH_CODE", con))
                                {
                                    AddParameterSafe(cmdReq, "@Adj_Qty", item.RecQty);
                                    AddParameterSafe(cmdReq, "@PO_TYPE", item.POType);
                                    AddParameterSafe(cmdReq, "@PO_NO", item.PONo);
                                    AddParameterSafe(cmdReq, "@MRN_TYPE", headerObj.DocType);
                                    AddParameterSafe(cmdReq, "@MRN_NO", vNo);

                                    AddParameterSafe(cmdReq, "@REQ_NO", item.ReqNo);
                                    AddParameterSafe(cmdReq, "@ITEM_CODE", item.ItemCode);

                                    AddParameterSafe(cmdReq, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdReq, "@BRANCH_CODE", globalVar.PubBranchCode);

                                    await cmdReq.ExecuteNonQueryAsync();
                                }

                                //==================== Check Pending ====================
                                using (var cmdCheck = new SqlCommand(@"
                                    SELECT COUNT(*)
                                    FROM PREQUEST2
                                    WHERE STATUS = 1
                                      AND V_TYPE = 'STPI'
                                      AND V_NO = @REQ_NO
                                      AND COMP_CODE = @COMP_CODE
                                      AND BRANCH_CODE = @BRANCH_CODE
                                      AND YEAR_CODE = @YEAR_CODE", con))
                                {
                                    AddParameterSafe(cmdCheck, "@REQ_NO", item.ReqNo);
                                    AddParameterSafe(cmdCheck, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdCheck, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdCheck, "@YEAR_CODE", globalVar.PubFYearCode);

                                    int pending = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());

                                    if (pending == 0)
                                    {
                                        using (var cmdReq1 = new SqlCommand(@"
                                        UPDATE PREQUEST1
                                        SET STATUS = 3
                                        WHERE V_TYPE = 'STPI'
                                          AND V_NO = @REQ_NO
                                          AND COMP_CODE = @COMP_CODE
                                          AND BRANCH_CODE = @BRANCH_CODE", con))
                                        {
                                            AddParameterSafe(cmdReq1, "@REQ_NO", item.ReqNo);
                                            AddParameterSafe(cmdReq1, "@COMP_CODE", globalVar.PubCompCode);
                                            AddParameterSafe(cmdReq1, "@BRANCH_CODE", globalVar.PubBranchCode);
                                            await cmdReq1.ExecuteNonQueryAsync();
                                        }
                                    }
                                }
                            }
                        }
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
                        command.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
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
                            header["ContainerList"] = await GetContainerList(con, partyCode, billNo);
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

                            await FillDepartmentFromWB(con, row, itemCode, wbType, wbNo);
                            row["WB_YN"] = await GetWBYN(con, itemCode);
                            await FillTCSAndPaymentDetails(con, row, gateType, gateNo);

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
        private async Task<decimal> GetRecQty(SqlConnection con, int itemCode, string gateType, int gateNo, string wbType)
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

        private async Task<decimal> GetWBQty(SqlConnection con, int itemCode, string gateType, int gateNo, string kantaType)
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
        private async Task FillTCSAndPaymentDetails(SqlConnection con, Dictionary<string, object> row, string gateType, int gateNo)
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

        //============Item Check(WB_YN)==================
        private async Task<string> GetWBYN(SqlConnection con, int itemCode)
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
        public IActionResult GetAllDatadetails([FromBody] GetImportExportExpensesAllDetailsResponseDetailsRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request");

            var gv = _globalVariableService.GetGlobalVariables();
            var response = new ImportExportExpensesAllDetailsResponse();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_GetPurchaseAllDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@VNO", request.VNO);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", request.vType);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        // ----------- PURCHASE1 -----------
                        while (reader.Read())
                        {
                            var obj = new ImportExportExpensesEntry1();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var prop = typeof(ImportExportExpensesEntry1).GetProperty(reader.GetName(i));
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
                                var obj = new ImportExportExpensesEntry2();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    var prop = typeof(ImportExportExpensesEntry2).GetProperty(reader.GetName(i));
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

                        // -----------Image -----------
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var obj = new ImportExportExpensesEntry3();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    var prop = typeof(ImportExportExpensesEntry3).GetProperty(columnName);

                                    if (prop == null || reader.IsDBNull(i))
                                        continue;

                                    // Special handling for byte[]
                                    if (columnName == "IMG_FILE")
                                    {
                                        prop.SetValue(obj, (byte[])reader["IMG_FILE"]);
                                    }
                                    else
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

        //=================Validation Method ====================

     
        private async Task<(bool IsValid, string Message)> ValidatePurchaseReceiptAsync(
    ImportExportExpensesEntryHeaderModel headerObj,
    List<ItemDetailImportExportExpensesEntryModel> itemDetails,
    bool isInsert,
    string vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var generalSetting = await _globalVariableService.LoadGeneralSetting();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                // ============================
                // Bill From GST Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.GST))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT 1
                    FROM SUBGROUP_ADDRESS
                    WHERE COMP_CODE=@CompCode
                      AND CODE=@Code
                      AND GSTIN=@GSTIN", con);

                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", headerObj.BillFrom);
                    cmd.Parameters.AddWithValue("@GSTIN", headerObj.GST);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj == null)
                        return (false, "Missmatch 'Bill from' GST No from Master Record.");
                }

                // ============================
                // Ship From GST Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.ShipGST))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT 1
                    FROM SUBGROUP_ADDRESS
                    WHERE COMP_CODE=@CompCode
                      AND CODE=@Code
                      AND GSTIN=@GSTIN", con);

                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", headerObj.ShipFrom);
                    cmd.Parameters.AddWithValue("@GSTIN", headerObj.ShipGST);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj == null)
                        return (false, "Missmatch 'Ship from' GST No from Master Record.");
                }

                // ============================
                // Ship From vs Purchase Order Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.ShipFrom) && itemDetails != null && itemDetails.Any())
                {
                    var firstItem = itemDetails.First();

                    SqlCommand cmd = new SqlCommand(@"
                    SELECT 1
                    FROM ORDER1
                    WHERE SHIP_FROM <> @SHIP_FROM
                      AND V_TYPE = @V_TYPE
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con);

                    cmd.Parameters.AddWithValue("@SHIP_FROM", headerObj.ShipFrom);
                    cmd.Parameters.AddWithValue("@V_TYPE", firstItem.POType);
                    cmd.Parameters.AddWithValue("@V_NO", firstItem.PONo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj != null)
                    {
                        return (false, "Ship From not matched as per Purchase Order., Please Check");
                    }
                }

                // ============================
                // GST State Validation
                // ============================

                // Party State (Supplier/Bill From)
                SqlCommand stateCmd = new SqlCommand(@"
                SELECT STATE_CODE
                FROM SUBGROUP_MAST
                WHERE CODE = @CODE
                    AND COMP_CODE = @COMP_CODE", con);

                stateCmd.Parameters.AddWithValue("@CODE", headerObj.BillFrom);
                stateCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                object stateObj = await stateCmd.ExecuteScalarAsync();

                if (stateObj != null && stateObj != DBNull.Value)
                {
                    int partyStateCode = Convert.ToInt32(stateObj);

                    // Company State
                    SqlCommand compStateCmd = new SqlCommand(@"
                    SELECT CM.STATE_CODE
                    FROM COMP_MAST C
                    INNER JOIN CITY_MAST CM
                        ON C.CITY_CODE = CM.CODE
                    WHERE C.CODE = @COMP_CODE", con);

                    compStateCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                    object compStateObj = await compStateCmd.ExecuteScalarAsync();

                    if (compStateObj != null && compStateObj != DBNull.Value)
                    {
                        int companyStateCode = Convert.ToInt32(compStateObj);

                        string stateType = partyStateCode == companyStateCode
                            ? "Local"
                            : "Central/Other";

                        // Local Party → IGST not allowed
                        if (partyStateCode == companyStateCode && headerObj.NumIGST > 0)
                        {
                            return (false, $"IGST not applicable as per Party State type is {stateType}");
                        }

                        // Interstate Party → CGST + SGST not allowed
                        if (partyStateCode != companyStateCode &&
                            (headerObj.NumCGST + headerObj.NumSGST) > 0)
                        {
                            return (false, $"CGST/SGST not applicable as per Party State type is {stateType}");
                        }

                        // Both tax types together not allowed
                        if (headerObj.NumIGST > 0 &&
                            (headerObj.NumCGST + headerObj.NumSGST) > 0)
                        {
                            return (false, "CGST+SGST+IGST all three type tax not applicable.");
                        }
                    }
                }

                // Only while updating
                if (!isInsert)
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT CONCAT(V_TYPE, V_NO)
                    FROM QC1
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO <> @V_NO
                      AND MRN_TYPE = @MRN_TYPE
                      AND MRN_NO = @MRN_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con);

                    cmd.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@MRN_TYPE", headerObj.DocType);
                    cmd.Parameters.AddWithValue("@MRN_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj != null)
                    {
                        return (false, $"MRN No engaged in QC Entry Document no : {obj}, modification not allowed.");
                    }
                }

                // ============================
                // Purchase Bill Reference Validation
                // ============================
                if (!isInsert)
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT CONCAT(V_TYPE, V_NO)
                    FROM PURCHASE2
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO <> @V_NO
                      AND REF_TYPE = @REF_TYPE
                      AND REF_NO = @REF_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con);

                    cmd.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@REF_TYPE", headerObj.DocType);
                    cmd.Parameters.AddWithValue("@REF_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    object obj = await cmd.ExecuteScalarAsync();

                    if (obj != null)
                    {
                        return (false, $"MRN No engaged in Purchase bill entry Document no : {obj}, modification not allowed.");
                    }
                }

                // ============================
                // Duplicate Bill No Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.BillNo))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1
                           DOC_ID,
                           V_DATE
                    FROM PURCHASE1
                    WHERE PARTY_CODE = @PARTY_CODE
                      AND BILL_NO = @BILL_NO
                      AND V_TYPE IN ('SRPU','RCPT','BFRC')
                      AND V_NO <> @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con);

                    cmd.Parameters.AddWithValue("@PARTY_CODE", headerObj.BillFrom);
                    cmd.Parameters.AddWithValue("@BILL_NO", headerObj.BillNo);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string docId = reader["DOC_ID"].ToString();
                            string vDate = Convert.ToDateTime(reader["V_DATE"]).ToString("dd/MM/yyyy");

                            return (false,
                                $"Bill No {headerObj.BillNo} already exists in MRN, Serial No : {docId} dated : {vDate}");
                        }
                    }
                }

                // ============================
                // Duplicate Container No Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.ContainerNo))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1
                           DOC_ID,
                           V_DATE
                    FROM PURCHASE1
                    WHERE PARTY_CODE = @PARTY_CODE
                      AND CONTAINER_NO = @CONTAINER_NO
                      AND BILL_NO = @BILL_NO
                      AND V_TYPE IN ('SRPU','RCPT','BFRC')
                      AND V_NO <> @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE
                      AND YEAR_CODE = @YEAR_CODE", con);

                    cmd.Parameters.AddWithValue("@PARTY_CODE", headerObj.BillFrom);
                    cmd.Parameters.AddWithValue("@CONTAINER_NO", headerObj.ContainerNo);
                    cmd.Parameters.AddWithValue("@BILL_NO", headerObj.BillNo);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            string docId = reader["DOC_ID"].ToString();
                            string vDate = Convert.ToDateTime(reader["V_DATE"]).ToString("dd/MM/yyyy");

                            return (false,
                                $"Container No. {headerObj.ContainerNo} already exists in MRN, Serial No : {docId} dated : {vDate}");
                        }
                    }
                }

                // ============================
                // Gate Date Validation
                // ============================
                if (!string.IsNullOrWhiteSpace(headerObj.GATE_TYPE) &&
                    !string.IsNullOrWhiteSpace(headerObj.GateNo))
                {
                    SqlCommand cmd = new SqlCommand(@"
                    SELECT V_DATE
                    FROM GATE1
                    WHERE V_TYPE = @V_TYPE
                      AND V_NO = @V_NO
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", con);

                    cmd.Parameters.AddWithValue("@V_TYPE", headerObj.GATE_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", headerObj.GateNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    object gateDateObj = await cmd.ExecuteScalarAsync();

                    if (gateDateObj != null && gateDateObj != DBNull.Value)
                    {
                        DateTime gateDate = Convert.ToDateTime(gateDateObj);
                        DateTime mrnDate = Convert.ToDateTime(headerObj.DocDate);

                        if (gateDate.Date > mrnDate.Date)
                        {
                            return (false,
                                $"MRN Date ({mrnDate:dd/MM/yyyy}) can not be less than Gate Date ({gateDate:dd/MM/yyyy}).");
                        }
                    }
                }

                // ============================
                // WB Qty Approval Validation
                // ============================
                foreach (var item in itemDetails)
                {
                    if (globalVar.PubCompCode == "1" &&
                        item.WBQty == 0 &&
                        !string.Equals(headerObj.ReturnType, "Return", StringComparison.OrdinalIgnoreCase))
                    {
                        SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(WB_YN,'')
                        FROM ITEM_MAST
                        WHERE CODE = @ITEM_CODE
                        AND COMP_CODE = @COMP_CODE", con);

                        cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                        string wbYN = Convert.ToString(await cmd.ExecuteScalarAsync());

                        if (wbYN == "Yes")
                        {
                            return (false, "WB Qty is 0, Approval required.");
                        }
                    }
                }

                foreach (var item in itemDetails)
                {
                    if (globalVar.PubCompCode != "1")
                    {
                        SqlCommand cmd = new SqlCommand(@"
                        SELECT ISNULL(WB_YN,'')
                        FROM ITEM_MAST
                        WHERE CODE = @ITEM_CODE
                          AND COMP_CODE = @COMP_CODE", con);

                        cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                        string wbYN = Convert.ToString(await cmd.ExecuteScalarAsync());

                        if (wbYN == "Yes")
                        {
                            if (item.RecQty == 0)
                            {
                                return (false, "WB_YN='Yes' and Received Qty is 0.");
                            }

                            if (string.IsNullOrWhiteSpace(item.KantaType) || item.KantaNo == 0)
                            {
                                return (false, $"WB Type and WB No is blank of Weighbridge item : {item.ItemName}");
                            }
                        }
                    }
                }

                // ============================
                // Duplicate Gate MRN Validation
                // ============================
                if (generalSetting.pubDefGateInMRN == "Yes")
                {
                    if (!string.IsNullOrWhiteSpace(headerObj.GATE_TYPE) &&
                        !string.IsNullOrWhiteSpace(headerObj.GateNo))
                    {
                        SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 1 CONCAT(V_TYPE, CAST(V_NO AS VARCHAR))
                        FROM PURCHASE2
                        WHERE V_TYPE = @MRN_TYPE
                          AND V_NO <> @MRN_NO
                          AND CONCAT(GATE_TYPE, GATE_NO) = CONCAT(@GATE_TYPE, @GATE_NO)
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con);

                        cmd.Parameters.AddWithValue("@MRN_TYPE", headerObj.DocType);
                        cmd.Parameters.AddWithValue("@MRN_NO", vNo);
                        cmd.Parameters.AddWithValue("@GATE_TYPE", headerObj.GATE_TYPE);
                        cmd.Parameters.AddWithValue("@GATE_NO", headerObj.GateNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                        object obj = await cmd.ExecuteScalarAsync();

                        if (obj != null)
                        {
                            return (false, $"GATE No already exist in MRN No : {obj}");
                        }
                    }
                }

                // ============================
                // Item Wise Validations
                // ============================
                foreach (var item in itemDetails)
                {
                    // ============================
                    // Gate Item Validation
                    // ============================
                    if (generalSetting.pubDefGateInMRN == "Yes")
                    {
                        if (!string.IsNullOrWhiteSpace(headerObj.GATE_TYPE) &&
                            (headerObj.DocType == "SRPU" || headerObj.DocType == "STJW"))
                        {
                            // Item Exists in Gate2
                            SqlCommand cmd = new SqlCommand(@"
                            SELECT LTRIM(RTRIM(ITEM_CODE))
                            FROM GATE2
                            WHERE ITEM_CODE=@ITEM_CODE
                              AND V_TYPE=@GATE_TYPE
                              AND V_NO=@GATE_NO
                              AND COMP_CODE=@COMP_CODE
                              AND BRANCH_CODE=@BRANCH_CODE", con);

                            cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                            cmd.Parameters.AddWithValue("@GATE_TYPE", item.GateType);
                            cmd.Parameters.AddWithValue("@GATE_NO", item.GateNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                            object gateItemObj = await cmd.ExecuteScalarAsync();

                            if (gateItemObj == null || gateItemObj == DBNull.Value)
                            {
                                return (false, $"Item {item.ItemName} not exist in Gate document No : {headerObj.GateNo}");
                            }

                            int gateItemCode = Convert.ToInt32(gateItemObj);

                            if (gateItemCode != item.ItemCode)
                            {
                                return (false, $"Item name not matched as per Gate record of {item.ItemName}");
                            }

                            // Gate Bill Qty Validation
                            SqlCommand qtyCmd = new SqlCommand(@"
                            SELECT QTY
                            FROM GATE2
                            WHERE ITEM_CODE=@ITEM_CODE
                              AND V_TYPE=@GATE_TYPE
                              AND V_NO=@GATE_NO
                              AND REF_TYPE=@PO_TYPE
                              AND REF_NO=@PO_NO
                              AND COMP_CODE=@COMP_CODE
                              AND BRANCH_CODE=@BRANCH_CODE", con);

                            qtyCmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                            qtyCmd.Parameters.AddWithValue("@GATE_TYPE", item.GateType);
                            qtyCmd.Parameters.AddWithValue("@GATE_NO", item.GateNo);
                            qtyCmd.Parameters.AddWithValue("@PO_TYPE", item.POType);
                            qtyCmd.Parameters.AddWithValue("@PO_NO", item.PONo);
                            qtyCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            qtyCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                            object qtyObj = await qtyCmd.ExecuteScalarAsync();

                            decimal gateQty = qtyObj == null || qtyObj == DBNull.Value
                                ? 0
                                : Convert.ToDecimal(qtyObj);

                            if (item.BillQty != gateQty)
                            {
                                return (false, $"MRN Bill Qty and Gate Bill Qty not matched of Item {item.ItemName}");
                            }
                        }
                    }

                    // ============================
                    // PO Mandatory Validation
                    // ============================
                    if (generalSetting.pubDefPOInMRN == "Yes")
                    {
                        if (item.PONo == 0)
                        {
                            return (false, $"PO Number is Required/Compulsory of Item {item.ItemName}");
                        }

                    }

                    // ============================
                    // Gate Number Mandatory Validation
                    // ============================
                    if (generalSetting.pubDefGateInMRN == "Yes")
                    {
                        if (item.GateNo == 0)
                        {
                            return (false, $"Gate Number is Required/Compulsory of Item {item.ItemName}");
                        }
                    }

                    // ============================
                    // PO / Sauda Validation
                    // ============================
                    if (generalSetting.pubDefPOInMRN == "Yes")
                    {
                        //================ RCPT / RCPI =================
                        if (headerObj.DocType == "RCPT" || headerObj.DocType == "RCPI")
                        {
                            // Current Item ka Sauda No
                            SqlCommand saudaCmd = new SqlCommand(@"
                            SELECT ISNULL(SAUDA_NO,0)
                            FROM ORDER2
                            WHERE V_TYPE=@V_TYPE
                                AND V_NO=@V_NO
                                AND COMP_CODE=@COMP_CODE
                                AND BRANCH_CODE=@BRANCH_CODE", con);

                            saudaCmd.Parameters.AddWithValue("@V_TYPE", item.POType);
                            saudaCmd.Parameters.AddWithValue("@V_NO", item.PONo);
                            saudaCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            saudaCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                            int saudaNo = Convert.ToInt32(await saudaCmd.ExecuteScalarAsync() ?? 0);

                            if (saudaNo > 0)
                            {
                                decimal billQty = 0;
                                decimal totalSaudaQty = 0;
                                decimal totalReceivedQty = 0;

                                // Same Sauda wale sab items ka Bill Qty
                                foreach (var itm in itemDetails)
                                {
                                    SqlCommand cmd = new SqlCommand(@"
                                    SELECT ISNULL(SAUDA_NO,0)
                                    FROM ORDER2
                                    WHERE V_TYPE=@V_TYPE
                                      AND V_NO=@V_NO
                                      AND COMP_CODE=@COMP_CODE
                                      AND BRANCH_CODE=@BRANCH_CODE", con);

                                    cmd.Parameters.AddWithValue("@V_TYPE", itm.POType);
                                    cmd.Parameters.AddWithValue("@V_NO", itm.PONo);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                    int saudaNo1 = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);

                                    if (saudaNo1 == saudaNo)
                                    {
                                        billQty += itm.BillQty ?? 0m;
                                    }
                                }

                                // Total Sauda Qty
                                SqlCommand qtyCmd = new SqlCommand(@"
                                SELECT ISNULL(SUM(QTY),0)
                                FROM SAUDA
                                WHERE V_TYPE='PAUD'
                                  AND V_NO=@V_NO
                                  AND COMP_CODE=@COMP_CODE
                                  AND BRANCH_CODE=@BRANCH_CODE", con);

                                qtyCmd.Parameters.AddWithValue("@V_NO", saudaNo);
                                qtyCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                qtyCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                totalSaudaQty = Convert.ToDecimal(await qtyCmd.ExecuteScalarAsync());

                                // Already Received Qty
                                SqlCommand recCmd = new SqlCommand(@"
                                SELECT ISNULL(SUM(RECD_QTY),0)
                                FROM PURCHASE2
                                WHERE SAUDA_TYPE='PAUD'
                                  AND SAUDA_NO=@SAUDA_NO
                                  AND COMP_CODE=@COMP_CODE
                                  AND BRANCH_CODE=@BRANCH_CODE
                                  AND V_TYPE=@MRN_TYPE
                                  AND V_NO<>@MRN_NO", con);

                                recCmd.Parameters.AddWithValue("@SAUDA_NO", saudaNo);
                                recCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                recCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                                recCmd.Parameters.AddWithValue("@MRN_TYPE", headerObj.DocType);
                                recCmd.Parameters.AddWithValue("@MRN_NO", vNo);

                                totalReceivedQty = Convert.ToDecimal(await recCmd.ExecuteScalarAsync());

                                totalReceivedQty += billQty;

                                // Sauda Date Validation
                                SqlCommand dateCmd = new SqlCommand(@"
                                SELECT V_DATE
                                FROM SAUDA
                                WHERE V_TYPE='PAUD'
                                  AND V_NO=@V_NO
                                  AND COMP_CODE=@COMP_CODE
                                  AND BRANCH_CODE=@BRANCH_CODE", con);

                                dateCmd.Parameters.AddWithValue("@V_NO", saudaNo);
                                dateCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                dateCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                object dtObj = await dateCmd.ExecuteScalarAsync();

                                if (dtObj != null && dtObj != DBNull.Value)
                                {
                                    DateTime saudaDate = Convert.ToDateTime(dtObj);

                                    if (saudaDate.AddDays(-2).Date >
                                        Convert.ToDateTime(headerObj.BillDate).Date)
                                    {
                                        return (false,
                                            $"Sauda No : '{saudaNo}' Date is Greater than Vendor Invoice Date");
                                    }
                                }

                                if (totalReceivedQty >
                                    (totalSaudaQty + Convert.ToDecimal(generalSetting.pubBPPurchTolQty)))
                                {
                                    decimal pendingQty = totalSaudaQty - totalReceivedQty + (headerObj.NumBillQty ?? 0m);

                                    return (false,
                                        $"Sauda Pending Quantity is = {pendingQty}, Your Invoice Qty is = {headerObj.NumBillQty}, Please Check it.");
                                }
                            }
                        }

                        //================ Other MRN =================
                        else
                        {
                            if (item.PONo > 0)
                            {
                                SqlCommand poDateCmd = new SqlCommand(@"
                                SELECT V_DATE
                                FROM ORDER1
                                WHERE V_TYPE=@V_TYPE
                                  AND V_NO=@V_NO
                                  AND COMP_CODE=@COMP_CODE
                                  AND BRANCH_CODE=@BRANCH_CODE", con);

                                poDateCmd.Parameters.AddWithValue("@V_TYPE", item.POType);
                                poDateCmd.Parameters.AddWithValue("@V_NO", item.PONo);
                                poDateCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                poDateCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                object poDateObj = await poDateCmd.ExecuteScalarAsync();

                                if (poDateObj != null && poDateObj != DBNull.Value)
                                {
                                    DateTime poDate = Convert.ToDateTime(poDateObj);

                                    if (poDate.Date >
                                        Convert.ToDateTime(headerObj.DocDate).Date)
                                    {
                                        return (false,
                                            $"PO No : '{item.POType}{item.PONo}' Date is Greater than Vendor Invoice Date");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return (true, "");
        }

        //==============Production Batch=====================
        [HttpPost]
        public IActionResult GetProductionBatch([FromBody] ProductionBatchRequest request)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            List<object> list = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string sql = @"
                SELECT
                    A.V_TYPE AS RefType,
                    A.V_NO AS RefNo,
                    A.ITEM_CODE AS ItemCode,
                    B.NAME AS ItemName,
                    A.BAG_NO AS BarcodeNo,
                    A.BATCH_NO AS BatchNo,
                    A.GROSS_QTY AS ApproxWeight,
                    A.QTY AS ActualWeight,
                    A.REF_TYPE,
                    A.REF_NO,
                    CASE
                        WHEN (COALESCE(A.REF_TYPE,'') <> ''
                              OR COALESCE(A.REF_NO,0) <> 0)
                        THEN 1
                        ELSE 0
                    END AS IsReferenced
                FROM PROD_BATCH A
                LEFT JOIN ITEM_MAST B
                    ON A.ITEM_CODE = B.CODE
                    AND B.COMP_CODE = A.COMP_CODE
                WHERE A.V_TYPE = @VTYPE
                    AND A.V_NO = @VNO
                    AND A.COMP_CODE = @COMP_CODE
                    AND A.YEAR_CODE = @YEAR_CODE";

                SqlCommand selectCmd = new SqlCommand(sql, con);

                selectCmd.Parameters.AddWithValue("@VTYPE", request.Vtype);
                selectCmd.Parameters.AddWithValue("@VNO", request.Vno);
                selectCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                selectCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                SqlDataReader dr = selectCmd.ExecuteReader();

                while (dr.Read())
                {
                    list.Add(new
                    {
                        refType = dr["RefType"]?.ToString(),
                        refNo = Convert.ToInt32(dr["RefNo"]),
                        itemCode = Convert.ToInt32(dr["ItemCode"]),
                        itemName = dr["ItemName"]?.ToString(),
                        barcodeNo = dr["BarcodeNo"]?.ToString(),
                        batchNo = dr["BatchNo"]?.ToString(),
                        approxWeight = dr["ApproxWeight"],
                        actualWeight = dr["ActualWeight"],
                        referenceType = dr["REF_TYPE"]?.ToString(),
                        referenceNo = dr["REF_NO"],
                        isReferenced = Convert.ToInt32(dr["IsReferenced"])
                    });
                }
            }

            return Json(list);
        }

        //==================Copy From Method =======================
        [HttpGet]
        public IActionResult GetCopyFromData(string vType, string receiptType, int partyCode, int currentVNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            List<object> list = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                //===========================
                // Update Query 
                //===========================
                string updateQuery1 = @"
                UPDATE ORDER2
                SET ADJ_QTY = 0
                FROM ORDER2, DOCTYPE_MAST
                WHERE ORDER2.V_TYPE = DOCTYPE_MAST.CODE
                AND DOCTYPE_MAST.DOCTYPE IN ('PurchaseOrder')
                AND ORDER2.COMP_CODE = @COMP_CODE
                AND ORDER2.BRANCH_CODE = @BRANCH_CODE
                AND ORDER2.STATUS = 1";

                using (SqlCommand cmd = new SqlCommand(updateQuery1, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.ExecuteNonQuery();
                }

                string updateQuery2 = @"
                UPDATE O
                SET O.ADJ_QTY = ISNULL(P.total_recd_qty,0)
                FROM ORDER2 O
                LEFT JOIN
                (
                    SELECT
                        P.PO_TYPE,
                        P.PO_NO,
                        P.COMP_CODE,
                        P.BRANCH_CODE,
                        P.ITEM_CODE,
                        SUM(P.RECD_QTY) AS total_recd_qty
                    FROM PURCHASE2 P
                    INNER JOIN DOCTYPE_MAST D
                        ON P.V_TYPE = D.CODE
                    WHERE D.DOCTYPE IN ('MaterialReceipt','ServiceReceipt','HighSeaPurchase')
                      AND P.V_TYPE = @ReceiptType
                      AND P.V_NO <> @CurrentVNo
                    GROUP BY
                        P.PO_TYPE,
                        P.PO_NO,
                        P.COMP_CODE,
                        P.BRANCH_CODE,
                        P.ITEM_CODE
                ) P
                ON O.V_TYPE = P.PO_TYPE
                AND O.V_NO = P.PO_NO
                AND O.COMP_CODE = P.COMP_CODE
                AND O.BRANCH_CODE = P.BRANCH_CODE
                AND O.ITEM_CODE = P.ITEM_CODE
                WHERE O.COMP_CODE = @COMP_CODE
                AND O.BRANCH_CODE = @BRANCH_CODE
                AND O.V_TYPE IN ('PORD','RORD','JORD')";

                using (SqlCommand cmd = new SqlCommand(updateQuery2, con))
                {
                    cmd.Parameters.AddWithValue("@ReceiptType", receiptType);
                    cmd.Parameters.AddWithValue("@CurrentVNo", currentVNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    cmd.ExecuteNonQuery();
                }

                //===========================
                // data from ORDER2 and related tables
                //===========================
                string sql = @"
                SELECT
                    a.V_NO AS VNo,
                    a.V_TYPE AS VType,
                    FORMAT(a.V_DATE,'dd/MM/yyyy') AS VDate,
                    a.ITEM_CODE AS ItemCode,
                    b.NAME AS ItemName,
                    e.NAME AS Unit,
                    a.NOS,
                    a.QTY,
                    (a.QTY - a.ADJ_QTY) AS BalQty,
                    a.RATE,
                    f.NAME AS TaxType,
                    a.PACK_PER,
                    a.DISC_PER,
                    a.CGST_PER,
                    a.SGST_PER,
                    a.IGST_PER,
                    a.CESS_PER,
                    a.CESS_AMT,
                    a.VAT_PER,
                    a.OTH_AMT,
                    d.NAME AS Make,
                    g.NAME AS Department,
                    a.REMARKS,
                    a.REQUEST_TYPE,
                    a.REQUEST_NO,
                    a.DEPT_CODE,
                    a.MAKE_CODE,
                    a.UOM_CODE,
                    a.TAX_CODE
                FROM ORDER2 a
                LEFT JOIN ITEM_MAST b
                    ON a.ITEM_CODE = b.CODE
                   AND b.COMP_CODE = a.COMP_CODE
                LEFT JOIN ORDER1 c
                    ON a.V_NO = c.V_NO
                   AND a.V_TYPE = c.V_TYPE
                   AND c.COMP_CODE = a.COMP_CODE
                   AND c.BRANCH_CODE = a.BRANCH_CODE
                LEFT JOIN ITEMMAKE_MAST d
                    ON a.MAKE_CODE = d.CODE
                   AND d.COMP_CODE = a.COMP_CODE
                LEFT JOIN ITEMUNIT_MAST e
                    ON a.UOM_CODE = e.CODE
                   AND e.COMP_CODE = a.COMP_CODE
                LEFT JOIN TAX_MAST f
                    ON a.TAX_CODE = f.CODE
                LEFT JOIN ITEMDEPT_MAST g
                    ON a.DEPT_CODE = g.CODE
                   AND g.COMP_CODE = a.COMP_CODE
                WHERE a.V_TYPE = @V_TYPE
                  AND c.PARTY_CODE = @PARTY_CODE
                  AND a.COMP_CODE = @COMP_CODE
                  AND a.BRANCH_CODE = @BRANCH_CODE
                  AND (a.QTY - a.ADJ_QTY) > 0
                  AND c.STATUS <> 3
                ORDER BY a.V_TYPE, a.V_DATE, a.V_NO";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new
                            {
                                VNo = dr["VNo"],
                                VType = dr["VType"]?.ToString(),
                                VDate = dr["VDate"]?.ToString(),
                                ItemCode = dr["ItemCode"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Nos = dr["NOS"],
                                Qty = dr["QTY"],
                                BalQty = dr["BalQty"],
                                Rate = dr["RATE"],
                                TaxType = dr["TaxType"]?.ToString(),
                                PackPer = dr["PACK_PER"],
                                DiscPer = dr["DISC_PER"],
                                CGSTPer = dr["CGST_PER"],
                                SGSTPer = dr["SGST_PER"],
                                IGSTPer = dr["IGST_PER"],
                                CessPer = dr["CESS_PER"],
                                CessAmt = dr["CESS_AMT"],
                                VATPer = dr["VAT_PER"],
                                OthAmt = dr["OTH_AMT"],
                                Make = dr["Make"]?.ToString(),
                                Department = dr["Department"]?.ToString(),
                                Remarks = dr["REMARKS"]?.ToString(),
                                ReqType = dr["REQUEST_TYPE"]?.ToString(),
                                ReqNo = dr["REQUEST_NO"],
                                DeptCode = dr["DEPT_CODE"],
                                MakeCode = dr["MAKE_CODE"],
                                UCode = dr["UOM_CODE"],
                                TaxCode = dr["TAX_CODE"]
                            });
                        }
                    }
                }
            }

            return Json(list);
        }

        //==============Create Intimation Methods=====================
        private async Task<string> GenerateIntimationVNo(SqlConnection con, SqlTransaction transaction)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            // Prefix Year
            string prefix;

            using (var cmd = new SqlCommand(
                "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE=@YEAR_CODE",
                con, transaction))
            {
                AddParameterSafe(cmd, "@YEAR_CODE", globalVar.PubFYearCode);
                prefix = Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "0000";
            }

            // Last Intimation VNo
            string lastVNo;

            using (var cmd = new SqlCommand(@"
            SELECT TOP 1 V_NO
            FROM INTIMATION
            WHERE V_TYPE='INTP'
              AND COMP_CODE=@COMP_CODE
              AND BRANCH_CODE=@BRANCH_CODE
              AND YEAR_CODE=@YEAR_CODE
            ORDER BY V_NO DESC", con, transaction))
            {
                AddParameterSafe(cmd, "@COMP_CODE", globalVar.PubCompCode);
                AddParameterSafe(cmd, "@BRANCH_CODE", globalVar.PubBranchCode);
                AddParameterSafe(cmd, "@YEAR_CODE", globalVar.PubFYearCode);

                lastVNo = Convert.ToString(await cmd.ExecuteScalarAsync());
            }

            int lastNumber = 0;

            if (!string.IsNullOrEmpty(lastVNo) && lastVNo.Length >= 9)
            {
                string numericPart = lastVNo.Substring(lastVNo.Length - 5);
                int.TryParse(numericPart, out lastNumber);
            }

            string runningNo = (lastNumber + 1).ToString("D5");

            return prefix + runningNo;
        }

        private async Task<bool> PurchaseDeptIntimation(SqlConnection con, SqlTransaction transaction, PurchaseReceiptHeaderModel headerObj, List<ItemDetailModel> itemDetails)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            //================= Check Voucher Type =================
            if (headerObj.DocType != "SRPU" && headerObj.DocType != "SRJW")
                return false;

            //================= Generate Intimation No =================
            int intiNo = Convert.ToInt32(await GenerateIntimationVNo(con, transaction));
            string intiDocId = "INTP" + intiNo;

            //================= Delete Existing Intimation =================
            using (var cmdDelete = new SqlCommand(@"
            DELETE FROM INTIMATION
            WHERE MRN_TYPE=@MRN_TYPE
              AND MRN_NO=@MRN_NO
              AND DEPT_CODE=114
              AND COMP_CODE=@COMP_CODE
              AND BRANCH_CODE=@BRANCH_CODE
              AND YEAR_CODE=@YEAR_CODE", con, transaction))
            {
                AddParameterSafe(cmdDelete, "@MRN_TYPE", headerObj.DocType);
                AddParameterSafe(cmdDelete, "@MRN_NO", headerObj.DocNo);
                AddParameterSafe(cmdDelete, "@COMP_CODE", globalVar.PubCompCode);
                AddParameterSafe(cmdDelete, "@BRANCH_CODE", globalVar.PubBranchCode);
                AddParameterSafe(cmdDelete, "@YEAR_CODE", globalVar.PubFYearCode);

                await cmdDelete.ExecuteNonQueryAsync();
            }

            int sno = 1;
            int count = 0;

            int qcYn = 0;


            //================= Loop Items =================
            foreach (var item in itemDetails)
            {
                decimal orderQty = 0;

                using (var cmdQty = new SqlCommand(@"
                SELECT ISNULL(QTY,0)
                FROM ORDER2
                WHERE ITEM_CODE=@ITEM_CODE
                  AND V_TYPE=@V_TYPE
                  AND V_NO=@V_NO
                  AND COMP_CODE=@COMP_CODE
                  AND BRANCH_CODE=@BRANCH_CODE", con, transaction))
                {
                    AddParameterSafe(cmdQty, "@ITEM_CODE", item.ItemCode);
                    AddParameterSafe(cmdQty, "@V_TYPE", item.POType);
                    AddParameterSafe(cmdQty, "@V_NO", item.PONo);
                    AddParameterSafe(cmdQty, "@COMP_CODE", globalVar.PubCompCode);
                    AddParameterSafe(cmdQty, "@BRANCH_CODE", globalVar.PubBranchCode);

                    var result = await cmdQty.ExecuteScalarAsync();

                    orderQty = result == DBNull.Value ? 0 : Convert.ToDecimal(result);
                }

                using (var cmdQc = new SqlCommand(@"
                SELECT ISNULL(QC_YN,0)
                FROM ITEM_MASTER
                WHERE ITEM_CODE=@ITEM_CODE
                  AND COMP_CODE=@COMP_CODE", con, transaction))
                {
                    AddParameterSafe(cmdQc, "@ITEM_CODE", item.ItemCode);
                    AddParameterSafe(cmdQc, "@COMP_CODE", globalVar.PubCompCode);

                    var result = await cmdQc.ExecuteScalarAsync();
                    qcYn = result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }

                if (orderQty < item.RecQty)
                {
                    using (var cmdInsert = new SqlCommand(@"
                        INSERT INTO INTIMATION
                        (
                            COMP_CODE,BRANCH_CODE,YEAR_CODE,DOC_ID,V_TYPE,V_NO,V_DATE,
                            QC_YN,DEPT_CODE,DEPT_NAME,
                            ITEM_CODE,ITEM_NAME,UOM_CODE,UOM_NAME,
                            MAKE_CODE,
                            REC_QTY,BILL_QTY,
                            MRN_TYPE,MRN_NO,MRN_DATE,
                            REQUEST_TYPE,REQUEST_NO,
                            ORDER_TYPE,ORDER_NO,
                            REMARKS,UUSER,UDATE,AED,
                            WSID,LIP,LID,SNO
                        )
                        VALUES
                        (
                            @COMP_CODE,@BRANCH_CODE,@YEAR_CODE,@DOC_ID,@V_TYPE,@V_NO,@V_DATE,
                            @QC_YN,@DEPT_CODE,@DEPT_NAME,
                            @ITEM_CODE,@ITEM_NAME,@UOM_CODE,@UOM_NAME,
                            @MAKE_CODE,
                            @REC_QTY,@BILL_QTY,
                            @MRN_TYPE,@MRN_NO,@MRN_DATE,
                            @REQUEST_TYPE,@REQUEST_NO,
                            @ORDER_TYPE,@ORDER_NO,
                            @REMARKS,@UUSER,GETDATE(),'A',
                            @WSID,@LIP,@LID,@SNO
                        )", con, transaction))
                    {
                        AddParameterSafe(cmdInsert, "@COMP_CODE", globalVar.PubCompCode);
                        AddParameterSafe(cmdInsert, "@BRANCH_CODE", globalVar.PubBranchCode);
                        AddParameterSafe(cmdInsert, "@YEAR_CODE", globalVar.PubFYearCode);

                        AddParameterSafe(cmdInsert, "@DOC_ID", intiDocId);
                        AddParameterSafe(cmdInsert, "@V_TYPE", "INTP");
                        AddParameterSafe(cmdInsert, "@V_NO", intiNo);
                        AddParameterSafe(cmdInsert, "@V_DATE", headerObj.DocDate);

                        AddParameterSafe(cmdInsert, "@QC_YN", qcYn);

                        AddParameterSafe(cmdInsert, "@DEPT_CODE", 114);
                        AddParameterSafe(cmdInsert, "@DEPT_NAME", "Purchase");

                        AddParameterSafe(cmdInsert, "@ITEM_CODE", item.ItemCode);
                        AddParameterSafe(cmdInsert, "@ITEM_NAME", item.ItemName);

                        AddParameterSafe(cmdInsert, "@UOM_CODE", item.UOMCode);
                        AddParameterSafe(cmdInsert, "@UOM_NAME", item.UOMName);

                        AddParameterSafe(cmdInsert, "@MAKE_CODE", item.MakeCode);
                        //AddParameterSafe(cmdInsert, "@MAKE_NAME", item.MakeName);

                        AddParameterSafe(cmdInsert, "@REC_QTY", item.RecQty);
                        AddParameterSafe(cmdInsert, "@BILL_QTY", item.BillQty);

                        AddParameterSafe(cmdInsert, "@MRN_TYPE", headerObj.DocType);
                        AddParameterSafe(cmdInsert, "@MRN_NO", headerObj.DocNo);
                        AddParameterSafe(cmdInsert, "@MRN_DATE", headerObj.DocDate);

                        AddParameterSafe(cmdInsert, "@REQUEST_TYPE", item.ReqType);
                        AddParameterSafe(cmdInsert, "@REQUEST_NO", item.ReqNo);

                        AddParameterSafe(cmdInsert, "@ORDER_TYPE", item.POType);
                        AddParameterSafe(cmdInsert, "@ORDER_NO", item.PONo);

                        AddParameterSafe(cmdInsert, "@REMARKS", "Quantity increased.");

                        AddParameterSafe(cmdInsert, "@UUSER", globalVar.PubUserId);
                        AddParameterSafe(cmdInsert, "@WSID", globalVar.PubWorkStationID);
                        AddParameterSafe(cmdInsert, "@LIP", globalVar.PubLocalId);
                        AddParameterSafe(cmdInsert, "@LID", Environment.MachineName);

                        AddParameterSafe(cmdInsert, "@SNO", sno);

                        if (await cmdInsert.ExecuteNonQueryAsync() > 0)
                        {
                            count++;
                        }
                    }
                }

                sno++;
            }

            return count > 0;
        }

        [HttpPost]
        public async Task<IActionResult> CreateIntimation([FromForm] string Header, [FromForm] List<ItemDetailModel> ItemDetails)
        {
            var headerObj = JsonConvert.DeserializeObject<PurchaseReceiptHeaderModel>(Header);

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        bool result = await PurchaseDeptIntimation(
                            con,
                            transaction,
                            headerObj,
                            ItemDetails);

                        transaction.Commit();

                        return Json(new
                        {
                            success = result,
                            message = result
                                ? "Intimation created successfully."
                                : "No intimation generated."
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        return Json(new
                        {
                            success = false,
                            message = ex.Message
                        });
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckIntimation(string mrnType, int mrnNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                string query = @"SELECT COUNT(1)
                         FROM INTIMATION
                         WHERE MRN_TYPE = @MRN_TYPE
                         AND MRN_NO = @MRN_NO
                         AND COMP_CODE = @COMP_CODE
                         AND BRANCH_CODE = @BRANCH_CODE
                         AND YEAR_CODE = @YEAR_CODE";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@MRN_TYPE", mrnType);
                    cmd.Parameters.AddWithValue("@MRN_NO", mrnNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    bool exists = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;

                    return Json(new
                    {
                        status = exists,
                        message = exists ? "" : "Intimation not created."
                    });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePendingGateIn()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                    UPDATE GATE2
                    SET MRN_TYPE = '', MRN_NO = 0
                    WHERE V_DATE BETWEEN DATEADD(DAY,-90,CAST(GETDATE() AS DATE))
                                    AND DATEADD(DAY,1,CAST(GETDATE() AS DATE))
                      AND COMP_CODE=@CompCode
                      AND YEAR_CODE=@YearCode
                      AND BRANCH_CODE=@BranchCode;

                    UPDATE GATE2
                    SET
                        MRN_TYPE = PURCHASE2.V_TYPE,
                        MRN_NO   = PURCHASE2.V_NO
                    FROM GATE2
                    INNER JOIN PURCHASE2
                        ON GATE2.V_TYPE = PURCHASE2.GATE_TYPE
                       AND GATE2.V_NO = PURCHASE2.GATE_NO
                       AND GATE2.COMP_CODE = PURCHASE2.COMP_CODE
                       AND GATE2.BRANCH_CODE = PURCHASE2.BRANCH_CODE
                       AND GATE2.YEAR_CODE = PURCHASE2.YEAR_CODE
                    WHERE PURCHASE2.V_TYPE IN ('SRPU','SRJW','RCPT','RCPI','BFRC')
                      AND GATE2.V_DATE BETWEEN DATEADD(DAY,-90,CAST(GETDATE() AS DATE))
                                          AND DATEADD(DAY,1,CAST(GETDATE() AS DATE))
                      AND GATE2.COMP_CODE=@CompCode
                      AND GATE2.YEAR_CODE=@YearCode
                      AND GATE2.BRANCH_CODE=@BranchCode";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { status = true });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("QUOTATION2", vdate, vtype, vno);
            return Ok(result);
        }

    }
}

