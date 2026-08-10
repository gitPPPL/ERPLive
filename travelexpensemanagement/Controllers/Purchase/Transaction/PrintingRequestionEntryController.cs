using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using static travelexpensemanagement.Controllers.QualityControl.Transaction.IncommingQCRMController;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PrintingRequestionEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        public PrintingRequestionEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService; ;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PrintingRequestionEntry/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetDocNo()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                string vType = "PRPI";
                string query = @"SELECT ISNULL(MAX(V_NO), 0) + 1 AS NextVNo FROM PRINT_REQUEST1 WHERE V_TYPE = @VType AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode
                AND YEAR_CODE = @YearCode";
                int nextVNo = 1;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@VType", SqlDbType.NVarChar, 10).Value = vType;
                    cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = gv.PubCompCode;
                    cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = gv.PubBranchCode;
                    cmd.Parameters.Add("@YearCode", SqlDbType.Int).Value = gv.PubFYearCode;
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        nextVNo = Convert.ToInt32(result);
                    }
                }
                return Json(new
                {
                    success = true,
                    nextVNo = nextVNo
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
        [HttpGet]
        public JsonResult GetPlace(string search = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT CODE,NAME FROM PLACE_MAST WHERE COMP_CODE={gv.PubCompCode}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                query += $" AND NAME LIKE '{search}%'";
            }
            query += " ORDER BY NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetDocType()
        {
            string query = @"SELECT Code, Name FROM DOCTYPE_MAST WHERE DOCTYPE='PrintingRequisition'";
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }
        [HttpGet]
        public JsonResult GetDepartment(string search = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT DISTINCT b.CODE,b.NAME  FROM USER_DEPT a LEFT JOIN ITEMDEPT_MAST b
            ON a.DEPT_CODE=b.CODE AND a.COMP_CODE=b.COMP_CODE WHERE a.USER_CODE=1  AND a.COMP_CODE={gv.PubCompCode}
            AND b.TRAN_TYPE='Store'";
            if (!string.IsNullOrWhiteSpace(search))
            {
                query += $" AND b.NAME LIKE '{search}%'";
            }
            query += " ORDER BY b.NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }

        [HttpGet]
        public JsonResult GetRequestBy(string search = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT b.CODE,b.FULL_NAME AS NAME FROM SUBUSER_MAST a  LEFT JOIN USER_MAST b
            ON b.CODE=a.USER_CODE WHERE a.COMP_CODE={gv.PubCompCode}";
            if (!string.IsNullOrWhiteSpace(search))
            {
                query += $" AND b.FULL_NAME LIKE '{search}%'";
            }
            query += "  AND ISNULL(b.CODE, '') <> '' AND ISNULL(b.FULL_NAME, '') <> '' ORDER BY b.FULL_NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetStatus()
        {
            string query = @"SELECT CODE,NAME FROM DOCSTATUS_MAST WHERE V_TYPE='Document'   ORDER BY CODE";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetItem()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT Code, Name FROM ITEM_MAST WHERE COMP_CODE = {gv.PubCompCode} AND ACTIVE = 1 ORDER BY Name";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpGet]
        public JsonResult GetMake(int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT a.MAKE_CODE AS Code,  b.NAME AS Name FROM ITEM_MAKE a LEFT JOIN ITEMMAKE_MAST b
            ON a.MAKE_CODE = b.CODE AND b.COMP_CODE = {gv.PubCompCode} WHERE a.ITEM_CODE = {itemCode}  AND a.COMP_CODE = {gv.PubCompCode} ORDER BY b.NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        public JsonResult GetPriority()
        {
            string query = @"SELECT CODE AS Value, NAME AS Text  FROM DOCSTATUS_MAST
            WHERE V_TYPE='Preority' ORDER BY CODE";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        public JsonResult GetWorkType()
        {
            string query = @"SELECT CODE AS Value, NAME AS Text FROM DOCSTATUS_MAST
             WHERE V_TYPE='WorkType' ORDER BY NAME";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpPost]
        public async Task<IActionResult> SaveAllData([FromForm] PrintingRequestModel model)
        {
            if (model == null || model.Header == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid request data."
                });
            }
            string action = model.ACTION?.Trim().ToUpper();
            if (action != "INSERT" && action != "UPDATE")
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid ACTION. Only INSERT or UPDATE is allowed."
                });
            }
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        var globalVar = _globalVariableService.GetGlobalVariables();
                        // COMMON VALUES
                        string vType = "PRPI";
                        string vNo = model.Header.DocNo?.Trim();
                        if (string.IsNullOrWhiteSpace(vNo))
                        {
                            throw new Exception("Document number is required.");
                        }

                        int vNoInt = Convert.ToInt32(vNo);
                        string compCode = globalVar.PubCompCode;
                        int branchCode = globalVar.PubBranchCode;
                        string yearCode = globalVar.PubFYearCode;
                        string docId = vType + vNo;
                        // INSERT
                        if (action == "INSERT")
                        {
                            // HEADER INSERT

                            string qryHeader = @"
                            INSERT INTO PRINT_REQUEST1 (COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, V_DATE, DOC_ID,
                            PLACE_CODE, OWNER_CODE, OWNER_NAME, DEPT_CODE, STATUS, TARGET_DATE, REASON, VALID_DATE, REMARKS, ACTIVE,
                            UUSER, UDATE, AED, WSID, LIP, LID)
                            VALUES (@COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @V_DATE, @DOC_ID, @PLACE_CODE,
                            @OWNER_CODE, @OWNER_NAME, @DEPT_CODE, @STATUS, @TARGET_DATE, @REASON, @VALID_DATE, @REMARKS, 1, @UUSER, @UDATE,
                            'A', @WSID, @LIP, @LID)";

                            using (SqlCommand cmdHeader =  new SqlCommand(qryHeader, con, tran))
                            {
                                cmdHeader.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = compCode;
                                cmdHeader.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = branchCode;
                                cmdHeader.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = yearCode;
                                cmdHeader.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 10).Value = vType;
                                cmdHeader.Parameters.Add("@V_NO", SqlDbType.Int).Value = vNoInt;
                                cmdHeader.Parameters.Add("@V_DATE", SqlDbType.DateTime).Value = model.Header.DocDate;
                                cmdHeader.Parameters.Add("@DOC_ID", SqlDbType.NVarChar, 50).Value = docId;
                                cmdHeader.Parameters.Add("@PLACE_CODE", SqlDbType.Int).Value = Convert.ToInt32(model.Header.Place);
                                cmdHeader.Parameters.Add("@OWNER_CODE", SqlDbType.Int).Value = Convert.ToInt32(model.Header.RequestBy);
                                cmdHeader.Parameters.Add("@OWNER_NAME", SqlDbType.NVarChar, 200).Value =  model.Header.RequestByName ?? "";
                                cmdHeader.Parameters.Add("@DEPT_CODE", SqlDbType.Int).Value = Convert.ToInt32(model.Header.Department);
                                cmdHeader.Parameters.Add("@STATUS", SqlDbType.Int).Value = Convert.ToInt32(model.Header.Status);
                                cmdHeader.Parameters.Add("@TARGET_DATE", SqlDbType.DateTime).Value = model.Header.RequiredDate.HasValue
                                        ? (object)model.Header.RequiredDate.Value : DBNull.Value;
                                cmdHeader.Parameters.Add("@REASON", SqlDbType.NVarChar, 500).Value =  model.Header.Reason ?? "";
                                cmdHeader.Parameters.Add("@VALID_DATE", SqlDbType.DateTime).Value = DateTime.Now;
                                cmdHeader.Parameters.Add("@REMARKS", SqlDbType.NVarChar, 500).Value =  model.Header.Remarks ?? "";
                                cmdHeader.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                                cmdHeader.Parameters.Add("@UDATE", SqlDbType.DateTime).Value = DateTime.Now;
                                cmdHeader.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = globalVar.PubWorkStationID ?? "";
                                cmdHeader.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = globalVar.PubLocalId ?? "";
                                cmdHeader.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value = Environment.MachineName;
                                await cmdHeader.ExecuteNonQueryAsync();
                            }
                        }
                        // UPDATE
                        else if (action == "UPDATE")
                        {
                            // HEADER UPDATE
                            string qryHeaderUpdate = @"UPDATE PRINT_REQUEST1 SET V_DATE = @V_DATE, DOC_ID = @DOC_ID,
                            PLACE_CODE = @PLACE_CODE, OWNER_CODE = @OWNER_CODE, OWNER_NAME = @OWNER_NAME, DEPT_CODE = @DEPT_CODE, STATUS = @STATUS,
                            TARGET_DATE = @TARGET_DATE, REASON = @REASON, VALID_DATE = @VALID_DATE, REMARKS = @REMARKS, UUSER = @UUSER,
                            UDATE = @UDATE, AED = 'E', WSID = @WSID, LIP = @LIP,
                            LID = @LID WHERE COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE
                            AND V_TYPE = @V_TYPE AND V_NO = @V_NO";
                            using (SqlCommand cmdHeader = new SqlCommand(qryHeaderUpdate, con, tran))
                            {
                                cmdHeader.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = compCode;

                                cmdHeader.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = branchCode;

                                cmdHeader.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = yearCode;

                                cmdHeader.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 10).Value = vType;

                                cmdHeader.Parameters.Add("@V_NO", SqlDbType.Int).Value = vNoInt;

                                cmdHeader.Parameters.Add("@V_DATE", SqlDbType.DateTime).Value = model.Header.DocDate;

                                cmdHeader.Parameters.Add("@DOC_ID", SqlDbType.NVarChar, 50).Value = docId;

                                cmdHeader.Parameters.Add("@PLACE_CODE", SqlDbType.Int).Value = Convert.ToInt32(model.Header.Place);

                                cmdHeader.Parameters.Add("@OWNER_CODE", SqlDbType.Int).Value = Convert.ToInt32(model.Header.RequestBy);

                                cmdHeader.Parameters.Add("@OWNER_NAME", SqlDbType.NVarChar, 200).Value = model.Header.RequestByName ?? "";

                                cmdHeader.Parameters.Add("@DEPT_CODE", SqlDbType.Int).Value = Convert.ToInt32(model.Header.Department);

                                cmdHeader.Parameters.Add("@STATUS", SqlDbType.Int).Value = Convert.ToInt32(model.Header.Status);

                                cmdHeader.Parameters.Add("@TARGET_DATE", SqlDbType.DateTime).Value = model.Header.RequiredDate.HasValue
                                        ? (object)model.Header.RequiredDate.Value : DBNull.Value;

                                cmdHeader.Parameters.Add("@REASON", SqlDbType.NVarChar, 500).Value = model.Header.Reason ?? "";

                                cmdHeader.Parameters.Add("@VALID_DATE", SqlDbType.DateTime).Value = DateTime.Now;

                                cmdHeader.Parameters.Add("@REMARKS", SqlDbType.NVarChar, 500).Value = model.Header.Remarks ?? "";

                                cmdHeader.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;

                                cmdHeader.Parameters.Add("@UDATE", SqlDbType.DateTime).Value = DateTime.Now;

                                cmdHeader.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = globalVar.PubWorkStationID ?? "";

                                cmdHeader.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = globalVar.PubLocalId ?? "";

                                cmdHeader.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value = Environment.MachineName;

                                int headerRows = await cmdHeader.ExecuteNonQueryAsync();

                                if (headerRows == 0)
                                {
                                    throw new Exception("Printing request header not found for update.");
                                }
                            }

                            // =================================================
                            // DELETE OLD DETAILS
                            // =================================================

                            string qryDeleteDetails = @" DELETE FROM PRINT_REQUEST2 WHERE COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE
                            AND YEAR_CODE = @YEAR_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO";
                            using (SqlCommand cmdDeleteDetails = new SqlCommand(qryDeleteDetails, con, tran))
                            {
                                cmdDeleteDetails.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = compCode;

                                cmdDeleteDetails.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = branchCode;

                                cmdDeleteDetails.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = yearCode;

                                cmdDeleteDetails.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 10).Value = vType;

                                cmdDeleteDetails.Parameters.Add("@V_NO", SqlDbType.Int).Value = vNoInt;

                                await cmdDeleteDetails.ExecuteNonQueryAsync();
                            }
                            // DELETE OLD IMAGES
                            string qryDeleteImages = @" DELETE FROM IMG_TABLE WHERE COMP_CODE = @COMP_CODE  AND BRANCH_CODE = @BRANCH_CODE
                            AND YEAR_CODE = @YEAR_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO";

                            using (SqlCommand cmdDeleteImages = new SqlCommand(qryDeleteImages, con, tran))
                            {
                                cmdDeleteImages.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = compCode;

                                cmdDeleteImages.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = branchCode;

                                cmdDeleteImages.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = yearCode;

                                cmdDeleteImages.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 10).Value = vType;

                                cmdDeleteImages.Parameters.Add("@V_NO", SqlDbType.Int).Value = vNoInt;

                                await cmdDeleteImages.ExecuteNonQueryAsync();
                            }
                        }
                        // INSERT DETAILS

                        if (model.Details != null && model.Details.Any())
                        {
                            int srNo = 1;
                            foreach (var item in model.Details)
                            {
                                string qryDetail = @"
                            INSERT INTO PRINT_REQUEST2(COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, V_DATE, DOC_ID,
                            ITEM_CODE, MAKE_CODE, TECH_DESC, UOM_CODE, REQ_QTY, MAT_TYPE, PRINT_TYPE, FINISH, REMARKS, REQ_REASON,
                            PLACE_USE, WORK_TYPE, PRIORITY_TYPE, SCRAP_TYPE, ACTIVE, STATUS, SRNO, AED)
                            VALUES(@COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @V_DATE, @DOC_ID, @ITEM_CODE,
                            @MAKE_CODE, @TECH_DESC, @UOM_CODE, @REQ_QTY, @MAT_TYPE, @PRINT_TYPE, @FINISH, @REMARKS, @REQ_REASON, @PLACE_USE,
                            @WORK_TYPE, @PRIORITY_TYPE, @SCRAP_TYPE, 1, @STATUS, @SRNO,'A')";

                                using (SqlCommand cmdDetail = new SqlCommand(qryDetail, con, tran))
                                {
                                    cmdDetail.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = compCode;
                                    cmdDetail.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = branchCode;
                                    cmdDetail.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = yearCode;
                                    cmdDetail.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 10).Value = vType;
                                    cmdDetail.Parameters.Add("@V_NO", SqlDbType.Int).Value = vNoInt;
                                    cmdDetail.Parameters.Add("@V_DATE", SqlDbType.DateTime).Value = model.Header.DocDate;
                                    cmdDetail.Parameters.Add("@DOC_ID", SqlDbType.NVarChar, 50).Value = docId;
                                    cmdDetail.Parameters.Add("@ITEM_CODE", SqlDbType.Int).Value = Convert.ToInt32(item.ItemCode);
                                    cmdDetail.Parameters.Add("@MAKE_CODE", SqlDbType.Int).Value = Convert.ToInt32(item.Make);
                                    cmdDetail.Parameters.Add("@TECH_DESC", SqlDbType.NVarChar, 500).Value = item.Description ?? "";
                                    cmdDetail.Parameters.Add("@UOM_CODE", SqlDbType.Int).Value = Convert.ToInt32(item.Unit);
                                    cmdDetail.Parameters.Add("@REQ_QTY", SqlDbType.Decimal).Value = item.Qty;
                                    cmdDetail.Parameters.Add("@MAT_TYPE", SqlDbType.NVarChar, 100).Value = item.MatType ?? "";
                                    cmdDetail.Parameters.Add("@PRINT_TYPE", SqlDbType.NVarChar, 100).Value = item.PrintingType ?? "";
                                    cmdDetail.Parameters.Add("@FINISH", SqlDbType.NVarChar, 100).Value = item.Finish ?? "";
                                    cmdDetail.Parameters.Add("@REMARKS", SqlDbType.NVarChar, 500).Value = item.Remarks ?? "";
                                    cmdDetail.Parameters.Add("@REQ_REASON",SqlDbType.NVarChar, 500).Value = item.Reason ?? "";
                                    cmdDetail.Parameters.Add("@PLACE_USE", SqlDbType.NVarChar, 200).Value = item.PlaceUse ?? "";
                                    cmdDetail.Parameters.Add("@WORK_TYPE", SqlDbType.NVarChar, 100).Value = item.WorkType ?? "";
                                    cmdDetail.Parameters.Add("@PRIORITY_TYPE", SqlDbType.NVarChar, 100).Value = item.Priority ?? "";
                                    cmdDetail.Parameters.Add("@SCRAP_TYPE", SqlDbType.NVarChar, 100).Value = item.ScrapType ?? "";
                                    cmdDetail.Parameters.Add("@STATUS", SqlDbType.Int).Value = Convert.ToInt32(model.Header.Status);
                                    cmdDetail.Parameters.Add("@SRNO", SqlDbType.Int).Value = srNo++;
                                    await cmdDetail.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // =====================================================
                        // INSERT FILES
                        // INSERT FOR BOTH INSERT & UPDATE
                        // =====================================================

                        if (model.Files != null && model.Files.Any())
                        {
                            int rowId = 1;
                            foreach (var file in model.Files)
                            {
                                if (file == null || file.Length == 0)
                                    continue;
                                byte[] fileBytes;
                                using (var ms = new MemoryStream())
                                {
                                    await file.CopyToAsync(ms);
                                    fileBytes = ms.ToArray();
                                }

                                string qryImage = @"
            INSERT INTO IMG_TABLE
            (
                COMP_CODE,
                BRANCH_CODE,
                YEAR_CODE,
                DOC_ID,
                V_NO,
                V_TYPE,
                V_DATE,
                ROWID,
                IMG_FILE,
                FILE_NAME,
                FILE_TYPE,
                UUSER,
                UDATE,
                AED,
                WSID,
                LIP,
                LID
            )
            VALUES
            (
                @COMP_CODE,
                @BRANCH_CODE,
                @YEAR_CODE,
                @DOC_ID,
                @V_NO,
                @V_TYPE,
                @V_DATE,
                @ROWID,
                @IMG_FILE,
                @FILE_NAME,
                @FILE_TYPE,
                @UUSER,
                GETDATE(),
                'A',
                @WSID,
                @LIP,
                @LID
            )";

                                using (SqlCommand cmdImage =
                                       new SqlCommand(qryImage, con, tran))
                                {
                                    cmdImage.Parameters.Add("@COMP_CODE",
                                        SqlDbType.Int).Value = compCode;

                                    cmdImage.Parameters.Add("@BRANCH_CODE",
                                        SqlDbType.Int).Value = branchCode;

                                    cmdImage.Parameters.Add("@YEAR_CODE",
                                        SqlDbType.Int).Value = yearCode;

                                    cmdImage.Parameters.Add("@DOC_ID",
                                        SqlDbType.NVarChar, 50).Value = docId;

                                    cmdImage.Parameters.Add("@V_NO",
                                        SqlDbType.Int).Value = vNoInt;

                                    cmdImage.Parameters.Add("@V_TYPE",
                                        SqlDbType.NVarChar, 10).Value = vType;

                                    cmdImage.Parameters.Add("@V_DATE",
                                        SqlDbType.DateTime).Value =
                                        model.Header.DocDate;

                                    cmdImage.Parameters.Add("@ROWID",
                                        SqlDbType.Int).Value = rowId++;

                                    cmdImage.Parameters.Add("@IMG_FILE",
                                        SqlDbType.VarBinary, -1).Value = fileBytes;

                                    cmdImage.Parameters.Add("@FILE_NAME",
                                        SqlDbType.NVarChar, 255).Value =
                                        file.FileName;

                                    cmdImage.Parameters.Add("@FILE_TYPE",
                                        SqlDbType.NVarChar, 50).Value =
                                        Path.GetExtension(file.FileName);

                                    cmdImage.Parameters.Add("@UUSER",
                                        SqlDbType.Int).Value =
                                        globalVar.PubUserId;

                                    cmdImage.Parameters.Add("@WSID",
                                        SqlDbType.NVarChar, 100).Value =
                                        globalVar.PubWorkStationID ?? "";

                                    cmdImage.Parameters.Add("@LIP",
                                        SqlDbType.NVarChar, 100).Value =
                                        globalVar.PubLocalId ?? "";

                                    cmdImage.Parameters.Add("@LID",
                                        SqlDbType.NVarChar, 100).Value =
                                        Environment.MachineName;

                                    await cmdImage.ExecuteNonQueryAsync();
                                }
                            }
                        }
                        // =====================================================
                        // COMMIT
                        // =====================================================

                        await tran.CommitAsync();

                        return Json(new
                        {
                            success = true,
                            message = action == "INSERT"
                                ? "Saved Successfully"
                                : "Updated Successfully",
                            action = action,
                            vType = vType,
                            vNo = vNo,
                            docId = docId
                        });
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            await tran.RollbackAsync();
                        }
                        catch
                        {
                            // Ignore rollback error
                        }

                        return Json(new
                        {
                            success = false,
                            message = ex.Message
                        });
                    }
                }
            }
        }

        [HttpPost]
        public IActionResult GetById([FromBody] GetByIdRequest request)
        {
            try
            {
                var global = _globalVariableService.GetGlobalVariables();

                int vNo = request.VNo;
                string vType = request.VType;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_GetPrintingRequestion", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int)
                        .Value = global.PubCompCode;

                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int)
                        .Value = global.PubBranchCode;

                    cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int)
                        .Value = global.PubFYearCode;

                    cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 15)
                        .Value = "GetById";

                    cmd.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 50)
                        .Value = vType;

                    cmd.Parameters.Add("@V_NO", SqlDbType.Int)
                        .Value = vNo;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // ==========================================
                        // HEADER
                        // ==========================================

                        var header = new Dictionary<string, object>();

                        if (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                header[reader.GetName(i)] =
                                    reader.IsDBNull(i)
                                        ? null
                                        : reader.GetValue(i);
                            }
                        }


                        // ==========================================
                        // DETAILS
                        // ==========================================

                        var details = new List<Dictionary<string, object>>();

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                var detail = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    detail[reader.GetName(i)] =
                                        reader.IsDBNull(i)
                                            ? null
                                            : reader.GetValue(i);
                                }

                                details.Add(detail);
                            }
                        }


                        // ==========================================
                        // FILES / IMAGES
                        // ==========================================

                        var images = new List<object>();

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                string fileName = "";
                                string base64 = "";

                                // FILE NAME
                                int fileNameIndex = reader.GetOrdinal("FILE_NAME");

                                if (!reader.IsDBNull(fileNameIndex))
                                {
                                    fileName =
                                        reader.GetValue(fileNameIndex).ToString();
                                }


                                // IMAGE BYTES
                                int imageIndex = reader.GetOrdinal("IMG_FILE");

                                if (!reader.IsDBNull(imageIndex))
                                {
                                    byte[] imageBytes =
                                        (byte[])reader.GetValue(imageIndex);

                                    base64 =
                                        Convert.ToBase64String(imageBytes);
                                }


                                images.Add(new
                                {
                                    filE_NAME = fileName,
                                    imG_FILE = base64
                                });
                            }
                        }


                        // ==========================================
                        // RESPONSE
                        // ==========================================

                        return Json(new
                        {
                            success = true,
                            header = header,
                            details = details,
                            images = images
                        });
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

    }
}
