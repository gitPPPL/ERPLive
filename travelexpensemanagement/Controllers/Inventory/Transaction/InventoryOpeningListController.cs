using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    [SessionAuthorize]
    public class InventoryOpeningListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly IOutwardEntryListRepository _outwardEntryListRepository;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;


        public InventoryOpeningListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DbHelper dbHelper, ModuleService.ModuleService moduleService, IOutwardEntryListRepository outwardEntryListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _outwardEntryListRepository = outwardEntryListRepository;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Material Outward";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Inventory/Transaction/InventoryOpeningList/Index.cshtml" , model);
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
            var headerList = new List<InventoryOpeningEntry_Header>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_InventoryOpening", conn))
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
                            headerList.Add(new InventoryOpeningEntry_Header
                            {
                           
                                NAME = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
                                YEAR_CODE = reader["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["YEAR_CODE"]) : 0,
                                COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                BRANCH_CODE = reader["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BRANCH_CODE"]) : 0,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                SHIFT = reader["SHIFT"] != DBNull.Value ? reader["SHIFT"].ToString() : string.Empty,
                                SLIP_NO = reader["SLIP_NO"] != DBNull.Value ? reader["SLIP_NO"].ToString() : string.Empty,
                                PORD_TYPE = reader["PORD_TYPE"] != DBNull.Value ? reader["PORD_TYPE"].ToString() : string.Empty,
                                PORD_NO = reader["PORD_NO"] != DBNull.Value ? Convert.ToInt32(reader["PORD_NO"]) : 0,
                                PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : 0,
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                DEPT_CODE = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0,
                                REMARKS = reader["REMARKS"] != DBNull.Value ? reader["REMARKS"].ToString() : string.Empty,
                                CONS_TYPE = reader["CONS_TYPE"] != DBNull.Value ? reader["CONS_TYPE"].ToString() : string.Empty,
                                STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0,
                                AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : 0,
                                PLAN_TYPE = reader["PLAN_TYPE"] != DBNull.Value ? reader["PLAN_TYPE"].ToString() : string.Empty,
                                PLAN_NO = reader["PLAN_NO"] != DBNull.Value ? Convert.ToInt32(reader["PLAN_NO"]) : 0,
                                FAPROV_STATUS = reader["FAPROV_STATUS"] != DBNull.Value ? reader["FAPROV_STATUS"].ToString() : string.Empty,
                                FAPROV_REMARKS = reader["FAPROV_REMARKS"] != DBNull.Value ? reader["FAPROV_REMARKS"].ToString() : string.Empty,
                                CODE = reader["CODE"] != DBNull.Value ? reader["CODE"].ToString() : string.Empty

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
        public IActionResult GetDataByCode(string DocID)

        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();


            InventoryOpeningEntry_Model wrapper = new InventoryOpeningEntry_Model
            {
                Header = new InventoryOpeningEntry_Header(),
                Details = new List<InventoryOpeningEntry_Details>()

            };

            try
            {

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    #region Fetch Header Data
                    using (SqlCommand cmd = new SqlCommand("sp_InventoryOpening", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@SaveAction", "Header");
                        cmd.Parameters.AddWithValue("@DOC_ID", DocID);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                wrapper.Header = new InventoryOpeningEntry_Header
                                {
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    V_NO = rdr["V_no"] != DBNull.Value ? Convert.ToInt32(rdr["V_no"]) : 0,
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_DATE = rdr["V_date"] != DBNull.Value ? Convert.ToDateTime(rdr["V_date"]) : DateTime.MinValue,
                                    REMARKS = rdr["REMARKS"]?.ToString()                            

                                };
                            }
                        }
                    }
                    #endregion

                    #region Fetch Dispatch Data
                    using (SqlCommand cmd4 = new SqlCommand("sp_InventoryOpening", con))
                    {
                        cmd4.CommandType = CommandType.StoredProcedure;
                        cmd4.Parameters.AddWithValue("@Action", "ShowData");
                        cmd4.Parameters.AddWithValue("@SaveAction", "Details");
                        cmd4.Parameters.AddWithValue("@DOC_ID", DocID);
                        cmd4.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd4.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd4.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);


                        using (SqlDataReader rdr = cmd4.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Details.Add(new InventoryOpeningEntry_Details
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    MAKE_CODE = rdr["MAKE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MAKE_CODE"]) : 0,
                                    UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                                    NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                                    QTY = rdr["QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY"]) : 0,
                                    RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                                    AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                                    TO_DEPT = rdr["TO_DEPT"] != DBNull.Value ? Convert.ToInt32(rdr["TO_DEPT"]) : 0,
                                    REMARKS = rdr["REMARKS"]?.ToString()
                                });
                            }
                        }
                    }
                    #endregion
                }

                // Return the data as a wrapped result in JSON format
                var resultWrapper = new
                {
                    Header = wrapper.Header,
                    Details = wrapper.Details

                };

                return Json(new { success = true, data = resultWrapper });
            }
            catch (Exception ex)
            {
                // Handle any errors and return them in the JSON response
                return Json(new { success = false, message = "Error fetching purchase requisition data", error = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult Delete(string DocID)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_InventoryOpening", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_ID", DocID);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Successfully Delete" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error Deleting Inward Entry .", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {

            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_InventoryOpening", conn))
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
                        var ws = workbook.Worksheets.Add("GateInward");

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
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                "GateInward.xlsx"
                            );
                        }
                    }
                }

            }
        }
        [HttpGet]
        public async Task<IActionResult> ExportToPdf(string searchTerm = null)
        {
            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_InventoryOpening", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Action", "ExportToExcel");

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    using (var stream = new MemoryStream())
                    {
                        // ✅ PDF Setup (Landscape A4)
                        var document = new iTextSharp.text.Document(
                            iTextSharp.text.PageSize.A4.Rotate(), 10, 10, 10, 10);

                        iTextSharp.text.pdf.PdfWriter.GetInstance(document, stream);
                        document.Open();

                        // ✅ Fonts (FIXED - no ambiguity)
                        var titleFont = iTextSharp.text.FontFactory.GetFont(
                            iTextSharp.text.FontFactory.HELVETICA_BOLD, 14);

                        var headerFont = iTextSharp.text.FontFactory.GetFont(
                            iTextSharp.text.FontFactory.HELVETICA_BOLD, 9);

                        var dataFont = iTextSharp.text.FontFactory.GetFont(
                            iTextSharp.text.FontFactory.HELVETICA, 8);

                        // ✅ Title
                        var title = new iTextSharp.text.Paragraph("Gate Inward Report", titleFont);
                        title.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                        document.Add(title);

                        document.Add(new iTextSharp.text.Paragraph(" ")); // spacing

                        // ✅ Table
                        int columnCount = reader.FieldCount;
                        var table = new iTextSharp.text.pdf.PdfPTable(columnCount);
                        table.WidthPercentage = 100;

                        // Header
                        for (int i = 0; i < columnCount; i++)
                        {
                            var cell = new iTextSharp.text.pdf.PdfPCell(
                                new iTextSharp.text.Phrase(reader.GetName(i), headerFont));

                            cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                            cell.BackgroundColor = iTextSharp.text.BaseColor.LIGHT_GRAY;

                            table.AddCell(cell);
                        }

                        // Data
                        while (await reader.ReadAsync())
                        {
                            for (int col = 0; col < columnCount; col++)
                            {
                                string value = "";

                                if (reader[col] != DBNull.Value)
                                {
                                    if (reader.GetFieldType(col) == typeof(DateTime))
                                    {
                                        value = Convert.ToDateTime(reader[col])
                                                        .ToString("dd-MM-yyyy");
                                    }
                                    else
                                    {
                                        value = reader[col].ToString();
                                    }
                                }

                                var cell = new iTextSharp.text.pdf.PdfPCell(
                                    new iTextSharp.text.Phrase(value, dataFont));

                                table.AddCell(cell);
                            }
                        }

                        document.Add(table);
                        document.Close();

                        return File(stream.ToArray(), "application/pdf", "GateInward.pdf");
                    }
                }
            }
        }


        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<InwardEntryDetailDto> docDetails = new List<InwardEntryDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InventoryOpening", conn))
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

    }
}
