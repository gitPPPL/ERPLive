using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class IncommingQCRMController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public IncommingQCRMController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/QualityControl/Transaction/IncommingQCRM/Index.cshtml");
        }
        public JsonResult GetddlDocType()
        {
            string query = $@" SELECT Code, Name FROM DOCTYPE_MAST WHERE DOCTYPE = 'QualityControl' AND Code NOT IN ('QCFF', 'QCST')";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpPost]
        public JsonResult GetDocNo(string docType, string docName)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = @"SELECT ISNULL(MAX(V_no), 0) + 1 AS NextVNo FROM QC1 WHERE V_TYPE = @V_TYPE 
                AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                var parameters = new[]
                {
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
                return Json(new { success = true, nextVNo = nextVNo, docType = docType });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetddlMRNNo(string VNo, string Vtype)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string vTypeFilter = Vtype switch
                {
                    "QCRM" => "AND a.V_TYPE = 'RCPT'",
                    "QCRI" => "AND a.V_TYPE = 'RCPI'",
                    "QCBF" => "AND a.V_TYPE = 'BFRC'",
                    _ => string.Empty
                };
                string query = $@" SELECT a.V_NO AS value, a.V_TYPE + CAST(a.V_NO AS VARCHAR) + ' | ' + FORMAT(a.V_DATE, 'dd/MM/yyyy' +' | ' + TRUCK_NO) AS text
                FROM PURCHASE1 a WHERE a.V_TYPE IN (SELECT Code FROM DOCTYPE_MAST WHERE DOCTYPE = 'MaterialReceipt' AND Code <> 'SRPU')
                AND a.COMP_CODE = {globalVar.PubCompCode} AND a.BRANCH_CODE = 1 AND a.YEAR_CODE = {globalVar.PubFYearCode} {vTypeFilter}
                ORDER BY  a.V_NO DESC;";
                var resultList = _dropdownService.GetDropdownList(query);
                return Json(resultList);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpPost]
        public IActionResult OnchangeItem([FromBody] ItemRequestCode model)
        {
            DataTable dt = new DataTable();
            var gv = _globalVariableService.GetGlobalVariables();

            string purchaseVType = model.V_TYPE switch
            {
                "QCRM" => "RCPT",
                "QCRI" => "RCPI",
                "QCBF" => "BFRC",
                _ => model.V_TYPE
            };
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT ITEM_CODE, ITEM_NAME, RECD_QTY FROM PURCHASE2 WHERE V_TYPE = @V_TYPE
                         AND V_NO = @V_NO AND ITEM_CODE = @ITEM_CODE AND COMP_CODE = @COMP_CODE
                         AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@V_TYPE", purchaseVType);
                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                    cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }
            }
            if (dt.Rows.Count == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Only the same item should be selected in this dropdown list."
                });
            }
            return Json(new
            {
                success = true,
                qty = Convert.ToDecimal(dt.Rows[0]["RECD_QTY"])
            });
        }
        public JsonResult GetddlQCIncharge()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT code, CONCAT(Name, '(', code, ')') AS Name FROM EMP_MAST WHERE Comp_code = '{globalVar.PubCompCode}' 
            AND Resign_date IS NULL AND Type IN ('Staff') ORDER BY Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlPartyName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select code, Name From SUBGROUP_MAST";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlChem()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT code, CONCAT(Name, '(', code, ')') AS Name FROM EMP_MAST WHERE Comp_code = '{globalVar.PubCompCode}' 
            AND Resign_date IS NULL AND Type IN ('Staff', 'Semi Staff') ORDER BY Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlItemName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT a.CODE, a.NAME FROM ITEM_MAST a LEFT JOIN ITEM_MGROUP b ON a.MGROUP_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE
             WHERE a.COMP_CODE = {globalVar.PubCompCode} AND b.MGROUP_TYPE IN ('Raw','Fuel') AND a.ACTIVE = 1 ORDER BY a.NAME";
            var moduleList = _dropdownService.GetDropdownList(query);

            return Json(moduleList);
        }
        [HttpPost]
        public async Task<IActionResult> GetGatDetailsList(string StrVNo, string StrV_type)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string strVType = "";

                if (!string.IsNullOrWhiteSpace(StrV_type))
                {
                    string firstPart = StrV_type.Split('|')[0].Trim();
                    strVType = Regex.Match(firstPart, @"^[A-Za-z]+").Value;
                }

                if (!int.TryParse(StrVNo, out int vNo))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid MRN No."
                    });
                }

                object header = null;
                var items = new List<object>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("usp_GetGateIncommingQCRMDetails", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@V_TYPE", strVType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    await con.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        // ==========================
                        // First Result Set
                        // ==========================
                        if (await reader.ReadAsync())
                        {
                            int isQCExists = reader["IsQCExists"] != DBNull.Value
                                ? Convert.ToInt32(reader["IsQCExists"])
                                : 0;

                            // QC Already Done OR No Item Found
                            if (isQCExists > 0)
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = reader["Message"]?.ToString()
                                });
                            }

                            header = new
                            {
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                V_NO = reader["V_NO"] != DBNull.Value
                                        ? Convert.ToInt32(reader["V_NO"])
                                        : 0,
                                V_DATE = reader["V_DATE"]?.ToString(),
                                PARTY_CODE = reader["PARTY_CODE"]?.ToString(),
                                PartyName = reader["PartyName"]?.ToString(),
                                TRANSPORT_NAME = reader["TRANSPORT_NAME"]?.ToString(),
                                BILL_NO = reader["BILL_NO"]?.ToString(),
                                BILL_DATE = reader["BILL_DATE"]?.ToString(),
                                TRUCK_NO = reader["TRUCK_NO"]?.ToString(),
                                CONTAINER_NO = reader["CONTAINER_NO"]?.ToString(),
                                InvoiceQty = reader["InvoiceQty"] != DBNull.Value
                                                ? Convert.ToDecimal(reader["InvoiceQty"])
                                                : 0,
                                ReceivedQty = reader["ReceivedQty"] != DBNull.Value
                                                ? Convert.ToDecimal(reader["ReceivedQty"])
                                                : 0,
                                ShortageQty = reader["ShortageQty"] != DBNull.Value
                                                ? Convert.ToDecimal(reader["ShortageQty"])
                                                : 0,
                                Bales = reader["Bales"] != DBNull.Value
                                                ? Convert.ToDecimal(reader["Bales"])
                                                : 0
                            };
                        }
                        else
                        {
                            return Json(new
                            {
                                success = false,
                                message = "No data found."
                            });
                        }
                        // ==========================
                        // Second Result Set
                        // ==========================
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(new
                                {
                                    ITEM_CODE = reader["ITEM_CODE"]?.ToString(),
                                    ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                    QTY = reader["RECD_QTY"] != DBNull.Value
                                            ? Convert.ToDecimal(reader["RECD_QTY"])
                                            : 0
                                });
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    header,
                    items
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
        [HttpPost]
        public async Task<IActionResult> GetItemDetails([FromBody] List<ItemRequest> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("No items provided.");
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var results = new List<object>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    foreach (var item in items)
                    {
                        using (SqlCommand command =
                               new SqlCommand("usp_GetGateIncommingQCRMFillList", con))
                        {
                            command.CommandType = CommandType.StoredProcedure;

                            command.Parameters.AddWithValue("@ItemCodes", item.ItemCode);
                            command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                            //command.Parameters.AddWithValue("@V_TYPE", string.IsNullOrWhiteSpace(item.V_TYPE)
                            //        ? DBNull.Value : (object)item.V_TYPE);

                            //command.Parameters.AddWithValue("@V_NO", item.V_NO.HasValue ? item.V_NO.Value : DBNull.Value);

                            command.Parameters.AddWithValue(
                                "@BRANCH_CODE",
                                gv.PubBranchCode);

                            command.Parameters.AddWithValue(
                                "@YEAR_CODE",
                                gv.PubFYearCode);

                            using (SqlDataReader reader =
                                   await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    results.Add(new
                                    {
                                        Item_Code = reader["ItemCode"]?.ToString(),
                                        Item_Name = reader["ItemName"]?.ToString(),
                                        QC_CODE = reader["QC_CODE"]?.ToString(),
                                        QCP_CODE = reader["QCP_CODE"]?.ToString(),
                                        Parameter = reader["Parameter"]?.ToString(),
                                        Unit = reader["Unit"]?.ToString(),
                                        QCP_STD = reader["QCP_STD"]?.ToString(),

                                        Result = reader["RESULT"] == DBNull.Value
                                            ? ""
                                            : reader["RESULT"].ToString(),

                                        DeductAmount = reader["DEDU_AMT"] == DBNull.Value
                                            ? ""
                                            : reader["DEDU_AMT"].ToString(),

                                        DeductNarr = reader["DEDU_NARR"] == DBNull.Value
                                            ? ""
                                            : reader["DEDU_NARR"].ToString(),

                                        Qty = reader["QTY"] == DBNull.Value
                                            ? 0
                                            : Convert.ToDecimal(reader["QTY"])
                                    });
                                }
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    data = results
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
        //=======================================save code===================================
        [HttpPost]
        public async Task<IActionResult> SaveQCData()
        {
            SqlTransaction transaction = null;

            try
            {
                using var reader = new StreamReader(Request.Body);
                string body = await reader.ReadToEndAsync();

                var request = JsonConvert.DeserializeObject<SaveQCRequest>(body);

                if (request == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Request data is null"
                    });
                }

                var header = request.Header;
                var globalVar = _globalVariableService.GetGlobalVariables();

                using var connection = _dbConnection.GetErpConnection();
                await connection.OpenAsync();

                transaction = connection.BeginTransaction();

                string DOC_ID = $"{header.docType}{header.docNo}";

                string gateNo = header.gateNo ?? "";
                string[] parts = gateNo.Split('|');

                string mrnPart = parts.Length > 0 ? parts[0].Trim() : "";
                string mrnDateText = parts.Length > 1 ? parts[1].Trim() : "";

                string mrnType = new string(mrnPart.TakeWhile(char.IsLetter).ToArray());
                string mrnNo = new string(mrnPart.SkipWhile(char.IsLetter).ToArray());

                DateTime? mrnDate = null;

                if (!string.IsNullOrWhiteSpace(mrnDateText))
                {
                    if (DateTime.TryParseExact(
                        mrnDateText,
                        "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out DateTime parsedDate))
                    {
                        mrnDate = parsedDate;
                    }
                }
                // 🔴 STEP 1: VALIDATION (IMPORTANT)
                var groupedItems = request.QCData.GroupBy(x => x.ItemCode);
                foreach (var group in groupedItems)
                {
                    int itemCode = Convert.ToInt32(group.Key);
                    decimal totalQty = group.Sum(x =>
                    {
                        decimal.TryParse(x.Qty, out decimal qty);
                        return qty;
                    });
                    using SqlCommand cmd = new SqlCommand(@" SELECT ISNULL(RECD_QTY,0) FROM PURCHASE2 WHERE V_NO = @V_NO
                        AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE 
                        AND ITEM_CODE = @ITEM_CODE", connection, transaction);

                    cmd.Parameters.AddWithValue("@V_NO", mrnNo);
                    cmd.Parameters.AddWithValue("@V_TYPE", mrnType);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                    var result = await cmd.ExecuteScalarAsync();
                    decimal dbQty = result == null ? 0 : Convert.ToDecimal(result);
                    string dbQtyDisplay = dbQty.ToString("0.####");
                    string totalQtyDisplay = totalQty.ToString("0.####");
                    if (totalQty == dbQty)
                    {
                        if (header.ACTION == "INSERT")
                        {
                            using (SqlCommand command = new SqlCommand(
                                "usp_InsertQC1IncommingQCRM",
                                connection,
                                transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;
                                command.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                command.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                command.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                                command.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
                                command.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
                                command.Parameters.AddWithValue("@V_DATE", Convert.ToDateTime(header.date));
                                command.Parameters.AddWithValue("@DOC_ID", DOC_ID);
                                command.Parameters.AddWithValue("@MRN_TYPE", mrnType);
                                command.Parameters.AddWithValue("@MRN_NO", mrnNo);
                                command.Parameters.AddWithValue("@PARTY_CODE", header.partyName);
                                command.Parameters.AddWithValue("@QC_INCHARGE", string.IsNullOrEmpty(header.qcIncharge) ? 0
                                        : Convert.ToInt32(header.qcIncharge));
                                command.Parameters.AddWithValue("@CHEMIST", string.IsNullOrEmpty(header.chem)
                                        ? 0 : Convert.ToInt32(header.chem));
                                command.Parameters.AddWithValue("@ITEM_CODE", 0);
                                command.Parameters.AddWithValue("@TRANSPORT", (object?)header.transport ?? DBNull.Value);
                                command.Parameters.AddWithValue("@TRUCK_NO",
                                    (object?)header.truckNo ?? DBNull.Value);
                                command.Parameters.AddWithValue("@CONTAINER_NO",
                                    (object?)header.containerNo ?? DBNull.Value);
                                command.Parameters.AddWithValue("@INV_QTY",
                                    header.invoiceQty ?? 0);
                                command.Parameters.AddWithValue("@RECD_QTY",
                                    header.recordedQty ?? 0);
                                command.Parameters.AddWithValue("@PUR_TYPE",
                                    (object?)header.purType ?? DBNull.Value);
                                command.Parameters.AddWithValue("@SHORT_QTY",
                                    header.shortage ?? 0);
                                command.Parameters.AddWithValue("@BILL_NO",
                                    (object?)header.billNo ?? DBNull.Value);
                                command.Parameters.AddWithValue("@BILL_DATE", string.IsNullOrWhiteSpace(header.billDate) ? DBNull.Value : Convert.ToDateTime(header.billDate));
                                command.Parameters.AddWithValue("@WASTE_WGT", header.wastage ?? 0);
                                command.Parameters.AddWithValue("@MRN_DATE",
                                    mrnDate.HasValue ? mrnDate.Value : DBNull.Value);
                                command.Parameters.AddWithValue("@BALES",
                                    header.bales ?? 0);
                                command.Parameters.AddWithValue("@DEDUCT_AMT",
                                    header.DeductAmount ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@DEDUCT_NARR",
                                    (object?)header.Narration ?? DBNull.Value);
                                command.Parameters.AddWithValue("@REMARKS",
                                    (object?)header.remarks ?? DBNull.Value);
                                command.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                command.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                command.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                command.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                command.Parameters.AddWithValue("@AED", "A");
                                command.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                                command.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                                command.Parameters.AddWithValue("@LID", Environment.MachineName);
                                command.Parameters.AddWithValue("@Action", "Insert");

                                await command.ExecuteNonQueryAsync();
                            }

                            // ==========================
                            // SAVE QC2 DETAILS
                            // ==========================

                            int sno = 1;
                            int rid = 1;
                            foreach (var item in request.QCData)
                            {

                                // CHECK: Details null or empty
                                if (item.Details == null || !item.Details.Any())
                                {
                                    return Json(new
                                    {
                                        success = false,
                                        message = "Details data missing hai. Please Update par click karein."
                                    });
                                }

                                int itemCode1 = 0;
                                int.TryParse(item.ItemCode, out itemCode1);

                                foreach (var detail in item.Details)
                                {
                                    //int RID = 1;
                                    using (SqlCommand detailCommand = new SqlCommand(
                                        "usp_InsertQC2IncommingQCRM",
                                        connection,
                                        transaction))
                                    {
                                        detailCommand.CommandType = CommandType.StoredProcedure;

                                        detailCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                        detailCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                        detailCommand.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                        detailCommand.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
                                        detailCommand.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
                                        detailCommand.Parameters.AddWithValue("@V_DATE", Convert.ToDateTime(header.date));
                                        detailCommand.Parameters.AddWithValue("@DOC_ID", DOC_ID);

                                        detailCommand.Parameters.AddWithValue("@ITEM_CODE", itemCode1);

                                        detailCommand.Parameters.AddWithValue("@QC_CODE",
                                            string.IsNullOrEmpty(detail.QC_CODE)
                                                ? DBNull.Value
                                                : Convert.ToInt32(detail.QC_CODE));

                                        detailCommand.Parameters.AddWithValue("@QCP_CODE",
                                            string.IsNullOrEmpty(detail.QCP_CODE)
                                                ? DBNull.Value
                                                : Convert.ToInt32(detail.QCP_CODE));

                                        detailCommand.Parameters.AddWithValue("@RID", rid);
                                        detailCommand.Parameters.AddWithValue("@WT_KG", DBNull.Value);
                                        detailCommand.Parameters.AddWithValue("@SNO", sno++);

                                        detailCommand.Parameters.AddWithValue("@UNIT",
                                            (object?)detail.Unit ?? DBNull.Value);

                                        decimal acceptance = 0;
                                        decimal.TryParse(detail.Level, out acceptance);

                                        detailCommand.Parameters.AddWithValue("@ACCEPTANCE", acceptance);

                                        decimal result1 = 0;
                                        decimal.TryParse(detail.Result, out result1);

                                        detailCommand.Parameters.AddWithValue("@RESULT", result1);

                                        detailCommand.Parameters.AddWithValue("@MIN_RES", DBNull.Value);
                                        detailCommand.Parameters.AddWithValue("@MAX_RES", DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@REMARK", DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@DEDU_AMT",
                                            detail.DeductAmont ?? (object)DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@ALLOW_AMT", DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@DEDU_NARR",
                                            (object?)detail.DeductNarr ?? DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@DEDU_AMT1", DBNull.Value);
                                        detailCommand.Parameters.AddWithValue("@DEDU_NARR1", DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                        detailCommand.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                        detailCommand.Parameters.AddWithValue("@AED", "A");
                                        detailCommand.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                                        detailCommand.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                                        detailCommand.Parameters.AddWithValue("@LID", Environment.MachineName);

                                        detailCommand.Parameters.AddWithValue("@Action", "Insert");

                                        await detailCommand.ExecuteNonQueryAsync();
                                    }
                                }

                                rid++;
                            }

                            await transaction.CommitAsync();

                            return Json(new
                            {
                                success = true,
                                message = "Data Saved Successfully"
                            });
                        }
                        else if (header.ACTION == "UPDATE")
                        {
                            using (SqlCommand command = new SqlCommand(
                                "usp_InsertQC1IncommingQCRM",
                                connection,
                                transaction))
                            {
                                command.CommandType = CommandType.StoredProcedure;

                                command.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                command.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                command.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                command.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
                                command.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
                                command.Parameters.AddWithValue("@V_DATE", Convert.ToDateTime(header.date));

                                command.Parameters.AddWithValue("@MRN_TYPE", mrnType);
                                command.Parameters.AddWithValue("@MRN_NO", mrnNo);
                                command.Parameters.AddWithValue("@MRN_DATE",
                                    mrnDate.HasValue ? mrnDate.Value : DBNull.Value);

                                command.Parameters.AddWithValue("@BALES", header.bales ?? 0);
                                command.Parameters.AddWithValue("@PARTY_CODE", header.partyName);

                                command.Parameters.AddWithValue("@BILL_NO",
                                    (object?)header.billNo ?? DBNull.Value);

                                command.Parameters.AddWithValue("@BILL_DATE",
                                    string.IsNullOrWhiteSpace(header.billDate)
                                        ? DBNull.Value
                                        : Convert.ToDateTime(header.billDate));

                                command.Parameters.AddWithValue("@TRANSPORT",
                                    (object?)header.transport ?? DBNull.Value);

                                command.Parameters.AddWithValue("@TRUCK_NO",
                                    (object?)header.truckNo ?? DBNull.Value);

                                command.Parameters.AddWithValue("@CONTAINER_NO",
                                    (object?)header.containerNo ?? DBNull.Value);

                                command.Parameters.AddWithValue("@INV_QTY",
                                    header.invoiceQty ?? 0);

                                command.Parameters.AddWithValue("@RECD_QTY",
                                    header.recordedQty ?? 0);

                                command.Parameters.AddWithValue("@SHORT_QTY",
                                    header.shortage ?? 0);

                                command.Parameters.AddWithValue("@REMARKS",
                                    (object?)header.remarks ?? DBNull.Value);

                                command.Parameters.AddWithValue("@DEDUCT_AMT",
                                    header.DeductAmount ?? (object)DBNull.Value);

                                command.Parameters.AddWithValue("@DEDUCT_NARR",
                                    (object?)header.Narration ?? DBNull.Value);

                                command.Parameters.AddWithValue("@PUR_TYPE",
                                    (object?)header.purType ?? DBNull.Value);

                                command.Parameters.AddWithValue("@WASTE_WGT",
                                    header.wastage ?? 0);

                                command.Parameters.AddWithValue("@ITEM_CODE", 0);
                                command.Parameters.AddWithValue("@ITEM_NAME", DBNull.Value);
                                command.Parameters.AddWithValue("@TENACITY_CODE", DBNull.Value);
                                command.Parameters.AddWithValue("@BALE_STATUSCODE", DBNull.Value);
                                command.Parameters.AddWithValue("@CREEL_NO", DBNull.Value);
                                command.Parameters.AddWithValue("@LAST_BALENO", DBNull.Value);
                                command.Parameters.AddWithValue("@LOT_NO", DBNull.Value);
                                command.Parameters.AddWithValue("@SHIFT", DBNull.Value);
                                command.Parameters.AddWithValue("@PROD_PLACECODE", DBNull.Value);
                                command.Parameters.AddWithValue("@PROD_LINE", DBNull.Value);
                                command.Parameters.AddWithValue("@STATUS", DBNull.Value);

                                command.Parameters.AddWithValue("@SAMPLE_RECDBY", DBNull.Value);
                                command.Parameters.AddWithValue("@FROM_BALENO", DBNull.Value);

                                command.Parameters.AddWithValue("@QC_INCHARGE",
                                    string.IsNullOrEmpty(header.qcIncharge)
                                        ? 0
                                        : Convert.ToInt32(header.qcIncharge));

                                command.Parameters.AddWithValue("@CHEMIST",
                                    string.IsNullOrEmpty(header.chem)
                                        ? 0
                                        : Convert.ToInt32(header.chem));

                                command.Parameters.AddWithValue("@QC_INCHARGENAME", DBNull.Value);
                                command.Parameters.AddWithValue("@CHEMISTNAME", DBNull.Value);

                                command.Parameters.AddWithValue("@NOS_PREQC", DBNull.Value);

                                command.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                command.Parameters.AddWithValue("@UDATE", DateTime.Now);

                                command.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                                command.Parameters.AddWithValue("@EDATE", DateTime.Now);

                                command.Parameters.AddWithValue("@AED", "E");
                                command.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                                command.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                                command.Parameters.AddWithValue("@LID", Environment.MachineName);

                                command.Parameters.AddWithValue("@Action", "Update");

                                await command.ExecuteNonQueryAsync();
                            }

                            // Delete old QC2 records first
                            using (SqlCommand deleteCmd = new SqlCommand(@"
                                DELETE FROM QC2
                                WHERE COMP_CODE=@COMP_CODE
                                AND YEAR_CODE=@YEAR_CODE
                                AND BRANCH_CODE=@BRANCH_CODE
                                AND V_TYPE=@V_TYPE
                                AND V_NO=@V_NO",
                                connection,
                                transaction))
                            {
                                deleteCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                deleteCmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                                deleteCmd.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
                                deleteCmd.Parameters.AddWithValue("@V_NO", header.docNo ?? "");

                                await deleteCmd.ExecuteNonQueryAsync();
                            }

                            // Reinsert QC2 details
                            int sno = 1;
                            int rid = 1;
                            foreach (var item in request.QCData)
                            {
                                int itemCode2 = 0;
                                int.TryParse(item.ItemCode, out itemCode2);

                                foreach (var detail in item.Details)
                                {
                                    using (SqlCommand detailCommand = new SqlCommand(
                                        "usp_InsertQC2IncommingQCRM",
                                        connection,
                                        transaction))
                                    {
                                        detailCommand.CommandType = CommandType.StoredProcedure;

                                        detailCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                        detailCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                        detailCommand.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                        detailCommand.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
                                        detailCommand.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
                                        detailCommand.Parameters.AddWithValue("@V_DATE", Convert.ToDateTime(header.date));
                                        detailCommand.Parameters.AddWithValue("@DOC_ID", DOC_ID);

                                        detailCommand.Parameters.AddWithValue("@ITEM_CODE", itemCode2);

                                        detailCommand.Parameters.AddWithValue("@QC_CODE",
                                            string.IsNullOrEmpty(detail.QC_CODE)
                                                ? DBNull.Value
                                                : Convert.ToInt32(detail.QC_CODE));

                                        detailCommand.Parameters.AddWithValue("@QCP_CODE",
                                            string.IsNullOrEmpty(detail.QCP_CODE)
                                                ? DBNull.Value
                                                : Convert.ToInt32(detail.QCP_CODE));

                                        detailCommand.Parameters.AddWithValue("@RID", rid);
                                        detailCommand.Parameters.AddWithValue("@WT_KG", DBNull.Value);
                                        detailCommand.Parameters.AddWithValue("@SNO", sno++);

                                        detailCommand.Parameters.AddWithValue("@UNIT",
                                            (object?)detail.Unit ?? DBNull.Value);

                                        decimal acceptance = 0;
                                        decimal.TryParse(detail.Level, out acceptance);

                                        detailCommand.Parameters.AddWithValue("@ACCEPTANCE", acceptance);

                                        decimal result2 = 0;
                                        decimal.TryParse(detail.Result, out result2);

                                        detailCommand.Parameters.AddWithValue("@RESULT", result2);

                                        detailCommand.Parameters.AddWithValue("@MIN_RES", DBNull.Value);
                                        detailCommand.Parameters.AddWithValue("@MAX_RES", DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@REMARK", DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@DEDU_AMT",
                                            detail.DeductAmont ?? (object)DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@ALLOW_AMT", DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@DEDU_NARR",
                                            (object?)detail.DeductNarr ?? DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@DEDU_AMT1", DBNull.Value);
                                        detailCommand.Parameters.AddWithValue("@DEDU_NARR1", DBNull.Value);

                                        detailCommand.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                        detailCommand.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                        detailCommand.Parameters.AddWithValue("@AED", "A");
                                        detailCommand.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                                        detailCommand.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                                        detailCommand.Parameters.AddWithValue("@LID", Environment.MachineName);

                                        detailCommand.Parameters.AddWithValue("@Action", "Insert");

                                        await detailCommand.ExecuteNonQueryAsync();
                                    }
                                }

                                rid++;
                            }
                            await transaction.CommitAsync();

                            return Json(new
                            {
                                success = true,
                                message = "Data Updated Successfully"
                            });
                        }

                    }
                    else
                    {
                        transaction?.Rollback();
                        return Json(new
                        {
                            success = false,
                            message = $"Item Code {itemCode}: Total Qty {totalQty} cannot exceed {dbQty}"
                        });
                    }

                }
                // INSERT
                return Json(new
                {
                    success = false,
                    message = "Invalid Action"
                });
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        
        private string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }
        
        public async Task<IActionResult> GetAllDatadetails([FromBody] RequestModel request)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new GateIncommingQCRM();

            try
            {
                if (!int.TryParse(request.vNo, out int vNo))
                    return BadRequest("Invalid gate number format.");

                string strVType = request.vType?.Length >= 4 ? request.vType.Substring(0, 4) : request.vType;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (var command = new SqlCommand("usp_GetGateIncommingQCRMList", con))
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

                        // ----------- Items List (pivot result) -----------
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
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.ItemResults.Add(new ItemResult
                                {
                                    ItemCodes = reader["ITEM_CODE"]?.ToString(),
                                    Result = reader["RESULT"]?.ToString(),
                                    RID = reader["RID"]?.ToString()
                                });
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

        //=========================================list Add popup===================================
        [HttpPost]
        public IActionResult GetbtnQCParameterList([FromBody] QCParameterModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "No data received" });
            }
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new List<object>();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand command = new SqlCommand("usp_GetIncommingQCRMPopupList", con))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    //  FIXED PARAMETERS
                    command.Parameters.AddWithValue("@V_TYPE", model.V_type);
                    command.Parameters.AddWithValue("@V_NO", model.VnNo);
                    command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    command.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    command.Parameters.AddWithValue("@ITEM_CODE", model.ItemCode);
                    command.Parameters.AddWithValue("@RID", model.RID);

                    con.Open();

                    using (SqlDataReader rdr = command.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            response.Add(new
                            {
                                Rid = rdr["Rid"],
                                QCID = rdr["QCID"],
                                QCPID = rdr["QCPID"],
                                Parameter = rdr["Parameter"],
                                Unit = rdr["UNIT"],
                                Level = rdr["Level"],
                                AllowAmt = rdr["ALLOW_AMT"],
                                DeduAmt = rdr["DEDU_AMT"],
                                DeduNarr = rdr["DEDU_NARR"],
                                ItemCode = rdr["ITEM_CODE"],
                                ItemName = rdr["ITEM_NAME"],
                                Result = rdr["RESULT"]
                            });
                        }
                    }
                }

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        //=========================================list Add popup===================================
        public class QCParameterModel
        {
            public string VnNo { get; set; }
            public string V_type { get; set; }
            public string ItemCode { get; set; }
            public string RID { get; set; }
        }
        public class ItemRequest
        {
            public int ItemCode { get; set; }
            public string? V_TYPE { get; set; }
            public int? V_NO { get; set; }
        }
        public class RequestModel
        {
            public string vNo { get; set; }
            public string vType { get; set; }
        }
        public class GateIncommingQCRM
        {
            public List<Dictionary<string, object>> Header { get; set; } = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> Items { get; set; } = new List<Dictionary<string, object>>();
            //public List<string> ItemCodes { get; set; } = new List<string>(); // Added this property to fix CS1061
            public List<ItemResult> ItemResults { get; set; } = new();
        }
        public class ItemResult
        {
            public string ItemCodes { get; set; }
            public string Result { get; set; }
            public string RID { get; set; }
        }

        // New Code start Block
        public class SaveQCRequest
        {
            public QCHeaderModel Header { get; set; }
            public List<QCItemModel> QCData { get; set; }
        }
        public class ItemRequestCode
        {
            public int V_NO { get; set; }
            public string V_TYPE { get; set; }
            public string ITEM_CODE { get; set; }
        }
        public class QCHeaderModel
        {
            public string? docType { get; set; }
            public string? docNo { get; set; }
            public string? date { get; set; }
            public string? gateNo { get; set; }
            public string? qcIncharge { get; set; }
            public string? chem { get; set; }
            public string? partyName { get; set; }
            public string? transport { get; set; }
            public string? truckNo { get; set; }
            public string? containerNo { get; set; }
            public decimal? invoiceQty { get; set; }
            public decimal? recordedQty { get; set; }
            public string? purType { get; set; }
            public decimal? shortage { get; set; }
            public string? billNo { get; set; }
            public string billDate { get; set; }
            public bool billDateChecked { get; set; }
            public decimal? wastage { get; set; }
            public string? gateDate { get; set; }
            public bool? gateDateChecked { get; set; }
            public int? bales { get; set; }
            public string? remarks { get; set; }
            public string MRNDate { get; set; }
            public decimal? DeductAmount { get; set; }
            public string? Narration { get; set; }
            public string? ACTION { get; set; }
        }
        public class QCItemModel
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string Qty { get; set; }
            public List<QCDetailModel> Details { get; set; }
        }
        public class QCDetailModel
        {
            public string? ItemCode { get; set; }
            public string? ItemName { get; set; }

            public string? QC_CODE { get; set; }
            public string? QCP_CODE { get; set; }

            public int? Qty { get; set; }

            public string? Parameter { get; set; }
            public string? Unit { get; set; }
            public string? Level { get; set; }
            public string? Result { get; set; }
            public decimal? DeductAmont { get; set; }
            public string? DeductNarr { get; set; }
        }
        public class GateDetailsRequest
        {
            public string StrVNo { get; set; }
            public string StrV_type { get; set; }
        }

        // New Cod End button
    }
}
