using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Payroll.MonthlyTransaction;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class EmployeeIncrementEntryController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public EmployeeIncrementEntryController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/MonthlyTransaction/EmployeeIncrementEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentList()
        {
            var dataList = await _masterDataService.GetEmployeeDepartMastAsync();
            return Json(dataList);
        }
        [HttpGet]
        public async Task<IActionResult> GetDesignationList()
        {
            var dataList = await _masterDataService.GetDesignationMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetPlaceList()
        {
            var dataList = await _masterDataService.GetPlaceListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetGradeList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"
                select CODE,NAME from GRADE_MAST order by name
                ");
                return Json(new
                {
                    status = true,
                    data = dataList
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });

            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBankList()
        {
            var dataList = await _masterDataService.GetBankMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeAndSalaryDetails(int empCode, string Vdate = null)
        {
            try
            {
                var companyCode = _globalValue.GetGlobalVariables().PubCompCode;                
                var previousDate = GetPreviousDate(Vdate);

                var EmpParameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCode},
                    {"@CODE", empCode},
                    {"@Action", "GetEmployeeData"}
                };
                var salaryParameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCode},
                    {"@CODE", empCode},
                    {"@Action", "GetSalaryData"}
                };
                var attendenceParameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCode},
                    {"@CODE", empCode},
                    {"@PreviousDt", previousDate},
                    {"@V_DATE", Vdate},
                    {"@Action","GetAttendenceData"}
                };
                var loomParameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCode},
                    {"@CODE", empCode},
                    {"@PreviousDt", previousDate},
                    {"@V_DATE", Vdate},
                    {"@Action", "GetLoomData"}
                };

               
                var employeeData = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_EmployeeIncrementEntry]", EmpParameter);
               
                var salaryData = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_EmployeeIncrementEntry]", salaryParameter);
                                
                var attendenceList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_EmployeeIncrementEntry]", attendenceParameter);               
     
                var loomDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_EmployeeIncrementEntry]", loomParameter);

                if (employeeData.Any() && salaryData.Any())
                {
                    return Json(new { status = true, data = new { employee = employeeData[0], salary = salaryData[0], attendence = attendenceList, loomdetail = loomDetailList } });
                }
                else
                {
                    return Json(new { status = false, message = "No data found" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        static string GetPreviousDate(string vdate)
        {
            DateTime date = DateTime.ParseExact(vdate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime previousDate = date.AddMonths(-6);
            return previousDate.ToString("dd-MM-yyyy");
        }

        [HttpGet]
        public async Task<IActionResult> GetEmpIncrementDetailsForUpdate(string id)
        {
            try
            {
                var companyCode = _globalValue.GetGlobalVariables().PubCompCode;               
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCode},
                    {"@V_NO", id},                    
                    {"@Action", "EmpIncrementEntryForUpdate"}
                };


                var employeeData = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_EmployeeIncrementEntry]", parameter);

               
                return Json(new { status = true, data=employeeData});
              
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }


        [HttpPost] // Use POST since you’re saving data
        public async Task<IActionResult> SaveOrUpdateEmpIncrementEntry([FromBody] PayEmployeeIncrement model)
        {
            if (model == null)
                return Json(new 
                {
                    status = false,                   
                    message = "Invalid request: Model is null."
                });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    int VNo = 0;
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd1 = new SqlCommand(
                    "SELECT ISNULL(MAX(V_NO), 0) + 1 FROM PAY_EMPINCREMENT WHERE COMP_CODE = @companyCode", con))
                    {
                        cmd1.CommandType = CommandType.Text;
                        cmd1.Parameters.AddWithValue("@companyCode", usersessionDt.PubCompCode);

                        object result = await cmd1.ExecuteScalarAsync();  // ✅ Get a single value

                        VNo = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 1; // ✅ Safe conversion
                    }

                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_EmployeeIncrementEntry]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        // Determine Action (Add/Edit)
                        cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");                        
                        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);                                   
                        cmd.Parameters.AddWithValue("@CODE", model.Code ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_DATE", model.VDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", VNo);
                        cmd.Parameters.AddWithValue("@M_TYPE", model.MType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TITLE", model.Title ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FIRSTNAME", model.FirstName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MIDDLENAME", model.MiddleName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@LASTNAME", model.LastName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NAME", model.Name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@JOIN_DATE", model.JoinDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@RESIGN_DATE", model.ResignDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PERMANENT_DATE", model.PermanentDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EFF_DATE", model.EffDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DESG_CODE", model.DesgCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", model.DeptCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PlaceCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GRADE_CODE", model.GradeCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PF_APPL", model.PfAppl ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PF_NO", model.PfNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PF_DATE", model.PfDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ESI_APPL", model.EsiAppl ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ESI_NO", model.EsiNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ESI_DATE", model.EsiDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AC_NO", model.AcNo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BANK_CODE", model.BankCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BANK_NAME", model.BankName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IFSC_CODE", model.IfscCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BRANCH", model.Branch ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AC_TYPE", model.AcType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BANK_VERIFY", model.BankVerify ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BASIC", model.Basic);
                        cmd.Parameters.AddWithValue("@HRA", model.Hra);
                        cmd.Parameters.AddWithValue("@SPL_ALLOW", model.SplAllow);
                        cmd.Parameters.AddWithValue("@SPL_ALLOW2", model.SplAllow2);
                        cmd.Parameters.AddWithValue("@OTHERS", model.Others);
                        cmd.Parameters.AddWithValue("@CONVEYANCE", model.Conveyance);
                        cmd.Parameters.AddWithValue("@UNIFORM", model.Uniform);
                        cmd.Parameters.AddWithValue("@SECURITY", model.Security);
                        cmd.Parameters.AddWithValue("@COMMUITY", model.Community);
                        cmd.Parameters.AddWithValue("@INSURANCE", model.Insurance);
                        cmd.Parameters.AddWithValue("@MOBILE_ALLOW", model.MobileAllow);
                        cmd.Parameters.AddWithValue("@TOT_SALARY", model.TotSalary);
                        cmd.Parameters.AddWithValue("@GW_SALARY", model.GwSalary);
                        cmd.Parameters.AddWithValue("@DUTY", model.Duty);
                        cmd.Parameters.AddWithValue("@BASICDATA", model.BasicData ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SALARYDATA", model.SalaryData ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BANKDATA", model.BankData ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", model.FaProvStatus ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", model.FaProvRemarks ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ACTIVE", model.Active);
                        cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                        // Execute the stored procedure
                        await cmd.ExecuteNonQueryAsync();
                    }

                    return Json(new ApiResponse<string>
                    {
                        status = true,                         
                        message = model.SaveOrUpdate == "Save"
                            ? "Employee Increment record saved successfully."
                            : "Employee Increment record updated successfully."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,                   
                    message = "Unexpected error: " + ex.Message
                });
            }
        }


    }
}
