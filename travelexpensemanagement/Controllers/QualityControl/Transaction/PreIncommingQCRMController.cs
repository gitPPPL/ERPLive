using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class PreIncommingQCRMController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PreIncommingQCRMController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/QualityControl/Transaction/PreIncommingQCRM/Index.cshtml");
        }
        public JsonResult GetddlDocType()
        {
            string query = $@" Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('PreQualityControl')";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetddlItemItemCode()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" Select CODE, NAME From SUBGROUP_MAST where COMP_CODE={globalVar.PubCompCode}";
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
        public JsonResult GetddlGateNo(string VNo, string Vtype)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string reftyp = Vtype == "QCPR" ? "INRM" : "INFU";
                string query = $@"SELECT a.V_NO AS value, a.V_TYPE + CAST(a.V_NO AS VARCHAR) + ' | ' +  ISNULL(a.TRUCK_NO, '') + ' | ' +  FORMAT(a.V_DATE, 'dd/MM/yyyy') AS text FROM Gate1 a LEFT JOIN Subgroup_mast b  ON a.PARTY_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE
                WHERE a.V_TYPE = '{reftyp}' AND a.COMP_CODE = {globalVar.PubCompCode} AND a.BRANCH_CODE = 1 AND a.YEAR_CODE = {globalVar.PubFYearCode} 
                ORDER BY a.V_NO DESC";
                //string query = $@"SELECT a.V_TYPE + CAST(a.V_NO AS VARCHAR) AS value, a.V_TYPE + CAST(a.V_NO AS VARCHAR) + ' | ' +  ISNULL(a.TRUCK_NO, '') + ' | ' +  FORMAT(a.V_DATE, 'dd/MM/yyyy') AS text FROM Gate1 a LEFT JOIN Subgroup_mast b  ON a.PARTY_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE
                //WHERE a.V_TYPE = '{reftyp}' AND a.COMP_CODE = {globalVar.PubCompCode} AND a.BRANCH_CODE = 1 AND a.YEAR_CODE = {globalVar.PubFYearCode} 
                //ORDER BY a.V_NO DESC";
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
            string query = $@"SELECT a.CODE, a.NAME FROM ITEM_MAST a LEFT JOIN ITEM_MGROUP b  ON a.MGROUP_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE 
            WHERE a.COMP_CODE = '{globalVar.PubCompCode}' AND b.MGROUP_TYPE = 'Raw' AND a.ACTIVE = 1 ORDER BY a.NAME";
            //string query = $@"SELECT a.CODE, a.NAME FROM ITEM_MAST a LEFT JOIN ITEM_MGROUP b  ON a.MGROUP_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE 
            //WHERE a.COMP_CODE = '{globalVar.PubCompCode}' AND a.ACTIVE = 1 ORDER BY a.NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpPost]
        public async Task<IActionResult> GetGatDetailsList(string StrVNo, string StrV_type)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string strVType = "";

            if (!string.IsNullOrWhiteSpace(StrV_type))
            {
                string firstPart = StrV_type.Split('|')[0].Trim();
                strVType = Regex.Match(firstPart, @"^[A-Za-z]+").Value;
            }
            // Convert StrVNo to INT safely
            if (!int.TryParse(StrVNo, out int vNo))
            {
                return BadRequest("Invalid V_NO");
            }
            var results = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (var command = new SqlCommand("usp_GetGatePreIncommingQCRMDetails", con))
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
                    while (await reader.ReadAsync())
                    {
                        var record = new
                        {
                            V_TYPE = reader["V_TYPE"]?.ToString(),
                            V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                            V_DATE = reader["V_DATE"]?.ToString(),
                            PARTY_CODE = reader["PARTY_CODE"]?.ToString(),
                            PartyName = reader["PartyName"]?.ToString(),
                            BILL_NO = reader["BILL_NO"]?.ToString(),
                            BILL_DATE = reader["BILL_DATE"]?.ToString(),
                            QTY = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : 0,
                            TRUCK_NO = reader["TRUCK_NO"]?.ToString(),
                            ITEM_CODE = reader["ITEM_CODE"]?.ToString(),
                            ITEM_NAME = reader["ITEM_NAME"]?.ToString()
                        };
                        results.Add(record);
                    }
                }
            }
            return Json(results);
        }
        [HttpPost]      
        public async Task<IActionResult> GetItemDetails([FromBody] List<ItemRequest> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("No items provided.");

            var gv = _globalVariableService.GetGlobalVariables();
            var results = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (var command = new SqlCommand("usp_GetGatePreIncommingFillList", con))
            {
                command.CommandType = CommandType.StoredProcedure;

                // Create TVP
                var tvpTable = new DataTable();
                tvpTable.Columns.Add("Code", typeof(int));
                foreach (var item in items)
                    tvpTable.Rows.Add(item.ItemCode);

                var tvpParam = new SqlParameter("@Codes", SqlDbType.Structured)
                {
                    TypeName = "dbo.CodeList",
                    Value = tvpTable
                };
                command.Parameters.Add(tvpParam);

                command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                await con.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var record = new
                        {
                            Item_Code = reader["ItemCode"]?.ToString(),
                            Item_Name = reader["ItemName"]?.ToString(),
                            QC_CODE = reader["QC_CODE"]?.ToString(),
                            QCP_CODE = reader["QCP_CODE"]?.ToString(),
                            Parameter = reader["Parameter"]?.ToString(),
                            Unit = reader["Unit"]?.ToString(),
                            QCP_STD = reader["QCP_STD"]?.ToString(),
                            QTY = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : 0
                        };

                        results.Add(record);
                    }
                }
            }

            return Json(results);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAllData([FromBody] PreIncommingSaveRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request is null" });

            var gv = _globalVariableService.GetGlobalVariables();
            var header = request.Header;
            string[] gateParts = header.GateNo.Split('|');
            string DoC_ID = gateParts[0].Trim();
            if (DoC_ID == "-- Select Party --")
            {
                DoC_ID = "0"; 
            }
            string V_type = header.DocType;
            string MRN_TYPE = Regex.Match(DoC_ID, @"^[A-Za-z]+").Value;
            string MRN_NO = Regex.Match(DoC_ID, @"\d+$").Value;
            string YEAR_CODE = gv.PubFYearCode;
            string COMP_CODE = gv.PubCompCode;
            int BRANCH_CODE = 1;
            string V_TYPE = header.DocType;
            string V_NO = header.DocNo;
            string DOC_ID = string.Empty;
            if (header.ACTION == "UPDATE")
            {
                V_TYPE = V_NO.Substring(0, 4); 
                V_NO = V_NO.Substring(4);
                DOC_ID = header.DocNo;
            }
            if (header.ACTION == "INSERT")
            {
                DOC_ID = header.DocType + header.DocNo;
            }
            DateTime V_DATE = ValidateSqlDateTime(header.Date);
            DateTime BILL_DATE = ValidateSqlDateTime(header.BillDate);
            DateTime MRN_DATE = ValidateSqlDateTime(header.GateDate);

            string USER_ID = gv.PubUserId;
            string firstItemCode = request.Details.FirstOrDefault()?.ItemCode;
            using (var connection = _dbConnection.GetErpConnection())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = transaction;
                            cmd.CommandText = "usp_InsertQC1PreIncommingQCRM";
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@YEAR_CODE", YEAR_CODE);
                            cmd.Parameters.AddWithValue("@COMP_CODE", COMP_CODE);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", BRANCH_CODE);
                            cmd.Parameters.AddWithValue("@V_TYPE", V_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", V_NO);
                            cmd.Parameters.AddWithValue("@V_DATE", V_DATE);
                            cmd.Parameters.AddWithValue("@DOC_ID", DOC_ID);
                            cmd.Parameters.AddWithValue("@MRN_TYPE", MRN_TYPE);
                            cmd.Parameters.AddWithValue("@MRN_NO", MRN_NO);
                            cmd.Parameters.AddWithValue("@QC_INCHARGE", int.TryParse(header.QcIncharge, out var qci) ? qci : 0);
                            cmd.Parameters.AddWithValue("@CHEMIST", int.TryParse(header.Chem, out var chem) ? chem : 0);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", firstItemCode);
                            cmd.Parameters.AddWithValue("@TRANSPORT", header.Transport ?? "");
                            cmd.Parameters.AddWithValue("@TRUCK_NO", header.TruckNo ?? "");
                            cmd.Parameters.AddWithValue("@CONTAINER_NO", header.ContainerNo ?? "");
                            cmd.Parameters.AddWithValue("@INV_QTY", header.InvoiceQty ?? 0);
                            cmd.Parameters.AddWithValue("@RECD_QTY", header.RecordedQty ?? 0);
                            cmd.Parameters.AddWithValue("@PUR_TYPE", header.PurType ?? "");
                            cmd.Parameters.AddWithValue("@SHORT_QTY", header.Shortage ?? 0);
                            cmd.Parameters.AddWithValue("@BILL_NO", header.BillNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_DATE", BILL_DATE);
                            cmd.Parameters.AddWithValue("@WASTE_WGT", header.Wastage ?? 0);
                            cmd.Parameters.AddWithValue("@MRN_DATE", MRN_DATE);
                            cmd.Parameters.AddWithValue("@BALES", header.Bales ?? 0);
                            cmd.Parameters.AddWithValue("@REMARKS", header.Remarks ?? "");
                            cmd.Parameters.AddWithValue("@UUSER", USER_ID);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", "");
                            cmd.Parameters.AddWithValue("@EDATE", "");
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@PARTY_CODE", header.PartyName);
                            cmd.Parameters.AddWithValue("@Action", header.ACTION == "UPDATE" ? "Update" : "Insert");
                            foreach (var p in new[] {
                                "@DEDUCT_AMT","@DEDUCT_NARR","@TENACITY_CODE","@BALE_STATUSCODE",
                                "@CREEL_NO","@LAST_BALENO","@LOT_NO","@SHIFT","@PROD_PLACECODE",
                                "@PROD_LINE","@SAMPLE_RECDBY","@FROM_BALENO","@QC_INCHARGENAME",
                                "@CHEMISTNAME","@NOS_PREQC","@ITEM_NAME","@STATUS","@QC_TIME"
                            })
                            {
                                cmd.Parameters.AddWithValue(p, DBNull.Value);
                            }
                            await cmd.ExecuteNonQueryAsync();
                        }
                        // ✅ STEP 2: If UPDATE, DELETE QC2 old rows
                        if (header.ACTION == "UPDATE")
                        {
                            using (var del = connection.CreateCommand())
                            {
                                del.Transaction = transaction;
                                del.CommandText = "DELETE FROM QC2 WHERE YEAR_CODE=@YEAR AND COMP_CODE=@COMP AND BRANCH_CODE=@BR AND V_TYPE=@VT AND V_NO=@VNO";
                                del.Parameters.AddWithValue("@YEAR", YEAR_CODE);
                                del.Parameters.AddWithValue("@COMP", COMP_CODE);
                                del.Parameters.AddWithValue("@BR", BRANCH_CODE);
                                del.Parameters.AddWithValue("@VT", V_TYPE);
                                del.Parameters.AddWithValue("@VNO", V_NO);

                                await del.ExecuteNonQueryAsync();
                            }
                        }
                        // ✅ STEP 3: Insert QC2 Detail Rows (ALWAYS INSERT)
                        int sno = 1;
                        var groups = request.Details.GroupBy(x => x.ItemCode);
                        
                        foreach (var d in request.Details)
                        {
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = "usp_InsertQC2PreIncommingQCRM";
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@YEAR_CODE", YEAR_CODE);
                                cmd.Parameters.AddWithValue("@COMP_CODE", COMP_CODE);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", BRANCH_CODE);
                                cmd.Parameters.AddWithValue("@V_TYPE", V_TYPE);
                                cmd.Parameters.AddWithValue("@V_NO", V_NO);
                                cmd.Parameters.AddWithValue("@V_DATE", V_DATE);
                                cmd.Parameters.AddWithValue("@DOC_ID", DOC_ID);
                                cmd.Parameters.AddWithValue("@ITEM_CODE", d.ItemCode);
                                cmd.Parameters.AddWithValue("@QC_CODE", int.TryParse(d.QC_CODE, out var qc) ? qc : 0);
                                cmd.Parameters.AddWithValue("@QCP_CODE", int.TryParse(d.QCP_CODE, out var qcp) ? qcp : 0);
                                cmd.Parameters.AddWithValue("@WT_KG", DBNull.Value);
                                cmd.Parameters.AddWithValue("@RID", d.RowIndex);
                                cmd.Parameters.AddWithValue("@SNO", sno++);
                                cmd.Parameters.AddWithValue("@UNIT", string.IsNullOrWhiteSpace(d.Unit) ? (object)DBNull.Value : d.Unit);

                                // ✅ ACCEPTANCE (decimal)
                                if (decimal.TryParse(d.QCP_STD, out var acc))
                                    cmd.Parameters.AddWithValue("@ACCEPTANCE", acc);
                                else
                                    cmd.Parameters.AddWithValue("@ACCEPTANCE", DBNull.Value);

                                // ✅ RESULT (decimal)
                                if (decimal.TryParse(d.ItemValue, out var result))
                                    cmd.Parameters.AddWithValue("@RESULT", result);
                                else
                                    cmd.Parameters.AddWithValue("@RESULT", DBNull.Value);
                                cmd.Parameters.AddWithValue("@MIN_RES", DBNull.Value);
                                cmd.Parameters.AddWithValue("@MAX_RES", DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARK", DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDU_AMT",
                                    decimal.TryParse(d.DeductAmt, out var da) ? da : (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ALLOW_AMT",
                                    decimal.TryParse(d.AllowAmt, out var aa) ? aa : (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDU_NARR",
                                    string.IsNullOrWhiteSpace(d.DeductNarr) ? (object)DBNull.Value : d.DeductNarr);
                                cmd.Parameters.AddWithValue("@DEDU_AMT1", DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDU_NARR1", DBNull.Value);
                                cmd.Parameters.AddWithValue("@UUSER", USER_ID);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", "");
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@Action", "Insert");
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();
                        //return Json(new { success = true, V_TYPE = V_TYPE, V_NO = header.DocNo });
                        return Json(new { success = true, message = "Data saved successfully", V_TYPE = V_TYPE, V_NO = header.DocNo });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, new { success = false, error = ex.Message });
                    }
                }
            }
        }
        public DateTime ValidateSqlDateTime(string dateString)
        {
            DateTime parsedDate;
            if (DateTime.TryParse(dateString, out parsedDate))
            {
                return parsedDate;  
            }
            else
            {
                return DateTime.Now;  
            }
        }
        private string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }
        [HttpPost]
        public async Task<IActionResult> GetItemsName([FromBody] ItemRequest model)
        {
            try
            {
                var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
                string query = "";
                bool isNew = model.Vno == null || model.Vno == 0;

                if (isNew)
                {
                    query = @" SELECT  a.CODE as ItemCode, a.NAME as ItemName, b.CODE AS QC_CODE,b.QCP_CODE, d.NAME AS Parameter,e.NAME AS Unit,b.QCP_STD,  
                    d.QTY,ROW_NUMBER() OVER (PARTITION BY b.CODE, b.QCP_CODE ORDER BY b.SRNO) AS rn  FROM ITEM_MAST a  INNER JOIN QC_MAST1 b ON a.QC_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE  
                    LEFT JOIN QCP_MAST d ON b.QCP_CODE = d.CODE AND a.COMP_CODE = d.COMP_CODE  LEFT JOIN QCPUNIT_MAST e ON b.QCP_UNIT = e.CODE  
                    WHERE a.CODE = @ItemCode AND a.COMP_CODE = @CompCode";
                }
                else
                {
                    query = @" SELECT a.ITEM_CODE, a.Qc_code AS QC_CODE, a.QCP_CODE, d.NAME AS Parameter, a.UNIT, e.QCP_STD, d.QTY, a.ALLOW_AMT, a.DEDU_AMT,
                    a.DEDU_NARR, b.NAME AS ItemName, a.RESULT FROM Qc2 a LEFT JOIN ITEM_MAST b ON a.ITEM_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE LEFT JOIN QCP_MAST d 
                    ON a.qcp_CODE = d.CODE  AND a.COMP_CODE = d.COMP_CODE LEFT JOIN QC_MAST1 e ON a.QC_CODE = e.CODE AND a.COMP_CODE = e.COMP_CODE AND e.QCP_CODE = a.QCP_CODE
                    WHERE a.ITEM_CODE = @ItemCode and a.YEAR_CODE = @YearCode AND a.COMP_CODE = @CompCode AND a.V_TYPE = @Vtype AND a.V_NO = @Vno;";
                }
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ItemCode", model.ItemCode ?? (object)DBNull.Value),
                    new SqlParameter("@CompCode", compCode),
                    new SqlParameter("@Vno", model.Vno ?? (object)DBNull.Value),
                    new SqlParameter("@Vtype", model.Vtype ?? (object)DBNull.Value),
                    new SqlParameter("@YearCode", 8)
                };

                var dt = await _dbHelper.ExecuteQueryAsync(query, parameters);

                List<object> list;

                if (isNew)
                {
                    list = dt.AsEnumerable().Select(row => new
                    {
                        ItemCode = row["ItemCode"].ToString(),
                        ItemName = row["ItemName"].ToString(),
                        QC_CODE = row["QC_CODE"].ToString(),
                        QCP_CODE = row["QCP_CODE"].ToString(),
                        Parameter = row["Parameter"].ToString(),
                        Unit = row["Unit"].ToString(),
                        QCP_STD = row["QCP_STD"].ToString(),
                        QTY = row["QTY"].ToString(),
                        rn = row["rn"].ToString()
                    }).ToList<object>();
                }
                else
                {
                    list = dt.AsEnumerable().Select(row => new
                    {
                        ItemCode = row["ITEM_CODE"].ToString(),
                        ItemName = row["ItemName"].ToString(),
                        QC_CODE = row["QC_CODE"].ToString(),
                        QCP_CODE = row["QCP_CODE"].ToString(),
                        Parameter = row["Parameter"].ToString(),
                        QCP_STD = row["QCP_STD"].ToString(),
                        QTY = row["QTY"].ToString(),
                        Unit = row["UNIT"].ToString(),
                        Allow = row["ALLOW_AMT"].ToString(),
                        Deduction = row["DEDU_AMT"].ToString(),
                        DeductionNarr = row["DEDU_NARR"].ToString(),
                        Result = row["RESULT"].ToString()
                    }).ToList<object>();
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetQcResultList([FromBody] QcResultRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Request is null" });
            try
            {
                var V_TYPE = request.vType;
                var V_NO = request.vNo;
                try
                {
                    var g = _globalVariableService.GetGlobalVariables();

                    string query = @"SELECT a.QC_CODE, a.QCP_CODE, d.NAME AS Parameter, a.UNIT, a.ALLOW_AMT, a.DEDU_AMT, a.DEDU_NARR, b.NAME AS ItemName, 
                    a.RESULT FROM QC2 a LEFT JOIN ITEM_MAST b ON a.ITEM_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE 
                    LEFT JOIN QCP_MAST d ON a.QCP_CODE = d.CODE AND a.COMP_CODE = d.COMP_CODE WHERE a.YEAR_CODE = @YearCode AND a.COMP_CODE = @CompCode 
                    AND a.V_TYPE = @VType AND a.V_NO = @VNo";

                    var parameters = new List<SqlParameter>
                    {
                        new SqlParameter("@YearCode", g.PubFYearCode), 
                        new SqlParameter("@CompCode", g.PubCompCode),
                        new SqlParameter("@VType", V_TYPE),
                        new SqlParameter("@VNo", V_NO)
                    };

                    var dt = await _dbHelper.ExecuteQueryAsync(query, parameters);
                    var list = dt.AsEnumerable().Select(row => new
                    {
                        QC_CODE = row["QC_CODE"].ToString(),
                        QCP_CODE = row["QCP_CODE"].ToString(),
                        Parameter = row["Parameter"].ToString(),
                        Unit = row["UNIT"].ToString(),
                        AllowAmt = row["ALLOW_AMT"].ToString(),
                        DeductAmt = row["DEDU_AMT"].ToString(),
                        DeductNarr = row["DEDU_NARR"].ToString(),
                        ItemName = row["ItemName"].ToString(),
                        ItemValue = row["RESULT"].ToString() // Result column mapped
                    }).ToList();

                    return Json(new { success = true, data = list });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAllDatadetails([FromBody] RequestModel request)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new GatePreIncommingQCRM();

            try
            {
                if (!int.TryParse(request.vNo, out int vNo))
                    return BadRequest("Invalid gate number format.");

                string strVType = request.vType?.Length >= 4 ? request.vType.Substring(0, 4) : request.vType;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (var command = new SqlCommand("usp_GetGatePreIncommingQCRMList", con))
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
                        // ✅ Result Set 1: Header
                        while (await reader.ReadAsync())
                        {
                            var header = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                                header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            response.Header.Add(header);
                        }

                        // ✅ Result Set 2: Items
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
                    }
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        public class RequestModel
        {
            public string vNo { get; set; }
            public string vType { get; set; }
        }

    }
}
