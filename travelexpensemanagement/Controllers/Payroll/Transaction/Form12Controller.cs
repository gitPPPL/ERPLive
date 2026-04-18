using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Cms;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;


namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class Form12Controller : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public Form12Controller(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/Form12/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetLoadData(DateTime vDATE)
        {
            try
            {
                var globalVars = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                        SELECT   
                        CODE, DEPARTMENT, EMP_CODE, EMP_NAME, FATHER_NAME, DESIGNATION,
                        D1, D2, D3, D4, D5, D6, D7, D8, D9, D10,
                        D11, D12, D13, D14, D15, D16, D17, D18, D19, D20,
                        D21, D22, D23, D24, D25, D26, D27, D28, D29, D30, D31,
                        W_DAY, E_SHIFT, V_DATE
                        FROM PAY_FORM12
                        WHERE 
                        MONTH(V_DATE) = MONTH(@vDATE) AND 
                        YEAR(V_DATE) = YEAR(@vDATE) AND 
                        COMP_CODE = @compCode AND 
                        BRANCH_CODE = @branchCode AND 
                        YEAR_CODE = @yearCode
                      
                        ORDER BY EMP_CODE;
                        ";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@compCode", globalVars.PubCompCode);
                        cmd.Parameters.AddWithValue("@branchCode", 1);
                        cmd.Parameters.AddWithValue("@yearCode", globalVars.PubFYearCode);
                        cmd.Parameters.AddWithValue("@vDATE", vDATE);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();

                            while (await reader.ReadAsync())
                            {
                                // Dynamically read D1 to D31
                                var days = new Dictionary<string, string>();
                                for (int i = 1; i <= 31; i++)
                                {
                                    string columnName = $"D{i}";
                                    days[columnName] = reader[columnName]?.ToString();
                                }

                                var item = new
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToDecimal(reader["CODE"]) : 0,
                                    DEPARTMENT = reader["DEPARTMENT"]?.ToString(),
                                    EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToDecimal(reader["EMP_CODE"]) : 0,
                                    EMP_NAME = reader["EMP_NAME"]?.ToString(),
                                    FATHER_NAME = reader["FATHER_NAME"]?.ToString(),
                                    DESIGNATION = reader["DESIGNATION"]?.ToString(),
                                    W_DAY = reader["W_DAY"]?.ToString(),
                                    E_SHIFT = reader["E_SHIFT"]?.ToString(),
                                    V_DATE = reader["V_DATE"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd")
                                        : null,
                                    DAYS = days // D1 to D31
                                };

                                results.Add(item);
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

        [HttpGet]
        public async Task<IActionResult> GetEmployeeAttendance(DateTime vDate, int skipEmpty = 0)
        {
            try
            {
                var globalVars = _globalVariableService.GetGlobalVariables();
                int month = vDate.Month;
                int year = vDate.Year;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // 1. Check if attendance exists
                    string existQuery = @"
                    SELECT 1 
                    FROM PAY_ATTEN 
                    WHERE v_type = 'ATTN' 
                    AND MONTH(v_date) = @Month 
                    AND YEAR(v_date) = @Year 
                    AND COMP_CODE = @CompCode 
                    AND BRANCH_CODE = @BranchCode 
                    AND YEAR_CODE = @YearCode";

                    using (SqlCommand existCmd = new SqlCommand(existQuery, con))
                    {
                        existCmd.Parameters.AddWithValue("@Month", month);
                        existCmd.Parameters.AddWithValue("@Year", year);
                        existCmd.Parameters.AddWithValue("@CompCode", globalVars.PubCompCode);
                        existCmd.Parameters.AddWithValue("@BranchCode", 1);
                        existCmd.Parameters.AddWithValue("@YearCode", globalVars.PubFYearCode);

                        var exists = await existCmd.ExecuteScalarAsync();
                        if (exists == null)
                        {
                            return Json(new { success = false, message = "Record not found for this month." });
                        }
                    }

                    // 2. Load employee master list
                    string empQuery = @"
                        SELECT  
                        EMP_MAST.code AS EMP_CODE,
                        EMP_MAST.name AS EMP_NAME,
                        EMP_MAST.SHIFT AS E_SHIFT,
                        DESG_MAST.name AS DESIGNATION,
                        DEPT_MAST.name AS DEPARTMENT
                        FROM EMP_MAST
                        LEFT JOIN DEPT_MAST ON DEPT_MAST.code = EMP_MAST.dept_code AND EMP_MAST.comp_code = DEPT_MAST.comp_code
                        LEFT JOIN DESG_MAST ON DESG_MAST.code = EMP_MAST.desg_code AND EMP_MAST.comp_code = DESG_MAST.comp_code
                        WHERE EMP_MAST.COMP_CODE = @CompCode
                        AND EMP_MAST.JOIN_DATE IS NOT NULL
                        AND (
                        EMP_MAST.TYPE = 'staff' 
                        OR EMP_MAST.TYPE = 'SEMI STAFF' 
                        OR EMP_MAST.PF_APPL = 'yes' 
                        OR EMP_MAST.ESI_APPL = 'yes'
                        )
                        ORDER BY EMP_MAST.code";

                    var employeeList = new List<Dictionary<string, object>>();

                    using (SqlCommand empCmd = new SqlCommand(empQuery, con))
                    {
                        empCmd.Parameters.AddWithValue("@CompCode", globalVars.PubCompCode);
                        using (var reader = await empCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var emp = new Dictionary<string, object>
                                {
                                    ["EMP_CODE"] = reader["EMP_CODE"].ToString(),
                                    ["EMP_NAME"] = reader["EMP_NAME"].ToString(),
                                    ["E_SHIFT"] = reader["E_SHIFT"].ToString(),
                                    ["DEPARTMENT"] = reader["DEPARTMENT"].ToString(),
                                    ["DESIGNATION"] = reader["DESIGNATION"].ToString()
                                };
                                employeeList.Add(emp);
                            }
                        }
                    }

                    // 3. Fetch all attendance records for the month
                    string attnQuery = @"
                    SELECT emp_code, v_date, status 
                    FROM PAY_ATTEN 
                    WHERE v_type = 'ATTN' 
                    AND MONTH(v_date) = @Month 
                    AND YEAR(v_date) = @Year 
                    AND COMP_CODE = @CompCode 
                    AND BRANCH_CODE = @BranchCode 
                    AND YEAR_CODE = @YearCode";

                    var attendanceMap = new Dictionary<string, Dictionary<int, string>>(); // emp_code => { day => status }
                    var workingDayMap = new Dictionary<string, int>();

                    using (SqlCommand attnCmd = new SqlCommand(attnQuery, con))
                    {
                        attnCmd.Parameters.AddWithValue("@Month", month);
                        attnCmd.Parameters.AddWithValue("@Year", year);
                        attnCmd.Parameters.AddWithValue("@CompCode", globalVars.PubCompCode);
                        attnCmd.Parameters.AddWithValue("@BranchCode", 1);
                        attnCmd.Parameters.AddWithValue("@YearCode", globalVars.PubFYearCode);

                        using (var reader = await attnCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string empCode = reader["emp_code"].ToString();
                                DateTime vdate = Convert.ToDateTime(reader["v_date"]);
                                string status = reader["status"].ToString();
                                int day = vdate.Day;

                                if (!attendanceMap.ContainsKey(empCode))
                                    attendanceMap[empCode] = new Dictionary<int, string>();

                                attendanceMap[empCode][day] = status;

                                if (new[] { "P", "E", "L", "K", "M" }.Contains(status))
                                {
                                    if (!workingDayMap.ContainsKey(empCode))
                                        workingDayMap[empCode] = 0;

                                    workingDayMap[empCode]++;
                                }
                            }
                        }
                    }

                    // 4. Build the result list
                    var resultList = new List<Dictionary<string, object>>();

                    foreach (var emp in employeeList)
                    {
                        string empCode = emp["EMP_CODE"].ToString();

                        // Skip employees with no 'P' days if skipEmpty = 1 (like VB.NET GoTo A)
                        if (skipEmpty == 1)
                        {
                            if (!attendanceMap.ContainsKey(empCode) ||
                                !attendanceMap[empCode].Values.Any(s => s == "P"))
                            {
                                continue;
                            }
                        }

                        var days = new Dictionary<string, string>();
                        if (attendanceMap.ContainsKey(empCode))
                        {
                            foreach (var kv in attendanceMap[empCode])
                            {
                                days[$"D{kv.Key}"] = kv.Value;
                            }
                        }

                        var record = new Dictionary<string, object>
                {
                    { "EMP_CODE", emp["EMP_CODE"] },
                    { "EMP_NAME", emp["EMP_NAME"] },
                    { "DEPARTMENT", emp["DEPARTMENT"] },
                    { "DESIGNATION", emp["DESIGNATION"] },
                    { "E_SHIFT", emp["E_SHIFT"] },
                    { "W_DAY", workingDayMap.ContainsKey(empCode) ? workingDayMap[empCode] : 0 },
                    { "DAYS", days }
                };

                        resultList.Add(record);
                    }

                    return Json(new { success = true, data = resultList });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error occurred while loading data.",
                    error = ex.Message
                });
            }
        }

        public class Form12SaveRequest
        {
            public DateTime VDate { get; set; }
            public List<From12_Model> Data { get; set; }
        }

        [HttpPost]
        public IActionResult SaveData([FromBody] Form12SaveRequest request)
        {
            if (request.Data == null || !request.Data.Any())
                return Json(new { success = false, message = "No data received." });

            var vMonth = request.VDate.Month;
            var vYear = request.VDate.Year;

            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                using var transaction = conn.BeginTransaction();

                // Delete existing records for the given month/year
                string deleteQuery = @"
                DELETE FROM PAY_FORM12
                WHERE DATEPART(month, V_DATE) = @Month
                AND DATEPART(year, V_DATE) = @Year
                AND COMP_CODE = @CompCode
                AND BRANCH_CODE = @BranchCode
                AND YEAR_CODE = @YearCode;
        ";

                using (var deleteCmd = new SqlCommand(deleteQuery, conn, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@Month", vMonth);
                    deleteCmd.Parameters.AddWithValue("@Year", vYear);
                    deleteCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@BranchCode", 1);
                    deleteCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);

                    deleteCmd.ExecuteNonQuery();
                }

                // Insert each row from the request
                foreach (var entry in request.Data)
                {
                    using var cmd = new SqlCommand("Sp_Form_12", conn , transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@Action", "save");
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@CODE", entry.CODE );
                    cmd.Parameters.AddWithValue("@DEPARTMENT", entry.DEPARTMENT );
                    cmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE );
                    cmd.Parameters.AddWithValue("@EMP_NAME", entry.EMP_NAME );
                    cmd.Parameters.AddWithValue("@FATHER_NAME", entry.FATHER_NAME );
                    cmd.Parameters.AddWithValue("@DESIGNATION", entry.DESIGNATION );
                    cmd.Parameters.AddWithValue("@V_DATE", entry.V_DATE );

                    // Explicitly add D1–D31 parameters
                    cmd.Parameters.AddWithValue("@D1", entry.D1 );
                    cmd.Parameters.AddWithValue("@D2", entry.D2 );
                    cmd.Parameters.AddWithValue("@D3", entry.D3 );
                    cmd.Parameters.AddWithValue("@D4", entry.D4 );
                    cmd.Parameters.AddWithValue("@D5", entry.D5);
                    cmd.Parameters.AddWithValue("@D6", entry.D6 );
                    cmd.Parameters.AddWithValue("@D7", entry.D7 );
                    cmd.Parameters.AddWithValue("@D8", entry.D8 );
                    cmd.Parameters.AddWithValue("@D9", entry.D9 );
                    cmd.Parameters.AddWithValue("@D10", entry.D10 );
                    cmd.Parameters.AddWithValue("@D11", entry.D11 );
                    cmd.Parameters.AddWithValue("@D12", entry.D12 );
                    cmd.Parameters.AddWithValue("@D13", entry.D13);
                    cmd.Parameters.AddWithValue("@D14", entry.D14 );
                    cmd.Parameters.AddWithValue("@D15", entry.D15 );
                    cmd.Parameters.AddWithValue("@D16", entry.D16 );
                    cmd.Parameters.AddWithValue("@D17", entry.D17 );
                    cmd.Parameters.AddWithValue("@D18", entry.D18 );
                    cmd.Parameters.AddWithValue("@D19", entry.D19 );
                    cmd.Parameters.AddWithValue("@D20", entry.D20 );
                    cmd.Parameters.AddWithValue("@D21", entry.D21 );
                    cmd.Parameters.AddWithValue("@D22", entry.D22 );
                    cmd.Parameters.AddWithValue("@D23", entry.D23 );
                    cmd.Parameters.AddWithValue("@D24", entry.D24 );
                    cmd.Parameters.AddWithValue("@D25", entry.D25 );
                    cmd.Parameters.AddWithValue("@D26", entry.D26 );
                    cmd.Parameters.AddWithValue("@D27", entry.D27 );
                    cmd.Parameters.AddWithValue("@D28", entry.D28 );
                    cmd.Parameters.AddWithValue("@D29", entry.D29 );
                    cmd.Parameters.AddWithValue("@D30", entry.D30 );
                    cmd.Parameters.AddWithValue("@D31", entry.D31 );

                    cmd.Parameters.AddWithValue("@W_DAY", entry.W_DAY );
                    cmd.Parameters.AddWithValue("@E_SHIFT", entry.E_SHIFT );

                    // Audit info
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.ExecuteNonQuery();
                   
                }
                transaction.Commit();

                return Json(new { success = true, message = "Shift schedule saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving shift schedule.", error = ex.Message });
            }
        }





        public class WeeklyOffRequest
        {
            public string MonthDate { get; set; }  // send as string, parse in controller
            public List<Dictionary<string, object>> Form12Data { get; set; }
        }

        [HttpPost]
        public IActionResult FillWeeklyOff([FromBody] WeeklyOffRequest request)
         {
            try
             {
                if (request == null)
                    return Json(new { success = false, message = "Request is null" });

                if (!DateTime.TryParse(request.MonthDate, out DateTime monthDate))
                    return Json(new { success = false, message = "Invalid month date format." });

                if (request.Form12Data == null || request.Form12Data.Count == 0)
                    return Json(new { success = false, message = "Form12 data is empty." });
                              
                DataTable dtForm12 = new DataTable();
                        
                var firstRow = request.Form12Data.First();
                foreach (var key in firstRow.Keys)
                    dtForm12.Columns.Add(key);

             
                foreach (var dict in request.Form12Data)
                {
                    var row = dtForm12.NewRow();
                    foreach (var kvp in dict)
                        row[kvp.Key] = kvp.Value ?? DBNull.Value;
                    dtForm12.Rows.Add(row);
                }

      
                var g = _globalVariableService.GetGlobalVariables();
                DateTime startDate = new DateTime(monthDate.Year, monthDate.Month, 1);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);
                int totalDays = endDate.Day;

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                foreach (DataRow emp in dtForm12.Rows)
                {
                    if (emp["CODE"] == DBNull.Value) continue;

                    int empCode = Convert.ToInt32(emp["CODE"]);
                    string offDay = "";

                    using (SqlCommand cmd = new SqlCommand("SELECT OFFDAY FROM EMP_MAST WHERE CODE=@CODE AND COMP_CODE=@COMP", conn))
                    {
                        cmd.Parameters.AddWithValue("@CODE", empCode);
                        cmd.Parameters.AddWithValue("@COMP", g.PubCompCode);
                        object? result = cmd.ExecuteScalar();
                        offDay = result?.ToString() ?? "";
                    }

                    int workDayCounter = 0;

    
                    for (int d = 1; d <= totalDays; d++)
                    {
                        string col = $"D{d}";
                        if (dtForm12.Columns.Contains(col) && emp[col]?.ToString() == "O")
                            emp[col] = "A";
                    }

                
                    for (int d = 1; d <= totalDays; d++)
                    {
                        string col = $"D{d}";
                        if (!dtForm12.Columns.Contains(col)) continue;

                        string? value = emp[col]?.ToString();
                        DateTime currentDate = new DateTime(monthDate.Year, monthDate.Month, d);
                        string dayName = currentDate.ToString("ddd");

                        if (dayName.Equals(offDay, StringComparison.OrdinalIgnoreCase))
                        {
                            if (value == "P" || value == "A")
                                value = "O";
                        }

                        if (value is "P" or "E" or "L" or "K" or "M")
                            workDayCounter++;

                        emp[col] = value;
                    }

                    emp["W_DAY"] = workDayCounter;

                    int salaryWorkday = 0;
                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(WORKDAY,0) FROM PAY_SALARY 
                    WHERE EMP_CODE=@EMP AND MONTH(SDATE)=@M AND YEAR(SDATE)=@Y AND COMP_CODE=@C;
                    SELECT ISNULL(WORKDAY,0) FROM PAY_SALARYC19
                    WHERE EMP_CODE=@EMP AND MONTH(SDATE)=@M AND YEAR(SDATE)=@Y AND COMP_CODE=@C
                    AND BRANCH_CODE=@B AND YEAR_CODE=@YCODE;", conn))
                    {
                        cmd.Parameters.AddWithValue("@EMP", empCode);
                        cmd.Parameters.AddWithValue("@M", monthDate.Month);
                        cmd.Parameters.AddWithValue("@Y", monthDate.Year);
                        cmd.Parameters.AddWithValue("@C", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@B", 1);
                        cmd.Parameters.AddWithValue("@YCODE", g.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                                salaryWorkday += Convert.ToInt32(reader[0]);
                            if (reader.NextResult() && reader.Read())
                                salaryWorkday += Convert.ToInt32(reader[0]);
                        }
                    }

                    int currentWork = workDayCounter;

                    if (salaryWorkday > 0 && currentWork != salaryWorkday)
                    {
                        for (int d = 1; d <= totalDays; d++)
                        {
                            string col = $"D{d}";
                            if (!dtForm12.Columns.Contains(col)) continue;

                            string? value = emp[col]?.ToString();

                            if (currentWork > salaryWorkday && value == "P")
                            {
                                emp[col] = "A";
                                currentWork--;
                            }
                            else if (currentWork < salaryWorkday && value == "A")
                            {
                                emp[col] = "P";
                                currentWork++;
                            }

                            if (currentWork == salaryWorkday)
                                break;
                        }

                        emp["W_DAY"] = currentWork;
                    }
                }

            
                var jsonData = new List<Dictionary<string, object>>();
                foreach (DataRow row in dtForm12.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dtForm12.Columns)
                    {
                        dict[col.ColumnName] = row[col] == DBNull.Value ? "" : row[col];
                    }
                    jsonData.Add(dict);
                }

                return Json(new
                {
                    success = true,
                    message = "Weekly off applied successfully!",
                    totalDays,
                    data = jsonData
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public IActionResult updateweekly([FromBody] WeeklyOffRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Request is null" });

                if (!DateTime.TryParse(request.MonthDate, out DateTime monthDate))
                    return Json(new { success = false, message = "Invalid month date format." });

                if (request.Form12Data == null || request.Form12Data.Count == 0)
                    return Json(new { success = false, message = "Form12 data is empty." });

     
                DataTable dt = new DataTable();
                var first = request.Form12Data.First();
                foreach (var key in first.Keys)
                    dt.Columns.Add(key);
                foreach (var dict in request.Form12Data)
                {
                    var row = dt.NewRow();
                    foreach (var kv in dict)
                        row[kv.Key] = kv.Value ?? DBNull.Value;
                    dt.Rows.Add(row);
                }


                DateTime firstOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
                DateTime lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
                int totalDays = lastOfMonth.Day;

                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    DataRow row = dt.Rows[i];
                    if (row["CODE"] == DBNull.Value) continue;

                    int empCode = Convert.ToInt32(row["CODE"]);
                    string offday = "";

                   
                    using (var cmdOff = new SqlCommand("SELECT OFFDAY FROM EMP_MAST WHERE CODE=@CODE AND COMP_CODE=@COMP", conn))
                    {
                        cmdOff.Parameters.AddWithValue("@CODE", empCode);
                        cmdOff.Parameters.AddWithValue("@COMP", g.PubCompCode);
                        object res = cmdOff.ExecuteScalar();
                        offday = res?.ToString() ?? "";
                    }

              
                    int salaryWorkdays = 0;

                    using (var cmdSal = new SqlCommand(@"
                SELECT ISNULL(WORKDAY, 0) AS WD FROM PAY_SALARY
                  WHERE EMP_CODE = @EMP AND MONTH(SDATE) = @M AND YEAR(SDATE) = @Y AND COMP_CODE = @C;
                SELECT ISNULL(WORKDAY, 0) AS WD FROM PAY_SALARYC19
                  WHERE EMP_CODE = @EMP AND MONTH(SDATE) = @M AND YEAR(SDATE) = @Y AND COMP_CODE = @C
                    AND BRANCH_CODE = @B AND YEAR_CODE = @YCODE;", conn))
                    {
                        cmdSal.Parameters.AddWithValue("@EMP", empCode);
                        cmdSal.Parameters.AddWithValue("@M", monthDate.Month);
                        cmdSal.Parameters.AddWithValue("@Y", monthDate.Year);
                        cmdSal.Parameters.AddWithValue("@C", g.PubCompCode);
                        cmdSal.Parameters.AddWithValue("@B", 1);
                        cmdSal.Parameters.AddWithValue("@YCODE", g.PubFYearCode);

                        using (var reader = cmdSal.ExecuteReader())
                        {
                            if (reader.Read())
                                salaryWorkdays += Convert.ToInt32(reader["WD"]);
                            if (reader.NextResult() && reader.Read())
                                salaryWorkdays += Convert.ToInt32(reader["WD"]);
                        }
                    }

                    int gridWorkDayValue = 0;
                    if (dt.Columns.Contains("W_DAY") && row["W_DAY"] != DBNull.Value)
                        gridWorkDayValue = Convert.ToInt32(row["W_DAY"]);

                    int diff = gridWorkDayValue - salaryWorkdays;

                    if (diff > 0)  
                    {
                        for (int d = 1; d <= totalDays; d++)
                        {
                            if (diff <= 0) break;
                            string colName = $"D{d}";
                            if (!dt.Columns.Contains(colName)) continue;

                            var cellVal = row[colName]?.ToString();
                            if (cellVal == "P")
                            {
                            
                                if (cellVal != "L" && cellVal != "E" && cellVal != "K" && cellVal != "M")
                                {
                                    row[colName] = "A";
                                    diff--;
                                }
                            }
                        }
                    }
                    else if (diff < 0)
                    {
                        for (int d = 1; d <= totalDays; d++)
                        {
                            if (diff == 0) break;
                            string colName = $"D{d}";
                            if (!dt.Columns.Contains(colName)) continue;

                            var cellVal = row[colName]?.ToString();
                            if (cellVal == "A" || cellVal == "O")
                            {
                              
                                if (cellVal != "L" && cellVal != "E" && cellVal != "K" && cellVal != "M" && cellVal != "P")
                                {
                                
                                    DateTime curDate = new DateTime(monthDate.Year, monthDate.Month, d);
                                    string dayName = curDate.ToString("ddd");

                                    if (!dayName.Equals(offday, StringComparison.OrdinalIgnoreCase))
                                    {
                                        row[colName] = "P";
                                        diff++;
                                    }
                                }
                            }
                        }
                    }
                }
                              
                foreach (DataRow row in dt.Rows)
                {
                    int wcount = 0;
                    for (int d = 1; d <= totalDays; d++)
                    {
                        string colName = $"D{d}";
                        if (!dt.Columns.Contains(colName)) continue;
                        var v = row[colName]?.ToString();
                        if (v == "P" || v == "E" || v == "L" || v == "K" || v == "M")
                            wcount++;
                    }
                  
                    if (dt.Columns.Contains("W_DAY"))
                        row["W_DAY"] = wcount;
                    else
                        dt.Columns.Add("W_DAY", typeof(int));  
                    row["W_DAY"] = wcount;
                }
                             
                var resultList = new List<Dictionary<string, object>>();
                foreach (DataRow r in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn c in dt.Columns)
                    {
                        dict[c.ColumnName] = r[c] == DBNull.Value ? "" : r[c];
                    }
                    resultList.Add(dict);
                }

                return Json(new
                {
                    success = true,
                    message = "Weekly off updated",
                    totalDays,
                    data = resultList
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        //[HttpPost]
        //public IActionResult CheckSalaryDay([FromBody] WeeklyOffRequest request)
        //{
        //    try
        //    {
        //        if (request == null)
        //            return Json(new { success = false, message = "Request is null" });

        //        if (!DateTime.TryParse(request.MonthDate, out DateTime monthDate))
        //            return Json(new { success = false, message = "Invalid month date format." });

        //        if (request.Form12Data == null || request.Form12Data.Count == 0)
        //            return Json(new { success = false, message = "Form12 data is empty." });

        //        DataTable dt = new DataTable();
        //        var first = request.Form12Data.First();
        //        foreach (var key in first.Keys)
        //            dt.Columns.Add(key);
        //        foreach (var dict in request.Form12Data)
        //        {
        //            var row = dt.NewRow();
        //            foreach (var kv in dict)
        //                row[kv.Key] = kv.Value ?? DBNull.Value;
        //            dt.Rows.Add(row);
        //        }

        //        DateTime firstOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
        //        DateTime lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        //        int totalDays = lastOfMonth.Day;

        //        var g = _globalVariableService.GetGlobalVariables();

        //        List<string> mismatchedEmployees = new List<string>();
        //        using var conn = _dbConnection.GetErpConnection();
        //        conn.Open();

        //        for (int i = 0; i < dt.Rows.Count; i++)
        //        {
        //            var row = dt.Rows[i];
        //            if (row["CODE"] == DBNull.Value) continue;

        //            int empCode = Convert.ToInt32(row["CODE"]);
        //            int salaryWorkdays = 0;

        //            using (var cmd1 = new SqlCommand(
        //                "SELECT ISNULL(WORKDAY,0) FROM PAY_SALARY WHERE EMP_CODE=@EMP AND MONTH(SDATE)=@M AND YEAR(SDATE)=@Y AND COMP_CODE=@C AND BRANCH_CODE=@B AND YEAR_CODE=@YCODE", conn))
        //            {
        //                cmd1.Parameters.AddWithValue("@EMP", empCode);
        //                cmd1.Parameters.AddWithValue("@M", monthDate.Month);
        //                cmd1.Parameters.AddWithValue("@Y", monthDate.Year);
        //                cmd1.Parameters.AddWithValue("@C", g.PubCompCode);
        //                cmd1.Parameters.AddWithValue("@B", 1);
        //                cmd1.Parameters.AddWithValue("@YCODE", g.PubFYearCode);

        //                var res = cmd1.ExecuteScalar();
        //                salaryWorkdays += res != null ? Convert.ToInt32(res) : 0;
        //            }

        //            using (var cmd2 = new SqlCommand(
        //                "SELECT ISNULL(WORKDAY,0) FROM PAY_SALARYC19 WHERE EMP_CODE=@EMP AND MONTH(SDATE)=@M AND YEAR(SDATE)=@Y AND COMP_CODE=@C AND BRANCH_CODE=@B AND YEAR_CODE=@YCODE", conn))
        //            {
        //                cmd2.Parameters.AddWithValue("@EMP", empCode);
        //                cmd2.Parameters.AddWithValue("@M", monthDate.Month);
        //                cmd2.Parameters.AddWithValue("@Y", monthDate.Year);
        //                cmd2.Parameters.AddWithValue("@C", g.PubCompCode);
        //                cmd2.Parameters.AddWithValue("@B", 1);
        //                cmd2.Parameters.AddWithValue("@YCODE", g.PubFYearCode);

        //                var res = cmd2.ExecuteScalar();
        //                salaryWorkdays += res != null ? Convert.ToInt32(res) : 0;
        //            }

        //            int gridWorkDays = 0;
        //            int colIndex = 5 + totalDays;
        //            if (dt.Columns.Count > colIndex && row[colIndex] != DBNull.Value)
        //                gridWorkDays = Convert.ToInt32(row[colIndex]);

        //            if (gridWorkDays != salaryWorkdays)
        //            {
        //                mismatchedEmployees.Add($"Code: {empCode}, Salary Workdays: {salaryWorkdays}");
        //            }
        //        }

        //        return Json(new
        //        {
        //            success = true,
        //            message = "Check completed",
        //            mismatches = mismatchedEmployees
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}


        [HttpPost]
        public IActionResult CheckSalaryDay([FromBody] WeeklyOffRequest request)
        {
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Request is null" });

                if (!DateTime.TryParse(request.MonthDate, out DateTime monthDate))
                    return Json(new { success = false, message = "Invalid month date format." });

                if (request.Form12Data == null || request.Form12Data.Count == 0)
                    return Json(new { success = false, message = "Form12 data is empty." });

                DataTable dt = new DataTable();
                var first = request.Form12Data.First();
                foreach (var key in first.Keys)
                    dt.Columns.Add(key);
                foreach (var dict in request.Form12Data)
                {
                    var row = dt.NewRow();
                    foreach (var kv in dict)
                        row[kv.Key] = kv.Value ?? DBNull.Value;
                    dt.Rows.Add(row);
                }

                DateTime firstOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
                DateTime lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
                int totalDays = lastOfMonth.Day;

                var g = _globalVariableService.GetGlobalVariables();

                List<string> errorList = new List<string>();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                foreach (DataRow row in dt.Rows)
                {
                    if (row["CODE"] == DBNull.Value) continue;
                    if (!int.TryParse(row["CODE"].ToString(), out int empCode))
                        continue;

                    int salaryWorkdays = 0;
                    // PAY_SALARY
                    using (var cmd1 = new SqlCommand(
                        "SELECT ISNULL(WORKDAY,0) FROM PAY_SALARY WHERE EMP_CODE=@EMP AND MONTH(SDATE)=@M AND YEAR(SDATE)=@Y AND COMP_CODE=@C AND BRANCH_CODE=@B AND YEAR_CODE=@YCODE",
                        conn))
                    {
                        cmd1.Parameters.AddWithValue("@EMP", empCode);
                        cmd1.Parameters.AddWithValue("@M", monthDate.Month);
                        cmd1.Parameters.AddWithValue("@Y", monthDate.Year);
                        cmd1.Parameters.AddWithValue("@C", g.PubCompCode);
                        cmd1.Parameters.AddWithValue("@B", 1);
                        cmd1.Parameters.AddWithValue("@YCODE", g.PubFYearCode);

                        object res = cmd1.ExecuteScalar();
                        if (res != null && int.TryParse(res.ToString(), out int v1))
                            salaryWorkdays += v1;
                    }

                    // PAY_SALARYC19
                    using (var cmd2 = new SqlCommand(
                        "SELECT ISNULL(WORKDAY,0) FROM PAY_SALARYC19 WHERE EMP_CODE=@EMP AND MONTH(SDATE)=@M AND YEAR(SDATE)=@Y AND COMP_CODE=@C AND BRANCH_CODE=@B AND YEAR_CODE=@YCODE",
                        conn))
                    {
                        cmd2.Parameters.AddWithValue("@EMP", empCode);
                        cmd2.Parameters.AddWithValue("@M", monthDate.Month);
                        cmd2.Parameters.AddWithValue("@Y", monthDate.Year);
                        cmd2.Parameters.AddWithValue("@C", g.PubCompCode);
                        cmd2.Parameters.AddWithValue("@B",1);
                        cmd2.Parameters.AddWithValue("@YCODE", g.PubFYearCode);

                        object res = cmd2.ExecuteScalar();
                        if (res != null && int.TryParse(res.ToString(), out int v2))
                            salaryWorkdays += v2;
                    }

               
                    int gridWorkDays = 0;
                    if (dt.Columns.Contains("W_DAY") && row["W_DAY"] != DBNull.Value)
                    {
                        if (!int.TryParse(row["W_DAY"].ToString(), out gridWorkDays))
                        {
                            gridWorkDays = 0;
                        }
                    }
                    else
                    {
                        int colIndex = 5 + totalDays;
                        if (colIndex < dt.Columns.Count)
                        {
                            object obj = row[colIndex];
                            if (obj != null && obj != DBNull.Value)
                            {
                                if (!int.TryParse(obj.ToString(), out gridWorkDays))
                                {
                                    gridWorkDays = 0;
                                }
                            }
                        }
                    }

                    if (gridWorkDays != salaryWorkdays)
                    {
                        errorList.Add($"Code: {empCode}, SalaryWorkdays: {salaryWorkdays}, Grid: {gridWorkDays}");
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Check complete",
                    data = errorList  
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
