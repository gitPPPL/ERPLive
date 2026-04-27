using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.Cms;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.SystemInitilization;
using travelexpensemanagement.Models.Payroll.MonthyTransaction;

namespace travelexpensemanagement.Controllers.Payroll.MonthyTransaction
{
    public class SalaryUnpaidEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public SalaryUnpaidEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/MonthlyTransaction/SalaryUnpaidEntry/Index.cshtml");
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
                    string lastV_NO_Query = "select max(V_no) from PAY_MESS where V_TYPE='UNPD' and COMP_CODE= @CompCode and BRANCH_CODE= 1 and YEAR_CODE= @YearCode  ";
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
        public JsonResult DDLGridEmpValidation(int EMPCODE, DateTime v_dATE)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                SELECT 1 FROM pay_salary 
                WHERE emp_code = @EmpCode 
                AND (ISNULL(HOLD, '') = '' OR HOLD = 'HOLD') 
                AND pay_date IS NULL 
                AND sdate >= @ThresholdDate
                AND comp_code = @CompCode
                AND branch_code = @BranchCode  and YEAR_CODE =@YEAR_CODE";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@EmpCode", EMPCODE);
                cmd.Parameters.AddWithValue("@ThresholdDate", v_dATE);
                cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                cmd.Parameters.AddWithValue("@BranchCode", 1);
                cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"No unpaid salary found for Emp-Code: {EMPCODE}"
                    });
                }

                return Json(new
                {
                    success = false,
                    message = $""
                });
            }
        }

        public JsonResult DDlDoctype()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('PayUnpaid') order by Name";
                var DDlDoctype = _dropdownService.GetDropdownList(query);
                return Json(DDlDoctype);
            }

        }

        public JsonResult DDLGridEmp()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code , Name from EMP_MAST where isnull(NAME, '')<> '' and  COMP_CODE =" + getdata.PubCompCode + " and ACTIVE = 1  order by  name asc  ";

                var DDLGridEmp = _dropdownService.GetDropdownList(query);

                return Json(DDLGridEmp);
            }

        }


        [HttpPost]
        public IActionResult SaveData([FromBody] List<MonthyTransaction_Model> data)
        {
            if (data == null || !data.Any())
                return Json(new { success = false, message = "No data received." });

            var g = _globalVariableService.GetGlobalVariables();

            DateTime currentDate = DateTime.Now;
            string loginDateStr = g.PubLoginDate.ToString("dd/mm/yyyy");
            var firstEntry = data.First();
      
            if (DateTime.TryParse(loginDateStr, out DateTime loginDate))
            {
                if (loginDate.Date != currentDate.Date)
                {
                    return Json(new { success = false, message = "Date cannot be greater than login date." });
                }
            }

            try
            {
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                         
                string yearCheckQuery = @"
                SELECT 1 
                FROM YEAR_MAST 
                WHERE @V_DATE BETWEEN START_DATE AND END_DATE 
                AND CODE = @Code";
 
                using (var checkCmd = new SqlCommand(yearCheckQuery, conn))
                {
                    string vdate = firstEntry.V_DATE?.ToString("yyyy-MM-dd") ?? "";
                    checkCmd.Parameters.AddWithValue("@V_DATE", vdate);

                    checkCmd.Parameters.AddWithValue("@Code", g.PubFYearCode);

                    var result = checkCmd.ExecuteScalar();
                    if (result == null)
                    {
                        return Json(new { success = false, message = "Transaction date is not within the financial year range." });
                    }
                }

                string deleteQuery = @"
                DELETE FROM PAY_MESS 
                WHERE COMP_CODE = @COMP_CODE 
                AND BRANCH_CODE = @BRANCH_CODE 
                AND YEAR_CODE = @YEAR_CODE 
                AND V_NO = @V_NO 
                AND V_TYPE = @V_TYPE";

                using (var deleteCmd = new SqlCommand(deleteQuery, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    deleteCmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    deleteCmd.Parameters.AddWithValue("@V_NO", firstEntry.V_NO);
                    deleteCmd.Parameters.AddWithValue("@V_TYPE", firstEntry.V_TYPE);

                    deleteCmd.ExecuteNonQuery();
                }


                foreach (var entry in data)
                {
                    using (var cmd = new SqlCommand("sp_SalaryUnpaidENTRY", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@Action", "save");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", entry.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", entry.V_DATE);
                        cmd.Parameters.AddWithValue("@V_TYPE", entry.V_TYPE);
                        cmd.Parameters.AddWithValue("@DOC_ID", entry.V_TYPE + entry.V_NO);
                        cmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE);
                        cmd.Parameters.AddWithValue("@AMOUNT", entry.AMOUNT);
                        cmd.Parameters.AddWithValue("@REMARK", entry.REMARK ?? string.Empty);
                        cmd.Parameters.AddWithValue("@SNO", entry.SNO);
                        cmd.Parameters.AddWithValue("@RELEASE_DATE", entry.RELEASE_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error saving data.",
                    error = ex.Message
                });
            }
        }

    }
}
