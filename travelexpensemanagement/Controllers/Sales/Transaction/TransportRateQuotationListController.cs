using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Sale;
using static travelexpensemanagement.Models.Sale.Sale_TransportRateQuatation_Model;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class TransportRateQuotationListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _Dbhelper;
        public TransportRateQuotationListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _Dbhelper = dbHelper;

        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/TransportRateQuotationList/Index.cshtml");
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            if (getvariabledata == null)
            {
                return Json(new { success = false, message = "Global variable data is null." });
            }

            int totalCount = 0;
            var headerList = new List<sale_TransportRate_Header>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_TransportRateQuatation", conn))
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
                            headerList.Add(new sale_TransportRate_Header
                            {
                                DO_NO = reader["DO_NO"] != DBNull.Value ? Convert.ToInt32(reader["DO_NO"]) : 0,
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                TransportName = reader["BILL_NAME"] != DBNull.Value ? reader["BILL_NAME"].ToString() : string.Empty,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                             
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

        [HttpDelete]
        public JsonResult Delete(int code)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TransportRateQuatation", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "TRQT");
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Transport Rate Quotation deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Transport Rate Quotation.", error = ex.Message });
            }
        }


        [HttpGet]

        public async Task<IActionResult> GetDataByCode(string code)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();

            var HeaderDetails = new Dictionary<string, object>
                    {
                        {"@Action","ShowData" },
                        {"@searchOption","Header" },
                        {"@DOC_ID", code },
                        {"@COMP_CODE", GetGlobalCode.PubCompCode },
                        {"@BRANCH_CODE", GetGlobalCode.PubBranchCode },
                        {"@YEAR_CODE", GetGlobalCode.PubFYearCode }
                    };

            var HeaderdataList = await _Dbhelper.GetJsonFromProcedureAsync("sp_TransportRateQuatation", HeaderDetails);

            var DetailsList = new Dictionary<string, object>
                    {
                        {"@Action","ShowData" },
                        {"@searchOption","Details" },
                        {"@DOC_ID", code },
                        {"@COMP_CODE", GetGlobalCode.PubCompCode },
                        {"@BRANCH_CODE", GetGlobalCode.PubBranchCode },
                        {"@YEAR_CODE", GetGlobalCode.PubFYearCode }
                    };

            var DetailList = await _Dbhelper.GetJsonFromProcedureAsync("sp_TransportRateQuatation", DetailsList);

            var resultWrapper = new
            {
                Header = HeaderdataList,
                Details = DetailList

            };

            return Json(new { success = true, data = resultWrapper  });

        }









    }
}
