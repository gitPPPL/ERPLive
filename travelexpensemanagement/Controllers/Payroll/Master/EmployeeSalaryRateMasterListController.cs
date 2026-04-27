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
    public class EmployeeSalaryRateMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public EmployeeSalaryRateMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Master/EmployeeSalaryRateMasterList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetAllEmployeeSalaryRates(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var employeeList = new List<PAY_EMPSALARY>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_EMPSALARY", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
        

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                employeeList.Add(new PAY_EMPSALARY
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    EFF_DATE = reader["EFF_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EFF_DATE"]) : DateTime.MinValue,
                                    EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                    M_TYPE = reader["M_TYPE"]?.ToString(),
                                    CODE = reader["CODE"]?.ToString(),
                                    EMP_NAME = reader["EMP_NAME"]?.ToString(),
                                    BASIC = reader["BASIC"] != DBNull.Value ? Convert.ToDecimal(reader["BASIC"]) : 0,
                                    HRA = reader["HRA"] != DBNull.Value ? Convert.ToDecimal(reader["HRA"]) : 0,
                                    SPL_ALLOW = reader["SPL_ALLOW"] != DBNull.Value ? Convert.ToDecimal(reader["SPL_ALLOW"]) : 0,
                                    SPL_ALLOW2 = reader["SPL_ALLOW2"] != DBNull.Value ? Convert.ToDecimal(reader["SPL_ALLOW2"]) : 0,
                                    OTHERS = reader["OTHERS"] != DBNull.Value ? Convert.ToDecimal(reader["OTHERS"]) : 0,
                                    CONVEYANCE = reader["CONVEYANCE"] != DBNull.Value ? Convert.ToDecimal(reader["CONVEYANCE"]) : 0,
                                    UNIFORM = reader["UNIFORM"] != DBNull.Value ? Convert.ToDecimal(reader["UNIFORM"]) : 0,
                                    SECURITY = reader["SECURITY"] != DBNull.Value ? Convert.ToDecimal(reader["SECURITY"]) : 0,
                                    COMMUITY = reader["COMMUITY"] != DBNull.Value ? Convert.ToDecimal(reader["COMMUITY"]) : 0,
                                    INSURANCE = reader["INSURANCE"] != DBNull.Value ? Convert.ToDecimal(reader["INSURANCE"]) : 0,
                                    MOBILE_ALLOW = reader["MOBILE_ALLOW"] != DBNull.Value ? Convert.ToDecimal(reader["MOBILE_ALLOW"]) : 0,
                                    TOT_SALARY = reader["TOT_SALARY"] != DBNull.Value ? Convert.ToDecimal(reader["TOT_SALARY"]) : 0,
                                    GW_SALARY = reader["GW_SALARY"] != DBNull.Value ? Convert.ToDecimal(reader["GW_SALARY"]) : 0,
                                    DUTY = reader["DUTY"] != DBNull.Value ? Convert.ToInt32(reader["DUTY"]) : 0,
                                    GRADE_CODE = reader["GRADE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["GRADE_CODE"]) : 0,
                                    DESG_CODE = reader["DESG_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DESG_CODE"]) : 0,
                                    DESG_NAME = reader["DESG_NAME"]?.ToString(),
                                    DEPT_NAME = reader["DEPT_NAME"]?.ToString(),
                                    IN_TIME = reader["IN_TIME"]?.ToString(),
                                    OUT_TIME = reader["OUT_TIME"]?.ToString(),
                                    FAPROV_STATUS = reader["FAPROV_STATUS"]?.ToString(),
                                    FAPROV_REMARKS = reader["FAPROV_REMARKS"]?.ToString(),
                                    OLD_GW_SALARY = reader["OLD_GW_SALARY"] != DBNull.Value ? Convert.ToDecimal(reader["OLD_GW_SALARY"]) : 0,
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    UPDATE_FLG = reader["UPDATE_FLG"]?.ToString(),
                                    VPF = reader["VPF"] != DBNull.Value ? Convert.ToDecimal(reader["VPF"]) : 0,
                                    SEARCH_CODE = reader["M_TYPE"]?.ToString() + reader["CODE"]?.ToString()
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = Convert.ToInt32(reader["TotalCount"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error loading employee salary rates", message = ex.Message });
            }

            return Json(new { data = employeeList, totalCount });
        }

        [HttpGet]
        public PAY_EMPSALARY GetEmployeeSalaryRateByCode(string code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            PAY_EMPSALARY salary = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_PAY_EMPSALARY", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            salary = new PAY_EMPSALARY
                            {
                                CODE = rdr["CODE"]?.ToString(),
                                EFF_DATE = rdr["EFF_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EFF_DATE"]) : DateTime.MinValue,
                                EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                EMP_NAME = rdr["EMP_NAME"]?.ToString(),
                                BASIC = rdr["BASIC"] != DBNull.Value ? Convert.ToDecimal(rdr["BASIC"]) : 0,
                                HRA = rdr["HRA"] != DBNull.Value ? Convert.ToDecimal(rdr["HRA"]) : 0,
                                SPL_ALLOW = rdr["SPL_ALLOW"] != DBNull.Value ? Convert.ToDecimal(rdr["SPL_ALLOW"]) : 0,
                                SPL_ALLOW2 = rdr["SPL_ALLOW2"] != DBNull.Value ? Convert.ToDecimal(rdr["SPL_ALLOW2"]) : 0,
                                OTHERS = rdr["OTHERS"] != DBNull.Value ? Convert.ToDecimal(rdr["OTHERS"]) : 0,
                                CONVEYANCE = rdr["CONVEYANCE"] != DBNull.Value ? Convert.ToDecimal(rdr["CONVEYANCE"]) : 0,
                                UNIFORM = rdr["UNIFORM"] != DBNull.Value ? Convert.ToDecimal(rdr["UNIFORM"]) : 0,
                                SECURITY = rdr["SECURITY"] != DBNull.Value ? Convert.ToDecimal(rdr["SECURITY"]) : 0,
                                INSURANCE = rdr["INSURANCE"] != DBNull.Value ? Convert.ToDecimal(rdr["INSURANCE"]) : 0,
                                MOBILE_ALLOW = rdr["MOBILE_ALLOW"] != DBNull.Value ? Convert.ToDecimal(rdr["MOBILE_ALLOW"]) : 0,
                                TOT_SALARY = rdr["TOT_SALARY"] != DBNull.Value ? Convert.ToDecimal(rdr["TOT_SALARY"]) : 0,
                                GW_SALARY = rdr["GW_SALARY"] != DBNull.Value ? Convert.ToDecimal(rdr["GW_SALARY"]) : 0,
                                DUTY = rdr["DUTY"] != DBNull.Value ? Convert.ToInt32(rdr["DUTY"]) : 0,
                                GRADE_CODE = rdr["GRADE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["GRADE_CODE"]) : 0,
                                DESG_CODE = rdr["DESG_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DESG_CODE"]) : 0,
                                DESG_NAME = rdr["DESG_NAME"]?.ToString(),
                                DEPT_NAME = rdr["DEPT_NAME"]?.ToString(),
                                IN_TIME = rdr["IN_TIME"]?.ToString(),
                                OUT_TIME = rdr["OUT_TIME"]?.ToString(),
                                VPF = rdr["VPF"] != DBNull.Value ? Convert.ToDecimal(rdr["VPF"]) : 0,
                            };
                        }
                    }
                }
            }

            return salary;
        }
        [HttpPost]
        public JsonResult DeleteEmpSalaryByCode(string code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_EMPSALARY", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode); 
                        cmd.Parameters.AddWithValue("@CODE", code);           

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Employee salary deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Employee salary.", error = ex.Message });
            }
        }


    }
}
 