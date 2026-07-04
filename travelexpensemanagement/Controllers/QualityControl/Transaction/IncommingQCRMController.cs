using Azure;
using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using StackExchange.Redis;
using System.Data;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
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

        [HttpPost]
        public IActionResult CreateSession()
        {
            try
            {
                string userID = _globalVariableService.GetGlobalVariables().PubUserId.ToString();
                string sessionId = $"{DateTime.Now:ddMMyyyyHHmmss}{userID}";
                HttpContext.Session.SetString("SESSION_ID", sessionId);

                return Json(new
                {
                    success = true,
                    sessionId = sessionId
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
                        // First Result Set
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
                        // Second Result Set
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

        //==================================Update Temp Table=================================
        [HttpPost]
        public IActionResult InsertTempQC([FromBody] TempQCRequest model)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            var globalVar = _globalVariableService.GetGlobalVariables();
                            string DocID = model.V_TYPE + model.V_NO;
                            string sessionId = HttpContext.Session.GetString("SESSION_ID");

                            // DELETE OLD DATA

                            //string deleteSql = @" DELETE FROM Temp_QC2 WHERE SESSION_ID = @SESSION_ID AND V_TYPE = @V_TYPE AND V_NO = @V_NO AND YEAR_CODE = @YEAR_CODE
                            //AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE";

                            //using (SqlCommand deleteCmd = new SqlCommand(deleteSql, con, tran))
                            //{
                            //    deleteCmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                            //    deleteCmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            //    deleteCmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            //    deleteCmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            //    deleteCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            //    deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                            //    deleteCmd.ExecuteNonQuery();
                            //}
                                // GET MAX RID
                                int maxRID = 0;

                            string ridQuery = @"SELECT ISNULL(MAX(RID),0) FROM Temp_QC2 WHERE SESSION_ID=@SESSION_ID  AND V_TYPE=@V_TYPE
                            AND V_NO=@V_NO
                            AND YEAR_CODE=@YEAR_CODE
                            AND COMP_CODE=@COMP_CODE
                            AND BRANCH_CODE=@BRANCH_CODE";

                            using (SqlCommand cmd = new SqlCommand(ridQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                maxRID = Convert.ToInt32(cmd.ExecuteScalar());
                            }


                            int Maxsno = 1;

                            string SnoQuery = @"SELECT ISNULL(MAX(SNO),0) FROM Temp_QC2 WHERE SESSION_ID=@SESSION_ID  AND V_TYPE=@V_TYPE
                            AND V_NO=@V_NO
                            AND YEAR_CODE=@YEAR_CODE
                            AND COMP_CODE=@COMP_CODE
                            AND BRANCH_CODE=@BRANCH_CODE";

                            using (SqlCommand cmd = new SqlCommand(SnoQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@SESSION_ID", sessionId);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                Maxsno = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            int currentRID = maxRID;
                            int sno = Maxsno;
                            int? previousItemCode = null;

                            // INSERT DATA
                            foreach (var item in model.Details)
                            {
                                if (previousItemCode == null ||
                                    previousItemCode != item.ItemCode)
                                {
                                    currentRID++;
                                    //sno = 1;
                                    previousItemCode = item.ItemCode;
                                }
                                sno++;
                                string insertSql = @"
                                    INSERT INTO Temp_QC2
                                    (
                                        SESSION_ID, YEAR_CODE,  COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DOC_ID, ITEM_CODE, QC_CODE, QCP_CODE, WT_KG,
                                        RID, SNO, UNIT, ACCEPTANCE, RESULT, DEDU_AMT1, DEDU_NARR1, UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID, UserID)
                                    VALUES
                                    ( @SESSION_ID, @YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO, @V_DATE, @DOC_ID, @ITEM_CODE, @QC_CODE, @QCP_CODE, @WT_KG,
                                    @RID, @SNO, @UNIT, @ACCEPTANCE, @RESULT, @DEDU_AMT1, @DEDU_NARR1, @UUSER, @UDATE, @EUSER, @EDATE, @AED, @WSID, @LIP, @LID, @UserID )";

                                using (SqlCommand command = new SqlCommand(insertSql, con, tran))
                                {
                                    command.Parameters.AddWithValue("@SESSION_ID", sessionId);

                                    command.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                    command.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                    command.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                                    command.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? "");
                                    command.Parameters.AddWithValue("@V_NO", model.V_NO ?? 0);
                                    command.Parameters.AddWithValue("@V_DATE", DateTime.Now);
                                    command.Parameters.AddWithValue("@DOC_ID", DocID);

                                    command.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode ?? 0);
                                    command.Parameters.AddWithValue("@QC_CODE", item.QC_CODE ?? 0);
                                    command.Parameters.AddWithValue("@QCP_CODE", item.QCP_CODE ?? 0);

                                    command.Parameters.AddWithValue("@WT_KG",
                                        item.Qty.HasValue ? Convert.ToInt32(item.Qty.Value) : DBNull.Value);

                                    command.Parameters.AddWithValue("@RID", currentRID);
                                    command.Parameters.AddWithValue("@SNO", sno);

                                    command.Parameters.AddWithValue("@UNIT",
                                        string.IsNullOrWhiteSpace(item.Unit)
                                        ? DBNull.Value
                                        : item.Unit);

                                    command.Parameters.AddWithValue("@ACCEPTANCE",
                                        item.Level.HasValue
                                        ? item.Level.Value
                                        : DBNull.Value);

                                    command.Parameters.AddWithValue("@RESULT",
                                        item.Result.HasValue
                                        ? item.Result.Value
                                        : DBNull.Value);

                                    command.Parameters.AddWithValue("@DEDU_AMT1",
                                        item.DeductAmount.HasValue
                                        ? item.DeductAmount.Value
                                        : DBNull.Value);

                                    command.Parameters.AddWithValue("@DEDU_NARR1",
                                        string.IsNullOrWhiteSpace(item.DeductNarr)
                                        ? DBNull.Value
                                        : item.DeductNarr);

                                    command.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                    command.Parameters.AddWithValue("@UDATE", DateTime.Now);

                                    command.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                                    command.Parameters.AddWithValue("@EDATE", DateTime.Now);

                                    command.Parameters.AddWithValue("@AED", "E");
                                    command.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "");
                                    command.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "");
                                    command.Parameters.AddWithValue("@LID", Environment.MachineName);
                                    command.Parameters.AddWithValue("@UserID", globalVar.PubUserId);

                                    command.ExecuteNonQuery();
                                }
                               // sno++;
                            }

                            tran.Commit();

                            return Json(new
                            {
                                success = true,
                                message = "Data saved successfully"
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
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        public class TempQCRequest
        {
            public string? V_TYPE { get; set; }
            public int? V_NO { get; set; }

            public List<TempQCDetail> Details { get; set; } = new();
        }

        public class TempQCDetail
        {
            public int? ItemCode { get; set; }
            public int? QC_CODE { get; set; }
            public int? QCP_CODE { get; set; }
            public decimal? Qty { get; set; }

            public string? Unit { get; set; }

            // Since ACCEPTANCE is decimal in SQL
            public decimal? Level { get; set; }

            public decimal? Result { get; set; }
            public decimal? DeductAmount { get; set; }
            public string? DeductNarr { get; set; }
        }

        //==================================Update Temp Table=================================

        //===================================calculator start Block==========================================
        [HttpPost]
        public IActionResult Checkcalculator([FromBody] Checkcalculatormodl model)
        {
            if (model == null || model.Details == null || !model.Details.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid request data"
                });
            }
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            var QcResultWeight = model.Details.FirstOrDefault()?.RESULT ?? 0;
                            foreach (var item in model.Details)
                            {
                                // 1. GET QC HEADER (qc_mast1)
                                decimal StdResult = 0;
                                string headerQuery = @"
                                SELECT TOP 1 ISNULL(DEDUCT_TYPE,'') AS DEDUCT_TYPE, ISNULL(DEDUCT_QTY,'') AS DEDUCT_QTY, QCP_STD
                                FROM qc_mast1 WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE AND QCP_CODE = @QCP_CODE";

                                QCResult header = new QCResult();

                                using (SqlCommand cmd = new SqlCommand(headerQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                    cmd.Parameters.AddWithValue("@CODE", item.QC_CODE);
                                    cmd.Parameters.AddWithValue("@QCP_CODE", item.QCP_CODE);

                                    using (var dr = cmd.ExecuteReader())
                                    {
                                        if (dr.Read())
                                        {
                                            header.DEDUCT_TYPE = dr["DEDUCT_TYPE"].ToString();
                                            header.DEDUCT_QTY = dr["DEDUCT_QTY"].ToString();
                                            //StdResult = dr["QCP_STD"].ToString();
                                            StdResult = dr["QCP_STD"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["QCP_STD"]);
                                            //stdresult.=dr("std_level")
                                        }
                                    }
                                }

                                if (header.DEDUCT_TYPE == "NA" || string.IsNullOrEmpty(header.DEDUCT_QTY))
                                    continue;

                                decimal finaldeductrate = 0;

                                string deductType = header.DEDUCT_TYPE;
                                string deductQty = header.DEDUCT_QTY;
                                int srno = 0;
                                string matchedDedType = "";

                                // 2. GET LAND RATE (PURCHASE2)
                                decimal landedRate = 0;

                                string landQuery = @" SELECT TOP 1 ISNULL(LAND_RATE,0) FROM PURCHASE2 WHERE V_TYPE = @V_TYPE
                                       AND V_NO = @V_NO AND ITEM_CODE = @ITEM_CODE AND COMP_CODE = @COMP_CODE
                                       and year_code=@year_code AND BRANCH_CODE = @BRANCH_CODE";

                                string purchaseVType = model.V_TYPE switch
                                {
                                    "QCRM" => "RCPT",
                                    "QCRI" => "RCPI",
                                    "QCBF" => "BFRC",
                                    _ => model.V_TYPE
                                };

                                using (SqlCommand cmd = new SqlCommand(landQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@V_TYPE", purchaseVType ?? "");
                                    cmd.Parameters.AddWithValue("@V_NO", model.MRN_NO ?? "");
                                    cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                    cmd.Parameters.AddWithValue("@year_code", globalVar.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                                    object res = cmd.ExecuteScalar();
                                    landedRate = Convert.ToDecimal(res ?? 0);
                                }

                                // 3. GET QC RANGE (qc_mast2)

                                string rangeQuery = @" SELECT SRNO, FROM_RESULT, TO_RESULT, DEDUCT_RATE, DEDUCT_TYPE FROM QC_MAST2 WHERE 
                                    CODE = @CODE AND QCP_CODE = @QCP_CODE AND COMP_CODE = @COMP_CODE ORDER BY SRNO";

                                var ranges = new List<QCRangeRow>();

                                using (SqlCommand cmd = new SqlCommand(rangeQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@CODE", item.QC_CODE);
                                    cmd.Parameters.AddWithValue("@QCP_CODE", item.QCP_CODE);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                                    using (var dr = cmd.ExecuteReader())
                                    {
                                        while (dr.Read())
                                        {
                                            ranges.Add(new QCRangeRow
                                            {
                                                SRNO = Convert.ToInt32(dr["SRNO"]),
                                                FROM_RESULT = Convert.ToDecimal(dr["FROM_RESULT"]),
                                                TO_RESULT = Convert.ToDecimal(dr["TO_RESULT"]),
                                                DEDUCT_RATE = Convert.ToDecimal(dr["DEDUCT_RATE"]),
                                                DEDUCT_TYPE = dr["DEDUCT_TYPE"].ToString(),
                                            });
                                        }
                                    }
                                }
                                // 4. MATCH RANGE (VB logic)
                                decimal deductAmt = 0;
                                string deductNarr = "";
                                string Parameter = item.Parameter;

                                foreach (var r in ranges)
                                {
                                    if (item.RESULT >= r.FROM_RESULT && item.RESULT <= r.TO_RESULT)
                                    {
                                        finaldeductrate = r.DEDUCT_RATE;
                                        matchedDedType = r.DEDUCT_TYPE;
                                        srno = r.SRNO;
                                        break;

                                    }
                                }
                                // 5. EXTRA LOGIC (VB style future extension)
                                if (finaldeductrate > 0 || landedRate > 0)
                                {
                                    if (deductType == "Fix" && deductQty == "Fix")
                                    {
                                        deductAmt = QcResultWeight * finaldeductrate;
                                        deductAmt = Math.Round(deductAmt, 0);
                                        // Narration
                                        //deductNarr = $"{Parameter}: {QcResultWeight:0.#####} * {finaldeductrate:0.00} = {deductAmt:0}";

                                        if (deductAmt > 0)
                                        {
                                            deductNarr = $"{Parameter}: {QcResultWeight:0.#####} * {finaldeductrate:0.00} = {deductAmt:0}";
                                        }
                                        else
                                        {
                                            deductNarr = "";
                                        }   
                                    }
                                    else if (deductType == "Fix" && deductQty == "%")
                                    {
                                        decimal accessPercentage = item.RESULT - StdResult;
                                        if (accessPercentage > 0)
                                        {
                                            decimal deductQtyValue = (QcResultWeight * accessPercentage) / 100m;
                                            deductAmt = Math.Round(deductQtyValue * finaldeductrate, 2);

                                            if (deductAmt > 0)
                                            {
                                                deductNarr = $"{Parameter}: {QcResultWeight:0.#####} % {accessPercentage:0.####} = {deductQtyValue:0.00} * {finaldeductrate:0.00} = {deductAmt:0.00}";
                                            }
                                            else
                                            {
                                                deductNarr = "";
                                            }
                                            //deductNarr = $"{Parameter}: {QcResultWeight:0.#####} % {accessPercentage:0.####} = {deductQtyValue:0.00} * {finaldeductrate:0.00} = {deductAmt:0.00}";
                                        }
                                    }
                                    else if (deductType == "Landed" && deductQty == "%")
                                    {
                                        decimal accessPercentage = item.RESULT - StdResult;
                                        if (accessPercentage > 0)
                                        {
                                            decimal deductQtyValue = (QcResultWeight * accessPercentage) / 100m;

                                            deductAmt = Math.Round(deductQtyValue * landedRate, 2);
                                            if (deductAmt > 0)
                                            {
                                                deductNarr = $"{Parameter}: {QcResultWeight:0.#####} % {accessPercentage:0.####} = {deductQtyValue:0.00} * {landedRate:0.00} = {deductAmt:0.00}";
                                            }
                                            else
                                            {
                                                deductNarr = "";
                                            }
                                            //deductNarr = $"{Parameter}: {QcResultWeight:0.#####} % {accessPercentage:0.####} = {deductQtyValue:0.00} * {landedRate:0.00} = {deductAmt:0.00}";
                                        }
                                    }

                                    else if (deductType == "GraceBaseLanded")
                                    {
                                        decimal deductAmtGraceBaseLanded = 0;
                                        StringBuilder narr = new StringBuilder();
                                        foreach (var r in ranges)
                                        {
                                            decimal accessPercentage = 0;
                                            // Calculate access percentage for each slab
                                            if (item.RESULT > r.FROM_RESULT && r.TO_RESULT <= item.RESULT)
                                            {
                                                accessPercentage = r.TO_RESULT - r.FROM_RESULT;
                                            }
                                            else if (item.RESULT > r.FROM_RESULT && item.RESULT < r.TO_RESULT)
                                            {
                                                accessPercentage = item.RESULT - r.FROM_RESULT;
                                            }
                                            else
                                            {
                                                continue;
                                            }
                                            if (accessPercentage <= 0)
                                                continue;
                                            decimal deductQtyValue = Math.Round((QcResultWeight * accessPercentage) / 100m, 2);
                                            decimal dedRate = 0;

                                            if (r.DEDUCT_TYPE == "Base")
                                            {
                                                dedRate = Math.Round(landedRate - r.DEDUCT_RATE, 2);
                                            }
                                            else if (r.DEDUCT_TYPE == "Landed Half")
                                            {
                                                dedRate = Math.Round(landedRate / 2m, 2);
                                            }
                                            else
                                            {
                                                dedRate = landedRate;
                                            }
                                            decimal rowAmount = Math.Round(deductQtyValue * dedRate, 2);
                                            // Add all slab amounts
                                            deductAmtGraceBaseLanded += rowAmount;
                                            // Build narration
                                            narr.Append(
                                                $"{QcResultWeight:0.#####}%{accessPercentage:0.##}={deductQtyValue:0.00} * {dedRate:0.00}={Math.Round(rowAmount, 0):0}, ");
                                        }
                                        // Remove last comma
                                        if (narr.Length > 2)
                                        {
                                            narr.Length -= 2;
                                        }
                                        deductAmt = Math.Round(deductAmtGraceBaseLanded, 2);
                                        deductNarr = $"{Parameter}:{narr}";
                                    }
                                    else if (deductType == "ColDiff")
                                    {
                                        decimal accessPercentage = item.RESULT - StdResult;
                                        if (accessPercentage > 0)
                                        {
                                            decimal deductQtyValue = Math.Round((QcResultWeight * accessPercentage) / 100m, 2);
                                            decimal diffRate = 0;
                                            string diffQuery = @"SELECT ISNULL(QCP_DIFF,0) FROM QCDISC_MAST
                                                WHERE COMP_CODE=@COMP_CODE AND V_TYPE='QDIS'
                                                AND ITEM_CODE=@ITEM_CODE AND QCP_CODE=@QCP_CODE";

                                            using (SqlCommand cmd = new SqlCommand(diffQuery, con, tran))
                                            {
                                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                                cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                                                cmd.Parameters.AddWithValue("@QCP_CODE", item.QCP_CODE);

                                                object obj = cmd.ExecuteScalar();
                                                diffRate = obj == DBNull.Value || obj == null ? 0 : Convert.ToDecimal(obj);
                                            }
                                            deductAmt = Math.Round(deductQtyValue * diffRate, 2);
                                            if (deductAmt > 0)
                                            {
                                                deductNarr =
                                                    $"{Parameter}: {QcResultWeight:0.#####} % {accessPercentage:0.#####} = " +
                                                    $"{deductQtyValue:0.00} * {diffRate:0.00} = {deductAmt:0.00}";
                                            }
                                        }
                                    }

                                    else if (deductType == "BasePrice")
                                    {
                                        decimal accessPercentage = item.RESULT - StdResult;
                                        if (accessPercentage > 0)
                                        {
                                            decimal deductQtyValue = Math.Round((QcResultWeight * accessPercentage) / 100m, 2);
                                            decimal basePrice = 0;
                                            string baseQuery = @" SELECT ISNULL(BASE_PRICE,0) FROM QC_MAST1 WHERE CODE=@CODE
                                             AND QCP_CODE=@QCP_CODE AND COMP_CODE=@COMP_CODE";

                                            using (SqlCommand cmd = new SqlCommand(baseQuery, con, tran))
                                            {
                                                cmd.Parameters.AddWithValue("@CODE", item.QC_CODE);
                                                cmd.Parameters.AddWithValue("@QCP_CODE", item.QCP_CODE);
                                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                                                object obj = cmd.ExecuteScalar();
                                                basePrice = obj == DBNull.Value || obj == null ? 0 : Convert.ToDecimal(obj);
                                            }

                                            decimal dedRate = landedRate - basePrice;

                                            deductAmt = Math.Round(deductQtyValue * dedRate, 2);

                                            if (deductAmt > 0)
                                            {
                                                deductNarr =
                                                    $"{Parameter}: {QcResultWeight:0.#####} % {accessPercentage:0.#####} = " +
                                                    $"{deductQtyValue:0.00} * {dedRate:0.00} " +
                                                    $"(LDRate:{landedRate:0.00} - BaseRate:{basePrice:0.00}) = {deductAmt:0.00}";
                                            }
                                        }
                                    }
                                    else if (deductType == "Landed")
                                    {
                                        decimal accessPercentage = item.RESULT - StdResult;

                                        if (accessPercentage > 0)
                                        {
                                            decimal deductQtyValue = Math.Round((QcResultWeight * accessPercentage) / 100m, 2);

                                            deductAmt = Math.Round(deductQtyValue * landedRate, 2);

                                            if (deductAmt > 0)
                                            {
                                                deductNarr =
                                                    $"{Parameter}: {QcResultWeight:0.#####} % {accessPercentage:0.#####} = " +
                                                    $"{deductQtyValue:0.00} * {landedRate:0.00} = {deductAmt:0.00}";
                                            }
                                        }
                                    }


                                }
                                // 6. OUTPUT
                                item.DEDUCT_RATE = deductAmt;
                                item.DEDUCT_TYPE = deductType;
                                item.MATCHED_SRNO = srno;
                                item.DEDUCT_NARR = deductNarr;
                            }

                            tran.Commit();

                            return Json(new
                            {
                                success = true,
                                message = "Calculation completed successfully",
                                data = model.Details
                            });
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return Json(new { success = false, message = ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class QCRangeRow
        {
            public decimal FROM_RESULT { get; set; }
            public decimal TO_RESULT { get; set; }
            public decimal DEDUCT_RATE { get; set; }
            public string DEDUCT_TYPE { get; set; }
            //public string DEDUCT_QTY { get; set; }
            public int SRNO { get; set; }
        }
        public class QCResult
        {
            public string DEDUCT_TYPE { get; set; }
            public string DEDUCT_QTY { get; set; }
            public decimal QTY { get; set; }
        }
        public class QCRange
        {
            public decimal FROM_RESULT { get; set; }
            public decimal TO_RESULT { get; set; }
            public decimal DEDUCT_RATE { get; set; }
        }
        public class Checkcalculatormodl
        {
            //public int? RID { get; set; }
            public string? SessionId { get; set; }
            public string? V_TYPE { get; set; }
            public int? V_NO { get; set; }
            public string? MRN_NO { get; set; }

            public List<QCDetail> Details { get; set; } = new();
        }
        public class QCDetail
        {
            public int QC_CODE { get; set; }
            public int QCP_CODE { get; set; }
            public decimal RESULT { get; set; }
            public string? Parameter { get; set; }
            public int? ItemCode { get; set; }

            public string? DEDUCT_TYPE { get; set; }
            public decimal? DEDUCT_QTY { get; set; }
            public decimal? DEDUCT_RATE { get; set; }
            public string? DEDUCT_NARR { get; set; }

            public int MATCHED_SRNO { get; set; }
        }
        //===================================calculator End Block============================================
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
            string sessionId = HttpContext.Session.GetString("SESSION_ID");
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
                            // SAVE QC2 DETAILS
                            using (SqlCommand detailCommand = new SqlCommand("usp_InsertQC2IncommingQCRM", connection, transaction))
                            {
                                detailCommand.CommandType = CommandType.StoredProcedure;
                                detailCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                detailCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                detailCommand.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                                detailCommand.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
                                detailCommand.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
                                detailCommand.Parameters.AddWithValue("@UserID", globalVar.PubUserId);
                                detailCommand.Parameters.AddWithValue("@Action", "Insert");
                                detailCommand.Parameters.AddWithValue("@SESSION_ID", sessionId);
                                await detailCommand.ExecuteNonQueryAsync();
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


                            using (SqlCommand detailCommand = new SqlCommand("usp_InsertQC2IncommingQCRM", connection, transaction))
                            {
                                detailCommand.CommandType = CommandType.StoredProcedure;
                                detailCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                detailCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                detailCommand.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                                detailCommand.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
                                detailCommand.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
                                detailCommand.Parameters.AddWithValue("@UserID", globalVar.PubUserId);
                                detailCommand.Parameters.AddWithValue("@Action", "Insert");
                                detailCommand.Parameters.AddWithValue("@SESSION_ID", sessionId);
                                await detailCommand.ExecuteNonQueryAsync();
                            }



                            //using (SqlCommand deleteCmd = new SqlCommand(@"
                            //        DELETE FROM QC2 WHERE YEAR_CODE = @YEAR_CODE AND COMP_CODE = @COMP_CODE
                            //        AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO and SESSION_ID = @SESSION_ID
                            //        ", connection, transaction))
                            //{
                            //    deleteCmd.Parameters.Add("@YEAR_CODE", SqlDbType.VarChar).Value = globalVar.PubFYearCode;
                            //    deleteCmd.Parameters.Add("@COMP_CODE", SqlDbType.VarChar).Value = globalVar.PubCompCode;
                            //    deleteCmd.Parameters.Add("@BRANCH_CODE", SqlDbType.VarChar).Value = globalVar.PubBranchCode;
                            //    deleteCmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar).Value = header.docType ?? "";
                            //    deleteCmd.Parameters.Add("@V_NO", SqlDbType.VarChar).Value = header.docNo ?? "";
                            //    deleteCmd.Parameters.Add("@SESSION_ID", SqlDbType.VarChar).Value = string.IsNullOrEmpty(sessionId) ? (object)DBNull.Value : sessionId;

                            //    await deleteCmd.ExecuteNonQueryAsync();
                            //}

                            //// Delete old QC2 records first
                            //using (SqlCommand detailCommand = new SqlCommand("usp_InsertQC2IncommingQCRM", connection, transaction))
                            //{
                            //    detailCommand.CommandType = CommandType.StoredProcedure;
                            //    detailCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            //    detailCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            //    detailCommand.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                            //    detailCommand.Parameters.AddWithValue("@V_TYPE", header.docType ?? "");
                            //    detailCommand.Parameters.AddWithValue("@V_NO", header.docNo ?? "");
                            //    detailCommand.Parameters.AddWithValue("@UserID", globalVar.PubUserId);
                            //    detailCommand.Parameters.AddWithValue("@Action", "Update");
                            //    await detailCommand.ExecuteNonQueryAsync();
                            //}
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
        //=======================================save code===================================

        private string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }
        //=========================================list vala full page ===================================
        public async Task<IActionResult> GetAllDatadetails([FromBody] RequestModel request)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new GateIncommingQCRM();
            string sessionId = HttpContext.Session.GetString("SESSION_ID");
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
                    //command.Parameters.AddWithValue("@UserID", gv.PubUserId);
                    command.Parameters.AddWithValue("@SESSION_ID", sessionId);

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
                                    RID = reader["RID"]?.ToString(),
                                    SESSION_ID = reader["SESSION_ID"]?.ToString()
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
        //=========================================list vala full page ===================================

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
                                Rid = rdr["Rid"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["Rid"]),
                                QCID = rdr["QCID"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["QCID"]),
                                QCPID = rdr["QCPID"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["QCPID"]),

                                Parameter = rdr["Parameter"] == DBNull.Value
                                                ? "" : rdr["Parameter"].ToString(),
                                Unit = rdr["UNIT"] == DBNull.Value
                                                ? "" : rdr["UNIT"].ToString(),
                                Level = rdr["Level"] == DBNull.Value
                                                ? 0 : Convert.ToDecimal(rdr["Level"]),
                                AllowAmt = rdr["ALLOW_AMT"] == DBNull.Value
                                                ? 0 : Convert.ToDecimal(rdr["ALLOW_AMT"]),
                                DeduAmt = rdr["DEDU_AMT1"] == DBNull.Value
                                                ? 0 : Convert.ToDecimal(rdr["DEDU_AMT1"]),
                                DeduNarr = rdr["DEDU_NARR1"] == DBNull.Value
                                                ? "" : rdr["DEDU_NARR1"].ToString(),
                                ItemCode = rdr["ITEM_CODE"] == DBNull.Value
                                                ? 0 : Convert.ToInt32(rdr["ITEM_CODE"]),
                                ItemName = rdr["ITEM_NAME"] == DBNull.Value
                                                ? "" : rdr["ITEM_NAME"].ToString(),
                                Result = rdr["RESULT"] == DBNull.Value
                                                ? 0 : Convert.ToDecimal(rdr["RESULT"])
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


        //=========================================Report Start Block===================================
        [HttpPost]
        public IActionResult PrintInncommingRMReport([FromBody] PrintReportModel model)
        {
            if (model == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Model is null."
                });
            }
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // Optional : Delete old temp data
                            string deleteQuery = @" DELETE FROM QC_TEMP1 WHERE V_NO=@V_NO AND V_TYPE=@V_TYPE";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);

                                cmd.ExecuteNonQuery();
                            }
                            // Insert into QC_TEMP1
                            string insertQuery = @"
                                INSERT INTO QC_TEMP1
                                (
                                    COMP_CODE,
                                    BRANCH_CODE,
                                    YEAR_CODE,
                                    V_NO,
                                    V_TYPE,
                                    QCID,
                                    QCPID,
                                    UNIT,
                                    QL_LEVEL,
                                    PARAMETERS
                                )
                                SELECT DISTINCT
                                    qc.COMP_CODE,
                                    qc.BRANCH_CODE,
                                    qc.YEAR_CODE,
                                    qc.V_NO,
                                    qc.V_TYPE,
                                    qc.QC_CODE,
                                    qc.QCP_CODE,
                                    qc.UNIT,
                                    qc.ACCEPTANCE,
                                    d.NAME
                                FROM QC2 qc
                                INNER JOIN ITEM_MAST i
                                    ON qc.ITEM_CODE = i.CODE
                                LEFT JOIN QCP_MAST d
                                    ON qc.QCP_CODE = d.CODE
                                WHERE qc.V_NO = @V_NO
                                  AND qc.V_TYPE = @V_TYPE
                                  AND qc.YEAR_CODE = @YEAR_CODE
                                  AND qc.COMP_CODE = @COMP_CODE
                                  AND qc.BRANCH_CODE = @BRANCH_CODE";
                            using (SqlCommand cmd = new SqlCommand(insertQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                cmd.ExecuteNonQuery();
                            }
                            // Get RID List
                            List<RidItem> ridList = new List<RidItem>();
                            string ridQuery = @"
                                SELECT DISTINCT qc.RID, it.NAME AS ItemName FROM QC2 qc
                                LEFT JOIN ITEM_MAST it ON qc.ITEM_CODE = it.CODE AND qc.COMP_CODE = it.COMP_CODE
                                WHERE qc.V_NO=@V_NO AND qc.V_TYPE=@V_TYPE AND qc.YEAR_CODE=@YEAR_CODE AND qc.COMP_CODE=@COMP_CODE
                                AND qc.BRANCH_CODE=@BRANCH_CODE ORDER BY qc.RID";

                            using (SqlCommand cmd = new SqlCommand(ridQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                                using (SqlDataReader dr = cmd.ExecuteReader())
                                {
                                    while (dr.Read())
                                    {
                                        ridList.Add(new RidItem
                                        {
                                            RID = Convert.ToInt32(dr["RID"]),
                                            ItemName = dr["ItemName"].ToString()
                                        });
                                    }
                                }
                            }
                            // Update T1,T2,T3....T8
                            int columnNo = 1;
                            foreach (var item in ridList)
                            {
                                if (columnNo > 8)
                                    break;
                                string columnName = $"T{columnNo}";


                                string updateQuery = $@"
                                    UPDATE T
                                    SET T.{columnName}=CAST(Q.RESULT AS DECIMAL(18,2))
                                    FROM QC_TEMP1 T
                                    INNER JOIN QC2 Q
                                    ON T.QCID=Q.QC_CODE
                                    AND T.QCPID=Q.QCP_CODE
                                    WHERE T.V_NO=@V_NO
                                    AND T.V_TYPE=@V_TYPE
                                    AND T.YEAR_CODE=@YEAR_CODE
                                    AND T.COMP_CODE=@COMP_CODE
                                    AND T.BRANCH_CODE=@BRANCH_CODE
                                    AND Q.V_NO=@V_NO
                                    AND Q.V_TYPE=@V_TYPE
                                    AND Q.YEAR_CODE=@YEAR_CODE
                                    AND Q.COMP_CODE=@COMP_CODE
                                    AND Q.BRANCH_CODE=@BRANCH_CODE
                                    AND Q.RID=@RID";

                                using (SqlCommand cmd = new SqlCommand(updateQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                    cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@RID", item.RID);

                                    cmd.ExecuteNonQuery();
                                }
                                columnNo++;
                            }

                            List<DEDUCT_AMTItem> DEDUCT_AMTItemList = new List<DEDUCT_AMTItem>();

                            string DEDUCT_AMTQuery = @"Select QC_CODE, QCP_CODE,RID, DEDU_AMT1, DEDU_NARR1 from QC2 where V_NO=@V_NO AND V_TYPE=@V_TYPE 
                            AND YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

                            using (SqlCommand cmd = new SqlCommand(DEDUCT_AMTQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                                using (SqlDataReader dr = cmd.ExecuteReader())
                                {
                                    while (dr.Read())
                                    {
                                        DEDUCT_AMTItemList.Add(new DEDUCT_AMTItem
                                        {
                                            QC_CODE = Convert.ToInt32(dr["QC_CODE"]),
                                            QCP_CODE = Convert.ToInt32(dr["QCP_CODE"]),
                                            DEDU_AMT1 = Convert.ToInt32(dr["DEDU_AMT1"]),
                                            RID = Convert.ToInt32(dr["RID"]),
                                            DEDU_NARR1 = dr["DEDU_NARR1"].ToString()
                                        });
                                    }
                                }
                            }

                            var grouped = DEDUCT_AMTItemList.GroupBy(x => new { x.QC_CODE, x.QCP_CODE });
                            foreach (var g in grouped)
                            {
                                decimal totalAmt = g.Sum(x => x.DEDU_AMT1);
                                string narration = string.Join(
                                     ", ",
                                     g.OrderBy(x => x.RID)
                                      .Where(x => !string.IsNullOrWhiteSpace(x.DEDU_NARR1))
                                      .Select(x => x.DEDU_NARR1)
                                );

                                string updateDeduct = @" UPDATE QC_TEMP1 SET DEDUCT_AMT=@DEDUCT_AMT, DEDUCT_NAR=@DEDUCT_NAR WHERE QCID=@QC_CODE
                                AND QCPID=@QCP_CODE AND V_NO=@V_NO AND V_TYPE=@V_TYPE AND YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE
                                AND BRANCH_CODE=@BRANCH_CODE";

                                using (SqlCommand cmd = new SqlCommand(updateDeduct, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@DEDUCT_AMT", totalAmt);
                                    cmd.Parameters.AddWithValue("@DEDUCT_NAR", narration);

                                    cmd.Parameters.AddWithValue("@QC_CODE", g.Key.QC_CODE);
                                    cmd.Parameters.AddWithValue("@QCP_CODE", g.Key.QCP_CODE);

                                    cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                    cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                            tran.Commit();
                                var requestData = new
                                {
                                   Reportname = "Rpt_QcRM",         
                                   Database = "ERPDB",               
                                   selectionFormula =
                                   "{QC1.COMP_CODE} = " + gv.PubCompCode +
                                   " AND {QC1.YEAR_CODE} = " + gv.PubFYearCode +
                                   " AND {QC1.BRANCH_CODE} = " + gv.PubBranchCode +
                                   " AND {QC1.V_TYPE} = '" + model.VType + "'" +
                                   " AND {QC1.V_NO} = " + model.VNo,
                                   Parameters = new
                                   {
                                       comp_name = gv.PubCompCode,
                                       comp_add1 = gv.Address1,
                                       comp_add2 = gv.Address2,
                                       RPTNAME = "Quality Report of Raw Material",
                                       T1 = ridList.Count > 0 ? ridList[0].ItemName : "",
                                       T2 = ridList.Count > 1 ? ridList[1].ItemName : "",
                                       T3 = ridList.Count > 2 ? ridList[2].ItemName : "",
                                       T4 = ridList.Count > 3 ? ridList[3].ItemName : "",
                                       T5 = ridList.Count > 4 ? ridList[4].ItemName : "",
                                       T6 = ridList.Count > 5 ? ridList[5].ItemName : "",
                                       T7 = ridList.Count > 6 ? ridList[6].ItemName : "",
                                       T8 = ridList.Count > 7 ? ridList[7].ItemName : ""
                                   }
                                };
                                    return Json(new
                                 {
                                       success = true,
                                        report = requestData
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
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        public class RidItem
        {
            public int RID { get; set; }
            public string ItemName { get; set; }
        }

        public class DEDUCT_AMTItem
        {
            public int DEDU_AMT1 { get; set; }
            public string DEDU_NARR1 { get; set; }
            public int QC_CODE { get; set; }
            public int QCP_CODE { get; set; }
            public int RID { get; set; }
        }

        public class PrintReportModel
        {
            public string? VType { get; set; }
            public int? VNo { get; set; }
        }
        //=========================================Report End Block===================================

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
            public string SESSION_ID { get; set; }
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
