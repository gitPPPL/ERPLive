using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;


namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class DeliveryChallanStoreController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IDeliveryChallanStoreRepository _deliveryChallanStoreRepository;
        public DeliveryChallanStoreController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper,
            GlobalValidationdate globalValidationdate, IDeliveryChallanStoreRepository deliveryChallanStoreRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _deliveryChallanStoreRepository = deliveryChallanStoreRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/DeliveryChallanStore/Index.cshtml");
        }

        public async Task<IActionResult> GetDdlList(string type, string vType = "", int shipFromCode = 0)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "doctype":
                    qry = @"SELECT CODE AS Value, NAME AS Text FROM DOCTYPE_MAST WHERE DOCTYPE = 'DeliveryChallan' ORDER BY NAME";
                    break;

                case "refbilltype":
                    qry = @"SELECT CODE AS Value, NAME AS Text FROM DOCTYPE_MAST WHERE CODE = 'JORD' ORDER BY NAME";
                    break;

                case "refno":
                    qry = $@"SELECT V_NO AS Value, V_NO AS Text FROM ORDER1 WHERE COMP_CODE = {gv.PubCompCode} AND YEAR_CODE = {gv.PubFYearCode} AND V_TYPE = '{vType}' 
                            ORDER BY V_NO";
                    break;

                case "party":
                    qry = $@"SELECT CODE AS Value, NAME AS Text FROM SUBGROUP_MAST WHERE NATURE NOT IN ('cash', 'bank', 'others') AND ACTIVE = 1 AND COMP_CODE = 
                            {gv.PubCompCode} ORDER BY NAME";
                    break;

                case "responsibleperson":
                    qry = @"SELECT CODE AS Value, cast(CODE as nvarchar(20)) + SPACE(2) + '|' + SPACE(2) + NAME AS Text FROM EMP_MAST WHERE TYPE = 'STAFF' AND RESIGN_DATE IS NULL AND ACTIVE = 1 ORDER BY NAME";
                    break;

                case "city":
                    qry = @"SELECT CODE AS Value, NAME AS Text FROM CITY_MAST ORDER BY NAME";
                    break;

                case "address":
                    qry = $@"select address_id Value, add1 Text from SUBGROUP_ADDRESS where code={shipFromCode} and 
                                COMP_CODE={gv.PubCompCode} order by ADDRESS_ID";
                    break;

                case "tax":
                    qry = $@"select name as Text, code as Value, CGST_PER,SGST_PER,IGST_PER from TAX_MAST
                            where ACTIVE = 1 order by name";
                    break;

                case "transport":
                    qry = $@"select CODE as Value, NAME as Text from TRANSPORT_MAST where COMP_CODE= {gv.PubCompCode} order by name";
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
            string baseQuery = $@"Select a.name as Text, a.CODE as Value, c.NAME as unit, c.CODE as ucode, a.HSN_CODE as hsncode
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

        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "dc_note1");
            return Json(new { status = true, V_NO = result });
        }

        public IActionResult GetAddressByBillToParty(int code, int addressId)
        {
            var result = _deliveryChallanStoreRepository.GetAddressByBillToParty(code, addressId);
            if(result.status)
            {
                return Json(new { success = true, addressDetails = result.data });
            }
            else
            {
                return Json(new { success = false, message = result.message });
            }
        }
  
        [HttpGet]
        public IActionResult GetDetailsOnRefNoLoad(string vType, int vNo)
        {
            var result = _deliveryChallanStoreRepository.GetDetailsOnRefNoLoad(vType, vNo);
            if(result.status)
            {
                return Json(new { success = true, data = result.data });
            }
            else
            {
                return Json(new { success = false, message = result.message });
            }
        }

        [HttpGet]
        public IActionResult GetDetailsOnWBLoad(int vNo)
        {
            var result = _deliveryChallanStoreRepository.GetDetailsOnWBLoad(vNo);
            if(result.status)
            {
                return Json(new { success = true, data = result.data });
            }
            else
            {
                return Json(new { success = false, message = result.message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveDeliveryChallanStore([FromBody] DeliveryChallanStoreModel model)
        {
            var result = await _deliveryChallanStoreRepository.SaveDeliveryChallanStore(model);
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public IActionResult GetDataById(string docId, string docType)
        {
            var result = _deliveryChallanStoreRepository.GetDataById(docId, docType);
            if(result.status)
            {
                return Json(new { success = true, data = result.data });
            }
            else
            {
                return Json(new { success = false, message = result.message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckWeight([FromBody] WBCheckRequest request)
        {
            var result = await _deliveryChallanStoreRepository.CheckWeight(request);
            if (result.status)
            {
                return Json(new { success = true, message = result.message });
            }
            else
            {
                return Json(new { success = false, message = result.message });
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
                    yearCode = gv.PubFYearCode,
                    branchCode = gv.PubBranchCode,
                    add1 = gv.Address1,
                    add2 = gv.Address2,
                    companyName = gv.CompanyName,
                    companyGst = gv.gstin,
                    phone = gv.Phone,
                    email = gv.Email,
                    db = databaseName
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("DC_NOTE1", vdate, vtype, vno);
            return Ok(result);
        }
    }
}
