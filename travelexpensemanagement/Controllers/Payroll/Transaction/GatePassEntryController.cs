using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.Cms;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.SystemInitilization;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class GatePassEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public GatePassEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/GatePassEntry/Index.cshtml");
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
                    string lastV_NO_Query = "select max(V_no) from Pay_GatePass where V_TYPE=@V_TYPE and COMP_CODE= @CompCode and BRANCH_CODE= 1 and YEAR_CODE= @YearCode  ";
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
                string query = "SELECT CODE, NAME FROM DOCTYPE_MAST  WHERE  DOCTYPE ='PayGatePas'";
                var DDlDoctype = _dropdownService.GetDropdownList(query);
                return Json(DDlDoctype);
            }

        }
        [HttpGet]
        public async Task<IActionResult> LoadData(DateTime v_date)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            string query = "";
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    query = @"
                        SELECT  a.V_TYPE, a.V_NO, a.EMP_CODE , a.EMP_NAME, a.SHIFT , a.E_TIME AS OutTime,
                        a.IN_TIME AS InTime, a.HOD_CODE, a.HOD_NAME AS HOD_NAME, a.GP_TYPE AS REASON,
                        b.CODE AS REASON_CODE,  a.GP_NO ,  b.DEDUCT_TYPE, a.remarks   FROM   PAY_INOUT a
                        LEFT JOIN   PAY_REASON_MAST b ON b.NAME = a.GP_TYPE
                        LEFT JOIN  emp_mast c ON c.code = a.emp_code AND c.comp_code = a.comp_code
                        WHERE a.V_TYPE IN('OUT', 'MOVE')  AND b.REASON_TYPE = 'GatePass'  AND a.COMP_CODE = @compCode
                        AND a.BRANCH_CODE = @branchCode AND a.YEAR_CODE = @YearCode AND a.V_DATE = @v_date
                        ORDER BY   a.V_NO;  ";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {

                        cmd.Parameters.AddWithValue("@v_date", v_date);
                        cmd.Parameters.AddWithValue("@compCode", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@branchCode", 1);
                        cmd.Parameters.AddWithValue("@YearCode", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();
                            while (await rdr.ReadAsync())
                            {
                                var result = new
                                {
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                    EMP_NAME = rdr["EMP_NAME"]?.ToString(),
                                    SHIFT = rdr["SHIFT"]?.ToString(),
                                    OutTime = rdr["OutTime"]?.ToString(),
                                    InTime = rdr["InTime"]?.ToString(),
                                    HOD_CODE = rdr["HOD_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["HOD_CODE"]) : 0,
                                    HOD_NAME = rdr["HOD_NAME"]?.ToString(),
                                    REASON = rdr["REASON"]?.ToString(),
                                    REASON_CODE = rdr["REASON_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["REASON_CODE"]) : 0,
                                    GP_NO = rdr["GP_NO"] != DBNull.Value ? Convert.ToInt32(rdr["GP_NO"]) : 0,
                                    DEDUCT_TYPE = rdr["DEDUCT_TYPE"]?.ToString(),
                                    remarks = rdr["remarks"]?.ToString(),


                                };
                                results.Add(result);
                            }

                            if (results.Any())
                            {
                                return Json(new
                                {
                                    success = true,
                                    message = "Data fetched successfully",
                                    data = results
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "No data found"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching data",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        [HttpPost]
        public IActionResult SaveData([FromBody] List<GatePassEntry> data)
        {
            if (data == null || !data.Any())
                return Json(new { success = false, message = "No data received." });

            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                foreach (var entry in data)
                {
                    string deleteQuery = @"
                    DELETE FROM pay_gatepass 
                    WHERE COMP_CODE = @COMP_CODE 
                    AND BRANCH_CODE = @BRANCH_CODE 
                    AND YEAR_CODE = @YEAR_CODE 
                    AND V_NO = @V_NO 
                    AND V_TYPE = @V_TYPE 
                    AND V_DATE = @V_DATE";

                    using (var deleteCmd = new SqlCommand(deleteQuery, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        deleteCmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        deleteCmd.Parameters.AddWithValue("@V_NO", entry.V_NO);
                        deleteCmd.Parameters.AddWithValue("@V_TYPE", entry.V_TYPE);
                        deleteCmd.Parameters.AddWithValue("@V_DATE", entry.V_DATE);
                        deleteCmd.Parameters.AddWithValue("@DOC_ID", entry.DOC_ID);
                        deleteCmd.ExecuteNonQuery();
                    }



                    string getTimeQuery = @"SELECT STATUS FROM PAY_ATTEN where EMP_CODE =@EMP_CODE and V_DATE= @V_DATE and STATUS ='P' AND  BRANCH_CODE   =@BRANCH_CODE AND COMP_CODE=@COMP_CODE
                      ";

                    string? sysTime = null;

                    using (var Cmds = new SqlCommand(getTimeQuery, conn))
                    {
                     
                        Cmds.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        Cmds.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        Cmds.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        Cmds.Parameters.AddWithValue("@V_TYPE", entry.V_TYPE);
                        Cmds.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = entry.V_DATE;
                        Cmds.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE);
               

                        var result = Cmds.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            sysTime = Convert.ToString(result);
                        }
                    }

                    using (var cmd = new SqlCommand("sp_GatePassEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@Action", "save");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@DOC_ID", entry.V_TYPE + entry.V_NO);
                        cmd.Parameters.AddWithValue("@V_NO", entry.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", entry.V_DATE);
                        cmd.Parameters.AddWithValue("@V_TYPE", entry.V_TYPE);
                        cmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE);
                        cmd.Parameters.AddWithValue("@EMP_NAME", entry.EMP_NAME);
                        cmd.Parameters.AddWithValue("@AHRS", entry.AHRS);
                        cmd.Parameters.AddWithValue("@BHRS", entry.BHRS);
                        cmd.Parameters.AddWithValue("@DUTY_TIME", entry.DUTY_TIME);
                        cmd.Parameters.AddWithValue("@IN_TIME", entry.IN_TIME);
                        cmd.Parameters.AddWithValue("@OUT_TIME", entry.OUT_TIME);
                        cmd.Parameters.AddWithValue("@REASON", entry.REASON);
                        cmd.Parameters.AddWithValue("@AUTH_BY", entry.AUTH_BY);
                        cmd.Parameters.AddWithValue("@GP_NO", entry.GP_NO);
                        cmd.Parameters.AddWithValue("@REMARK", entry.REMARK);
                        cmd.Parameters.AddWithValue("@REASON_CODE", entry.REASON_CODE);
                        cmd.Parameters.AddWithValue("@HOD_CODE", entry.HOD_CODE);
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", entry.FAPROV_STATUS);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", entry.FAPROV_REMARKS);
                        cmd.Parameters.AddWithValue("@DUR", entry.DUR);
                              
                        cmd.Parameters.AddWithValue("@SYS_TIME", entry.SYS_TIME ?? sysTime ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@SNO", entry.SNO);
                        cmd.Parameters.AddWithValue("@REF_TYPE", entry.REF_TYPE);
                        cmd.Parameters.AddWithValue("@REF_NO", entry.@REF_NO);
                        cmd.Parameters.AddWithValue("@GATE_NO", entry.GATE_NO);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Gate Pass Entry saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving Gate Pass Entry.", error = ex.Message });
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

        public JsonResult DDLGridReason()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,NAME from PAY_REASON_MAST where REASON_TYPE='GatePass' ";

                var DDLGridReason = _dropdownService.GetDropdownList(query);

                return Json(DDLGridReason);
            }

        }
        public JsonResult DDLGridHOD(int empCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            string compCode = getdata.PubCompCode;
            string query = "";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                if(empCode == 0)
                    {
                    query = $@"
                    SELECT DISTINCT a.EMP_CODE, b.NAME
                    FROM PAYGATE_HOD a left join EMP_MAST b  on a.EMP_CODE = b.CODE 
                    and a.COMP_CODE = b.COMP_CODE  where a.COMP_CODE = {getdata.PubCompCode}   and b.NAME <> ''  ";
                }
            else
                {
                     query = $@"
                        SELECT DISTINCT a.EMP_CODE, b.NAME
                        FROM PAYGATE_HOD a
                        LEFT JOIN emp_mast b ON a.emp_code = b.code AND a.COMP_CODE = b.COMP_CODE
                        WHERE a.DEPT_CODE = (
                        SELECT dept_code
                        FROM emp_mast
                        WHERE code = {empCode} AND comp_code = {getdata.PubCompCode}
                        )
                        AND a.ALLOW = 'Y'
                        AND a.COMP_CODE = {getdata.PubCompCode}
                        AND b.RESIGN_DATE IS NULL
                        ";

                }
                var DDLGridReason = _dropdownService.GetDropdownList(query);

                return Json(DDLGridReason);
            }
        }

    }

}






