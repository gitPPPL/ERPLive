using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Master;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class EmployeeSalaryRateMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public EmployeeSalaryRateMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Master/EmployeeSalaryRateMaster/Index.cshtml");
        }
        public IActionResult GetDeptList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM DEPT_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE = 1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetEmpList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM EMP_MAST WHERE COMP_CODE = '" + compCode + "' AND ACTIVE = 1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetDesgList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM DESG_MAST WHERE COMP_CODE = '" + compCode + "' AND ACTIVE = 1 ORDER BY NAME ";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetGradeList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM SALARY_LEVEL WHERE COMP_CODE = '" + compCode + "' AND ACTIVE = 1 ORDER BY NAME ";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public JsonResult GetDetailsByCode(string code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = @"
                    SELECT TOP 1 
                    PES.EMP_CODE, 
                    EM.NAME,
                    PES.DEPT_NAME, 
                    PES.DESG_NAME, 
                    PES.GRADE_CODE, 
                    EFF_DATE
                    FROM PAY_EMPSALARY PES 
                    LEFT JOIN EMP_MAST EM on PES.EMP_CODE=EM.CODE
                    WHERE PES.COMP_CODE = @COMP_CODE AND PES.CODE = @CODE";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@CODE", code);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Json(new
                            {
                                empCode = reader["EMP_CODE"]?.ToString(),
                                empName = reader["NAME"]?.ToString(),
                                deptCode = reader["DEPT_NAME"]?.ToString(),
                                desgCode = reader["DESG_NAME"]?.ToString(),
                                gradeCode = reader["GRADE_CODE"]?.ToString(),
                                lastEffDate = Convert.ToDateTime(reader["EFF_DATE"]).ToString("yyyy-MM-dd")
                            });
                        }
                    }
                }
            }

            return Json(null);
        }

        [HttpGet]
        public JsonResult GetDepartmentAndDesignationByUserName(int empCode)
        {
            string deprt = null;
            string desig = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;


            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT DEPT_CODE, DESG_CODE FROM EMP_MAST WHERE COMP_CODE='" + compCode + "' AND CODE = @EmpCode";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmpCode", empCode);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            deprt = reader["DEPT_CODE"]?.ToString();
                            desig = reader["DESG_CODE"]?.ToString();
                        }
                    }
                }
            }

            var result = new
            {
                depT_NAME = deprt,
                desG_NAME = desig
            };

            return Json(result);
        }

        [HttpGet]
        public JsonResult GetSalaryByGrade(int gradeCode)
        {
            string Basic = null;
            string Hra = null;
            string Conv = null;
            string Others = null;
            string Tot_Amt = null;
            string Gw_Amt = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;


            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT BASIC,HRA,CONV,OTHERS,TOT_AMT,GW_AMT FROM SALARY_LEVEL WHERE COMP_CODE='" + compCode + "' AND CODE = @GradeCode";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@GradeCode", gradeCode);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Basic = reader["BASIC"]?.ToString();
                            Hra = reader["HRA"]?.ToString();
                            Conv = reader["CONV"]?.ToString();
                            Others = reader["OTHERS"]?.ToString();
                            Tot_Amt = reader["TOT_AMT"]?.ToString();
                            Gw_Amt = reader["GW_AMT"]?.ToString();
                        }
                    }
                }
            }

            var result = new
            {
                basic = Basic,
                hra = Hra,
                conv = Conv,
                others = Others,
                tot_Amt = Tot_Amt,
                gw_Amt = Gw_Amt,
            };

            return Json(result);
        }

        [HttpPost]
        public IActionResult SaveSalaryRate([FromBody] PAY_EMPSALARY model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            var result = SaveOrUpdateSalaryRate(model, action);

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
        public string SaveOrUpdateSalaryRate(PAY_EMPSALARY model, string action)
        {
            string currentDate = DateTime.Now.ToString("yyyyMMdd");

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_EMPSALARY", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var globalVar = _globalVariableService.GetGlobalVariables();

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", currentDate + model.EMP_CODE);
                        cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", SqlDbType.Int).Value = globalVar.PubCompCode;
                        cmd.Parameters.AddWithValue("@EFF_DATE", model.EFF_DATE);
                        cmd.Parameters.AddWithValue("@EMP_NAME", model.EMP_NAME ?? "");
                        cmd.Parameters.AddWithValue("@DESG_NAME", model.DESG_NAME ?? "");
                        cmd.Parameters.AddWithValue("@DEPT_NAME", model.DEPT_NAME ?? "");
                        cmd.Parameters.AddWithValue("@DESG_CODE", model.DESG_CODE);
                        cmd.Parameters.AddWithValue("@GRADE_CODE", model.GRADE_CODE);
                        cmd.Parameters.AddWithValue("@BASIC", model.BASIC);
                        cmd.Parameters.AddWithValue("@HRA", model.HRA);
                        cmd.Parameters.AddWithValue("@SPL_ALLOW", model.SPL_ALLOW);
                        cmd.Parameters.AddWithValue("@SPL_ALLOW2", model.SPL_ALLOW2);
                        cmd.Parameters.AddWithValue("@OTHERS", model.OTHERS);
                        cmd.Parameters.AddWithValue("@CONVEYANCE", model.CONVEYANCE);
                        cmd.Parameters.AddWithValue("@UNIFORM", model.UNIFORM);
                        cmd.Parameters.AddWithValue("@MOBILE_ALLOW", model.MOBILE_ALLOW);
                        cmd.Parameters.AddWithValue("@TOT_SALARY", model.TOT_SALARY);
                        cmd.Parameters.AddWithValue("@GW_SALARY", model.GW_SALARY);
                        cmd.Parameters.AddWithValue("@DUTY", model.DUTY);
                        cmd.Parameters.AddWithValue("@VPF", model.VPF);
                        cmd.Parameters.AddWithValue("@M_TYPE", "EMSR");

                        cmd.Parameters.AddWithValue("@IN_TIME", model.IN_TIME ?? "");
                        cmd.Parameters.AddWithValue("@OUT_TIME", model.OUT_TIME ?? "");

                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);

                        cmd.Parameters.AddWithValue("@AED", model.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        con.Open();
                        cmd.ExecuteNonQuery();
                        return "Success";
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return $"SQL Error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

    }
}
