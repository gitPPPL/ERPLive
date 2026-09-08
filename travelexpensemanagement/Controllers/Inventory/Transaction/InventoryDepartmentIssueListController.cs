using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{

    [SessionAuthorize]
    public class InventoryDepartmentIssueListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IInventoryDepartmentIssueListRepository _inventoryDepartmentIssueListRepository;
        public string Fromname = "AdjustmentIssue";

        public InventoryDepartmentIssueListController(DataBaseConnection dbConnection,
         GlobalVariableService globalVariableService,  DropdownService dropdownService,  travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
         travelexpensemanagement.ModuleService.ModuleService moduleService,  GlobalValidationdate globalValidationdate,IInventoryDepartmentIssueListRepository inventoryDepartmentIssueListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _inventoryDepartmentIssueListRepository = inventoryDepartmentIssueListRepository;
        }
        public IActionResult Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/Inventory/Transaction/InventoryDepartmentIssueList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetList(  string searchTerm = "", int pageNumber = 1,  int pageSize = 10)
        {
            try
            {
                var result = await _inventoryDepartmentIssueListRepository.GetListAsync(searchTerm, pageNumber, pageSize, "AdjustmentIssue");

                return Json(new  { success = true,  data = result.Lists,  totalCount = result.TotalCount, pageNumber = pageNumber,  pageSize = pageSize });
            }
            catch (Exception ex)
            {
                return Json(new {  success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Delete(string docId , int V_NO , string V_TYPE)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docId))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Document ID is required."
                    });
                }

                var result = await _inventoryDepartmentIssueListRepository.DeleteAsync(docId, V_NO, V_TYPE);

                if (result)
                {
                    return Json(new {  success = true,  message = "Successfully Deleted"  });
                }

                return Json(new { success = false, message = "Unable to delete Department Issue."  });
            }
            catch (Exception ex)
            {
                return Json(new { success = false,  message = "Error Deleting Department Issue.", error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> DocDetailsCode(string docCode)
        {
            try
            {
                var data = await _inventoryDepartmentIssueListRepository.DocDetailsCodeAsync(docCode);

                return Json(new {  success = true, data = data  });
            }
            catch (Exception ex)
            {
                return Json(new { success = false,  message = "Error getting document details.",  error = ex.Message });
            }
        }


        [HttpPost]
        public IActionResult GetDataByCode(string DocID)
        {
            try
            {
                var data = _inventoryDepartmentIssueListRepository.GetDataByCode(DocID);

                return Json(new {  success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false,  message = "Error fetching inventory department issue data", error = ex.Message });
            }
        }



        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {

            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_InventoryDepartmentIssue", conn))
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
                        var ws = workbook.Worksheets.Add("InventoryDepartmentIssue");

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

                            return File(
                                stream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryDepartmentIssue.xlsx"
                            );
                        }
                    }
                }

            }
        }


        [HttpGet]
        public async Task<IActionResult> ExportPdf(string searchTerm = null, string Sp_Name = "sp_InventoryDepartmentIssue", string Actionparameter = "ExportToExcel", string ReportName = "InventoryDepartmentIssue")
        {
            byte[] pdfBytes = await _globalValidationdate.ExportToPdf(searchTerm, Sp_Name, Actionparameter, ReportName);
            string fileName = string.IsNullOrWhiteSpace(ReportName) ? "Report.pdf" : ReportName + ".pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

    }
}
