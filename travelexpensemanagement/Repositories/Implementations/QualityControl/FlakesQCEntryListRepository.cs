using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

using travelexpensemanagement.Repositories.Interfaces.QualityControl;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl
{

    public class FlakesQCEntryListRepository : IFlakesQCEntryListRepository
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;

        public FlakesQCEntryListRepository(  DataBaseConnection dbConnection, GlobalVariableService globalVariableService,  GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
        }
        public async Task<object> DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<InwardEntryDetailDto> docDetails = new List<InwardEntryDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_FlakesQCEntry", conn))
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
            return (new { success = true, data = docDetails });
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

        public async Task<byte[]> ExportToExcel(string searchTerm = null)
        {
            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_FlakesQCEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "ExportToExcel");
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);

                    cmd.Parameters.AddWithValue(
                        "@SearchTerm",
                        string.IsNullOrWhiteSpace(searchTerm)
                            ? (object)DBNull.Value
                            : searchTerm
                    );

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Gate Outward");

                        // Header
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var cell = ws.Cell(1, i + 1);

                            cell.Value = reader.GetName(i);

                            cell.Style.Font.Bold = true;

                            cell.Style.Alignment.Horizontal =
                                ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        }

                        int row = 2;

                        // Data
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

                        // Formatting
                        ws.Columns().AdjustToContents();

                        foreach (var col in ws.Columns())
                        {
                            if (col.Width > 40)
                                col.Width = 40;

                            if (col.Width < 10)
                                col.Width = 10;
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

                            byte[] bytes = stream.ToArray();

                            return bytes;
                        }
                    }
                }
            }
        }

        public async Task<byte[]> ExportToPdf(string searchTerm = null)
        {
            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_FlakesQCEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "ExportToPDF");
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);

                    cmd.Parameters.AddWithValue(
                        "@SearchTerm",
                        string.IsNullOrWhiteSpace(searchTerm)
                            ? (object)DBNull.Value
                            : searchTerm
                    );

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    using (var stream = new MemoryStream())
                    {
                        Document document = new Document(
                            PageSize.A4.Rotate(),
                            10,
                            10,
                            10,
                            10
                        );

                        PdfWriter.GetInstance(document, stream);

                        document.Open();

                        // Title
                        Font titleFont = FontFactory.GetFont(
                            FontFactory.HELVETICA_BOLD,
                            14
                        );

                        Paragraph title = new Paragraph(
                            "Gate Outward Report",
                            titleFont
                        );

                        title.Alignment = Element.ALIGN_CENTER;

                        document.Add(title);

                        document.Add(new Paragraph(" "));

                        int columnCount = reader.FieldCount;

                        PdfPTable table = new PdfPTable(columnCount);

                        table.WidthPercentage = 100;

                        table.HeaderRows = 1;

                        table.SplitLate = false;

                        // Header Font
                        Font headerFont = FontFactory.GetFont(
                            FontFactory.HELVETICA_BOLD,
                            9
                        );

                        // Headers
                        for (int i = 0; i < columnCount; i++)
                        {
                            PdfPCell headerCell = new PdfPCell(
                                new Phrase(reader.GetName(i), headerFont)
                            );

                            headerCell.HorizontalAlignment = Element.ALIGN_CENTER;

                            headerCell.BackgroundColor = BaseColor.LIGHT_GRAY;

                            table.AddCell(headerCell);
                        }

                        // Data Font
                        Font dataFont = FontFactory.GetFont(
                            FontFactory.HELVETICA,
                            8
                        );

                        // Data Rows
                        while (await reader.ReadAsync())
                        {
                            for (int col = 0; col < columnCount; col++)
                            {
                                string value = "";

                                if (reader[col] != DBNull.Value)
                                {
                                    if (reader.GetFieldType(col) == typeof(DateTime))
                                    {
                                        value = Convert
                                            .ToDateTime(reader[col])
                                            .ToString("dd-MM-yyyy");
                                    }
                                    else
                                    {
                                        value = reader[col].ToString();
                                    }
                                }

                                PdfPCell dataCell = new PdfPCell(
                                    new Phrase(value, dataFont)
                                );

                                table.AddCell(dataCell);
                            }
                        }

                        document.Add(table);

                        document.Close();

                        byte[] bytes = stream.ToArray();

                        return bytes;
                    }
                }
            }
        }

        public async Task<bool> Delete(int code)
        {
            try
            {
                var getGlobalCode = _globalVariableService.GetGlobalVariables();

                using SqlConnection con = _dbConnection.GetErpConnection();

                using SqlCommand cmd = new("sp_FlakesQCEntry", con);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "DELETE");
                cmd.Parameters.AddWithValue("@V_NO", code);
                cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);

                await con.OpenAsync();

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                //_globalValidationdate.LogInsertUpdateDelete(
                //    destinationTable: "PROD1_QC",
                //    sourceTable: "PROD1_QC",
                //    transactionType: "Transaction",
                //    codeVNo: code.ToString(),
                //    vtype: "SFQC"
                //);

                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}