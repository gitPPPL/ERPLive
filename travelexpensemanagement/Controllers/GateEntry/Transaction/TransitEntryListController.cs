using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;



namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TransitEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public TransitEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
DropdownService dropdownService, DbHelper dbHelper,
ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;

        }


        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/TransitEntryList/Index.cshtml");
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
            var headerList = new List<TransitEntryModel>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_TransitEntry", conn))
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
                            headerList.Add(new TransitEntryModel
                            {
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                Doctype_Name = reader["doctype"] != DBNull.Value ? reader["doctype"].ToString() : string.Empty,
                                FORM_NO = reader["FORM_NO"] != DBNull.Value ? reader["FORM_NO"].ToString() : string.Empty,
                                FORM_DATE = reader["FORM_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["FORM_DATE"]) : DateTime.MinValue,
                                EXPIRY_DATE = reader["EXPIRY_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EXPIRY_DATE"]) : DateTime.MinValue,
                                partyname = reader["PARTY_NAME"] != DBNull.Value ? reader["PARTY_NAME"].ToString() : string.Empty,
                                PARTY_CODE = reader["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARTY_CODE"]) : 0,
                                PARTY_GSTIN = reader["PARTY_GSTIN"] != DBNull.Value ? reader["PARTY_GSTIN"].ToString() : string.Empty,
                                OTHER_GSTIN = reader["OTHER_GSTIN"] != DBNull.Value ? reader["OTHER_GSTIN"].ToString() : string.Empty,
                                NOS = reader["NOS"] != DBNull.Value ? Convert.ToInt32(reader["NOS"]) : 0,
                                BILL_NO = reader["BILL_NO"] != DBNull.Value ? reader["BILL_NO"].ToString() : string.Empty,
                                BILL_DATE = reader["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["BILL_DATE"]) : DateTime.MinValue,
                                GR_NO = reader["GR_NO"] != DBNull.Value ? reader["GR_NO"].ToString() : string.Empty,
                                GR_DATE = reader["GR_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["GR_DATE"]) : DateTime.MinValue,
                                TRUCK_NO = reader["TRUCK_NO"] != DBNull.Value ? reader["TRUCK_NO"].ToString() : string.Empty,
                                TRANSPORT = reader["TRANSPORT"] != DBNull.Value ? reader["TRANSPORT"].ToString() : string.Empty,
                                ORD_TYPE = reader["ORD_TYPE"] != DBNull.Value ? reader["ORD_TYPE"].ToString() : string.Empty,
                                ORD_NO = reader["ORD_NO"] != DBNull.Value ? Convert.ToInt32(reader["ORD_NO"]) : 0,
                                HSN_CODE = reader["HSN_CODE"] != DBNull.Value ? Convert.ToInt32(reader["HSN_CODE"]) : 0,
                                ITEM_DESC = reader["ITEM_DESC"] != DBNull.Value ? reader["ITEM_DESC"].ToString() : string.Empty,
                                BILL_AMT = reader["BILL_AMT"] != DBNull.Value ? Convert.ToInt32(reader["BILL_AMT"]) : 0,
                                SGST_AMT = reader["SGST_AMT"] != DBNull.Value ? Convert.ToInt32(reader["SGST_AMT"]) : 0,
                                CGST_AMT = reader["CGST_AMT"] != DBNull.Value ? Convert.ToInt32(reader["CGST_AMT"]) : 0,
                                IGST_AMT = reader["IGST_AMT"] != DBNull.Value ? Convert.ToInt32(reader["IGST_AMT"]) : 0,
                                CESS_AMT = reader["CESS_AMT"] != DBNull.Value ? Convert.ToInt32(reader["CESS_AMT"]) : 0,
                                CESS_NONADVOLAMT = reader["CESS_NONADVOLAMT"] != DBNull.Value ? Convert.ToInt32(reader["CESS_NONADVOLAMT"]) : 0,
                                OTHER_AMT = reader["OTHER_AMT"] != DBNull.Value ? Convert.ToInt32(reader["OTHER_AMT"]) : 0,
                                TOTAL_AMT = reader["TOTAL_AMT"] != DBNull.Value ? Convert.ToInt32(reader["TOTAL_AMT"]) : 0,
                                UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue
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

        public IActionResult GetDataByID(int code , string vtype)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            TransitEntryModel TransitEntryModel = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TransitEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);

                     


                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                TransitEntryModel = new TransitEntryModel
                                {

                                    V_TYPE = rdr["V_TYPE"] != DBNull.Value ? rdr["V_TYPE"].ToString() : null,
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    PARTY_CODE = rdr["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PARTY_CODE"]) : 0,
                                    partyname  = rdr["PARTY_NAME"] != DBNull.Value ? rdr["PARTY_NAME"].ToString() : null,
                                    PARTY_GSTIN  = rdr["PARTY_GSTIN"] != DBNull.Value ? rdr["PARTY_GSTIN"].ToString() : null,
                                    BILL_NO = rdr["BILL_NO"] != DBNull.Value ? rdr["BILL_NO"].ToString() : null,
                                    BILL_DATE = rdr["BILL_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["BILL_DATE"]) : DateTime.MinValue,
                                    BILL_AMT = rdr["BILL_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["BILL_AMT"]) : 0,
                                    HSN_CODE = rdr["HSN_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["HSN_CODE"]) : 0,
                                    OTHER_GSTIN = rdr["OTHER_GSTIN"] != DBNull.Value ? rdr["OTHER_GSTIN"].ToString() : null,
                                    NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                                    ITEM_DESC   = rdr["ITEM_DESC"] != DBNull.Value ? rdr["ITEM_DESC"].ToString() : null,
                                    SGST_AMT = rdr["SGST_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["SGST_AMT"]) : 0,
                                    CGST_AMT = rdr["CGST_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["CGST_AMT"]) : 0,
                                    CESS_AMT = rdr["CESS_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["CESS_AMT"]) : 0,
                                    CESS_NONADVOLAMT = rdr["CESS_NONADVOLAMT"] != DBNull.Value ? Convert.ToInt32(rdr["CESS_NONADVOLAMT"]) : 0,
                                    GR_NO = rdr["GR_NO"] != DBNull.Value ? rdr["GR_NO"].ToString() : null,
                                    GR_DATE = rdr["GR_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["GR_DATE"]) : DateTime.MinValue,
                                    TRUCK_NO = rdr["TRUCK_NO"] != DBNull.Value ? rdr["TRUCK_NO"].ToString() : null,
                                    FORM_NO = rdr["FORM_NO"] != DBNull.Value ? rdr["FORM_NO"].ToString() : null,
                                    FORM_DATE = rdr["FORM_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["FORM_DATE"]) : DateTime.MinValue,
                                    EXPIRY_DATE = rdr["EXPIRY_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EXPIRY_DATE"]) : DateTime.MinValue,
                                    ORD_NO = rdr["ORD_NO"] != DBNull.Value ? Convert.ToInt32(rdr["ORD_NO"]) : 0,
                                    IGST_AMT = rdr["IGST_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["IGST_AMT"]) : 0,
                                    OTHER_AMT = rdr["OTHER_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["OTHER_AMT"]) : 0,
                                    TOTAL_AMT = rdr["TOTAL_AMT"] != DBNull.Value ? Convert.ToInt32(rdr["TOTAL_AMT"]) : 0,
                                    STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : 0,
                                    TRANSPORT = rdr["TRANSPORT"] != DBNull.Value ? rdr["TRANSPORT"].ToString() : null,
                                    GATE_NO =  rdr["ORD_NO"] != DBNull.Value ? Convert.ToInt32(rdr["ORD_NO"]) : 0,
                                    GATE_DATE = rdr["FORM_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["FORM_DATE"]) : DateTime.MinValue,

                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = TransitEntryModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }






        [HttpPost]
        public JsonResult Delete(int code, string VType)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TransitEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", VType);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Transit Entry deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Transit Entry .", error = ex.Message });
            }
        }




    }
}
