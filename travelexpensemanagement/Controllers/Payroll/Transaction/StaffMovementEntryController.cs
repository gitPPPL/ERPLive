using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class StaffMovementEntryController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public StaffMovementEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/StaffMovementEntry/Index.cshtml");
        }


        public JsonResult GetVNo(string vtype)
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
                    string lastV_NO_Query = "select max(V_no) from PAY_INOUT where V_TYPE=@V_TYPE and COMP_CODE= @CompCode and BRANCH_CODE= 1 and YEAR_CODE= @YearCode  ";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vtype);
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

        public JsonResult DDlDoctype()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE, NAME FROM DOCTYPE_MAST  WHERE Code in ('MOVE')";
                var DDlDoctype = _dropdownService.GetDropdownList(query);
                return Json(DDlDoctype);
            }


        }

        public JsonResult DDLHod()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = " select DISTINCT EMP_CODE,EMP_NAME ,CONVERT(VARCHAR,EMP_CODE)+EMP_NAME [CODENM] from PAYGATE_HOD  WHERE  ALLOW='Y' AND COMP_CODE= " + getdata.PubCompCode + " ";

                var DDLHod = _dropdownService.GetDropdownList(query);
                return Json(DDLHod);
            }
        }

        public JsonResult DDLOutType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from PAY_REASON_MAST";

                var DDLOutType = _dropdownService.GetDropdownList(query);
                return Json(DDLOutType);
            }
        }


        public JsonResult DDLEmp()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT em.code,em.name FROM emp_mast EM WHERE ISNULL(EM.RESIGN_DATE,'') = '' and EM.COMP_CODE = " + getdata.PubCompCode + "  order by   em.name asc    ;";
                var DDLEmplist = _dropdownService.GetDropdownList(query);
                return Json(DDLEmplist);
            }
        }

        public JsonResult FetchDataByEmpcode(int empcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var result = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" SELECT DM.NAME AS DEPT_NAME, EM.DEPT_CODE
                FROM emp_mast EM
                LEFT JOIN DEPT_MAST DM ON DM.CODE = EM.DEPT_CODE AND DM.COMP_CODE = EM.COMP_CODE
                WHERE ISNULL(EM.RESIGN_DATE, '') = '' AND EM.COMP_CODE = @CompCode AND EM.CODE = @EmpCode";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@EmpCode", empcode);

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new
                            {
                                DeptName = reader["DEPT_NAME"]?.ToString(),
                                DeptCode = reader["DEPT_CODE"]?.ToString()
                            };
                            result.Add(item);
                        }
                    }
                }
            }

            return Json(result);
        }



        [HttpPost]
        public IActionResult SavedData([FromBody] StaffMovementEntry_Model Header)
        {
            if (Header == null)
                return Json(new { success = false, message = "Input model is null" });

            var action = Header.action?.ToUpper() == "INSERT" ? "INSERT" : "Update";
            var result = SubmitRequest(Header, action);

            return result == "Success"
                ? Json(new { success = true })
                : Json(new { success = false, message = result });
        }

        private string SubmitRequest(StaffMovementEntry_Model Header, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();

                conn.Open();

                using (var cmd = new SqlCommand("sp_StaffMovementEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", Header.action);
                    cmd.Parameters.AddWithValue("@v_NO", Header.V_NO);
                    cmd.Parameters.AddWithValue("@DOC_ID", Header.V_TYPE + Header.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", Header.V_DATE);
                    cmd.Parameters.AddWithValue("@V_TYPE", Header.V_TYPE);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@SHIFT", Header.SHIFT);
                    cmd.Parameters.AddWithValue("@EMP_CODE", Header.EMP_CODE);
                    cmd.Parameters.AddWithValue("@EMP_NAME", Header.EMP_NAME);
                    cmd.Parameters.AddWithValue("@DEPT_CODE", Header.DEPT_CODE);
                    cmd.Parameters.AddWithValue("@DEPT_NAME", Header.DEPT_NAME);
                    cmd.Parameters.AddWithValue("@REMARKS", Header.REMARKS);
                    cmd.Parameters.AddWithValue("@E_DATE", Header.E_DATE);
                    cmd.Parameters.AddWithValue("@E_TIME", Header.E_TIME);
                    cmd.Parameters.AddWithValue("@IN_TIME", Header.IN_TIME);
                    cmd.Parameters.AddWithValue("@GP_NO", Header.GP_NO);
                    cmd.Parameters.AddWithValue("@HOD_CODE", Header.HOD_CODE);
                    cmd.Parameters.AddWithValue("@HOD_NAME", Header.HOD_NAME);
                    cmd.Parameters.AddWithValue("@GP_TYPE", Header.GP_TYPE);
                    cmd.Parameters.AddWithValue("@REASON_CODE", Header.REASON_CODE);
                    cmd.Parameters.AddWithValue("@GP_HRS", 0);
                    cmd.Parameters.AddWithValue("@LATE_HRS", 0);
                    cmd.Parameters.AddWithValue("@SLEEP_HRS", 0);
                    cmd.Parameters.AddWithValue("@WORKPLACE_PLACE", "");
                    cmd.Parameters.AddWithValue("@WORKPLACE_CODE", 0);
                    cmd.Parameters.AddWithValue("@APPROVE", "");
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

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }


    }
}
