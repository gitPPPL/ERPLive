using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseRequestModel;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseRequestController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;

        public PurchaseRequestController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DropdownService dropdownService, DbHelper dbHelper,
            ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }

        public IActionResult Index()
        {
            var userLevel = _globalVariableService.GetGlobalVariables().PubUserLevel;

            ViewBag.UserLevel = userLevel;

            return View("~/Views/Purchase/Transaction/PurchaseRequest/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetVNo()
        {
            var result = _globalValidationdate.GetVNo("STPI", "PREQUEST1");
            return Json(new { status = true, V_NO = result });
        }

        //============Check IsApprovalBody=============
        [HttpGet]
        public JsonResult CheckIsApprovalBody()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            int result = 0;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                    SELECT 1 
                    FROM DOC_APPROSTAGE 
                    WHERE USER_CODE = @UserCode 
                    AND DOC_CODE = @DocCode 
                    AND COMP_CODE = @CompCode";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@UserCode", getdata.PubUserId);
                    cmd.Parameters.AddWithValue("@DocCode", "STPI");
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);

                    con.Open();
                    object queryResult = cmd.ExecuteScalar();

                    if (queryResult != null && queryResult != DBNull.Value)
                    {
                        result = Convert.ToInt32(queryResult);
                    }
                }
            }

            return Json(new { exists = result == 1 });
        }

        //===========Check IsFinalApprovalBody============
        [HttpGet]
        public async Task<JsonResult> CheckIsFinalApprovalBody()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string approvUser = null;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"
                    SELECT APPROV_USER 
                    FROM DOC_APPROSTAGE 
                    WHERE USER_CODE = @UserCode 
                    AND DOC_CODE = @DocCode 
                    AND COMP_CODE = @CompCode";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserCode", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@DocCode", "STPI");
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);

                        con.Open();
                        object result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            approvUser = result.ToString();
                        }
                    }
                }

                return Json(new { success = true, exists = approvUser == "FINAL" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public JsonResult GetDropdown(string type, int data = 0)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = "";

            switch (type)
            {
                case "Department":
                    
                    if (data == 1)
                    {
                        query = $@"
                        SELECT DISTINCT b.CODE, b.NAME FROM USER_DEPT a LEFT JOIN ITEMDEPT_MAST b ON a.DEPT_CODE = b.CODE 
                         WHERE   a.USER_CODE = {gv.PubUserId} AND a.COMP_CODE = '{gv.PubCompCode}' AND b.TRAN_TYPE = 'Store' ORDER BY b.NAME ASC";
                    }
                    else
                    {
                        query = $@"
                        SELECT DISTINCT b.CODE, b.NAME FROM USER_DEPT a LEFT JOIN ITEMDEPT_MAST b ON a.DEPT_CODE = b.CODE 
                         WHERE a.COMP_CODE = '{gv.PubCompCode}' AND b.TRAN_TYPE = 'Store' ORDER BY b.NAME ASC";
                    }
                        break;
                
                case "DocStatus":
                    query = $@"Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                    break;

                case "Place":
                    query = $@"
                        select CODE,NAME from PLACE_MAST  WHERE  COMP_CODE = {gv.PubCompCode}  AND  NAME <> ''  ORDER BY NAME asc";
                    break;

                case "Requester":
                    query = $@"
                        Select a.Code , a.Name from EMP_MAST a where a.RESIGN_DATE is null and a.COMP_CODE=  {gv.PubCompCode}  order by a.Name asc";
                    break;

                case "PlaceUse":
                    query = $@"
                        select CODE , NAME  From MACHINE_MAST where COMP_CODE = {gv.PubCompCode} and NAME <> '' order by NAME asc  ";
                    break;

                case "Make":
                    query = $@"
                        SELECT distinct  a.MAKE_CODE as Code , b.name as Name FROM ITEM_MAKE a LEFT JOIN ITEMMAKE_MAST b ON a.MAKE_CODE = b.CODE AND
                        b.comp_code = a.comp_code where a.ITEM_CODE ={data} and b.COMP_CODE = {gv.PubCompCode}  order by  b.name asc ;";
                    break;
                
                case "Priority":
                    query = $@"select CODE, NAME from DOCSTATUS_MAST WHERE V_TYPE = 'Preority' order by CODE";
                    break;
                
                case "WorkType":
                    query = $@"select CODE, NAME from DOCSTATUS_MAST WHERE V_TYPE = 'WorkType' order by name";
                    break;
                
                case "ApprovalStatus":
                    query = $@"select CODE, NAME from DOCSTATUS_MAST WHERE V_TYPE = 'Approval' and CODE in (4,5,8) order by name";
                    break;
            }

            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetddlItems(int deptid = 0)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "";
            if (compCode == "1")
            {   
                query += $@"SELECT b.CODE, b.name, c.NAME as Unit, c.CODE as UCode FROM item_dept a 
                        LEFT JOIN item_mast b ON a.ITEM_CODE = b.code AND b.ACTIVE = 1 AND b.comp_code = a.COMP_CODE 
                        LEFT JOIN ITEMUNIT_MAST c ON b.UNIT_CODE = c.CODE AND c.comp_code = 1  
                        LEFT JOIN ITEM_MGROUP d ON b.MGROUP_CODE = d.CODE AND d.comp_code = a.COMP_CODE  
                        WHERE a.comp_code = {compCode}  AND d.mgroup_type IN ('Store', 'Fuel') AND  
                        a.DEPT_CODE  = {deptid} 
                        and b.NAME <> '' 
                        ORDER BY b.name  asc ";
            }
            else
            {
                query += $@"
                    Select a.CODE, a.name, c.NAME as Unit, c.CODE as UCode
                    from item_mast a 
                    left outer join ITEMUNIT_MAST c on a.UNIT_CODE=c.CODE and c.comp_code=a.comp_Code 
                    Left join ITEM_MGROUP d on a.MGROUP_CODE=d.CODE and a.comp_code=d.comp_code
                    where a.comp_code={compCode} and a.active=1 and d.mgroup_type in ('Store','Fuel') 
                    group by a.name ,a.CODE, c.NAME ,c.CODE,a.HSN_CODE,a.CATLOG  order by a.name";
            }
                var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = dataList });
        }
        [HttpGet]
        public async Task<JsonResult> GetPlanList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT a.V_no as PlanNo, a.V_Type as PlanType, b.NAME as deptName, c.NAME as FaltName, d.NAME as MachName, format(a.V_date,'dd/MM/yyyy') as ComplainDate
                          FROM PM_MAINTENANCEPLAN a 
                          left join ItemDept_Mast b on a.Dept_code=b.code and a.comp_code=b.comp_code
                          Left join Falt_Mast c on a.Fault_code=c.Code and a.comp_code=c.comp_code
                          Left join Machine_Mast d on a.mach_code=d.Code and a.comp_code=d.comp_code
                           where a.comp_code={gv.PubCompCode} and a.Branch_code= {gv.PubBranchCode}
                          and a.Year_code={gv.PubFYearCode} and V_type='PMCP'  order by a.V_no asc";
            var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = dataList });
        }

        //======================================
        public JsonResult GetApporxiateRate(int Itemcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            decimal? approxRate = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                 SELECT TOP 1 APROX_RATE 
                 FROM PREQUEST2 
                 WHERE item_code = @Itemcode 
                 AND COMP_CODE = @CompCode 
                 AND Branch_Code = @BranchCode
                 ORDER BY v_date DESC, v_no DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Itemcode", Itemcode);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);


                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        approxRate = Convert.ToDecimal(result);
                    }
                }
            }
            return Json(new { Rate = approxRate });
        }
        
        public JsonResult GetPendingQty(int Itemcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            decimal? PendingQty = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //string query = @" SELECT sum(isnull(Qty,0)-isnull(ADJ_QTY,0)) AS RemainingQty FROM ORDER2 WHERE 
                //ITEM_CODE = @Itemcode AND Status = 1 AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode ";

                string query = $@"SELECT ISNULL(sum(isnull(b.Qty,0)-isnull(b.ADJ_QTY,0)),0)
                                FROM Order1 a 
                                left Join ORDER2 b on a.v_no=b.v_no and a.v_type=b.v_type and a.comp_code=b.comp_code and a.branch_code=b.branch_code 
                                and a.year_code=b.year_code 
                                WHERE b.ITEM_CODE=@Itemcode and a.status=1 and b.status=1 and a.COMP_CODE=@CompCode and a.BRANCH_CODE=@BranchCode";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Itemcode", Itemcode);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);

                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        PendingQty = Convert.ToDecimal(result);
                    }
                }
            }

            return Json(new { PendingQty = PendingQty });
        }
        
        public JsonResult GetTotal_Qty(int Itemcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            decimal? Total_Qty = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"   SELECT   SUM(ISNULL(req_Qty, 0) - ISNULL(ADJ_QTY, 0)) AS Total_Qty FROM  PREQUEST2 WHERE ITEM_CODE=@Itemcode
                and status=1 and COMP_CODE=@CompCode  and BRANCH_CODE=@BranchCode ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Itemcode", Itemcode);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        Total_Qty = Convert.ToDecimal(result);
                    }
                }
            }

            return Json(new { Total_Qty = Total_Qty });
        }
        
        public JsonResult GetTECH_DESC(int Itemcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            string? TECH_DESC = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                    Select top 1 TECH_DESC from PREQUEST2 where ITEM_CODE= @Itemcode   and ISNULL(TECH_DESC,'')<>'' 
                    and COMP_CODE=@CompCode  and BRANCH_CODE=@BranchCode and YEAR_CODE=@yearcode
                    Order by V_Date desc,V_NO desc;
                ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Itemcode", Itemcode);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);
                    cmd.Parameters.AddWithValue("@yearcode", getdata.PubFYearCode);

                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        TECH_DESC = Convert.ToString(result);
                    }
                }
            }

            return Json(new { TECH_DESC = TECH_DESC });
        }
        
        public JsonResult GetCurrentStock(int Itemcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            decimal? CurrentStocklist = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" Select isnull(QTY,0) from tmpStockBalance where ITEM_CODE=@Itemcode and Comp_code=@CompCode; ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Itemcode", Itemcode);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        CurrentStocklist = Convert.ToDecimal(result);
                    }
                }
            }

            return Json(new { CurrentStocklist = CurrentStocklist });
        }

        public JsonResult GetAvgConsumption(int itemCode, DateTime vDate)
        {
            var globalVars = _globalVariableService.GetGlobalVariables();
            if (vDate <= DateTime.MinValue.AddDays(90))
            {
                return Json(new { avgConsumption = 0, message = "Invalid date provided." });
            }
            DateTime endDate = vDate;
            DateTime startDate = vDate.AddDays(-90);
            decimal avgConsumption = 0;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //string query = @"Select isnull(sum(qty),0) from ISSUE2 where V_TYPE='SICO' and 
                //  item_code= @ItemCode and COMP_CODE=@CompCode   and BRANCH_CODE=@BranchCode   and v_date  between  @StartDate and @EndDate ";
                string query = @"Select isnull(sum(qty),0) from ISSUE2 where V_TYPE in ('SICO','BFIS','PRDI') and 
                  item_code= @ItemCode and COMP_CODE=@CompCode   and BRANCH_CODE=@BranchCode   and v_date  between  @StartDate and @EndDate ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@ItemCode", SqlDbType.Int).Value = itemCode;
                    cmd.Parameters.Add("@CompCode", SqlDbType.VarChar).Value = globalVars.PubCompCode;
                    cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = globalVars.PubBranchCode;
                    cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;
                    cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = endDate;

                    con.Open();

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        avgConsumption = Convert.ToDecimal(result);

                        avgConsumption = avgConsumption / 3;

                    }
                }
            }
            return Json(new { avgConsumption = avgConsumption });
        }

        //=============================================
        [HttpPost]
        public IActionResult SavedData([FromBody] PurchaseRequest_model request)
        {
            if (request?.Header == null)
                return Json(new { success = false, message = "Input model is null" });

            var action = request.Header.action == "INSERT" ? "Insert" : "Update";
            var result = SubmitRequest(request.Header, request.ItamDetails, request.PurchaseDocuments, action);

            return result == "Success"
                ? Json(new { success = true })
                : Json(new { success = false, message = result });
        }

        private string SubmitRequest(Header header, List<ItamDetails> itamDetails, List<PurchaseDocuments> purchaseDocuments, string action)
        {
            {
                try
                {
                    var g = _globalVariableService.GetGlobalVariables();
                    using var conn = _dbConnection.GetErpConnection();
                    conn.Open();

                    string deletePRequest2Sql = @"
                    DELETE FROM PREQUEST2 
                    WHERE COMP_CODE = @CompCode 
                    AND V_NO = @VNo 
                    AND BRANCH_CODE = @BranchCode 
                    AND YEAR_CODE = @YearCode and  V_TYPE = 'STPI';";
                    using (var deletePRequest2Cmd = conn.CreateCommand())
                    {
                        deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                        deletePRequest2Cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        deletePRequest2Cmd.Parameters.AddWithValue("@VNo", header.V_NO);
                        deletePRequest2Cmd.Parameters.AddWithValue("@BranchCode", 1);
                        deletePRequest2Cmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                        deletePRequest2Cmd.ExecuteNonQuery();
                    }

                    string deleteImgTableSql = @"
                    DELETE FROM IMG_TABLE 
                    WHERE COMP_CODE = @CompCode 
                    AND V_NO = @VNo 
                    AND BRANCH_CODE = @BranchCode 
                    AND V_TYPE = @V_TYPE
                    AND YEAR_CODE = @YearCode;";
                    using (var deleteImgTableCmd = conn.CreateCommand())
                    {
                        deleteImgTableCmd.CommandText = deleteImgTableSql;
                        deleteImgTableCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        deleteImgTableCmd.Parameters.AddWithValue("@VNo", header.V_NO);
                        deleteImgTableCmd.Parameters.AddWithValue("@BranchCode", 1);
                        deleteImgTableCmd.Parameters.AddWithValue("@V_TYPE", "STPI");
                        deleteImgTableCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                        deleteImgTableCmd.ExecuteNonQuery();
                    }

                    conn.Close();

                    conn.Open();
                    using (var cmd = new SqlCommand("sp_PurchaseReq1", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "Header");
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", "STPI");
                        cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                        cmd.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "STPI") + header.V_NO);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", header.DEPT_CODE);
                        cmd.Parameters.AddWithValue("@TARGET_DATE", header.TARGET_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REASON", header.REASON ?? "");
                        cmd.Parameters.AddWithValue("@PLACE_CODE", header.PLACE_CODE);
                        cmd.Parameters.AddWithValue("@URGENT_REQUEST", header.URGENT_REQUEST);
                        cmd.Parameters.AddWithValue("@status", header.STATUS);
                        cmd.Parameters.AddWithValue("@OWNER_CODE", header.OWNER_CODE);
                        cmd.Parameters.AddWithValue("@OWNER_NAME", header.OWNER_NAME);
                        cmd.Parameters.AddWithValue("@PLAN_NO", header.PLAN_NO);
                        cmd.Parameters.AddWithValue("@PLAN_TYPE", header.PLAN_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS ?? "");
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", header.FAPROV_STATUS ?? "");
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", header.FAPROV_REMARKS ?? "");
                        cmd.Parameters.AddWithValue("@USER_CODE", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.ExecuteNonQuery();
                    }

                    /// save dateails 

                    foreach (var d in itamDetails)
                    {
                        if (!d.ITEM_CODE.HasValue || d.ITEM_CODE == 0)
                            continue;
                        using var cmd2 = new SqlCommand("sp_PurchaseReq1", conn) { CommandType = CommandType.StoredProcedure };
                        cmd2.Parameters.AddWithValue("@Action", "INSERT");
                        cmd2.Parameters.AddWithValue("@SaveAction", "table");
                        cmd2.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd2.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd2.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd2.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd2.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                        cmd2.Parameters.AddWithValue("@V_TYPE", "STPI");
                        cmd2.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "STPI") + header.V_NO);
                        cmd2.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                        cmd2.Parameters.AddWithValue("@MAKE_CODE", d.MAKE_CODE);
                        cmd2.Parameters.AddWithValue("@DEPT_CODE", header.DEPT_CODE);
                        cmd2.Parameters.AddWithValue("@TECH_DESC", d.TECH_DESC ?? "");
                        cmd2.Parameters.AddWithValue("@UOM_CODE", d.UOM_CODE);
                        cmd2.Parameters.AddWithValue("@STD_REQ", d.STD_REQ);
                        cmd2.Parameters.AddWithValue("@CUR_STK", d.CUR_STK);
                        cmd2.Parameters.AddWithValue("@AVG_CONS", d.AVG_CONS ?? 0);
                        cmd2.Parameters.AddWithValue("@RESERVE_QTY", /*d.RESERVE_QTY*/10);
                        cmd2.Parameters.AddWithValue("@OPEN_POQTY", d.OPEN_POQTY);
                        cmd2.Parameters.AddWithValue("@OPEN_RQQTY", d.OPEN_RQQTY);
                        cmd2.Parameters.AddWithValue("@USER_QTY", d.USER_QTY);
                        cmd2.Parameters.AddWithValue("@REQ_QTY", d.REQ_QTY);
                        cmd2.Parameters.AddWithValue("@REQ_REASON", d.REQ_REASON ?? "");
                        cmd2.Parameters.AddWithValue("@REMARKS", d.REMARKS ?? "");
                        cmd2.Parameters.AddWithValue("@PLACE_USE", d.PLACE_USE ?? "");
                        cmd2.Parameters.AddWithValue("@PLACE_USECODE", d.PLACE_Code);
                        cmd2.Parameters.AddWithValue("@APROX_RATE", d.APROX_RATE);
                        
                        cmd2.Parameters.AddWithValue("@PRIORITY_CODE", d.PRIORITY_CODE ?? 0);
                        cmd2.Parameters.AddWithValue("@PRIORITY_TYPE", d.PRIORITY_TYPE ?? "");
                        
                        cmd2.Parameters.AddWithValue("@SCRAP_TYPE", d.SCRAP_TYPE ?? "");
                        
                        cmd2.Parameters.AddWithValue("@WORK_TYPECODE", d.WORK_TYPECODE ?? 0);
                        cmd2.Parameters.AddWithValue("@WORK_TYPE", d.WORK_TYPE ?? "");
                        
                        cmd2.Parameters.AddWithValue("@APROV_CODE", d.APROV_CODE ?? 0);
                        cmd2.Parameters.AddWithValue("@APROV_STATUS", d.APROV_STATUS ?? "");
                        
                        cmd2.Parameters.AddWithValue("@APROV_REMARKS", d.APROV_REMARKS ?? "");
                        cmd2.Parameters.AddWithValue("@STATUS", d.STATUS);
                        cmd2.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd2.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd2.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd2.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd2.Parameters.AddWithValue("@AED", "A");
                        cmd2.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd2.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd2.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd2.ExecuteNonQuery();
                    }

                    foreach (var Attachment in purchaseDocuments)
                    {

                        if (string.IsNullOrWhiteSpace(Attachment.FILE_NAME))
                            continue;
                        using var cmd3 = new SqlCommand("sp_PurchaseReq1", conn) { CommandType = CommandType.StoredProcedure };
                        cmd3.Parameters.AddWithValue("@Action", "INSERT");
                        cmd3.Parameters.AddWithValue("@SaveAction", "Documnets");
                        cmd3.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd3.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd3.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd3.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "STPI") + header.V_NO);
                        cmd3.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd3.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                        cmd3.Parameters.AddWithValue("@V_TYPE", "STPI");
                        cmd3.Parameters.AddWithValue("@FILE_NAME", Attachment.FILE_NAME);
                        cmd3.Parameters.AddWithValue("@FILE_Path", "/attachments/pan/" + (Attachment.FILE_Path ?? ""));
                        cmd3.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd3.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd3.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd3.Parameters.AddWithValue("@AED", "A");
                        cmd3.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd3.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd3.ExecuteNonQuery();
                    }
                    return "Success";
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }

        }

        //===========================Methods For Validation=======
        [HttpGet]
        public async Task<IActionResult> GetPurchaseRequests(int itemCode, int deptCode, int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string result = "";

                string query = @"
                SELECT CONCAT(a.V_TYPE, a.V_NO) AS DocNo
                FROM PREQUEST2 a
                INNER JOIN PREQUEST1 b 
                    ON a.V_TYPE = b.V_TYPE 
                    AND a.V_NO = b.V_NO 
                    AND a.COMP_CODE = b.COMP_CODE 
                    AND a.BRANCH_CODE = b.BRANCH_CODE
                WHERE a.ITEM_CODE = @ItemCode
                    AND b.DEPT_CODE = @DeptCode
                    AND ISNULL(a.STATUS, 0) = 1
                    AND a.COMP_CODE = @CompCode
                    AND a.BRANCH_CODE = @BranchCode
                    AND a.V_TYPE = 'STPI'
                    AND a.V_NO <> @VNo";

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                        cmd.Parameters.AddWithValue("@DeptCode", deptCode);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@VNo", vNo);

                        await conn.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                result = reader["DocNo"].ToString();
                            }
                        }
                    }
                }

                return Json (new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json (new
                {
                    success = false,
                    message = "Error occurred while fetching data" + ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemMake(int itemCode, int makeCode)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                bool result = false;

                string query = @"
                SELECT a.Make_Code
                FROM ITEM_MAKE a
                LEFT JOIN ITEMMAKE_MAST b 
                    ON a.MAKE_CODE = b.CODE 
                    AND a.COMP_CODE = b.COMP_CODE
                WHERE a.Item_Code = @ItemCode
                    AND a.Make_Code = @MakeCode
                    AND a.COMP_CODE = @CompCode";

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                    cmd.Parameters.AddWithValue("@MakeCode", makeCode);
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);

                    await conn.OpenAsync();

                    var value = await cmd.ExecuteScalarAsync();

                    result = (value != null);
                }

                return Json(new { success = true, exists = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckMonthlyReq(int itemCode)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                bool exists = false;

                string query = @"
                select 1 from item_mast a 
                left join ITEM_DEPT b on a.CODE=b.ITEM_CODE and a.COMP_CODE=b.COMP_CODE 
                left join ITEM_MGROUP c on a.MGROUP_CODE=c.CODE and a.comp_code=c.comp_code 
                where a.code= @ItemCode
                and a.active=1 and c.mgroup_type in ('Store','Fuel') and a.comp_code=@CompCode and isnull(b.DEPT_QTY,0)>0";
                //string query = @"
                //SELECT 1 
                //FROM item_mast 
                //WHERE code = @ItemCode 
                //    AND active = 1 
                //    AND comp_code = @CompCode 
                //    AND planning_method = 'MRP'";

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);

                    await conn.OpenAsync();

                    var value = await cmd.ExecuteScalarAsync();

                    exists = (value != null);
                }

                return Json(new { success = true, exists = exists});
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, exists = false});
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetMaxRequestCount(int vNo, DateTime vDate)
        {
            try
            {
                int count = 0;

                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();
                int maxRequest = gs.pubMaxRequestInADay;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"
                    SELECT COUNT(*) 
                    FROM PREQUEST1 
                    WHERE V_TYPE = 'STPI' 
                    AND V_NO <> @VNo 
                    AND V_DATE = @VDate 
                    AND UUSER = @UUser 
                    AND COMP_CODE = @CompCode 
                    AND BRANCH_CODE = @BranchCode 
                    AND YEAR_CODE = @YearCode";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@VNo", vNo);
                        cmd.Parameters.AddWithValue("@VDate", vDate);
                        cmd.Parameters.AddWithValue("@UUser", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                        await con.OpenAsync();
                        object result = await cmd.ExecuteScalarAsync();

                        if (result != null && result != DBNull.Value)
                        {
                            count = Convert.ToInt32(result);
                        }
                    }
                }

                bool isWithinLimit = count < maxRequest;
                
                return Json(new { success = true, isWithinLimit });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //===Check Modification Days
        [HttpGet]
        public JsonResult checkModificationDays(DateTime? vDate)
        {
            if (!vDate.HasValue)
            {
                return Json(new { success = false, message = "Doc Date is empty!!" });
            }
            var (allowed, message) = _globalValidationdate.CheckModificationDays(vDate.Value);
            return Json(new { success = true, isAllowed = allowed, message = message });
        }

        [HttpGet]
        public JsonResult GetApprovalStatus(int VNo)
        {
            string status = string.Empty;
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"SELECT FAPROV_STATUS
                                 FROM PREQUEST1
                                 WHERE v_type = @v_type
                                   AND v_NO = @v_NO
                                   AND comp_code = @comp_code
                                   AND branch_code = @branch_code
                                   AND year_code = @year_code";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@v_type", "STPI");
                        cmd.Parameters.AddWithValue("@v_NO", VNo);
                        cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@branch_code", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);

                        con.Open();

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            status = result.ToString().ToUpper();
                        }
                    }
                }

                return Json(new {Success = true, FAPROV_STATUS = status});
            }
            catch (Exception ex)
            {
                return Json(new {Success = false, Message = ex.Message});
            }
        }

        [HttpGet]
        public IActionResult ValidateDepartmentAccess(int deptCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            bool exists = false;

            try
            {
                string query = @"
                    SELECT TOP 1 1 
                    FROM USER_DEPT 
                    WHERE USER_CODE = @UserCode 
                      AND DEPT_CODE = @DeptCode 
                      AND COMP_CODE = @CompCode";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@UserCode", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@DeptCode", deptCode);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);

                        con.Open();

                        object result = cmd.ExecuteScalar();

                        if (result != null)
                            exists = true;
                    }
                }


                return Json(new { success = true, exists });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        //=============Modal Methods===============
            //=============Overall History==========
        [HttpGet]
        public JsonResult GetLastTenPurchaseRequest(List<int> itemCodes)
        {
            List<LastTenPurchaseRequestModel> list = new List<LastTenPurchaseRequestModel>();
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string itemCodeString = string.Join(",", itemCodes);



                string query = $@"
                    WITH RankedHistory AS (
                    SELECT b.ITEM_CODE as ItemCode, a.V_NO as VNo, format(a.V_DATE,'dd/MM/yyyy') as VDate, d.NAME as Department, c.NAME as ItemName,
                    e.NAME as MakeName,
                    f.NAME as Unit, b.REQ_QTY as Qty, b.PLACE_USE as PlaceofUse, b.TECH_DESC as TechDesc, a.Remarks, iif(a.STATUS=1, 'Open',
                    iif(a.STATUS=2,'Cancel','Close')) as Status, ROW_NUMBER() OVER (PARTITION BY b.ITEM_CODE ORDER BY a.v_date DESC) AS rn
                    FROM PREQUEST1 a 
                    left join PREQUEST2 b on a.V_No=b.V_No and a.V_TYPE=b.V_TYPE and a.COMP_CODE=b.COMP_CODE and a.BRANCH_CODE=b.BRANCH_CODE 
                    and a.YEAR_CODE=b.YEAR_CODE left join ITEM_MAST c on b.ITEM_CODE=c.CODE and b.comp_code=c.COMP_CODE left join ITEMDEPT_MAST d 
                    on a.DEPT_CODE=d.CODE and a.comp_code=d.COMP_CODE 
                    left join ITEMMAKE_MAST e on b.Make_CODE=e.CODE and b.comp_code=e.COMP_CODE 
                    left join ITEMUNIT_MAST f on b.UOM_CODE=f.CODE and b.comp_code=f.COMP_CODE 
                    where a.COMP_CODE=@CompCode and a.BRANCH_CODE=@BranchCode and a.V_TYPE='STPI' and b.ITEM_CODE in  (" + itemCodeString + @")
                    ) SELECT * FROM RankedHistory WHERE rn <= 10 ORDER BY ItemCode, rn";

                
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

                        con.Open();

                        SqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseRequestModel
                            {
                                ItemCode = dr["ItemCode"] != DBNull.Value ? Convert.ToInt32(dr["ItemCode"]) : 0,
                                VNo = dr["VNo"]?.ToString(),
                                VDate = dr["VDate"]?.ToString(),
                                Department = dr["Department"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                MakeName = dr["MakeName"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                PlaceofUse = dr["PlaceofUse"]?.ToString(),
                                TechDesc = dr["TechDesc"]?.ToString(),
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }

                        con.Close();
                    }
                }

                return Json(new {success = true, data = list});
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = ex.Message});
            }
        }
        
        [HttpGet]
        public JsonResult GetLastTenConsumptionDetails(List<int> itemCodes)
        {
            List<LastTenConsumptionModel> list = new List<LastTenConsumptionModel>();
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                string itemCodeString = string.Join(",", itemCodes);

                string query = $@"
                    WITH RankedHistory AS 
                    (
                        SELECT 
                            b.ITEM_CODE AS ItemCode, 
                            a.V_NO AS VNo, 
                            FORMAT(a.V_DATE, 'dd/MM/yyyy') AS Date, 
                            c.NAME AS ItemName, 
                            d.NAME AS Make, 
                            f.NAME AS Unit, 
                            b.QTY AS Qty, 
                            b.Rate, 
                            g.NAME AS Department, 
                            h.NAME AS Machine, 
                            a.Remarks, 
                            IIF(a.STATUS = 1, 'Open', IIF(a.STATUS = 2, 'Cancel', 'Close')) AS Status,
                            ROW_NUMBER() OVER (PARTITION BY b.ITEM_CODE ORDER BY a.V_DATE DESC) AS rn
                        FROM ISSUE1 a
                        LEFT JOIN ISSUE2 b 
                            ON a.V_NO = b.V_NO 
                            AND a.V_TYPE = b.V_TYPE 
                            AND a.COMP_CODE = b.COMP_CODE 
                            AND a.BRANCH_CODE = b.BRANCH_CODE 
                            AND a.YEAR_CODE = b.YEAR_CODE
                        LEFT JOIN ITEM_MAST c 
                            ON b.ITEM_CODE = c.CODE 
                            AND c.ACTIVE = 1 
                            AND b.COMP_CODE = c.COMP_CODE
                        LEFT JOIN ITEMMAKE_MAST d 
                            ON b.MAKE_CODE = d.CODE 
                            AND b.COMP_CODE = d.COMP_CODE
                        LEFT JOIN ITEMUNIT_MAST f 
                            ON c.UNIT_CODE = f.CODE 
                            AND c.COMP_CODE = f.COMP_CODE
                        LEFT JOIN ITEMDEPT_MAST g 
                            ON b.TO_DEPT = g.CODE 
                            AND b.COMP_CODE = g.COMP_CODE
                        LEFT JOIN MACHINE_MAST h 
                            ON b.MACH_CODE = h.CODE 
                            AND b.COMP_CODE = h.COMP_CODE
                        WHERE a.COMP_CODE = @CompCode 
                            AND a.BRANCH_CODE = @BranchCode
                            AND a.V_TYPE='SICO'
                            AND c.ACTIVE = 1 
                            AND b.ITEM_CODE IN ({itemCodeString})
                    )
                    SELECT * 
                    FROM RankedHistory 
                    WHERE rn <= 10 
                    ORDER BY ItemCode, rn";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

                        con.Open();

                        SqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            list.Add(new LastTenConsumptionModel
                            {
                                ItemCode = dr["ItemCode"] != DBNull.Value ? Convert.ToInt32(dr["ItemCode"]) : 0,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["Date"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                Department = dr["Department"]?.ToString(),
                                Machine = dr["Machine"]?.ToString(),
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }

                        con.Close();
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetLastTenPurchaseHistory(List<int> itemCodes)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string itemCodeString = string.Join(",", itemCodes);

                string query = $@"
                WITH RankedHistory AS
                (
                    SELECT
                        b.ITEM_CODE AS ItemCode,
                        a.V_NO AS VNo,
                        FORMAT(a.V_DATE,'dd/MM/yyyy') AS Date,
                        e.NAME AS Supplier,
                        c.NAME AS ItemName,
                        d.NAME AS Make,
                        f.NAME AS Unit,
                        b.BILL_QTY AS Qty,
                        b.RATE AS Rate,
                        b.OTH_AMT AS OthAmt,
                        b.CGST_PER AS CGSTPer,
                        b.SGST_PER AS SGSTPer,
                        b.IGST_PER AS IGSTPer,
                        b.PACK_PER AS PackPer,
                        b.DISC_PER AS DiscPer,
                        b.LAND_RATE AS LDRate,
                        a.REMARKS AS Remarks,
                        IIF(a.STATUS=1,'Open',IIF(a.STATUS=2,'Cancel','Close')) AS Status,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY b.ITEM_CODE
                            ORDER BY a.V_DATE DESC
                        ) AS rn
                    FROM PURCHASE1 a
                    LEFT JOIN PURCHASE2 b
                        ON a.V_NO = b.V_NO
                        AND a.V_TYPE = b.V_TYPE
                        AND a.COMP_CODE = b.COMP_CODE
                        AND a.BRANCH_CODE = b.BRANCH_CODE
                        AND a.YEAR_CODE = b.YEAR_CODE
                    LEFT JOIN ITEM_MAST c
                        ON b.ITEM_CODE = c.CODE
                        AND c.ACTIVE = 1
                        AND b.COMP_CODE = c.COMP_CODE
                    LEFT JOIN ITEMMAKE_MAST d
                        ON b.MAKE_CODE = d.CODE
                        AND b.COMP_CODE = d.COMP_CODE
                    LEFT JOIN SUBGROUP_MAST e
                        ON a.PARTY_CODE = e.CODE
                        AND a.COMP_CODE = e.COMP_CODE
                    LEFT JOIN ITEMUNIT_MAST f
                        ON c.UNIT_CODE = f.CODE
                        AND c.COMP_CODE = f.COMP_CODE
                    WHERE a.COMP_CODE = @CompCode
                        AND a.BRANCH_CODE = @BranchCode
                        AND a.V_TYPE IN
                        (
                            SELECT CODE
                            FROM DOCTYPE_MAST
                            WHERE DOCTYPE = 'PurchaseInvoice'
                        )
                        AND c.ACTIVE = 1
                        AND b.ITEM_CODE IN ({itemCodeString})
                )
                SELECT *
                FROM RankedHistory
                WHERE rn <= 10
                ORDER BY ItemCode,rn";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

                        con.Open();

                        SqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseHistoryModel
                            {
                                ItemCode = dr["ItemCode"] != DBNull.Value ? Convert.ToInt32(dr["ItemCode"]) : 0,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["Date"]?.ToString(),
                                Supplier = dr["Supplier"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                OthAmt = dr["OthAmt"] != DBNull.Value ? Convert.ToDecimal(dr["OthAmt"]) : 0,
                                CGSTPer = dr["CGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["CGSTPer"]) : 0,
                                SGSTPer = dr["SGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["SGSTPer"]) : 0,
                                IGSTPer = dr["IGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["IGSTPer"]) : 0,
                                PackPer = dr["PackPer"] != DBNull.Value ? Convert.ToDecimal(dr["PackPer"]) : 0,
                                DiscPer = dr["DiscPer"] != DBNull.Value ? Convert.ToDecimal(dr["DiscPer"]) : 0,
                                LDRate = dr["LDRate"] != DBNull.Value ? Convert.ToDecimal(dr["LDRate"]) : 0,
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }

                        con.Close();
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetLastTenOrderHistory(List<int> itemCodes)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string itemCodeString = string.Join(",", itemCodes);

                string query = $@"
                WITH RankedHistory AS 
                (
                    SELECT 
                        b.ITEM_CODE AS ItemCode,
                        a.V_NO AS VNo,
                        FORMAT(a.V_DATE,'dd/MM/yyyy') AS Date,
                        e.NAME AS Supplier,
                        c.NAME AS ItemName,
                        d.NAME AS Make,
                        f.NAME AS Unit,
                        b.QTY AS Qty,
                        b.RATE AS Rate,
                        b.OTH_AMT AS OthAmt,
                        b.CGST_PER AS CGSTPer,
                        b.SGST_PER AS SGSTPer,
                        b.IGST_PER AS IGSTPer,
                        b.PACK_PER AS PackPer,
                        b.DISC_PER AS DiscPer,
                        b.LAND_RATE AS LDRate,
                        a.REMARKS AS Remarks,
                        IIF(a.STATUS=1,'Open',IIF(a.STATUS=2,'Cancel','Close')) AS Status,
                        ROW_NUMBER() OVER 
                        (
                            PARTITION BY b.ITEM_CODE 
                            ORDER BY a.V_DATE DESC
                        ) AS rn
                    FROM order1 a
                    LEFT JOIN order2 b 
                        ON a.V_NO = b.V_NO 
                        AND a.V_TYPE = b.V_TYPE 
                        AND a.COMP_CODE = b.COMP_CODE 
                        AND a.BRANCH_CODE = b.BRANCH_CODE 
                        AND a.YEAR_CODE = b.YEAR_CODE
                    LEFT JOIN ITEM_MAST c 
                        ON b.ITEM_CODE = c.CODE 
                        AND c.ACTIVE = 1 
                        AND b.COMP_CODE = c.COMP_CODE
                    LEFT JOIN ITEMMAKE_MAST d 
                        ON b.MAKE_CODE = d.CODE 
                        AND b.COMP_CODE = d.COMP_CODE
                    LEFT JOIN SUBGROUP_MAST e 
                        ON a.PARTY_CODE = e.CODE 
                        AND a.COMP_CODE = e.COMP_CODE
                    LEFT JOIN ITEMUNIT_MAST f 
                        ON c.UNIT_CODE = f.CODE 
                        AND c.COMP_CODE = f.COMP_CODE
                    WHERE a.COMP_CODE = @CompCode
                        AND a.BRANCH_CODE = @BranchCode
                        AND a.V_TYPE IN 
                        (
                            SELECT CODE 
                            FROM DOCTYPE_MAST 
                            WHERE DOCTYPE = 'Purchaseorder'
                        )
                        AND c.ACTIVE = 1
                        AND b.ITEM_CODE IN ({itemCodeString})
                )
                SELECT * 
                FROM RankedHistory 
                WHERE rn <= 10 
                ORDER BY ItemCode, rn";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);

                        con.Open();
                        SqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseHistoryModel
                            {
                                ItemCode = dr["ItemCode"] != DBNull.Value ? Convert.ToInt32(dr["ItemCode"]) : 0,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["Date"]?.ToString(),
                                Supplier = dr["Supplier"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                OthAmt = dr["OthAmt"] != DBNull.Value ? Convert.ToDecimal(dr["OthAmt"]) : 0,
                                CGSTPer = dr["CGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["CGSTPer"]) : 0,
                                SGSTPer = dr["SGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["SGSTPer"]) : 0,
                                IGSTPer = dr["IGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["IGSTPer"]) : 0,
                                PackPer = dr["PackPer"] != DBNull.Value ? Convert.ToDecimal(dr["PackPer"]) : 0,
                                DiscPer = dr["DiscPer"] != DBNull.Value ? Convert.ToDecimal(dr["DiscPer"]) : 0,
                                LDRate = dr["LDRate"] != DBNull.Value ? Convert.ToDecimal(dr["LDRate"]) : 0,
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }

                        con.Close();
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

           //===================Row Wise History============
        [HttpGet]
        public JsonResult GetItemWisePurchaseRequest(int itemCode)
        {
            List<LastTenPurchaseRequestModel> list = new List<LastTenPurchaseRequestModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string query = @"
                SELECT TOP 10 
                    a.V_NO AS VNo,
                    FORMAT(a.V_DATE,'dd/MM/yyyy') AS VDate,
                    d.NAME AS Department,
                    c.NAME AS ItemName,
                    e.NAME AS MakeName,
                    f.NAME AS Unit,
                    b.REQ_QTY AS Qty,
                    b.PLACE_USE AS PlaceofUse,
                    b.TECH_DESC AS TechDesc,
                    a.Remarks,
                    IIF(a.STATUS=1,'Open',IIF(a.STATUS=2,'Cancel','Close')) AS Status
                FROM PREQUEST1 a
                LEFT JOIN PREQUEST2 b 
                    ON a.V_No = b.V_No 
                    AND a.V_TYPE = b.V_TYPE 
                    AND a.COMP_CODE = b.COMP_CODE 
                    AND a.BRANCH_CODE = b.BRANCH_CODE
                    AND a.YEAR_CODE = b.YEAR_CODE
                LEFT JOIN ITEM_MAST c 
                    ON b.ITEM_CODE = c.CODE 
                    AND b.COMP_CODE = c.COMP_CODE
                LEFT JOIN ITEMDEPT_MAST d 
                    ON a.DEPT_CODE = d.CODE 
                    AND a.COMP_CODE = d.COMP_CODE
                LEFT JOIN ITEMMAKE_MAST e 
                    ON b.MAKE_CODE = e.CODE 
                    AND b.COMP_CODE = e.COMP_CODE
                LEFT JOIN ITEMUNIT_MAST f 
                    ON b.UOM_CODE = f.CODE 
                    AND b.COMP_CODE = f.COMP_CODE
                WHERE a.COMP_CODE = @CompCode
                    AND a.BRANCH_CODE = @BranchCode
                    AND a.V_TYPE = 'STPI'
                    AND b.ITEM_CODE = @ItemCode
                    AND c.ACTIVE = 1
                ORDER BY a.V_DATE DESC";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new LastTenPurchaseRequestModel
                                {
                                    ItemCode = itemCode,
                                    VNo = dr["VNo"]?.ToString(),
                                    VDate = dr["VDate"]?.ToString(),
                                    Department = dr["Department"]?.ToString(),
                                    ItemName = dr["ItemName"]?.ToString(),
                                    MakeName = dr["MakeName"]?.ToString(),
                                    Unit = dr["Unit"]?.ToString(),
                                    Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                    PlaceofUse = dr["PlaceofUse"]?.ToString(),
                                    TechDesc = dr["TechDesc"]?.ToString(),
                                    Remarks = dr["Remarks"]?.ToString(),
                                    Status = dr["Status"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetItemWiseConsumptionHistory(int itemCode)
        {
            List<LastTenConsumptionModel> list = new List<LastTenConsumptionModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string query = @"
                SELECT TOP 10 
                    a.V_NO AS VNo,
                    FORMAT(a.V_DATE,'dd/MM/yyyy') AS VDate,
                    c.NAME AS ItemName,
                    d.NAME AS Make,
                    f.NAME AS Unit,
                    b.QTY AS Qty,
                    b.RATE AS Rate,
                    g.NAME AS Department,
                    h.NAME AS Machine,
                    a.REMARKS AS Remarks,
                    IIF(a.STATUS=1,'Open',
                        IIF(a.STATUS=2,'Cancel','Close')) AS Status
                FROM ISSUE1 a
                LEFT JOIN ISSUE2 b 
                    ON a.V_No = b.V_No 
                    AND a.V_TYPE = b.V_TYPE 
                    AND a.COMP_CODE = b.COMP_CODE 
                    AND a.BRANCH_CODE = b.BRANCH_CODE 
                    AND a.YEAR_CODE = b.YEAR_CODE
                LEFT JOIN ITEM_MAST c 
                    ON b.ITEM_CODE = c.CODE 
                    AND c.COMP_CODE = a.COMP_CODE
                LEFT JOIN ITEMMAKE_MAST d 
                    ON b.MAKE_CODE = d.CODE 
                    AND d.COMP_CODE = a.COMP_CODE
                LEFT JOIN ITEMUNIT_MAST f 
                    ON c.UNIT_CODE = f.CODE 
                    AND f.COMP_CODE = a.COMP_CODE
                LEFT JOIN ITEMDEPT_MAST g 
                    ON b.TO_DEPT = g.CODE 
                    AND g.COMP_CODE = a.COMP_CODE
                LEFT JOIN MACHINE_MAST h 
                    ON b.MACH_CODE = h.CODE 
                    AND h.COMP_CODE = a.COMP_CODE
                WHERE a.COMP_CODE = @CompCode
                    AND a.BRANCH_CODE = @BranchCode
                    AND a.V_TYPE = 'SICO'
                    AND b.ITEM_CODE = @ItemCode
                    AND c.ACTIVE = 1
                ORDER BY a.V_DATE DESC";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new LastTenConsumptionModel
                            {
                                ItemCode = itemCode,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["VDate"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                Department = dr["Department"]?.ToString(),
                                Machine = dr["Machine"]?.ToString(),
                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetItemWisePurchaseOrderHistory(int itemCode)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string query = @"
                SELECT TOP 10 
                    a.V_NO AS VNo,
                    FORMAT(a.V_DATE,'dd/MM/yyyy') AS VDate,
                    e.NAME AS Supplier,
                    c.NAME AS ItemName,
                    d.NAME AS Make,
                    f.NAME AS Unit,
                    b.QTY AS Qty,
                    b.RATE AS Rate,
                    b.OTH_AMT AS OthAmt,
                    b.CGST_PER AS CGSTPer,
                    b.SGST_PER AS SGSTPer,
                    b.IGST_PER AS IGSTPer,
                    b.PACK_PER AS PackPer,
                    b.DISC_PER AS DiscPer,
                    b.LAND_RATE AS LDRate,
                    a.REMARKS AS Remarks,
                    IIF(a.STATUS=1,'Open',
                        IIF(a.STATUS=2,'Cancel','Close')) AS Status
                FROM order1 a
                LEFT JOIN order2 b 
                    ON a.V_No = b.V_No 
                    AND a.V_TYPE = b.V_TYPE 
                    AND a.COMP_CODE = b.COMP_CODE 
                    AND a.BRANCH_CODE = b.BRANCH_CODE 
                    AND a.YEAR_CODE = b.YEAR_CODE

                LEFT JOIN ITEM_MAST c 
                    ON b.ITEM_CODE = c.CODE 
                    AND c.COMP_CODE = a.COMP_CODE

                LEFT JOIN ITEMMAKE_MAST d 
                    ON b.MAKE_CODE = d.CODE 
                    AND d.COMP_CODE = a.COMP_CODE

                LEFT JOIN SUBGROUP_MAST e 
                    ON a.PARTY_CODE = e.CODE 
                    AND e.COMP_CODE = a.COMP_CODE

                LEFT JOIN ITEMUNIT_MAST f 
                    ON c.UNIT_CODE = f.CODE 
                    AND f.COMP_CODE = a.COMP_CODE

                WHERE a.COMP_CODE = @CompCode
                    AND a.BRANCH_CODE = @BranchCode
                    AND a.V_TYPE IN (
                        SELECT CODE 
                        FROM DOCTYPE_MAST 
                        WHERE DOCTYPE = 'Purchaseorder'
                    )
                    AND c.ACTIVE = 1
                    AND b.ITEM_CODE = @ItemCode

                ORDER BY a.V_DATE DESC";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseHistoryModel
                            {
                                ItemCode = itemCode,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["VDate"]?.ToString(),
                                Supplier = dr["Supplier"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),

                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                OthAmt = dr["OthAmt"] != DBNull.Value ? Convert.ToDecimal(dr["OthAmt"]) : 0,

                                CGSTPer = dr["CGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["CGSTPer"]) : 0,
                                SGSTPer = dr["SGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["SGSTPer"]) : 0,
                                IGSTPer = dr["IGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["IGSTPer"]) : 0,

                                PackPer = dr["PackPer"] != DBNull.Value ? Convert.ToDecimal(dr["PackPer"]) : 0,
                                DiscPer = dr["DiscPer"] != DBNull.Value ? Convert.ToDecimal(dr["DiscPer"]) : 0,

                                LDRate = dr["LDRate"] != DBNull.Value ? Convert.ToDecimal(dr["LDRate"]) : 0,

                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetItemWisePurchaseQuotationHistory(int itemCode)
        {
            List<ItemWisePurchaseQuotationHistoryModel> list = new List<ItemWisePurchaseQuotationHistoryModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string query = @"
                SELECT TOP 10
                    a.V_NO AS VNo,
                    FORMAT(a.V_DATE,'dd/MM/yyyy') AS VDate,
                    e.NAME AS Supplier,
                    c.NAME AS ItemName,
                    d.NAME AS Make,
                    f.NAME AS Unit,
                    a.GROUP_NO AS GroupNo,
                    b.QTY AS Qty,
                    b.RATE AS Rate,
                    b.FREIGHT AS Freight,
                    b.CGST_PER AS CGSTPer,
                    b.SGST_PER AS SGSTPer,
                    b.IGST_PER AS IGSTPer,
                    b.PACK_PER AS PackPer,
                    b.DISC_PER AS DiscPer,
                    b.OTH_EXPS AS OthExps,
                    b.LD_RATE AS LDRate,
                    a.REMARKS AS Remarks,
                    IIF(a.STATUS = 1, 'Open',
                        IIF(a.STATUS = 2, 'Cancel', 'Close')) AS Status
                FROM QUOTATION1 a

                LEFT JOIN QUOTATION2 b
                    ON a.V_NO = b.V_NO
                    AND a.V_TYPE = b.V_TYPE
                    AND a.COMP_CODE = b.COMP_CODE
                    AND a.BRANCH_CODE = b.BRANCH_CODE
                    AND a.YEAR_CODE = b.YEAR_CODE

                LEFT JOIN ITEM_MAST c
                    ON b.ITEM_CODE = c.CODE
                    AND c.COMP_CODE = a.COMP_CODE

                LEFT JOIN ITEMMAKE_MAST d
                    ON b.MAKE_CODE = d.CODE
                    AND d.COMP_CODE = a.COMP_CODE

                LEFT JOIN SUBGROUP_MAST e
                    ON a.PARTY_CODE = e.CODE
                    AND e.COMP_CODE = a.COMP_CODE

                LEFT JOIN ITEMUNIT_MAST f
                    ON c.UNIT_CODE = f.CODE
                    AND f.COMP_CODE = a.COMP_CODE

                WHERE a.COMP_CODE = @CompCode
                    AND a.BRANCH_CODE = @BranchCode
                    AND a.V_TYPE IN (
                        SELECT CODE
                        FROM DOCTYPE_MAST
                        WHERE DOCTYPE = 'Purchasequotation'
                    )
                    AND c.ACTIVE = 1
                    AND b.ITEM_CODE = @ItemCode

                ORDER BY a.V_DATE DESC";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new ItemWisePurchaseQuotationHistoryModel
                            {
                                ItemCode = itemCode,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["VDate"]?.ToString(),
                                Supplier = dr["Supplier"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),
                                GroupNo = dr["GroupNo"]?.ToString(),

                                Qty = dr["Qty"] != DBNull.Value ? Convert.ToDecimal(dr["Qty"]) : 0,
                                Rate = dr["Rate"] != DBNull.Value ? Convert.ToDecimal(dr["Rate"]) : 0,
                                Freight = dr["Freight"] != DBNull.Value ? Convert.ToDecimal(dr["Freight"]) : 0,

                                CGSTPer = dr["CGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["CGSTPer"]) : 0,
                                SGSTPer = dr["SGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["SGSTPer"]) : 0,
                                IGSTPer = dr["IGSTPer"] != DBNull.Value ? Convert.ToDecimal(dr["IGSTPer"]) : 0,

                                PackPer = dr["PackPer"] != DBNull.Value ? Convert.ToDecimal(dr["PackPer"]) : 0,
                                DiscPer = dr["DiscPer"] != DBNull.Value ? Convert.ToDecimal(dr["DiscPer"]) : 0,

                                OthExps = dr["OthExps"] != DBNull.Value ? Convert.ToDecimal(dr["OthExps"]) : 0,
                                LDRate = dr["LDRate"] != DBNull.Value ? Convert.ToDecimal(dr["LDRate"]) : 0,

                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetItemWisePurchaseReceiptHistory(int itemCode)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string query = @"
                SELECT TOP 10
                    a.V_NO AS VNo,
                    FORMAT(a.V_DATE,'dd/MM/yyyy') AS VDate,
                    e.NAME AS Supplier,
                    c.NAME AS ItemName,
                    d.NAME AS Make,
                    f.NAME AS Unit,
                    b.RECD_QTY AS Qty,
                    b.RATE AS Rate,
                    b.OTH_AMT AS OthAmt,
                    b.CGST_PER AS CGSTPer,
                    b.SGST_PER AS SGSTPer,
                    b.IGST_PER AS IGSTPer,
                    b.PACK_PER AS PackPer,
                    b.DISC_PER AS DiscPer,
                    b.LAND_RATE AS LDRate,
                    a.REMARKS AS Remarks,
                    IIF(a.STATUS = 1, 'Open',
                        IIF(a.STATUS = 2, 'Cancel', 'Close')) AS Status
                FROM PURCHASE1 a

                LEFT JOIN PURCHASE2 b
                    ON a.V_NO = b.V_NO
                    AND a.V_TYPE = b.V_TYPE
                    AND a.COMP_CODE = b.COMP_CODE
                    AND a.BRANCH_CODE = b.BRANCH_CODE
                    AND a.YEAR_CODE = b.YEAR_CODE

                LEFT JOIN ITEM_MAST c
                    ON b.ITEM_CODE = c.CODE
                    AND c.COMP_CODE = a.COMP_CODE

                LEFT JOIN ITEMMAKE_MAST d
                    ON b.MAKE_CODE = d.CODE
                    AND d.COMP_CODE = a.COMP_CODE

                LEFT JOIN SUBGROUP_MAST e
                    ON a.PARTY_CODE = e.CODE
                    AND e.COMP_CODE = a.COMP_CODE

                LEFT JOIN ITEMUNIT_MAST f
                    ON c.UNIT_CODE = f.CODE
                    AND f.COMP_CODE = a.COMP_CODE

                WHERE a.COMP_CODE = @CompCode
                    AND a.BRANCH_CODE = @BranchCode
                    AND a.V_TYPE IN
                    (
                        SELECT CODE
                        FROM DOCTYPE_MAST
                        WHERE DOCTYPE = 'materialreceipt'
                    )
                    AND c.ACTIVE = 1
                    AND b.ITEM_CODE = @ItemCode

                ORDER BY a.V_DATE DESC";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseHistoryModel
                            {
                                ItemCode = itemCode,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["VDate"]?.ToString(),
                                Supplier = dr["Supplier"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),

                                Qty = dr["Qty"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["Qty"])
                                    : 0,

                                Rate = dr["Rate"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["Rate"])
                                    : 0,

                                OthAmt = dr["OthAmt"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["OthAmt"])
                                    : 0,

                                CGSTPer = dr["CGSTPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["CGSTPer"])
                                    : 0,

                                SGSTPer = dr["SGSTPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["SGSTPer"])
                                    : 0,

                                IGSTPer = dr["IGSTPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["IGSTPer"])
                                    : 0,

                                PackPer = dr["PackPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["PackPer"])
                                    : 0,

                                DiscPer = dr["DiscPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["DiscPer"])
                                    : 0,

                                LDRate = dr["LDRate"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["LDRate"])
                                    : 0,

                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetItemWisePurchaseHistory(int itemCode)
        {
            List<LastTenPurchaseHistoryModel> list = new List<LastTenPurchaseHistoryModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                string query = @"
                SELECT TOP 10
                    a.V_NO AS VNo,
                    FORMAT(a.V_DATE,'dd/MM/yyyy') AS VDate,
                    e.NAME AS Supplier,
                    c.NAME AS ItemName,
                    d.NAME AS Make,
                    f.NAME AS Unit,
                    b.BILL_QTY AS Qty,
                    b.RATE AS Rate,
                    b.OTH_AMT AS OthAmt,
                    b.CGST_PER AS CGSTPer,
                    b.SGST_PER AS SGSTPer,
                    b.IGST_PER AS IGSTPer,
                    b.PACK_PER AS PackPer,
                    b.DISC_PER AS DiscPer,
                    b.LAND_RATE AS LDRate,
                    a.REMARKS AS Remarks,
                    IIF(a.STATUS = 1, 'Open',
                        IIF(a.STATUS = 2, 'Cancel', 'Close')) AS Status
                FROM PURCHASE1 a

                LEFT JOIN PURCHASE2 b
                    ON a.V_NO = b.V_NO
                    AND a.V_TYPE = b.V_TYPE
                    AND a.COMP_CODE = b.COMP_CODE
                    AND a.BRANCH_CODE = b.BRANCH_CODE
                    AND a.YEAR_CODE = b.YEAR_CODE

                LEFT JOIN ITEM_MAST c
                    ON b.ITEM_CODE = c.CODE
                    AND c.COMP_CODE = a.COMP_CODE

                LEFT JOIN ITEMMAKE_MAST d
                    ON b.MAKE_CODE = d.CODE
                    AND d.COMP_CODE = a.COMP_CODE

                LEFT JOIN SUBGROUP_MAST e
                    ON a.PARTY_CODE = e.CODE
                    AND e.COMP_CODE = a.COMP_CODE

                LEFT JOIN ITEMUNIT_MAST f
                    ON c.UNIT_CODE = f.CODE
                    AND f.COMP_CODE = a.COMP_CODE

                WHERE a.COMP_CODE = @CompCode
                    AND a.BRANCH_CODE = @BranchCode
                    AND a.V_TYPE IN
                    (
                        SELECT CODE
                        FROM DOCTYPE_MAST
                        WHERE DOCTYPE = 'PurchaseInvoice'
                    )
                    AND c.ACTIVE = 1
                    AND b.ITEM_CODE = @ItemCode

                ORDER BY a.V_DATE DESC";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new LastTenPurchaseHistoryModel
                            {
                                ItemCode = itemCode,
                                VNo = dr["VNo"]?.ToString(),
                                Date = dr["VDate"]?.ToString(),
                                Supplier = dr["Supplier"]?.ToString(),
                                ItemName = dr["ItemName"]?.ToString(),
                                Make = dr["Make"]?.ToString(),
                                Unit = dr["Unit"]?.ToString(),

                                Qty = dr["Qty"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["Qty"])
                                    : 0,

                                Rate = dr["Rate"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["Rate"])
                                    : 0,

                                OthAmt = dr["OthAmt"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["OthAmt"])
                                    : 0,

                                CGSTPer = dr["CGSTPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["CGSTPer"])
                                    : 0,

                                SGSTPer = dr["SGSTPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["SGSTPer"])
                                    : 0,

                                IGSTPer = dr["IGSTPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["IGSTPer"])
                                    : 0,

                                PackPer = dr["PackPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["PackPer"])
                                    : 0,

                                DiscPer = dr["DiscPer"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["DiscPer"])
                                    : 0,

                                LDRate = dr["LDRate"] != DBNull.Value
                                    ? Convert.ToDecimal(dr["LDRate"])
                                    : 0,

                                Remarks = dr["Remarks"]?.ToString(),
                                Status = dr["Status"]?.ToString()
                            });
                        }
                    }
                }

                return Json(new { success = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

}