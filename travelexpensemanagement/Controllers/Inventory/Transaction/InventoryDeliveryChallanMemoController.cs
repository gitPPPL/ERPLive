using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    [SessionAuthorize]
    public class InventoryDeliveryChallanMemoController : Controller
    {
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly IInventoryDeliveryChallanMemoRepository _repo;
        public InventoryDeliveryChallanMemoController(GlobalValidationdate globalValidationdate, GlobalVariableService globalVariableService,
            DbHelper dbHelper, DataBaseConnection dbConnection, IInventoryDeliveryChallanMemoRepository repo)
        {
            _globalValidationdate = globalValidationdate;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _repo = repo;
        }
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/InventoryDeliveryChallanMemo/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetVNo()
        {
            var result = _globalValidationdate.GetVNo("GTMO", "GATE_MEMO1");
            return Json(new { status = true, V_NO = result });
        }

        public async Task<IActionResult> GetDropdown(string type)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "employee":
                    qry = $@"SELECT CODE as value, CAST(CODE AS NVARCHAR(10)) + SPACE(2) + '|' + SPACE(2) + NAME  as text FROM EMP_MAST where 
                            COMP_CODE = {gv.PubCompCode} and ACTIVE= 1 AND RESIGN_DATE IS NULL order by NAME";
                    break;

                case "vendor":
                    qry = @"SELECT CODE as value, NAME as text FROM SUBGROUP_MAST where COMP_CODE =1 and ACTIVE= 1 order by NAME";
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
            string baseQuery = $@"Select a.name as Text, a.CODE as Value, c.NAME as unit, c.CODE as ucode
                                from item_mast a 
                                left join ITEMUNIT_MAST c on a.UNIT_CODE=c.CODE and a.comp_code=c.comp_code
                                where a.comp_code={gv.PubCompCode}        
                            {{SEARCH_PLACEHOLDER}} 
                            group by a.name ,a.CODE , c.NAME ,c.CODE, a.HSN_CODE";

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
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("GATE_MEMO1", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpPost]
        public IActionResult SaveDeliveryChallanMemo([FromBody] InventoryDeliveryChallanMemoModel model)
        {
            var result = _repo.SaveDeliveryChallanMemo(model);
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public IActionResult GetDataById(string docId)
        {
            var result = _repo.GetDataById(docId);
            if (result.data == null || !result.status)
            {
                return Json(new { success = false, message = result.message });
            }
            return Json(new { success = true, data = result.data });
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
