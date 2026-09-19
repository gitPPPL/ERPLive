using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesSaudaEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly ISaleSaudaEntryRepository _saleSaudaEntryRepository;

        public SalesSaudaEntryController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, ISaleSaudaEntryRepository saleSaudaEntryRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _saleSaudaEntryRepository = saleSaudaEntryRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesSaudaEntry/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GenerateVNo()
        {
            string newV_NO = "00001";
            string vType = "SAUD";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = @"SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string query = @"
                    SELECT ISNULL(MAX(CAST(RIGHT(CAST(V_NO AS VARCHAR(20)), 5) AS INT)), 0) + 1 FROM SAUDA WHERE V_TYPE = @VType AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@VType", vType);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    int nextNo = Convert.ToInt32(cmd.ExecuteScalar());

                    newV_NO = prefixYR + nextNo.ToString("D5");
                }

                return Json(new
                {
                    v_NO = newV_NO,
                    v_TYPE = vType
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetDropdown(string type)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = "";

            switch (type)
            {
                case "PaymentTerm":
                    query = $@"select CODE, NAME  from PAYTERM_MAST where comp_code= {globalVariables.PubCompCode} ORDER BY NAME";
                    break;

                case "DocStatus":
                    query = $@"Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                    break;

                case "NotifyTo":
                    query = $@"Select Code,Name from SUBGROUP_MAST where COMP_CODE= {globalVariables.PubCompCode} and NATURE='Customer' and active=1 Order by NAME";
                    break;

                case "ComissionAgent":
                    query = $@"Select Code,Name from SUBGROUP_MAST where COMP_CODE= {globalVariables.PubCompCode} and NATURE='Broker' and active=1 Order by NAME";
                    break;

                case "CurrencyMast":
                    query = $@"select code, ShortName  from CURRENCY_MAST ORDER BY code";
                    break;

                case "CountryMast":
                    query = $@"Select Code,Name from country_mast Order by Name";
                    break;

                case "TenacityGroup":
                    query = $@"Select Code,Name from TENACITY_GRPMAST where Comp_code= {globalVariables.PubCompCode} Order by Name";
                    break;

                case "CityMast":
                    query = $@"select code , name from CITY_MAST order by name ";
                    break;

                case "SoldBy":
                    query = $@"Select code,name from SALESEXECUTIVE_MAST where COMP_CODE = {globalVariables.PubCompCode} and active=1";
                    break;
            }
             
            var dropdownList = _dropdownService.GetDropdownList(query);
            
            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult GetCustomerAgentList()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT 
                a.CODE,
                a.NAME,
                a.ADD1,
                a.ADD2,
                a.ADD3,
                a.CITY_CODE,
                b.NAME AS CityName,
                a.MOBILE,
                a.Nature,
                a.GSTIN,
                a.PINCODE,
                c.NAME AS Country
            FROM SUBGROUP_MAST a
            LEFT JOIN CITY_MAST b 
                ON a.CITY_CODE = b.CODE
            LEFT JOIN COUNTRY_MAST c 
                ON b.COUNTRY_CODE = c.CODE
            WHERE a.Nature IN ('Customer', 'Agent', 'Broker', 'Supplier')
              AND a.Active = 1
              AND a.COMP_CODE = @COMP_CODE
            ORDER BY a.NAME";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    value = reader["CODE"],
                    text = reader["NAME"],
                    add1 = reader["ADD1"],
                    add2 = reader["ADD2"],
                    add3 = reader["ADD3"],
                    cityCode = reader["CITY_CODE"],
                    cityName = reader["CityName"],
                    mobile = reader["MOBILE"],
                    nature = reader["Nature"],
                    gstin = reader["GSTIN"],
                    pincode = reader["PINCODE"],
                    country = reader["Country"]
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetCustomerCreditLimit(int customerCode)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                // ==========================================
                // 1. GET CREDIT LIMIT
                // ==========================================
                string creditLimitQuery = @"
                SELECT ISNULL(CR_LIMIT, 0)
                FROM CRLIMIT_MAST
                WHERE PARTY_CODE = @PARTY_CODE
                  AND COMP_CODE = @COMP_CODE";

                decimal creditLimit = 0;

                using (var cmd = new SqlCommand(creditLimitQuery, con))
                {
                    cmd.Parameters.AddWithValue("@PARTY_CODE", customerCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

                    var result = cmd.ExecuteScalar();

                    if (result == null)
                    {
                        return Json(new
                        {
                            exists = false,
                            message = "Please First Create, Credit Limit Master for This Customer"
                        });
                    }

                    creditLimit = Convert.ToDecimal(result);
                }

                // ==========================================
                // 2. GET OUTSTANDING
                // ==========================================
                string outstandingQuery = @"
                SELECT
                    ISNULL((
                        SELECT SUM(AMT)
                        FROM LEDGER2
                        WHERE DR_CODE = @PARTY_CODE
                          AND COMP_CODE = @COMP_CODE
                    ), 0)
                    -
                    ISNULL((
                        SELECT SUM(AMT)
                        FROM LEDGER2
                        WHERE CR_CODE = @PARTY_CODE
                          AND COMP_CODE = @COMP_CODE
                    ), 0)";

                decimal outstanding = 0;

                using (var cmd = new SqlCommand(outstandingQuery, con))
                {
                    cmd.Parameters.AddWithValue("@PARTY_CODE", customerCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

                    outstanding = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // ==========================================
                // 3. AVAILABLE LIMIT
                // ==========================================
                decimal availableLimit = creditLimit - outstanding;

                return Json(new
                {
                    exists = true,
                    creditLimit = creditLimit,
                    outstanding = outstanding,
                    availableLimit = availableLimit
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetItemList(int pageNo = 1, int pageSize = 500,string searchTerm = "")
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT 
                a.CODE,
                a.NAME,
                a.SHORTNAME,
                b.MGROUP_TYPE
            FROM ITEM_MAST a
            LEFT JOIN ITEM_MGROUP b
                ON b.CODE = a.MGROUP_CODE
                AND b.COMP_CODE = a.COMP_CODE
            WHERE a.COMP_CODE = @COMP_CODE
              AND a.Active = 1
              AND (
                    @SEARCH_TERM = ''
                    OR a.CODE LIKE '%' + @SEARCH_TERM + '%'
                    OR a.NAME LIKE '%' + @SEARCH_TERM + '%'
                    OR a.SHORTNAME LIKE '%' + @SEARCH_TERM + '%'
                  )
            ORDER BY a.NAME
            OFFSET @OFFSET ROWS
            FETCH NEXT @PAGE_SIZE ROWS ONLY";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            cmd.Parameters.AddWithValue("@SEARCH_TERM", searchTerm?.Trim() ?? "");

            int offset = (pageNo - 1) * pageSize;

            cmd.Parameters.AddWithValue("@OFFSET", offset);
            cmd.Parameters.AddWithValue("@PAGE_SIZE", pageSize);

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    value = reader["CODE"],
                    text = reader["NAME"],
                    shortName = reader["SHORTNAME"],
                    mgroupType = reader["MGROUP_TYPE"]
                });
            }

            return Json(new
            {
                data = list,
                pageNo = pageNo,
                pageSize = pageSize,
                hasMore = list.Count == pageSize
            });
        }

        [HttpGet]
        public IActionResult GetPIList(string searchTerm = "")
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT 
                CONCAT(a.V_TYPE, a.V_NO) AS DocNo,
                FORMAT(a.V_DATE, 'dd/MM/yyyy') AS VDate,
                b.NAME AS CustomerName,
                a.V_NO
            FROM SALE1 a
            LEFT JOIN SUBGROUP_MAST b
                ON a.BILL_CODE = b.CODE
                AND a.COMP_CODE = b.COMP_CODE
            WHERE a.V_TYPE = 'DMEU'
              AND a.COMP_CODE = @COMP_CODE
              AND a.BRANCH_CODE = @BRANCH_CODE
              AND a.STATUS = 1
              AND (
                    @SEARCH_TERM = ''
                    OR CONCAT(a.V_TYPE, a.V_NO) LIKE '%' + @SEARCH_TERM
                  )
            ORDER BY a.V_NO";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
            cmd.Parameters.AddWithValue("@SEARCH_TERM", searchTerm?.Trim() ?? "");

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    docNo = reader["DocNo"],
                    vDate = reader["VDate"],
                    customerName = reader["CustomerName"],
                    vNo = reader["V_NO"]
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetPIDetails(string docNo)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            if (string.IsNullOrWhiteSpace(docNo))
            {
                return BadRequest(new { message = "PI Document No is required." });
            }

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            // ==========================================================
            // SALE1 → PI HEADER
            // ==========================================================

            string headerQuery = @"
            SELECT TOP 1
                a.*,
                b.NAME AS CityName
            FROM SALE1 a
            LEFT JOIN CITY_MAST b
                ON a.BILL_CITY = b.CODE
            WHERE CONCAT(a.V_TYPE, a.V_NO) = @DOC_NO
              AND a.COMP_CODE = @COMP_CODE
              AND a.BRANCH_CODE = @BRANCH_CODE";

            using var headerCmd = new SqlCommand(headerQuery, con);

            headerCmd.Parameters.AddWithValue("@DOC_NO", docNo.Trim());
            headerCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            headerCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
            
            using var reader = headerCmd.ExecuteReader();

            if (!reader.Read())
            {
                reader.Close();

                return NotFound(new { message = "PI not found."});
            }

            var result = new Dictionary<string, object?>();

            result["billName"] = reader["Bill_Name"];
            result["billCode"] = reader["Bill_Code"];
            result["add1"] = reader["Bill_Add1"];
            result["add2"] = reader["Bill_Add2"];
            result["add3"] = reader["Bill_Add3"];
            result["cityCode"] = reader["Bill_City"];
            result["cityName"] = reader["CityName"];
            result["quantity"] = reader["TOT_NET"];
            result["itemType"] = reader["Item_type"];
            result["finalDestCountry"] = reader["FINAL_DEST_COUNTRY"];
            result["incoterm"] = reader["INCOTERM"];
            result["payTerm"] = reader["PAY_TERM"];
            result["soldBy"] = reader["SOLD_BY"];
            result["buyerOrderNo"] = reader["BUYER_ORDNO"];
            result["currency"] = reader["IMPORT_CURRENCY"];

            reader.Close();

            // ==========================================================
            // SALE2 → ITEM + RATE
            // ==========================================================

            string itemQuery = @"
            SELECT TOP 1
                s.ITEM_CODE,
                s.RATE,
                i.NAME AS ItemName,
                i.SHORTNAME AS ItemShortName,
                i.GROUP_CODE,
                ig.SALE_GROUP
            FROM SALE2 s
            LEFT JOIN ITEM_MAST i
                ON s.ITEM_CODE = i.CODE
                AND i.COMP_CODE = @COMP_CODE
            LEFT JOIN ITEM_GROUP ig
                ON i.GROUP_CODE = ig.CODE
                AND i.COMP_CODE = ig.COMP_CODE
            WHERE CONCAT(s.V_TYPE, s.V_NO) = @DOC_NO
              AND s.COMP_CODE = @COMP_CODE
              AND s.BRANCH_CODE = @BRANCH_CODE";

            using var itemCmd = new SqlCommand(itemQuery, con);

            itemCmd.Parameters.AddWithValue("@DOC_NO", docNo.Trim());
            itemCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            itemCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);

            using var itemReader = itemCmd.ExecuteReader();

            if (itemReader.Read())
            {
                result["itemCode"] = itemReader["ITEM_CODE"];
                result["rate"] = itemReader["RATE"];
                result["itemName"] = itemReader["ItemName"];
                result["itemShortName"] = itemReader["ItemShortName"];
                result["groupCode"] = itemReader["GROUP_CODE"];
                result["saleGroup"] = itemReader["SALE_GROUP"];
            }

            itemReader.Close();

            // ==========================================================
            // COUNTRY
            // ==========================================================

            string countryQuery = @"
            SELECT ISNULL(c.NAME, '') AS CountryName
            FROM CITY_MAST cm
            LEFT JOIN COUNTRY_MAST c
                ON cm.COUNTRY_CODE = c.CODE
            WHERE cm.CODE = @CITY_CODE";

            using var countryCmd = new SqlCommand(countryQuery, con);

            countryCmd.Parameters.AddWithValue("@CITY_CODE",result["cityCode"] ?? 0);

            result["country"] = countryCmd.ExecuteScalar()?.ToString() ?? "";

            // ==========================================================
            // SALES EXECUTIVE
            // ==========================================================

            string soldByQuery = @"
            SELECT TOP 1 NAME
            FROM SALESEXECUTIVE_MAST
            WHERE CODE = @SOLD_BY
              AND COMP_CODE = @COMP_CODE";

            using var soldByCmd = new SqlCommand(soldByQuery, con);

            soldByCmd.Parameters.AddWithValue("@SOLD_BY", result["soldBy"] ?? 0);
            soldByCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

            result["soldByName"] = soldByCmd.ExecuteScalar()?.ToString() ?? "";

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateData([FromBody] SaleSaudaEntryModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                var result = await _saleSaudaEntryRepository.SaveAndUpdateDataAsync(model);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
