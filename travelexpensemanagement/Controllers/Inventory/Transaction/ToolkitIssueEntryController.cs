using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    [SessionAuthorize]
    public class ToolkitIssueEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IToolkitIssueEntryRepository _repository;
        public ToolkitIssueEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper,
            GlobalValidationdate globalValidationdate, IToolkitIssueEntryRepository repository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _repository = repository;
        }
        private string docType = "TOIS";

        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/ToolkitIssueEntry/Index.cshtml");
        }

        public async Task<IActionResult> GetDdlList(string type)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type)
            {
                case "doctype":
                    qry += $@"SELECT CODE as Value, NAME as Text FROM DOCTYPE_MAST  WHERE code in ('{docType}')    order by name";
                    break;

                //case "item":
                //    qry += $@"SELECT CODE as Value, NAME as Text FROM ITEM_MAST  WHERE comp_code = {gv.PubCompCode}  order by name";
                //    break;
                case "employee":
                    qry += $@"SELECT CODE as Value, name as EmpName, (convert(varchar,code ) + space(2) + '|' + space(2) + NAME) as Text FROM EMP_MAST  
                            WHERE comp_code = {gv.PubCompCode} order by name ";
                    break;
                case "place":
                    qry += $@"SELECT CODE as Value, NAME as Text FROM PLACE_MAST  WHERE comp_code = {gv.PubCompCode}   order by name";
                    break;
                case "department":
                    qry += $@"SELECT CODE as Value, NAME as Text FROM ITEMDEPT_MAST  WHERE comp_code = {gv.PubCompCode}   order by name";
                    break;
                default:
                    break;
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }

        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "STOOL");
            return Json(new { status = true, V_NO = result });
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
            string countQuery = $@"
        SELECT COUNT(1) as TotalRecords 
        FROM ({customizedBaseQuery}) AS TempTable";

            // 3. Build the Paginated Data Query
            string dataQuery = $@"
        {customizedBaseQuery}
        ORDER BY {orderByColumn}
        OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY;";

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
            string baseQuery = $@"SELECT CODE as Value, NAME as Text FROM ITEM_MAST  WHERE comp_code = {gv.PubCompCode}        
                            {{SEARCH_PLACEHOLDER}} ";

            // 2. Define what the SQL engine should filter by when searching
            string safeSearch = searchTerm.Replace("'", "''");
            string searchFilterSql = $"AND (name LIKE '%{safeSearch}%')";

            // 3. Hand it off to the automated execution block
            return await ExecutePaginatedDropdown(
                baseQuery: baseQuery,
                orderByColumn: "name",
                searchTerm: searchTerm,
                page: page,
                searchFilterSql: searchFilterSql
            );
        }


        [HttpPost]
        public IActionResult SaveOrUpdate([FromBody] ToolKitIssueModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid request!" });
            }
            try
            {
                var result = _repository.SaveOrUpdate(model);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetDataById(string vType, string vNo)
        {
            try
            {
                var result = _repository.GetDataById(vType, vNo);
                if (result == null || result.data == null)
                {
                    return Json(new { success = result.status, message = result.message });
                }
                return Json(new { success = result.status, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = true, message = ex.Message });
            }
        }

        [HttpGet]
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
                    branchCode = gv.PubBranchCode,
                    add1 = gv.Address1,
                    add2 = gv.Address2,
                    companyName = gv.CompanyName,
                    db = databaseName,
                    userid = gv.PubUserId,
                    wsid = gv.PubWorkStationID
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> PrepareToolKitBalReport(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var result = await _repository.PrepareToolKitBalReportAsync(fromDate, toDate);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
