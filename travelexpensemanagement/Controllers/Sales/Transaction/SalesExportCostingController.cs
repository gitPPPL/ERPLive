using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    [SessionAuthorize]
    public class SalesExportCostingController : Controller
    {
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly ISalesExportCostingRepository _salesExportCostingRepository;
        public SalesExportCostingController(GlobalValidationdate globalValidationdate, GlobalVariableService globalVariableService,
            DbHelper dbHelper, DataBaseConnection dbConnection, ISalesExportCostingRepository salesExportCostingRepository)
        {
            _globalValidationdate = globalValidationdate;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _salesExportCostingRepository = salesExportCostingRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesExportCosting/Index.cshtml");
        }

        const string doctype = "EXPC";
        
        [HttpGet]
        public JsonResult GetVNo()
        {
            var result = _globalValidationdate.GetVNo(doctype, "COSTING_EXPORT1");
            return Json(new { status = true, V_NO = result });
        }

        public async Task<IActionResult> GetDropdown(string type)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "party":
                    qry = $@"SELECT CODE as value, NAME as text FROM SUBGROUP_MAST where Nature='Customer' and COMP_CODE ={gv.PubCompCode} and ACTIVE= 1 order by NAME";
                    break;

                case "delivery":
                    qry = "SELECT CODE as value, NAME  as text FROM City_mast order by NAME";
                    break;

                case "agent":
                    qry = $@"SELECT CODE as value, NAME as text FROM SUBGROUP_MAST where Nature='Broker' and COMP_CODE ={gv.PubCompCode} and ACTIVE= 1 order by NAME";
                    break;

                case "currency":
                    qry = "SELECT CODE as value, shortName as text FROM Currency_mast Where code>1 Order by shortName";
                    break;

                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }

        [NonAction]
        public async Task<IActionResult> ExecutePaginatedDropdown(string baseQuery, string orderByColumn, string searchTerm, int page, string searchFilterSql)
        {
            int pageSize = 30;
            int offset = (page - 1) * pageSize;

            // 1. Inject the search condition into the query if the user typed something
            string customizedBaseQuery = baseQuery;
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Replace the placeholder {SEARCH_PLACEHOLDER} with the actual dynamic LIKE clauses
                customizedBaseQuery = baseQuery.Replace("{SEARCH_PLACEHOLDER}", searchFilterSql);
            }
            else
            {
                // If no search term, clear out the placeholder safely
                customizedBaseQuery = baseQuery.Replace("{SEARCH_PLACEHOLDER}", "");
            }

            // 2. Build the Total Count Query by wrapping your exact SQL
            string countQuery = $@"SELECT COUNT(1) as TotalRecords  FROM ({customizedBaseQuery}) AS TempTable";

            // 3. Build the Paginated Data Query
            string dataQuery = $@"{customizedBaseQuery} ORDER BY {orderByColumn} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY;";

            // 4. Run both queries simultaneously
            var dataListTask = _dbHelper.GetJsonDataAsync(dataQuery);
            var totalCountTask = _dbHelper.GetJsonDataAsync(countQuery);

            await Task.WhenAll(dataListTask, totalCountTask);

            var countList = totalCountTask.Result;
            int totalCount = 0;

            if (countList != null && countList.Count > 0)
            {
                var firstRow = countList[0] as IDictionary<string, object>;
                if (firstRow != null && firstRow.ContainsKey("TotalRecords"))
                {
                    totalCount = Convert.ToInt32(firstRow["TotalRecords"]);
                }
            }

            return Json(new { success = true, data = dataListTask.Result, totalCount = totalCount });
        }

        [HttpGet]
        public async Task<IActionResult> GetItemList(string searchTerm = "", int page = 1)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            // 1. Define your base query with a {SEARCH_PLACEHOLDER} token
            string baseQuery = $@"Select a.name as Text, a.CODE as Value
                                from item_mast a 
                                where a.comp_code={gv.PubCompCode} and a.active = 1       
                            {{SEARCH_PLACEHOLDER}}";

            // 2. Define what the SQL engine should filter by when searching
            string safeSearch = searchTerm.Replace("'", "''");
            string searchFilterSql = $"AND (a.name LIKE '%{safeSearch}%')";

            // 3. Hand it off to the automated execution block
            return await ExecutePaginatedDropdown(
                baseQuery: baseQuery,
                orderByColumn: "a.name",
                searchTerm: searchTerm,
                page: page,
                searchFilterSql: searchFilterSql
            );
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = doctype;
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("COSTING_EXPORT1", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpPost]
        public IActionResult SaveSalesExportCosting([FromBody] SalesExportCostingModel model)
        {
            var result = _salesExportCostingRepository.SaveSalesExportCosting(model);
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public IActionResult GetDataById(int docId)
        {
            var result = _salesExportCostingRepository.GetDataById(docId);
            if (result.status)
            {
                return Json(new { success = true, data = result.data });
            }
            else
            {
                return Json(new { success = false, message = result.message });
            }
        }

        public async Task<JsonResult> getGlobalValues()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();
                using var erpCon = _dbConnection.GetErpConnection();

                string databaseName;
                using (var connection = _dbConnection.GetErpConnection())
                {
                    databaseName = connection.Database; // Get the database name
                }

                var response = new
                {
                    compCode = gv.PubCompCode,
                    yearCode = gv.PubFYearCode,
                    branchCode = gv.PubBranchCode,
                    add1 = gv.Address1,
                    add2 = gv.Address2,
                    companyName = gv.CompanyName,
                    db = databaseName
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
