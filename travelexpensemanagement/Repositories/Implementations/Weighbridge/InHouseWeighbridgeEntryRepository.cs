using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge;

namespace travelexpensemanagement.Repositories.Implementations.Weighbridge
{
    public class InHouseWeighbridgeEntryRepository : IInHouseWeighbridgeEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DbHelper _dbHelper;
        public InHouseWeighbridgeEntryRepository(DataBaseConnection dbConnection,GlobalVariableService globalVariableService,GlobalValidationdate globalValidationdate,DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dbHelper = dbHelper;
        }
        public async Task<IActionResult> SaveOrUpdateInHouseWeighBridgeEntryasync(WBEntryModel model)
        {
            if (model == null)
            {
                return new JsonResult(new {  status = false,  message = "Model data is null." });
            }
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalVariableService.GetGlobalVariables();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            bool success = true;
                            string errorMessage = string.Empty;

                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_WBEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue(
                                    "@Action",
                                    model.SaveOrUpdate == "Save" ? "Add" : "Edit"
                                );
           
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_SHIFT", model.V_SHIFT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_TYPE", model.WB_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_TYPE", model.GATE_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_NO", model.GATE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_QTY", model.PARTY_QTY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GROSS_NO", model.GROSS_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TARE_NO", model.TARE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VEHICLE_NO", model.VEHICLE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STATUS_DATE", model.STATUS_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@NET_WGT", model.NET_WGT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FINAL_TYPE", model.FINAL_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FINAL_REM", model.FINAL_REM ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_GROSSWT", model.PARTY_GROSSWT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_TRWT", model.PARTY_TRWT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_WBNO", model.PARTY_WBNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SMALL_BAG", model.SMALL_BAG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MEDIUM_BAG", model.MEDIUM_BAG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LARGE_BAG", model.LARGE_BAG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);
        
                                var tvp = new SqlParameter("@WB2Data", SqlDbType.Structured)
                                {
                                    TypeName = "Type_WB2",
                                    Value = ToWB2DataTable(model.WB2Data)
                                };

                                cmd.Parameters.Add(tvp);

                                // Return Parameter
                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.ReturnValue
                                };

                                cmd.Parameters.Add(returnParam);

                                // Output Parameter
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 54000)
                                {
                                    Direction = ParameterDirection.Output
                                };

                                cmd.Parameters.Add(errorParam);

                                // Execute
                                await cmd.ExecuteNonQueryAsync();

                                int result = returnParam.Value != DBNull.Value
                                    ? Convert.ToInt32(returnParam.Value)
                                    : 0;

                                errorMessage = errorParam.Value?.ToString() ?? "";

                                if (result <= 0)
                                {
                                    success = false;
                                }
                            }

                            if (success)
                            {
                                await transaction.CommitAsync();

                                if (model.SaveOrUpdate == "Update")
                                {
                                    _globalValidationdate.LogInsertUpdateDelete(
                                        destinationTable: "WB1",
                                        sourceTable: "WB1",
                                        transactionType: "Transaction",
                                        codeVNo: model.V_NO.ToString(),
                                        vtype: model.V_TYPE
                                    );

                                    _globalValidationdate.LogInsertUpdateDelete(
                                        destinationTable: "WB2",
                                        sourceTable: "WB2",
                                        transactionType: "Transaction",
                                        codeVNo: model.V_NO.ToString(),
                                        vtype: model.V_TYPE
                                    );
                                }
                                return new JsonResult(new  { status = true, message = "Data saved/updated successfully." });
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return new JsonResult(new  {  status = false,  message = "Failed to save/update data."  });
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();

                            return new JsonResult(new  { status = false, message = "Transaction failed : " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new {  status = false,  message = "Error : " + ex.Message  });
            }
        }
        private DataTable ToWB2DataTable(List<TypeWB2> items)
        {
            var table = new DataTable();

            table.Columns.Add("V_SHIFT", typeof(string));
            table.Columns.Add("TYPE", typeof(string));
            table.Columns.Add("WEIGHT", typeof(decimal));
            table.Columns.Add("TARE_WGT", typeof(decimal));
            table.Columns.Add("NET_WGT", typeof(decimal));
            table.Columns.Add("WGT_DATE", typeof(DateTime));
            table.Columns.Add("WGT_TIME", typeof(string));
            table.Columns.Add("FROM_PLACE", typeof(int));
            table.Columns.Add("FROM_NAME", typeof(string));
            table.Columns.Add("TO_PLACE", typeof(int));
            table.Columns.Add("TO_NAME", typeof(string));
            table.Columns.Add("ITEM_CODE", typeof(int));
            table.Columns.Add("ITEM_NAME", typeof(string));
            table.Columns.Add("REMARKS", typeof(string));
            table.Columns.Add("STATUS", typeof(string));
            table.Columns.Add("Ref_type", typeof(string));
            table.Columns.Add("Ref_no", typeof(int));
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("wb_time", typeof(string));
            table.Columns.Add("COND", typeof(string));
            table.Columns.Add("MOIS_PER", typeof(decimal));
            table.Columns.Add("MOIS_WT", typeof(decimal));

            int srno = 1;

            foreach (var item in items ?? new List<TypeWB2>())
            {
                table.Rows.Add(
                    item.V_SHIFT ?? (object)DBNull.Value,
                    item.TYPE ?? (object)DBNull.Value,
                    item.WEIGHT,
                    item.TARE_WGT,
                    item.NET_WGT,
                    item.WGT_DATE,
                    item.WGT_TIME ?? (object)DBNull.Value,
                    item.FROM_PLACE,
                    item.FROM_NAME ?? (object)DBNull.Value,
                    item.TO_PLACE,
                    item.TO_NAME ?? (object)DBNull.Value,
                    item.ITEM_CODE,
                    item.ITEM_NAME ?? (object)DBNull.Value,
                    item.REMARKS ?? (object)DBNull.Value,
                    item.STATUS ?? (object)DBNull.Value,
                    item.Ref_type ?? (object)DBNull.Value,
                    item.Ref_no,
                    srno,
                    item.wb_time ?? (object)DBNull.Value,
                    item.COND ?? (object)DBNull.Value,
                    item.MOIS_PER,
                    item.MOIS_WT
                );

                srno++;
            }

            return table;
        }

        [HttpGet]
        public async Task<byte[]> ExportToExcel(string searchTerm = null) 
        {
            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_GetWBEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Excel");
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm",  string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm);

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

                            return stream.ToArray();
                        }
                    }
                }
            }
        }

        [HttpGet]
        public async Task<byte[]> ExportToPdf(string searchTerm = null)   
        {
            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_GetWBEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "ExportToExcel");
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);

                    cmd.Parameters.AddWithValue(  "@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm);

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

                        return stream.ToArray();
                    }
                }
            }
        }

    }
}