using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class IncommingQCRMListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public IncommingQCRMListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
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
            return View("~/Views/QualityControl/Transaction/IncommingQCRMList/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetQCIncommingQCEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                using (var con = _dbConnection.GetErpConnection())
                //using (var cmd = new SqlCommand("usp_InsertQC1PreIncommingQCRM", con))
                using (var cmd = new SqlCommand("usp_InsertQC1IncommingQCRM", con))
                //usp_InsertQC1IncommingQCRMList
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Required parameters
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@V_TYPE", DBNull.Value);

                    // Paging + search
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                SearchCode = reader["SearchCode"]?.ToString(),
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                DocTypeName = reader["DocTypeName"]?.ToString(),
                                V_NO = reader["V_NO"]?.ToString(),
                                V_DATE = reader["V_DATE"]?.ToString(),
                                MRN_NO = reader["MRN_NO"]?.ToString(),
                                MRN_TYPE = reader["MRN_TYPE"]?.ToString(),
                                MRNDate = reader["MRNDate"]?.ToString(),
                                BALES = reader["BALES"]?.ToString(),
                                PARTY_CODE = reader["PARTY_CODE"]?.ToString(),
                                PartyName = reader["PartyName"]?.ToString(),
                                BillNo = reader["BillNo"]?.ToString(),
                                BillDate = reader["BillDate"]?.ToString(),
                                TransportName = reader["TransportName"]?.ToString(),
                                TruckNo = reader["TruckNo"]?.ToString(),
                                INV_QTY = reader["INV_QTY"]?.ToString(),
                                RECD_QTY = reader["RECD_QTY"]?.ToString(),
                                SHORT_QTY = reader["SHORT_QTY"]?.ToString(),
                                REMARKS = reader["REMARKS"]?.ToString(),
                                DEDUCT_AMT = reader["DEDUCT_AMT"]?.ToString(),
                                DEDUCT_NARR = reader["DEDUCT_NARR"]?.ToString(),
                                PUR_TYPE = reader["PUR_TYPE"]?.ToString(),
                                WASTE_WGT = reader["WASTE_WGT"]?.ToString(),
                                STATUS = reader["STATUS"]?.ToString()
                            });
                        }
                        // Read total count (2nd resultset)
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader.GetInt32(0);
                        }
                    }
                }
                return Json(new { items = results, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while fetching the QC Temperature Entry List.",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult Delete(int vNo, string docType)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("usp_InsertQC1IncommingQCRM", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", docType);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new
                {
                    status = true,
                    message = "Record deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

    }
}

