using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;  // Use Microsoft.Data.SqlClient instead of System.Data.SqlClient
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class ProductionIncentiveCreationController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public ProductionIncentiveCreationController(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            DropdownService dropdownService,
            DbHelper dbHelper,
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
            return View("~/Views/Payroll/MonthlyTransaction/ProductionIncentiveCreation/Index.cshtml");
        }
         public JsonResult DDLProductionMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                string query = "select code, name  from PROD_INCMAST where comp_code=" + getdata.PubCompCode + " group by code,name order by Code";

                var DDLProductionMaster = _dropdownService.GetDropdownList(query);

                return Json(DDLProductionMaster);
            }
        }
         public JsonResult DDLEmpMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                string query = "select code,  name    from EMP_MAST where comp_code=" + getdata.PubCompCode + " order by name ";

                var DDLEmpMaster = _dropdownService.GetDropdownList(query);

                return Json(DDLEmpMaster);
            }
        }
         public JsonResult DDLDEPT_MAST()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                string query = "select code, name  from DEPT_MAST where comp_code=" + getdata.PubCompCode + "  order by name ";

                var DDLDEPT_MAST = _dropdownService.GetDropdownList(query);

                return Json(DDLDEPT_MAST);
            }
        }
         public JsonResult DDLPlace()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code, name  from PLACE_MAST where comp_code=" + getdata.PubCompCode + " order by Code";

                var Place_Mast = _dropdownService.GetDropdownList(query);

                return Json(Place_Mast);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessMonthlyIncentive([FromBody] JsonElement requestData)
        {
            // Extract parameters from the incoming request data
            var fromDateStr = requestData.GetProperty("fromDate").GetString();
            var toDateStr = requestData.GetProperty("toDate").GetString();
            var deptCode = requestData.GetProperty("Dept").GetString();
            var employees = requestData.GetProperty("employees")
                                       .EnumerateArray()
                                       .Select(e => new
                                       {
                                           Code = e.GetProperty("code").GetInt32(),
                                           Name = e.GetProperty("name").GetString()
                                       })
                                       .ToList();
            var rdSelectedDept = requestData.GetProperty("departmentSelection").GetString() == "selected";
            var employeeSelection = requestData.GetProperty("employeeSelection").GetString() == "selected";
            var placeCode = requestData.GetProperty("place").GetString();

            DateTime fromDate, toDate;

            // Validate From Date
            if (string.IsNullOrWhiteSpace(fromDateStr) || !DateTime.TryParse(fromDateStr, out fromDate))
            {
                return Json(new { success = false, message = "Invalid or missing From Date." });
            }

            // Validate To Date
            if (string.IsNullOrWhiteSpace(toDateStr) || !DateTime.TryParse(toDateStr, out toDate))
            {
                return Json(new { success = false, message = "Invalid or missing To Date." });
            }

            // Validate date range
            if (toDate < fromDate)
            {
                return Json(new { success = false, message = "Invalid Date Range." });
            }

            double incAmt = 0;
            double gross = 0;
            double wages = 0;
            double loomInc = 0;
            int workDay = 0;
            string sqlCondCheckAccount = string.Empty;
            string sqlDeptCode = string.Empty;
            int srNo = 0;

            // Global variables (example, adjust accordingly)
            var getdata = _globalVariableService.GetGlobalVariables();
            string pubCompCode = "2";
            string pubBranchCode = "1";
            string pubFYearCode = "2";
            string pubUserId = getdata.PubUserId;

            // Handling employee codes in the query
            if (employees.Count > 0)
            {
                sqlCondCheckAccount = " AND (";
                foreach (var employee in employees)
                {
                    if (srNo == 0)
                    {
                        sqlCondCheckAccount += $" EMP_MAST.CODE={employee.Code}";
                    }
                    else
                    {
                        sqlCondCheckAccount += $" OR EMP_MAST.CODE={employee.Code}";
                    }
                    srNo++;
                }
                sqlCondCheckAccount += ")";
            }

            // Handling department selection condition
            if (rdSelectedDept)
            {
                sqlDeptCode = $" AND EMP_MAST.DEPT_CODE = {deptCode}";
            }

            DateTime dateTo = fromDate.AddMonths(1).AddDays(-1);
            string vType = "INCA";

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Main query to fetch employee data
                    string sql = @"
            SELECT salary.*, 
            EMP_MAST.name as EmpName ,
            EMP_MAST.DEPT_CODE,
            EMP_MAST.place_code
            FROM Pay_Salary salary
            LEFT JOIN EMP_MAST 
            ON EMP_MAST.code = salary.emp_code 
            AND EMP_MAST.Comp_code = salary.Comp_code
            WHERE salary.comp_code = @CompCode 
            AND salary.BRANCH_CODE = @BranchCode
            AND salary.YEAR_CODE = @YearCode
            AND YEAR(salary.SDATE) = YEAR(@FromDate) 
            AND MONTH(salary.SDATE) = MONTH(@FromDate)
            AND WorkDay > 0" + sqlCondCheckAccount + sqlDeptCode + @"
            ORDER BY EMP_MAST.CODE";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", pubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", pubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", pubFYearCode);
                        cmd.Parameters.AddWithValue("@FromDate", fromDate);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int empCode = reader.GetInt32(reader.GetOrdinal("EMP_CODE"));
                                string empName = reader.GetString(reader.GetOrdinal("EmpName"));

                                // Handle incentive deletion (if exists)
                                string deleteSql = @"DELETE FROM PAY_INCENTIVE_LOOM
                            WHERE EMP_CODE = @EmpCode 
                            AND COMP_CODE = @CompCode 
                            AND BRANCH_CODE = @BranchCode 
                            AND YEAR_CODE = @YearCode 
                            AND V_TYPE = @VType 
                            AND YEAR(V_DATE) = YEAR(@FromDate) 
                            AND MONTH(V_DATE) = MONTH(@FromDate)
                            AND (FINAL IS NULL OR FINAL = 'N')";

                                using (SqlCommand deleteCmd = new SqlCommand(deleteSql, con))
                                {
                                    deleteCmd.Parameters.AddWithValue("@EmpCode", empCode);
                                    deleteCmd.Parameters.AddWithValue("@CompCode", pubCompCode);
                                    deleteCmd.Parameters.AddWithValue("@BranchCode", pubBranchCode);
                                    deleteCmd.Parameters.AddWithValue("@YearCode", pubFYearCode);
                                    deleteCmd.Parameters.AddWithValue("@VType", vType);
                                    deleteCmd.Parameters.AddWithValue("@FromDate", fromDate.ToString("yyyyMM"));
                                    deleteCmd.ExecuteNonQuery();
                                }

                                // Check if incentive already exists
                                string checkIncentiveSql = @"SELECT COUNT(*) FROM PAY_INCENTIVE_LOOM
                            WHERE EMP_CODE = @EmpCode 
                            AND COMP_CODE = @CompCode 
                            AND BRANCH_CODE = @BranchCode 
                            AND YEAR_CODE = @YearCode 
                            AND V_TYPE = @VType 
                            AND FORMAT(V_DATE, 'yyyyMM') = @FromDate 
                            AND FINAL = 'Y'";

                                using (SqlCommand checkCmd = new SqlCommand(checkIncentiveSql, con))
                                {
                                    checkCmd.Parameters.AddWithValue("@EmpCode", empCode);
                                    checkCmd.Parameters.AddWithValue("@CompCode", pubCompCode);
                                    checkCmd.Parameters.AddWithValue("@BranchCode", pubBranchCode);
                                    checkCmd.Parameters.AddWithValue("@YearCode", pubFYearCode);
                                    checkCmd.Parameters.AddWithValue("@VType", vType);
                                    checkCmd.Parameters.AddWithValue("@FromDate", fromDate.ToString("yyyyMM"));

                                    int incentiveCount = (int)checkCmd.ExecuteScalar();

                                    if (incentiveCount > 0)
                                    {
                                        // Incentive already created
                                        continue;
                                    }
                                }

                                // Insert new incentive for the employee
                                string insertSql = @"INSERT INTO PAY_INCENTIVE_LOOM 
                            (CompCode, YearCode, BranchCode, VType, VDate, EmpCode, PlaceCode, LoomType, TotalWages, 
                            TotalLoomInc, WDay, ActualProd, MeshConv, MakeConv, GramConv, ColorConv, TotalProd, 
                            IncRate, IncAmt, Final, UUser, UDate)
                            VALUES 
                            (@CompCode, @YearCode, @BranchCode, @VType, @VDate, @EmpCode, @PlaceCode, @LoomType, 
                            @TotalWages, @TotalLoomInc, @WDay, @ActualProd, @MeshConv, @MakeConv, @GramConv, 
                            @ColorConv, @TotalProd, @IncRate, @IncAmt, @Final, @UUser, @UDate)";

                                using (SqlCommand insertCmd = new SqlCommand(insertSql, con))
                                {
                                    insertCmd.Parameters.AddWithValue("@CompCode", pubCompCode);
                                    insertCmd.Parameters.AddWithValue("@YearCode", pubFYearCode);
                                    insertCmd.Parameters.AddWithValue("@BranchCode", pubBranchCode);
                                    insertCmd.Parameters.AddWithValue("@VType", vType);
                                    insertCmd.Parameters.AddWithValue("@VDate", dateTo);
                                    insertCmd.Parameters.AddWithValue("@EmpCode", empCode);
                                    insertCmd.Parameters.AddWithValue("@PlaceCode", placeCode);
                                    insertCmd.Parameters.AddWithValue("@LoomType", "Loom");
                                    insertCmd.Parameters.AddWithValue("@TotalWages", wages);
                                    insertCmd.Parameters.AddWithValue("@TotalLoomInc", loomInc);
                                    insertCmd.Parameters.AddWithValue("@WDay", workDay);
                                    insertCmd.Parameters.AddWithValue("@ActualProd", 0);
                                    insertCmd.Parameters.AddWithValue("@MeshConv", 0);
                                    insertCmd.Parameters.AddWithValue("@MakeConv", 0);
                                    insertCmd.Parameters.AddWithValue("@GramConv", 0);
                                    insertCmd.Parameters.AddWithValue("@ColorConv", 0);
                                    insertCmd.Parameters.AddWithValue("@TotalProd", 0);
                                    insertCmd.Parameters.AddWithValue("@IncRate", 0);
                                    insertCmd.Parameters.AddWithValue("@IncAmt", incAmt);
                                    insertCmd.Parameters.AddWithValue("@Final", "N");
                                    insertCmd.Parameters.AddWithValue("@UUser", pubUserId);
                                    insertCmd.Parameters.AddWithValue("@UDate", DateTime.Now);

                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    con.Close();
                }

                return Json(new { success = true, message = "Incentive creation successful!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while processing the incentive." });
            }
        }







        public async Task<IActionResult> GetDataCopyForm(int code)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables(); 
            try
            {
        
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

               
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT a.INCE_RATE, b.name AS Place, RANGE AS AvgProd, Rate, 0.0 AS EffRate, b.code AS Placecode " +
                        "FROM PROD_INCMAST a " +
                        "LEFT JOIN PLACE_MAST b ON a.PLACE_CODE = b.code AND a.COMP_CODE = b.COMP_CODE " +
                        "WHERE a.code = @Code AND a.COMP_CODE = @CompCode AND a.BRANCH_CODE = @BranchCode " +
                        "ORDER BY a.INCE_RATE, a.SNO", con))
                    {
                        cmd.CommandType = CommandType.Text;

                    
                        cmd.Parameters.AddWithValue("@Code", code); 
                        cmd.Parameters.AddWithValue("@CompCode", GetGlobalCode.PubCompCode); 
                        cmd.Parameters.AddWithValue("@BranchCode", 1); 

                        
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>(); 

                           
                            while (await rdr.ReadAsync())
                            {
                                var result = new
                                {
                                    INCE_RATE = rdr["INCE_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["INCE_RATE"]) : 0,
                                    Place = rdr["Place"]?.ToString(),
                                    AvgProd = rdr["AvgProd"] != DBNull.Value ? Convert.ToDecimal(rdr["AvgProd"]) : 0,
                                    Rate = rdr["Rate"] != DBNull.Value ? Convert.ToDecimal(rdr["Rate"]) : 0,
                                    EffRate = 0.0, 
                                    Placecode = rdr["Placecode"] != DBNull.Value ? Convert.ToInt32(rdr["Placecode"]) : 0
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

    }
}
