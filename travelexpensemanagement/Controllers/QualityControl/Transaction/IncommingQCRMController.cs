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
                string query = $@" SELECT a.V_NO AS value, a.V_TYPE + CAST(a.V_NO AS VARCHAR) + ' | ' + FORMAT(a.V_DATE, 'dd/MM/yyyy') AS text
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
                        // ===========================
                        // First Result Set (Header)
                        // ===========================
                        if (await reader.ReadAsync())
                        {
                            int isQCExists = reader["IsQCExists"] != DBNull.Value
                                ? Convert.ToInt32(reader["IsQCExists"])
                                : 0;

                            if (isQCExists == 1)
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
                                V_NO = Convert.ToInt32(reader["V_NO"]),
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

                        // ===========================
                        // Second Result Set (Items)
                        // ===========================
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(new
                                {
                                    ITEM_CODE = reader["ITEM_CODE"]?.ToString(),
                                    ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                    QTY = reader["QTY"] != DBNull.Value
                                            ? Convert.ToDecimal(reader["QTY"])
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

                            command.Parameters.AddWithValue(
                                "@V_TYPE",
                                string.IsNullOrWhiteSpace(item.V_TYPE)
                                    ? DBNull.Value
                                    : (object)item.V_TYPE);

                            command.Parameters.AddWithValue(
                                "@V_NO",
                                item.V_NO.HasValue
                                    ? item.V_NO.Value
                                    : DBNull.Value);

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

        //[HttpPost]
        //public async Task<IActionResult> GetItemDetails([FromBody] List<ItemRequest> items)
        //{
        //    if (items == null || items.Count == 0)
        //        return BadRequest("No items provided.");

        //    try
        //    {
        //        var gv = _globalVariableService.GetGlobalVariables();
        //        var results = new List<object>();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            await con.OpenAsync();

        //            foreach (var item in items)
        //            {
        //                using (var command = new SqlCommand("usp_GetGateIncommingQCRMFillList", con))
        //                {
        //                    command.CommandType = CommandType.StoredProcedure;

        //                    command.Parameters.AddWithValue("@ItemCodes", item.ItemCode);
        //                    command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

        //                    using (var reader = await command.ExecuteReaderAsync())
        //                    {
        //                        while (await reader.ReadAsync())
        //                        {
        //                            results.Add(new
        //                            {
        //                                Item_Code = reader["ItemCode"]?.ToString(),
        //                                Item_Name = reader["ItemName"]?.ToString(),
        //                                QC_CODE = reader["QC_CODE"]?.ToString(),
        //                                QCP_CODE = reader["QCP_CODE"]?.ToString(),
        //                                Parameter = reader["Parameter"]?.ToString(),
        //                                Unit = reader["Unit"]?.ToString(),
        //                                QCP_STD = reader["QCP_STD"]?.ToString(),
        //                                QTY = reader["QTY"] != DBNull.Value
        //                                      ? Convert.ToDecimal(reader["QTY"])
        //                                      : 0
        //                            });
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        return Json(new
        //        {
        //            success = true,
        //            data = results
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

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

                // ==========================
                // INSERT
                // ==========================
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

                    foreach (var item in request.QCData)
                    {
                        int itemCode = 0;
                        int.TryParse(item.ItemCode, out itemCode);

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

                                detailCommand.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                                detailCommand.Parameters.AddWithValue("@QC_CODE",
                                    string.IsNullOrEmpty(detail.QC_CODE)
                                        ? DBNull.Value
                                        : Convert.ToInt32(detail.QC_CODE));

                                detailCommand.Parameters.AddWithValue("@QCP_CODE",
                                    string.IsNullOrEmpty(detail.QCP_CODE)
                                        ? DBNull.Value
                                        : Convert.ToInt32(detail.QCP_CODE));

                                detailCommand.Parameters.AddWithValue("@RID", DBNull.Value);
                                detailCommand.Parameters.AddWithValue("@WT_KG", DBNull.Value);
                                detailCommand.Parameters.AddWithValue("@SNO", sno++);

                                detailCommand.Parameters.AddWithValue("@UNIT",
                                    (object?)detail.Unit ?? DBNull.Value);

                                decimal acceptance = 0;
                                decimal.TryParse(detail.Level, out acceptance);

                                detailCommand.Parameters.AddWithValue("@ACCEPTANCE", acceptance);

                                decimal result = 0;
                                decimal.TryParse(detail.Result, out result);

                                detailCommand.Parameters.AddWithValue("@RESULT", result);

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
                    return Json(new
                    {
                        success = false,
                        message = "Update logic not implemented yet."
                    });
                }

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




        //[HttpPost]
        //public async Task<IActionResult> SaveQCData()
        //{
        //    SqlTransaction transaction = null;
        //    try
        //    {
        //        using var reader = new StreamReader(Request.Body);
        //        string body = await reader.ReadToEndAsync();

        //        var request = JsonConvert.DeserializeObject<SaveQCRequest>(body);

        //        if (request == null)
        //        {
        //            return Json(new
        //            {
        //                success = false,
        //                message = "Request data is null"
        //            });
        //        }
        //        var header = request.Header;
        //        var globalVar = _globalVariableService.GetGlobalVariables();
        //        using var connection = _dbConnection.GetErpConnection();
        //        await connection.OpenAsync();
        //        transaction = connection.BeginTransaction();
        //        // HEADER VALUES
        //        string DOC_ID = $"{header.docType}{header.docNo}";
        //        string gateNo = header.gateNo ?? "";
        //        string[] parts = gateNo.Split('|');
        //        string mrnPart = parts.Length > 0 ? parts[0].Trim() : "";
        //        string mrnDateText = parts.Length > 1 ? parts[1].Trim() : "";
        //        string mrnType = new string(mrnPart.TakeWhile(char.IsLetter).ToArray());
        //        string mrnNo = new string(mrnPart.SkipWhile(char.IsLetter).ToArray());

        //        DateTime? mrnDate = null;

        //        if (!string.IsNullOrWhiteSpace(mrnDateText))
        //        {
        //            if (DateTime.TryParseExact(
        //                mrnDateText,
        //                "dd/MM/yyyy",
        //                System.Globalization.CultureInfo.InvariantCulture,
        //                System.Globalization.DateTimeStyles.None,
        //                out DateTime parsedDate))
        //            {
        //                mrnDate = parsedDate;
        //            }
        //        }
        //        // SAVE QC1 HEADER
        //        if (header.ACTION == "INSERT")
        //        {
        //            using SqlCommand command = new SqlCommand("usp_InsertQC1IncommingQCRM", connection, transaction);
        //            command.CommandType = CommandType.StoredProcedure;
        //            command.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
        //            command.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //            command.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
        //            command.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
        //            command.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
        //            command.Parameters.AddWithValue("@V_DATE", Convert.ToDateTime(header.date));
        //            command.Parameters.AddWithValue("@DOC_ID", DOC_ID);
        //            command.Parameters.AddWithValue("@MRN_TYPE", mrnType);
        //            command.Parameters.AddWithValue("@MRN_NO", mrnNo);
        //            command.Parameters.AddWithValue("@PARTY_CODE", header.partyName);
        //            command.Parameters.AddWithValue("@QC_INCHARGE", string.IsNullOrEmpty(header.qcIncharge) ? 0
        //                    : Convert.ToInt32(header.qcIncharge));
        //            command.Parameters.AddWithValue("@CHEMIST", string.IsNullOrEmpty(header.chem)
        //                    ? 0 : Convert.ToInt32(header.chem));
        //            command.Parameters.AddWithValue("@ITEM_CODE", 0);
        //            command.Parameters.AddWithValue("@TRANSPORT", (object?)header.transport ?? DBNull.Value);
        //            command.Parameters.AddWithValue("@TRUCK_NO",
        //                (object?)header.truckNo ?? DBNull.Value);
        //            command.Parameters.AddWithValue("@CONTAINER_NO",
        //                (object?)header.containerNo ?? DBNull.Value);
        //            command.Parameters.AddWithValue("@INV_QTY",
        //                header.invoiceQty ?? 0);
        //            command.Parameters.AddWithValue("@RECD_QTY",
        //                header.recordedQty ?? 0);
        //            command.Parameters.AddWithValue("@PUR_TYPE",
        //                (object?)header.purType ?? DBNull.Value);
        //            command.Parameters.AddWithValue("@SHORT_QTY",
        //                header.shortage ?? 0);
        //            command.Parameters.AddWithValue("@BILL_NO",
        //                (object?)header.billNo ?? DBNull.Value);
        //            command.Parameters.AddWithValue("@BILL_DATE", string.IsNullOrWhiteSpace(header.billDate) ? DBNull.Value : Convert.ToDateTime(header.billDate));
        //            command.Parameters.AddWithValue("@WASTE_WGT", header.wastage ?? 0);
        //            command.Parameters.AddWithValue("@MRN_DATE",
        //                mrnDate.HasValue ? mrnDate.Value : DBNull.Value);
        //            command.Parameters.AddWithValue("@BALES",
        //                header.bales ?? 0);
        //            command.Parameters.AddWithValue("@DEDUCT_AMT",
        //                header.DeductAmount ?? (object)DBNull.Value);
        //            command.Parameters.AddWithValue("@DEDUCT_NARR",
        //                (object?)header.Narration ?? DBNull.Value);
        //            command.Parameters.AddWithValue("@REMARKS",
        //                (object?)header.remarks ?? DBNull.Value);
        //            command.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //            command.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //            command.Parameters.AddWithValue("@EUSER", DBNull.Value);
        //            command.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //            command.Parameters.AddWithValue("@AED", "A");
        //            command.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
        //            command.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
        //            command.Parameters.AddWithValue("@LID", Environment.MachineName);
        //            command.Parameters.AddWithValue("@Action", "Insert");
        //            await command.ExecuteNonQueryAsync();

        //            int sno = 1;
        //            foreach (var item in request.QCData)
        //            {
        //                int itemCode = 0;
        //                int.TryParse(item.ItemCode, out itemCode);

        //                foreach (var detail in item.Details)
        //                {
        //                    using SqlCommand command = new SqlCommand("usp_InsertQC2IncommingQCRM", connection, transaction);
        //                    command.CommandType = CommandType.StoredProcedure;
        //                    command.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
        //                    command.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                    command.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
        //                    command.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
        //                    command.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
        //                    command.Parameters.AddWithValue("@V_DATE", Convert.ToDateTime(header.date));
        //                    command.Parameters.AddWithValue("@DOC_ID", DOC_ID);
        //                    command.Parameters.AddWithValue("@ITEM_CODE", itemCode);
        //                    command.Parameters.AddWithValue("@QC_CODE", detail.QC_CODE);
        //                    command.Parameters.AddWithValue("@QCP_CODE",
        //                        string.IsNullOrEmpty(detail.QCP_CODE) ? DBNull.Value : Convert.ToInt32(detail.QCP_CODE));
        //                    command.Parameters.AddWithValue("@WT_KG", DBNull.Value);
        //                    command.Parameters.AddWithValue("@RID", DBNull.Value);
        //                    command.Parameters.AddWithValue("@SNO", sno++);
        //                    command.Parameters.AddWithValue("@UNIT", (object?)detail.Unit ?? DBNull.Value);

        //                    decimal acceptance = 0;
        //                    decimal.TryParse(detail.Level, out acceptance);
        //                    command.Parameters.AddWithValue("@ACCEPTANCE", acceptance);
        //                    decimal result = 0;
        //                    decimal.TryParse(detail.Result, out result);
        //                    command.Parameters.AddWithValue("@RESULT", result);
        //                    command.Parameters.AddWithValue("@MIN_RES", DBNull.Value);
        //                    command.Parameters.AddWithValue("@MAX_RES", DBNull.Value);
        //                    command.Parameters.AddWithValue("@REMARK", DBNull.Value);
        //                    command.Parameters.AddWithValue("@DEDU_AMT", detail.DeductAmont ?? (object)DBNull.Value);
        //                    command.Parameters.AddWithValue("@ALLOW_AMT", DBNull.Value);
        //                    command.Parameters.AddWithValue("@DEDU_NARR", (object?)detail.DeductNarr ?? DBNull.Value);
        //                    command.Parameters.AddWithValue("@DEDU_AMT1", DBNull.Value);
        //                    command.Parameters.AddWithValue("@DEDU_NARR1", DBNull.Value);
        //                    command.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //                    command.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                    command.Parameters.AddWithValue("@EUSER", DBNull.Value);
        //                    command.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //                    command.Parameters.AddWithValue("@AED", "A");
        //                    command.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
        //                    command.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
        //                    command.Parameters.AddWithValue("@LID", Environment.MachineName);
        //                    command.Parameters.AddWithValue("@Action", "Insert");
        //                    await command.ExecuteNonQueryAsync();
        //                }
        //            }
        //            await transaction.CommitAsync();
        //            return Json(new
        //            {
        //                success = true,
        //                message = "Data Saved Successfully"
        //            });

        //        }
        //        else if (header.ACTION == "UPDATE")
        //        { 
        //        }
        //        // SAVE QC2 DETAILS



        //    }
        //    catch (Exception ex)
        //    {
        //        if (transaction != null)
        //            await transaction.RollbackAsync();

        //        return Json(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}


        //[HttpPost]
        //public async Task<IActionResult> SaveAllData([FromBody] IncommingSaveRequest request)
        //{
        //    if (!ModelState.IsValid || request == null)
        //        return BadRequest(ModelState);
        //    var gv = _globalVariableService.GetGlobalVariables();
        //    var header = request.Header;
        //    string gateNoFull = header.GateNo;
        //    string DoC_ID = gateNoFull.Split('|')[0].Trim();
        //    string V_type = header.DocType;

        //    string MRN_TYPE = Regex.Match(DoC_ID, @"^[A-Za-z]+").Value;
        //    string MRN_NO = Regex.Match(DoC_ID, @"\d+$").Value;

        //    string yourYearCode = gv.PubFYearCode;
        //    string yourCompCode = gv.PubCompCode;
        //    int yourBranchCode = 1;
        //    string yourVType = V_type;
        //    string yourVNo = header.DocNo;

        //    string DoC_IDNew = header.DocType + header.DocNo;
        //    DateTime yourVDate = DateTime.Parse(header.Date);
        //    string yourDocId = DoC_ID;
        //    string yourUserId = gv.PubUserId;
        //    string firstItemCode = request.Details.FirstOrDefault()?.Items?.FirstOrDefault()?.Keys.FirstOrDefault();
        //    using (var connection = _dbConnection.GetErpConnection())
        //    {
        //        await connection.OpenAsync();
        //        using (var transaction = connection.BeginTransaction())
        //        {
        //            try
        //            {
        //                // 1. Insert Header
        //                if (header.ACTION == "INSERT")
        //                {
        //                    using (var command = connection.CreateCommand())
        //                    {
        //                        command.Transaction = transaction;
        //                        command.CommandText = "usp_InsertQC1IncommingQCRM";
        //                        command.CommandType = CommandType.StoredProcedure;

        //                        command.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
        //                        command.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
        //                        command.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
        //                        command.Parameters.AddWithValue("@V_TYPE", yourVType);
        //                        command.Parameters.AddWithValue("@V_NO", yourVNo);
        //                        command.Parameters.AddWithValue("@V_DATE", yourVDate);
        //                        command.Parameters.AddWithValue("@DOC_ID", DoC_IDNew);
        //                        command.Parameters.AddWithValue("@MRN_TYPE", MRN_TYPE);
        //                        command.Parameters.AddWithValue("@MRN_NO", MRN_NO);
        //                        command.Parameters.AddWithValue("@QC_INCHARGE", int.TryParse(header.QcIncharge, out int qci) ? qci : 0);
        //                        command.Parameters.AddWithValue("@CHEMIST", int.TryParse(header.Chem, out int chem) ? chem : 0);
        //                        command.Parameters.AddWithValue("@ITEM_CODE", firstItemCode);
        //                        command.Parameters.AddWithValue("@TRANSPORT", Truncate(header.Transport, 50));
        //                        command.Parameters.AddWithValue("@TRUCK_NO", Truncate(header.TruckNo, 20));
        //                        command.Parameters.AddWithValue("@CONTAINER_NO", Truncate(header.ContainerNo, 20));
        //                        command.Parameters.AddWithValue("@INV_QTY", header.InvoiceQty ?? 0);
        //                        command.Parameters.AddWithValue("@RECD_QTY", header.RecordedQty ?? 0);
        //                        command.Parameters.AddWithValue("@PUR_TYPE", Truncate(header.PurType, 20));
        //                        command.Parameters.AddWithValue("@SHORT_QTY", header.Shortage ?? 0);
        //                        command.Parameters.AddWithValue("@BILL_NO", Truncate(header.BillNo?.ToString(), 50) ?? (object)DBNull.Value);
        //                        command.Parameters.AddWithValue("@BILL_DATE", DateTime.Parse(header.BillDate));
        //                        command.Parameters.AddWithValue("@WASTE_WGT", header.Wastage ?? 0);
        //                        //command.Parameters.AddWithValue("@MRN_DATE", DateTime.Parse(header.MRNDate));
        //                        if (DateTime.TryParse(header.MRNDate, out DateTime parsedDate))
        //                            command.Parameters.AddWithValue("@MRN_DATE", parsedDate);
        //                        else
        //                            command.Parameters.AddWithValue("@MRN_DATE", DBNull.Value);
        //                        command.Parameters.AddWithValue("@BALES", header.Bales ?? 0);

        //                        command.Parameters.AddWithValue("@DEDUCT_AMT", header.DeductAmount ?? (object)DBNull.Value);
        //                        command.Parameters.AddWithValue("@DEDUCT_NARR", header.Narration ?? (object)DBNull.Value);

        //                        command.Parameters.AddWithValue("@REMARKS", Truncate(header.Remarks, 200) ?? string.Empty);
        //                        command.Parameters.AddWithValue("@UUSER", yourUserId);
        //                        command.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                        command.Parameters.AddWithValue("@EUSER", "");
        //                        command.Parameters.AddWithValue("@EDATE", "");
        //                        command.Parameters.AddWithValue("@AED", "A");
        //                        command.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
        //                        command.Parameters.AddWithValue("@LIP", gv.PubLocalId);
        //                        command.Parameters.AddWithValue("@LID", Environment.MachineName);
        //                        command.Parameters.AddWithValue("@Action", "Insert");

        //                        // Fill unused parameters with null
        //                        foreach (var paramName in new[]
        //                        {
        //                        "@TENACITY_CODE", "@BALE_STATUSCODE",
        //                        "@CREEL_NO", "@LAST_BALENO", "@LOT_NO", "@SHIFT", "@PROD_PLACECODE", "@PROD_LINE", "@SAMPLE_RECDBY",
        //                        "@FROM_BALENO", "@QC_INCHARGENAME", "@CHEMISTNAME", "@NOS_PREQC"
        //                        })
        //                        {
        //                            command.Parameters.AddWithValue(paramName, DBNull.Value);
        //                        }
        //                        await command.ExecuteNonQueryAsync();
        //                    }

        //                    // 2. Insert Detail Rows and Dynamic Items

        //                    //var groupedDetails = request.Details
        //                    //.SelectMany(detail => detail.Items.Select(item => new { detail, itemCode = item.Key, itemValue = item.Value }))
        //                    //.GroupBy(x => x.itemCode);
        //                    var groupedDetails = request.Details
        //                    .SelectMany(detail => detail.Items.SelectMany(itemDict => itemDict.Select(kvp => new { detail, itemCode = kvp.Key, itemValue = kvp.Value }))).GroupBy(x => x.itemCode);

        //                    int sno = 1;

        //                    foreach (var detailGroup in groupedDetails)
        //                    {
        //                        int rid = 1;
        //                        foreach (var kvp in detailGroup)
        //                        {
        //                            string itemCodeStr = kvp.itemCode;
        //                            string itemValueStr = kvp.itemValue;
        //                            var detail = kvp.detail;

        //                            using (var command = connection.CreateCommand())
        //                            {
        //                                command.Transaction = transaction;
        //                                command.CommandText = "usp_InsertQC2IncommingQCRM";
        //                                command.CommandType = CommandType.StoredProcedure;

        //                                command.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
        //                                command.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
        //                                command.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
        //                                command.Parameters.AddWithValue("@V_TYPE", yourVType);
        //                                command.Parameters.AddWithValue("@V_NO", yourVNo);
        //                                command.Parameters.AddWithValue("@V_DATE", yourVDate);
        //                                command.Parameters.AddWithValue("@DOC_ID", DoC_IDNew);

        //                                command.Parameters.AddWithValue("@item_code", int.TryParse(itemCodeStr, out var itemCode) ? itemCode : 0);
        //                                command.Parameters.AddWithValue("@QC_CODE", int.TryParse(detail.QC_CODE, out var qcCode) ? qcCode : 0);
        //                                command.Parameters.AddWithValue("@QCP_CODE", int.TryParse(detail.QCP_CODE, out var qcpCode) ? qcpCode : 0);

        //                                command.Parameters.AddWithValue("@WT_KG", DBNull.Value);
        //                                command.Parameters.AddWithValue("@RID", detail.RowIndex);
        //                                command.Parameters.AddWithValue("@SNO", sno++);
        //                                command.Parameters.AddWithValue("@UNIT", detail.Unit ?? (object)DBNull.Value);
        //                                command.Parameters.AddWithValue("@ACCEPTANCE", detail.QCP_STD ?? (object)DBNull.Value);
        //                                command.Parameters.AddWithValue("@RESULT", string.IsNullOrWhiteSpace(itemValueStr) ? (object)DBNull.Value : itemValueStr);
        //                                command.Parameters.AddWithValue("@MIN_RES", DBNull.Value);
        //                                command.Parameters.AddWithValue("@MAX_RES", DBNull.Value);
        //                                command.Parameters.AddWithValue("@REMARK", DBNull.Value);

        //                                command.Parameters.AddWithValue("@DEDU_AMT", decimal.TryParse(detail.DeductAmt, out var deduAmt) ? deduAmt : (object)DBNull.Value);
        //                                command.Parameters.AddWithValue("@ALLOW_AMT", decimal.TryParse(detail.AllowAmt, out var allowAmt) ? allowAmt : (object)DBNull.Value);

        //                                command.Parameters.AddWithValue("@DEDU_NARR", Truncate(detail.DeductNarr, 100) ?? (object)DBNull.Value);
        //                                command.Parameters.AddWithValue("@DEDU_AMT1", DBNull.Value);
        //                                command.Parameters.AddWithValue("@DEDU_NARR1", DBNull.Value);

        //                                command.Parameters.AddWithValue("@UUSER", yourUserId);
        //                                command.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                                command.Parameters.AddWithValue("@EUSER", 1); // Changed from rid1++ to fixed or meaningful value
        //                                command.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //                                command.Parameters.AddWithValue("@AED", "A");
        //                                command.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
        //                                command.Parameters.AddWithValue("@LIP", gv.PubLocalId);
        //                                command.Parameters.AddWithValue("@LID", Environment.MachineName);
        //                                command.Parameters.AddWithValue("@Action", "Insert");

        //                                await command.ExecuteNonQueryAsync();
        //                            }
        //                        }
        //                    }
        //                    transaction.Commit();

        //                }
        //                else if (header.ACTION == "UPDATE")
        //                {
        //                    // 1. Update Header
        //                    using (var command = connection.CreateCommand())
        //                    {
        //                        command.Transaction = transaction;
        //                        command.CommandText = "usp_InsertQC1PreIncommingQCRM";
        //                        command.CommandType = CommandType.StoredProcedure;

        //                        command.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
        //                        command.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
        //                        command.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
        //                        command.Parameters.AddWithValue("@V_TYPE", yourVType);
        //                        command.Parameters.AddWithValue("@V_NO", yourVNo);
        //                        command.Parameters.AddWithValue("@V_DATE", yourVDate);
        //                        command.Parameters.AddWithValue("@DOC_ID", DoC_IDNew);
        //                        command.Parameters.AddWithValue("@QC_INCHARGE", int.TryParse(header.QcIncharge, out int qci) ? qci : 0);
        //                        command.Parameters.AddWithValue("@CHEMIST", int.TryParse(header.Chem, out int chem) ? chem : 0);
        //                        command.Parameters.AddWithValue("@ITEM_CODE", firstItemCode);
        //                        command.Parameters.AddWithValue("@TRANSPORT", Truncate(header.Transport, 50));
        //                        command.Parameters.AddWithValue("@TRUCK_NO", Truncate(header.TruckNo, 20));
        //                        command.Parameters.AddWithValue("@CONTAINER_NO", Truncate(header.ContainerNo, 20));
        //                        command.Parameters.AddWithValue("@INV_QTY", header.InvoiceQty ?? 0);
        //                        command.Parameters.AddWithValue("@RECD_QTY", header.RecordedQty ?? 0);
        //                        command.Parameters.AddWithValue("@PUR_TYPE", Truncate(header.PurType, 20));
        //                        command.Parameters.AddWithValue("@SHORT_QTY", header.Shortage ?? 0);
        //                        command.Parameters.AddWithValue("@BILL_NO", Truncate(header.BillNo?.ToString(), 50) ?? (object)DBNull.Value);
        //                        command.Parameters.AddWithValue("@BILL_DATE", DateTime.Parse(header.BillDate));
        //                        command.Parameters.AddWithValue("@WASTE_WGT", header.Wastage ?? 0);
        //                        command.Parameters.AddWithValue("@MRN_DATE", DateTime.Parse(header.MRNDate));
        //                        command.Parameters.AddWithValue("@BALES", header.Bales ?? 0);
        //                        command.Parameters.AddWithValue("@REMARKS", Truncate(header.Remarks, 200) ?? string.Empty);
        //                        command.Parameters.AddWithValue("@UUSER", yourUserId);
        //                        command.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                        command.Parameters.AddWithValue("@EUSER", "");
        //                        command.Parameters.AddWithValue("@EDATE", "");
        //                        command.Parameters.AddWithValue("@AED", "A");
        //                        command.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
        //                        command.Parameters.AddWithValue("@LIP", gv.PubLocalId);
        //                        command.Parameters.AddWithValue("@LID", Environment.MachineName);
        //                        command.Parameters.AddWithValue("@Action", "Update");

        //                        foreach (var paramName in new[]
        //                        {
        //                            "@MRN_TYPE", "@MRN_NO", "@DEDUCT_AMT", "@DEDUCT_NARR", "@TENACITY_CODE", "@BALE_STATUSCODE",
        //                            "@CREEL_NO", "@LAST_BALENO", "@LOT_NO", "@SHIFT", "@PROD_PLACECODE", "@PROD_LINE", "@SAMPLE_RECDBY",
        //                            "@FROM_BALENO", "@QC_INCHARGENAME", "@CHEMISTNAME", "@NOS_PREQC"
        //                        })
        //                        {
        //                            command.Parameters.AddWithValue(paramName, DBNull.Value);
        //                        }
        //                        await command.ExecuteNonQueryAsync();
        //                    }

        //                    // ✅ Optionally clear old details before inserting again
        //                    using (var deleteCmd = connection.CreateCommand())
        //                    {
        //                        deleteCmd.Transaction = transaction;
        //                        deleteCmd.CommandText = "DELETE FROM QC2 WHERE YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND V_TYPE=@V_TYPE AND V_NO=@V_NO";
        //                        deleteCmd.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
        //                        deleteCmd.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
        //                        deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
        //                        deleteCmd.Parameters.AddWithValue("@V_TYPE", yourVType);
        //                        deleteCmd.Parameters.AddWithValue("@V_NO", yourVNo);
        //                        await deleteCmd.ExecuteNonQueryAsync();
        //                    }

        //                    // 2. Re-Insert Detail Rows
        //                    int sno = 1;
        //                    foreach (var detail in request.Details)
        //                    {
        //                        foreach (var itemDict in detail.Items)
        //                        {
        //                            foreach (var kvp in itemDict)
        //                            {
        //                                string itemCodeStr = kvp.Key;
        //                                string itemValueStr = kvp.Value;

        //                                using (var command = connection.CreateCommand())
        //                                {
        //                                    command.Transaction = transaction;
        //                                    command.CommandText = "usp_InsertQC2PreIncommingQCRM";
        //                                    command.CommandType = CommandType.StoredProcedure;

        //                                    command.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
        //                                    command.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
        //                                    command.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
        //                                    command.Parameters.AddWithValue("@V_TYPE", yourVType);
        //                                    command.Parameters.AddWithValue("@V_NO", yourVNo);
        //                                    command.Parameters.AddWithValue("@V_DATE", yourVDate);
        //                                    command.Parameters.AddWithValue("@DOC_ID", DoC_IDNew);

        //                                    command.Parameters.AddWithValue("@item_code", int.TryParse(itemCodeStr, out var itemCode) ? itemCode : 0);
        //                                    command.Parameters.AddWithValue("@QC_CODE", int.TryParse(detail.QC_CODE, out var qcCode) ? qcCode : 0);
        //                                    command.Parameters.AddWithValue("@QCP_CODE", int.TryParse(detail.QCP_CODE, out var qcpCode) ? qcpCode : 0);

        //                                    command.Parameters.AddWithValue("@WT_KG", DBNull.Value);
        //                                    command.Parameters.AddWithValue("@RID", DBNull.Value);
        //                                    command.Parameters.AddWithValue("@SNO", sno++);
        //                                    command.Parameters.AddWithValue("@UNIT", detail.Unit ?? (object)DBNull.Value);
        //                                    command.Parameters.AddWithValue("@ACCEPTANCE", detail.QCP_STD ?? (object)DBNull.Value);
        //                                    command.Parameters.AddWithValue("@RESULT", string.IsNullOrWhiteSpace(itemValueStr) ? (object)DBNull.Value : itemValueStr);
        //                                    command.Parameters.AddWithValue("@MIN_RES", DBNull.Value);
        //                                    command.Parameters.AddWithValue("@MAX_RES", DBNull.Value);
        //                                    command.Parameters.AddWithValue("@REMARK", DBNull.Value);

        //                                    command.Parameters.AddWithValue("@DEDU_AMT", decimal.TryParse(detail.DeductAmt, out var deduAmt) ? deduAmt : (object)DBNull.Value);
        //                                    command.Parameters.AddWithValue("@ALLOW_AMT", decimal.TryParse(detail.AllowAmt, out var allowAmt) ? allowAmt : (object)DBNull.Value);

        //                                    command.Parameters.AddWithValue("@DEDU_NARR", Truncate(detail.DeductNarr, 100) ?? (object)DBNull.Value);
        //                                    command.Parameters.AddWithValue("@DEDU_AMT1", DBNull.Value);
        //                                    command.Parameters.AddWithValue("@DEDU_NARR1", DBNull.Value);

        //                                    command.Parameters.AddWithValue("@UUSER", yourUserId);
        //                                    command.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                                    command.Parameters.AddWithValue("@EUSER", "");
        //                                    command.Parameters.AddWithValue("@EDATE", "");
        //                                    command.Parameters.AddWithValue("@AED", "A");
        //                                    command.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
        //                                    command.Parameters.AddWithValue("@LIP", gv.PubLocalId);
        //                                    command.Parameters.AddWithValue("@LID", Environment.MachineName);
        //                                    command.Parameters.AddWithValue("@Action", "Update");

        //                                    await command.ExecuteNonQueryAsync();
        //                                }
        //                            }
        //                        }
        //                    }
        //                    transaction.Commit();
        //                    return Ok(new { success = true, message = "Header and detail data updated successfully." }); // ✅ FIX
        //                }
        //                else
        //                {
        //                    return BadRequest(new { success = false, message = "Invalid ACTION value." });
        //                }

        //                return Ok(new { success = true, message = "Header and detail data saved successfully." });
        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                return StatusCode(500, new { success = false, message = "Error saving data", error = ex.Message });
        //            }
        //        }
        //    }
        //}
        private string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }

        //public async Task<IActionResult> GetAllDatadetails([FromBody] RequestModel request)
        //{
        //    var gv = _globalVariableService.GetGlobalVariables();
        //    var response = new GatePreIncommingQCRM();
        //    try
        //    { 
        //        if (!int.TryParse(request.vNo, out int vNo))
        //            return BadRequest("Invalid gate number format.");

        //        string strVType = request.vType?.Length >= 4 ? request.vType.Substring(0, 4) : request.vType;

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        using (var command = new SqlCommand("usp_GetGateIncommingQCRMList", con))
        //        {
        //            command.CommandType = CommandType.StoredProcedure;
        //            command.Parameters.AddWithValue("@V_TYPE", strVType);
        //            command.Parameters.AddWithValue("@V_NO", vNo);
        //            command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
        //            command.Parameters.AddWithValue("@BRANCH_CODE", 1);
        //            command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
        //            await con.OpenAsync();
        //            using (var reader = await command.ExecuteReaderAsync())
        //            {
        //                // ----------- Header List -----------
        //                while (await reader.ReadAsync())
        //                {
        //                    var header = new Dictionary<string, object>();
        //                    for (int i = 0; i < reader.FieldCount; i++)
        //                        header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        //                    response.Header.Add(header);
        //                }
        //                // ----------- Items List (pivot result) -----------
        //                if (await reader.NextResultAsync())
        //                {
        //                    while (await reader.ReadAsync())
        //                    {
        //                        var item = new Dictionary<string, object>();
        //                        for (int i = 0; i < reader.FieldCount; i++)
        //                            item[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        //                        response.Items.Add(item);
        //                    }
        //                }
        //            }
        //        }
        //        return Json(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"An error occurred: {ex.Message}");
        //    }
        //}

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

                        // ----------- Item Codes List (third result set) -----------
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var itemCode = reader["ITEM_CODE"]?.ToString();
                                if (!string.IsNullOrEmpty(itemCode))
                                    response.ItemCodes.Add(itemCode); 
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
        //public class ItemRequest
        //{
        //    public string ItemCode { get; set; }
        //    public string ItemName { get; set; }
        //}
        public class GateIncommingQCRM
        {
            public List<Dictionary<string, object>> Header { get; set; } = new List<Dictionary<string, object>>();
            public List<Dictionary<string, object>> Items { get; set; } = new List<Dictionary<string, object>>();
            public List<string> ItemCodes { get; set; } = new List<string>(); // Added this property to fix CS1061
        }


        // New Code start Block
        public class SaveQCRequest
        {
            public QCHeaderModel Header { get; set; }
            public List<QCItemModel> QCData { get; set; }
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
            public List<QCDetailModel> Details { get; set; }
        }

        //public class QCDetailModel
        //{
        //    public string? QCP_CODE { get; set; }
        //    public string? Parameter { get; set; }
        //    public string? Unit { get; set; }
        //    public string? Level { get; set; }
        //    public string? Result { get; set; }
        //    public decimal? DeductAmont { get; set; }
        //    public string? DeductNarr { get; set; }
        //}

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


        // New Cod End button
    }
}
