using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Query;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Org.BouncyCastle.Crypto;
using System.Data;
using System.Diagnostics;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.PayRoll;
using travelexpensemanagement.Models.TaskManagement_Model;


namespace travelexpensemanagement.Controllers.TaskManagement
{
    public class CreateNewTaskController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public CreateNewTaskController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            TempData["loginDate"] = _globalVariableService.GetGlobalVariables().PubLoginDate;

            ViewBag.CompCode = _globalVariableService .GetGlobalVariables().PubCompCode;
            ViewBag.userCode = _globalVariableService .GetGlobalVariables().PubUserId;
            ViewBag.UserName = _globalVariableService .GetGlobalVariables().PubUserName;
            return View("~/Views/TaskManagement/CreateNewTask/Index.cshtml");
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

                    string lastV_NO_Query = "SELECT MAX(V_NO) FROM CONDATABASE.dbo.TASK_1  WHERE COMP_CODE = @CompCode AND YEAR_CODE = @YearCode  and BRANCH_CODE = @BRANCH_CODE and V_TYPE = 'TASK'   ";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

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

        public string GetintiNo(string vType)
        {
            string newV_NO = "";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // 1️⃣ Get Prefix
                    string prefixYR = "0000";
                    string prefixQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                    using (SqlCommand prefixCmd = new SqlCommand(prefixQuery, con))
                    {
                        prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";
                    }

                    // 2️⃣ Get Last Full V_NO
                    string query = @"
                SELECT MAX(V_NO)
                FROM CONDATABASE.dbo.INTIMATION_TASK
                WHERE V_TYPE = @VType
                AND COMP_CODE = @CompCode
                AND BRANCH_CODE = @BranchCode
                AND YEAR_CODE = @YearCode";

                    string lastVno = "";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@VType", vType);

                        lastVno = cmd.ExecuteScalar()?.ToString();
                    }

                    int nextNumber = 1;

                    if (!string.IsNullOrEmpty(lastVno) && lastVno.StartsWith(prefixYR))
                    {
                        // Remove prefix
                        string numericPart = lastVno.Substring(prefixYR.Length);

                        nextNumber = Convert.ToInt32(numericPart) + 1;
                    }

                    // Add prefix ONLY ONCE
                    newV_NO = prefixYR + nextNumber.ToString("D5");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return "ERROR";
            }

            return newV_NO;
        }


        [HttpGet]
        public IActionResult DropdownCompany()
        {

            var getdata = _globalVariableService.GetGlobalVariables();

            string query = "Select code,name from CONDATABASE.dbo.Comp_mast order by code ;";

            var CompList = _dropdownService.GetDropdownList(query);

            return Json(CompList);
        }

        [HttpGet]
        public IActionResult DropdownDepartment()
        {

            var getdata = _globalVariableService.GetGlobalVariables();

            string query = "Select Code , NAME from DEPT_MAST where COMP_CODE = "+ getdata.PubCompCode  + "  and ACTIVE = 1 and name is not null order by  name ;";

            var CompList = _dropdownService.GetDropdownList(query);

            return Json(CompList);
        }

        [HttpGet]
        public IActionResult Dropdown_To(int SelectedComp)
        {

            var getdata = _globalVariableService.GetGlobalVariables();

            string query = " SELECT DISTINCT  a.code,   UPPER(a.Full_name) AS Full_name    " +
            " FROM CONDATABASE.dbo.USER_MAST a where a.Active = 1 and   a.COMP_CODE   = " + SelectedComp + "  " +
            " ORDER BY Full_name;  ";


            var moduleList = _dropdownService.GetDropdownList(query);

            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult Dropdown_CCTo(int SelectedComp)
        {

            var getdata = _globalVariableService.GetGlobalVariables();

            string query = " SELECT DISTINCT  a.code,   UPPER(a.Full_name) AS Full_name    " +
            " FROM CONDATABASE.dbo.USER_MAST a where a.Active = 1 and   a.COMP_CODE   = " + SelectedComp + "  " +
            " ORDER BY Full_name;  ";


            var moduleList = _dropdownService.GetDropdownList(query);

            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult Dropdown_BCCTo(int SelectedComp)
        {

            var getdata = _globalVariableService.GetGlobalVariables();


            string query = " SELECT DISTINCT  a.code,   UPPER(a.Full_name) AS Full_name    " +
            " FROM CONDATABASE.dbo.USER_MAST a where a.Active = 1 and   a.COMP_CODE   = " + SelectedComp + "  " +
            " ORDER BY Full_name;  ";

            var moduleList = _dropdownService.GetDropdownList(query);

            return Json(moduleList);
        }

        public async Task<JsonResult> GetSelectTodata(int UserCode)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                try
                {
                    {
                        con.Open();
                        var parameterlist = new Dictionary<string, object>
                        {
                            {"@Action", "TODATA"},
                            {"@ASSIGN_PERSON_CODE",UserCode },
                            {"@COMP_CODE",globalVariable.PubCompCode }

                        };
                        var Data = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_CreateTask]", parameterlist);

                        return Json(new { Success = true, data = Data });
                    }
                }
                catch (Exception er)
                {
                    return Json(new { success = false, message = er.Message });
                }
            }
        }

        public async Task<JsonResult> GetSelectCCTodata(int UserCode)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                try
                {
                    {
                        con.Open();
                        var parameterlist = new Dictionary<string, object>
                        {
                            {"@Action", "CCTODATA"},
                            {"@CC_CODE",UserCode },
                            {"@COMP_CODE",globalVariable.PubCompCode }

                        };
                        var Data = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_CreateTask]", parameterlist);

                        return Json(new { Success = true, data = Data });
                    }
                }
                catch (Exception er)
                {
                    return Json(new { success = false, message = er.Message });
                }
            }
        }

        public async Task<JsonResult> GetSelectBCCTodata(int UserCode)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                try
                {
                    {
                        con.Open();
                        var parameterlist = new Dictionary<string, object>
                        {
                            {"@Action", "BBCTODATA"},
                            {"@BCC_CODE",UserCode },
                            {"@COMP_CODE",globalVariable.PubCompCode }

                        };
                        var Data = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_CreateTask]", parameterlist);

                        return Json(new { Success = true, data = Data });
                    }
                }
                catch (Exception er)
                {
                    return Json(new { success = false, message = er.Message });
                }
            }
        }

        public JsonResult GetSelectCompName()
        {
              var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"SELECT NAME 
                         FROM CONDATABASE.dbo.COMP_MAST 
                         WHERE CODE = @Code";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Code", getdata.PubCompCode);

                    var name = cmd.ExecuteScalar()?.ToString();

                    return Json(new { NAME = name });
                }
            }
        }

        [HttpPost]
        public IActionResult SaveTempMaster([FromBody] CreateTask_Model data)
        {
            if (data == null)
                return Json(new { success = false, message = "Invalid or empty data." });

            string action = data.action == "INSERT" ? "Insert" : "Update";

            string result = ProcessHoliday(data, action); 

            if (result == "Success")
                return Json(new { success = true, message = "Saved successfully!" });

            return Json(new { success = false, message = result });
        }

        private string ProcessHoliday(CreateTask_Model data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();    
                    
                    string INITV_NO = GetintiNo("TSKL");
                    string intidocid = "TSKL" + INITV_NO;
                    string filePath = "";
                    string deletePRequest2Sql = @" DELETE FROM dbo.IMG_TABLE
                                                WHERE V_TYPE = @V_TYPE
                                                AND V_NO = @V_NO
                                                AND comp_code = @comp_code
                                                AND BRANCH_CODE = @Branch_code
                                                AND YEAR_CODE = @Year_code;";

                    using (var deletePRequest2Cmd = conn.CreateCommand())
                    {
                        deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                        deletePRequest2Cmd.Parameters.AddWithValue("@V_TYPE", "TASK");
                        deletePRequest2Cmd.Parameters.AddWithValue("@V_NO", data.V_NO);                
                        deletePRequest2Cmd.Parameters.AddWithValue("@Branch_code", globalVar.PubBranchCode);
                        deletePRequest2Cmd.Parameters.AddWithValue("@comp_code", globalVar.PubCompCode);
                        deletePRequest2Cmd.Parameters.AddWithValue("@Year_code", globalVar.PubFYearCode);
                        deletePRequest2Cmd.ExecuteNonQuery();
                    }

                    if(data.CC_CODE == int.Parse(globalVar.PubUserId))
                    {
                        string SQL = @" UPDATE CONDATABASE.dbo.TASK_1
                                        SET CC_REMARKS = @CC_REMARKS
                                        WHERE V_TYPE = @V_TYPE
                                        AND V_NO = @V_NO
                                        AND COMP_CODE = @COMP_CODE
                                        AND BRANCH_CODE = @BRANCH_CODE
                                        AND YEAR_CODE = @YEAR_CODE;";
                        using (var deletePRequest2Cmd = conn.CreateCommand())
                        {
                            deletePRequest2Cmd.CommandText = SQL;
                            deletePRequest2Cmd.Parameters.AddWithValue("@CC_REMARKS", data.CC_REMARKS);
                            deletePRequest2Cmd.Parameters.AddWithValue("@V_TYPE", "TASK");
                            deletePRequest2Cmd.Parameters.AddWithValue("@V_NO", data.V_NO);
                            deletePRequest2Cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                            deletePRequest2Cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            deletePRequest2Cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                

                            deletePRequest2Cmd.ExecuteNonQuery();
                        }
                    }

                    if (data.BCC_CODE == int.Parse(globalVar.PubUserId))
                    {

                        string SQL = @" UPDATE CONDATABASE.dbo.TASK_1
                                        SET BCC_REMARKS = @BCC_REMARKS
                                        WHERE V_TYPE = @V_TYPE
                                        AND V_NO = @V_NO
                                        AND COMP_CODE = @COMP_CODE
                                        AND BRANCH_CODE = @BRANCH_CODE
                                        AND YEAR_CODE = @YEAR_CODE;";

                        using (var deletePRequest2Cmd = conn.CreateCommand())
                        {
                            deletePRequest2Cmd.CommandText = SQL;
                            deletePRequest2Cmd.Parameters.AddWithValue("@CC_REMARKS", data.BCC_REMARKS);
                            deletePRequest2Cmd.Parameters.AddWithValue("@V_TYPE", "TASK");
                            deletePRequest2Cmd.Parameters.AddWithValue("@V_NO", data.V_NO);
                            deletePRequest2Cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                            deletePRequest2Cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            deletePRequest2Cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);


                            deletePRequest2Cmd.ExecuteNonQuery();
                        }



                    }

                    if (data.FILE_PATH != "")
                    {
                        string fileName1 = Path.GetFileName(data.FILE_PATH);
                        string saveFolder = Path.Combine("wwwroot", "images", "Task_File");
                        filePath = Path.Combine(saveFolder, fileName1);
                    }

                    if (data.STATUS == "Pending")
                    {

                        using (SqlCommand cmd = new SqlCommand("sp_CreateTask", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@Action", "SAVE_INTIMATION_TASK");
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            cmd.Parameters.AddWithValue("@DOC_ID", intidocid);
                            cmd.Parameters.AddWithValue("@V_TYPE", "TSKL");
                            cmd.Parameters.AddWithValue("@V_NO", INITV_NO);
                            cmd.Parameters.AddWithValue("@V_DATE", data.V_DATE);                        
                            cmd.Parameters.AddWithValue("@ITEM_NAME", data.TASK_SUBJECT);
                            cmd.Parameters.AddWithValue("@ORDER_TYPE", "TASK");                      
                            cmd.Parameters.AddWithValue("@ORDER_NO", data.V_NO);
                            cmd.Parameters.AddWithValue("@REQUEST_NO", 0);
                            cmd.Parameters.AddWithValue("@PLACE_CODE", 0);
                            cmd.Parameters.AddWithValue("@USER_CODE", data.CC_CODE);
                            cmd.Parameters.AddWithValue("@UOM_CODE", data.BCC_CODE);
                            cmd.Parameters.AddWithValue("@FROM_DEPT", data.SUPERVISOR_CODE);
                            cmd.Parameters.AddWithValue("@FROM_DEPTNAME",data.FROM_DEPTNAME);
                            cmd.Parameters.AddWithValue("@DEPT_CODE", data.ASSIGN_PERSON_CODE );
                            cmd.Parameters.AddWithValue("@DEPT_NAME", data.DEPT_NAME);
                            cmd.Parameters.AddWithValue("@DEPT_STATUSDATE", data.START_DATETIME );
                            cmd.Parameters.AddWithValue("@QC_STATUSDATE", data.END_DATETIME );
                            cmd.Parameters.AddWithValue("@UOM_NAME", data.STATUS );
                            cmd.Parameters.AddWithValue("@MAKE_NAME", data.PRIORITY );
                            cmd.Parameters.AddWithValue("@REMARKS", data.REMARKS );
                            cmd.Parameters.AddWithValue("@REMARKS2", data.TASK_DESC );
                            cmd.Parameters.AddWithValue("@REMARKS3", data.CC_REMARKS );
                            cmd.Parameters.AddWithValue("@REMARKS4", data.BCC_REMARKS );
                            cmd.Parameters.AddWithValue("@F_PATH", filePath);                     
                            cmd.Parameters.AddWithValue("@RF_PATH", "" );
                            cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@SNO", 1);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("sp_CreateTask", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "TASK");
                        cmd.Parameters.AddWithValue("@V_NO", data.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", data.V_DATE);
                        cmd.Parameters.AddWithValue("@DOC_ID", "TASK" + data.V_NO);
                        cmd.Parameters.AddWithValue("@REF_NO", data.REF_NO);
                        cmd.Parameters.AddWithValue("@TASK_SUBJECT", data.TASK_SUBJECT);
                        cmd.Parameters.AddWithValue("@ASSIGN_COMPANY", data.ASSIGN_COMPANY);
                        cmd.Parameters.AddWithValue("@ASSIGN_PERSON_CODE", data.ASSIGN_PERSON_CODE);
                        cmd.Parameters.AddWithValue("@SUPERVISOR_CODE", data.SUPERVISOR_CODE);
                        cmd.Parameters.AddWithValue("@CC_COMPANY", data.CC_COMPANY);
                        cmd.Parameters.AddWithValue("@CC_CODE", data.CC_CODE);
                        cmd.Parameters.AddWithValue("@CC_REMARKS", data.CC_REMARKS);
                        cmd.Parameters.AddWithValue("@BCC_COMPANY", data.BCC_COMPANY);
                        cmd.Parameters.AddWithValue("@BCC_CODE", data.BCC_CODE);
                        cmd.Parameters.AddWithValue("@BCC_REMARKS", data.BCC_REMARKS);
                        cmd.Parameters.AddWithValue("@TASK_DESC", data.TASK_DESC);
                        cmd.Parameters.AddWithValue("@START_DATETIME", data.START_DATETIME);
                        cmd.Parameters.AddWithValue("@END_DATETIME", data.END_DATETIME);
                        cmd.Parameters.AddWithValue("@FILE_PATH", filePath);
                        cmd.Parameters.AddWithValue("@RFILE_PATH", filePath);
                        cmd.Parameters.AddWithValue("@PRIORITY", data.PRIORITY);
                        cmd.Parameters.AddWithValue("@STATUS", data.STATUS);
                        cmd.Parameters.AddWithValue("@FREQUENCY", data.FREQUENCY);
                        cmd.Parameters.AddWithValue("@ALERT_FOR", data.ASSIGN_COMPANY);
                      
                         if(data.CC_CODE != 0)
                        {
                            cmd.Parameters.AddWithValue("@ALERTCC_FOR", data.CC_COMPANY);

                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@ALERTCC_FOR", DBNull.Value);
                        }
                        if (data.BCC_CODE != 0)
                        {
                            cmd.Parameters.AddWithValue("@ALERTBCC_FOR", data. BCC_COMPANY);

                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@ALERTBCC_FOR", DBNull.Value);
                        }

                         cmd.Parameters.AddWithValue("@SEND_MAIL", data.SEND_MAIL);
                        cmd.Parameters.AddWithValue("@SEND_SMS", data.SEND_SMS);
                        cmd.Parameters.AddWithValue("@REMARKS", data.REMARKS);
                        cmd.Parameters.AddWithValue("@MOBILE1", data.MOBILE1);
                        cmd.Parameters.AddWithValue("@MOBILE2", data.MOBILE2);
                        cmd.Parameters.AddWithValue("@MOBILE3", data.MOBILE3);
                        cmd.Parameters.AddWithValue("@EMAIL1", data.EMAIL1);
                        cmd.Parameters.AddWithValue("@EMAIL2", data.EMAIL2);                
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);            

                        cmd.ExecuteNonQuery();
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [HttpGet]
        public IActionResult DropdownUser()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            string query = "select code , USER_NAME from CONDATABASE.dbo.USER_MAST where COMP_CODE = " + getdata.PubCompCode + "  and ACTIVE = 1 order by USER_NAME ;";

            var CompList = _dropdownService.GetDropdownList(query);

            return Json(CompList);
        }

    }
}
