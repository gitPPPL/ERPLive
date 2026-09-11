using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;


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

    }
}
