using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class SampleQCController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public SampleQCController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService,
            travelexpensemanagement.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/SampleQC/Index.cshtml");
        }

        public JsonResult GetddlDocType()
        {
            string query = $@" Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('QualityControlSample')";
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


        public JsonResult GetddlCityName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT a.CODE, a.NAME FROM CITY_MAST a WHERE  a.ACTIVE = 1 ORDER BY A.NAME ";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlStateName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT a.CODE, a.NAME FROM STATE_MAST a WHERE   a.ACTIVE = 1 ORDER BY A.NAME ";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlPartyName()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query = $@"SELECT a.code, a.name, b.Add1, b.Add2, b.Add3, b.City_code, c.name AS City, d.name AS State " +
             "FROM SUBGROUP_MAST a " +
             "LEFT JOIN SUBGROUP_ADDRESS b ON a.COMP_CODE = b.COMP_CODE AND a.CODE = b.code AND b.IS_DEFAULT = 1 " +
             "LEFT JOIN CITY_MAST c ON b.City_Code = c.Code " +
             "LEFT JOIN State_MAST d ON c.State_Code = d.Code " +
             "WHERE a.COMP_CODE = " + globalVar.PubCompCode + " AND a.ACTIVE = 1 ORDER BY A.NAME ";

            var moduleList = GetPartyDetailList(query);
            return Json(moduleList);
        }

        public JsonResult GetddlPartyAddress(string PartyCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query = $@" SELECT  B.CODE, CONCAT(B.ADD1, B.ADD2) AS NAME , B.ADD1, B.ADD2, B.ADD3, B.CITY_CODE, C.NAME AS CITY, D.NAME AS STATE " +
             " FROM SUBGROUP_MAST A " +
             " LEFT JOIN SUBGROUP_ADDRESS B ON A.COMP_CODE = B.COMP_CODE AND A.CODE = B.CODE AND B.IS_DEFAULT = 1 " +
             " LEFT JOIN CITY_MAST c ON b.City_Code = c.Code " +
             " LEFT JOIN State_MAST d ON c.State_Code = d.Code " +
             " WHERE a.COMP_CODE = " + globalVar.PubCompCode + "  AND A.CODE = '" + PartyCode + "' AND A.ACTIVE = 1 ORDER BY A.NAME ";

            var moduleList = GetPartyDetailList(query);

            return Json(moduleList);
        }

        public List<object> GetPartyDetailList(string query)
        {
            List<object> dropdownItems = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    dropdownItems.Add(new
                    {
                        Value = reader[0].ToString(),
                        Text = reader[1].ToString(),
                        Add1 = reader[2].ToString(),
                        Add2 = reader[3].ToString(),
                        Add3 = reader[4].ToString(),
                        City = reader[6].ToString(),
                        State = reader[7].ToString()
                    });
                }
            }
            return dropdownItems;
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
        public async Task<IActionResult> GetItemQCPDetails([FromBody] List<ItemRequest> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("No items provided.");

            var gv = _globalVariableService.GetGlobalVariables();
            var results = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (var command = new SqlCommand("usp_getItemQcpList", con))
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
                command.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                await con.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var record = new
                        {
                            itemCode = reader["ItemCode"]?.ToString(),
                            Item_Name = reader["ItemName"]?.ToString(),
                            QC_CODE = reader["QC_CODE"]?.ToString(),
                            qcpid = reader["QCP_CODE"]?.ToString(),
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
        public async Task<IActionResult> SaveAllData([FromBody] SampleQCSaveRequest request)
       {
            //!ModelState.IsValid ||     -- fix it later
            if (request == null || !ModelState.IsValid)
                return BadRequest(ModelState);
            var gv = _globalVariableService.GetGlobalVariables();
            var header = request.Header;
            string gateNoFull = header.DocNo;
            string DoC_ID = header.DocType + header.DocNo;
            string V_type = header.DocType;

            string MRN_TYPE = Regex.Match(DoC_ID, @"^[A-Za-z]+").Value;
            string MRN_NO = Regex.Match(DoC_ID, @"\d+$").Value;

            string yourYearCode = gv.PubFYearCode;
            string yourCompCode = gv.PubCompCode;
            int yourBranchCode = 1;
            string yourVType = V_type;
            string yourVNo = header.DocNo;

            string DoC_IDNew = header.DocType + header.DocNo;
            DateTime yourVDate = DateTime.Parse(header.Date);
            string yourDocId = DoC_ID;
            string yourUserId = gv.PubUserId;
            string firstItemCode = request.Details.FirstOrDefault()?.Items?.FirstOrDefault().Name;

            using (var connection = _dbConnection.GetErpConnection())
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert Header
                        if (header.Action == "INSERT")
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText = "usp_SampleQCRM";
                                command.CommandType = CommandType.StoredProcedure;

                                // Assuming 'command' is a SqlCommand object
                                command.Parameters.AddWithValue("@DOC_ID", yourDocId);
                                command.Parameters.AddWithValue("@V_NO", yourVNo);
                                command.Parameters.AddWithValue("@V_TYPE", yourVType);
                                command.Parameters.AddWithValue("@V_DATE", yourVDate);
                                command.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
                                command.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
                                command.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
                                command.Parameters.AddWithValue("@PARTY_CODE", header.PartyCode);
                                command.Parameters.AddWithValue("@TRANSPORT", header.TransportName);
                                command.Parameters.AddWithValue("@CONTAINER_NO", header.ContainerNo);
                                command.Parameters.AddWithValue("@SAMPLE_RECDBY", header.SampleRecordedBy);
                                command.Parameters.AddWithValue("@TRUCK_NO", header.TruckNo);
                                command.Parameters.AddWithValue("@RECD_QTY", header.RecdQty);
                                command.Parameters.AddWithValue("@MRN_TYPE", MRN_TYPE);
                                command.Parameters.AddWithValue("@MRN_NO", MRN_NO);
                                command.Parameters.AddWithValue("@REMARKS", Truncate(header.Remarks, 200) ?? string.Empty);
                                command.Parameters.AddWithValue("@DEDUCT_AMT", DBNull.Value);
                                command.Parameters.AddWithValue("@DEDUCT_NARR", DBNull.Value);
                                command.Parameters.AddWithValue("@QC_INCHARGE", header.QcIncharge);
                                command.Parameters.AddWithValue("@QC_INCHARGENAME", header.QcInchargeName);
                                command.Parameters.AddWithValue("@CHEMIST", header.Chemist);
                                command.Parameters.AddWithValue("@CHEMISTNAME", header.ChemistName);
                                command.Parameters.AddWithValue("@UUSER", yourUserId);
                                command.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                command.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                command.Parameters.AddWithValue("@LID", Environment.MachineName);
                                command.Parameters.AddWithValue("@Action", "Insert");

                                await command.ExecuteNonQueryAsync();
                            }

                            // 2. Insert Detail Rows and Dynamic Items

                            var groupedDetails = request.Details
                            .SelectMany(detail => detail.Items.Select(kvp => new { detail, itemCode = kvp.Name, itemValue = kvp.Value })).GroupBy(x => x.itemCode);

                            int sno = 1;

                            foreach (var detailGroup in groupedDetails)
                            {
                                int rid = 1;
                                foreach (var kvp in detailGroup)
                                {
                                    string itemCodeStr = kvp.itemCode;
                                    string itemValueStr = kvp.itemValue;
                                    var detail = kvp.detail;

                                    using (var command = connection.CreateCommand())
                                    {
                                        command.Transaction = transaction;
                                        command.CommandText = "usp_InsertQc2SampleQCRM";
                                        command.CommandType = CommandType.StoredProcedure;

                                        command.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
                                        command.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
                                        command.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
                                        command.Parameters.AddWithValue("@V_TYPE", yourVType);
                                        command.Parameters.AddWithValue("@V_NO", yourVNo);
                                        command.Parameters.AddWithValue("@V_DATE", yourVDate);
                                        command.Parameters.AddWithValue("@DOC_ID", yourDocId);
                                        command.Parameters.AddWithValue("@ITEM_CODE", int.TryParse(itemCodeStr, out var itemCode) ? itemCode : 0);
                                        command.Parameters.AddWithValue("@QC_CODE", int.TryParse(detail.QC_CODE, out var qcCode) ? qcCode : 0);
                                        command.Parameters.AddWithValue("@QCP_CODE", int.TryParse(detail.QCP_CODE, out var qcpCode) ? qcpCode : 0);
                                        command.Parameters.AddWithValue("@WT_KG", DBNull.Value);
                                        command.Parameters.AddWithValue("@RID", rid);
                                        command.Parameters.AddWithValue("@SNO", sno);
                                        command.Parameters.AddWithValue("@UNIT", kvp.detail.Unit);
                                        command.Parameters.AddWithValue("@ACCEPTANCE", DBNull.Value);
                                        command.Parameters.AddWithValue("@RESULT", double.TryParse(itemValueStr, out var itemvalue) ? itemvalue : 0);
                                        command.Parameters.AddWithValue("@REMARK", kvp.detail.DeductNarr);
                                        command.Parameters.AddWithValue("@DEDU_AMT", kvp.detail.DeductAmt);
                                        command.Parameters.AddWithValue("@ALLOW_AMT", kvp.detail.AllowAmt);
                                        command.Parameters.AddWithValue("@DEDU_NARR", kvp.detail.DeductNarr);
                                        command.Parameters.AddWithValue("@DEDU_AMT1", DBNull.Value);
                                        command.Parameters.AddWithValue("@DEDU_NARR1", DBNull.Value);
                                        command.Parameters.AddWithValue("@UUSER", yourUserId);
                                        command.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                        command.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                        command.Parameters.AddWithValue("@LID", Environment.MachineName);
                                        command.Parameters.AddWithValue("@Action", "Insert");
                                        await command.ExecuteNonQueryAsync();
                                        rid++;
                                        sno++;
                                    }
                                }
                            }
                            transaction.Commit();
                        }
                        else if (header.Action == "UPDATE")
                        {
                            // 1. Update Header
                            using (var command = connection.CreateCommand())
                            {
                                command.Transaction = transaction;
                                command.CommandText = "usp_SampleQCRM";
                                command.CommandType = CommandType.StoredProcedure;

                                // Assuming 'command' is a SqlCommand object
                                command.Parameters.AddWithValue("@DOC_ID", yourDocId);
                                command.Parameters.AddWithValue("@V_NO", yourVNo);
                                command.Parameters.AddWithValue("@V_TYPE", yourVType);
                                command.Parameters.AddWithValue("@V_DATE", yourVDate);
                                command.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
                                command.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
                                command.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
                                command.Parameters.AddWithValue("@PARTY_CODE", header.PartyCode);
                                command.Parameters.AddWithValue("@TRANSPORT", header.TransportName);
                                command.Parameters.AddWithValue("@CONTAINER_NO", header.ContainerNo);
                                command.Parameters.AddWithValue("@SAMPLE_RECDBY", header.SampleRecordedBy);
                                command.Parameters.AddWithValue("@TRUCK_NO", header.TruckNo);
                                command.Parameters.AddWithValue("@RECD_QTY", header.RecdQty);
                                command.Parameters.AddWithValue("@MRN_TYPE", MRN_TYPE);
                                command.Parameters.AddWithValue("@MRN_NO", MRN_NO);
                                command.Parameters.AddWithValue("@REMARKS", Truncate(header.Remarks, 200) ?? string.Empty);
                                command.Parameters.AddWithValue("@DEDUCT_AMT", DBNull.Value);
                                command.Parameters.AddWithValue("@DEDUCT_NARR", DBNull.Value);
                                command.Parameters.AddWithValue("@QC_INCHARGE", header.QcIncharge);
                                command.Parameters.AddWithValue("@QC_INCHARGENAME", header.QcInchargeName);
                                command.Parameters.AddWithValue("@CHEMIST", header.Chemist);
                                command.Parameters.AddWithValue("@CHEMISTNAME", header.ChemistName);
                                command.Parameters.AddWithValue("@UUSER", yourUserId);
                                command.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                command.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                command.Parameters.AddWithValue("@LID", Environment.MachineName);
                                command.Parameters.AddWithValue("@Action", "Update");

                                await command.ExecuteNonQueryAsync();
                            }

                            // ✅ Optionally clear old details before inserting again
                            using (var deleteCmd = connection.CreateCommand())
                            {
                                deleteCmd.Transaction = transaction;
                                deleteCmd.CommandText = "DELETE FROM QC2 WHERE YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND V_TYPE=@V_TYPE AND V_NO=@V_NO";
                                deleteCmd.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
                                deleteCmd.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
                                deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
                                deleteCmd.Parameters.AddWithValue("@V_TYPE", yourVType);
                                deleteCmd.Parameters.AddWithValue("@V_NO", yourVNo);
                                await deleteCmd.ExecuteNonQueryAsync();
                            }

                            // 2. Re-Insert Detail Rows
                            var groupedDetails = request.Details
                                .SelectMany(detail => detail.Items.Select(kvp => new { detail, itemCode = kvp.Name, itemValue = kvp.Value })).GroupBy(x => x.itemCode);

                            int sno = 1;
                            foreach (var detailGroup in groupedDetails)
                            {
                                int rid = 1;
                                foreach (var kvp in detailGroup)
                                {
                                    string itemCodeStr = kvp.itemCode;
                                    string itemValueStr = kvp.itemValue;
                                    var detail = kvp.detail;

                                    using (var command = connection.CreateCommand())
                                    {
                                        command.Transaction = transaction;
                                        command.CommandText = "usp_InsertQc2SampleQCRM";
                                        command.CommandType = CommandType.StoredProcedure;

                                        command.Parameters.AddWithValue("@YEAR_CODE", yourYearCode);
                                        command.Parameters.AddWithValue("@COMP_CODE", yourCompCode);
                                        command.Parameters.AddWithValue("@BRANCH_CODE", yourBranchCode);
                                        command.Parameters.AddWithValue("@V_TYPE", yourVType);
                                        command.Parameters.AddWithValue("@V_NO", yourVNo);
                                        command.Parameters.AddWithValue("@V_DATE", yourVDate);
                                        command.Parameters.AddWithValue("@DOC_ID", yourDocId);
                                        command.Parameters.AddWithValue("@ITEM_CODE", int.TryParse(itemCodeStr, out var itemCode) ? itemCode : 0);
                                        command.Parameters.AddWithValue("@QC_CODE", int.TryParse(detail.QC_CODE, out var qcCode) ? qcCode : 0);
                                        command.Parameters.AddWithValue("@QCP_CODE", int.TryParse(detail.QCP_CODE, out var qcpCode) ? qcpCode : 0);
                                        command.Parameters.AddWithValue("@WT_KG", DBNull.Value);
                                        command.Parameters.AddWithValue("@RID", rid);
                                        command.Parameters.AddWithValue("@SNO", sno);
                                        command.Parameters.AddWithValue("@UNIT", kvp.detail.Unit);
                                        command.Parameters.AddWithValue("@ACCEPTANCE", DBNull.Value);
                                        command.Parameters.AddWithValue("@RESULT", double.TryParse(itemValueStr, out var itemvalue) ? itemvalue : 0);
                                        command.Parameters.AddWithValue("@REMARK", kvp.detail.DeductNarr);
                                        command.Parameters.AddWithValue("@DEDU_AMT", kvp.detail.DeductAmt);
                                        command.Parameters.AddWithValue("@ALLOW_AMT", kvp.detail.AllowAmt);
                                        command.Parameters.AddWithValue("@DEDU_NARR", kvp.detail.DeductNarr);
                                        command.Parameters.AddWithValue("@DEDU_AMT1", DBNull.Value);
                                        command.Parameters.AddWithValue("@DEDU_NARR1", DBNull.Value);
                                        command.Parameters.AddWithValue("@UUSER", yourUserId);
                                        command.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                        command.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                        command.Parameters.AddWithValue("@LID", Environment.MachineName);
                                        command.Parameters.AddWithValue("@Action", "Insert");
                                        await command.ExecuteNonQueryAsync();
                                        rid++;
                                        sno++;
                                    }
                                }
                            }
                            transaction.Commit();
                            return Ok(new { success = true, message = "Header and detail data updated successfully." }); // ✅ FIX
                        }
                        else
                        {
                            return BadRequest(new { success = false, message = "Invalid ACTION value." });
                        }
                        return Ok(new { success = true, message = "Header and detail data saved successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, new { success = false, message = "Error saving data", error = ex.Message });
                    }
                }
            }
        }

        private string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return input.Length <= maxLength ? input : input.Substring(0, maxLength);
        }

        public class ItemRequest
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
        }

    }
}
