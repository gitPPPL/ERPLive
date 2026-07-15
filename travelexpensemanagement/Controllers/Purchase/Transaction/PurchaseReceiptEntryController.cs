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
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
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
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IPurchaseReceiptEntryRepository _purchaseReceiptEntryRepository;

        public PurchaseReceiptEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, IPurchaseReceiptEntryRepository purchaseReceiptEntryRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _purchaseReceiptEntryRepository = purchaseReceiptEntryRepository;
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
            return View("~/Views/Purchase/Transaction/PurchaseReceiptEntry/Index.cshtml");
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
        public async Task<IActionResult> SaveAllData([FromForm] string Header, [FromForm] List<ItemDetailModel> ItemDetails,[FromForm] List<AttachmentModel> Attachments)
        {
            var result = await _purchaseReceiptEntryRepository.SaveAllData(
                Header,
                ItemDetails,
                Attachments);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    status = "validation",
                    message = result.Message
                });
            }

            return Ok(new
            {
                status = "success",
                message = result.Message
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetGatDetailsList(string StrVNo, string StrV_type)
        {
            var result = await _purchaseReceiptEntryRepository.GetGatDetailsList(StrVNo, StrV_type);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    status = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                status = true,
                message = result.Message,
                wbType = result.WBType,
                wbNo = result.WBNo,
                header = result.Header,
                items = result.Items
            });
        }

        [HttpPost]
        public async Task<IActionResult> GetAllDatadetails([FromBody] GetDetailsRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request");

            var result = await _purchaseReceiptEntryRepository.GetAllDatadetails(request);

            if (!result.Success)
            {
                return StatusCode(500, result.Message);
            }

            return Json(result.Data);
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
        public async Task<IActionResult> CreateIntimation([FromForm] string Header,[FromForm] List<ItemDetailModel> ItemDetails)
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
        public async Task<IActionResult> ValidateGate(string gateType, int gateNo, string docType, int currentVNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    //==============================
                    // 1. Duplicate MRN Check
                    //==============================
                    string mrnNo = "";

                    string duplicateQuery = @"
                    SELECT TOP 1
                        V_TYPE + CAST(V_NO AS VARCHAR(20))
                    FROM PURCHASE2
                    WHERE V_TYPE=@DocType
                      AND V_NO<>@CurrentVNo
                      AND CONCAT(GATE_TYPE,GATE_NO)=CONCAT(@GateType,@GateNo)
                      AND COMP_CODE=@CompCode
                      AND BRANCH_CODE=@BranchCode";

                    using (SqlCommand cmd = new SqlCommand(duplicateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DocType", docType);
                        cmd.Parameters.AddWithValue("@CurrentVNo", currentVNo);
                        cmd.Parameters.AddWithValue("@GateType", gateType);
                        cmd.Parameters.AddWithValue("@GateNo", gateNo);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                            mrnNo = result.ToString();
                    }
                     
                    if (!string.IsNullOrEmpty(mrnNo))
                    {
                        return Json(new
                        {
                            status = false,
                            message = "GATE No already exists in MRN No : " + mrnNo
                        });
                    }

                    //==============================
                    // 2. Approval Check
                    //==============================
                    string approvalStatus = "";

                    string approvalQuery = @"
                    SELECT FAPROV_STATUS
                    FROM GATE1
                    WHERE V_TYPE=@GateType
                      AND V_NO=@GateNo
                      AND COMP_CODE=@CompCode
                      AND BRANCH_CODE=@BranchCode";

                    using (SqlCommand cmd = new SqlCommand(approvalQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@GateType", gateType);
                        cmd.Parameters.AddWithValue("@GateNo", gateNo);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

                        var result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                            approvalStatus = result.ToString();
                    }
                    
                    approvalStatus = approvalStatus?.Trim();

                    if (!approvalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    {
                        return Json(new
                        {
                            status = false,
                            message = $"Gate No {gateType}{gateNo} not approved. Please contact Gate Inward operator to make Approval of this document."
                        });
                    }
                    //==============================
                    // Passed all validations
                    //==============================
                    return Json(new
                    {
                        status = true,
                        approvalStatus = approvalStatus
                    });
                }
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

        private static void AddParameterSafe(SqlCommand cmd, string paramName, object value)
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

    }
}
