using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    [SessionAuthorize]
    public class EInvoiceCancellationApprovalListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public EInvoiceCancellationApprovalListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, GlobalValidationdate globalValidationdate, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/EInvoiceCancellationApprovalList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            if (getvariabledata == null)
            {
                return Json(new { success = false, message = "Global variable data is null." });
            }

            int totalCount = 0;
            var headerList = new List<EInvoiceCancellationApproval_Detail>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_EInvoiceCancellationApproval", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getvariabledata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getvariabledata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getvariabledata.PubBranchCode);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            headerList.Add(new EInvoiceCancellationApproval_Detail
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue

                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data.", error = ex.Message });
            }


            return Json(new { success = true, lists = headerList, totalCount });
        }

        [HttpPost]
        public IActionResult GetDataByCode([FromForm] string DOC_ID)
        {
            var global = _globalVariableService.GetGlobalVariables();

            EInvoiceCancellationApproval_Model wrapper = new EInvoiceCancellationApproval_Model();

            try
            {
                if (string.IsNullOrWhiteSpace(DOC_ID))
                {
                    return Json(new { success = false, message = "DOC_ID is required." });
                }

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_EInvoiceCancellationApproval", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 30).Value = "SelectedIdData";
                        cmd.Parameters.Add("@DOC_ID", SqlDbType.NVarChar, 30).Value = DOC_ID;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = global.PubCompCode;
                        cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = global.PubBranchCode;
                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = global.PubFYearCode;

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Details.Add(new EInvoiceCancellationApproval_Detail
                                {
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : null,
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : null,
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                                    REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : null,
                                    REF_DATE = rdr["REF_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["REF_DATE"]) : null,
                                    INV_STATUS = rdr["INV_STATUS"]?.ToString(),
                                    EINV_STATUS = rdr["EINV_STATUS"]?.ToString(),
                                    EINV_DATE = rdr["EINV_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EINV_DATE"]) : null,
                                    EWB_STATUS = rdr["EWB_STATUS"]?.ToString(),
                                    EWB_DATE = rdr["EWB_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EWB_DATE"]) : null,
                                    BILL_AMT = rdr["BILL_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["BILL_AMT"]) : null,
                                    BILL_CODE = rdr["BILL_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_CODE"]) : null,
                                    BILL_NAME = rdr["BILL_NAME"]?.ToString(),
                                    BILL_GST = rdr["BILL_GST"]?.ToString(),
                                    BILL_PINCODE = rdr["BILL_PINCODE"]?.ToString(),
                                    SHIP_CODE = rdr["SHIP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_CODE"]) : null,
                                    SHIP_NAME = rdr["SHIP_NAME"]?.ToString(),
                                    SHIP_GST = rdr["SHIP_GST"]?.ToString(),
                                    SHIP_PINCODE = rdr["SHIP_PINCODE"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    IRN = rdr["IRN"]?.ToString(),
                                    STATUS = rdr["STATUS"]?.ToString(),
                                    REASON = rdr["REASON"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, data = wrapper.Details });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching E-Invoice Cancellation Approval data.", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {

            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_EInvoiceCancellationApproval", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Action", "ExportToExcel");

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("EInvoiceCancellationApproval");

                        // Header
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var cell = ws.Cell(1, i + 1);
                            cell.Value = reader.GetName(i);
                            cell.Style.Font.Bold = true;
                            cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        }

                        int row = 2;
                        while (await reader.ReadAsync())
                        {
                            for (int col = 0; col < reader.FieldCount; col++)
                            {
                                var cell = ws.Cell(row, col + 1);

                                if (reader[col] == DBNull.Value)
                                {
                                    cell.Value = "";
                                }
                                else if (reader.GetFieldType(col) == typeof(DateTime))
                                {
                                    cell.Value = Convert.ToDateTime(reader[col]);
                                    cell.Style.DateFormat.Format = "dd-MM-yyyy";
                                }
                                else
                                {
                                    cell.Value = reader[col].ToString();
                                }
                            }
                            row++;
                        }

                        ws.Columns().AdjustToContents();

                        foreach (var col in ws.Columns())
                        {
                            if (col.Width > 40) col.Width = 40;
                            if (col.Width < 10) col.Width = 10;
                        }

                        ws.Style.Alignment.WrapText = true;
                        ws.SheetView.FreezeRows(1);

                        var range = ws.RangeUsed();
                        if (range != null)
                        {
                            range.CreateTable();
                        }

                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            stream.Position = 0;

                            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EInvoiceCancellationApproval.xlsx");
                        }
                    }
                }

            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(string searchTerm = null, string Sp_Name = "sp_EInvoiceCancellationApproval", string Actionparameter = "ExportToExcel", string ReportName = "EInvoiceCancellationApproval")
        {
            byte[] pdfBytes = await _globalValidationdate.ExportToPdf(searchTerm, Sp_Name, Actionparameter, ReportName);
            string fileName = string.IsNullOrWhiteSpace(ReportName) ? "Report.pdf" : ReportName + ".pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<InwardEntryDetailDto> docDetails = new List<InwardEntryDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_EInvoiceCancellationApproval", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@DOC_ID", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var InwardEntryDetailDto = new InwardEntryDetailDto
                            {
                                Code = reader["Code"]?.ToString(),
                                UUser = reader["UUser"]?.ToString(),
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            };
                            docDetails.Add(InwardEntryDetailDto);
                        }
                    }
                }
            }

            return Json(new { success = true, data = docDetails });
        }
        public class InwardEntryDetailDto
        {
            public string? Code { get; set; }
            public string? UUser { get; set; }
            public DateTime? UDATE { get; set; }
            public string? EUSER { get; set; }
            public DateTime? EDATE { get; set; }
            public string? WSID { get; set; }
            public string? LIP { get; set; }
            public string? LID { get; set; }
        }

        public JsonResult Delete(int code, string VType)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Check approval status
                    string sql = @"SELECT STATUS FROM APPROVAL_STATUS
                           WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE  AND 
                           BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE
                           AND STATUS = 'OPEN' AND USER_CODE <> @USER_CODE";

                    using (SqlCommand cmdCheck = new SqlCommand(sql, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@V_TYPE", VType);
                        cmdCheck.Parameters.AddWithValue("@V_NO", code);
                        cmdCheck.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmdCheck.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);
                        cmdCheck.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmdCheck.Parameters.AddWithValue("@USER_CODE", getGlobalCode.PubUserId);

                        object result = cmdCheck.ExecuteScalar();

                        if (result != null)
                        {
                            return Json(new { success = false, status = "Validation", message = "This Document Approval is in process, Deletion not allowed." });
                        }
                    }

                    // Delete using Stored Procedure
                    using (SqlCommand cmd = new SqlCommand("sp_EInvoiceCancellationApproval", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@V_TYPE", VType);
                        cmd.Parameters.AddWithValue("@DOC_ID", VType + code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Successfully Deleted" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error Deleting E-Invoice Cancellation Approval.", error = ex.Message });
            }
        }

    }
}
