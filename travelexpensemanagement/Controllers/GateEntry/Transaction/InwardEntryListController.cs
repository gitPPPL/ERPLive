using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Models.GateEntry;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class InwardEntryListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly GlobalVariableService _globalVariableService;

        public InwardEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
         travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,  ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/InwardEntryList/Index.cshtml");
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
            var headerList = new List<InwardEntry_Header>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_InwardEntry", conn))
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
                            headerList.Add(new InwardEntry_Header
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["Vouchertype"] != DBNull.Value ? reader["Vouchertype"].ToString() : string.Empty,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                VtypeCode = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                TRUCK_NO = reader["Truck_no"] != DBNull.Value ? reader["Truck_no"].ToString() : string.Empty,
                                BILL_NO = reader["BILL_NO"] != DBNull.Value ? reader["BILL_NO"].ToString() : string.Empty,
                                BILL_DATE = reader["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["BILL_DATE"]) : null,
                                PARTY_NAME = reader["PartyName"] != DBNull.Value ? reader["PartyName"].ToString() : string.Empty,
                                TRANSIT_NO = reader["TRANSIT_NO"] != DBNull.Value ? Convert.ToInt32(reader["TRANSIT_NO"]) : 0,
                                WAYBILL_NO = reader["WAYBILL_NO"] != DBNull.Value ? reader["WAYBILL_NO"].ToString() : string.Empty,
                                R_DATE = reader["R_Date"] != DBNull.Value ? Convert.ToDateTime(reader["R_Date"]) : null,
                                R_TIME = !string.IsNullOrEmpty(reader["R_Time"] as string)
                                ? DateTime.TryParse(reader["R_Time"].ToString(), out DateTime dateValue) ? dateValue.ToString("HH:mm:ss") : "00:00:00"
                                : "00:00:00"

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
        public IActionResult GetDataByPendingorder(int PartyCode , string V_TYPE, DateTime V_DATE)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            var Datalist = new List<object>();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd3 = new SqlCommand("sp_InwardEntry", con))
                    {
                        cmd3.CommandType = CommandType.StoredProcedure;
                        cmd3.Parameters.AddWithValue("@Action", "PartyBillNo");
                        cmd3.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd3.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd3.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd3.Parameters.AddWithValue("@PARTY_CODE", PartyCode);
                        cmd3.Parameters.AddWithValue("@V_TYPE", V_TYPE);
                        cmd3.Parameters.Add("@V_DATE", SqlDbType.Date).Value = V_DATE;

                        using (SqlDataReader rdr = cmd3.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var ITEM_CODE = rdr["ITEM_CODE"]?.ToString();
                                    var ItemName = rdr["ITEM_NAME"]?.ToString();
                                    var UNIT_NAME = rdr["UNIT_NAME"]?.ToString();
                                    var PACKING_NOS = rdr["NOS"]?.ToString();
                                    var QTY = rdr["QTY"]?.ToString();
                                    var balqty = rdr["P_QTY"]?.ToString();
                                    var DocType = rdr["V_type"]?.ToString();
                                    var DocNo = rdr["V_NO"]?.ToString();
                                    var DocDate = rdr["Date"]?.ToString();
                                    var RATE = rdr["Rate"]?.ToString();
                                    var REMARK = rdr["Remark"]?.ToString();
                                    var DEPARTMENT = rdr["Department"]?.ToString();
                                    var DeptCode = rdr["DEPT_CODE"]?.ToString();
                                    var EMPTY_YN = "";
                                    var UOM_CODE = rdr["UNIT_CODE"]?.ToString();

                                    if (!string.IsNullOrEmpty(ITEM_CODE) && !string.IsNullOrEmpty(ITEM_CODE))
                                    {
                                        // Add anonymous object to the list
                                        Datalist.Add(new
                                        {
                                            ITEM_CODE = ITEM_CODE,
                                            ItemName = ItemName,
                                            UNIT_NAME = UNIT_NAME,
                                            PACKING_NOS = PACKING_NOS,
                                            QTY = QTY,
                                            balqty = balqty,
                                            DocType = DocType,
                                            DocNo = DocNo,
                                            DocDate = DocDate,
                                            RATE = RATE,
                                            REMARK = REMARK,
                                            DEPARTMENT = DEPARTMENT,
                                            DeptCode = DeptCode,
                                            EMPTY_YN = EMPTY_YN,
                                            UOM_CODE = UOM_CODE
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

        [HttpPost]
        public JsonResult Delete(int code, string VType)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            string Message = "";
            string Message1 = "";
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string sql = "SELECT  v_no, v_date FROM WB1  WHERE  GATE_TYPE = @VType    AND GATE_NO =@code    AND COMP_CODE = @pubCompCode" +
                    "  AND BRANCH_CODE = @pubBranchCode  AND YEAR_CODE = @pubFYearCode;";

                    using (var cmdCheck = new SqlCommand(sql, con))
                    {
                      
                        cmdCheck.Parameters.AddWithValue("@code", code);
                        cmdCheck.Parameters.AddWithValue("@VType", VType);
                        cmdCheck.Parameters.AddWithValue("@pubCompCode", getGlobalCode.PubCompCode);
                        cmdCheck.Parameters.AddWithValue("@pubBranchCode", getGlobalCode.PubBranchCode);
                        cmdCheck.Parameters.AddWithValue("@pubFYearCode", getGlobalCode.PubFYearCode);
                        using var reader = cmdCheck.ExecuteReader();

                        if (reader.Read())
                        {
                            var v_no = reader["v_no"];
                            var v_date = reader["v_date"];
                            Message = $"This document exists in Weighbridge Serial No :{v_no} dated :{v_date}";
                            return Json(new { success = false, message = Message });
                        }
                    }

                    string sql1 = @"SELECT  v_no,v_date FROM   PURCHASE2 WHERE   REF_TYPE = @RefType  AND REF_NO = @RefNo  AND COMP_CODE = @CompCode
                    AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode;";

                    using (var cmdCheck1 = new SqlCommand(sql1, con))
                    {

                        cmdCheck1.Parameters.AddWithValue("@RefNo", code);
                        cmdCheck1.Parameters.AddWithValue("@RefType", VType);
                        cmdCheck1.Parameters.AddWithValue("@CompCode", getGlobalCode.PubCompCode);
                        cmdCheck1.Parameters.AddWithValue("@BranchCode", getGlobalCode.PubBranchCode);
                        cmdCheck1.Parameters.AddWithValue("@YearCode", getGlobalCode.PubFYearCode);
                        using var reader = cmdCheck1.ExecuteReader();

                        if (reader.Read())
                        {
                            var v_no = reader["v_no"];
                            var v_date = reader["v_date"];
                            Message1 = $"This document exists in Weighbridge Serial No :{v_no} dated :{v_date}";
                            return Json(new { success = false, message = Message1 });
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", VType);
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

        [HttpPost]
        public IActionResult GetDataByCode([FromForm] int code, [FromForm] string vtype)

        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();


            InwardEntryModel wrapper = new InwardEntryModel
            {
                Header = new InwardEntry_Header(),
                Deatils = new List<Details>()

            };

            try
            {
       
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    #region Fetch Header Data
                    using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@ShowActionOption", "Header");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                wrapper.Header = new InwardEntry_Header
                                {
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    V_TIME = rdr["V_TIME"]?.ToString(),
                                    R_DATE = rdr["R_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["R_DATE"]) : DateTime.MinValue,
                                    R_TIME = rdr["R_TIME"]?.ToString(),
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_DATE = rdr["V_date"] != DBNull.Value ? Convert.ToDateTime(rdr["V_date"]) : DateTime.MinValue,
                                    V_NO = rdr["V_no"] != DBNull.Value ? Convert.ToInt32(rdr["V_no"]) : 0,
                                    BILL_NO = rdr["BILL_NO"]?.ToString(),
                                    BILL_DATE = rdr["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["BILL_DATE"]) : DateTime.MinValue,
                                    BILL_AMT = rdr["BILL_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_AMT"]) : 0,
                                    CHALL_DATE = rdr["CHALL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["CHALL_DATE"]) : DateTime.MinValue,
                                    DISP_PLAN_TYPE = rdr["DISP_PLAN_TYPE"]?.ToString(),
                                    DISP_PLAN_NO = rdr["DISP_PLAN_NO"] != DBNull.Value ? Convert.ToInt32(rdr["DISP_PLAN_NO"]) : 0,
                                    CHALL_NO = rdr["CHALL_NO"]?.ToString(),
                                    PARTY_NAME = rdr["Party_name"]?.ToString(),
                                    PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : 0,
                                    TRANSPORT_CODE = rdr["TRANSPORT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSPORT_CODE"]) : 0,
                                    T_name = rdr["T_name"]?.ToString(),
                                    DRIVER_NAME = rdr["DRIVER_NAME"]?.ToString(),
                                    DRIVER_NO = rdr["DRIVER_NO"]?.ToString(),
                                    DL_NO = rdr["DL_NO"]?.ToString(),
                                    RC_NO = rdr["RC_NO"]?.ToString(),
                                    INSU_NO = rdr["INSU_NO"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    Add1 = rdr["Add1"]?.ToString(),
                                    Add2 = rdr["Add2"]?.ToString(),
                                    Add3 = rdr["Add2"]?.ToString(),
                                    PAN_NO = rdr["PAN_NO"]?.ToString(),
                                    Remarks2 = rdr["Remarks2"]?.ToString(),

                                    PARTY_CITY = rdr["PARTY_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CITY"]) : 0,
                                    City = rdr["City"]?.ToString(),
                                    State = rdr["State"]?.ToString(),
                                    PARTY_GST = rdr["PARTY_GST"]?.ToString(),
                                    ShipAddress = rdr["ShipAddress"]?.ToString(),
                                    PARTY_PINCODE = rdr["PARTY_PINCODE"]?.ToString(),
                                    PARTY_ADDRESSID = rdr["PARTY_ADDRESSID"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_ADDRESSID"]) : 0,
                                    TRANSIT_NO = rdr["TRANSIT_NO"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSIT_NO"]) : 0,
                                    WAYBILL_NO = rdr["WAYBILL_NO"]?.ToString(),
                                    TRUCK_NO = rdr["TRUCK_NO"]?.ToString(),
                                    OUT_DATE = rdr["Out_Date"] != DBNull.Value ? Convert.ToDateTime(rdr["Out_Date"]) : DateTime.MinValue,
                                    OUT_TIME = rdr["OUT_TIME"]?.ToString(),
                                    INSU_EXPDT = rdr["INSU_EXPDT"] != DBNull.Value ? Convert.ToDateTime(rdr["INSU_EXPDT"]) : DateTime.MinValue,
                                    DL_EXPDT = rdr["DL_EXPDT"] != DBNull.Value ? Convert.ToDateTime(rdr["DL_EXPDT"]) : DateTime.MinValue,
                                    CONTAINER_NO = rdr["CONTAINER_NO"]?.ToString(),
                                    CONTAINER_SIZE = rdr["CONTAINER_SIZE"]?.ToString(),
                                    ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
                                    FAPROV_STATUS = rdr["FAPROV_STATUS"]?.ToString(),
                                    SHIP_PARTY = rdr["SHIP_PARTY"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_PARTY"]) : 0,
                                    SHIP_BILLNO = rdr["SHIP_BILLNO"]?.ToString(),
                                    SHIP_BILLDATE = rdr["SHIP_BILLDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["SHIP_BILLDATE"]) : DateTime.MinValue,
                                    EWB_DATE = rdr["EWB_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EWB_DATE"]) : DateTime.MinValue,
                                    EWB_EXPDATE = rdr["EWB_EXPDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EWB_EXPDATE"]) : DateTime.MinValue,
                                    EWB_INVNO = rdr["EWB_INVNO"]?.ToString(),
                                    EWB_INVAMT = rdr["EWB_INVAMT"] != DBNull.Value ? Convert.ToInt32(rdr["EWB_INVAMT"]) : 0,
                                    PARTY_WBSLIPNO = rdr["PARTY_WBSLIPNO"]?.ToString(),
                                    RETURN_TYPE = rdr["RETURN_TYPE"]?.ToString(),
                                    PARTY_WBGRWT = rdr["PARTY_WBGRWT"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_WBGRWT"]) : 0,
                                    PARTY_WBTRWT = rdr["PARTY_WBTRWT"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_WBTRWT"]) : 0,
                                    PARTY_WBTIME = rdr["PARTY_WBTIME"] != DBNull.Value ? Convert.ToDateTime(rdr["PARTY_WBTIME"]) : DateTime.MinValue,
                                    PARTY_EWBCITY = rdr["PARTY_EWBCITY"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_EWBCITY"]) : 0,
                                    GR_NO = rdr["GR_NO"]?.ToString(),
                                    GR_DATE = rdr["GR_Date"] != DBNull.Value ? Convert.ToDateTime(rdr["GR_Date"]) : DateTime.MinValue,
                                    STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : 0



                                };
                            }
                        }
                    }
                    #endregion


                    #region Fetch Dispatch Data
                    using (SqlCommand cmd4 = new SqlCommand("sp_InwardEntry", con))
                    {
                        cmd4.CommandType = CommandType.StoredProcedure;
                        cmd4.Parameters.AddWithValue("@Action", "ShowData");
                        cmd4.Parameters.AddWithValue("@ShowActionOption", "Details");
                        cmd4.Parameters.AddWithValue("@V_NO", code);
                        cmd4.Parameters.AddWithValue("@V_TYPE", vtype);
                        cmd4.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd4.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd4.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);


                        using (SqlDataReader rdr = cmd4.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Deatils.Add(new Details
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    ITEM_NAME = rdr["Item_Name"]?.ToString(),
                                    Department = rdr["Department"]?.ToString(),
                                    DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                                    Unit = rdr["Unit"]?.ToString(),
                                    NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                                    QTY = rdr["QTY"] != DBNull.Value ? Convert.ToInt32(rdr["QTY"]) : 0,
                                    SHIP_RATE = rdr["SHIP_RATE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_RATE"]) : 0,
                                    EMPTY = rdr["EMPTY"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    REF_TYPE = rdr["REF_TYPE"]?.ToString(),
                                    REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0,
                                    UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0


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
                    Details = wrapper.Deatils

                };

                return Json(new { success = true, data = resultWrapper });
            }
            catch (Exception ex)
            {
                // Handle any errors and return them in the JSON response
                return Json(new { success = false, message = "Error fetching purchase requisition data", error = ex.Message });
            }
        }

        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<InwardEntryDetailDto> docDetails = new List<InwardEntryDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn))
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

                using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn))
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

                using (SqlCommand cmd = new SqlCommand("sp_InwardEntry", conn))
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

                        return File( stream.ToArray(), "application/pdf", "GateInward.pdf" );
                    }
                }
            }
        }

    }
}
