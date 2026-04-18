using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class CourierTrackingEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CourierTrackingEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/CourierTrackingEntryList/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetCourierTrackingEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;

            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand("sp_InsertCourierTracking", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrEmpty(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                VNo = reader["V_NO"] ?? "",
                                DocType = reader["V_TYPE"] ?? "",
                                DocNo = reader["DOC_ID"] ?? "",
                                VDate = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd") : "",
                                //PartyName = reader["PARTY_NAME"] ?? "",
                                //CityName = reader["CITY_NAME"] ?? "",
                                PartyName = reader["PARTY_NAME"] == DBNull.Value ? "" : reader["PARTY_NAME"].ToString(),
                                CityName = reader["CITY_NAME"] == DBNull.Value ? "" : reader["CITY_NAME"].ToString(),
                                CourierName = reader["COURIER_NAME"] ?? "",
                                DocketNo = reader["DOCKET_NO"] ?? "",
                                Purpose = reader["PURPOSE"] ?? "",
                                Weight = reader["WEIGHT"] != DBNull.Value ? Convert.ToDecimal(reader["WEIGHT"]) : 0,
                                Remarks = reader["REMARKS"] ?? ""
                            });
                        }

                        // ✅ Second result set: total count
                        if (reader.NextResult())
                        {
                            if (reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }
            }

            return Json(new { items = results, totalCount });
        }

        [HttpPost]
        public JsonResult Delete(string vNo, string docType)
        {
            var global = _globalVariableService.GetGlobalVariables();
            var docid = docType + vNo;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@DOC_ID", docid);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting this record.", error = ex.Message });
            }
        }


    }
}
