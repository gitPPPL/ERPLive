using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.Data;
using System.Text;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Models.Sale;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SaleDeliveryStatusUpdateController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public SaleDeliveryStatusUpdateController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            TempData["LoginDate"] = _globalVariableService.GetGlobalVariables().PubLoginDate;
            return View("~/Views/Sales/Transaction/SaleDeliveryStatusUpdate/Index.cshtml");
        }

        public JsonResult DDlcustomerName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select CODE,NAME from SUBGROUP_MAST where COMP_CODE="+ getdata.PubCompCode +" and NATURE in ('Customer','Broker') order by Name ";
                var DDlcustomerName = _dropdownService.GetDropdownList(query);
                return Json(DDlcustomerName);
            }

        }

        public JsonResult DDlItemtype()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            var DDlItemtype = new List<string>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                string query = "select  DISTINCT MGROUP_TYPE  from ITEM_MGROUP where comp_code=" + getdata.PubCompCode + " order by MGROUP_TYPE";

                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        DDlItemtype.Add(reader["MGROUP_TYPE"].ToString());
                    }
                }
            }
            return Json(DDlItemtype);
        }

        public JsonResult FillData(DateTime VDate, int PARTY_CODE, string MGROUP_TYPE)
        {
            try
            {
                string formattedDate = VDate.ToString("yyyy-MM-dd");
                var getdata = _globalVariableService.GetGlobalVariables();
                var pubBranchCode = getdata.PubBranchCode;
                var pubWorkStation = getdata.PubWorkStationID;
                var pubUserId = getdata.PubUserId;
                var pubCompCode = getdata.PubCompCode;              
                var sqlstr = new StringBuilder(); 
                sqlstr.AppendLine("UPDATE order2 SET adj_qty = 0 WHERE v_type IN ('SORD') AND comp_code = @CompCode AND branch_code = @BranchCode;");
                sqlstr.AppendLine("UPDATE order1 SET order1.status = sauda.status FROM order1, sauda WHERE order1.sauda_no = sauda.v_no AND order1.sauda_type = sauda.v_type");
                sqlstr.AppendLine("AND sauda.comp_code = order1.comp_code AND sauda.branch_code = order1.branch_code AND order1.comp_code = @CompCode");
                sqlstr.AppendLine("AND order1.branch_code = @BranchCode AND order1.v_type = 'SORD' AND sauda.v_type = 'SAUD' AND sauda.status NOT IN (1);");
                sqlstr.AppendLine("UPDATE order2 SET order2.status = order1.status FROM order1, order2 WHERE order1.v_no = order2.v_no AND order1.v_type = order2.v_type");
                sqlstr.AppendLine("AND order2.comp_code = order1.comp_code AND order2.branch_code = order1.branch_code AND order1.comp_code = @CompCode");
                sqlstr.AppendLine("AND order1.branch_code = @BranchCode AND order1.v_type = 'SORD';");
                sqlstr.AppendLine("UPDATE order2 SET adj_qty = (SELECT SUM(qty) FROM sale2 WHERE sale2.ord_type = order2.v_type AND sale2.ord_no = order2.v_no");
                sqlstr.AppendLine("AND sale2.item_code = order2.item_code AND sale2.comp_code = order2.comp_code AND sale2.branch_code = order2.branch_code");
                sqlstr.AppendLine("AND ISNULL(sale2.status,0) <> 2 AND sale2.v_type IN (SELECT code FROM DOCTYPE_MAST WHERE DOCTYPE = 'Salesinvoice'))");
                sqlstr.AppendLine("FROM order2, sale2 WHERE sale2.ord_type = order2.v_type AND sale2.ord_no = order2.v_no AND sale2.item_code = order2.item_code");
                sqlstr.AppendLine("AND sale2.comp_code = order2.comp_code AND sale2.branch_code = order2.branch_code AND ISNULL(sale2.status, 0) <> 2");
                sqlstr.AppendLine("AND order2.comp_code = @CompCode AND order2.branch_code = @BranchCode;");

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(sqlstr.ToString(), con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", pubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", pubBranchCode);
                        cmd.Parameters.AddWithValue("@VDate", formattedDate);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Build the final query to fetch data
                string qry = @"
                    SELECT a.V_TYPE, a.V_No, a.V_Date, d.NAME AS 'PartyName', a.ITEM_CODE, a.SNO, a.ITEM_NAME AS 'ItemName', a.Qty,
                    ISNULL(SUM(c.QTY), 0) AS 'RecQty', (ISNULL(a.QTY, 0) - ISNULL(SUM(c.QTY), 0)) AS 'BalQty',
                    a.delivery_Date AS 'disp_through', '' AS 'disp_ref', a.Remarks AS 'disp_remarks', b.PARTY_CODE
                    FROM ORDER_DELPLAN a
                    LEFT JOIN order1 b ON a.V_NO = b.v_no AND a.V_TYPE = b.V_TYPE AND a.V_DATE = b.V_DATE
                    AND a.COMP_CODE = b.COMP_CODE AND a.BRANCH_CODE = b.BRANCH_CODE AND a.YEAR_CODE = b.YEAR_CODE
                    LEFT JOIN Sale2 c ON a.item_code = c.item_code AND a.v_type = c.ORD_TYPE AND a.v_no = c.ORD_NO
                    AND a.COMP_CODE = c.COMP_CODE
                    LEFT JOIN SUBGROUP_MAST d ON b.PARTY_CODE = d.CODE AND a.COMP_CODE = d.comp_code
                    LEFT JOIN Item_mast e ON a.Item_code = e.code AND a.comp_code = e.comp_code
                    LEFT JOIN Item_Mgroup f ON e.Mgroup_code = f.Code AND e.comp_code = f.comp_code
                    WHERE a.V_TYPE = 'SORD' AND b.status = 1 AND a.V_DATE < @VDate
                    AND a.COMP_CODE = @CompCode AND a.BRANCH_CODE = @BranchCode AND a.YEAR_CODE = @YearCode";

                // Add optional filters
                if (PARTY_CODE >= 0)
                {
                    qry += " AND b.PARTY_CODE = @PartyCode";
                }

                if (!string.IsNullOrEmpty(MGROUP_TYPE))
                {
                    qry += " AND f.MGROUP_TYPE = @MGroupType";
                }

                qry += @"
                GROUP BY a.V_TYPE, a.V_No, a.V_Date, d.NAME, a.ITEM_CODE, a.SNO, a.ITEM_NAME, a.delivery_Date, b.PARTY_CODE, a.Qty, a.Remarks
                HAVING (ISNULL(a.QTY, 0) - ISNULL(SUM(c.QTY), 0)) > 0
                ORDER BY a.V_No, a.SNO ";               
                                               
                var result = new List<object>();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(qry, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", pubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", pubBranchCode);
                        cmd.Parameters.AddWithValue("@VDate", formattedDate);
                        cmd.Parameters.AddWithValue("@YearCode", 8); 
                        cmd.Parameters.AddWithValue("@PartyCode", PARTY_CODE);
                        cmd.Parameters.AddWithValue("@MGroupType", MGROUP_TYPE);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new
                                {
                                  
                                    V_Type = reader["V_Type"] as string ?? string.Empty,
                                    V_No = reader["V_No"],
                                    V_Date = reader["V_Date"],
                                    Name = reader["NAME"] as string ?? string.Empty, 
                                    Item_Code = reader["ITEM_CODE"],
                                    SNo = reader["SNO"],
                                    Item_Name = reader["ITEM_NAME"] as string ?? string.Empty,
                                    Delivery_Date = reader["delivery_Date"],
                                    PARTY_CODE = reader["PARTY_CODE"],
                                    Qty = reader["Qty"],
                                    RecQty = reader["RecQty"],
                                    BalQty = reader["BalQty"],
                                    Remarks = reader["Remarks"] as string ?? string.Empty
                            
                                  
                                };
                                result.Add(item);
                            }
                        }
                    }
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        public IActionResult SaveDispatchDetails([FromBody] List<SaleDeliveryPlanUpdate_Model> models)
        {
            if (models == null || !models.Any())
            {
                return BadRequest(new { success = false, message = "No dispatch delivery data provided." });
            }

            var getdata = _globalVariableService.GetGlobalVariables();

            string query = @"
            UPDATE ORDER_DELPLAN 
            SET Delivery_Date = @Delivery_Date,
            Remarks = @Remarks
            WHERE V_TYPE = 'SORD'
            AND V_NO = @V_NO
            AND ITEM_CODE = @ITEM_CODE
            AND SNO = @SNO
            AND COMP_CODE = @COMP_CODE
            AND BRANCH_CODE = @BRANCH_CODE
            AND YEAR_CODE = @YEAR_CODE";

            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            foreach (var model in models)
                            {
                                using (var cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.CommandType = CommandType.Text;
                                                                       
                                    string formattedDate = model.Delivery_Date.HasValue
                                        ? model.Delivery_Date.Value.ToString("yyyy-MM-dd")  
                                        : null;
                                    cmd.Parameters.AddWithValue("@Delivery_Date", formattedDate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                    cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE);
                                    cmd.Parameters.AddWithValue("@SNO", model.SNO);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return Ok(new { success = true, message = "Dispatch data updated successfully!" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(500, new
                            {
                                success = false,
                                message = "An error occurred while saving data.",
                                error = ex.Message
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database connection error.",
                    error = ex.Message
                });
            }
        }
    }
}
