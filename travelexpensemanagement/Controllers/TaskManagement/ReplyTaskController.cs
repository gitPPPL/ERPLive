
using DocumentFormat.OpenXml.Office.Word;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.TODO;
namespace travelexpensemanagement.Controllers.TodoList

{
    public class ReplyTaskController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public ReplyTaskController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.userCode = _globalVariableService.GetGlobalVariables().PubUserId;
            return View("~/Views/TaskManagement/ReplyTask/Index.cshtml");          
        }

        [HttpGet]
        public async Task<IActionResult> GetTaskDetail(int vno)
        {
            try
            {
                var userSession = _globalVariableService.GetGlobalVariables();
                var parameters = new Dictionary<string, object>
                {
                    { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                    { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                    { "@BRANCH_CODE", userSession.PubBranchCode}, 
                    { "@Order_No",  vno},                     
                    { "@ORDER_TYPE",  "TASK"},                     
                    { "@Action", "SHOWDATA"}            
                };
                var result = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TaskReply]", parameters);
                return Json(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new
                { status = false, message = ex.Message });
            }
        }

        public string GetVNo()
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

                    string lastV_NO_Query = "SELECT MAX(V_NO) FROM CONDATABASE.dbo.INTIMATION_TASK WHERE COMP_CODE = @CompCode AND YEAR_CODE = @YearCode  and BRANCH_CODE = @BRANCH_CODE and V_TYPE = 'TSKL'  and ORDER_TYPE='TASK'  ";
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
                return  "" ;
            }


            return  newV_NO;
        }

        public IActionResult SaveData([FromBody] TaskDetail_Model model)
        {
            if (model == null )
            {
                return BadRequest("No  data provided.");
            }

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                try
                {
                    con.Open();

                    using (var transaction = con.BeginTransaction())
                    {                      
                            SaveDispatchDeliveryData(con, transaction, model);                   

                        transaction.Commit();
                    }

                    return Ok(new { success = true, message = " Data saved successfully!" });
                }
                catch (Exception ex)
                {

                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }
        }

        private void SaveDispatchDeliveryData(SqlConnection connection, SqlTransaction transaction, TaskDetail_Model model )
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using var conn = _dbConnection.GetErpConnection();

            string V_no = GetVNo();

            conn.Open();
            string deletePRequest2Sql = @" DELETE FROM CONDATABASE.dbo.INTIMATION_TASK
                WHERE ORDER_TYPE = @ORDER_TYPE  AND ORDER_NO = @ORDER_NO
                AND V_TYPE = @V_TYPE AND V_NO = @V_NO
                AND comp_code = @comp_code AND Branch_code = @Branch_code
                AND Year_code = @Year_code;";
            using (var deletePRequest2Cmd = conn.CreateCommand())
            {
                deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                deletePRequest2Cmd.Parameters.AddWithValue("@V_TYPE", "TSKL");
                deletePRequest2Cmd.Parameters.AddWithValue("@V_NO", V_no);
                deletePRequest2Cmd.Parameters.AddWithValue("@ORDER_TYPE", "TASK");
                deletePRequest2Cmd.Parameters.AddWithValue("@ORDER_NO", model.V_NO);
                deletePRequest2Cmd.Parameters.AddWithValue("@Branch_code", getdata.PubBranchCode);
                deletePRequest2Cmd.Parameters.AddWithValue("@comp_code", getdata.PubCompCode);
                deletePRequest2Cmd.Parameters.AddWithValue("@Year_code", getdata.PubFYearCode);
                deletePRequest2Cmd.ExecuteNonQuery();
            }
            conn.Close();

            string filePath = "";
            string CCfilePath = "";
            string BCCfilePath = "";
                       
            if (model.RFILE_PATH != "")
            {
                string fileName1 = Path.GetFileName(model.RFILE_PATH);
                string saveFolder = Path.Combine("wwwroot", "images", "Task_File");
                filePath = Path.Combine(saveFolder, fileName1);
            }
          
            if (model.CCFILEPATH != "")
            {
                string fileName2 = Path.GetFileName(model.CCFILEPATH);
                string saveFolder = Path.Combine("wwwroot", "images", "Task_File");
                CCfilePath = Path.Combine(saveFolder, fileName2);
            }
      
            if (model.BCCFILEPATH != "")
            {
                string fileName3 = Path.GetFileName(model.BCCFILEPATH);
                string saveFolder = Path.Combine("wwwroot", "images", "Task_File");
                BCCfilePath = Path.Combine(saveFolder, fileName3);
            }

            using (var cmd = new SqlCommand("sp_TaskReply", connection, transaction))
            { 
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "UPDATE");
                cmd.Parameters.AddWithValue("@STATUS",model.STATUS );
                cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS);
                cmd.Parameters.AddWithValue("@REMARKS3", model.REMARKS3);
                cmd.Parameters.AddWithValue("@REMARKS4", model.REMARKS4);
                cmd.Parameters.AddWithValue("@RFILE_PATH", filePath);       
                cmd.Parameters.AddWithValue("@CCFILEPATH", CCfilePath);       
                cmd.Parameters.AddWithValue("@BCCFILEPATH", BCCfilePath);       
                cmd.Parameters.AddWithValue("@RWSID", getdata.PubWorkStationID);       
                cmd.Parameters.AddWithValue("@RLIP", getdata.PubLocalId);                  
                cmd.Parameters.AddWithValue("@V_TYPE", "TASK");       
                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);       
                cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);       
                cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);                     
                cmd.ExecuteNonQuery();
            }

            using (var cmd1 = new SqlCommand("sp_TaskReply", connection, transaction))
            {
                cmd1.CommandType = CommandType.StoredProcedure;
                cmd1.Parameters.AddWithValue("@Action", "INSERT");
                cmd1.Parameters.AddWithValue("@COMP_CODE",getdata.PubCompCode);
                cmd1.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                cmd1.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                cmd1.Parameters.AddWithValue("@DOC_ID", "TSKL" + V_no);
                cmd1.Parameters.AddWithValue("@V_TYPE", "TSKL");
                cmd1.Parameters.AddWithValue("@V_NO", V_no);
                cmd1.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                cmd1.Parameters.AddWithValue("@ORDER_TYPE", "TASK");
                cmd1.Parameters.AddWithValue("@ORDER_NO", model.ORDER_NO);
                cmd1.Parameters.AddWithValue("@PLACE_CODE", 0);
                cmd1.Parameters.AddWithValue("@FROM_DEPT", model.FROM_DEPT);
                cmd1.Parameters.AddWithValue("@FROM_DEPTNAME", model.FROM_DEPTNAME);
                cmd1.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE);
                cmd1.Parameters.AddWithValue("@DEPT_NAME", model.DEPT_NAME);
                cmd1.Parameters.AddWithValue("@USER_CODE", model.USER_CODE);
                cmd1.Parameters.AddWithValue("@UOM_CODE", model.UOM_CODE);
                cmd1.Parameters.AddWithValue("@DEPT_STATUSDATE", model.DEPT_STATUSDATE);
                cmd1.Parameters.AddWithValue("@QC_STATUSDATE", model.QC_STATUSDATE);
                cmd1.Parameters.AddWithValue("@ITEM_NAME", model.ITEM_NAME);
                cmd1.Parameters.AddWithValue("@UOM_NAME", model.UOM_NAME);
                cmd1.Parameters.AddWithValue("@MAKE_NAME", model.MAKE_NAME);
                cmd1.Parameters.AddWithValue("@REMARKS", model.REMARKS);
                cmd1.Parameters.AddWithValue("@REMARKS2", model.REMARKS2);
                cmd1.Parameters.AddWithValue("@REMARKS3", model.REMARKS3);
                cmd1.Parameters.AddWithValue("@REMARKS4", model.REMARKS4);
                cmd1.Parameters.AddWithValue("@F_PATH", filePath);
                cmd1.Parameters.AddWithValue("@CCFILEPATH", CCfilePath);
                cmd1.Parameters.AddWithValue("@BCCFILEPATH", BCCfilePath);
                cmd1.Parameters.AddWithValue("@UUSER", getdata.PubUserId);
                cmd1.Parameters.AddWithValue("@WSID", getdata.PubWorkStationID);
                cmd1.Parameters.AddWithValue("@AED", "A");
                cmd1.Parameters.AddWithValue("@LIP", getdata.PubLocalId);
                cmd1.Parameters.AddWithValue("@LID", Environment.MachineName) ;
                cmd1.Parameters.AddWithValue("@SNO", 1) ;
                cmd1.ExecuteNonQuery();

            }



        }


        public async Task<JsonResult> GetCode(int  Vno)
        {
            try
            {
                var result = new List<object>();
                var Globalvariable = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    string sql = @"SELECT CC_CODE,  BCC_CODE , ASSIGN_PERSON_CODE , SUPERVISOR_CODE
                    FROM CONDATABASE.dbo.TASK_1
                    WHERE V_NO = @Vno and V_TYPE ='TASK'  and   COMP_CODE = @CompCode
                    AND BRANCH_CODE = @BranchCode
                    AND YEAR_CODE = @YearCode  ";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = Globalvariable.PubCompCode;
                        cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = Globalvariable.PubBranchCode;
                        cmd.Parameters.Add("@YearCode", SqlDbType.Int).Value = Globalvariable.PubFYearCode;
                        cmd.Parameters.Add("@Vno", SqlDbType.Int).Value = Vno;

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            int ccOrdinal = reader.GetOrdinal("CC_CODE");
                            int bccOrdinal = reader.GetOrdinal("BCC_CODE");
                            int ASSIGN_PERSON_CODE = reader.GetOrdinal("ASSIGN_PERSON_CODE");
                            int SUPERVISOR_CODE = reader.GetOrdinal("SUPERVISOR_CODE");

                            while (await reader.ReadAsync())
                            {
                                result.Add(new
                                {
                                    CC_CODE = reader.IsDBNull(ccOrdinal) ? (int?)null : reader.GetInt32(ccOrdinal),
                                    BCC_CODE = reader.IsDBNull(bccOrdinal) ? (int?)null : reader.GetInt32(bccOrdinal),
                                    ASSIGN_PERSON_CODE = reader.IsDBNull(ASSIGN_PERSON_CODE) ? (int?)null : reader.GetInt32(ASSIGN_PERSON_CODE),
                                    SUPERVISOR_CODE = reader.IsDBNull(SUPERVISOR_CODE) ? (int?)null : reader.GetInt32(SUPERVISOR_CODE)
                                });
                            }
                        }
                    }
                }
                return Json(new { status = true, message = "Success", data = result });
            }
            catch (Exception error)
            {
                return Json(new { status = false, message = error.Message });
            }
        }
    }
}

