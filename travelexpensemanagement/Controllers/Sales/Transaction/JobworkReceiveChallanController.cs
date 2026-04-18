using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.Data;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class JobworkReceiveChallanController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public JobworkReceiveChallanController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Sales/Transaction/JobworkReceiveChallan/Index.cshtml");
        }
        public IActionResult GetDocumentNo(string documentType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            int documentNo = 0;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" SELECT ISNULL(MAX(V_no), 0) + 1 AS documentNo FROM SALE1 WHERE V_TYPE = @VTYPE AND COMP_CODE = @COMP AND BRANCH_CODE = 1
                AND YEAR_CODE = @YEAR";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@VTYPE", documentType);
                    cmd.Parameters.AddWithValue("@COMP", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR", globalVar.PubFYearCode);
                    con.Open();
                    documentNo = Convert.ToInt32(cmd.ExecuteScalar());
                    con.Close();
                }
            }
            return Json(new { documentNo });
        }
        public JsonResult GetddlDocumentType()
        {
            string query = $@" Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('JobworkReceived') order by Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlPartyName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" Select Code,Name from SUBGROUP_MAST where comp_code={globalVar.PubCompCode} order by Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlSaleThrough()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select code, Name from SUBGROUP_MAST where NATURE like 'Broker' and COMP_CODE ={globalVar.PubCompCode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlConsignee()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT a.CODE AS Code, a.NAME AS P_name, ISNULL(a.ADD1, '') AS Add1,ISNULL(a.ADD2, '') AS Add2,ISNULL(a.ADD3, '') AS Add3,
            ISNULL(a.CITY_CODE, '') AS C_code,ISNULL(c.NAME, '') AS C_name,ISNULL(a.AGENT_CODE, '') AS agent_code,ISNULL(b.NAME, '') AS agent_name,a.GSTIN,
            a.Pincode FROM SUBGROUP_MAST a LEFT JOIN CITY_MAST c ON c.CODE = a.CITY_CODE LEFT JOIN SUBGROUP_MAST b  ON b.CODE = a.AGENT_CODE AND b.COMP_CODE = a.COMP_CODE
            WHERE a.COMP_CODE = {globalVar.PubCompCode};";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlTaxType()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select a.code,a.name,a.CGST_PER,a.SGST_PER,a.IGST_PER,a.TDS_PER,a.TCS_PER,a.OTH_PER from TAX_MAST a where a.ACTIVE = 1";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlReferenceNo()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT V_TYPE AS value, V_NO AS text FROM SALE1 WHERE V_TYPE = 'SAGT' AND ISNULL(Status, 0) <> 2  AND COMP_CODE = {globalVar.PubCompCode}  AND BRANCH_CODE = {globalVar.PubBranchCode}
            AND Year_code >= {globalVar.PubFYearCode};";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlGateNo()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select V_Type as value,V_NO as text from GATE1 where V_Type='INJB' and comp_code={globalVar.PubCompCode} and Branch_Code={globalVar.PubBranchCode} and Year_code>=4 order by V_TYpe,V_NO";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlWBNo()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code,Name from SUBGROUP_MAST  where comp_code={globalVar.PubCompCode} order by Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlPackNo(string docType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string vvtyp;
            if (docType == "SAJR")
            {
                vvtyp = (globalVar.PubCompCode == "2" || globalVar.PubCompCode == "5") ? "FPJR" : "FFRC";
            }
            else
            {
                vvtyp = "FPIS";
            }
            string query = $@" SELECT V_TYPE AS value, V_NO AS text FROM PRODUCTION1 WHERE V_TYPE = '{vvtyp}'
            AND COMP_CODE = {globalVar.PubCompCode}  AND BRANCH_CODE = {globalVar.PubBranchCode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlSaudaNo()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT V_TYPE AS value, V_NO AS text, Rate AS Rate, PARTY_CODE FROM SAUDA where V_TYPE='SAUD' 
            and FAPROV_STATUS='Approved' and COMP_CODE={globalVar.PubCompCode} and BRANCH_CODE={globalVar.PubBranchCode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetProductName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT b.CODE AS Code, b.NAME AS Name, b.hsn_code AS HSN FROM item_mast b  WHERE b.ACTIVE = 1 AND b.comp_code = {globalVar.PubCompCode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetTaxTypeList()
        {
            string sql = @"Select Code as value, NAME as text From TAX_MAST";
            var moduleList = _dropdownService.GetDropdownList(sql);
            return Json(moduleList);
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
                    sql = @" SELECT CODE, CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 FROM TAX_MAST WHERE CODE = @Code";
                    cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@Code", codeValue);
                }
                else
                {
                    sql = @" SELECT CODE, CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 FROM TAX_MAST WHERE NAME = @Name";
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
        public JsonResult GetddlTransport()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" Select CODE, Name From TRANSPORT_MAST where comp_code={globalVar.PubCompCode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlWBParty()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code,Name from SUBGROUP_MAST  where comp_code={globalVar.PubCompCode} order by Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlLoadParty()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code,Name from SUBGROUP_MAST where comp_code={globalVar.PubCompCode} order by Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlLoadNatureofJW()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT DISTINCT INSU_DETAIL AS Value, INSU_DETAIL AS Text FROM SALE1 WHERE V_TYPE IN ('SAJI','SAJR')  
            AND ISNULL(INSU_DETAIL,'') <> '' AND COMP_CODE = {globalVar.PubCompCode} ORDER BY INSU_DETAIL";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpPost]
        public async Task<IActionResult> GetReferenceDetails(string refValue, string refText)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var headerList = new List<Dictionary<string, object>>();
            var itemList = new List<Dictionary<string, object>>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_GetSaleDetailsByReference", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@VType", SqlDbType.VarChar).Value = refValue;
                cmd.Parameters.Add("@VNo", SqlDbType.VarChar).Value = refText;
                cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = gv.PubCompCode;
                cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = gv.PubBranchCode;

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    // ===== HEADER =====
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] =
                                reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        headerList.Add(row);
                    }
                    // ===== ITEMS =====
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] =
                                    reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            itemList.Add(row);
                        }
                    }
                }
            }
            return Json(new
            {
                success = headerList.Count > 0,
                header = headerList,
                items = itemList
            });
        }
        [HttpPost]
        public IActionResult Save([FromBody] JobworkReceive salesReturn)
        {
            if (salesReturn == null || salesReturn.FormData == null)
                return BadRequest("Invalid data received");
            if (salesReturn.RowData == null || !salesReturn.RowData.Any())
                return BadRequest("Row data is empty");

            var g = _globalVariableService.GetGlobalVariables();
            var f = salesReturn.FormData;
            string DOC_ID = "";
            if (f.ACTION == "INSERT")
            {
                DOC_ID = Clean(f.DocumentType) + Clean(f.DocumentNo);
            }
            else
            {
                DOC_ID = f.DocumentNo;
            }
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // ================= HEADER =================
                            using (SqlCommand cmd = new SqlCommand("sp_JobworkReceive", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.Add("@DOC_ID", SqlDbType.VarChar, 30).Value = DOC_ID;
                                cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;
                                cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = g.PubBranchCode;
                                cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = g.PubFYearCode;

                                cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar, 10).Value = Clean(f.DocumentType);
                                cmd.Parameters.Add("@V_NO", SqlDbType.VarChar, 20).Value = Clean(f.DocumentNo);
                                cmd.Parameters.Add("@V_DATE", SqlDbType.Date).Value = DateTime.TryParse(f.DocumentDate, out var vDate) ? vDate : DateTime.Now;

                                // BILL
                                cmd.Parameters.Add("@BILL_CODE", SqlDbType.VarChar, 20).Value = Clean("0");
                                cmd.Parameters.Add("@BILL_NAME", SqlDbType.VarChar, 200).Value = Clean(f.PartyName);
                                cmd.Parameters.Add("@BILL_ADD1", SqlDbType.VarChar, 200).Value = Clean(f.AddressL1);
                                cmd.Parameters.Add("@BILL_ADD2", SqlDbType.VarChar, 200).Value = Clean(f.AddressL2);
                                cmd.Parameters.Add("@BILL_ADD3", SqlDbType.VarChar, 200).Value = Clean(f.AddressL3);
                                cmd.Parameters.Add("@BILL_CITY", SqlDbType.VarChar, 100).Value = "";
                                cmd.Parameters.Add("@BILL_GST", SqlDbType.VarChar, 20).Value = "";
                                cmd.Parameters.Add("@BILL_PINCODE", SqlDbType.VarChar, 10).Value = Clean(f.Pincode);

                                // SHIP
                                cmd.Parameters.Add("@SHIP_CODE", SqlDbType.VarChar, 20).Value = Clean(f.Consignee);
                                cmd.Parameters.Add("@SHIP_NAME", SqlDbType.VarChar, 200).Value = Clean("");
                                cmd.Parameters.Add("@SHIP_ADD1", SqlDbType.VarChar, 200).Value = Clean(f.TransactionAddressL1);
                                cmd.Parameters.Add("@SHIP_ADD2", SqlDbType.VarChar, 200).Value = Clean(f.TransactionAddressL2);
                                cmd.Parameters.Add("@SHIP_ADD3", SqlDbType.VarChar, 200).Value = Clean(f.TransactionAddressL3);
                                cmd.Parameters.Add("@SHIP_CITY", SqlDbType.VarChar, 100).Value = "";
                                cmd.Parameters.Add("@SHIP_GST", SqlDbType.VarChar, 20).Value = "";
                                cmd.Parameters.Add("@SHIP_PINCODE", SqlDbType.VarChar, 10).Value = Clean(f.TransactionPIN);

                                // TAX / PACK
                                cmd.Parameters.Add("@TAX_CODE", SqlDbType.VarChar, 20).Value = Clean(f.TaxType);
                                cmd.Parameters.Add("@PACK_TYPE", SqlDbType.VarChar, 50).Value = Clean(f.ProductionType);
                                cmd.Parameters.Add("@PACK_NO", SqlDbType.Int).Value =
                                    int.TryParse(f.PackNo, out var pno) ? pno : 0;

                                // AMOUNT
                                AddDecimal(cmd, "@AMOUNT", f.TotalAmount);
                                cmd.Parameters.Add("@INSU_DETAIL", SqlDbType.VarChar, 20).Value = Clean(f.NatureJW);

                                // DEFAULT ZEROS
                                AddZeroDecimals(cmd,
                                    "@PACK_PER", "@PACK_AMT", "@CGST_PER", "@CGST_AMT",
                                    "@SGST_PER", "@SGST_AMT", "@IGST_PER", "@IGST_AMT",
                                    "@CESS_PER", "@CESS_AMT", "@LOAD_PER", "@LOAD_AMT",
                                    "@WB_AMT", "@FRT_AMT", "@ROUND_OFF", "@INSU_PER",
                                    "@INSU_AMT", "@TCS_PER", "@TCS_AMT", "@TDS_PER",
                                    "@TDS_AMT", "@WB_QTY", "@DISC_PER", "@DISC_AMT",
                                    "@FRT_TOPAY"
                                );

                                AddDecimal(cmd, "@NAMOUNT", f.TotalAmount);
                                AddDecimal(cmd, "@TOT_GROSS", f.TotalAmount);
                                AddDecimal(cmd, "@TOT_NET", f.TotalAmount);

                                cmd.Parameters.Add("@UUSER", SqlDbType.VarChar, 50).Value = g.PubUserId;
                                cmd.Parameters.Add("@WSID", SqlDbType.VarChar, 50).Value = g.PubWorkStationID;
                                cmd.Parameters.Add("@LIP", SqlDbType.VarChar, 50).Value = g.PubLocalId;
                                cmd.Parameters.Add("@LID", SqlDbType.VarChar, 50).Value = Environment.MachineName;
                                cmd.Parameters.Add("@Action", SqlDbType.VarChar, 20).Value = f.ACTION == "INSERT" ? "Insert" : "Update";

                                cmd.ExecuteNonQuery();
                            }
                            // ================= DETAILS =================

                            string deleteQuery = @"DELETE FROM SALE2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND YEAR_CODE = @YEAR_CODE AND COMP_CODE = @COMP_CODE 
                            AND BRANCH_CODE = @BRANCH_CODE";

                            using (SqlCommand cmdDelete = new SqlCommand(deleteQuery, con, tran))
                            {
                                cmdDelete.Parameters.Add("@V_NO", SqlDbType.VarChar, 20).Value = Clean(f.DocumentNo);
                                cmdDelete.Parameters.Add("@V_TYPE", SqlDbType.VarChar, 10).Value = Clean(f.DocumentType);
                                cmdDelete.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = g.PubFYearCode;
                                cmdDelete.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;
                                cmdDelete.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = g.PubBranchCode;
                                cmdDelete.ExecuteNonQuery();
                            }
                            foreach (var row in salesReturn.RowData)
                            {
                                using (SqlCommand cmd = new SqlCommand("sp_JobworkReceiveSales2", con, tran))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.Add("@DOC_ID", SqlDbType.VarChar, 30).Value = DOC_ID;
                                    cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = g.PubFYearCode;
                                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;
                                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = g.PubBranchCode;
                                    cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar, 10).Value = Clean(f.DocumentType);
                                    cmd.Parameters.Add("@V_NO", SqlDbType.VarChar, 20).Value = Clean(f.DocumentNo);
                                    cmd.Parameters.Add("@V_DATE", SqlDbType.Date).Value = DateTime.Now;
                                    cmd.Parameters.Add("@ITEM_CODE", SqlDbType.VarChar, 50).Value = Clean(row.Code);
                                    cmd.Parameters.Add("@ITEM_NAME", SqlDbType.VarChar, 200).Value = Clean(row.ProductName);
                                    cmd.Parameters.Add("@HSN_CODE", SqlDbType.VarChar, 20).Value = Clean(row.Hsn);

                                    cmd.Parameters.Add("@NOS", SqlDbType.Int).Value = row.Nos ?? 0;

                                    AddDecimal(cmd, "@GROSS_QTY", row.GrossQuantity);
                                    AddDecimal(cmd, "@QTY", row.NetQuantity);
                                    AddDecimal(cmd, "@RATE", row.Rate);
                                    AddDecimal(cmd, "@AMOUNT", row.Amount);

                                    AddDecimal(cmd, "@CGST_PER", row.CgstPer);
                                    AddDecimal(cmd, "@CGST_AMT", row.CgstAmt);
                                    AddDecimal(cmd, "@SGST_PER", row.SgstPer);
                                    AddDecimal(cmd, "@SGST_AMT", row.SgstAmt);
                                    AddDecimal(cmd, "@IGST_PER", row.IgstPer);
                                    AddDecimal(cmd, "@IGST_AMT", row.IgstAmt);

                                    cmd.Parameters.Add("@UserId", SqlDbType.VarChar, 50).Value = g.PubUserId;
                                    cmd.Parameters.Add("@WSID", SqlDbType.VarChar, 50).Value = g.PubWorkStationID;
                                    cmd.Parameters.Add("@LIP", SqlDbType.VarChar, 50).Value = g.PubLocalId;
                                    cmd.Parameters.Add("@LID", SqlDbType.VarChar, 50).Value = Environment.MachineName;
                                    cmd.Parameters.Add("@Action", SqlDbType.VarChar, 10).Value = "Insert";

                                    cmd.ExecuteNonQuery();
                                }
                            }
                            tran.Commit();
                            return Ok(new
                            {
                                success = true,
                                docId = DOC_ID,
                                message = "Sales Return saved successfully"
                            });
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost]
        public IActionResult GetID([FromBody] JobworkReceiveModel data)
        {
            if (data == null)
                return BadRequest("Invalid request data");

            var g = _globalVariableService.GetGlobalVariables();
            var headerList = new List<Dictionary<string, object>>();
            var itemList = new List<Dictionary<string, object>>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_GetSalesReturnByVoucher", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@VoucherNo", data.VoucherNo ?? "");
                cmd.Parameters.AddWithValue("@VType", data.vType ?? "");
                cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                cmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    // HEADER
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        headerList.Add(row);
                    }
                    // ITEMS
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                            itemList.Add(row);
                        }
                    }
                }
            }
            return Ok(new
            {
                Header = headerList,
                Items = itemList
            });
        }
        private static string Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        }
        private static void AddDecimal(SqlCommand cmd, string name, decimal? value)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = 18;
            p.Scale = 2;
            p.Value = value ?? 0;
        }
        private static void AddZeroDecimals(SqlCommand cmd, params string[] names)
        {
            foreach (var name in names)
                AddDecimal(cmd, name, 0);
        }
    }
}
