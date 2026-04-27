using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.SemiFinishedGoods.ReceiptAfterProductionEntry;

namespace travelexpensemanagement.Controllers.Production.SemiFinishedGoods
{
    public class ReceiptAfterProductionEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ReceiptAfterProductionEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/Production/SemiFinishedGoods/ReceiptAfterProductionEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult Status()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
            var status = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = status });
        }

        [HttpGet]
        public IActionResult DONO()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT a.BILL_CODE, a.BILL_NAME, a.V_TYPE, a.V_NO FROM DO1 a LEFT JOIN City_mast b ON a.SHIP_CITY = b.CODE WHERE a.COMP_CODE = 1 AND a.BRANCH_CODE = 1 AND NOT EXISTS (SELECT 1 FROM PRODUCTION1 p WHERE p.PORD_TYPE = a.V_TYPE AND p.PORD_NO = a.V_NO AND p.COMP_CODE = 1  AND p.BRANCH_CODE = 1);";
            var doNo = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = doNo });
        }

        [HttpGet]
        public IActionResult ProdOrderNo()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT  V_NO, V_TYPE + CAST(V_NO AS VARCHAR) AS Refid, V_TYPE FROM PROD_ORDER1 WHERE V_Type IN (SELECT CODE FROM DOCTYPE_MAST WHERE DOCTYPE = 'ProductionOrder' AND ACTIVE = 1) AND COMP_CODE = 1 AND YEAR_CODE = 8 AND BRANCH_CODE = 1";
            var prodNo = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = prodNo });
        }

        [HttpGet]
        public IActionResult DOCTYPE()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT Code, Name FROM DOCTYPE_MAST WHERE CODE IN ('PRDR');";
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
        }

        [HttpGet]
        public IActionResult ItemMaster()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT a.CODE AS Value, (a.NAME + '|' + ISNULL(b.NAME,'')) AS Text FROM ITEM_MAST a LEFT JOIN ITEMUNIT_MAST b ON a.UNIT_CODE = b.CODE AND b.COMP_CODE = 1 INNER JOIN ITEM_MGROUP c ON a.MGROUP_CODE = c.CODE AND c.COMP_CODE = 1 WHERE a.COMP_CODE = 1 AND a.ACTIVE = 1 ORDER BY a.NAME";
            var item = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = item});
        }

        [HttpGet]
        public IActionResult DepartmentDDl()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select code ,name from ITEMDEPT_MAST where Active=1 and COMP_CODE=1 and TRAN_TYPE='Store' order by name";
            var department = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = department });
        }

        public JsonResult GenerateVNo(string vType)
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

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 FROM ISSUE1 WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);

                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    lastVnoCmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

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
        public IActionResult SaveData([FromBody] ReceiptAfterProductionEntry model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Model is null (binding failed)" });
            }
            var globalVariable = _globalVariableService.GetGlobalVariables();

            try
            {
                bool isUpdate = !string.IsNullOrEmpty(model.DOC_ID);
                string docId = model.DOC_ID;

                if (!isUpdate)
                {
                    docId = model.V_TYPE + model.V_NO;
                }

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();

                    try
                    {
                        SqlCommand cmd = new SqlCommand("sp_RecieptAfterProduction_Entry", con, trans);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SLIP_NO", model.SLIP_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORD_TYPE", model.PORD_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORD_NO", model.PORD_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLAN_TYPE", model.PLAN_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLAN_NO", model.PLAN_NO ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.Parameters.AddWithValue("@Action", isUpdate ? "Update" : "InsertHeader");

                        cmd.ExecuteNonQuery();

                        foreach (var item in model.ItemList)
                        {
                            SqlCommand cmdItem = new SqlCommand("sp_RecieptAfterProduction_Entry", con,trans);
                            cmdItem.CommandType = CommandType.StoredProcedure;

                            cmdItem.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmdItem.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmdItem.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmdItem.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmdItem.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            cmdItem.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                            cmdItem.Parameters.AddWithValue("@DOC_ID", docId);
                            cmdItem.Parameters.AddWithValue("@ITEM_CODE", item.ITEM_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@ITEM_NAME", item.ITEM_NAME ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@UOM_NAME", item.UOM_NAME ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@LOT_NO", item.LOT_NO ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@NOS", item.NOS ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@QTY", item.QTY ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@FROM_DEPT", item.FROM_DEPT ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@IREMARKS", item.IREMARKS ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@RATE", item.RATE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@IAMOUNT", item.IAMOUNT ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@LAND_RATE", item.LAND_RATE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@LAND_AMT", item.LAND_AMT ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@IPORD_TYPE", item.IPORD_TYPE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@IPORD_NO", item.IPORD_NO ?? (object)DBNull.Value);

                            cmdItem.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmdItem.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                            cmdItem.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmdItem.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmdItem.Parameters.AddWithValue("@LID", Environment.MachineName);

                            cmdItem.Parameters.AddWithValue("@Action", "InsertFooter");

                            cmdItem.ExecuteNonQuery();

                        }
                        trans.Commit();
                        string message = isUpdate ? "Record Updated Successfully" : "Record Inserted Successfully";
                        return Json(new { success = true, message = message, isUpdate = isUpdate });
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
    
            } catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        [HttpGet]
        public IActionResult loadDataOnEdit(string docId)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new ReceiptAfterProductionEntry();
            var item = new List<Item>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_RecieptAfterProduction_Entry", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@Action", "Edit");

                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        model = new ReceiptAfterProductionEntry
                        {
                            DOC_ID = reader["DOC_ID"]?.ToString(),
                            V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                            V_TYPE = reader["V_TYPE"]?.ToString(),
                            V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                            SHIFT = reader["SHIFT"]?.ToString(),
                            SLIP_NO = reader["SLIP_NO"]?.ToString(),
                            PORD_TYPE = reader["PORD_TYPE"]?.ToString(),
                            PORD_NO = reader["PORD_NO"] != DBNull.Value ? Convert.ToInt32(reader["PORD_NO"]) : 0,
                            REMARKS = reader["REMARKS"]?.ToString(),
                            STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0,
                            PLAN_TYPE = reader["PLAN_TYPE"]?.ToString(),
                            PLAN_NO = reader["PLAN_NO"] != DBNull.Value ? Convert.ToInt32(reader["PLAN_NO"]) : 0
                        };
                    };
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            item.Add(new Item
                            {
                                ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                                ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                UOM_NAME = reader["UOM_NAME"]?.ToString(),
                                LOT_NO = reader["LOT_NO"]?.ToString(),
                                NOS = reader["NOS"] != DBNull.Value ? Convert.ToInt32(reader["NOS"]) : 0,
                                QTY = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : 0,
                                FROM_DEPT= reader["FROM_DEPT"] != DBNull.Value ? Convert.ToInt32(reader["FROM_DEPT"]) : 0,
                                IREMARKS = reader["IREMARKS"]?.ToString(),
                                RATE = reader["RATE"] != DBNull.Value ? Convert.ToDecimal(reader["RATE"]) : 0,
                                IAMOUNT = reader["IAMOUNT"] != DBNull.Value ? Convert.ToDecimal(reader["IAMOUNT"]) : 0,
                                LAND_RATE = reader["LAND_RATE"] != DBNull.Value ? Convert.ToDecimal(reader["LAND_RATE"]) : 0,
                                LAND_AMT = reader["LAND_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["LAND_AMT"]) : 0,
                                IPORD_TYPE = reader["IPORD_TYPE"]?.ToString(),
                                IPORD_NO = reader["IPORD_NO"] != DBNull.Value ? Convert.ToInt32(reader["IPORD_NO"]) : 0
                            });
                        };
                    };
                }
                return Json(new { success = true, header = model, items = item });
            }
            catch (Exception ex)
            {
                return Json(new { success = true, message = ex.Message });
            }
        }

    }
}
