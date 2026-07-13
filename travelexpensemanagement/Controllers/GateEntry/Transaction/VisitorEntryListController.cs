using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

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
        private readonly IVisitorListRepository _visitorListRepo;
        public VisitorEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
        ModuleService.ModuleService moduleService, IVisitorListRepository visitorListRepo)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _visitorListRepo = visitorListRepo;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Visitor Inward";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/GateEntry/Transaction/VisitorEntryList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllVisitors(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var (visitors, totalCount) = _visitorListRepo.GetAllVisitors(searchTerm, pageNumber, pageSize);

            return Json(new { success = true, visitors, totalCount });
        }

        [HttpGet]
        public IActionResult GetVisitorByVno(string docId)
        {
            string base64Image;
            var visitor = _visitorListRepo.GetVisitorByVno(docId, out base64Image);

            return Json(new
            {
                success = true,
                data = visitor,
                base64Image = base64Image
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportVisitorToExcel(string searchTerm = null)
        {
            var dt = await _visitorListRepo.ExportVisitorToExcel(searchTerm);

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Visitor");

                // Header
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = dt.Columns[i].ColumnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                }

                // Data
                for (int row = 0; row < dt.Rows.Count; row++)
                {
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        var cell = ws.Cell(row + 2, col + 1);
                        var value = dt.Rows[row][col];

                        if (value == DBNull.Value)
                            cell.Value = "";
                        else if (value is DateTime dtVal)
                        {
                            cell.Value = dtVal;
                            cell.Style.DateFormat.Format = "dd-MM-yyyy";
                        }
                        else
                            cell.Value = value.ToString();
                    }
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Visitor.xlsx");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportVisitorToPdf(string searchTerm = null)
        {
            var dt = await _visitorListRepo.ExportVisitorToPdf(searchTerm);

            using (var stream = new MemoryStream())
            {
                Document document = new Document(PageSize.A4.Rotate(), 10, 10, 10, 10);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                var title = new Paragraph("Visitor Entry Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };

                document.Add(title);
                document.Add(new Paragraph(" "));

                PdfPTable table = new PdfPTable(dt.Columns.Count);
                table.WidthPercentage = 100;

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);

                foreach (DataColumn col in dt.Columns)
                {
                    table.AddCell(new PdfPCell(new Phrase(col.ColumnName, headerFont))
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        BackgroundColor = BaseColor.LIGHT_GRAY
                    });
                }

                var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

                foreach (DataRow row in dt.Rows)
                {
                    foreach (var item in row.ItemArray)
                    {
                        string value = "";

                        if (item != DBNull.Value)
                        {
                            if (item is DateTime dtVal)
                                value = dtVal.ToString("dd-MM-yyyy");
                            else
                                value = item.ToString();
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
 