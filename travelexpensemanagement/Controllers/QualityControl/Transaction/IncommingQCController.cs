using Azure;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
    public class IncommingQCController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public IncommingQCController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/QualityControl/Transaction/IncommingQC/Index.cshtml");
        }
        public JsonResult GetddlDocType()
        {
            string query = $@"select  code ,name from DOCTYPE_MAST  WHERE CODE='QCST'";
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
        public JsonResult GetddlMRNNo(string VNo, string Vtype)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = $@" SELECT V_NO AS value, V_TYPE + CAST(V_NO AS VARCHAR) AS text FROM PURCHASE1 WHERE V_TYPE = 'SRPU' 
                AND COMP_CODE = {globalVar.PubCompCode} AND BRANCH_CODE = 1 AND YEAR_CODE = {globalVar.PubFYearCode} ORDER BY V_NO DESC";
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
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetPartyName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"select CODE,NAME from SUBGROUP_MAST WHERE COMP_CODE='"+ globalVar.PubCompCode +"'  order by NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetItemMaster()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"select Code, NAME from ITEM_MAST where COMP_CODE='" + globalVar.PubCompCode + "' and ACTIVE=1 and name<>''";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetParticulars()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"select Code, NAME from QCP_MAST where COMP_CODE='" + globalVar.PubCompCode + "' and ACTIVE=1 and name<>''";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetUnits()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"select Code, NAME from QCPUNIT_MAST where ACTIVE=1 and name<>''";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpPost]
        public IActionResult SendDropdownData(string DocType, string MRNText, string VNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string MRNType = new string(MRNText.TakeWhile(char.IsLetter).ToArray());
            string MRNNo = new string(MRNText.SkipWhile(char.IsLetter).ToArray());

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("sp_Fullfilldata", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DocType", DocType);
                        cmd.Parameters.AddWithValue("@MRNType", MRNType);
                        cmd.Parameters.AddWithValue("@MRNNo", Convert.ToInt64(MRNNo));
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            // Step 1: QC Status
                            bool isQCComplete = false;
                            //if (reader.Read())
                            //{
                            //    isQCComplete = Convert.ToBoolean(reader["IsQCComplete"]);
                            //}
                            // Step 2: Header Data
                            reader.NextResult();
                            var headerData = new List<object>();
                            while (reader.Read())
                            {
                                headerData.Add(new
                                {
                                    V_TYPE = reader["V_TYPE"],
                                    V_NO = reader["V_NO"],
                                    V_DATE = reader["V_DATE"],
                                    PARTY_CODE = reader["PARTY_CODE"],
                                    NAME = reader["NAME"],
                                    TRANSPORT_NAME = reader["TRANSPORT_NAME"],
                                    BILL_NO = reader["BILL_NO"],
                                    BILL_DATE = reader["BILL_DATE"],
                                    BILL_QTY = reader["BILL_QTY"],
                                    RECD_QTY = reader["RECD_QTY"],
                                    TRUCK_NO = reader["TRUCK_NO"],
                                    DOCUMENT_NAME = reader["DOCUMENT_NAME"]
                                });
                            }
                            // Step 3: Item Data
                            reader.NextResult();
                            var itemData = new List<object>();
                            while (reader.Read())
                            {
                                itemData.Add(new
                                {
                                    ITEM_NAME = reader["ITEM_CODE"],
                                    PARTICULAR_NAME = reader["PARTICULAR_CODE"],
                                    UNIT_NAME = reader["UNIT_CODE"],
                                    STD_LEVEL = reader["STD_LEVEL"],
                                    deduction_amt = reader["deduction_amt"],
                                    allow_amt = reader["allow_amt"],
                                    deduction_narration = reader["deduction_narration"],
                                    ITEM_CODE = reader["ITEM_CODE"],
                                    QCP_CODE = reader["QCP_CODE"],
                                    QC_CODE = reader["QC_CODE"],
                                    NOS = reader["NOS"]
                                });
                            }
                            return Json(new
                            {
                                success = isQCComplete,
                                message = isQCComplete ? "QC is complete. You can proceed." : "QC is not done yet for this MRN.",
                                headerData,
                                itemData
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        public IActionResult SaveQCData([FromBody] IncommingQC model)
        {
            if (model == null || model.Header == null || model.Items == null || !model.Items.Any())
                return BadRequest(new { success = false, message = "Invalid data" });

            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var header = model.Header;
                var items = model.Items;

                string MRNType = string.IsNullOrEmpty(header.MRNNo) ? "" : new string(header.MRNNo.TakeWhile(char.IsLetter).ToArray());
                string MRNNo = string.IsNullOrEmpty(header.MRNNo) ? "" : new string(header.MRNNo.SkipWhile(char.IsLetter).ToArray());
                string DocNo = "QCST" + header.DocNo;

                using (var conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        if (header.ACTION == "INSERT")
                        {
                            // ================= Insert Header =================
                            string qryHeader = @"INSERT INTO QC1 
                        (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DOC_ID, MRN_NO, MRN_TYPE, MRN_DATE, BALES, PARTY_CODE,
                         QC_INCHARGE, QC_INCHARGENAME, CHEMIST, CHEMISTNAME, BILL_NO, BILL_DATE, TRANSPORT, TRUCK_NO, INV_QTY, RECD_QTY,
                         SHORT_QTY, REMARKS, DEDUCT_AMT, DEDUCT_NARR, PUR_TYPE, WASTE_WGT, UUSER, UDATE, AED, WSID, LIP, LID)
                        VALUES
                        (@YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO, @V_DATE, @DOC_ID, @MRN_NO, @MRN_TYPE, @MRN_DATE, @BALES, @PARTY_CODE,
                         @QC_INCHARGE, @QC_INCHARGENAME, @CHEMIST, @CHEMISTNAME, @BILL_NO, @BILL_DATE, @TRANSPORT, @TRUCK_NO, @INV_QTY, @RECD_QTY,
                         @SHORT_QTY, @REMARKS, @DEDUCT_AMT, @DEDUCT_NARR, @PUR_TYPE, @WASTE_WGT, @UUSER, GETDATE(), 'A', @WSID, @LIP, @LID)";
                            using (var cmd = new SqlCommand(qryHeader, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@V_TYPE", "QCST");
                                cmd.Parameters.AddWithValue("@V_NO", header.DocNo);
                                cmd.Parameters.AddWithValue("@V_DATE", header.DocDate);
                                cmd.Parameters.AddWithValue("@DOC_ID", DocNo);
                                cmd.Parameters.AddWithValue("@MRN_NO", MRNNo);
                                cmd.Parameters.AddWithValue("@MRN_TYPE", MRNType);
                                cmd.Parameters.AddWithValue("@MRN_DATE", header.MRNDate);
                                cmd.Parameters.AddWithValue("@BALES", header.Bales);
                                cmd.Parameters.AddWithValue("@PARTY_CODE", header.PartyCode);
                                cmd.Parameters.AddWithValue("@QC_INCHARGE", header.QCIncharge);
                                cmd.Parameters.AddWithValue("@QC_INCHARGENAME", DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHEMIST", header.Chemist);
                                cmd.Parameters.AddWithValue("@CHEMISTNAME", DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_NO", header.BillNo ?? "");
                                cmd.Parameters.AddWithValue("@BILL_DATE", header.BillDate);
                                cmd.Parameters.AddWithValue("@TRANSPORT", header.Transport ?? "");
                                cmd.Parameters.AddWithValue("@TRUCK_NO", header.TruckNo ?? "");
                                cmd.Parameters.AddWithValue("@INV_QTY", header.InvoiceQty);
                                cmd.Parameters.AddWithValue("@RECD_QTY", header.RecordedQty);
                                cmd.Parameters.AddWithValue("@SHORT_QTY", header.Shortage);
                                cmd.Parameters.AddWithValue("@REMARKS", header.Remarks);
                                cmd.Parameters.AddWithValue("@DEDUCT_AMT", header.DeductionAmount);
                                cmd.Parameters.AddWithValue("@DEDUCT_NARR", header.DeductionNarration);
                                cmd.Parameters.AddWithValue("@PUR_TYPE", header.PurchaseType ?? "");
                                cmd.Parameters.AddWithValue("@WASTE_WGT", header.Wastage);
                                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                cmd.ExecuteNonQuery();
                            }
                        }
                        else if (header.ACTION == "UPDATE")
                        {
                            // ================= Update Header =================
                            string qryHeaderUpdate = @"UPDATE QC1 SET 
                        V_TYPE=@V_TYPE, V_NO=@V_NO, V_DATE=@V_DATE, MRN_NO=@MRN_NO, MRN_TYPE=@MRN_TYPE, MRN_DATE=@MRN_DATE, BALES=@BALES, PARTY_CODE=@PARTY_CODE,
                        QC_INCHARGE=@QC_INCHARGE, CHEMIST=@CHEMIST, BILL_NO=@BILL_NO, BILL_DATE=@BILL_DATE, TRANSPORT=@TRANSPORT,
                        TRUCK_NO=@TRUCK_NO, INV_QTY=@INV_QTY, RECD_QTY=@RECD_QTY, SHORT_QTY=@SHORT_QTY, REMARKS=@REMARKS,
                        DEDUCT_AMT=@DEDUCT_AMT, DEDUCT_NARR=@DEDUCT_NARR, PUR_TYPE=@PUR_TYPE, WASTE_WGT=@WASTE_WGT, UUSER=@UUSER, UDATE=GETDATE()
                        WHERE DOC_ID=@DOC_ID";

                            using (var cmd = new SqlCommand(qryHeaderUpdate, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@DOC_ID", DocNo);
                                cmd.Parameters.AddWithValue("@V_TYPE", "QCST");
                                cmd.Parameters.AddWithValue("@V_NO", header.DocNo);
                                cmd.Parameters.AddWithValue("@V_DATE", header.DocDate);
                                cmd.Parameters.AddWithValue("@MRN_NO", MRNNo);
                                cmd.Parameters.AddWithValue("@MRN_TYPE", MRNType);
                                cmd.Parameters.AddWithValue("@MRN_DATE", header.MRNDate);
                                cmd.Parameters.AddWithValue("@BALES", header.Bales);
                                cmd.Parameters.AddWithValue("@PARTY_CODE", header.PartyCode);
                                cmd.Parameters.AddWithValue("@QC_INCHARGE", header.QCIncharge);
                                cmd.Parameters.AddWithValue("@CHEMIST", header.Chemist);
                                cmd.Parameters.AddWithValue("@BILL_NO", header.BillNo ?? "");
                                cmd.Parameters.AddWithValue("@BILL_DATE", header.BillDate);
                                cmd.Parameters.AddWithValue("@TRANSPORT", header.Transport ?? "");
                                cmd.Parameters.AddWithValue("@TRUCK_NO", header.TruckNo ?? "");
                                cmd.Parameters.AddWithValue("@INV_QTY", header.InvoiceQty);
                                cmd.Parameters.AddWithValue("@RECD_QTY", header.RecordedQty);
                                cmd.Parameters.AddWithValue("@SHORT_QTY", header.Shortage);
                                cmd.Parameters.AddWithValue("@REMARKS", header.Remarks);
                                cmd.Parameters.AddWithValue("@DEDUCT_AMT", header.DeductionAmount);
                                cmd.Parameters.AddWithValue("@DEDUCT_NARR", header.DeductionNarration);
                                cmd.Parameters.AddWithValue("@PUR_TYPE", header.PurchaseType ?? "");
                                cmd.Parameters.AddWithValue("@WASTE_WGT", header.Wastage);
                                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);

                                cmd.ExecuteNonQuery();
                            }
                            // ================= Delete old QC2 items based on V_TYPE & V_NO =================
                            string qryDeleteItems = "DELETE FROM QC2 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO";
                            using (var cmdDel = new SqlCommand(qryDeleteItems, conn, transaction))
                            {
                                cmdDel.Parameters.AddWithValue("@V_TYPE", "QCST");
                                cmdDel.Parameters.AddWithValue("@V_NO", header.DocNo);
                                cmdDel.ExecuteNonQuery();
                            }
                        }
                        // ================= Insert Items (both insert & update) =================
                        string qryItem = @"INSERT INTO QC2
                    (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DOC_ID, ITEM_CODE, QC_CODE, QCP_CODE, UNIT, ACCEPTANCE, RESULT,
                     REMARK, DEDU_AMT, ALLOW_AMT, DEDU_NARR, EUSER, EDATE, AED, WSID, LIP, LID, SNO)
                    VALUES
                    (@YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO, @V_DATE, @DOC_ID, @ITEM_CODE, @QC_CODE, @QCP_CODE, @UNIT,
                     @ACCEPTANCE, @RESULT, @REMARK, @DEDU_AMT, @ALLOW_AMT, @DEDU_NARR, @EUSER, GETDATE(), 'E', @WSID, @LIP, @LID, @SNO)";

                        int sno = 1;
                        foreach (var item in items)
                        {
                            using (var cmd = new SqlCommand(qryItem, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@V_TYPE", "QCST");
                                cmd.Parameters.AddWithValue("@V_NO", header.DocNo);
                                cmd.Parameters.AddWithValue("@V_DATE", header.DocDate);
                                cmd.Parameters.AddWithValue("@DOC_ID", DocNo);
                                cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                                cmd.Parameters.AddWithValue("@QC_CODE", 0);
                                cmd.Parameters.AddWithValue("@QCP_CODE", item.ParticularName);
                                cmd.Parameters.AddWithValue("@UNIT", item.UnitName ?? "");
                                cmd.Parameters.AddWithValue("@ACCEPTANCE", 0);
                                cmd.Parameters.AddWithValue("@RESULT", decimal.TryParse(item.Result, out decimal res) ? res : 0);
                                cmd.Parameters.AddWithValue("@REMARK", item.Remarks ?? "");
                                cmd.Parameters.AddWithValue("@DEDU_AMT", item.DeductionAmt);
                                cmd.Parameters.AddWithValue("@ALLOW_AMT", item.AllowAmt);
                                cmd.Parameters.AddWithValue("@DEDU_NARR", item.DeductionNarration ?? "");
                                cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@SNO", sno++);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                }
                return Json(new { success = true, message = "QC Data saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetAllDatadetails([FromBody] RequestModel request)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var response = new GatePreIncommingQCRM
                {
                    Header = new List<Dictionary<string, object>>(),
                    Items = new List<Dictionary<string, object>>()
                };

                if (string.IsNullOrEmpty(request.vType))
                    return BadRequest("vType is required.");
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand command = new SqlCommand("usp_GetIncommingQCList", con))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@V_TYPE", request.vType);
                    command.Parameters.AddWithValue("@V_NO", request.vNo);
                    command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    command.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                    await con.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // -------- Header Result --------
                        while (await reader.ReadAsync())
                        {
                            var header = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            response.Header.Add(header);
                        }

                        // -------- Items Result --------
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
                    }
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
        public class RequestModel
        {
            public string vNo { get; set; }
            public string vType { get; set; }
        }

    }
}
