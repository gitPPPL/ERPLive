using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Implementations.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class DeliveryOrderController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IDeliveryOrderRepository _deliveryOrderRepository;

        public DeliveryOrderController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IDeliveryOrderRepository deliveryOrderRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _deliveryOrderRepository = deliveryOrderRepository;
        }

        public IActionResult Index(string mode = "DOrder")
        {
            ViewBag.FormMode = mode;

            return View("~/Views/Sales/Transaction/DeliveryOrder/Index.cshtml");
        }

        [HttpGet]
        public IActionResult DocTypeDDL()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('DeliveryOrder')  order by Name";
            var dropdownList = _dropdownService.GetDropdownList(query);
            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult DocStatusDDL()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
            var dropdownList = _dropdownService.GetDropdownList(query);
            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult DocTransportDDL()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"select code,ltrim(rtrim(name))'Name',TDS_PER from TRANSPORT_MAST where Active=1 and comp_code= {globalVariables.PubCompCode}";
            var dropdownList = _dropdownService.GetDropdownList(query);
            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult FoamTypeDDL()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code,Name from FORM_MAST where comp_code= {globalVariables.PubCompCode} order by Name";
            var dropdownList = _dropdownService.GetDropdownList(query);
            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult ProdTypeDDL()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = $@"
            SELECT DISTINCT
                SALE_GROUP AS Code,
                SALE_GROUP AS Name
            FROM ITEM_GROUP
            WHERE COMP_CODE = {globalVariables.PubCompCode}
            ORDER BY SALE_GROUP";

            var dropdownList = _dropdownService.GetDropdownList(query);

            return Json(dropdownList);
        }

        public JsonResult GetNextV_NO(string vType)
        {
            string newV_NO = "00001";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 FROM DO1 WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);

                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    lastVnoCmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

                    object result = lastVnoCmd.ExecuteScalar();

                    int nextNo = Convert.ToInt32(result);

                    string runningPart = nextNo.ToString("D5");

                    newV_NO = prefixYR + runningPart;

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
        public IActionResult PartyNameDDL(string search = "", int page = 1, int pageSize = 500)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            int offset = (page - 1) * pageSize;

            string query = $@"
            SELECT TOP {pageSize}
                Code AS id,
                Name AS text
            FROM SUBGROUP_MAST
            WHERE COMP_CODE = {globalVariables.PubCompCode}
              AND ACTIVE = 1
              AND Name LIKE '%{search.Replace("'", "''")}%'
              AND Code NOT IN
              (
                  SELECT TOP {offset} Code
                  FROM SUBGROUP_MAST
                  WHERE COMP_CODE = {globalVariables.PubCompCode}
                    AND ACTIVE = 1
                    AND Name LIKE '%{search.Replace("'", "''")}%'
                  ORDER BY Name
              )
            ORDER BY Name";

            var dropdownList = _dropdownService.GetDropdownList(query);

            return Json(new
            {
                results = dropdownList,
                pagination = new
                {
                    more = dropdownList.Count == pageSize
                }
            });
        }

        [HttpGet]
        public IActionResult GetPartyAddressList(int partyCode)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT
                a.Address_Id,
                a.Add1,
                a.Add2,
                a.Add3,
                a.GSTIN,
                a.City_Code,
                b.Name AS City,
                a.State_Code,
                c.Name AS State,
                a.Country_Code,
                d.Name AS Country,
                a.Pincode,
                a.Distance
            FROM Subgroup_Address a
            LEFT JOIN CITY_MAST b
                ON a.CITY_CODE = b.Code
            LEFT JOIN STATE_MAST c
                ON a.STATE_CODE = c.Code
            LEFT JOIN COUNTRY_MAST d
                ON a.COUNTRY_CODE = d.Code
            WHERE a.COMP_CODE = @COMP_CODE
              AND a.Code = @PARTY_CODE
            ORDER BY a.Address_Id";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    addressId = reader["Address_Id"],
                    add1 = reader["Add1"],
                    add2 = reader["Add2"],
                    add3 = reader["Add3"],
                    gstin = reader["GSTIN"],
                    cityCode = reader["City_Code"],
                    city = reader["City"],
                    stateCode = reader["State_Code"],
                    state = reader["State"],
                    countryCode = reader["Country_Code"],
                    country = reader["Country"],
                    pincode = reader["Pincode"],
                    distance = reader["Distance"]
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetPartyCreditDetails(int partyCode)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT
                a.CODE AS Code,
                LTRIM(RTRIM(a.NAME)) AS P_name,
                ISNULL(a.ADD1, '') AS Add1,
                ISNULL(a.ADD2, '') AS Add2,
                ISNULL(a.ADD3, '') AS Add3,
                ISNULL(a.CITY_CODE, '') AS C_code,
                ISNULL(c.NAME, '') AS C_name,
                ISNULL(a.AGENT_CODE, '') AS agent_code,
                ISNULL(d.NAME, '') AS agent_name,
                a.GSTIN,
                ISNULL(e.Drbal, 0) AS Drbal,
                ISNULL(e.crbal, 0) AS crbal,
                ISNULL(a.CREDIT_LIMIT, 0) AS CREDIT_LIMIT,
                a.PINCODE,
                a.Distance,
                ISNULL(a.BILL_TYPE, '') AS BILL_TYPE,
                ISNULL(a.Disc_per, 0) AS discper
            FROM SUBGROUP_MAST a
            LEFT JOIN CITY_MAST c
                ON c.CODE = a.CITY_CODE
            LEFT JOIN SUBGROUP_MAST d
                ON d.CODE = a.AGENT_CODE
                AND d.COMP_CODE = a.COMP_CODE
            LEFT JOIN vwLedger e
                ON a.CODE = e.CODE
                AND a.COMP_CODE = e.COMP_CODE
            WHERE a.Active = 1
                AND a.COMP_CODE = @COMP_CODE
                AND a.CODE = @PARTY_CODE";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return Json(new
                {
                    code = reader["Code"],
                    partyName = reader["P_name"],
                    creditLimit = reader["CREDIT_LIMIT"],
                    drBal = reader["Drbal"],
                    crBal = reader["crbal"],
                    agentCode = reader["agent_code"],
                    agentName = reader["agent_name"],
                    billType = reader["BILL_TYPE"],
                    discPer = reader["discper"]
                });
            }

            return Json(null);
        }

        [HttpGet]
        public IActionResult BrokerDDL(string search = "")
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT 
                Code AS id,
                LTRIM(RTRIM(Name)) AS text
            FROM SUBGROUP_MAST
            WHERE NATURE LIKE 'Broker'
              AND Active = 1
              AND COMP_CODE = @COMP_CODE
              AND LTRIM(RTRIM(Name)) LIKE @SEARCH
            ORDER BY Name";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            cmd.Parameters.AddWithValue("@SEARCH", $"%{search}%");

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    value = reader["id"],
                    text = reader["text"]
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult TaxDDL(int stationCode, string search = "")
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            // Get Station State
            string stateQuery = @"
            SELECT State_Code
            FROM CITY_MAST
            WHERE Code = @STATION_CODE";

            string stationStateCode = "";

            using (var stateCmd = new SqlCommand(stateQuery, con))
            {
                stateCmd.Parameters.AddWithValue("@STATION_CODE", stationCode);

                stationStateCode = stateCmd.ExecuteScalar()?.ToString() ?? "";
            }

            // Get Company State
            string companyStateQuery = @"
            SELECT STATE_CODE
            FROM COMP_MAST
            WHERE CODE = @COMP_CODE";

            string companyStateCode = "";

            using (var companyCmd = new SqlCommand(companyStateQuery, con))
            {
                companyCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

                companyStateCode = companyCmd.ExecuteScalar()?.ToString() ?? "";
            }

            string stateType;

            if (companyStateCode == stationStateCode)
            {
                stateType = "'Import','Local'";
            }
            else
            {
                stateType = "'Import','Central'";
            }

            string query = $@"
            SELECT
                Code AS id,
                LTRIM(RTRIM(Name)) AS text,
                CGST_PER,
                SGST_PER,
                IGST_PER,
                TDS_PER,
                TCS_PER,
                OTH_PER
            FROM TAX_MAST
            WHERE ACTIVE = 1
              AND T_Type IN ({stateType})
              AND LTRIM(RTRIM(Name)) LIKE @SEARCH
            ORDER BY Name";

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@SEARCH", $"{search}%");

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    id = reader["id"],
                    text = reader["text"],
                    cgstPer = reader["CGST_PER"],
                    sgstPer = reader["SGST_PER"],
                    igstPer = reader["IGST_PER"],
                    tdsPer = reader["TDS_PER"],
                    tcsPer = reader["TCS_PER"],
                    othPer = reader["OTH_PER"]
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult StationDDL(string search = "")
        {
            string query = @"
            SELECT
                Code AS id,
                LTRIM(RTRIM(Name)) AS text
            FROM CITY_MAST
            WHERE LTRIM(RTRIM(Name)) LIKE @SEARCH
            ORDER BY Name";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@SEARCH", $"{search}%");

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    value = reader["id"],
                    text = reader["text"]
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetPartyLastSaleDetails(int partyCode)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT TOP 1
                a.FORM_CODE,
                a.TAX_CODE,
                a.TPT_DISTANCE,
                b.NAME AS TaxName
            FROM SALE1 a
            LEFT JOIN TAX_MAST b
                ON a.TAX_CODE = b.CODE
            WHERE a.COMP_CODE = @COMP_CODE
              AND a.V_TYPE = 'SAGT'
              AND a.BILL_CODE = @PARTY_CODE
            ORDER BY a.V_DATE DESC, a.V_NO DESC";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);

            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return Json(new
                {
                    formCode = reader["FORM_CODE"] == DBNull.Value ? "" : reader["FORM_CODE"].ToString(),
                    taxCode = reader["TAX_CODE"] == DBNull.Value ? "" : reader["TAX_CODE"].ToString(),
                    taxName = reader["TaxName"] == DBNull.Value ? "" : reader["TaxName"].ToString(),
                    tptDistance = reader["TPT_DISTANCE"] == DBNull.Value ? "" : reader["TPT_DISTANCE"].ToString()
                });
            }

            return Json(null);
        }

        [HttpGet]
        public IActionResult GetStock()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                // =========================================================
                // 1. DELETE OLD TEMP_INV1
                // =========================================================


                string deleteTempInv1 = @"DELETE FROM temp_inv1 WHERE wsid = @WSID AND userid = @USER_ID";

                using (var cmd = new SqlCommand(deleteTempInv1, con))
                {
                    cmd.Parameters.AddWithValue("@WSID", globalVariables.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@USER_ID", globalVariables.PubUserId);

                    cmd.ExecuteNonQuery();
                }

                // =========================================================
                // COMPANY CODE 2 OR 5
                // =========================================================

                if (globalVariables.PubCompCode == "2" || globalVariables.PubCompCode == "5")
                {
                    // -----------------------------------------------------
                    // OPENING RECEIPT
                    // -----------------------------------------------------

                    string query1 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept, prod_mtr, avg_mtr, avg_gram, item_code, open_qty, open_amt
                    )
                    SELECT
                        @WSID, @USER_ID,@COMP_CODE, place_code, 0, 0, 0, item_code, COUNT(*), SUM(qty)
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
                        ('FPRC','FFRC','FLRC','FPJR','FPDR','FSRC','STAD')
                      AND v_date < @V_DATE
                    GROUP BY place_code, item_code";

                    ExecuteStockQuery(con, query1, globalVariables);


                    // -----------------------------------------------------
                    // OPENING ISSUE
                    // -----------------------------------------------------

                    string query2 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        prod_mtr, avg_mtr, avg_gram,
                        item_code, open_qty, open_amt
                    )
                    SELECT
                        @WSID, @USER_ID, @COMP_CODE, place_code,
                        0, 0, 0, item_code, -COUNT(*),  -SUM(qty)
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
                        ('FPIS','FFIS','FLIS','FPJI','FSIS')
                      AND v_date < @V_DATE
                    GROUP BY place_code, item_code";

                    ExecuteStockQuery(con, query2, globalVariables);

                    // -----------------------------------------------------
                    // RECEIPT TODAY
                    // -----------------------------------------------------

                    string query3 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept, prod_mtr, avg_mtr, avg_gram, item_code, recd_qty, recd_amt
                    )
                    SELECT
                        @WSID, @USER_ID, @COMP_CODE, place_code,  SUM(mtr), AVG(avg_qty), AVG(gram), item_code, COUNT(*), SUM(qty)
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
                      AND v_date BETWEEN @V_DATE AND @V_DATE
                    GROUP BY place_code, item_code";

                    ExecuteStockQuery(con, query3, globalVariables);

                    // -----------------------------------------------------
                    // ISSUE TODAY
                    // -----------------------------------------------------

                    string query4 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        prod_mtr, avg_mtr, avg_gram,
                        item_code, issu_qty, issu_amt
                    )
                    SELECT
                        @WSID, @USER_ID, @COMP_CODE, place_code,
                        0, 0, 0, item_code, COUNT(*), SUM(qty)
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
                      AND v_date BETWEEN @V_DATE AND @V_DATE
                    GROUP BY place_code, item_code";

                    ExecuteStockQuery(con, query4, globalVariables);


                    // -----------------------------------------------------
                    // RECEIPT ADJUSTMENT
                    // -----------------------------------------------------

                    string query5 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        prod_mtr, avg_mtr, avg_gram,
                        item_code, adjr_qty, adjr_amt
                    )
                    SELECT
                        @WSID, @USER_ID, @COMP_CODE, place_code,
                        0, 0, 0, item_code, COUNT(*), SUM(qty)
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
                        ('FLRC','FPJR','FFRC','FPDR','FSRC','STAD')
                      AND v_date BETWEEN @V_DATE AND @V_DATE
                    GROUP BY place_code, item_code";

                    ExecuteStockQuery(con, query5, globalVariables);

                    // -----------------------------------------------------
                    // ISSUE ADJUSTMENT
                    // -----------------------------------------------------

                    string query6 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        prod_mtr, avg_mtr, avg_gram,
                        item_code, adji_qty, adji_amt
                    )
                    SELECT
                        @WSID, @USER_ID, @COMP_CODE, place_code,
                        0, 0, 0, item_code, COUNT(*), SUM(qty)
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
                        ('FLIS','FPJI','FFIS','FSIS')
                      AND v_date BETWEEN @V_DATE AND @V_DATE
                    GROUP BY place_code, item_code";

                    ExecuteStockQuery(con, query6, globalVariables);

                    // =====================================================
                    // DELETE TEMP_STOCK
                    // =====================================================

                    string deleteTempStock = @"
                    DELETE FROM temp_stock
                    WHERE wsid = @WSID
                    AND userid = @USER_ID";

                    using (var cmd = new SqlCommand(deleteTempStock, con))
                    {
                        cmd.Parameters.AddWithValue("@WSID", globalVariables.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@USER_ID", globalVariables.PubUserId);
                        cmd.ExecuteNonQuery();
                    }


                    // =====================================================
                    // INSERT TEMP_STOCK
                    // =====================================================

                    string insertTempStock = @"
                    INSERT INTO temp_stock
                    (
                        wsid, userid, comp_code, place_code, prod_mtr, avg_mtr,
                        avg_gram, item_code, open_qty, open_wgt, recd_qty,
                        recd_wgt,  adjr_qty, adjr_wgt, issu_qty, issu_wgt,
                        adji_qty, adji_wgt
                    )
                    SELECT
                        wsid, userid, comp_code, 0, SUM(prod_mtr), SUM(avg_mtr),
                        SUM(avg_gram), item_code, SUM(open_qty), SUM(open_amt),
                        SUM(recd_qty), SUM(recd_amt), SUM(adjr_qty), SUM(adjr_amt),
                        SUM(issu_qty), SUM(issu_amt), SUM(adji_qty), SUM(adji_amt)
                    FROM temp_inv1
                    WHERE wsid = @WSID
                      AND userid = @USER_ID
                      AND comp_code = @COMP_CODE
                    GROUP BY
                        wsid,
                        userid,
                        comp_code,
                        item_code";

                    ExecuteStockQuery(con, insertTempStock, globalVariables);
                }
                else
                {
                    // =====================================================
                    // OTHER COMPANIES
                    // =====================================================

                    string query1 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        tenacity_code, status_code, lot_no,
                        item_code, open_qty, open_amt
                    )
                    SELECT
                        @WSID,
                        @USER_ID,
                        @COMP_CODE,
                        place_code,
                        tenacity_code,
                        status_code,
                        lot_no,
                        item_code,
                        COUNT(*),
                        SUM(qty)
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
                        ('FPRC','FFRC','FLRC','FPJR','FPDR')
                      AND v_date < @V_DATE
                    GROUP BY
                        place_code,
                        item_code,
                        tenacity_code,
                        status_code,
                        lot_no";

                    ExecuteStockQuery(con, query1, globalVariables);


                    string query2 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept, tenacity_code, status_code, lot_no, item_code, open_qty, open_amt
                    )
                    SELECT
                        @WSID, @USER_ID, @COMP_CODE, place_code, tenacity_code, status_code,  lot_no, item_code, -COUNT(*), -SUM(qty)
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
                        ('FPIS','FFIS','FLIS','FPJI')
                      AND v_date < @V_DATE
                    GROUP BY
                        place_code,
                        item_code,
                        tenacity_code,
                        status_code,
                        lot_no";

                    ExecuteStockQuery(con, query2, globalVariables);

                    string query3 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        tenacity_code, status_code, lot_no,
                        item_code, recd_qty, recd_amt
                    )
                    SELECT
                        @WSID,
                        @USER_ID,
                        @COMP_CODE,
                        place_code,
                        tenacity_code,
                        status_code,
                        lot_no,
                        item_code,
                        COUNT(*),
                        SUM(qty)
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
                      AND v_date BETWEEN @V_DATE AND @V_DATE
                    GROUP BY
                        place_code,
                        item_code,
                        tenacity_code,
                        status_code,
                        lot_no";

                    ExecuteStockQuery(con, query3, globalVariables);


                    string query4 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        tenacity_code, status_code, lot_no,
                        item_code, issu_qty, issu_amt
                    )
                    SELECT
                        @WSID,
                        @USER_ID,
                        @COMP_CODE,
                        place_code,
                        tenacity_code,
                        status_code,
                        lot_no,
                        item_code,
                        COUNT(*),
                        SUM(qty)
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
                      AND v_date BETWEEN @V_DATE AND @V_DATE
                    GROUP BY
                        place_code,
                        item_code,
                        tenacity_code,
                        status_code,
                        lot_no";

                    ExecuteStockQuery(con, query4, globalVariables);


                    string query5 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        tenacity_code, status_code, lot_no,
                        item_code, adjr_qty, adjr_amt
                    )
                    SELECT
                        @WSID,
                        @USER_ID,
                        @COMP_CODE,
                        place_code,
                        tenacity_code,
                        status_code,
                        lot_no,
                        item_code,
                        COUNT(*),
                        SUM(qty)
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
                        ('FLRC','FPJR','FFRC','FPDR')
                      AND v_date BETWEEN @V_DATE AND @V_DATE
                    GROUP BY
                        place_code,
                        item_code,
                        tenacity_code,
                        status_code,
                        lot_no";

                    ExecuteStockQuery(con, query5, globalVariables);

                    string query6 = @"
                    INSERT INTO temp_inv1
                    (
                        wsid, userid, comp_code, to_dept,
                        tenacity_code, status_code, lot_no,
                        item_code, adji_qty, adji_amt
                    )
                    SELECT
                        @WSID, @USER_ID, @COMP_CODE, place_code, tenacity_code,
                        status_code, lot_no,  item_code, COUNT(*), SUM(qty)
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
                        ('FLIS','FPJI','FFIS')
                      AND v_date BETWEEN @V_DATE AND @V_DATE
                    GROUP BY
                        place_code,
                        item_code,
                        tenacity_code,
                        status_code,
                        lot_no";

                    ExecuteStockQuery(con, query6, globalVariables);

                    // -----------------------------------------------------
                    // UPDATE DEPARTMENT
                    // -----------------------------------------------------

                    string updateDept = @"
                    UPDATE temp_inv1
                    SET to_dept = 1
                    WHERE wsid = @WSID
                      AND userid = @USER_ID
                      AND comp_code = @COMP_CODE
                      AND (to_dept = 0 OR to_dept IS NULL)";

                    ExecuteStockQuery(con, updateDept, globalVariables);

                    // -----------------------------------------------------
                    // DELETE TEMP_STOCK
                    // -----------------------------------------------------

                    string deleteTempStock = @"
                    DELETE FROM temp_stock
                    WHERE wsid = @WSID
                      AND userid = @USER_ID";

                    using (var cmd = new SqlCommand(deleteTempStock, con))
                    {
                        cmd.Parameters.AddWithValue("@WSID", globalVariables.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@USER_ID", globalVariables.PubUserId);

                        cmd.ExecuteNonQuery();
                    }

                    // -----------------------------------------------------
                    // INSERT TEMP_STOCK
                    // -----------------------------------------------------

                    string insertTempStock = @"
                    INSERT INTO temp_stock
                    (
                        wsid, userid, comp_code, place_code, tenacity_code, status_code,
                        lot_no, item_code, open_qty, open_wgt, recd_qty,  recd_wgt, adjr_qty,
                        adjr_wgt, issu_qty, issu_wgt, adji_qty, adji_wgt
                    )
                    SELECT
                        wsid, userid, comp_code, to_dept, tenacity_code, status_code,
                        lot_no, item_code, SUM(open_qty), SUM(open_amt), SUM(recd_qty),
                        SUM(recd_amt), SUM(adjr_qty), SUM(adjr_amt), SUM(issu_qty),
                        SUM(issu_amt), SUM(adji_qty), SUM(adji_amt)
                    FROM temp_inv1
                    WHERE wsid = @WSID
                      AND userid = @USER_ID
                      AND comp_code = @COMP_CODE
                    GROUP BY
                        wsid,
                        userid,
                        comp_code,
                        to_dept,
                        tenacity_code,
                        status_code,
                        lot_no,
                        item_code";

                    ExecuteStockQuery(con, insertTempStock, globalVariables);
                }

                return Json(new
                {
                    success = true,
                    message = "Stock updated successfully"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"GetStock Error: {ex.Message}");

                return Json(new
                {
                    success = false,
                    message = "Failed to update stock",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetItemStock()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = @"
            SELECT 
                LTRIM(RTRIM(a.NAME)) AS Name,
                a.CODE,
                b.mgroup_type,
                SUM(ISNULL(c.open_wgt, 0))
                + SUM(ISNULL(c.recd_wgt, 0))
                + SUM(ISNULL(c.adjr_wgt, 0))
                - SUM(ISNULL(c.issu_wgt, 0))
                - SUM(ISNULL(c.adji_wgt, 0)) AS StockQty
            FROM ITEM_MAST a
            LEFT JOIN ITEM_MGROUP b 
                ON b.CODE = a.MGROUP_CODE 
                AND b.COMP_CODE = a.COMP_CODE
            LEFT JOIN Temp_Stock c 
                ON a.CODE = c.item_code 
                AND a.COMP_CODE = c.comp_code 
                AND c.userid = @USER_ID
            WHERE a.Active = 1
                AND a.comp_code = @COMP_CODE
            GROUP BY 
                a.NAME,
                a.CODE,
                b.mgroup_type
            ORDER BY a.NAME";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@USER_ID", globalVariables.PubUserId);
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    code = reader["CODE"],
                    name = reader["Name"],
                    mgroupType = reader["mgroup_type"],
                    stockQty = reader["StockQty"]
                });
            }
                             
            return Json(list);
        }
         
        private void ExecuteStockQuery(SqlConnection con, string query, dynamic globalVariables)
        {
            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@WSID", globalVariables.PubWorkStationID);
            cmd.Parameters.AddWithValue("@USER_ID", globalVariables.PubUserId);
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
            cmd.Parameters.AddWithValue("@V_DATE", DateTime.Today);
            cmd.ExecuteNonQuery();
        }
        
        [HttpGet]
        public IActionResult GetPendingDeliveryOrder(int partyCode, string vType)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                // 1. Update ORDER1 status from SAUDA
                string updateOrder1 = @"
                UPDATE o
                SET o.status = s.status
                FROM ORDER1 o
                INNER JOIN SAUDA s
                    ON o.sauda_no = s.v_no
                    AND o.sauda_type = s.v_type
                    AND o.comp_code = s.comp_code
                    AND o.branch_code = s.branch_code
                WHERE o.comp_code = @COMP_CODE
                    AND o.branch_code = @BRANCH_CODE
                    AND o.v_type = 'SORD'
                    AND s.v_type = 'SAUD'
                    AND o.status = 1";

                using (var cmd = new SqlCommand(updateOrder1, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.ExecuteNonQuery();
                }

                // 2. Update ORDER2 status and reset adjusted quantity
                string updateOrder2 = @"
                UPDATE o2
                SET o2.status = o1.status,
                    o2.adj_qty = 0
                FROM ORDER2 o2
                INNER JOIN ORDER1 o1
                    ON o1.v_no = o2.v_no
                    AND o1.v_type = o2.v_type
                    AND o1.comp_code = o2.comp_code
                    AND o1.branch_code = o2.branch_code
                WHERE o1.comp_code = @COMP_CODE
                    AND o1.branch_code = @BRANCH_CODE
                    AND o1.v_type = 'SORD'
                    AND o1.status = 1";

                using (var cmd = new SqlCommand(updateOrder2, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.ExecuteNonQuery();
                }

                // 3. Calculate adjusted quantity from sales invoices
                string updateAdjustedQty = @"
                WITH SaleSummary AS
                (
                    SELECT
                        s.ord_type,
                        s.ord_no,
                        s.item_code,
                        s.comp_code,
                        s.branch_code,
                        SUM(s.qty) AS total_qty
                    FROM SALE2 s
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
                FROM ORDER2 o
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

                using (var cmd = new SqlCommand(updateAdjustedQty, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.ExecuteNonQuery();
                }

                // 4. Fetch pending orders for the selected party
                string query = @"
                SELECT
                    a.doc_id,
                    c.code,
                    c.Name AS [Item Name],
                    a.QTY AS [Order Qty],
                    SUM(ISNULL(a.Qty, 0) - ISNULL(a.Adj_Qty, 0)) AS [Pending Qty],
                    a.Rate AS [Rate],
                    a.V_Date AS [Order Date],
                    d.Rate AS [Sauda Rate],
                    a.SAUDA_TYPE,
                    a.SAUDA_NO,
                    d.Item_type AS [ItemType],
                    a.V_TYPE AS ORDER_TYPE,
                    a.V_NO AS ORDER_NO,
                    d.OFFERNO
                FROM ORDER2 a
                LEFT JOIN ORDER1 b
                    ON a.V_TYPE = b.V_TYPE
                    AND a.V_NO = b.V_NO
                    AND a.COMP_CODE = b.COMP_CODE
                    AND a.BRANCH_CODE = b.BRANCH_CODE
                LEFT JOIN ITEM_MAST c
                    ON a.Item_Code = c.code
                    AND a.COMP_CODE = c.COMP_CODE
                LEFT JOIN SAUDA d
                    ON d.V_TYPE = b.SAUDA_TYPE
                    AND d.V_NO = b.SAUDA_NO
                    AND d.COMP_CODE = a.COMP_CODE
                    AND d.BRANCH_CODE = a.BRANCH_CODE
                WHERE b.PARTY_CODE = @PARTY_CODE
                    AND a.COMP_CODE = @COMP_CODE
                    AND a.BRANCH_CODE = @BRANCH_CODE
                    AND b.status = 1
                GROUP BY
                    a.doc_id,
                    c.code,
                    c.Name,
                    d.Item_type,
                    a.QTY,
                    a.Rate,
                    a.V_Date,
                    d.Rate,
                    a.SAUDA_TYPE,
                    a.SAUDA_NO,
                    a.V_TYPE,
                    a.V_NO,
                    d.OFFERNO
                HAVING SUM(ISNULL(a.Qty, 0) - ISNULL(a.Adj_Qty, 0)) > 0
                ORDER BY a.doc_id DESC";

                using var selectCmd = new SqlCommand(query, con);

                selectCmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);
                selectCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                selectCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);

                using var reader = selectCmd.ExecuteReader();

                var pendingOrders = new List<object>();

                while (reader.Read())
                {
                    pendingOrders.Add(new
                    {
                        docId = reader["doc_id"],
                        code = reader["code"],
                        itemName = reader["Item Name"],
                        orderQty = reader["Order Qty"],
                        pendingQty = reader["Pending Qty"],
                        rate = reader["Rate"],
                        orderDate = reader["Order Date"],
                        saudaRate = reader["Sauda Rate"],
                        saudaType = reader["SAUDA_TYPE"],
                        saudaNo = reader["SAUDA_NO"],
                        itemType = reader["ItemType"],
                        orderType = reader["ORDER_TYPE"],
                        orderNo = reader["ORDER_NO"],
                        offerNo = reader["OFFERNO"]
                    });
                }
                
                if (pendingOrders.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Order Number Not Available",
                        data = pendingOrders
                    });
                }

                return Json(new
                {
                    success = true,
                    data = pendingOrders
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetPackingVoucherList(string vType, string pType, int partyCode, DateTime vDate)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string packType = "";
            string filterCondition = "";

            // Determine packing voucher types
            if (vType == "DOGT" || vType == "SABS")
            {
                packType = pType == "Flakes" ? "'SFIS'" : "'FPIS'";
            }
            else if (vType == "SACH")
            {
                packType = "'FGIS'";
            }
            else if (vType == "SAJI")
            {
                if (globalVariables.PubCompCode == "2" || globalVariables.PubCompCode == "5")
                {
                    packType = "'FPJI','FFIS'";
                    filterCondition = " AND a.MAC_TYPE IN ('Packing Slip', 'Job Issue')";
                }
                else
                {
                    packType = "'FFIS'";
                    filterCondition = " AND a.MAC_TYPE = 'Job Issue'";
                }
            }
            else
            {
                return Json(new List<object>());
            }

            string query = $@"
            SELECT
                a.V_TYPE,
                LTRIM(RTRIM(a.V_NO)) AS V_NO
            FROM PRODUCTION1 a
            WHERE a.V_DATE >= @V_DATE
              AND a.V_DATE < DATEADD(DAY, 1, @V_DATE)
              AND a.V_TYPE IN ({packType})
              AND a.COMP_CODE = @COMP_CODE
              AND a.BRANCH_CODE = @BRANCH_CODE
              AND a.YEAR_CODE = @YEAR_CODE
              AND
              (
                  a.PARTY_CODE = @PARTY_CODE
                  OR a.PARTY_CODE =
                  (
                      SELECT s.MAIN_CODE
                      FROM SUBGROUP_MAST s
                      WHERE s.COMP_CODE = a.COMP_CODE
                        AND s.CODE = a.PARTY_CODE
                        AND s.ACTIVE = 1
                  )
              )
              {filterCondition}
              ORDER BY a.V_NO";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@V_DATE", vDate);
            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
            cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);

            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    vType = reader["V_TYPE"].ToString(),
                    vNo = reader["V_NO"].ToString()
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetWaybillList()
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                string query = @"
                SELECT
                    V_TYPE,
                    LTRIM(RTRIM(V_NO)) AS V_NO
                FROM WB1
                WHERE V_TYPE = 'KANT'
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE
                ORDER BY V_NO";

                using var con = _dbConnection.GetErpConnection();
                con.Open();

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);

                using var reader = cmd.ExecuteReader();

                var list = new List<object>();

                while (reader.Read())
                {
                    list.Add(new
                    {
                        vType = reader["V_TYPE"].ToString(),
                        vNo = reader["V_NO"].ToString()
                    });
                }

                return Json(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load waybill list." });
            }
        }

        [HttpGet]
        public IActionResult GetIssueVoucherList()
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                string issueType =
                globalVariables.PubCompCode == "1" || globalVariables.PubCompCode == "3" || globalVariables.PubCompCode == "7" || globalVariables.PubCompCode == "8"
                        ? "RAID" : "RAIS";

                string query = @"
                SELECT
                    V_TYPE,
                    LTRIM(RTRIM(V_NO)) AS V_NO
                FROM Issue2
                WHERE COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE
                  AND V_TYPE = @V_TYPE
                GROUP BY V_TYPE, V_NO
                ORDER BY V_NO";

                using var con = _dbConnection.GetErpConnection();
                con.Open();

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                cmd.Parameters.AddWithValue("@V_TYPE", issueType);

                using var reader = cmd.ExecuteReader();

                var list = new List<object>();

                while (reader.Read())
                {
                    list.Add(new
                    {
                        vType = reader["V_TYPE"].ToString(),
                        vNo = reader["V_NO"].ToString()
                    });
                }

                return Json(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load issue voucher list." });
            }
        }

        //==============Calculate========================
        [HttpGet]
        public async Task<IActionResult> GetCalculationSettings()
        {
            var settings = await _globalVariableService.LoadGeneralSetting();

            return Json(new
            {
                tonnageRate = settings.pubDefTonnageRate,
                weightCalcOnBales = settings.pubDefWtCalconBales,
                taxOnInsurance = settings.pubDefTaxonInsuInSI,
                taxOnFreight = settings.pubDefTaxonFrtInSI,
                insuranceInBillAmount = settings.pubDefInsuInBillAmtInSI,
                freightInBillAmount = settings.pubDefFrtInBillAmtInSI,
                insurancePercent = settings.pubBPInsuPer,
                tcsPercent = settings.pubBPTCSPer
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetItemCalculationDetails(int itemCode)
        {
            try
            {
                using var connection = _dbConnection.GetErpConnection();
                await connection.OpenAsync();

                var globalVariables = _globalVariableService.GetGlobalVariables();

                const string query = @"
                SELECT
                    ISNULL(b.REPORT_TYPE, '') AS REPORT_TYPE,
                    ISNULL(a.Sale_Rate, 0) AS Sale_Rate,
                    ISNULL(a.Taxable_Rate, 0) AS Taxable_Rate,
                    ISNULL(a.Net_Wt, 0) AS Net_Wt,
                    ISNULL(a.Packing_Wt, 0) AS Packing_Wt,
                    ISNULL(a.Packing_Nos, 0) AS Packing_Nos
                FROM ITEM_MAST a
                LEFT JOIN ITEM_MGROUP b
                    ON a.MGROUP_CODE = b.CODE
                    AND a.COMP_CODE = b.COMP_CODE
                WHERE a.COMP_CODE = @CompCode
                    AND a.CODE = @ItemCode
                ORDER BY b.REPORT_TYPE";

                using var command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@CompCode", globalVariables.PubCompCode);
                command.Parameters.AddWithValue("@ItemCode", itemCode);

                using var reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return NotFound(new
                    {
                        message = "Item calculation details not found."
                    });
                }

                return Json(new
                {
                    reportType = reader["REPORT_TYPE"].ToString(),
                    saleRate = Convert.ToDecimal(reader["Sale_Rate"]),
                    taxableRate = Convert.ToDecimal(reader["Taxable_Rate"]),
                    netWt = Convert.ToDecimal(reader["Net_Wt"]),
                    packingWt = Convert.ToDecimal(reader["Packing_Wt"]),
                    packingNos = Convert.ToDecimal(reader["Packing_Nos"])
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load item calculation details."});
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateData([FromForm] DeliveryOrderModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                var result = await _deliveryOrderRepository.SaveAndUpdateDataAsync(model);

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
            var result = await _deliveryOrderRepository.LoadEditDataAsync(vNo, vType);
            return Json(result);
        }

        //========================ReConcile Delivery Order========================
        [HttpPost]
        public async Task<IActionResult> ReconcileDeliveryOrder([FromBody] ReconcileDeliveryOrderRequest model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            int fg = 0;
            string gateType = "";
            int gateNo = 0;

            int transportCode = 0;
            string transportName = "";
            string driverName = "";
            string driverNo = "";
            string truckNo = "";

            var itemUpdates = new List<object>();
            var messages = new List<string>();

            decimal freight = model.FREIGHT;
            decimal insurancePercent = model.INSURANCE_PERCENT;
            decimal insuranceAmount = model.INSURANCE_AMOUNT;
            decimal tdsPercent = model.TDS_PERCENT;
            decimal tdsAmount = model.TDS_AMOUNT;

            string? wbType = null;
            int? wbNo = null;
            decimal wbQty = 0;

            string? packType = model.PACK_TYPE;
            int packNo = model.PACK_NO;

            string? issueType = model.ISSUE_TYPE;
            int issueNo = model.ISSUE_NO;

            string buyerOrderNo = "";
            string paymentTerm = "";

            decimal cgstPercent = 0;
            decimal sgstPercent = 0;
            decimal igstPercent = 0;

            string grNo = "";
            DateTime? grDate = null;

            using SqlConnection con = _dbConnection.GetErpConnection();
            await con.OpenAsync();

            try
            {
                // ---------------- GATE / TRANSPORT ----------------

                using (SqlCommand cmd = new SqlCommand(@"
                SELECT
                    a.TRANSPORT_CODE,
                    b.NAME AS Transport,
                    a.DRIVER_NAME,
                    a.DRIVER_NO,
                    a.TRUCK_NO,
                    a.V_TYPE,
                    a.V_NO,
                    b.TDS_PER
                FROM GATE1 a
                LEFT JOIN TRANSPORT_MAST b
                    ON a.TRANSPORT_CODE = b.CODE
                   AND a.COMP_CODE = b.COMP_CODE
                WHERE a.V_TYPE = 'TRGI'
                  AND a.YEAR_CODE = @YEAR_CODE
                  AND a.DISP_PLAN_TYPE = @V_TYPE
                  AND a.DISP_PLAN_NO = @V_NO", con))
                {
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)model.V_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        gateType = Convert.ToString(reader["V_TYPE"]) ?? "";
                        gateNo = Convert.ToInt32(reader["V_NO"]);

                        transportCode = reader["TRANSPORT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TRANSPORT_CODE"]);

                        transportName = Convert.ToString(reader["Transport"]) ?? "";
                        driverName = Convert.ToString(reader["DRIVER_NAME"]) ?? "";
                        driverNo = Convert.ToString(reader["DRIVER_NO"]) ?? "";
                        truckNo = Convert.ToString(reader["TRUCK_NO"]) ?? "";

                        tdsPercent = reader["TDS_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TDS_PER"]);

                        fg++;
                    }
                }

                // ---------------- APPROVED TRANSPORT QUOTE ----------------

                using (SqlCommand cmd = new SqlCommand(@"
                SELECT
                    ISNULL(b.OUR_RATE, 0) AS FRT,
                    b.GRNO,
                    b.GRDATE
                FROM TRANSPORT_QT1 a
                LEFT JOIN TRANSPORT_QT2 b
                    ON a.V_TYPE = b.V_TYPE
                   AND a.V_NO = b.V_NO
                   AND a.COMP_CODE = b.COMP_CODE
                   AND a.BRANCH_CODE = b.BRANCH_CODE
                   AND a.YEAR_CODE = b.YEAR_CODE
                WHERE a.DO_TYPE = 'DOGT'
                  AND a.DO_NO = @V_NO
                  AND ISNULL(b.TRUCK_NO, '') <> ''
                  AND a.FAPROV_STATUS = 'Approved'
                  AND a.COMP_CODE = @COMP_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        freight = reader["FRT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["FRT"]);

                        grNo = Convert.ToString(reader["GRNO"]) ?? "";

                        if (reader["GRDATE"] != DBNull.Value)
                        {
                            grDate = Convert.ToDateTime(reader["GRDATE"]);

                            if (model.V_DATE.HasValue && grDate.Value != model.V_DATE.Value)
                            {
                                messages.Add("Please check GR Date and Invoice date not matched.");
                            }
                        }
                    }
                }

                // ---------------- INSURANCE / TDS CALCULATION ----------------
                
                var settings = await _globalVariableService.LoadGeneralSetting();

                insurancePercent = Convert.ToDecimal(settings.pubBPInsuPer);

                if (Convert.ToString(settings.pubDefTaxonInsuInSI) == "Yes")
                {
                    insuranceAmount = Math.Ceiling((model.SUBTOTAL + freight) * (insurancePercent / 100000m));
                }
                else
                {
                    insuranceAmount = Math.Ceiling((model.NET_AMOUNT + freight) * (insurancePercent / 100000m));
                }

                tdsAmount = Math.Round( freight * (tdsPercent / 100m), MidpointRounding.ToEven);

                // ---------------- WB DATA ----------------

                if (gateNo > 0)
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT
                        a.V_TYPE,
                        a.V_NO,
                        SUM(b.NET_WGT) AS QTY
                    FROM WB1 a
                    LEFT JOIN WB2 b
                        ON a.V_TYPE = b.V_TYPE
                       AND a.V_NO = b.V_NO
                       AND a.COMP_CODE = b.COMP_CODE
                       AND a.BRANCH_CODE = b.BRANCH_CODE
                       AND a.YEAR_CODE = b.YEAR_CODE
                    WHERE a.GATE_TYPE = @GATE_TYPE
                      AND a.GATE_NO = @GATE_NO
                      AND a.WB_TYPE = 'Sales'
                      AND a.V_TYPE = 'KANT'
                      AND a.COMP_CODE = @COMP_CODE
                      AND a.BRANCH_CODE = @BRANCH_CODE
                      AND ISNULL(b.ITEM_CODE, 0) > 0
                    GROUP BY a.V_TYPE, a.V_NO", con))
                    {
                        cmd.Parameters.AddWithValue("@GATE_TYPE", gateType);
                        cmd.Parameters.AddWithValue("@GATE_NO", gateNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            wbType = Convert.ToString(reader["V_TYPE"]);
                            wbNo = Convert.ToInt32(reader["V_NO"]);
                            wbQty = reader["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY"]);

                            fg++;
                        }
                    }

                    // ---------------- PACKING DOCUMENT ----------------

                    string tableN1;
                    string tableN2;
                    string vtyp;
                    string planColumn;
                    string planNoColumn;

                    if (model.PROD_TYPE == "Chips")
                    {
                        vtyp = "SFEI";
                        tableN1 = "PROD_SFG1";
                        tableN2 = "PROD_SFG2";
                        planColumn = "DO_TYPE";
                        planNoColumn = "DO_NO";
                    }
                    else if (model.PROD_TYPE == "Flakes")
                    {
                        vtyp = "SFIS";
                        tableN1 = "PROD_SFG1";
                        tableN2 = "PROD_SFG2";
                        planColumn = "DO_TYPE";
                        planNoColumn = "DO_NO";
                    }
                    else
                    {
                        vtyp = "FPIS";
                        tableN1 = "PRODUCTION1";
                        tableN2 = "PRODUCTION2";
                        planColumn = "PORD_TYPE";
                        planNoColumn = "PORD_NO";
                    }

                    // Table/column names above are selected from fixed values.
                    string packQuery = $@"
                    SELECT V_TYPE, V_NO
                    FROM {tableN1}
                    WHERE {planColumn} = @V_TYPE
                      AND {planNoColumn} = @V_NO
                      AND V_TYPE = @PACK_VTYPE
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE";

                    var packNumbers = new List<int>();

                    using (SqlCommand cmd = new SqlCommand(packQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)model.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@PACK_VTYPE", vtyp);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            packNumbers.Add(Convert.ToInt32(reader["V_NO"]));
                        }
                    }

                    if (packNumbers.Count > 0)
                    {
                        packNo = packNumbers[0];
                        packType = vtyp;
                        fg++;
                    }
                    else
                    {
                        packNo = model.PACK_NO;
                        packType = model.PACK_TYPE;
                    }

                    // ---------------- SAUDA BUYER ORDER / PAYMENT TERM ----------------

                    if (model.Footer != null && model.Footer.Count > 0)
                    {
                        var firstRow = model.Footer[0];

                        using (SqlCommand cmd = new SqlCommand(@"
                        SELECT
                            ISNULL(a.OFFERNO, '') AS OFFERNO,
                            ISNULL(b.NAME, '') AS PAYMENT_TERM
                        FROM SAUDA a
                        LEFT JOIN PAYTERM_MAST b
                            ON a.PAYTERM_CODE = b.CODE
                           AND a.COMP_CODE = b.COMP_CODE
                        WHERE a.V_TYPE = @SAUDA_TYPE
                          AND a.V_NO = @SAUDA_NO
                          AND a.COMP_CODE = @COMP_CODE
                          AND a.BRANCH_CODE = @BRANCH_CODE", con))
                        {
                            cmd.Parameters.AddWithValue("@SAUDA_TYPE", (object?)firstRow.SAUDA_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SAUDA_NO", firstRow.SAUDA_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                            if (await reader.ReadAsync())
                            {
                                buyerOrderNo = Convert.ToString(reader["OFFERNO"]) ?? "";
                                paymentTerm = Convert.ToString(reader["PAYMENT_TERM"]) ?? "";
                            }
                        }
                    }

                    // ---------------- PACKING ITEM DETAILS ----------------

                    var detailPackNumbers = packNumbers.Count > 0 ? packNumbers : packNo > 0 ? new List<int> { packNo } : new List<int>();

                    if (detailPackNumbers.Count > 0 && !string.IsNullOrWhiteSpace(packType))
                    {
                        string parameterNames = string.Join(",",detailPackNumbers.Select((_, i) => "@PACKNO" + i));

                        string detailQuery = $@"
                        SELECT
                            ITEM_CODE,
                            ITEM_NAME,
                            COUNT(*) AS NOS,
                            ISNULL(SUM(GROSS_QTY), 0) AS GROSS,
                            ISNULL(SUM(QTY), 0) AS QTY,
                            DOROW_ID,
                            V_NO
                        FROM {tableN2}
                        WHERE V_TYPE = @PACK_VTYPE
                          AND V_NO IN ({parameterNames})
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE
                        GROUP BY ITEM_CODE, ITEM_NAME, DOROW_ID, V_NO
                        ORDER BY ITEM_CODE, DOROW_ID";

                        using SqlCommand cmd = new SqlCommand(detailQuery, con);

                        cmd.Parameters.AddWithValue("@PACK_VTYPE", packType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                        for (int i = 0; i < detailPackNumbers.Count; i++)
                        {
                            cmd.Parameters.AddWithValue("@PACKNO" + i, detailPackNumbers[i]);
                        }

                        using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            int itemCode = Convert.ToInt32(reader["ITEM_CODE"]);
                            decimal qty = Convert.ToDecimal(reader["QTY"]);

                            string hsnCode = "";

                            using (SqlConnection itemCon = _dbConnection.GetErpConnection())
                            {
                                await itemCon.OpenAsync();

                                using (SqlCommand itemCmd = new SqlCommand(@"
                                SELECT ISNULL(HSN_CODE, '')
                                FROM ITEM_MAST
                                WHERE CODE = @ITEM_CODE
                                  AND COMP_CODE = @COMP_CODE", itemCon))
                                {
                                    itemCmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                                    itemCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                                    object? hsn = await itemCmd.ExecuteScalarAsync();
                                    hsnCode = Convert.ToString(hsn) ?? "";
                                }
                            }

                            itemUpdates.Add(new
                            {
                                source = "Packing",
                                ITEM_CODE = itemCode,
                                DOROW_ID = Convert.ToString(reader["DOROW_ID"]) ?? "",
                                NOS = Convert.ToDecimal(reader["NOS"]),
                                GROSS_QTY = Convert.ToDecimal(reader["GROSS"]),
                                QTY = qty,
                                PACK_NO = Convert.ToInt32(reader["V_NO"]),
                                TAX_CODE = model.TAX_CODE,
                                HSN_CODE = hsnCode
                            });
                        }
                    }

                    // ---------------- ISSUE DATA (ONLY WHEN NO PACKING NO) ----------------

                    if (packNo <= 0)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 1 V_TYPE, V_NO
                        FROM ISSUE1
                        WHERE PLAN_TYPE = @V_TYPE
                          AND PLAN_NO = @V_NO
                          AND V_TYPE = 'RAID'
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con))
                        {
                            cmd.Parameters.AddWithValue("@V_TYPE", (object?)model.V_TYPE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                            if (await reader.ReadAsync())
                            {
                                issueType = Convert.ToString(reader["V_TYPE"]);
                                issueNo = Convert.ToInt32(reader["V_NO"]);
                                fg++;
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(@"
                        SELECT
                            ITEM_CODE,
                            ITEM_NAME,
                            ISNULL(SUM(NOS), 0) AS NOS,
                            ISNULL(SUM(QTY), 0) AS GROSS,
                            ISNULL(SUM(QTY), 0) AS QTY
                        FROM ISSUE2
                        WHERE V_TYPE = @ISSUE_TYPE
                          AND V_NO = @ISSUE_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE
                        GROUP BY ITEM_CODE, ITEM_NAME
                        ORDER BY ITEM_CODE", con))
                        {
                            cmd.Parameters.AddWithValue("@ISSUE_TYPE", (object?)issueType ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ISSUE_NO", issueNo);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                            if (await reader.ReadAsync())
                            {
                                fg++;

                                do
                                {
                                    int itemCode = Convert.ToInt32(reader["ITEM_CODE"]);
                                    string hsnCode = "";

                                    using (SqlConnection itemCon = _dbConnection.GetErpConnection())
                                    {
                                        await itemCon.OpenAsync();

                                        using (SqlCommand itemCmd = new SqlCommand(@"
                                        SELECT ISNULL(HSN_CODE, '')
                                        FROM ITEM_MAST
                                        WHERE CODE = @ITEM_CODE
                                          AND COMP_CODE = @COMP_CODE", itemCon))
                                        {
                                            itemCmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                                            itemCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                                            object? hsn = await itemCmd.ExecuteScalarAsync();
                                            hsnCode = Convert.ToString(hsn) ?? "";
                                        }
                                    }

                                    itemUpdates.Add(new
                                    {
                                        source = "Issue",
                                        ITEM_CODE = itemCode,
                                        NOS = Convert.ToDecimal(reader["NOS"]),
                                        GROSS_QTY = Convert.ToDecimal(reader["GROSS"]),
                                        QTY = Convert.ToDecimal(reader["QTY"]),
                                        PACK_NO = packNo,
                                        WB_QTY = Convert.ToDecimal(reader["QTY"]),
                                        TAX_CODE = model.TAX_CODE,
                                        HSN_CODE = hsnCode
                                    });
                                }
                                while (await reader.ReadAsync());
                            }
                        }
                    }

                }

                // ---------------- TAX RATES ----------------

                if (itemUpdates.Count > 0 && model.TAX_CODE > 0)
                {
                    using (SqlCommand taxCmd = new SqlCommand(@"
                            SELECT
                                ISNULL(CGST_PER, 0) AS CGST_PER,
                                ISNULL(SGST_PER, 0) AS SGST_PER,
                                ISNULL(IGST_PER, 0) AS IGST_PER
                            FROM TAX_MAST
                            WHERE CODE = @TAX_CODE", con))
                    {
                        taxCmd.Parameters.AddWithValue("@TAX_CODE", model.TAX_CODE);

                        using SqlDataReader taxReader = await taxCmd.ExecuteReaderAsync();

                        if (await taxReader.ReadAsync())
                        {
                            cgstPercent = Convert.ToDecimal(taxReader["CGST_PER"]);
                            sgstPercent = Convert.ToDecimal(taxReader["SGST_PER"]);
                            igstPercent = Convert.ToDecimal(taxReader["IGST_PER"]);
                        }
                    }
                }

                // ---------------- CREDIT LIMIT VALIDATION ----------------

                if (fg < 3)
                {
                    return BadRequest(new
                    {
                        success = false,
                        fg,
                        message = "Delivery Order not yet Linked in All/Any documents. (like Packing/Issue Slip, Gate, WB)\nReconciliation can not complete.",
                        messages
                    });
                }

                if (model.SUPPLY_TYPE != "EXPWOP" && model.SUPPLY_TYPE != "EXPWP")
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(CR_LIMIT, 0)
                    FROM CRLIMIT_MAST
                    WHERE PARTY_CODE = @PARTY_CODE
                      AND COMP_CODE = @COMP_CODE", con))
                    {
                        cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                        object? limitResult = await cmd.ExecuteScalarAsync();

                        if (limitResult == null || limitResult == DBNull.Value)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "CR Limit Refused by Insurance company. Reconciliation failed."
                            });
                        }

                        decimal crLimitAmt = Convert.ToDecimal(limitResult);

                        if (crLimitAmt <= 0)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Credit Limit is 0. Reconciliation failed."
                            });
                        }

                        decimal drAmt;
                        decimal crAmt;
                        decimal doAmt;

                        using (SqlCommand balanceCmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(AMT), 0)
                        FROM LEDGER2
                        WHERE DR_CODE = @PARTY_CODE
                          AND COMP_CODE = @COMP_CODE
                          AND DR_CODE > 0
                          AND BRANCH_CODE = @BRANCH_CODE", con))
                        {
                            balanceCmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE);
                            balanceCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            balanceCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            object? result = await balanceCmd.ExecuteScalarAsync();

                            drAmt = result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                        }

                        using (SqlCommand balanceCmd = new SqlCommand(@"
                        SELECT ISNULL(SUM(AMT), 0)
                        FROM LEDGER2
                        WHERE CR_CODE = @PARTY_CODE
                          AND COMP_CODE = @COMP_CODE
                          AND CR_CODE > 0
                          AND BRANCH_CODE = @BRANCH_CODE", con))
                        {
                            balanceCmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE);
                            balanceCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            balanceCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            object? result = await balanceCmd.ExecuteScalarAsync();

                            crAmt = result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                        }

                        using (SqlCommand doCmd = new SqlCommand(@"
                        SELECT ISNULL(NAMOUNT, 0)
                        FROM DO1
                        WHERE V_TYPE = 'DOGT'
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE", con))
                        {
                            doCmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            doCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            doCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);

                            object? result = await doCmd.ExecuteScalarAsync();

                            doAmt = result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
                        }

                        decimal outstanding = drAmt - crAmt;
                        decimal balanceAmount = outstanding + doAmt;

                        if (balanceAmount - crLimitAmt > 1)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Total DO Amount exceeds Credit Limit (Incl. this DO). Reconciliation failed."
                            });
                        }
                    }
                }

                // Intentionally do not call UpdateDO_data() here.

                return Ok(new
                {
                    success = true,
                    fg,
                    message = "Reconciliation validation completed.",
                    messages,
                    TRANSPORT_CODE = transportCode,
                    TRANSPORT_NAME = transportName,
                    DRIVER_NAME = driverName,
                    DRIVER_NO = driverNo,
                    TRUCK_NO = truckNo,
                    GR_NO = grNo,
                    GR_DATE = grDate,
                    TRANSPORT_FREIGHT = freight,
                    INSURANCE_PERCENT = insurancePercent,
                    INSURANCE_AMOUNT = insuranceAmount,
                    TDS_PERCENT = tdsPercent,
                    TDS_AMOUNT = tdsAmount,
                    WB_TYPE = wbType,
                    WB_NO = wbNo,
                    WB_QTY = wbQty,
                    PACK_TYPE = packType,
                    PACK_NO = packNo,
                    ISSUE_TYPE = issueType,
                    ISSUE_NO = issueNo,
                    BUYER_ORDNO = buyerOrderNo,
                    PAYMENT_TERM = paymentTerm,
                    CGST_PERCENT = cgstPercent,
                    SGST_PERCENT = sgstPercent,
                    IGST_PERCENT = igstPercent,
                    TAX_CODE = model.TAX_CODE,
                    ItemUpdates = itemUpdates
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDO([FromBody] DeliveryOrderModel model)
        {
            var result = await _deliveryOrderRepository.UpdateDOAsync(model);
            return Json(result);
        }

        //==================Porforma Invoice Generation========================
        public JsonResult GetProformaV_NO()
        {
            string newV_NO = "00001";
            string vType = "DMEU";
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 FROM SALE1 WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);

                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    lastVnoCmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

                    object result = lastVnoCmd.ExecuteScalar();

                    int nextNo = Convert.ToInt32(result);

                    string runningPart = nextNo.ToString("D5");

                    newV_NO = prefixYR + runningPart;

                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { v_NO = newV_NO });
        }

        [HttpPost]
        public async Task<IActionResult> SaveProformaData([FromBody] DeliveryOrderModel model, [FromQuery] string vNo)
        {
            var sourceType = model?.V_TYPE;
            var sourceNo = model?.V_NO;
            var result = await _deliveryOrderRepository.SaveProformaDataAsync(model, vNo);
            return Ok(result);
        }

        //===================Sales Invoice Generation========================
        public JsonResult GetSalesInvoiceV_NO()
        {
            string newV_NO = "00001";
            string vType = "SAGT";
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 FROM SALE1 WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);

                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    lastVnoCmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

                    object result = lastVnoCmd.ExecuteScalar();

                    int nextNo = Convert.ToInt32(result);

                    string runningPart = nextNo.ToString("D5");

                    newV_NO = prefixYR + runningPart;

                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { v_NO = newV_NO });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSaleData([FromBody] DeliveryOrderModel model, [FromQuery] string vNo)
        {
            var sourceType = model?.V_TYPE;
            var sourceNo = model?.V_NO;
            var result = await _deliveryOrderRepository.SaveSaleInvoiceDataAsync(model, vNo);
            return Ok(result);
        }


    }
}
