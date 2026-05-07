using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;



namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class OutwardEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public OutwardEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
         DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;

        }
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/OutwardEntryList/Index.cshtml");
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            int totalCount = 0;
            var headerList = new List<OutWordEntry_Header>();
            var detailsList = new List<Details>();
            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_OutwardEntry", conn))
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
                            headerList.Add(new OutWordEntry_Header
                            {
                                DOC_ID = reader["DOC_ID"]?.ToString(),
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                REF_NO = reader["Ref_no"] != DBNull.Value ? Convert.ToInt32(reader["Ref_no"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                TRUCK_NO = reader["Truck_no"]?.ToString(),
                                BILL_NO = reader["BILL_NO"]?.ToString(),
                                BILL_DATE = reader["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["BILL_DATE"]) : DateTime.MinValue,
                                PARTY_NAME = reader["NAME"]?.ToString(),
                                REF_TYPE = reader["Ref_type"]?.ToString(),
                                V_TYPE = reader["V_TYPE"]?.ToString()
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
        public IActionResult GetDataByCode([FromForm] int rowId, [FromForm] string vtype)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            OutWordEntryModel wrapper = new OutWordEntryModel
            {
                Header = new OutWordEntry_Header(),
                detailsOutwardEntry = new List<DetailsOutwardEntry>()
            };
            try
            {
           
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    #region Fetch Header Data
                    using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@ShowActionOption", "Header");
                        cmd.Parameters.AddWithValue("@V_NO", rowId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                wrapper.Header = new OutWordEntry_Header
                                {
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_NO = rdr["V_no"] != DBNull.Value ? Convert.ToInt32(rdr["V_no"]) : 0,
                                    V_DATE = rdr["V_date"] != DBNull.Value ? Convert.ToDateTime(rdr["V_date"]) : DateTime.MinValue,
                                    V_TIME = rdr["V_TIME"]?.ToString(),
                                    ITEM_TYPE = rdr["ITEM_TYPE"]?.ToString(),
                                    PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : 0,
                                    Add1 = rdr["ADD1"]?.ToString(),
                                    Add2 = rdr["ADD2"]?.ToString(),
                                    Add3 = rdr["ADD3"]?.ToString(),
                                    PARTY_CITY = rdr["PARTY_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CITY"]) : 0,
                                    City = rdr["CITY"]?.ToString(),
                                    STATE_CODE = rdr["STATE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["STATE_CODE"]) : 0,
                                    state = rdr["state"]?.ToString(),
                                    PARTY_PINCODE = rdr["PARTY_PINCODE"]?.ToString(),
                                    TRUCK_NO = rdr["TRUCK_NO"]?.ToString(),
                                    PARTY_GST = rdr["PARTY_GST"]?.ToString(),
                                    REMARKS = rdr["Remarks2"]?.ToString(),
                                    DOC_ID = rdr["DOC_ID"]?.ToString()
                                };
                            }
                        }
                    }
                    #endregion

                    #region Fetch Dispatch Data
                    using (SqlCommand cmd4 = new SqlCommand("sp_OutwardEntry", con))
                    {
                        cmd4.CommandType = CommandType.StoredProcedure;
                        cmd4.Parameters.AddWithValue("@Action", "ShowData");
                        cmd4.Parameters.AddWithValue("@ShowActionOption", "Details");
                        cmd4.Parameters.AddWithValue("@V_NO", rowId);
                        cmd4.Parameters.AddWithValue("@V_TYPE", vtype);
                        cmd4.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd4.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd4.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd4.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.detailsOutwardEntry.Add(new DetailsOutwardEntry
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                                    UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                                    NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                                    QTY = rdr["QTY"] != DBNull.Value ? Convert.ToInt32(rdr["QTY"]) : 0,
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                                    REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0,                                 

                                });
                            }
                        }
                    }
                    #endregion
                }                             
                var resultWrapper = new
                {
                    Header = wrapper.Header,
                    Details = wrapper.detailsOutwardEntry

                };

                return Json(new { success = true, data = resultWrapper });
            }
            catch (Exception ex)
            {
        
                return Json(new { success = false, message = "Error fetching purchase requisition data", error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Delete( string docId)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);  
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error Deleting Outward Entry.", error = ex.Message });
            }
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<InwardEntryDetailDto> docDetails = new List<InwardEntryDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_OutwardEntry", conn))
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

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {
            string query = @" select  a.V_NO as 'Voucher No',a.V_DATE 'Voucher Date',b.Ref_type  as 'Ref Type',b.Ref_no as 'Ref No',a.Truck_no as 'Truck No',a.WayBill_No as 'WayBill No' ,a.BILL_NO as 'BILL NO' ,a.BILL_DATE as 'BILL DATE' ,d.NAME as 'Party Name'    
            FROM gate1 AS a  
            LEFT JOIN gate2 AS b   ON a.v_type = b.v_type   AND a.v_no = b.v_no   AND a.comp_code = b.comp_code  AND a.branch_code = b.branch_code   AND a.Year_Code = b.Year_Code  
            LEFT JOIN DOCTYPE_MAST AS c   ON c.CODE = a.V_TYPE  
            LEFT JOIN SUBGROUP_MAST AS d     ON d.CODE = a.PARTY_CODE    AND d.COMP_CODE = a.COMP_CODE  
            WHERE    a.comp_code = @comp_code  AND a.YEAR_CODE = @YEAR_CODE    AND a.BRANCH_CODE = @BRANCH_CODE    AND c.DOCTYPE = 'GateOutward'  
            AND (@SearchTerm IS NULL OR d.NAME LIKE '%' + @SearchTerm + '%')    ORDER BY   a.V_TYPE,  a.V_NO DESC  ";

            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("GateInward");
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
                            if (col.Width > 40)
                                col.Width = 40;
                        }

                        foreach (var col in ws.Columns())
                        {
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
                            stream.Position = 0;

                            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GateInward.xlsx");
                        }
                    }
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> ExportToPdf(string searchTerm = null)
        {
            string query = @" select  a.V_NO as 'Voucher No',a.V_DATE 'Voucher Date',b.Ref_type  as 'Ref Type',b.Ref_no as 'Ref No',a.Truck_no as 'Truck No',a.WayBill_No as 'WayBill No' ,a.BILL_NO as 'BILL NO' ,a.BILL_DATE as 'BILL DATE' ,d.NAME as 'Party Name'    
            FROM gate1 AS a  
            LEFT JOIN gate2 AS b   ON a.v_type = b.v_type   AND a.v_no = b.v_no   AND a.comp_code = b.comp_code  AND a.branch_code = b.branch_code   AND a.Year_Code = b.Year_Code  
            LEFT JOIN DOCTYPE_MAST AS c   ON c.CODE = a.V_TYPE  
            LEFT JOIN SUBGROUP_MAST AS d     ON d.CODE = a.PARTY_CODE    AND d.COMP_CODE = a.COMP_CODE  
            WHERE    a.comp_code = @comp_code  AND a.YEAR_CODE = @YEAR_CODE    AND a.BRANCH_CODE = @BRANCH_CODE    AND c.DOCTYPE = 'GateOutward'  
            AND (@SearchTerm IS NULL OR d.NAME LIKE '%' + @SearchTerm + '%')    ORDER BY   a.V_TYPE,  a.V_NO DESC  ";

            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
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

                        Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                        Paragraph title = new Paragraph("Gate Inward Report", titleFont);
                        title.Alignment = Element.ALIGN_CENTER;
                        document.Add(title);

                        document.Add(new Paragraph(" "));

                        int columnCount = reader.FieldCount;
                        PdfPTable table = new PdfPTable(columnCount);
                        table.WidthPercentage = 100;

                        Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);

                        for (int i = 0; i < columnCount; i++)
                        {
                            PdfPCell cell = new PdfPCell(new Phrase(reader.GetName(i), headerFont));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                            table.AddCell(cell);
                        }

                        Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 8);

                        while (await reader.ReadAsync())
                        {
                            for (int col = 0; col < columnCount; col++)
                            {
                                string value = "";

                                if (reader[col] != DBNull.Value)
                                {
                                    if (reader.GetFieldType(col) == typeof(DateTime))
                                    {
                                        value = Convert.ToDateTime(reader[col]).ToString("dd-MM-yyyy");
                                    }
                                    else
                                    {
                                        value = reader[col].ToString();
                                    }
                                }

                                PdfPCell cell = new PdfPCell(new Phrase(value, dataFont));
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

        [HttpGet]
        public IActionResult GetDataByPendingorder(int PartyCode, string Type, DateTime v_date, int BILL_NO)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            var Datalist = new List<object>();
            DateTime fromDate = v_date.AddDays(-10);

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd3 = new SqlCommand("[dbo].[sp_OutwardEntry]", con))
                    {
                        cmd3.CommandType = CommandType.StoredProcedure;
                        cmd3.Parameters.AddWithValue("@Action", "PENDINGORDER");
                        cmd3.Parameters.AddWithValue("@ShowActionOption", Type);
                        cmd3.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd3.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd3.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd3.Parameters.AddWithValue("@PARTY_CODE", PartyCode);
                        cmd3.Parameters.Add("@v_date", SqlDbType.SmallDateTime).Value = v_date;
                        cmd3.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = fromDate;
                        cmd3.Parameters.AddWithValue("@BILL_NO", BILL_NO);

                        using (SqlDataReader rdr = cmd3.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var V_type = rdr["V_type"]?.ToString();
                                    var V_no = rdr["V_no"]?.ToString();
                                    var V_DATE = rdr["V_DATE"]?.ToString();
                                    var Item_code = rdr["Item_code"]?.ToString();
                                    var Item_name = rdr["Item_name"]?.ToString();
                                    var UNIT_NAME = rdr["UNIT_NAME"]?.ToString();
                                    var UNIT_CODE = rdr["UNIT_CODE"]?.ToString();
                                    var NOS = rdr["NOS"]?.ToString();
                                    var QTY = rdr["QTY"]?.ToString();
                                    var P_QTY = rdr["P_QTY"]?.ToString();
                                    var REMARK = rdr["REMARK"]?.ToString();
                                    var SRNO = rdr["SRNO"]?.ToString();

                                    if (!string.IsNullOrEmpty(Item_name) && !string.IsNullOrEmpty(Item_name))
                                    {
                                        Datalist.Add(new
                                        {
                                            V_type = V_type,
                                            V_no = V_no,
                                            V_DATE = V_DATE,
                                            Item_code = Item_code,
                                            Item_name = Item_name,
                                            UNIT_NAME = UNIT_NAME,
                                            UNIT_CODE = UNIT_CODE,
                                            NOS = NOS,
                                            QTY = QTY,
                                            P_QTY = P_QTY,
                                            REMARK = REMARK,
                                            SRNO = SRNO
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                return Json(new { success = true, data = Datalist });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching attachment data", error = ex.Message });
            }
        }

    }
}

      
