using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    [SessionAuthorize]
    public class SalesReturnController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly ISalesReturnRepository _repo;
        public SalesReturnController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, GlobalValidationdate globalValidationdate, ISalesReturnRepository repo)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _repo = repo;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesReturn/Index.cshtml");
        }

        public async Task<IActionResult> GetDropdown(string type, string data = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "doctype":
                    qry = $@"Select Code as value, Name as text from DOCTYPE_MAST where DOCTYPE in ('SalesReturn','JobworkReceived') order by Name";
                    break;

                case "party":
                    qry = $@"Select Code as value, Name as text from SUBGROUP_MAST where comp_code={gv.PubCompCode} and active=1 order by Name";
                    break;

                case "salethrough":
                    qry = $@"select code as value, Name as text from SUBGROUP_MAST where NATURE like 'Broker' and active=1 and COMP_CODE ={gv.PubCompCode} 
                            order by name";
                    break;

                case "tax":
                    qry = $@"select a.code as value, a.name as text, a.CGST_PER,a.SGST_PER,a.IGST_PER,a.TDS_PER,a.TCS_PER,a.OTH_PER from TAX_MAST a 
                            where a.ACTIVE = 1 order by name";
                    break;

                case "reference":
                    qry = $@"SELECT DOC_ID AS value, V_NO AS text FROM SALE1 WHERE V_TYPE = 'SAGT' AND ISNULL(Status, 0) <> 2  AND COMP_CODE = {gv.PubCompCode}  
                            AND BRANCH_CODE = {gv.PubBranchCode} AND Year_code >= 7 order by v_no";
                    break;

                case "gate":
                    string gateVType = data.ToUpper() == "SAJR" ? "INJB" : "INSR";
                    qry = $@"Select DOC_ID as value, V_NO as text from GATE1 where V_Type='{gateVType}' and comp_code={gv.PubCompCode} and Branch_Code={gv.PubBranchCode} and 
                            Year_code>=4 order by V_TYpe,V_NO";
                    break;

                case "wb":
                    qry = $@"select DOC_ID as value, ltrim(rtrim(V_NO)) as text from WB1 where V_TYPE ='KANT' AND COMP_CODE ={gv.PubCompCode} and BRANCH_CODE =
                            {gv.PubBranchCode} and Year_code>=4 order by v_no";
                    break;

                case "sauda":
                    qry = $@"SELECT DOC_ID AS value, V_NO AS text, Rate AS Rate, PARTY_CODE FROM SAUDA where V_TYPE='SAUD' 
                            and FAPROV_STATUS='Approved' and COMP_CODE={gv.PubCompCode} and BRANCH_CODE={gv.PubBranchCode} order by v_no";
                    break;

                case "transport":
                    qry = $@"Select CODE as value, Name as text, TDS_PER From TRANSPORT_MAST where comp_code={gv.PubCompCode} order by Name";
                    break;

                case "address":
                    qry = $@"select address_id as value, add1 as text from SUBGROUP_ADDRESS where code={data} and COMP_CODE={gv.PubCompCode} order by ADDRESS_ID";
                    break;

                case "city":
                    qry = $@"select CODE as value, NAME as text from CITY_MAST order by NAME";
                    break;

                case "formtype":
                    qry = $@"Select Code as value, Name as text from FORM_MAST where comp_code = {gv.PubCompCode} order by Name";
                    break;

                case "prodtype":
                    qry = $@"Select Distinct SALE_GROUP as value, SALE_GROUP as text from ITEM_GROUP where comp_code = {gv.PubCompCode} AND SALE_GROUP <> '' 
                                order by SALE_GROUP";
                    break;

                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }

        //=========================================V_NO===============================
        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "SALE1");
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
            string baseQuery = $@"Select name as Text, CODE as Value, HSN_CODE from item_mast 
                                where comp_code={gv.PubCompCode} and active = 1       
                            {{SEARCH_PLACEHOLDER}}";

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

        [HttpGet]
        public async Task<IActionResult> GetPartyAddress(int code, int addressId)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var PartyAddList = await _dbHelper.GetJsonDataAsync(
                                    $@"select sg.ADD1 as add1, sg.ADD2 as add2, sg.ADD3 as add3, sg.PINCODE as pincode, sg.CITY_CODE as cityCode, sg.GSTIN as gstin,
                                     sgm.MOBILE as mobile
                                     from SUBGROUP_ADDRESS sg 
                                     left join CITY_MAST cm on sg.CITY_CODE=cm.CODE  
                                     left join SUBGROUP_MAST sgm on sg.CODE = sgm.CODE
                                     where sg.COMP_CODE={gv.PubCompCode} and sg.code={code} and sg.ADDRESS_ID = {addressId} order by ADD1"
                );
                return Json(new { status = true, data = PartyAddList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        public JsonResult GetddlPackNo(string docType, int partyCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string vvtyp;
            if (docType == "SAJR")
            {
                vvtyp = (gv.PubCompCode == "2" || gv.PubCompCode == "5") ? "FPJR" : "FFRC";
            }
            else
            {
                vvtyp = "FPIS";
            }
            string qry = $@"SELECT DOC_ID as value, LTRIM(RTRIM(A.V_NO)) as text FROM PRODUCTION1 A WHERE A.V_TYPE = '{vvtyp}' AND A.COMP_CODE = {gv.PubCompCode} AND 
            A.BRANCH_CODE = {gv.PubBranchCode} AND A.YEAR_CODE = {gv.PubFYearCode} AND (A.PARTY_CODE = {partyCode} OR A.PARTY_CODE = (SELECT MAIN_CODE FROM SUBGROUP_MAST 
            WHERE COMP_CODE = A.COMP_CODE AND CODE = A.PARTY_CODE AND ACTIVE = 1 GROUP BY MAIN_CODE)) ORDER BY A.V_NO";
            var moduleList = _dropdownService.GetDropdownList(qry);
            return Json(moduleList);
        }

        [HttpPost]
        public async Task<IActionResult> GetReferenceDetails(string refValue)
        {
            var result = await _repo.GetReferenceDetails(refValue);
            if (result.data == null)
            {
                return Json(new { success = false, message = "Reference details not found." });
            }
            dynamic data = result.data;
            return Json(new
            {
                success = true,
                header = data.header,
                items = data.items
            });
        }

        [HttpPost]
        public IActionResult Save([FromBody] SalesReturn salesReturn)
        {

            if (salesReturn.FormData == null || salesReturn.RowData == null)
                return Json(new { success = false, message = "Invalid request." });

            try
            {
                var result = _repo.Save(salesReturn);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        [HttpGet]
        public IActionResult GetID(int id, string vType)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(vType))
            {
                return Json(new { success = false, message = "Invalid ID or VType." });
            }
            try
            {
                var result = _repo.GetID(id, vType);
                if (result.data == null)
                {
                    return Json(new { success = false, message = "Sales return details not found." });
                }
                dynamic data = result.data;
                return Json(new { success = true, Header = data.Header, Items = data.Items });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while retrieving sales return details." + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSaudaItemDetails(string saudaType, string saudaNo, int partyCode, string itemCodes)
        {
            try
            {
                var result = await _repo.GetSaudaItemDetails(saudaType, saudaNo, partyCode, itemCodes);
                if (result.data == null)
                {
                    return Json(new { success = false, message = "Sauda details not found!" });
                }
                return Json(new { success = true, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetWBWeight(string wbDocId)
        {
            try
            {
                var result = _repo.GetWBWeight(wbDocId);
                return Json(new { success = true, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetPackingData(string packType, int packNo, string vType, int vNo)
        {
            try
            {
                var result = _repo.GetPackingData(packType, packNo, vType, vNo);
                if (result.data == null)
                {
                    return Json(new { success = false, message = "Packing details not found!" });
                }
                dynamic data = result.data;
                return Json(new { success = true, exists = data.exists, data = data.data, existingVNo = data.existingVNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetGateData(string GateDocId)
        {
            try
            {
                var result = _repo.GetGateData(GateDocId);
                if (result.data == null)
                {
                    return Json(new { success = false, message = "Gate details not found!" });
                }
                dynamic data = result.data;
                return Json(new { success = true, party = data.party, items = data.items });
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
            var result = await _globalValidationdate.CheckValidDate("Sale1", vdate, vtype, vno);
            return Ok(result);
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
                    db = databaseName,
                    tcsper = gs.pubBPTCSPer,
                    phone = gv.Phone,
                    gst = gv.gstin,
                    pan = gv.PAN,
                    website = gv.Website,
                    email = gv.Email,
                    regadd1 = gv.RegAdd1,
                    cin = gv.CINNO,
                    regadd2 = gv.RegAdd2,
                    userlevel = gv.PubUserLevel
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ValidateData([FromBody] ValidateDataRequest model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid request data" });

            try
            {
                var result = _repo.ValidateData(model);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CalculateSaudaRate(int partyCode, string itemCodes)
        {
            try
            {
                var result = await _repo.CalculateSaudaRate(partyCode, itemCodes);
                if (result.data == null)
                {
                    return Json(new { success = false, message = "Sauda rate calculation failed." });
                }
                return Json(new { success = true, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult PostSalesReturn(string vType, int vNo)
        {
            try
            {
                var result = _repo.PostSalesReturn(vType, vNo);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error while posting voucher." });
            }
        }

        [HttpPost]
        public IActionResult SaveTransport([FromBody] SaveTransportRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.VType) || req.VNo <= 0)
                    return Json(new { success = false, message = "Invalid request." });

                var result = _repo.SaveTransport(req);
                return Json(new { success = result.status, message = result.message, warnings = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error occurred while saving transport data." });
            }
        }
    }
}
