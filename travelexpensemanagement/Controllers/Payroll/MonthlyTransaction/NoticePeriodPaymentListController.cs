using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class NoticePeriodPaymentListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public NoticePeriodPaymentListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/MonthlyTransaction/NoticePeriodPaymentList/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetNoticePeriodPaymentList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                using (var con = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_InsertPAYNOTICE", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@V_TYPE", DBNull.Value);
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
                                V_NO = reader["V_NO"]?.ToString(),
                                DocDate = reader["DocDate"]?.ToString(),
                                EMP_CODE = reader["EMP_CODE"]?.ToString(),
                                Days = reader["Days"]?.ToString(),
                                Gross = reader["Gross"]?.ToString(),
                                Rate = reader["Rate"]?.ToString(),
                                Amount = reader["Amount"]?.ToString(),
                                UUSER = reader["UUSER"]?.ToString(),
                                UDATE = reader["UDATE"]?.ToString(),
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"]?.ToString(),
                                AED = reader["AED"]?.ToString(),
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            });
                        }
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = Convert.ToInt32(reader["TotalCount"]);
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
                    message = "Error while fetching Notice Period Payments.",
                    error = ex.Message
                });
            }
        }

    }
}
