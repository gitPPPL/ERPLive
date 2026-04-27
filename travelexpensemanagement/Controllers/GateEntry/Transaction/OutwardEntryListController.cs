using iTextSharp.text;
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
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
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

            if (getvariabledata == null)
            {
                return Json(new { success = false, message = "Global variable data is null." });
            }

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
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        // First result set: Header data
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

                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data.", error = ex.Message });
            }

            return Json(new { success = true, headers = headerList, details = detailsList, totalCount });
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



    }
}

      
