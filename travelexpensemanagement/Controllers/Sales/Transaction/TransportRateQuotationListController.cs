using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Sale;
using static travelexpensemanagement.Models.Sale.Sale_TransportRateQuatation_Model;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class TransportRateQuotationListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _Dbhelper;
        private readonly GlobalValidationdate _globalValidationdate;



        public TransportRateQuotationListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper,
        ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _Dbhelper = dbHelper;
            _globalValidationdate = globalValidationdate;

        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/TransportRateQuotationList/Index.cshtml");
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            if (getvariabledata == null)
            {
                return Json(new { success = false, message = "Global variable data is null." });
            }

            int totalCount = 0;
            var headerList = new List<sale_TransportRate_Header>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_TransportRateQuatation", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getvariabledata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getvariabledata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            headerList.Add(new sale_TransportRate_Header
                            {
                                DO_NO = reader["DO_NO"] != DBNull.Value ? Convert.ToInt32(reader["DO_NO"]) : 0,
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                BILL_NAME = reader["BILL_NAME"] != DBNull.Value ? reader["BILL_NAME"].ToString() : string.Empty,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                             
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

        [HttpDelete]
        public JsonResult Delete(int code)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TransportRateQuatation", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "TRQT");
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Transport Rate Quotation deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Transport Rate Quotation.", error = ex.Message });
            }
        }

        [HttpGet]

        public async Task<IActionResult> GetDataByCode(string code)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();

            var HeaderDetails = new Dictionary<string, object>
                    {
                        {"@Action","ShowData" },
                        {"@searchOption","Header" },
                        {"@DOC_ID", code },
                        {"@COMP_CODE", GetGlobalCode.PubCompCode },
                        {"@BRANCH_CODE", GetGlobalCode.PubBranchCode },
                        {"@YEAR_CODE", GetGlobalCode.PubFYearCode }
                    };

            var HeaderdataList = await _Dbhelper.GetJsonFromProcedureAsync("sp_TransportRateQuatation", HeaderDetails);

            var DetailsList = new Dictionary<string, object>
                    {
                        {"@Action","ShowData" },
                        {"@searchOption","Details" },
                        {"@DOC_ID", code },
                        {"@COMP_CODE", GetGlobalCode.PubCompCode },
                        {"@BRANCH_CODE", GetGlobalCode.PubBranchCode },
                        {"@YEAR_CODE", GetGlobalCode.PubFYearCode }
                    };

            var DetailList = await _Dbhelper.GetJsonFromProcedureAsync("sp_TransportRateQuatation", DetailsList);

            var resultWrapper = new
            {
                Header = HeaderdataList,
                Details = DetailList

            };

            return Json(new { success = true, data = resultWrapper  });

        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {

            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_TransportRateQuatation", conn))
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
                        var ws = workbook.Worksheets.Add("TransportRateQuotation");

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

                            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "TransportRateQuotation.xlsx");
                        }
                    }
                }

            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(string searchTerm = null, string Sp_Name = "sp_TransportRateQuatation", string Actionparameter = "ExportToExcel", string ReportName = "TransportRateQuotation")
        {
            byte[] pdfBytes = await _globalValidationdate.ExportToPdf(searchTerm, Sp_Name, Actionparameter, ReportName);
            string fileName = string.IsNullOrWhiteSpace(ReportName) ? "Report.pdf" : ReportName + ".pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }


        public JsonResult Delete(int code, string VType)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();        

                    using (SqlCommand cmd = new SqlCommand("sp_TransportRateQuatation", con))
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
                return Json(new { success = true, message = "Successfully Delete" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error Deleting Transport Rate Quatation .", error = ex.Message });
            }
        }


        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<InwardEntryDetailDto> docDetails = new List<InwardEntryDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_TransportRateQuatation", conn))
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
