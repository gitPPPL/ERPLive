using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Loom_Process.LoomBreakDownEntry;

namespace travelexpensemanagement.Controllers.Production.LoomProcess
{
    public class LoomBreakDownEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public LoomBreakDownEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Production/LoomProcess/LoomBreakDownEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult DepartmentDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"select code,name from [itemdept_mast]  where  tran_type='Production' and comp_code=" + getData.PubCompCode + "ORDER BY NAME";
            var department = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = department });
        }

        [HttpGet]
        public IActionResult RepairTypeDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"Select code,name from [FALT_MAST]  where comp_code=" + getData.PubCompCode + "ORDER BY NAME";
            var repair = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = repair });
        }

        [HttpGet]
        public IActionResult FaultDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"select code,name from [PMBREAKDOWN_MAST]  where comp_code=" + getData.PubCompCode + "ORDER BY NAME ";
            var repair = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = repair });
        }

        [HttpGet]
        public IActionResult EmployeeDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @" SELECT CODE AS Value, LTRIM(RTRIM(NAME)) + SPACE(30 - LEN(LTRIM(RTRIM(NAME)))) + ' | ' + CAST(CODE AS VARCHAR) AS Text FROM EMP_MAST 
                              WHERE RESIGN_DATE IS NULL AND COMP_CODE = " + getData.PubCompCode + @" AND ACTIVE = 1 ORDER BY NAME";
            var repair = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = repair });
        }

        [HttpGet]
        public IActionResult PlaceDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"select code,name from PLACE_MAST where comp_code=" + getData.PubCompCode + "ORDER BY Code";
            var repair = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = repair });
        }

        [HttpGet]
        public IActionResult block()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT DISTINCT BLOCK AS Value,BLOCK AS Text FROM Machine_Mast WHERE Comp_code = 2 AND Type = 'Loom' AND BLOCK IS NOT NULL ORDER BY BLOCK";
            var block = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = block });
        }

        public JsonResult GenerateVNo()
        {
            string newV_NO = "00001";
            string vType = "BKDN";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Year Prefix
                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 FROM BREAK_DOWN_LOOM WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                    SqlCommand cmd = new SqlCommand(lastV_NO_Query, con);

                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                    object result = cmd.ExecuteScalar();

                    int nextNo = Convert.ToInt32(result);

                    newV_NO = prefixYR + nextNo.ToString("D5");
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error generating V_NO: " + ex.Message });
            }

            return Json(new { v_NO = newV_NO, v_TYPE = vType });
        }

        [HttpPost]
        public IActionResult SaveAndUpdateData([FromBody] LoomBreakDownEntry model)
        {
            var globalVariable= _globalVariableService.GetGlobalVariables();
            string vType = "BKDN";
            
            if (model == null)
            {
                return Json(new { success = false, message = "Model is null (binding failed)" });
            }

            string docId = vType + model.V_NO;
            string action = string.IsNullOrEmpty(model.DOC_ID) ? "Insert" : "Update";
            try
            {
                using(SqlConnection con= _dbConnection.GetErpConnection())
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_Break_Down_Loom", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@STOP_DATE", model.STOP_DATE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@STOP_TIME", model.STOP_TIME ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@LOOM_CODE", model.LOOM_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BD_CODE", model.BD_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FAULT_CODE", model.FAULT_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ST_DATE", model.ST_DATE ?? (object)DBNull.Value); 
                    cmd.Parameters.AddWithValue("@ST_TIME", model.ST_TIME ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@HRS", model.HRS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@MINT", model.MINT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CONV_MINT", model.HRS * 60 + model.MINT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CONV_HRS", model.HRS + (model.MINT / 60.0) ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.ExecuteNonQuery();

                }
                return Json(new { success = true, message = action == "Insert" ? "Data Saved Successfully!!" : "Data Updated Successfully!!" });

            }
            catch(Exception ex)
            {
                return Json(new {success= false, message= ex.Message});
            }
        }

        [HttpGet]
        public IActionResult loadDataOnEdit(string docId)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new LoomBreakDownEntry();

            try
            {
                 using(SqlConnection con= _dbConnection.GetErpConnection())
                 {
                    SqlCommand cmd = new SqlCommand("sp_Break_Down_Loom",con);
                    cmd.CommandType= CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@Action", "Edit");
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new LoomBreakDownEntry
                            {
                                DOC_ID = reader["DOC_ID"]?.ToString(),
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                SHIFT = reader["SHIFT"]?.ToString(),
                                STOP_DATE = reader["STOP_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["STOP_DATE"]) : (DateTime?)null,
                                STOP_TIME= reader["STOP_TIME"]?.ToString(),
                                LOOM_CODE = reader["LOOM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LOOM_CODE"]): null,
                                BD_CODE = reader["BD_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BD_CODE"]) : null,
                                FAULT_CODE = reader["FAULT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["FAULT_CODE"]) : null,
                                ST_DATE = reader["ST_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["ST_DATE"]) : (DateTime?)null,
                                ST_TIME = reader["STOP_TIME"]?.ToString(),
                                HRS = reader["HRS"] != DBNull.Value ? Convert.ToInt32(reader["HRS"]) : null,
                                MINT = reader["MINT"] != DBNull.Value ? Convert.ToInt32(reader["MINT"]) : null,
                                REMARKS = reader["REMARKS"]?.ToString(),
                            };
                        }
                    }
                 }
                return Json(new { success = true, data = model });
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
