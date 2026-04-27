using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class DeliveryChallanMemoListController : Controller
    {
        private readonly DataBaseConnection _dbConnection; 
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public DeliveryChallanMemoListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/GateEntry/Transaction/DeliveryChallanMemoList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetGateMemos(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var gateMemos = new List<GATE_MEMO1>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GATE_MEMO1_MGMT", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SubAction", "GETALLBYVNO");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                gateMemos.Add(new GATE_MEMO1
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    BRANCH_CODE = reader["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BRANCH_CODE"]) : 0,
                                    YEAR_CODE = reader["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["YEAR_CODE"]) : 0,
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                    EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                    EMP_NAME = reader["EMP_NAME"]?.ToString(),
                                    VENDOR_CODE = reader["VENDOR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["VENDOR_CODE"]) : 0,
                                    VENDOR_NAME = reader["VENDOR_NAME"]?.ToString(),
                                    TRANSPORT_CODE = reader["TRANSPORT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["TRANSPORT_CODE"]) : 0,
                                    TRANSPORT_NAME = reader["TRANSPORT_NAME"]?.ToString(),
                                    THROUGH = reader["THROUGH"]?.ToString(),
                                    RETURN_DATE = reader["RETURN_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["RETURN_DATE"]) : DateTime.MinValue,
                                    REMARKS = reader["REMARKS"]?.ToString(),
                                    STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0,
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching gate memos", error = ex.Message });
            }

            return Json(new { success = true, lists = gateMemos, totalCount });
        }

    }
}
