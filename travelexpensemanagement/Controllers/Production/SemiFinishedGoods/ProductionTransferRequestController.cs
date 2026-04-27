using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.SemiFinishedGoods.ProductionTransferRequest;

namespace travelexpensemanagement.Controllers.Production.SemiFinishedGoods
{
    public class ProductionTransferRequestController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService; 
        public ProductionTransferRequestController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Production/SemiFinishedGoods/ProductionTransferRequest/Index.cshtml");
        }

        [HttpGet]
        public IActionResult DOCTYPE()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT Code, Name FROM DOCTYPE_MAST WHERE Code = 'IRPD'";
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
        }

        [HttpGet]
        public IActionResult UserDepartmentDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string compCode = getData.PubCompCode;
            string query = "SELECT b.CODE, b.NAME " + "FROM USER_DEPT a " + "LEFT JOIN ITEMDEPT_MAST b ON a.DEPT_CODE = b.CODE AND a.COMP_CODE = b.COMP_CODE " + "WHERE b.Active = 1 " +
                           "AND a.COMP_CODE = " + compCode + " " + "AND b.TRAN_TYPE = 'Production'"; 
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
        }

        [HttpGet]
        public IActionResult PlaceDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string compCode = getData.PubCompCode;
            string query = "SELECT CODE, NAME FROM PLACE_MAST WHERE COMP_CODE = " + compCode; 
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
        }

        [HttpGet]
        public IActionResult ItemMaster()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string compCode = getData.PubCompCode;
            string query = @"SELECT  a.CODE AS value, a.NAME + ' | ' + ISNULL(c.NAME,'') AS text, c.NAME AS unit, c.CODE AS ucode
                            FROM ITEM_MAST a LEFT JOIN ITEMUNIT_MAST c ON a.UNIT_CODE = c.CODE AND c.COMP_CODE = " + compCode + @"
                            WHERE a.COMP_CODE = " + compCode + @" AND a.ACTIVE = 1 GROUP BY a.NAME, a.CODE, c.NAME, c.CODE ORDER BY 
                            a.NAME";
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
        }

        [HttpGet]
        public IActionResult MakeDDL(string itemCode)
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string compCode = getData.PubCompCode;

            string query = $@" SELECT  a.MAKE_CODE AS Mcode, b.Name AS Make FROM ITEM_MAKE a LEFT JOIN ITEMMAKE_MAST b ON a.MAKE_CODE = b.CODE AND b.COMP_CODE = {compCode}
                        WHERE a.ITEM_CODE = '{itemCode}'AND a.COMP_CODE = {compCode}";

            var data = _dropdownService.GetDropdownList(query);

            return Json(new { success = true, data = data });
        }

        [HttpGet]
        public IActionResult PlaceDepartmentDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string compCode = getData.PubCompCode;
            string query = $@"SELECT CODE AS value, NAME AS text FROM ITEMDEPT_MAST WHERE Active = 1 AND TRAN_TYPE = 'Production' AND COMP_CODE = { compCode }";
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
        }

        public IActionResult GetProductionOrderDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();

            string query = @"
                            SELECT 
                                a.V_NO AS VALUE,
                                a.V_TYPE + '-' + CAST(a.V_NO AS VARCHAR) + ' | ' + ISNULL(b.SHORTNAME,'') AS TEXT
                            FROM PROD_ORDER1 a
                            LEFT JOIN ITEM_MAST b 
                                ON a.ITEM_CODE = b.CODE 
                                AND a.COMP_CODE = b.COMP_CODE
                            WHERE a.COMP_CODE = " + getData.PubCompCode + @"
                            AND a.BRANCH_CODE = " + getData.PubBranchCode + @"
                            AND a.YEAR_CODE = " + getData.PubFYearCode;

            var data = _dropdownService.GetDropdownList(query);

            return Json(new { success = true, data = data });
        }

        [HttpGet]
        public IActionResult Status()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
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

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 AS NewVNo FROM TRF_REQUEST1 WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE;";
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
        public IActionResult SaveAndUpdateData([FromBody] ProductionTransferRequest model)
        {
            var globalVariable= _globalVariableService.GetGlobalVariables();
            if (model == null)
            {
                return Json(new { success = false, message = "Model is null (binding failed)" });
            }
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
                        SqlCommand cmd = new SqlCommand("sp_Production_Transfer_Request", con, trans);
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
                        cmd.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.Parameters.AddWithValue("@Action", isUpdate ? "Update" : "InsertHeader");
                        cmd.ExecuteNonQuery();

                        foreach (var Item in model.ItemList)
                        {
                            SqlCommand cmdItem = new SqlCommand("sp_Production_Transfer_Request", con, trans);
                            cmdItem.CommandType = CommandType.StoredProcedure;

                            cmdItem.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmdItem.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmdItem.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmdItem.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmdItem.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            cmdItem.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                            cmdItem.Parameters.AddWithValue("@DOC_ID", docId);
                            cmdItem.Parameters.AddWithValue("@ITEM_CODE", Item.ITEM_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@ITEM_NAME", Item.ITEM_NAME ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@MAKE_CODE", Item.MAKE_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@UOM_NAME", Item.UOM_NAME ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@FROM_DEPT", Item.FROM_DEPT ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@TO_DEPT", Item.TO_DEPT ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@NOS", Item.NOS ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@QTY", Item.QTY ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@IREMARKS", Item.IREMARKS ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@IPORD_TYPE", Item.IPORD_TYPE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@IPORD_NO", Item.IPORD_NO ?? (object)DBNull.Value);

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
                return Json(new {success= false, message=ex.Message});
            }
        }

        [HttpGet]
        public IActionResult loadDataOnEdit(string docId)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new ProductionTransferRequest();
            var item = new List<Item>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_Production_Transfer_Request", con);
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
                        model = new ProductionTransferRequest
                        {
                            DOC_ID = reader["DOC_ID"]?.ToString(),
                            V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                            V_TYPE = reader["V_TYPE"]?.ToString(),
                            V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                            SHIFT = reader["SHIFT"]?.ToString(),
                            SLIP_NO = reader["SLIP_NO"]?.ToString(),
                            PORD_TYPE = reader["PORD_TYPE"]?.ToString(),
                            PORD_NO = reader["PORD_NO"] != DBNull.Value ? Convert.ToInt32(reader["PORD_NO"]) : 0,
                            PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : 0,
                            DEPT_CODE= reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0,
                            REMARKS = reader["REMARKS"]?.ToString(),
                            STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0,

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
                                NOS = reader["NOS"] != DBNull.Value ? Convert.ToInt32(reader["NOS"]) : 0,
                                QTY = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : 0,
                                FROM_DEPT = reader["FROM_DEPT"] != DBNull.Value ? Convert.ToInt32(reader["FROM_DEPT"]) : 0,
                                TO_DEPT = reader["TO_DEPT"] != DBNull.Value ? Convert.ToInt32(reader["TO_DEPT"]) : 0,
                                MAKE_CODE= reader["MAKE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MAKE_CODE"]) : 0,
                                IREMARKS = reader["IREMARKS"]?.ToString(),
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
                return Json(new { success = true, message = ex.ToString() });
            }
        }

        public JsonResult GetPendingOrder(int dept, string date)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT 1 AS NOS, ISNULL(SUM(a.QTY), 0) - ISNULL(SUM(c.QTY), 0) AS QTY, a.V_TYPE, a.V_NO, a.ITEM_CODE, b.NAME, d.Prod_Place
                    FROM PROD_ORDER2 a LEFT JOIN PROD_ORDER1 d ON a.V_TYPE = d.V_TYPE AND a.V_NO = d.V_NO AND a.COMP_CODE = d.COMP_CODE AND a.BRANCH_CODE = d.BRANCH_CODE 
                    AND a.YEAR_CODE = d.YEAR_CODE LEFT JOIN ITEM_MAST b ON b.CODE = a.ITEM_CODE AND b.ACTIVE = 1 AND b.COMP_CODE = a.COMP_CODE  
                    LEFT JOIN ISSUE2 c ON a.V_TYPE = c.PORD_TYPE AND a.V_NO = c.PORD_NO AND a.ITEM_CODE = c.ITEM_CODE AND a.COMP_CODE = c.COMP_CODE AND a.BRANCH_CODE = c.BRANCH_CODE
                    WHERE d.eff_date = @Date AND d.Prod_Place = @Dept AND a.COMP_CODE = 1 AND a.BRANCH_CODE = 1 AND a.YEAR_CODE = 8
                    GROUP BY  a.V_TYPE,a.V_NO, a.ITEM_CODE, b.NAME, d.Prod_Place ORDER BY a.V_TYPE, a.V_NO, b.NAME ", con))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@Dept", dept);
                    cmd.Parameters.AddWithValue("@Date", date); 

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dt);
                }
            }

            var data = dt.AsEnumerable().Select(row => new {
                V_TYPE = row["V_TYPE"]?.ToString(),
                V_NO = row["V_NO"],
                ITEM_CODE = row["ITEM_CODE"],
                NAME = row["NAME"]?.ToString(),
                QTY = row["QTY"],
                Prod_Place = row["Prod_Place"]
            });

            return Json(new { success = true, data = data });
        }
    }

}
