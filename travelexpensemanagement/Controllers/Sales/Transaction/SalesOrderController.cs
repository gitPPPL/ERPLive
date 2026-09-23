using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesOrderController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly ISalesOrderRepository _repo;
        public SalesOrderController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, GlobalValidationdate globalValidationdate,
            ISalesOrderRepository repo)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalVariableService = globalValue;
            _globalValidationdate = globalValidationdate;
            _repo = repo;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesOrder/Index.cshtml");
        }

        const string doctype = "SORD";


        [HttpGet]
        public async Task<JsonResult> GetVNo()
        {
            var result = _globalValidationdate.GetVNo(doctype, "ORDER1");

            var gs = await _globalVariableService.LoadGeneralSetting();
            string pubDefSSINSO = gs.pubDefSSINSO;
            return Json(new { status = true, V_NO = result, pubDefSSINSO });
        }

        public async Task<IActionResult> GetDropdown(string type, int data = 0)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "saudano":
                    qry = $@"select V_NO as text, DOC_ID as value From SAUDA where v_type='SAUD' and Status=1 and COMP_CODE={gv.PubCompCode} and BRANCH_CODE={gv.PubBranchCode} 
                            order by V_No";
                    break;

                case "paymentterm":
                    qry = $@"Select Code as value, Name as text from PAYTERM_MAST where comp_code= {gv.PubCompCode} order by Name";
                    break;

                case "issueno":
                    string issueVType = gv.PubCompCode == "1" ? "RAID" : "RAIS";
                    qry = $@"select distinct DOC_ID as value, ltrim(rtrim(V_NO)) as text from Issue2 where COMP_CODE = {gv.PubCompCode} and BRANCH_CODE = {gv.PubBranchCode} 
                            and year_code = {gv.PubFYearCode} and V_type = '{issueVType}' order by text ";
                    break;

                case "billtoshipto":
                    qry = $@"select CODE as value, ltrim(rtrim(name)) as text from SUBGROUP_MAST where comp_code={gv.PubCompCode} order by Name";
                    break;

                case "city":
                    qry = $@"select CODE as value, NAME as text from CITY_MAST order by NAME";
                    break;

                case "packingno":
                    qry = $@"select DOC_ID as value, ltrim(rtrim(V_NO)) as text from PRODUCTION1 where V_TYPE ='FPIS' and COMP_CODE={gv.PubCompCode} and BRANCH_CODE={gv.PubBranchCode}
                            order by V_no";
                    break;

                case "address":
                    qry = $@"select address_id as value, add1 as text from SUBGROUP_ADDRESS where code={data} and COMP_CODE={gv.PubCompCode} order by ADDRESS_ID";
                    break;

                case "status":
                    qry = $@"Select Code as value, Name as text from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                    break;

                case "tax":
                    qry = $@"SELECT NAME as text, CODE as value, CGST_PER, SGST_PER, IGST_PER, isnull(VAT_PER,0) VAT_PER from TAX_MAST WHERE ACTIVE = 1 ORDER BY NAME";
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

        [HttpGet]
        public async Task<IActionResult> GetPartyAddress(int code, int addressId)
        {
            try
            {
                var result = await _repo.GetPartyAddress(code, addressId);
                return Json(new { status = result.status, data = result.data, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSaudaDataList(string saudaNo)
        {
            try
            {
                var result = await _repo.GetSaudaDataList(saudaNo);
                return Json(new { status = result.status, data = result.data, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetIssueDetails(string issueNo)
        {
            var result = await _repo.GetIssueDetails(issueNo);
            return Json(new { status = result.status, data = result.data, message = result.message });
        }

        [HttpGet]
        public async Task<IActionResult> GetPackingDetail(int vno, string packingNo)
        {
            try
            {
                var result = await _repo.GetPackingDetail(vno, packingNo, doctype);
                if (result.data != null)
                {
                    dynamic data = result.data;

                    return Json(new
                    {
                        status = result.status,
                        data = data.details,
                        totalQty = data.totalQty,
                        totalNos = data.totalNos
                    });
                }
                return Json(new { status = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLinkedOrders(int saudaNo)
        {
            var result = await _repo.GetLinkedOrders(saudaNo);
            return Json(new { status = result.status, data = result.data, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> GetSaudaRate([FromBody] GetSaudaRateRequest request)
        {
            if (request == null || request.SaudaNo <= 0)
            {
                return BadRequest(new { status = false, message = "Invalid Request!" });
            }

            try
            {
                var result = await _repo.GetSaudaRate(request);

                if (result.data != null)
                {
                    dynamic data = result.data;

                    return Ok(new
                    {
                        status = result.status,
                        data = new
                        {
                            saudaNo = request.SaudaNo,
                            saudaRate = data.saudaRate,
                            items = request.Items
                        }
                    });
                }
                return Json(new { status = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPackAmount(int itemCode)
        {
            try
            {
                var result = await _repo.GetPackAmount(itemCode);
                return Json(new { success = result.status, data = result.data, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCalculateAmtData(int itemCode)
        {
            try
            {
                var result = await _repo.GetCalculateAmtData(itemCode);
                if (result.data != null)
                {
                    return Json(new { success = result.status, data = result.data });
                }
                else
                {
                    return Json(new { success = result.status, message = result.message });
                }
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
            string vtype = doctype;
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("ORDER1", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> ValidateData([FromBody] ValidateDataModel model)
        {
            try
            {
                var result = await _repo.ValidateData(model);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderRecordsById(int id, string vType)
        {
            try
            {
                var result = await _repo.GetPurchaseOrderRecordsById(id, vType);
                if (result.data != null)
                {
                    dynamic data = result.data;
                    return Json(new { status = result.status, header = data.header, detail = data.detail, existingItems = data.existingItems });
                }
                else
                {
                    return Json(new { status = false, message = result.message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckSaudaApproval([FromBody] PurchaseOrder model)
        {
            try
            {
                var result = await _repo.CheckSaudaApproval(model);
                return Ok(new { status = true, isApprovalRequired = result.IsApprovalRequired, message = result.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateSalesOrder([FromBody] PurchaseOrder POmodel)
        {
            if (POmodel == null)
                return Json(new { status = false, message = " data save failed." });
            try
            {
                var result = await _repo.SaveOrUpdateSalesOrder(POmodel, doctype);
                if (result.data != null)
                {
                    dynamic data = result.data;
                    return Json(new { status = result.status, message = result.message, isWarning = data.isWarning });
                }
                return Json(new { status = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetOrderAdjustment(int ordNo)
        {
            try
            {
                var result = _repo.GetOrderAdjustment(ordNo);
                return Json(new { status = result.status, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveDispatchDetails([FromBody] List<DispatchDeliveryPlaning> dispatch)
        {
            try
            {
                var result = _repo.SaveDispatchDetails(dispatch, doctype);
                return Json(new { status = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetDispatchDeliveryPlan(int vNo)
        {
            try
            {
                var result = _repo.GetDispatchDeliveryPlan(vNo, doctype);
                return Json(new { status = result.status, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public async Task<JsonResult> getGlobalValues()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();
                using var erpCon = _dbcontext.GetErpConnection();

                string databaseName;
                using (var connection = _dbcontext.GetErpConnection())
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
