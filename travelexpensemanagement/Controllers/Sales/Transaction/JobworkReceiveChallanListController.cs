using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class JobworkReceiveChallanListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public JobworkReceiveChallanListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Sales/Transaction/JobworkReceiveChallanList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAlldataList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var g = _globalVariableService.GetGlobalVariables();
            DataSet ds = new DataSet();
            int totalCount = 0;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_JobworkReceive", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);
            }
            var data = new List<Dictionary<string, object>>();

            if (ds.Tables.Count > 0)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in ds.Tables[0].Columns)
                    {
                        dict[col.ColumnName] = row[col] == DBNull.Value ? "" : row[col].ToString();
                    }
                    data.Add(dict);
                }
            }
            if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
            {
                totalCount = Convert.ToInt32(ds.Tables[1].Rows[0][0]);
            }
            return Json(new
            {
                data,
                totalCount
            });
        }

    }
}
