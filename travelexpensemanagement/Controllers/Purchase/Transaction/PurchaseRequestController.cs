using iTextSharp.text.pdf.parser.clipper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.X509;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using static Azure.Core.HttpHeader;
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

        public PurchaseRequestController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DropdownService dropdownService, DbHelper dbHelper,
            ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PurchaseRequest/Index.cshtml");
        }
        public JsonResult GetVNo()
        {
            string newV_NO = "00000";
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
                    string lastV_NO_Query = "SELECT MAX(V_NO) FROM PREQUEST1 WHERE COMP_CODE = @CompCode AND YEAR_CODE = @YearCode  and PREQUEST1.BRANCH_CODE = @BRANCH_CODE  and V_TYPE = 'STPI'  ";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    object result = lastVnoCmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        int lastV_NO = Convert.ToInt32(result);
                        newV_NO = (lastV_NO + 1).ToString("D5");
                    }
                    else
                    {
                        newV_NO = prefixYR + "00001";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }
        public JsonResult DDLDeptMast()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                string query = $@" SELECT DISTINCT b.CODE, b.NAME FROM USER_DEPT a LEFT JOIN ITEMDEPT_MAST b ON a.DEPT_CODE = b.CODE 
                WHERE   a.USER_CODE = {getdata.PubUserId} AND a.COMP_CODE = '{getdata.PubCompCode}' AND b.TRAN_TYPE = 'Store' ORDER BY b.NAME ASC;";

                var DeptList = _dropdownService.GetDropdownList(query);
                return Json(DeptList);
            }
        }

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
                string query = @" SELECT sum(isnull(Qty,0)-isnull(ADJ_QTY,0)) AS RemainingQty FROM ORDER2 WHERE 
                ITEM_CODE = @Itemcode AND Status = 1 AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode ";

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
                string query = @"Select isnull(sum(qty),0) from ISSUE2 where V_TYPE='SICO' and 
                  item_code= @ItemCode and COMP_CODE=@CompCode   and BRANCH_CODE=@BranchCode   and v_date  between  @StartDate and @EndDate ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@ItemCode", SqlDbType.Int).Value = itemCode;
                    cmd.Parameters.Add("@CompCode", SqlDbType.VarChar).Value = globalVars.PubCompCode;
                    cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = 1;
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
        // end

        public JsonResult GetUnit(int Itemcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            List<object> resultList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" SELECT   A.UNIT_CODE , A.UNIT_NAME   FROM item_mast A
                WHERE A.COMP_CODE = @CompCode AND A.CODE = @Itemcode   AND A.NAME <> '' order by A.UNIT_NAME asc  ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Itemcode", Itemcode);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultList.Add(new
                            {
                                CODE = reader["UNIT_CODE"],
                                NAME = reader["UNIT_NAME"]
                            });
                        }
                    }
                }
            }

            return Json(resultList);
        }
        public JsonResult DDLplaceMast()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE,NAME from PLACE_MAST  WHERE  COMP_CODE = " + getdata.PubCompCode + "  AND  NAME <> ''  ORDER BY NAME asc";

                var PlaceList = _dropdownService.GetDropdownList(query);
                return Json(PlaceList);
            }

        }
        public JsonResult DDLRequester()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = " Select a.Code , a.Name from EMP_MAST a where a.RESIGN_DATE is null and a.COMP_CODE=  " + getdata.PubCompCode + "  order by a.Name asc ";

                var RequesterList = _dropdownService.GetDropdownList(query);
                return Json(RequesterList);
            }

        }
        public JsonResult DDLComplainNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT a.V_no VNo ,  a.V_Type FROM PM_MAINTENANCEPLAN a " +
                  "left join ItemDept_Mast b on a.Dept_code=b.code and a.comp_code=b.comp_code " +
                  "Left join Falt_Mast c on a.Fault_code=c.Code and a.comp_code=c.comp_code " +
                  "Left join Machine_Mast d on a.mach_code=d.Code and a.comp_code=d.comp_code" +
                  " where a.comp_code=" + getdata.PubCompCode + " and a.Branch_code= 1" +
                  "and a.Year_code=" + getdata.PubFYearCode + " and V_type='PMCP'  order by a.V_no asc ";
                var ComplainNoList = _dropdownService.GetDropdownList(query);
                return Json(ComplainNoList);
            }
        }
        public JsonResult DDLplaceUse()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = " select CODE , NAME  From MACHINE_MAST where COMP_CODE = " + getdata.PubCompCode + " and NAME <> '' order by NAME asc  ";
                var PlaceUseList = _dropdownService.GetDropdownList(query);
                return Json(PlaceUseList);
            }

        }
        public JsonResult DDLMake(int itemid)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                string query = "SELECT   a.MAKE_CODE  , b.name  FROM ITEM_MAKE a LEFT JOIN ITEMMAKE_MAST b ON a.MAKE_CODE = b.CODE AND " +
                    "b.comp_code = a.comp_code where b.CODE =" + itemid + " and b.COMP_CODE = " + getdata.PubCompCode + "  order by  b.name asc ;";

                var MakeList = _dropdownService.GetDropdownList(query);

                return Json(MakeList);
            }

        }
        public JsonResult DDLItemMast(int deptid = 0)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "";
                query ="SELECT  b.CODE ,b.name  " + "FROM item_dept a " +
                "LEFT JOIN item_mast b ON a.ITEM_CODE = b.code AND b.ACTIVE = 1 AND b.comp_code = " + getdata.PubCompCode + " " +
                "LEFT OUTER JOIN ITEMUNIT_MAST c ON b.UNIT_CODE = c.CODE AND c.comp_code = " + getdata.PubCompCode + "  " +
                "LEFT JOIN ITEM_MGROUP d ON b.MGROUP_CODE = d.CODE AND d.comp_code = " + getdata.PubCompCode + "  " +
                "WHERE a.comp_code = " + getdata.PubCompCode + "  AND d.mgroup_type IN ('Store', 'Fuel') AND  a.DEPT_CODE <> 0 or  a.DEPT_CODE  = " + deptid + " and b.NAME <> ''   " +
                "GROUP BY b.name, b.CODE " + "ORDER BY b.name  asc ;";
                var IitemMastList = _dropdownService.GetDropdownList(query);
                return Json(IitemMastList);
            }
        }

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
                        cmd2.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd2.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd2.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                        cmd2.Parameters.AddWithValue("@V_TYPE", "STPI");
                        cmd2.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                        cmd2.Parameters.AddWithValue("@MAKE_CODE", d.MAKE_CODE);
                        cmd2.Parameters.AddWithValue("@DEPT_CODE", header.DEPT_CODE);
                        cmd2.Parameters.AddWithValue("@TECH_DESC", d.TECH_DESC ?? "");
                        cmd2.Parameters.AddWithValue("@UOM_CODE", d.UOM_CODE);
                        cmd2.Parameters.AddWithValue("@STD_REQ", d.STD_REQ);
                        cmd2.Parameters.AddWithValue("@CUR_STK", d.CUR_STK);
                        cmd2.Parameters.AddWithValue("@AVG_CONS", d.AVG_CONS ?? 0);
                        cmd2.Parameters.AddWithValue("@RESERVE_QTY", d.RESERVE_QTY);
                        cmd2.Parameters.AddWithValue("@OPEN_POQTY", d.OPEN_POQTY);
                        cmd2.Parameters.AddWithValue("@OPEN_RQQTY", d.OPEN_RQQTY);
                        cmd2.Parameters.AddWithValue("@USER_QTY", d.USER_QTY);
                        cmd2.Parameters.AddWithValue("@REQ_QTY", d.REQ_QTY);
                        cmd2.Parameters.AddWithValue("@REQ_REASON", d.REQ_REASON ?? "");
                        cmd2.Parameters.AddWithValue("@REMARKS", d.REMARKS ?? "");
                        cmd2.Parameters.AddWithValue("@PLACE_USE", d.PLACE_USE ?? "");
                        cmd2.Parameters.AddWithValue("@APROX_RATE", d.APROX_RATE);
                        cmd2.Parameters.AddWithValue("@PRIORITY_TYPE", d.PRIORITY_TYPE ?? "");
                        cmd2.Parameters.AddWithValue("@SCRAP_TYPE", d.SCRAP_TYPE ?? "");
                        cmd2.Parameters.AddWithValue("@WORK_TYPE", d.WORK_TYPE ?? "");
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

    }
}