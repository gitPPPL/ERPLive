using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PrintingRequestionListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        public PrintingRequestionListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService; ;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PrintingRequestionList/Index.cshtml");
        }
        public JsonResult GetPrintingRequestionList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var response = new RepositoryResponseList<PrintingRequestionModel>
            {
                status = false,
                message = "No data found",
                totalCount = 0,
                data = new List<PrintingRequestionModel>()
            };
            try
            {
                var global = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetPrintingRequestion", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = global.PubCompCode;
                        cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = global.PubBranchCode;
                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = global.PubFYearCode;
                        cmd.Parameters.Add("@SearchTerm", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm;
                        cmd.Parameters.Add("@PageNumber", SqlDbType.Int).Value = pageNumber;
                        cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
                        cmd.Parameters.AddWithValue("@Action", "ListPage");
                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            var dataList = new List<PrintingRequestionModel>();
                            while (reader.Read())
                            {
                                var model = new PrintingRequestionModel
                                {
                                    VNo = reader["VNo"] == DBNull.Value ? "" : reader["VNo"].ToString(),
                                    VType = reader["VType"] == DBNull.Value ? "" : reader["VType"].ToString(),
                                    VDate = reader["VDate"] == DBNull.Value ? "" : reader["VDate"].ToString(),
                                    Department = reader["Department"] == DBNull.Value ? "" : reader["Department"].ToString(),
                                    OwnerName = reader["OwnerName"] == DBNull.Value ? "" : reader["OwnerName"].ToString(),
                                    Place = reader["Place"] == DBNull.Value ? "" : reader["Place"].ToString(),
                                    ValidDate = reader["ValidDate"] == DBNull.Value ? "" : reader["ValidDate"].ToString(),
                                    TargetDate = reader["TargetDate"] == DBNull.Value ? "" : reader["TargetDate"].ToString(),
                                    Remarks = reader["Remarks"] == DBNull.Value ? "" : reader["Remarks"].ToString(),
                                    Status = reader["Status"] == DBNull.Value ? "" : reader["Status"].ToString()
                                };
                                dataList.Add(model);
                            }
                            int totalCount = 0;
                            if (reader.NextResult() && reader.Read())
                            {
                                if (reader["TotalCount"] != DBNull.Value)
                                {
                                    totalCount = Convert.ToInt32(
                                        reader["TotalCount"]
                                    );
                                }
                            }
                            response.status = true;
                            response.data = dataList;
                            response.totalCount = totalCount;
                            response.message = dataList.Any() ? "Data retrieved successfully" : "No records found";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
            }
            return Json(response);
        }
        
    }
}
