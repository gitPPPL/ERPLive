using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;


namespace travelexpensemanagement.Controllers.Sales.Transaction
{

    [SessionAuthorize]

    public class SalesProformaInvoiceListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;


        public SalesProformaInvoiceListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;

        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesProformaInvoiceList/Index.cshtml");
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
            var headerList = new List<SalesProformaInvoice_Header>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_SalesProformaInvoice", conn))
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
                            headerList.Add(new SalesProformaInvoice_Header
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["VoucherName"] != DBNull.Value ? reader["VoucherName"].ToString() : string.Empty,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_DATE = reader["V_DATE"] != DBNull.Value  ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                BILL_NAME = reader["BILL_NAME"] != DBNull.Value ? reader["BILL_NAME"].ToString() : string.Empty,
                    
                                BILL_ADD1 = reader["BILL_ADD1"] != DBNull.Value ? reader["BILL_ADD1"].ToString() : string.Empty,
                                BILL_ADD2 = reader["BILL_ADD2"] != DBNull.Value ? reader["BILL_ADD2"].ToString() : string.Empty,
                                BILL_ADD3 = reader["BILL_ADD3"] != DBNull.Value ? reader["BILL_ADD3"].ToString() : string.Empty,
                                BILL_CITYName = reader["b_CITY"] != DBNull.Value ? reader["b_CITY"].ToString() : string.Empty,
                                AGENT_Name = reader["AGENT"] != DBNull.Value ? reader["AGENT"].ToString() : string.Empty,
                                SHIP_NAME = reader["SHIP_NAME"] != DBNull.Value ? reader["SHIP_NAME"].ToString() : string.Empty,
                                SHIP_ADD1 = reader["SHIP_ADD1"] != DBNull.Value ? reader["SHIP_ADD1"].ToString() : string.Empty,
                                SHIP_ADD2 = reader["SHIP_ADD2"] != DBNull.Value ? reader["SHIP_ADD2"].ToString() : string.Empty,
                                SHIP_ADD3 = reader["SHIP_ADD3"] != DBNull.Value ? reader["SHIP_ADD3"].ToString() : string.Empty,
                                statename = reader["S_NAME"] != DBNull.Value ? reader["S_NAME"].ToString() : string.Empty,
                                TAX_Name = reader["TAX_NAME"] != DBNull.Value ? reader["TAX_NAME"].ToString() : string.Empty,
                                ITEM_TYPE = reader["ITEM_TYPE"] != DBNull.Value ? reader["ITEM_TYPE"].ToString() : string.Empty,
                                WB_NO = reader["WB_NO"] != DBNull.Value ? Convert.ToInt32(reader["WB_NO"]) : 0,
                                PACK_NO = reader["PACK_NO"] != DBNull.Value ? Convert.ToInt32(reader["PACK_NO"]) : 0,
                                AMOUNT = reader["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["AMOUNT"]) : 0,
                                PACK_PER = reader["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(reader["PACK_PER"]) : 0,
                                PACK_AMT = reader["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["PACK_AMT"]) : 0,
                                TOT_NET = reader["TOT_NET"] != DBNull.Value ? Convert.ToDecimal(reader["TOT_NET"]) : 0,
                                CGST_PER = reader["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_PER"]) : 0,
                                CGST_AMT = reader["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_AMT"]) : 0,
                                SGST_PER = reader["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_PER"]) : 0,
                                SGST_AMT = reader["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_AMT"]) : 0,
                                IGST_PER = reader["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_PER"]) : 0,
                                IGST_AMT = reader["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_AMT"]) : 0,
                                CESS_PER = reader["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CESS_PER"]) : 0,
                                LOAD_PER = reader["LOAD_PER"] != DBNull.Value ? Convert.ToDecimal(reader["LOAD_PER"]) : 0,
                                LOAD_AMT = reader["LOAD_PER"] != DBNull.Value ? Convert.ToDecimal(reader["LOAD_AMT"]) : 0,
                                LOAD_AC = reader["LOAD_AC"] != DBNull.Value ? reader["LOAD_AC"].ToString() : string.Empty,
                                WB_AMT = reader["WB_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["WB_AMT"]) : 0,
                                WB_AC = reader["LOAD_AC"] != DBNull.Value ? reader["LOAD_AC"].ToString() : string.Empty,
                                FRT_AMT = reader["FRT_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["FRT_AMT"]) : 0,
                                ROUND_OFF = reader["ROUND_OFF"] != DBNull.Value ? Convert.ToDecimal(reader["ROUND_OFF"]) : 0,
                                NAMOUNT = reader["NAMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["NAMOUNT"]) : 0,
                                INSU_PER = reader["INSU_PER"] != DBNull.Value ? Convert.ToDecimal(reader["INSU_PER"]) : 0,
                                INSU_AMT = reader["INSU_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["INSU_AMT"]) : 0,
                                TDS_PER = reader["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["TDS_PER"]) : 0,
                                LOAD_REM = reader["LOAD_REM"] != DBNull.Value ? reader["LOAD_REM"].ToString() : string.Empty,
                                WB_REM = reader["WB_REM"] != DBNull.Value ? reader["WB_REM"].ToString() : string.Empty,
                                TOT_GROSS = reader["TOT_GROSS"] != DBNull.Value ? Convert.ToDecimal(reader["TOT_GROSS"]) : 0,
                                GR_NO = reader["GR_NO"] != DBNull.Value ? reader["GR_NO"].ToString() : string.Empty,
                                GR_DATE = reader["GR_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["GR_DATE"])  : DateTime.MinValue,
                                VEHICLE_NO = reader["VEHICLE_NO"] != DBNull.Value ? reader["VEHICLE_NO"].ToString() : string.Empty,
                                TRANSPORT_NAME = reader["TRANSPORT_NAME"] != DBNull.Value ? reader["TRANSPORT_NAME"].ToString() : string.Empty,
                                DRIVER_NO = reader["DRIVER_NO"] != DBNull.Value ? reader["DRIVER_NO"].ToString() : string.Empty,
                                WAYBILL_NO = reader["WAYBILL_NO"] != DBNull.Value ? reader["WAYBILL_NO"].ToString() : string.Empty,
                                FRT_TOPAY = reader["FRT_TOPAY"] != DBNull.Value ? Convert.ToDecimal(reader["FRT_TOPAY"]) : 0,
                                WB_QTY = reader["FRT_TOPAY"] != DBNull.Value ? Convert.ToDecimal(reader["FRT_TOPAY"]) : 0,
                                DISC_PER = reader["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_PER"]) : 0,
                                DISC_AMT = reader["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DISC_AMT"]) : 0,
                                TRANSPORT_CODE = reader["TRANSPORT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["TRANSPORT_CODE"]) : 0

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
        public IActionResult GetDataByCode( [FromForm] string DOC_ID)

        
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();


            SalesProformaInvoice_Model wrapper = new SalesProformaInvoice_Model
            {
                Header = new SalesProformaInvoice_Header(),
                Details = new List<SalesProformaInvoice_Detail>()
            };

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    #region Fetch Header Data
                    using (SqlCommand cmd = new SqlCommand("sp_SalesProformaInvoice", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@ShowActionOption", "Header");
                        cmd.Parameters.AddWithValue("@DOC_ID", DOC_ID);                   
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);


                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                wrapper.Header = new SalesProformaInvoice_Header
                                {
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_DATE = rdr["V_date"] != DBNull.Value ? Convert.ToDateTime(rdr["V_date"]) : DateTime.MinValue,
                                    V_NO = rdr["V_no"] != DBNull.Value ? Convert.ToInt32(rdr["V_no"]) : 0,
                                    SHIP_TYPE = rdr["SHIP_TYPE"]?.ToString(),
                                    BILL_CODE = rdr["BILL_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_CODE"]) : 0,
                                    BILL_NAME = rdr["BILL_NAME"]?.ToString(),
                                    BILL_ADD1 = rdr["BILL_ADD1"]?.ToString(),
                                    BILL_ADD2 = rdr["BILL_ADD2"]?.ToString(),
                                    BILL_ADD3 = rdr["BILL_ADD3"]?.ToString(),
                                    BILL_CITY = rdr["BILL_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_CITY"]) : 0,
                                    BILL_STATE = rdr["BILL_STATE"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_STATE"]) : 0,
                                    BILL_COUNTRY = rdr["BILL_COUNTRY"]?.ToString(),
                                    BILL_PINCODE = rdr["BILL_PINCODE"]?.ToString(),
                                    BILL_GST = rdr["BILL_GST"]?.ToString(),
                                    AGENT_CODE = rdr["AGENT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["AGENT_CODE"]) : 0,
                                    SHIP_CODE = rdr["SHIP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_CODE"]) : 0,
                                    SHIP_ADD1 = rdr["SHIP_ADD1"]?.ToString(),
                                    SHIP_ADD2 = rdr["SHIP_ADD2"]?.ToString(),
                                    SHIP_ADD3 = rdr["SHIP_ADD3"]?.ToString(),
                                    SHIP_CITY = rdr["SHIP_CITY"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_CITY"]) : 0,
                                    SHIP_STATE = rdr["SHIP_STATE"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_STATE"]) : 0,
                                    SHIP_COUNTRY = rdr["SHIP_COUNTRY"] != DBNull.Value ? Convert.ToInt32(rdr["SHIP_COUNTRY"]) : 0,
                                    SHIP_PINCODE = rdr["SHIP_PINCODE"]?.ToString(),
                                    SHIP_GST = rdr["SHIP_GST"]?.ToString(),
                                    IMPORT_CURRENCY = rdr["IMPORT_CURRENCY"]?.ToString(),
                                    CURRENCY = rdr["CURRENCY"]?.ToString(),
                                    EXRATE = rdr["EXRATE"] != DBNull.Value ? Convert.ToDecimal(rdr["EXRATE"]) : 0,
                                    TAX_CODE = rdr["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TAX_CODE"]) : 0,
                                    ITEM_TYPE = rdr["ITEM_TYPE"]?.ToString(),
                                    SUPPLY_TYPE = rdr["SUPPLY_TYPE"]?.ToString(),
                                    WB_NO = rdr["WB_NO"] != DBNull.Value ? Convert.ToInt32(rdr["WB_NO"]) : 0,
                                    PACK_TYPE = rdr["PACK_TYPE"]?.ToString(),
                                    PACK_NO = rdr["PACK_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PACK_NO"]) : 0,
                                    AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                                    PACK_PER = rdr["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_PER"]) : 0,
                                    PACK_AMT = rdr["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_AMT"]) : 0,
                                    TCS_PER = rdr["TCS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["TCS_PER"]) : 0,
                                    TCS_AMT = rdr["TCS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["TCS_AMT"]) : 0,
                                    NAMOUNT = rdr["NAMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["NAMOUNT"]) : 0,
                                    CGST_PER = rdr["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_PER"]) : 0,
                                    CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_AMT"]) : 0,
                                    SGST_PER = rdr["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_PER"]) : 0,
                                    SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_AMT"]) : 0,
                                    IGST_PER = rdr["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_PER"]) : 0,
                                    IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_AMT"]) : 0,
                                    CESS_PER = rdr["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_PER"]) : 0,
                                    CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_AMT"]) : 0,
                                    LOAD_PER = rdr["LOAD_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["LOAD_PER"]) : 0,
                                    LOAD_AMT = rdr["LOAD_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["LOAD_AMT"]) : 0,
                                    LOAD_AC = rdr["LOAD_AC"]?.ToString(),
                                    WB_AMT = rdr["WB_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_AMT"]) : 0,
                                    WB_AC = rdr["WB_AC"]?.ToString(),
                                    TOT_NOS = rdr["TOT_NOS"] != DBNull.Value ? Convert.ToInt32(rdr["TOT_NOS"]) : 0,
                                    TOT_NET = rdr["TOT_NET"] != DBNull.Value ? Convert.ToDecimal(rdr["TOT_NET"]) : 0,
                                    FRT_AMT = rdr["FRT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["FRT_AMT"]) : 0,
                                    ROUND_OFF = rdr["ROUND_OFF"] != DBNull.Value ? Convert.ToDecimal(rdr["ROUND_OFF"]) : 0,
                                    INSU_PER = rdr["INSU_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["INSU_PER"]) : 0,
                                    INSU_AMT = rdr["INSU_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["INSU_AMT"]) : 0,
                                    TDS_PER = rdr["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["TDS_PER"]) : 0,
                                    LOAD_REM = rdr["LOAD_REM"]?.ToString(),
                                    WB_REM = rdr["WB_REM"]?.ToString(),
                                    PAY_TERM = rdr["PAY_TERM"] != DBNull.Value ? Convert.ToInt32(rdr["PAY_TERM"]) : 0,
                                    TOT_GROSS = rdr["TOT_GROSS"] != DBNull.Value ? Convert.ToDecimal(rdr["TOT_GROSS"]) : 0,
                                    GR_NO = rdr["GR_NO"]?.ToString(),                               
                                    GR_DATE = rdr["GR_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["GR_DATE"]) : DateTime.MinValue,
                                    VEHICLE_NO = rdr["VEHICLE_NO"]?.ToString(),
                                    TRANSPORT_CODE = rdr["TRANSPORT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TRANSPORT_CODE"]) : 0,
                                    DRIVER_NAME = rdr["DRIVER_NAME"]?.ToString(),
                                    DRIVER_NO = rdr["DRIVER_NO"]?.ToString(),
                                    WAYBILL_NO = rdr["WAYBILL_NO"]?.ToString(),
                                    FRT_TOPAY = rdr["FRT_TOPAY"] != DBNull.Value ? Convert.ToDecimal(rdr["FRT_TOPAY"]) : 0,
                                    SOLD_BY = rdr["SOLD_BY"] != DBNull.Value ? Convert.ToInt32(rdr["SOLD_BY"]) : 0,
                                    BUYER_ORDNO = rdr["BUYER_ORDNO"]?.ToString(),
                                    WB_QTY = rdr["WB_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_QTY"]) : 0,
                                    DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : 0,
                                    DISC_AMT = rdr["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_AMT"]) : 0,
                                     FINAL_DEST = rdr["FINAL_DEST"]?.ToString(),
                                    FINAL_DEST_COUNTRY = rdr["FINAL_DEST_COUNTRY"]?.ToString(),
                                    DELIVERY_TERMS = rdr["DELIVERY_TERMS"]?.ToString(),
                                    LUT_DETAIL = rdr["LUT_DETAIL"]?.ToString(),
                                    INSU_DETAIL = rdr["INSU_DETAIL"]?.ToString(),
                                    SAUDA_TYPE = rdr["SAUDA_TYPE"]?.ToString(),
                                    SAUDA_NO = rdr["SAUDA_NO"] != DBNull.Value ? Convert.ToInt32(rdr["SAUDA_NO"]) : 0,
                                    SAUDA_RATE = rdr["SAUDA_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["SAUDA_RATE"]) : 0,
                                    TRADE_TERM = rdr["TRADE_TERM"]?.ToString(),
                                    SHIPMENT_TYPE = rdr["SHIPMENT_TYPE"]?.ToString(),
                                    MODEOF_PAYMENT = rdr["MODEOF_PAYMENT"]?.ToString(),
                                    DEL_SCH = rdr["DEL_SCH"]?.ToString(),
                                    CONTAINER_SIZE = rdr["CONTAINER_SIZE"]?.ToString(),
                                    WB_TYPE = rdr["WB_TYPE"]?.ToString(),
                                    STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : 0,
                                    PORT_LOADING = rdr["PORT_LOADING"]?.ToString(),
                                    PORT_DISCHARGE = rdr["PORT_DISCHARGE"]?.ToString()  ,
                                    REMARK = rdr["REMARK"]?.ToString()                            

                                };
                            }
                        }
                    }
                    #endregion

                    #region Fetch Dispatch Data
                    using (SqlCommand cmd4 = new SqlCommand("sp_SalesProformaInvoice", con))
                    {
                        cmd4.CommandType = CommandType.StoredProcedure;
                        cmd4.Parameters.AddWithValue("@Action", "ShowData");
                        cmd4.Parameters.AddWithValue("@ShowActionOption", "Details");
                        cmd4.Parameters.AddWithValue("@DOC_ID", DOC_ID);
                        cmd4.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd4.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd4.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);


                        using (SqlDataReader rdr = cmd4.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Details.Add(new SalesProformaInvoice_Detail
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    PROD_DESC = rdr["PROD_DESC"]?.ToString(),
                                    HSN_CODE = rdr["HSN_CODE"]?.ToString(),
                                    NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                                    GROSS_QTY = rdr["GROSS_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["GROSS_QTY"]) : 0,
                                    QTY = rdr["QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY"]) : 0,
                                    FOR_RATE = rdr["FOR_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["FOR_RATE"]) : 0,
                                    RATE = rdr["RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["RATE"]) : 0,
                                    AMOUNT = rdr["AMOUNT"] != DBNull.Value ? Convert.ToDecimal(rdr["AMOUNT"]) : 0,
                                    PACK_PER = rdr["PACK_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_PER"]) : 0,
                                    PACK_AMT = rdr["PACK_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["PACK_AMT"]) : 0,
                                    DISC_PER = rdr["DISC_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_PER"]) : 0,
                                    DISC_AMT = rdr["DISC_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DISC_AMT"]) : 0,
                                    FREIGHT_AMT = rdr["FREIGHT_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["FREIGHT_AMT"]) : 0,
                                    CGST_PER = rdr["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_PER"]) : 0,
                                    CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CGST_AMT"]) : 0,
                                    SGST_PER = rdr["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_PER"]) : 0,
                                    SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["SGST_AMT"]) : 0,
                                    IGST_PER = rdr["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_PER"]) : 0,
                                    IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["IGST_AMT"]) : 0,
                                    CESS_PER = rdr["CESS_PER"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_PER"]) : 0,
                                    CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["CESS_AMT"]) : 0,
                                    REMARK = rdr["REMARK"]?.ToString(),
                                    PACK_NO = rdr["PACK_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PACK_NO"]) : 0,
                                    SAUDA_TYPE = rdr["SAUDA_TYPE"]?.ToString(),
                                    SAUDA_NO = rdr["SAUDA_NO"] != DBNull.Value ? Convert.ToInt32(rdr["SAUDA_NO"]) : 0,
                                    SAUDA_RATE = rdr["SAUDA_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["SAUDA_RATE"]) : 0,
                                    ORD_TYPE = rdr["ORD_TYPE"]?.ToString(),
                                    ORD_NO = rdr["ORD_NO"] != DBNull.Value ? Convert.ToInt32(rdr["ORD_NO"]) : 0,
                                    ORD_RATE = rdr["ORD_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["ORD_RATE"]) : 0,
                                    TAX_CODE = rdr["TAX_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["TAX_CODE"]) : 0                        

                                });
                            }
                        }
                    }
                    #endregion
                }

                 var resultWrapper = new {  Header = wrapper.Header, Details = wrapper.Details };

                return Json(new { success = true, data = resultWrapper });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "Error fetching purchase requisition data", error = ex.Message });
            }
        }






    }
}
