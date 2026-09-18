using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Org.BouncyCastle.Asn1.Cmp;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesOrderController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataservice;
        private readonly GlobalValidationdate _globalValidationdate;
        public SalesOrderController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue,
            ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService, GlobalValidationdate globalValidationdate)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalVariableService = globalValue;
            _moduleService = moduleService;
            _masterDataservice = masterDataService;
            _globalValidationdate = globalValidationdate;
        }

        public IActionResult Index()
        {
            TempData["LoginDate"] = _globalVariableService.GetGlobalVariables().PubLoginDate;
            ViewBag.CurrentMenu = "Sales Order";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/SalesOrder/Index.cshtml", model);
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
                return Json(new { status = true, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSaudaDataList(string saudaNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string qry = $@"SELECT a.PARTY_CODE, a.ADD1, a.ADD2, a.ADD3, a.CITY_CODE, city.NAME AS C_Name, a.PHONE, a.PARTY_TO, a.PAYTERM_CODE, 
                            sg.GSTIN, sg.PINCODE, a.ITEM_CODE, im.SHORTNAME, im.NAME AS ITEM_NAME, a.QTY, COALESCE(a.NOS, 0) AS NOS, a.RATE, 
                            party.NAME AS Party, a.TENACITY_GRP
                            FROM SAUDA a
                            LEFT JOIN SUBGROUP_MAST sg ON sg.CODE = a.PARTY_CODE AND sg.COMP_CODE = a.COMP_CODE 
                            LEFT JOIN CITY_MAST city ON city.CODE = a.CITY_CODE
                            LEFT JOIN PAYTERM_MAST pt ON pt.CODE = a.PAYTERM_CODE AND pt.COMP_CODE = a.COMP_CODE
                            LEFT JOIN ITEM_MAST im ON im.CODE = a.ITEM_CODE AND im.COMP_CODE = a.COMP_CODE
                            LEFT JOIN SUBGROUP_MAST party ON party.CODE = a.PARTY_CODE AND party.COMP_CODE = a.COMP_CODE
                            WHERE a.STATUS = 1 AND a.DOC_ID = '{saudaNo}' AND a.COMP_CODE = {gv.PubCompCode} AND a.BRANCH_CODE = {gv.PubBranchCode}
                            ORDER BY a.V_NO;";

                var dataList = await _dbHelper.GetJsonDataAsync(qry);
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetIssueDetails(string issueNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string qry = $@"select ITEM_CODE, ITEM_NAME, sum(Nos) as NOS, sum(QTY) as QTY from ISSUE2 where COMP_CODE= {gv.PubCompCode} and Branch_code={gv.PubBranchCode} and 
                            DOC_ID = '{issueNo}' group by item_code,item_name order by Item_code;";
            var dataList = await _dbHelper.GetJsonDataAsync(qry);
            return Json(new { status = true, data = dataList });
        }

        [HttpGet]
        public async Task<IActionResult> GetPackingDetail(int vno, string packingNo)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                string saleQry = $@"SELECT a.ITEM_CODE AS ItemCode, b.NAME AS ITEM_NAME, SUM(a.Nos) AS Nos, SUM(a.Qty) AS Qty FROM Sale2 a
                            LEFT JOIN ITEM_MAST b ON a.ITEM_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE
							WHERE a.STATUS <> 2 AND a.ORD_TYPE = '{doctype}' AND a.ORD_NO = {vno} AND a.COMP_CODE = {gv.PubCompCode}
							AND a.BRANCH_CODE = {gv.PubBranchCode} GROUP BY a.ITEM_CODE, b.NAME ORDER BY a.ITEM_CODE;";

                var saleRows = await _dbHelper.GetJsonDataAsync(saleQry);
                var saleData = saleRows.Select(x =>
                {
                    var row = (IDictionary<string, object?>)x;
                    return new PackingDetailModel
                    {
                        ITEM_CODE = Convert.ToInt32(row["ItemCode"] ?? 0),
                        ITEM_NAME = Convert.ToString(row["ITEM_NAME"]) ?? "",
                        NOS = Convert.ToDecimal(row["Nos"] ?? 0),
                        QTY = Convert.ToDecimal(row["Qty"] ?? 0)
                    };
                }).ToList();


                string productionQry = $@"SELECT a.ITEM_CODE AS ItemCode, b.NAME AS ITEM_NAME, SUM(a.Nos) AS Nos, SUM(a.Qty) AS Qty 
								  FROM production2 a LEFT JOIN ITEM_MAST b ON a.ITEM_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE
								  WHERE a.DOC_ID = '{packingNo}' AND a.COMP_CODE = {gv.PubCompCode} AND a.BRANCH_CODE = 
								  {gv.PubBranchCode} GROUP BY a.ITEM_CODE, b.NAME ORDER BY a.ITEM_CODE;";

                var productionRows = await _dbHelper.GetJsonDataAsync(productionQry);
                var productionData = productionRows.Select(x =>
                {
                    var row = (IDictionary<string, object?>)x;
                    return new PackingDetailModel
                    {
                        ITEM_CODE = Convert.ToInt32(row["ItemCode"] ?? 0),
                        ITEM_NAME = Convert.ToString(row["ITEM_NAME"]) ?? "",
                        NOS = Convert.ToDecimal(row["Nos"] ?? 0),
                        QTY = Convert.ToDecimal(row["Qty"] ?? 0)
                    };
                }).ToList();


                var result = new List<PackingDetailModel>();
                foreach (var sale in saleData)
                {
                    result.Add(new PackingDetailModel
                    {
                        ITEM_CODE = sale.ITEM_CODE,
                        ITEM_NAME = sale.ITEM_NAME,
                        NOS = 0,
                        QTY = sale.QTY
                    });
                }

                foreach (var production in productionData)
                {
                    var existingItem = result.FirstOrDefault(
                        x => x.ITEM_CODE == production.ITEM_CODE
                    );

                    if (existingItem != null)
                    {
                        existingItem.NOS += production.NOS;
                        existingItem.QTY += production.QTY;
                    }
                    else
                    {
                        result.Add(new PackingDetailModel
                        {
                            ITEM_CODE = production.ITEM_CODE,
                            ITEM_NAME = production.ITEM_NAME,
                            NOS = production.NOS,
                            QTY = production.QTY
                        });
                    }
                }

                decimal totalQty = result.Sum(x => x.QTY);
                decimal totalNos = result.Sum(x => x.NOS);

                return Json(new { status = true, data = result, totalQty = totalQty, totalNos = totalNos });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLinkedOrders(int saudaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string qry = $@"Select CONCAT(a.V_TYPE,a.V_NO) as OrderNo, c.NAME as Party, b.ITEM_NAME as ItemName, b.QTY as Quantity, b.Rate, b.TENACITY_NAME as Tenacity,
                            b.TENACITY_TYPE as TenacityGrp from ORDER1 a 
                            inner join ORDER2 b on a.V_TYPE = b.v_type and a.v_no = b.v_no and a.COMP_CODE = b.COMP_CODE and a.BRANCH_CODE = b.BRANCH_CODE and 
                            a.YEAR_CODE = b.YEAR_CODE
                            left join SUBGROUP_MAST c on a.PARTY_CODE = c.CODE and a.COMP_CODE = c.COMP_CODE 
                            where a.SAUDA_TYPE = 'SAUD' and a.SAUDA_NO = {saudaNo}
                            and a.COMP_CODE = {gv.PubCompCode} and a.BRANCH_CODE = {gv.PubBranchCode}";
            var dataList = await _dbHelper.GetJsonDataAsync(qry);
            return Json(new { status = true, data = dataList });
        }

        [HttpPost]
        public async Task<IActionResult> GetSaudaRate([FromBody] GetSaudaRateRequest request)
        {
            if (request == null || request.SaudaNo <= 0)
            {
                return BadRequest(new { status = false, message = "Invalid Request!" });
            }

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                decimal saudaRate = 0;
                int saudaItemCode = 0;

                // 1. GET SAUDA RATE AND ITEM CODE
                if (request.Items != null && request.Items.Count > 0)
                {
                    string saudaQuery = @"SELECT TOP 1 ISNULL(rate, 0) AS rate, ISNULL(Item_code, 0) AS Item_code FROM SAUDA WHERE V_TYPE = 'SAUD' AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE
                                        AND BRANCH_CODE = @BRANCH_CODE";

                    using (var con = _dbcontext.GetErpConnection())
                    using (SqlCommand cmd = new SqlCommand(saudaQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", request.SaudaNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                saudaRate = reader["rate"] != DBNull.Value ? Convert.ToDecimal(reader["rate"]) : 0;
                                saudaItemCode = reader["Item_code"] != DBNull.Value ? Convert.ToInt32(reader["Item_code"]) : 0;
                            }
                        }
                    }
                }

                if (saudaRate == 0)
                {
                    return Ok(new { status = false, message = "Sauda Rate is blank" });
                }

                // 2. COMP CODE 2 / 5 - RATE CALCULATION
                if (gv.PubCompCode == "2" || gv.PubCompCode == "5")
                {
                    int discGrpCode = 0;

                    // Get Discount Group
                    string discGrpQuery = @"SELECT ISNULL(DISCGRP_CODE, 0) FROM SUBGROUP_MAST WHERE code = @CODE AND comp_code = @COMP_CODE";
                    discGrpCode = await _dbHelper.GetExecuteScalarAsync<int>(discGrpQuery, new Dictionary<string, object> { { "@CODE", request.BillToCode }, { "@COMP_CODE", gv.PubCompCode } });

                    // If no Discount Group -> Get Agent -> Discount Group
                    if (discGrpCode <= 0)
                    {
                        string agentQuery = @"SELECT ISNULL(AGENT_CODE, 0) FROM SUBGROUP_MAST WHERE code = @CODE AND comp_code = @COMP_CODE";
                        int agentCode = await _dbHelper.GetExecuteScalarAsync<int>(agentQuery, new Dictionary<string, object> { { "@CODE", request.BillToCode }, { "@COMP_CODE", gv.PubCompCode } });

                        if (agentCode > 0)
                        {
                            discGrpCode = await _dbHelper.GetExecuteScalarAsync<int>(discGrpQuery, new Dictionary<string, object> { { "@CODE", agentCode }, { "@COMP_CODE", gv.PubCompCode } });
                        }
                    }

                    // Freight Rate
                    decimal freightRate = 0;

                    if (request.Freight > 0 && request.TotalQty > 0)
                    {
                        freightRate = -Math.Round(request.Freight / request.TotalQty, 2);
                    }

                    // 3. ITEM LOOP
                    foreach (var item in request.Items)
                    {
                        if (item.ItemCode <= 0) continue;

                        decimal sizeDiff = 0;
                        decimal colorDiff = 0;
                        decimal gramDiff = 0;
                        decimal itemDiff = 0;
                        decimal meshRate = 0;

                        // Get Item Group Type
                        string itemGroupQuery = @"SELECT TOP 1 item_group.group_type FROM item_group INNER JOIN item_mast ON item_mast.group_code = item_group.code AND item_mast.comp_code = 
                                                    item_group.comp_code WHERE item_mast.comp_code = @COMP_CODE AND item_mast.code = @ITEM_CODE";

                        string itemGroupType = await _dbHelper.GetExecuteScalarAsync<string>(itemGroupQuery, new Dictionary<string, object> {{ "@COMP_CODE", gv.PubCompCode },
                                                { "@ITEM_CODE", item.ItemCode }}) ?? "";

                        // Size Difference
                        string sizeQuery = @"SELECT TOP 1 ISNULL(size_diff, 0) FROM disc_mast LEFT JOIN ITEMSIZE_MAST ON itemsize_mast.code = disc_mast.size_code AND itemsize_mast.comp_code = 
                                                disc_mast.comp_code LEFT JOIN item_mast ON item_mast.size_code = disc_mast.size_code AND item_mast.comp_code = disc_mast.comp_code WHERE 
                                                item_mast.code = @ITEM_CODE AND item_mast.comp_code = @COMP_CODE AND disc_mast.code = @DISCGRP_CODE AND disc_mast.v_type = 'SALE'";

                        sizeDiff = await _dbHelper.GetExecuteScalarAsync<decimal>(sizeQuery, new Dictionary<string, object> {{ "@ITEM_CODE", item.ItemCode }, { "@COMP_CODE", gv.PubCompCode },
                                    { "@DISCGRP_CODE", discGrpCode }});

                        // Color Difference
                        string colorQuery = @"SELECT TOP 1 ISNULL(color_diff, 0) FROM disc_mast LEFT JOIN color_mast ON color_mast.code = disc_mast.color_code AND color_mast.comp_code = 
                                                disc_mast.comp_code LEFT JOIN item_mast ON item_mast.color_code = disc_mast.color_code AND item_mast.comp_code = disc_mast.comp_code
                                                WHERE item_mast.code = @ITEM_CODE AND item_mast.comp_code = @COMP_CODE AND disc_mast.code = @DISCGRP_CODE AND disc_mast.v_type = 'SALE'";

                        colorDiff = await _dbHelper.GetExecuteScalarAsync<decimal>(colorQuery, new Dictionary<string, object> {{ "@ITEM_CODE", item.ItemCode }, { "@COMP_CODE", gv.PubCompCode },
                                    { "@DISCGRP_CODE", discGrpCode }});

                        // Gram Difference / RM Discount
                        if (itemGroupType == "Finish HD UnLam" || itemGroupType == "Finish HD Lam")
                        {
                            string rmDiscQuery = @"SELECT TOP 1 ISNULL(rate, 0) FROM rmdisc_mast LEFT JOIN item_mast ON item_mast.CAT_code = rmdisc_mast.sauda_item AND item_mast.comp_code = 
                                                    rmdisc_mast.comp_code WHERE item_mast.code = @ITEM_CODE AND item_mast.comp_code = @COMP_CODE AND rmdisc_mast.dtype = 'Sales'";

                            gramDiff = await _dbHelper.GetExecuteScalarAsync<decimal>(rmDiscQuery, new Dictionary<string, object> { { "@ITEM_CODE", item.ItemCode }, { "@COMP_CODE", gv.PubCompCode } });
                        }
                        else
                        {
                            string gramQuery = @"SELECT TOP 1 ISNULL(gram_diff, 0) FROM disc_mast LEFT JOIN ITEMCAT_MAST ON ITEMCAT_MAST.code = disc_mast.gram_code AND ITEMCAT_MAST.comp_code = 
                                                disc_mast.comp_code LEFT JOIN item_mast ON item_mast.CAT_code = disc_mast.gram_code AND item_mast.comp_code = disc_mast.comp_code
                                                WHERE item_mast.code = @ITEM_CODE AND item_mast.comp_code = @COMP_CODE AND disc_mast.code = @DISCGRP_CODE AND disc_mast.v_type = 'SALE'";

                            gramDiff = await _dbHelper.GetExecuteScalarAsync<decimal>(gramQuery, new Dictionary<string, object> {{ "@ITEM_CODE", item.ItemCode }, { "@COMP_CODE", gv.PubCompCode },
                                        { "@DISCGRP_CODE", discGrpCode }});
                        }

                        // COMP CODE 5 : MESH RATE
                        if (gv.PubCompCode == "5")
                        {
                            string meshQuery = @"SELECT ISNULL(meshconv_code, 0) FROM ITEM_MAST WHERE code = @ITEM_CODE AND comp_code = @COMP_CODE";

                            // Sauda Mesh
                            int saudaMesh = await _dbHelper.GetExecuteScalarAsync<int>(meshQuery, new Dictionary<string, object> { { "@ITEM_CODE", saudaItemCode }, { "@COMP_CODE", gv.PubCompCode } });

                            // Order Mesh
                            int orderMesh = await _dbHelper.GetExecuteScalarAsync<int>(meshQuery, new Dictionary<string, object> { { "@ITEM_CODE", item.ItemCode }, { "@COMP_CODE", gv.PubCompCode } });

                            // Market Rate Query
                            string marketRateQuery = @"SELECT TOP 1 ISNULL(MIN_RATE, 0) FROM MARKET_RATE1 a LEFT JOIN MARKET_RATE2 b ON a.V_Type = b.V_Type AND a.v_no = b.v_no AND a.comp_code = 
                                                        b.comp_code AND a.branch_code = b.branch_code AND a.year_code = b.year_code LEFT JOIN item_mast c ON c.code = b.item_code AND c.comp_code = 
                                                        b.comp_code WHERE a.Comp_code = @COMP_CODE AND a.faprov_status = 'Approved' AND c.meshconv_code = @MESH_CODE AND a.eff_date >= DATEADD(DAY, 
                                                        -20, GETDATE()) ORDER BY a.V_DATE DESC, a.V_no DESC";

                            // Sauda Market Rate
                            decimal marketSaudaRate = await _dbHelper.GetExecuteScalarAsync<decimal>(marketRateQuery, new Dictionary<string, object> { { "@COMP_CODE", gv.PubCompCode },
                                                        { "@MESH_CODE", saudaMesh }});

                            // Order Market Rate
                            decimal marketOrderRate = await _dbHelper.GetExecuteScalarAsync<decimal>(marketRateQuery, new Dictionary<string, object> {{ "@COMP_CODE", gv.PubCompCode },
                                                        { "@MESH_CODE", orderMesh }});

                            if (marketOrderRate != 0 && marketSaudaRate != 0)
                            {
                                meshRate = marketOrderRate - marketSaudaRate;
                            }
                        }

                        // ITEM DIFFERENCE
                        string itemDiffQuery = @"SELECT TOP 1 ISNULL(item_diff, 0) FROM disc_mast WHERE disc_mast.code = @Item_Code AND disc_mast.comp_code = @COMP_CODE AND disc_mast.code = 
                                                @DISCGRP_CODE AND disc_mast.v_type = 'SALE'";

                        itemDiff = await _dbHelper.GetExecuteScalarAsync<decimal>(itemDiffQuery, new Dictionary<string, object> {{ "@Item_Code", item.ItemCode }, { "@DISCGRP_CODE", discGrpCode },
                                    { "@COMP_CODE", gv.PubCompCode }});

                        // FINAL RATE
                        item.Rate = saudaRate + sizeDiff + colorDiff + gramDiff + itemDiff + meshRate + freightRate;
                    }
                }

                // 4. TAX CALCULATION
                string stateQuery = @"SELECT ISNULL(State_Code, 0) FROM CITY_MAST WHERE code = @STATION_CODE";
                int stateCode = await _dbHelper.GetExecuteScalarAsync<int>(stateQuery, new Dictionary<string, object> { { "@STATION_CODE", request.StationCode } });

                foreach (var item in request.Items)
                {
                    if (item.ItemCode <= 0) continue;

                    if (request.BillToCode <= 0) continue;

                    // Item GST
                    string gstQuery = @"SELECT ISNULL(IGST_PER, 0) AS IGST_PER, ISNULL(CGST_PER, 0) AS CGST_PER FROM Item_mast WHERE code = @ITEM_CODE AND comp_code = @COMP_CODE";

                    decimal igstPer = 0;
                    decimal cgstPer = 0;

                    using (var con = _dbcontext.GetErpConnection())
                    using (SqlCommand cmd = new SqlCommand(gstQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                igstPer = reader["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_PER"]) : 0;
                                cgstPer = reader["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_PER"]) : 0;
                            }
                        }
                    }

                    // SAME STATE -> CGST + SGST
                    if (Convert.ToInt32(gv.STATE_CODE) == stateCode)
                    {
                        if (cgstPer > 0)
                        {
                            string taxQuery = @"SELECT TOP 1 code FROM Tax_mast WHERE CGST_PER = @CGST_PER";
                            int taxCode = await _dbHelper.GetExecuteScalarAsync<int>(taxQuery, new Dictionary<string, object> { { "@CGST_PER", cgstPer } });

                            item.TaxCode = taxCode;
                            item.CGSTPer = cgstPer;
                            item.SGSTPer = cgstPer;
                            item.IGSTPer = 0;
                        }
                    }

                    // DIFFERENT STATE -> IGST
                    else
                    {
                        if (igstPer > 0)
                        {
                            string taxQuery = @"SELECT TOP 1 code FROM Tax_mast WHERE IGST_PER = @IGST_PER";
                            int taxCode = await _dbHelper.GetExecuteScalarAsync<int>(taxQuery, new Dictionary<string, object> { { "@IGST_PER", igstPer } });

                            item.TaxCode = taxCode;
                            item.CGSTPer = 0;
                            item.SGSTPer = 0;
                            item.IGSTPer = igstPer;
                        }
                    }
                }

                // 5. RETURN JSON
                return Ok(new { status = true, data = new { saudaNo = request.SaudaNo, saudaRate = saudaRate, items = request.Items } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPackAmount(int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string query = @"SELECT COALESCE(PACKING_WT, 0) FROM ITEM_MAST WHERE COMP_CODE = @COMP_CODE AND CODE = @ITEM_CODE";

                var parameters = new Dictionary<string, object>
                {
                    ["@COMP_CODE"] = gv.PubCompCode,
                    ["@ITEM_CODE"] = itemCode
                };

                decimal result = await _dbHelper.GetExecuteScalarAsync<decimal>(query, parameters);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCalculateAmtData(int itemCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var gs = await _globalVariableService.LoadGeneralSetting();
            try
            {
                string query = @"SELECT b.REPORT_TYPE, a.Sale_Rate, a.Taxable_Rate, a.Net_Wt FROM Item_mast a LEFT JOIN ITEM_MGROUP b  ON a.MGROUP_CODE = b.code AND a.comp_code = 
                                b.comp_code WHERE a.comp_code = @COMP_CODE AND a.code = @ITEM_CODE ORDER BY REPORT_TYPE";


                using var con = _dbcontext.GetErpConnection();
                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                await con.OpenAsync();

                var result = new List<CalculateAmtItemModel>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new CalculateAmtItemModel
                    {
                        ReportType = reader["REPORT_TYPE"] == DBNull.Value ? "" : Convert.ToString(reader["REPORT_TYPE"]),
                        SaleRate = reader["Sale_Rate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Sale_Rate"]),
                        TaxableRate = reader["Taxable_Rate"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Taxable_Rate"]),
                        NetWt = reader["Net_Wt"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Net_Wt"]),
                        pubDefTonnageRate = gs.pubDefTonnageRate,
                        pubDefWtCalconBales = gs.pubDefWtCalconBales
                    });
                }

                return Json(new { success = true, data = result });
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
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string query = @"SELECT a.ITEM_CODE FROM SALE2 a LEFT JOIN ITEM_MAST b ON a.ITEM_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE WHERE a.STATUS <> 2
                                AND a.ORD_TYPE = 'SORD' AND a.ORD_NO = @VNO AND a.COMP_CODE = @COMP_CODE AND a.BRANCH_CODE = @BRANCH_CODE GROUP BY a.ITEM_CODE, a.V_NO, 
                                b.SHORTNAME ORDER BY a.V_NO";

                using (var con = _dbcontext.GetErpConnection())
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@VNO", model.VNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    await con.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int itemCode = Convert.ToInt32(reader["ITEM_CODE"]);
                            if (!model.ItemCodes.Contains(itemCode))
                            {
                                return Json(new { success = false, message = "Sold item must exist in Order." });
                            }
                        }
                    }
                }

                return Json(new { success = true });
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

                if (string.IsNullOrEmpty(vType) || id <= 0)
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var userSession = _globalVariableService.GetGlobalVariables();
                var parametersHeader = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", userSession.PubBranchCode },
                { "@V_TYPE", vType},
                { "@V_NO", id },
                { "@Action", "PurchaseOrderHeader" }
                };

                var parametersDetail = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", userSession.PubBranchCode },
                { "@V_TYPE", vType},
                { "@V_NO", id },
                { "@Action", "PurchaseOrderDetail" }
                };

                var header = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersHeader);
                var detail = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersDetail);

                return Json(new { status = true, header = header, detail = detail });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateSalesOrder([FromBody] PurchaseOrder POmodel)
        {
            if (POmodel == null)
                return Json(new { status = false, message = " data save failed." });
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var gv = _globalVariableService.GetGlobalVariables();

                    var isApprovalBody = false;
                    var isFinalApprovalBody = false;

                    string approvalSql = @"SELECT TOP 1 APPROV_USER FROM DOC_APPROSTAGE WHERE USER_CODE=@USER_CODE AND DOC_CODE=@DOC_CODE AND COMP_CODE=@COMP_CODE";

                    using (var approvalCmd = new SqlCommand(approvalSql, con))
                    {
                        approvalCmd.Parameters.AddWithValue("@USER_CODE", gv.PubUserId);
                        approvalCmd.Parameters.AddWithValue("@DOC_CODE", doctype ?? "");
                        approvalCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                        using var approvalReader = await approvalCmd.ExecuteReaderAsync();

                        if (await approvalReader.ReadAsync())
                        {
                            isApprovalBody = true;

                            if (approvalReader["APPROV_USER"] != DBNull.Value &&
                                approvalReader["APPROV_USER"].ToString() == "FINAL")
                            {
                                isFinalApprovalBody = true;
                            }
                        }
                    }


                    var validation = await ValidateSalesOrder(con, POmodel, isFinalApprovalBody);
                    if (!validation.IsValid)
                    {
                        return Json(new { status = false, message = validation.Message });
                    }

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            var docid = $"{doctype}{POmodel.VNo}";

                            // Header Insert/Update
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PurchaseOrder_AE]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                if (POmodel.SaveOrUpdate == "Save")
                                {
                                    cmd.Parameters.AddWithValue("@Action", "HeaderInsert");
                                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId ?? (object)DBNull.Value);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@Action", "Update");
                                    cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId ?? (object)DBNull.Value);
                                }

                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@PLACE_CODE", POmodel.PlaceCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_TYPE", doctype ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_NO", POmodel.VNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_DATE", POmodel.VDate ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_TYPE", POmodel.WbType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_NO", POmodel.WbNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_CODE", POmodel.PartyCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_CODE", POmodel.ShipCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_FROM", POmodel.ShipFrom ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PRICE_TYPE", POmodel.PriceType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_REF", POmodel.PartyRef ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", POmodel.ImportCurrency ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EXRATE", POmodel.ExRate ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@NOS", POmodel.Nos ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QTY", POmodel.Qty ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AMOUNT", POmodel.Amount ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PACK_AMT", POmodel.PackAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISC_AMT", POmodel.DiscAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CGST_AMT", POmodel.CgstAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SGST_AMT", POmodel.SgstAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IGST_AMT", POmodel.IgstAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@OTH_AMT", POmodel.OthAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VAT_AMT", POmodel.VatAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CESS_PER", POmodel.CessPer ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CESS_AMT", POmodel.CessAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TCS_PER", POmodel.TcsPer ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TCS_AMT", POmodel.TcsAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@NET_AMT", POmodel.NetAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DELIVERY_TERM", POmodel.DeliveryTerm ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DELIVERY_DATE", POmodel.DeliveryDate ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VALIDITY_DATE", POmodel.ValidityDate ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRANSPORT_TERM", POmodel.TransportTerm ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PAYTERM_CODE", POmodel.PaytermCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PAYMENT_TERM", POmodel.PaymentTerm ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PRICE_TERM", POmodel.PriceTerm ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SAUDA_TYPE", POmodel.SaudaType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SAUDA_NO", POmodel.SaudaNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DELIVERY_PERIOD", POmodel.DeliveryPeriod ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DELIVERY_TO", POmodel.DeliveryTo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", POmodel.Remarks ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@POTYPE", POmodel.PoType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOC_ID", docid ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", "");
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", "");
                                cmd.Parameters.AddWithValue("@MAILSEND", POmodel.MailSend ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STATUS", POmodel.Status ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@CDISC_AMT", POmodel.CDiscAmt ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AUTOGEN_PO", POmodel.AutoGenPo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@POACCEPT_FLG", POmodel.PoAcceptFlg ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@POATTACH_PATH", POmodel.PoAttachPath ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@POATTCH_DATE", POmodel.PoAttachDate ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_ADD1", POmodel.BillAdd1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_ADD2", POmodel.BillAdd2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_ADD3", POmodel.BillAdd3 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_CITY", POmodel.BillCity ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_PINCODE", POmodel.BillPincode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_GST", POmodel.BillGst ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_ADD1", POmodel.ShipAdd1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_ADD2", POmodel.ShipAdd2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_ADD3", POmodel.ShipAdd3 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_CITY", POmodel.ShipCity ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_PINCODE", POmodel.ShipPincode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_GST", POmodel.ShipGst ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TAX_CODE", POmodel.TaxCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ITEM_TYPE", POmodel.ItemType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SUPPLY_TYPE", POmodel.SupplyType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRAN_TYPE", POmodel.TranType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FORM_CODE", POmodel.FormCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VEHICLE_NO", POmodel.VehicleNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INV_TYPE", POmodel.InvType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INV_NO", POmodel.InvNo ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_NAME", POmodel.PartyName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_NAME", POmodel.ShipName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EXWORK_FRT", POmodel.ExWorkFrt ?? (object)DBNull.Value);

                                cmd.ExecuteNonQuery();

                            }


                            // Footer Insert

                            string delQry = @"DELETE FROM ORDER2 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND 
                                                V_TYPE = @V_TYPE AND V_NO = @V_NO";
                            using (SqlCommand cmd = new SqlCommand(delQry, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", doctype);
                                cmd.Parameters.AddWithValue("@V_NO", POmodel.VNo ?? (object)DBNull.Value);

                                cmd.ExecuteNonQuery();
                            }

                            int i = 1;
                            foreach (var item in POmodel.ItemRecords)
                            {
                                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PurchaseOrder_AE]", con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@Action", "FooterInsert");
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", doctype ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_NO", POmodel.VNo ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_DATE", POmodel.VDate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SNO", i++);
                                    cmd.Parameters.AddWithValue("@PLACE_CODE", item.PlaceCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ITEM_NAME", item.ItemName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@MAKE_CODE", item.MakeCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@NOS", item.NOS ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@QTY", item.Qty);
                                    cmd.Parameters.AddWithValue("@ADJ_QTY", item.AdjQty ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@GATE_QTY", item.GateQty ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UOM_NAME", item.UomName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UOM_CODE", item.UomCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@RATE", item.Rate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IMPORT_RATE", item.ImportRate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CALC_RATE", item.CalcRate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AMOUNT", item.Amount ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PACK_PER", item.PackPer ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PACK_AMT", item.PackAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DISC_PER", item.DiscPer ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DISC_AMT", item.DiscAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TAX_CODE", item.TaxCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CGST_PER", item.CgstPer ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CGST_AMT", item.CgstAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SGST_PER", item.SgstPer ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SGST_AMT", item.SgstAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IGST_PER", item.IgstPer ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IGST_AMT", item.IgstAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@VAT_PER", item.VatPer ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@VAT_AMT", item.VatAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CESS_PER", item.CessPer ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CESS_AMT", item.CessAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@OTH_AMT", item.OthAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@NET_AMT", item.NetAmt ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LAND_RATE", item.LandRate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@STATUS", POmodel.Status ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PLACE_USE", item.PlaceUse ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DEPT_NAME", item.DeptName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REMARKS", item.Remarks ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PREORITY_LEVEL", item.PreorityLevel ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@PREORITY_REMARKS", item.PreorityRemarks ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@RATE_MONTHLY", item.RateMonthly ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@RATE_QUARTERLY", item.RateQuarterly ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@RATE_ANNUALY", item.RateAnnualy ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@RATE_SPECIAL", item.RateSpecial ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REQUEST_TYPE", item.RequestType ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@REQUEST_NO", item.RequestNo ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@APPROVAL_TYPE", item.ApprovalType ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@APPROVAL_NO", item.ApprovalNo ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DEPT_CODE", item.DeptCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DELIVERY_DATE", item.DeliveryDate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SAUDA_TYPE", POmodel.SaudaType ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SAUDA_NO", POmodel.SaudaNo ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DOC_ID", docid ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DISP_THROUGH", item.DispThrough ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DISP_REF", item.DispRef ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DISP_REMARKS", item.DispRemarks ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TENACITY_GRPCODE", item.TenacityGrpCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TENACITY_TYPE", item.TenacityType ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TENACITY_CODE", item.TenacityCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TENACITY_NAME", item.TenacityName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", "");
                                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", "");
                                    cmd.Parameters.AddWithValue("@COLOR_CODE", item.ColorCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@GRAM_CODE", item.GramCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return Json(new { status = true, message = "Data save/update successfully." });
                        }
                        catch (Exception ex)
                        {
                            transaction?.Rollback();
                            return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        private async Task<(bool IsValid, string Message)> ValidateSalesOrder(SqlConnection con, PurchaseOrder model, bool isFinalApprovalBody)
        {
            int fg = 0;
            int saudaICode = 0;
            decimal saudaRate = 0;

            var gv = _globalVariableService.GetGlobalVariables();
            var gs = await _globalVariableService.LoadGeneralSetting();

            // SAUDA / RATE VALIDATION
            if (gs.pubDefSSINSO == "Yes")
            {
                if (model.SaudaNo > 0)
                {
                    string sql = @"SELECT TOP 1 ITEM_CODE, RATE, V_DATE FROM SAUDA WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

                    using var cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@V_TYPE", model.SaudaType ?? "");
                    cmd.Parameters.AddWithValue("@V_NO", model.SaudaNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    using var dr = await cmd.ExecuteReaderAsync();

                    if (await dr.ReadAsync())
                    {
                        saudaICode = dr["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ITEM_CODE"]);
                        saudaRate = dr["RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["RATE"]);
                    }
                }

                foreach (var item in model.ItemRecords)
                {
                    string existSql = @"SELECT TOP 1 1 FROM RMDISC_MAST WHERE DTYPE='Sales' AND SAUDA_ITEM=@SAUDA_ITEM AND COMP_CODE=@COMP_CODE";

                    using var existCmd = new SqlCommand(existSql, con);
                    existCmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaICode);
                    existCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                    object existResult = await existCmd.ExecuteScalarAsync();

                    if (existResult != null)
                    {
                        string rateSql = @"SELECT TOP 1 ISNULL(RATE,0) FROM RMDISC_MAST WHERE ITEM_CODE=@ITEM_CODE AND COMP_CODE=@COMP_CODE AND SAUDA_ITEM=@SAUDA_ITEM
                                   AND EFF_DATE<@EFF_DATE ORDER BY EFF_DATE DESC";

                        using var rateCmd = new SqlCommand(rateSql, con);
                        rateCmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        rateCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        rateCmd.Parameters.AddWithValue("@SAUDA_ITEM", saudaICode);
                        rateCmd.Parameters.AddWithValue("@EFF_DATE", model.VDate);

                        object result = await rateCmd.ExecuteScalarAsync();

                        if (result == null || result == DBNull.Value)
                        {
                            fg = 1;
                        }
                        else
                        {
                            decimal rmRate = Convert.ToDecimal(result);

                            if (item.Rate > saudaRate + rmRate)
                                fg = 1;
                        }
                    }
                    else
                    {
                        string saudaCheckSql = @"SELECT TOP 1 1 FROM SAUDA WHERE V_TYPE='SAUD' AND V_NO=@V_NO AND PARTY_CODE=@PARTY_CODE AND ITEM_CODE=@ITEM_CODE
                                         AND RATE=@RATE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

                        using var saudaCheckCmd = new SqlCommand(saudaCheckSql, con);
                        saudaCheckCmd.Parameters.AddWithValue("@V_NO", model.SaudaNo);
                        saudaCheckCmd.Parameters.AddWithValue("@PARTY_CODE", model.PartyCode);
                        saudaCheckCmd.Parameters.AddWithValue("@ITEM_CODE", item.ItemCode);
                        saudaCheckCmd.Parameters.AddWithValue("@RATE", item.Rate);
                        saudaCheckCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        saudaCheckCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                        object saudaResult = await saudaCheckCmd.ExecuteScalarAsync();

                        if (saudaResult == null)
                            fg = 1;
                    }
                }
            }

            // APPROVAL REQUIRED
            if (!isFinalApprovalBody && fg == 1)
            {
                return (false, $"Item Name/Party Name/Rate not matched with data in SAUDA of Sauda No:{model.SaudaNo}, Approval Required.");
            }

            // SAUDA QTY VS ORDER QTY
            decimal currentOrderQty = model.ItemRecords.Sum(x => x.Qty);

            string sqlSaudaQty = @"SELECT ISNULL(SUM(QTY),0) FROM SAUDA WHERE V_TYPE='SAUD' AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

            using (var cmd = new SqlCommand(sqlSaudaQty, con))
            {
                cmd.Parameters.AddWithValue("@V_NO", model.SaudaNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                decimal saudaQty = Convert.ToDecimal(await cmd.ExecuteScalarAsync());

                string sqlOldOrderQty = @"SELECT ISNULL(SUM(QTY),0) FROM ORDER2 WHERE SAUDA_TYPE='SAUD' AND SAUDA_NO=@SAUDA_NO AND COMP_CODE=@COMP_CODE
                                            AND BRANCH_CODE=@BRANCH_CODE AND V_TYPE='SORD' AND V_NO<>@V_NO";

                using var cmd2 = new SqlCommand(sqlOldOrderQty, con);
                cmd2.Parameters.AddWithValue("@SAUDA_NO", model.SaudaNo);
                cmd2.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd2.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd2.Parameters.AddWithValue("@V_NO", model.VNo);

                decimal oldOrderQty = Convert.ToDecimal(await cmd2.ExecuteScalarAsync());

                if (Math.Round(currentOrderQty + oldOrderQty, 2) > saudaQty)
                {
                    if (gs.pubDefSSINSO == "Yes" &&
                        gv.PubCompCode != "2" &&
                        gv.PubCompCode != "5")
                    {
                        return (false, $"total Order Quantity({currentOrderQty + oldOrderQty}) is greater than Sauda Quantity ({saudaQty}).");
                    }
                }
            }

            // SALES QTY VS ORDER QTY
            string sqlSales = @"SELECT ITEM_CODE, b.SHORTNAME, SUM(QTY) AS QTY FROM SALE2 a LEFT JOIN ITEM_MAST b ON a.ITEM_CODE=b.CODE
                                AND b.COMP_CODE=a.COMP_CODE LEFT JOIN DOCTYPE_MAST c ON a.V_TYPE=c.CODE WHERE c.DOCTYPE='SalesInvoice'
                                AND a.STATUS<>2 AND ORD_TYPE='SORD' AND ORD_NO=@ORD_NO AND a.COMP_CODE=@COMP_CODE AND a.BRANCH_CODE=@BRANCH_CODE
                                GROUP BY a.ITEM_CODE,b.SHORTNAME";

            using var salesCmd = new SqlCommand(sqlSales, con);

            salesCmd.Parameters.AddWithValue("@ORD_NO", model.VNo);
            salesCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            salesCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

            using var reader = await salesCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int itemCode = Convert.ToInt32(reader["ITEM_CODE"]);
                string itemName = reader["SHORTNAME"] == DBNull.Value ? "" : reader["SHORTNAME"].ToString();
                decimal soldQty = Convert.ToDecimal(reader["QTY"]);
                
                var matchingItems = model.ItemRecords.Where(x => x.ItemCode == itemCode).ToList();

                // Item not found in Order
                if (!matchingItems.Any())
                {
                    return (false, $"Item :({itemCode}) {itemName} and Sale qty ={soldQty} not found in Order, serial no {model.VNo} as per sales record.");
                }

                decimal orderQty = matchingItems.Sum(x => x.Qty);

                // Sold Qty > Order Qty
                if (soldQty > orderQty)
                {
                    return (false, $"Item : {itemName} and Order Qty ({orderQty}) can not less than Sold Qty ({soldQty})");
                }
            }

            return (true, "");
        }
    }
}
