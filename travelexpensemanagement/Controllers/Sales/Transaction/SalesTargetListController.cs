using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesTargetListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
 
        public SalesTargetListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
       ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesTargetList/Index.cshtml");
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
            var headerList = new List<SalesTargetmodel_Detail>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_SalesTarget", conn))
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
                            headerList.Add(new SalesTargetmodel_Detail
                            {
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,               
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                Partyname = reader["Partyname"] != DBNull.Value ? reader["Partyname"].ToString() : string.Empty,
                                REMARKS = reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : string.Empty,
                                PLAN_MONTH = reader["PLAN_MONTH"] != DBNull.Value ? Convert.ToDateTime(reader["PLAN_MONTH"]) : DateTime.MinValue
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
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();

            SalesTargetmodel wrapper = new SalesTargetmodel
            {
          
                Details = new List<SalesTargetmodel_Detail>()
            };

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();        

                    #region Fetch Dispatch Data
                    using (SqlCommand cmd4 = new SqlCommand("sp_SalesTarget", con))
                    {
                        cmd4.CommandType = CommandType.StoredProcedure;
                        cmd4.Parameters.AddWithValue("@Action", "SHOWDATA");
                        cmd4.Parameters.AddWithValue("@DOC_ID", DOC_ID);
                        cmd4.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd4.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd4.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);


                        using (SqlDataReader rdr = cmd4.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Details.Add(new SalesTargetmodel_Detail
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    PLAN_MONTH = rdr["PLAN_MONTH"] != DBNull.Value ? Convert.ToDateTime(rdr["PLAN_MONTH"]) : DateTime.MinValue,
                                    PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : 0,
                                    salesThrough = rdr["salesThrough"] != DBNull.Value ? Convert.ToInt32(rdr["salesThrough"]) : 0,
                                    SOLD_QTY = rdr["SOLD_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["SOLD_QTY"]) : 0,
                                    TARGET_QTY = rdr["TARGET_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["TARGET_QTY"]) : 0,
                                    MIN_PRICE = rdr["MIN_PRICE"] != DBNull.Value ? Convert.ToDecimal(rdr["MIN_PRICE"]) : 0,
                                    MAX_PRICE = rdr["MAX_PRICE"] != DBNull.Value ? Convert.ToDecimal(rdr["MAX_PRICE"]) : 0,
                                    APP_PRICE = rdr["APP_PRICE"] != DBNull.Value ? Convert.ToDecimal(rdr["APP_PRICE"]) : 0,
                                    VIR_PRICE = rdr["VIR_PRICE"] != DBNull.Value ? Convert.ToDecimal(rdr["VIR_PRICE"]) : 0
                                });
                            }
                        }
                    }
                    #endregion
                }

                var resultWrapper = new {  Details = wrapper.Details };

                return Json(new { success = true, data = resultWrapper });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "Error fetching purchase requisition data", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {

            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_SalesTarget", conn))
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
                        var ws = workbook.Worksheets.Add("SalesTarget");

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

                            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesTarget.xlsx");
                        }
                    }
                }

            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(string searchTerm = null, string Sp_Name = "sp_SalesTarget", string Actionparameter = "ExportToExcel", string ReportName = "SalesTarget")
        {
            byte[] pdfBytes = await _globalValidationdate.ExportToPdf(searchTerm, Sp_Name, Actionparameter, ReportName);
            string fileName = string.IsNullOrWhiteSpace(ReportName) ? "Report.pdf" : ReportName + ".pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

    }
}