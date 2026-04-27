using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Gate_Entry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VisitorEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public VisitorEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
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
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/GateEntry/Transaction/VisitorEntryList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetAllVisitors(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var visitors = new List<VISITOR>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE",globalVar.PubBranchCode);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                visitors.Add(new VISITOR
                                {
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                    DOC_ID = reader["DOC_ID"].ToString(),
                                    NAME = reader["NAME"]?.ToString(),
                                    ORGANIZATION = reader["ORGANIZATION"]?.ToString(),
                                    IN_TIME = reader["IN_TIME"]?.ToString(),
                                    OUT_TIME = reader["OUT_TIME"]?.ToString(),
                                    MEET_NAME = reader["MEET_NAME"]?.ToString(),
                                    PURPOSE = reader["PURPOSE"]?.ToString(),
                                    ADDRESS = reader["ADDRESS"]?.ToString(),
                                    MOBILE_NO = reader["MOBILE_NO"]?.ToString(),
                                    VEHICLE_NO = reader["VEHICLE_NO"]?.ToString(),
                                    //MATERIAL = reader["MATERIAL"]?.ToString(),
                                    //CARD_NO = reader["CARD_NO"]?.ToString()
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching visitors", error = ex.Message });
            }

            return Json(new { success = true, visitors, totalCount });
        }

        [HttpGet]
        public IActionResult GetVisitorByVno(string docId)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            VISITOR visitor = null;
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn)) 
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "GETBYID");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        //cmd.Parameters.AddWithValue("@V_TYPE", vType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                        conn.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                visitor = new VISITOR
                                {
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : (int?)null,
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : (DateTime?)null,
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    //SLIP_NO = rdr["SLIP_NO"]?.ToString(),
                                    NAME = rdr["NAME"]?.ToString(),
                                    CARD_NO = rdr["CARD_NO"]?.ToString(),
                                    ORGANIZATION = rdr["ORGANIZATION"]?.ToString(),
                                    ADDRESS = rdr["ADDRESS"]?.ToString(),
                                    MEET_CODE = rdr["MEET_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MEET_CODE"]) : (int?)null,
                                    MEET_NAME = rdr["MEET_NAME"]?.ToString(),
                                    IN_TIME = rdr["IN_TIME"]?.ToString(),
                                    OUT_DATE = rdr["OUT_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["OUT_DATE"]) : (DateTime?)null,
                                    OUT_TIME = rdr["OUT_TIME"]?.ToString(),
                                    MOBILE_NO = rdr["MOBILE_NO"]?.ToString(),
                                    PURPOSE = rdr["PURPOSE"]?.ToString(),
                                    VEHICLE_NO = rdr["VEHICLE_NO"]?.ToString(),
                                    MATERIAL = rdr["MATERIAL"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    IMG_FILE = rdr["IMG_FILE"] != DBNull.Value ? (byte[])rdr["IMG_FILE"] : null,
                                    FILE_NAME = rdr["FILE_NAME"]?.ToString()
                                    // Add other fields as needed
                                };
                            }
                        }
                    }
                }

                //if (visitor != null && visitor.IMG_FILE != null)
                //{
                //    var imageBytes = visitor.IMG_FILE;
                //    visitor.IMG_FILE = null;

                //    var base64String = Convert.ToBase64String(imageBytes);
                //    return Json(new { success = true, data = visitor, base64Image = base64String });
                //}
                string base64Image = null;

                if (visitor != null && visitor.IMG_FILE != null)
                {
                    base64Image = Convert.ToBase64String(visitor.IMG_FILE);
                }

                return Json(new
                {
                    success = true,
                    data = visitor,
                    base64Image = base64Image
                });

                return Json(new { success = true, data = visitor });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching visitor data", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportVisitorToExcel(string searchTerm = null)
        {
            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // 🔹 Required Params
                    cmd.Parameters.AddWithValue("@Action", "EXPORT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", 100000); 

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Visitor");

                        // Header
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var cell = ws.Cell(1, i + 1);
                            cell.Value = reader.GetName(i);
                            cell.Style.Font.Bold = true;
                            cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        }

                        // Data
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

                        // UI Improve
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

                        // Export
                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            stream.Position = 0;

                            return File(
                                stream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "Visitor.xlsx"
                            );
                        }
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportVisitorToPdf(string searchTerm = null)
        {
            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT_PDF");
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    using (var stream = new MemoryStream())
                    {
                        Document document = new Document(PageSize.A4.Rotate(), 10, 10, 10, 10);
                        PdfWriter.GetInstance(document, stream);
                        document.Open();

                        // Title
                        var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                        var title = new Paragraph("Visitor Entry Report", titleFont)
                        {
                            Alignment = Element.ALIGN_CENTER
                        };
                        document.Add(title);
                        document.Add(new Paragraph(" "));

                        // Table
                        int columnCount = reader.FieldCount;
                        PdfPTable table = new PdfPTable(columnCount);
                        table.WidthPercentage = 100;

                        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);

                        for (int i = 0; i < columnCount; i++)
                        {
                            var cell = new PdfPCell(new Phrase(reader.GetName(i), headerFont))
                            {
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                BackgroundColor = BaseColor.LIGHT_GRAY
                            };
                            table.AddCell(cell);
                        }

                        var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

                        while (await reader.ReadAsync())
                        {
                            for (int col = 0; col < columnCount; col++)
                            {
                                string value = "";

                                if (reader[col] != DBNull.Value)
                                {
                                    if (reader.GetFieldType(col) == typeof(DateTime))
                                        value = Convert.ToDateTime(reader[col]).ToString("dd-MM-yyyy");
                                    else
                                        value = reader[col].ToString();
                                }

                                table.AddCell(new PdfPCell(new Phrase(value, dataFont)));
                            }
                        }

                        document.Add(table);
                        document.Close();

                        return File(stream.ToArray(), "application/pdf", "VisitorReport.pdf");
                    }
                }
            }
        }

    }
}
 