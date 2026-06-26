using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.ModuleService;
namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseSaudaListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public PurchaseSaudaListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Purchase Contract";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel,
            };
            return View("~/Views/Purchase/Transaction/PurchaseSaudaList/Index.cshtml", model);
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            if (getvariabledata == null)
            {
                return Json(new { success = false, message = "Global variable data is null." });
            }

            int totalCount = 0;
            var headerList = new List<PurchaseSauda_Header>(); 

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_PurchaseSauda", conn))
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
                            headerList.Add(new PurchaseSauda_Header
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                CustomerName = reader["customerName"] != DBNull.Value ? reader["customerName"].ToString() : string.Empty,
                                CityName = reader["City"] != DBNull.Value ? reader["City"].ToString() : string.Empty,
                                PHONE = reader["PHONE"] != DBNull.Value ? reader["PHONE"].ToString() : string.Empty,
                                PARTY_TO = reader["PARTY_TO"] != DBNull.Value ? reader["PARTY_TO"].ToString() : string.Empty,
                                ItemName = reader["ItemName"] != DBNull.Value ? reader["ItemName"].ToString() : string.Empty,
                                Type = reader["ITEM_TYPE"] != DBNull.Value ? reader["ITEM_TYPE"].ToString() : string.Empty,
                                REMARK = reader["REMARK"] != DBNull.Value ? reader["REMARK"].ToString() : string.Empty,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_TYPE = reader["v_TYPE"] != DBNull.Value ? reader["v_TYPE"].ToString() : string.Empty,
                                FRT_TERM = reader["FRT_TERM"] != DBNull.Value ? reader["FRT_TERM"].ToString() : string.Empty,
                                PINO = reader["PINO"] != DBNull.Value ? reader["PINO"].ToString() : string.Empty,
                                OFFERNO = reader["OFFERNO"] != DBNull.Value ? reader["OFFERNO"].ToString() : string.Empty,
                                QTY = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : 0,
                                RATE = reader["RATE"] != DBNull.Value ? Convert.ToDecimal(reader["RATE"]) : 0
                                                             
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

        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();

            PurchaseSauda_model wrapper = new PurchaseSauda_model
            {
                Header = new PurchaseSauda_Header(),
                DispatchDelivery = new List<DispatchDeliveryPlaning>(),
                Document = new List<DocumentAttachment>()
            };

            try
            {
         
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    #region Fetch Header Data
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseSauda", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@searchOption", "Header");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                wrapper.Header = new PurchaseSauda_Header
                                {
                                        DOC_ID = rdr["DOC_ID"]?.ToString(),
                                        V_NO = rdr["V_no"] != DBNull.Value ? Convert.ToInt32(rdr["V_no"]) : 0,
                                        V_DATE = rdr["V_date"] != DBNull.Value ? Convert.ToDateTime(rdr["V_date"]) : DateTime.MinValue,
                                        V_TYPE = rdr["V_TYPE"]?.ToString(),
                                        REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0,
                                        REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                                        SHIP_CODE = rdr["SHIP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_CODE"]) : 0,
                                        PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : 0,
                                        ADD1 = rdr["ADD1"]?.ToString(),
                                        ADD2 = rdr["ADD2"]?.ToString(),
                                        ADD3 = rdr["ADD3"]?.ToString(),
                                        CITY_CODE = rdr["CITY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CITY_CODE"]) : 0,
                                        CityName = rdr["CityName"]?.ToString(),
                                        PHONE = rdr["PHONE"]?.ToString(),
                                        PARTY_TO = rdr["PARTY_TO"]?.ToString(),
                                        ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                        TRUCK_NO = rdr["TRUCK_NO"] != DBNull.Value ? Convert.ToInt32(rdr["TRUCK_NO"]) : 0,
                                        EXRATE = rdr["EXRATE"] != DBNull.Value ? Convert.ToDecimal(rdr["EXRATE"]) : 0,
                                        DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : 0,
                                        FRT_RATE = rdr["FRT_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["FRT_RATE"]) : 0,
                                        REMARK = rdr["REMARK"]?.ToString(),
                                        PINO  = rdr["PINO"]?.ToString(),
                                        PIDATE = rdr["PIDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["PIDATE"]) : DateTime.MinValue,
                                        OFFERNO = rdr["OFFERNO"]?.ToString(),
                                        BROKER_RATE = rdr["BROKER_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["BROKER_RATE"]) : 0,
                                        BROKER = rdr["BROKER"] != DBNull.Value ? Convert.ToInt32(rdr["BROKER"]) : 0,
                                        PACK_TYPE = rdr["PACK_TYPE"]?.ToString(),
                                        DISPATCH_FROM =  rdr["DISPATCH_FROM"] != DBNull.Value ? Convert.ToInt32(rdr["DISPATCH_FROM"]) : 0,
                                        PAYMENT_STATUS = rdr["PAYMENT_STATUS"]?.ToString(),
                                        CURRENCY = rdr["CURRENCY"]?.ToString(),
                                        SBLC_DUEDATE = rdr["SBLC_DUEDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["SBLC_DUEDATE"]) : DateTime.MinValue,
                                        GRADE = rdr["GRADE"]?.ToString(),
                                        ITEM_REMARKS  = rdr["ITEM_REMARKS"]?.ToString(),
                                        WASTE_PER = rdr["Waste_per"] != DBNull.Value ? Convert.ToDecimal(rdr["Waste_per"]) : 0,
                                        RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                                        TAX_CODE = rdr["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TAX_CODE"]) : 0,
                                        ONLY_NATURAL = rdr["ONLY_NATURAL"] != DBNull.Value ? Convert.ToInt32(rdr["ONLY_NATURAL"]) : 0,
                                        ITEM_TYPE = rdr["ITEM_TYPE"]?.ToString(),
                                        QTY = rdr["QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY"]) : 0,
                                        FRT_TERM = rdr["FRT_TERM"]?.ToString(),
                                        NET_RATE = rdr["NET_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["NET_RATE"]) : 0,
                                        PAYTERM_CODE = rdr["PAYTERM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PAYTERM_CODE"]) : 0,
                                        DEL_TERM  = rdr["DEL_TERM"]?.ToString(),
                                        STATUS =  rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : 0,
                                        LC_DUEDATE = rdr["LC_DUEDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["LC_DUEDATE"]) : DateTime.MinValue,
                                        DEAL_THROUGH = rdr["DEAL_THROUGH"] != DBNull.Value ? Convert.ToInt32(rdr["DEAL_THROUGH"]) : 0,
                                        SHIP_TYPE = rdr["Ship_type"]?.ToString(),
                                        Delivery_From = rdr["Delivery_From"]?.ToString(),
                                        COUNTRY = rdr["COUNTRY"]?.ToString(),
                                        COUNTRY_CODE = rdr["COUNTRY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COUNTRY_CODE"]) : 0,
                                        TAX_RATE = rdr["TAX_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["TAX_RATE"]) : 0,
                                        OfferRate = rdr["Offer_Rate"] != DBNull.Value ? Convert.ToDecimal(rdr["Offer_Rate"]) : 0
                                       

                                };
                            }
                        }
                    }
                    #endregion

                    #region Fetch Attachment Data
                    using (SqlCommand cmd3 = new SqlCommand("sp_PurchaseSauda", con))
                    {
                        cmd3.CommandType = CommandType.StoredProcedure;
                        cmd3.Parameters.AddWithValue("@Action", "ShowData");
                        cmd3.Parameters.AddWithValue("@searchOption", "Attachment");
                        cmd3.Parameters.AddWithValue("@V_NO", code);
                        cmd3.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd3.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode) ;
                        cmd3.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd3.Parameters.AddWithValue("@V_TYPE", "PAUD");

                        using (SqlDataReader rdr = cmd3.ExecuteReader())
                        {
                      
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var fileName = rdr["FILE_NAME"]?.ToString();
                                    var filePath = rdr["FILE_Path"]?.ToString();

                                    if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(filePath))
                                    {
                                        wrapper.Document.Add(new DocumentAttachment
                                        {
                                            FileName = fileName,
                                            FilePath = filePath
                                        });
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region Fetch Dispatch Data
                    using (SqlCommand cmd4 = new SqlCommand("sp_PurchaseSauda", con))
                    {
                        cmd4.CommandType = CommandType.StoredProcedure;
                        cmd4.Parameters.AddWithValue("@Action", "ShowData");
                        cmd4.Parameters.AddWithValue("@searchOption", "Dispatch");
                        cmd4.Parameters.AddWithValue("@V_NO", code);
                        cmd4.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd4.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd4.Parameters.AddWithValue("@V_TYPE", "PAUD");

                        cmd4.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd4.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.DispatchDelivery.Add(new DispatchDeliveryPlaning
                                {
                                    ItemCode = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    ItemName = rdr["ITEM_NAME"]?.ToString(),
                                    DeliveryDate = rdr["DELIVERY_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["DELIVERY_DATE"]) : DateTime.MinValue,
                                    Qty = rdr["QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY"]) : 0,
                                    Remarks = rdr["Remarks"]?.ToString(),
                                });
                            }
                        }
                    }
                    #endregion
                }

       
                var resultWrapper = new
                {
                    Header = wrapper.Header,
                    Details = wrapper.DispatchDelivery,
                    Attachment = wrapper.Document
                };

                return Json(new { success = true, data = resultWrapper });
            }
            catch (Exception ex)
            {
              
                return Json(new { success = false, message = "Error fetching purchase requisition data", error = ex.Message });
            }
        }

       public JsonResult Deletevalidation(int code, string v_type)
        {
            var global = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // 1. GATE check
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 1 v_no, v_date  FROM GATE2 WHERE REF_TYPE = @v_type  AND REF_NO = @v_no
                        AND COMP_CODE = @comp AND BRANCH_CODE = @branch AND YEAR_CODE = @year", con))
                    {
                        cmd.Parameters.AddWithValue("@v_type", v_type);
                        cmd.Parameters.AddWithValue("@v_no", code);
                        cmd.Parameters.AddWithValue("@comp", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@branch", global.PubBranchCode);
                        cmd.Parameters.AddWithValue("@year", global.PubFYearCode);

                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                if(global.PubUserLevel != "1")
                                {

                                    return Json(new { success = false, message = $"Exists in Gate Serial No: {r["v_no"]} dated {r["v_date"]}" });

                                }
                                return Json(new { success = true, message = $"Exists in Gate Serial No: {r["v_no"]} dated {r["v_date"]}" });

                            }
                        }
                    }

                    // 2. ORDER check
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 1 v_no, v_date
                        FROM ORDER2
                        WHERE SAUDA_TYPE = @v_type
                        AND SAUDA_NO = @v_no
                        AND COMP_CODE = @comp
                        AND BRANCH_CODE = @branch
                        AND YEAR_CODE = @year", con))
                    {
                        cmd.Parameters.AddWithValue("@v_type", v_type);
                        cmd.Parameters.AddWithValue("@v_no", code);
                        cmd.Parameters.AddWithValue("@comp", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@branch", global.PubBranchCode);
                        cmd.Parameters.AddWithValue("@year", global.PubFYearCode);

                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {

                                if (global.PubUserLevel != "1")
                                {
                                    return Json(new
                                    {
                                        success = false,
                                        message = $"Exists in Order Serial No: {r["v_no"]} dated {r["v_date"]}"
                                    });
                                }

                                    return Json(new
                                    {
                                        success = true,
                                        message = $"Exists in Order Serial No: {r["v_no"]} dated {r["v_date"]}"
                                    });
                            }
                        }
                    }

                    // 3. Approval check
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT FAPROV_STATUS
                        FROM SAUDA
                        WHERE V_TYPE = @v_type
                        AND V_NO = @v_no
                        AND COMP_CODE = @comp
                        AND BRANCH_CODE = @branch
                        AND YEAR_CODE = @year", con))
                    {
                        cmd.Parameters.AddWithValue("@v_type", v_type);
                        cmd.Parameters.AddWithValue("@v_no", code);
                        cmd.Parameters.AddWithValue("@comp", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@branch", global.PubBranchCode);
                        cmd.Parameters.AddWithValue("@year", global.PubFYearCode);

                        var status = cmd.ExecuteScalar()?.ToString();

                        if (status == "Approved")
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Document is Approved. Deletion not allowed."
                            });
                        }
                    }
                }

                // ✅ If everything is OK
                return Json(new
                {
                    success = true,
                    message = "Validation passed. Safe to delete."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error occurred during validation.",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult Delete(int code, string v_type)
        {
            var global = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    // 4. Delete using stored procedure
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseSauda", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", v_type);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                        cmd.ExecuteNonQuery();
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Purchase Sauda deleted successfully."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error deleting Purchase Sauda.",
                    error = ex.Message
                });
            }
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<InwardEntryDetailDto> docDetails = new List<InwardEntryDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PurchaseSauda", conn))
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

            var global = _globalVariableService.GetGlobalVariables();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_PurchaseSauda", conn))
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

                using (SqlCommand cmd = new SqlCommand("sp_PurchaseSauda", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Action", "ExportToPdf");

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

        [HttpGet]
        public async Task<object> GetDataByPurchaseHistory(int V_NO)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            var Datalist = new List<object>();    
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd3 = new SqlCommand("[dbo].[sp_PurchaseSauda]", con))
                    {
                        cmd3.CommandType = CommandType.StoredProcedure;
                        cmd3.Parameters.AddWithValue("@Action", "ShowPurchaseHistory");                   
                        cmd3.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd3.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);                   
                        cmd3.Parameters.AddWithValue("@V_NO", V_NO);           

                        using (SqlDataReader rdr = cmd3.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var Doc_id = rdr["Doc_id"]?.ToString();
                                    var qty = rdr["qty"]?.ToString();
                                    var vdate = rdr["v_date"]?.ToString();
                                    var party_name = rdr["party_name"]?.ToString();
                  

                                    if (!string.IsNullOrEmpty(party_name) && !string.IsNullOrEmpty(party_name))
                                    {
                                        Datalist.Add(new
                                        {
                                            Doc_id = Doc_id,
                                            qty = qty,
                                            vdate = vdate,
                                            party_name = party_name                                      
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                return (new { success = true, data = Datalist });
            }
            catch (Exception ex)
            {
                return (new { success = false, message = "Error fetching attachment data", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<object> GetModificationData(int V_NO)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            var Datalist = new List<object>();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd3 = new SqlCommand("[dbo].[sp_PurchaseSauda]", con))
                    {
                        cmd3.CommandType = CommandType.StoredProcedure;
                        cmd3.Parameters.AddWithValue("@Action", "MODIFICATIONHISTORY");
                        cmd3.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd3.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd3.Parameters.AddWithValue("@V_NO", V_NO);

                        using (SqlDataReader rdr = cmd3.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var PARTY_CODE = rdr["PARTY_CODE"]?.ToString();
                                    var SaudaNo = rdr["SaudaNo"]?.ToString();
                                    var Party = rdr["Party"]?.ToString();
                                    var ItemName = rdr["ItemName"]?.ToString();
                                    var Qty = rdr["Qty"]?.ToString();
                                    var Rate = rdr["Rate"]?.ToString();
                                    var Remark = rdr["Remark"]?.ToString();
                                    var ModifyDate = rdr["ModifyDate"]?.ToString();
                          
                                    if (!string.IsNullOrEmpty(PARTY_CODE) && !string.IsNullOrEmpty(PARTY_CODE))
                                    {
                                        Datalist.Add(new
                                        {
                                            SaudaNo = SaudaNo,
                                            Party = Party,
                                            ItemName = ItemName,
                                            Qty = Qty,
                                            Rate = Rate,
                                            Remark = Remark,
                                            ModifyDate = ModifyDate
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                return (new { success = true, data = Datalist });
            }
            catch (Exception ex)
            {
                return (new { success = false, message = "Error fetching attachment data", error = ex.Message });
            }
        }

    }
}
