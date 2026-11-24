using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Implementations.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class DispatchPlaningController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        public readonly IDispatchPlanningRepository _dispatchPlanningRepository;

        public DispatchPlaningController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IDispatchPlanningRepository dispatchPlanningRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _dispatchPlanningRepository = dispatchPlanningRepository;
        }

        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.GlobalVariables = globalVar;
            return View("~/Views/Sales/Transaction/DispatchPlaning/Index.cshtml");
        }

        [HttpGet]
        public IActionResult DocTypeDDL()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('SalesPlanning')  order by Name";
            var dropdownList = _dropdownService.GetDropdownList(query);
            return Json(dropdownList);
        }

        public JsonResult GetNextV_NO()
        {
            string newV_NO = "00001";
            string vType = "DISP";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                    using (SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con))
                    {
                        prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                        string lastV_NO_Query = @"SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 FROM DISPATCH_PLAN WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                        using (SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con))
                        {
                            lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vType);
                            lastVnoCmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                            lastVnoCmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                            lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

                            int nextNo = Convert.ToInt32(lastVnoCmd.ExecuteScalar());

                            string runningPart = nextNo.ToString("D5");

                            newV_NO = prefixYR + runningPart;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");

                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { v_NO = newV_NO });
        }

        [HttpGet]
        public IActionResult GetItemList(int page = 1, string search = "")
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                int pageSize = 500;
                int offset = (page - 1) * pageSize;

                var items = new List<object>();

                string query = @"
                SELECT code, shortname
                FROM item_mast
                WHERE active = 1
                AND COMP_CODE = @COMP_CODE
                AND (
                    @SEARCH = ''
                    OR shortname LIKE '%' + @SEARCH + '%'
                    OR CAST(code AS VARCHAR(50)) LIKE '%' + @SEARCH + '%'
                )
                ORDER BY name, code
                OFFSET @OFFSET ROWS
                FETCH NEXT @FETCH ROWS ONLY";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                        cmd.Parameters.AddWithValue("@SEARCH", search.Trim());
                        cmd.Parameters.AddWithValue("@OFFSET", offset);
                        cmd.Parameters.AddWithValue("@FETCH", pageSize + 1);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new
                                {
                                    id = reader["code"].ToString(),
                                    text = reader["shortname"].ToString()
                                });
                            }
                        }
                    }
                }

                bool hasMore = items.Count > pageSize;

                if (hasMore)
                {
                    items.RemoveAt(items.Count - 1);
                }

                return Json(new
                {
                    results = items,
                    pagination = new
                    {
                        more = hasMore
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetItemList: {ex.Message}");

                return StatusCode(500, new {  error = "Unable to load item list." });
            }
        }

        [HttpGet]
        public IActionResult GetTenacityList()
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                string query = @$"SELECT code, name FROM TENACITY_MAST WHERE active = 1 AND COMP_CODE = {globalVariables.PubCompCode} ORDER BY name";

                var dropdownList = _dropdownService.GetDropdownList(query);

                return Json(dropdownList);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return StatusCode(500, new { error = "Unable to load Tenacity." });
            }
        }
        
        [HttpGet]
        public IActionResult GetStatusList()
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                string query = @$"SELECT code, name FROM STATUS_MAST WHERE COMP_CODE = {globalVariables.PubCompCode} ORDER BY name";

                var dropdownList = _dropdownService.GetDropdownList(query);

                return Json(dropdownList);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return StatusCode(500, new { error = "Unable to load Status." });
            }
        }
         
        [HttpGet]
        public IActionResult GetAllPendingOrders(string sortBy = "ItemName", string vNo = "")
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // 1. Update ORDER1 Status
                    string updateOrder1 = @"
                    UPDATE o
                    SET o.status = s.status
                    FROM order1 o
                    INNER JOIN sauda s
                        ON o.sauda_no = s.v_no
                        AND o.sauda_type = s.v_type
                        AND o.comp_code = s.comp_code
                        AND o.branch_code = s.branch_code
                    WHERE o.comp_code = @COMP_CODE
                        AND o.branch_code = @BRANCH_CODE
                        AND o.v_type = 'SORD'
                        AND s.v_type = 'SAUD'
                        AND o.status = 1";
                    
                    using (SqlCommand cmd = new SqlCommand(updateOrder1, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Update ORDER2 Status and Adjust Quantity
                    string updateOrder2 = @"
                    UPDATE o2
                    SET o2.status = o1.status,
                        o2.adj_qty = 0
                    FROM order2 o2
                    INNER JOIN order1 o1
                        ON o1.v_no = o2.v_no
                        AND o1.v_type = o2.v_type
                        AND o1.comp_code = o2.comp_code
                        AND o1.branch_code = o2.branch_code
                    WHERE o1.comp_code = @COMP_CODE
                        AND o1.branch_code = @BRANCH_CODE
                        AND o1.v_type = 'SORD'
                        AND o1.status = 1";

                    using (SqlCommand cmd = new SqlCommand(updateOrder2, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Update Adjust Quantity from Sales Invoice
                    string updateAdjQty = @"
                    WITH SaleSummary AS
                    (
                        SELECT
                            s.ord_type,
                            s.ord_no,
                            s.item_code,
                            s.comp_code,
                            s.branch_code,
                            SUM(s.qty) AS total_qty
                        FROM sale2 s
                        WHERE ISNULL(s.status, 0) <> 2
                            AND s.v_type IN
                            (
                                SELECT code
                                FROM DOCTYPE_MAST
                                WHERE DOCTYPE = 'Salesinvoice'
                            )
                            AND s.comp_code = @COMP_CODE
                            AND s.branch_code = @BRANCH_CODE
                        GROUP BY
                            s.ord_type,
                            s.ord_no,
                            s.item_code,
                            s.comp_code,
                            s.branch_code
                    )
                    UPDATE o
                    SET o.adj_qty = ss.total_qty
                    FROM order2 o
                    INNER JOIN SaleSummary ss
                        ON o.v_type = ss.ord_type
                        AND o.v_no = ss.ord_no
                        AND o.item_code = ss.item_code
                        AND o.comp_code = ss.comp_code
                        AND o.branch_code = ss.branch_code
                    WHERE o.comp_code = @COMP_CODE
                        AND o.branch_code = @BRANCH_CODE
                        AND o.status = 1
                        AND o.v_type = 'SORD'";

                    using (SqlCommand cmd = new SqlCommand(updateAdjQty, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Sorting
                    string orderBy = sortBy switch
                    {
                        "ShortName" => "ORDER BY subgroup_mast.name, order2.v_no, item_mast.shortname",
                        "ItemGram" => "ORDER BY item_mast.gram_code",
                        "ItemSize" => "ORDER BY item_mast.size_code",
                        "ItemColor" => "ORDER BY item_mast.color_code",
                        _ => "ORDER BY subgroup_mast.name, item_mast.shortname"
                    };

                    // 5. Get All Pending Orders
                    string query = $@"
                    SELECT DISTINCT
                        order2.v_type AS DocType,
                        order2.v_no AS DocNo,
                        order2.v_date AS DocDate,
                        subgroup_mast.name AS PartyName,
                        item_mast.shortname AS ItemName,
                        Tenacity_mast.name AS Tenacity,
                        order2.qty AS Qty,
                        order2.rate AS Rate,
                        ISNULL(order2.adj_qty, 0) AS SaleQty,
                        ISNULL(order2.qty, 0) - ISNULL(order2.adj_qty, 0) AS BalanceQty,
                        ISNULL(DISPATCH_PLAN2.plan_qty, 0) AS PlanQty,
                        order1.party_code AS PartyCode,
                        order2.item_code AS ItemCode,
                        item_mast.size_code AS SizeCode,
                        item_mast.color_code AS ColorCode,
                        order2.tenacity_code AS TenacityCode,
                        'OK' AS StatusName,
                        1 AS StatusCode
                    FROM order2
                    LEFT JOIN order1
                        ON order1.year_code = order2.year_code
                        AND order1.comp_code = order2.comp_code
                        AND order1.branch_code = order2.branch_code
                        AND order1.v_type = order2.v_type
                        AND order1.v_no = order2.v_no
                    LEFT JOIN subgroup_mast
                        ON subgroup_mast.code = order1.party_code
                        AND subgroup_mast.comp_code = order1.comp_code
                    LEFT JOIN item_mast
                        ON item_mast.code = order2.item_code
                        AND order2.comp_code = item_mast.comp_code
                    LEFT JOIN item_mgroup
                        ON item_mgroup.code = item_mast.mgroup_code
                        AND item_mgroup.comp_code = item_mast.comp_code
                    LEFT JOIN DISPATCH_PLAN2
                        ON DISPATCH_PLAN2.V_NO = @V_NO
                        AND DISPATCH_PLAN2.V_TYPE = 'DISP'
                        AND DISPATCH_PLAN2.doc_no = order2.v_no
                        AND DISPATCH_PLAN2.doc_type = order2.v_type
                        AND DISPATCH_PLAN2.ITEM_CODE = order2.item_code
                        AND order2.comp_code = DISPATCH_PLAN2.comp_code
                    LEFT JOIN doctype_mast
                        ON doctype_mast.code = order2.v_type
                    LEFT JOIN Tenacity_mast
                        ON order2.Tenacity_code = Tenacity_mast.code
                        AND order2.comp_code = Tenacity_mast.comp_code
                    WHERE item_mgroup.mgroup_type IN ('Finish', 'SFG')
                        AND ISNULL(order2.qty, 0) > ISNULL(order2.adj_qty, 0)
                        AND order1.status = 1
                        AND order1.comp_code = @COMP_CODE
                        AND order1.branch_code = @BRANCH_CODE
                        AND doctype_mast.doctype IN ('SalesOrder')
                        AND item_mast.code <> 0
                    {orderBy}";

                    var result = new List<object>();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", vNo ?? "");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new
                                {
                                    docType = reader["DocType"].ToString(),
                                    docNo = reader["DocNo"].ToString(),
                                    docDate = Convert.ToDateTime(reader["DocDate"]).ToString("yyyy-MM-dd"),
                                    partyName = reader["PartyName"].ToString(),
                                    itemName = reader["ItemName"].ToString(),
                                    itemCode = reader["ItemCode"].ToString(),
                                    tenacity = reader["Tenacity"].ToString(),
                                    tenacityCode = reader["TenacityCode"].ToString(),
                                    qty = Convert.ToDecimal(reader["Qty"]),
                                    rate = Convert.ToDecimal(reader["Rate"]),
                                    saleQty = Convert.ToDecimal(reader["SaleQty"]),
                                    balanceQty = Convert.ToDecimal(reader["BalanceQty"]),
                                    planQty = Convert.ToDecimal(reader["PlanQty"]),
                                    partyCode = reader["PartyCode"].ToString(),
                                    sizeCode = reader["SizeCode"].ToString(),
                                    colorCode = reader["ColorCode"].ToString(),
                                    statusName = reader["StatusName"].ToString(),
                                    statusCode = reader["StatusCode"].ToString()
                                });
                            }
                        }
                    }
                    
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return StatusCode(500, new { error = "Unable to load pending orders." });
            }
        }

        [HttpGet]
        public IActionResult GetFillData(DateTime vDate, string sortBy = "ItemName")
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string orderBy = sortBy switch
                {
                    "ShortName" => "ORDER BY item_mast.shortname",
                    "ItemGram" => "ORDER BY item_mast.gram_code",
                    "ItemSize" => "ORDER BY item_mast.size_code",
                    "ItemColor" => "ORDER BY item_mast.color_code",
                    _ => "ORDER BY item_mast.name"
                };

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string sql = @"DELETE FROM temp_inv1 WHERE wsid = @WSID AND userid = @USER_ID;";

                    // =========================================
                    // Company 2 / 5
                    // =========================================

                    if (gv.PubCompCode == "2" || gv.PubCompCode == "5")
                    {
                        sql += @"

                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, to_dept, prod_mtr, avg_mtr, avg_gram, item_code, open_qty, open_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, place_code, 0, 0, 0, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN
                            ('FPRC','FFRC','FLRC','FPDR','FSRC','STAD','FPJR')
                            AND production2.v_date < @V_DATE
                        GROUP BY place_code, item_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, to_dept, prod_mtr, avg_mtr, avg_gram, item_code, open_qty, open_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE,  place_code, 0, 0, 0, item_code, -COUNT(*), -SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN
                            ('FPIS','FFIS','FLIS','FSIS','FPJI')
                            AND production2.v_date < @V_DATE
                        GROUP BY place_code, item_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, to_dept, prod_mtr, avg_mtr, avg_gram, item_code, recd_qty, recd_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, place_code, SUM(mtr), AVG(avg_qty), AVG(gram),  item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN ('FPRC')
                            AND production2.v_date = @V_DATE
                        GROUP BY place_code, item_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, to_dept, prod_mtr, avg_mtr, avg_gram, item_code, issu_qty, issu_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, place_code, 0, 0, 0, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN ('FPIS')
                            AND production2.v_date = @V_DATE
                        GROUP BY place_code, item_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, to_dept, prod_mtr, avg_mtr, avg_gram, item_code, adjr_qty, adjr_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, place_code, 0, 0, 0, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN
                            ('FLRC','FFRC','FPDR','FSRC','STAD','FPJR')
                            AND production2.v_date = @V_DATE
                        GROUP BY place_code, item_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, to_dept, prod_mtr, avg_mtr, avg_gram, item_code, adji_qty, adji_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, place_code, 0, 0, 0, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN
                            ('FLIS','FFIS','FSIS','FPJI')
                            AND production2.v_date = @V_DATE
                        GROUP BY place_code, item_code;

                        DELETE FROM temp_stock  WHERE wsid = @WSID AND userid = @USER_ID;

                        INSERT INTO temp_stock
                        (
                            wsid, userid, comp_code, place_code, prod_mtr, avg_mtr, avg_gram, item_code, open_qty, open_wgt, recd_qty, recd_wgt,
                            adjr_qty, adjr_wgt, issu_qty, issu_wgt, adji_qty, adji_wgt
                        )
                        SELECT
                            wsid, userid, comp_code, 0, SUM(prod_mtr), SUM(avg_mtr), SUM(avg_gram),  item_code, SUM(open_qty), SUM(open_amt),
                            SUM(recd_qty), SUM(recd_amt), SUM(adjr_qty), SUM(adjr_amt), SUM(issu_qty), SUM(issu_amt), SUM(adji_qty), SUM(adji_amt)
                        FROM temp_inv1
                        WHERE wsid = @WSID
                            AND userid = @USER_ID
                            AND comp_code = @COMP_CODE
                        GROUP BY wsid, userid, comp_code, item_code;
                        ";
                    }
                    else
                    {
                        // =========================================
                        // Other Companies
                        // =========================================

                        sql += @"

                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code,tenacity_code, status_code, item_code, open_qty, open_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, tenacity_code, status_code, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN
                            ('FPRC','FFRC','FLRC','FPDR','FSRC','STAD')
                            AND production2.v_date < @V_DATE
                        GROUP BY item_code, tenacity_code, status_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, tenacity_code, status_code, item_code, open_qty, open_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, tenacity_code, status_code, item_code, -COUNT(*), -SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN
                            ('FPIS','FFIS','FLIS','FSIS')
                            AND production2.v_date < @V_DATE
                        GROUP BY item_code, tenacity_code, status_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, tenacity_code, status_code, item_code, recd_qty, recd_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, tenacity_code, status_code, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN ('FPRC')
                            AND production2.v_date = @V_DATE
                        GROUP BY item_code, tenacity_code, status_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, tenacity_code, status_code, item_code, issu_qty, issu_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, tenacity_code, status_code, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN ('FPIS')
                            AND production2.v_date = @V_DATE
                        GROUP BY item_code, tenacity_code, status_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, tenacity_code, status_code, item_code, adjr_qty, adjr_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, tenacity_code, status_code, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN
                            ('FLRC','FFRC','FPDR','FSRC','STAD')
                            AND production2.v_date = @V_DATE
                        GROUP BY item_code, tenacity_code, status_code;


                        INSERT INTO temp_inv1
                        (
                            wsid, userid, comp_code, tenacity_code, status_code, item_code, adji_qty, adji_amt
                        )
                        SELECT
                            @WSID, @USER_ID, @COMP_CODE, tenacity_code, status_code, item_code, COUNT(*), SUM(qty)
                        FROM production2
                        LEFT JOIN item_mast
                            ON item_mast.code = production2.item_code
                            AND item_mast.comp_code = production2.comp_code
                        LEFT JOIN item_group
                            ON item_group.code = item_mast.group_code
                            AND item_mast.comp_code = item_group.comp_code
                        LEFT JOIN doctype_mast
                            ON doctype_mast.code = production2.v_type
                        WHERE production2.comp_code = @COMP_CODE
                            AND production2.branch_code = @BRANCH_CODE
                            AND production2.v_type IN
                            ('FLIS','FFIS','FSIS')
                            AND production2.v_date = @V_DATE
                        GROUP BY item_code, tenacity_code, status_code;


                        DELETE FROM temp_stock
                        WHERE wsid = @WSID
                            AND userid = @USER_ID;


                        INSERT INTO temp_stock
                        (
                            wsid, userid, comp_code, tenacity_code, status_code, item_code, open_qty, open_wgt, recd_qty, recd_wgt,
                            adjr_qty, adjr_wgt, issu_qty, issu_wgt, adji_qty, adji_wgt, srno
                        )
                        SELECT
                            wsid, userid, comp_code, 1, 1, item_code, ISNULL(SUM(open_qty), 0), ISNULL(SUM(open_amt), 0), ISNULL(SUM(recd_qty), 0),
                            ISNULL(SUM(recd_amt), 0), ISNULL(SUM(adjr_qty), 0), ISNULL(SUM(adjr_amt), 0), ISNULL(SUM(issu_qty), 0), ISNULL(SUM(issu_amt), 0),
                            ISNULL(SUM(adji_qty), 0), ISNULL(SUM(adji_amt), 0), ROW_NUMBER() OVER (ORDER BY item_code)
                        FROM temp_inv1
                        WHERE wsid = @WSID
                            AND userid = @USER_ID
                            AND comp_code = @COMP_CODE
                        GROUP BY wsid, userid, comp_code, item_code;
                        ";
                    }

                    // =========================================
                    // Final Stock Data                         
                    // =========================================

                    sql += $@"
                    SELECT
                        item_mast.code AS ItemCode,
                        item_mast.shortname AS ItemName,
                        ISNULL(temp_stock.open_qty, 0) AS OpenQty,
                        ISNULL(temp_stock.open_wgt, 0) AS OpenWgt,
                        0 AS StatusCode,
                        0 AS TenacityCode,
                        '' AS Tenacity,
                        '' AS Status
                    FROM temp_stock
                    LEFT JOIN item_mast
                        ON temp_stock.comp_code = item_mast.comp_code
                        AND temp_stock.item_code = item_mast.code
                    WHERE temp_stock.wsid = @WSID
                        AND temp_stock.userid = @USER_ID
                        AND ISNULL(temp_stock.open_wgt, 0) > 0
                    {orderBy};
                    ";

                    var result = new List<object>();

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.CommandTimeout = 120;

                        cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@USER_ID", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.Add("@V_DATE", System.Data.SqlDbType.DateTime).Value = vDate.Date;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new
                                {
                                    itemCode = reader["ItemCode"].ToString(),
                                    itemName = reader["ItemName"].ToString(),

                                    openQty = Convert.ToDecimal(reader["OpenQty"]),
                                    stock = Convert.ToDecimal(reader["OpenWgt"]),

                                    tenacityCode = reader["TenacityCode"].ToString(),
                                    tenacity = reader["Tenacity"].ToString(),

                                    statusCode = reader["StatusCode"].ToString(),
                                    status = reader["Status"].ToString(),

                                    baleNo = ""
                                });
                            }
                        }
                    }

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetFillData: {ex.Message}");

                return StatusCode(500, new
                {
                    error = "Unable to fill stock data.",
                    details = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateData([FromForm] DispatchPlanningModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                var result = await _dispatchPlanningRepository.SaveAndUpdateDataAsync(model);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadEditData(int vNo, string vType)
        {
            try
            {
                var result = await _dispatchPlanningRepository.LoadEditDataAsync(vNo, vType);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetDispatchPlan2Data(int vNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"
                    SELECT
                        a.V_TYPE,
                        a.V_NO,
                        a.V_DATE AS VDate,
                        a.ITEM_CODE AS ItemCode,
                        b.SHORTNAME AS ItemName,
                        a.TENACITY_CODE AS TenacityCode,
                        d.NAME AS Tenacity,
                        a.STATUS_CODE AS StatusCode,
                        e.NAME AS Status,
                        a.AC_CODE AS PartyCode,
                        c.NAME AS Party,
                        a.PLAN_QTY AS PlanQty
                    FROM DISPATCH_PLAN2 a
                    LEFT JOIN ITEM_MAST b
                        ON a.ITEM_CODE = b.CODE
                        AND a.COMP_CODE = b.COMP_CODE
                    LEFT JOIN SUBGROUP_MAST c
                        ON a.AC_CODE = c.CODE
                        AND a.COMP_CODE = c.COMP_CODE
                    LEFT JOIN TENACITY_MAST d
                        ON a.TENACITY_CODE = d.CODE
                        AND a.COMP_CODE = d.COMP_CODE
                    LEFT JOIN STATUS_MAST e
                        ON a.STATUS_CODE = e.CODE
                        AND a.COMP_CODE = e.COMP_CODE
                    WHERE a.V_TYPE = 'DISP'
                        AND a.V_NO = @V_NO
                        AND a.COMP_CODE = @COMP_CODE
                        AND a.BRANCH_CODE = @BRANCH_CODE
                        AND a.YEAR_CODE = @YEAR_CODE
                    ORDER BY b.SHORTNAME";

                    var result = new List<object>();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Add(new
                                {
                                    v_TYPE = reader["V_TYPE"].ToString(),
                                    v_NO = reader["V_NO"].ToString(),
                                    vDate = reader["VDate"] == DBNull.Value ? ""  : Convert.ToDateTime(reader["VDate"]).ToString("yyyy-MM-dd"),

                                    itemCode = reader["ItemCode"].ToString(),
                                    itemName = reader["ItemName"].ToString(),

                                    tenacityCode = reader["TenacityCode"].ToString(),
                                    tenacity = reader["Tenacity"].ToString(),

                                    statusCode = reader["StatusCode"].ToString(),
                                    status = reader["Status"].ToString(),

                                    partyCode = reader["PartyCode"].ToString(),
                                    partyName = reader["Party"].ToString(),

                                    planQty = reader["PlanQty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["PlanQty"])
                                });
                            }
                        }
                    }

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetDispatchPlan2Data Error: {ex.Message}");

                return StatusCode(500, new {  success = false,  message = "Unable to load Dispatch Plan details.", error = ex.Message });
            }
        }


    }
}
