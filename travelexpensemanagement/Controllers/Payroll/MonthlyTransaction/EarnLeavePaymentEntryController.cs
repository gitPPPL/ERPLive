using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Monthly_Transaction;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class EarnLeavePaymentEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public EarnLeavePaymentEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/MonthlyTransaction/EarnLeavePaymentEntry/Index.cshtml");
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
                    string lastV_NO_Query = "select max(V_no) from PAY_LEAVEBAL where V_TYPE='PAY' and COMP_CODE= @CompCode and BRANCH_CODE= 1 and YEAR_CODE= @YearCode  ";
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

        public JsonResult DDlEmp()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE , name FROM EMP_MAST WHERE COMP_CODE = " + getdata.PubCompCode + " AND " +
                    "RESIGN_DATE IS NULL and ACTIVE =1 ORDER BY NAME";
                var DDlEmpCode = _dropdownService.GetDropdownList(query);
                return Json(DDlEmpCode);
            }

        }


        [HttpPost]
        public IActionResult SaveTempMaster([FromBody] EarnLeavePaymentEntry_Model data)
        {
            if (data == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            string action = data.action == "INSERT" ? "Insert" : "Update";

            var result = Submitbtn(data, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }

        [HttpPost]
        private string Submitbtn(EarnLeavePaymentEntry_Model data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_PAY_LEAVEBAL", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@DOC_ID", "PAY" + data.V_NO);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@V_TYPE", "PAY");
                        cmd.Parameters.AddWithValue("@V_NO", data.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", data.V_DATE);
                        cmd.Parameters.AddWithValue("@EMP_CODE", data.EMP_CODE);
                        cmd.Parameters.AddWithValue("@LEAVE_CODE", data.LEAVE_CODE);
                        cmd.Parameters.AddWithValue("@LEAVE_TYPE", data.LEAVE_TYPE);
                        cmd.Parameters.AddWithValue("@OP_DAYS", data.OP_DAYS);
                        cmd.Parameters.AddWithValue("@CUR_DAYS", data.CUR_DAYS);
                        cmd.Parameters.AddWithValue("@PAY_DAYS", data.PAY_DAYS);
                        cmd.Parameters.AddWithValue("@SALARY_DAYS ", data.SALARY_DAYS);
                        cmd.Parameters.AddWithValue("@BAL_DAYS ", data.BAL_DAYS);
                        cmd.Parameters.AddWithValue("@MNTH ", data.MNTH);
                        cmd.Parameters.AddWithValue("@GROSS ", data.GROSS);
                        cmd.Parameters.AddWithValue("@RATE ", data.RATE);
                        cmd.Parameters.AddWithValue("@AMOUNT", data.AMOUNT);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);


                        int rowsInserted = cmd.ExecuteNonQuery();

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {

                return $"Error: {ex.Message}";
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetEmployeeBalance(int empId, DateTime vDate)
        {
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();
                var comp_code = getdata.PubCompCode;
                decimal opBal = 0;
                decimal opbal1 = 0;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    async Task<decimal> GetScalarValueAsync(string query)
                    {
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            var result = await cmd.ExecuteScalarAsync();
                            return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                        }
                    }
                                      
                    string dateStr = vDate.ToString("yyyy-MM-dd");
                                       
                    opBal += await GetScalarValueAsync($@"
                    SELECT SUM(OP_DAYS) 
                    FROM PAY_LEAVEBAL 
                    WHERE EMP_CODE={empId} AND COMP_CODE={comp_code} 
                    AND V_DATE<'{dateStr}' AND V_TYPE='BAL'");

                  
                    opBal -= await GetScalarValueAsync($@"
                    SELECT SUM(PAY_DAYS) 
                    FROM PAY_LEAVEBAL 
                    WHERE EMP_CODE={empId} AND COMP_CODE={comp_code} 
                    AND V_DATE<'{dateStr}' AND V_TYPE='PAY'");

                    // 3️⃣ Subtract EL_DAY from PAY_SALARY
                    opBal -= await GetScalarValueAsync($@"
                    SELECT SUM(EL_DAY) 
                    FROM PAY_SALARY 
                    WHERE EMP_CODE={empId} AND COMP_CODE={comp_code} 
                    AND SDATE<'{dateStr}'");

                    opbal1 = await GetScalarValueAsync($@"
                    SELECT SUM(WORKDAY) - SUM(EL_DAY) - SUM(CL_DAY)
                    FROM PAY_SALARY 
                    WHERE EMP_CODE={empId} AND COMP_CODE={comp_code} 
                    AND SDATE<='{dateStr}'");

                    if (opbal1 > 0)
                        opbal1 = opbal1 / 20.0m;
                    else
                        opbal1 = 0;

               
                    opBal += opbal1;

                   
                    decimal gross = await GetScalarValueAsync($@"
                    SELECT TOP 1 BASIC  
                    FROM PAY_EMPSALARY 
                    WHERE EMP_CODE={empId} AND COMP_CODE={comp_code} 
                    ORDER BY EFF_DATE DESC");

                   
                    var result = new
                    {
                        Baldays = Math.Round(opBal),
                        Gross = gross
                    };

                    return Json(result);
                }
            }
            catch (Exception ex)
            {
              
                return Json(new { error = ex.Message });
            }
        }


    }
}
