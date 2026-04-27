using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.AddAttachmentService;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class TimeMachineDataImportController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly FileHelper _filehelper;
        public TimeMachineDataImportController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/TimeMachineDataImport/Index.cshtml");
        }

        public int GetNextV_NO(int compCode)
        {
            int nextVNo = 1;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                var query = @"SELECT ISNULL(MAX(V_NO), 0) FROM PAY_TIMEDATA WHERE COMP_CODE = @cCode";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@cCode", compCode);
                    con.Open();

                    var result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int maxVNo))
                    {
                        nextVNo = maxVNo + 1;
                    }
                }
            }

            return nextVNo;
        }


        [HttpGet]
        public async Task<IActionResult> GetDataList(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var parsedData = new List<dynamic>();

                using (var conn = _dbConnection.GetErpConnection())
                {
                    await conn.OpenAsync();

                    var fetchCmd = new SqlCommand(@"
                SELECT COMP_CODE,V_TYPE, V_NO, V_DATE, EMP_CODE, EMP_NAME, DEPT, IN_TIME, OUT_TIME, DATA
                FROM PAY_TIMEDATA
                WHERE COMP_CODE = @cCode
                ORDER BY EMP_CODE
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", conn);

                    fetchCmd.Parameters.AddWithValue("@cCode", _globalVariableService.GetGlobalVariables().PubCompCode);
                    fetchCmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                    fetchCmd.Parameters.AddWithValue("@pageSize", pageSize);

                    using (var fetchReader = await fetchCmd.ExecuteReaderAsync())
                    {
                        while (await fetchReader.ReadAsync())
                        {
                            parsedData.Add(new
                            {
                                Code = fetchReader["EMP_CODE"]?.ToString(),
                                SrNo = fetchReader["V_NO"]?.ToString(),
                                Date = Convert.ToDateTime(fetchReader["V_DATE"]).ToString("dd-MM-yyyy"),
                                Emp_code = fetchReader["EMP_CODE"]?.ToString(),
                                Emp_name = fetchReader["EMP_NAME"]?.ToString(),
                                Deptt = fetchReader["DEPT"]?.ToString(),
                                In_Time = fetchReader["IN_TIME"]?.ToString(),
                                Out_Time = fetchReader["OUT_TIME"]?.ToString(),
                                Data = fetchReader["DATA"]?.ToString(),
                                Type = fetchReader["V_TYPE"]?.ToString()
                            });
                        }
                    }
                }

                // To get total count for pagination, you can run a separate count query:
                int totalCount = 0;
                using (var conn = _dbConnection.GetErpConnection())
                {
                    await conn.OpenAsync();

                    var countCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM PAY_TIMEDATA WHERE COMP_CODE = @cCode", conn);

                    countCmd.Parameters.AddWithValue("@cCode", _globalVariableService.GetGlobalVariables().PubCompCode);

                    totalCount = (int)await countCmd.ExecuteScalarAsync();
                }

                return Json(new { success = true, data = parsedData, totalCount = totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        //[HttpGet]
        //public async Task<IActionResult> GetDataList()
        //{
        //    try
        //    {
        //        var parsedData = new List<dynamic>();

        //        using (var conn = _dbConnection.GetErpConnection())
        //        {
        //            await conn.OpenAsync();

        //            var fetchCmd = new SqlCommand(@"
        //        SELECT COMP_CODE,V_NO, V_DATE, EMP_CODE, EMP_NAME, DEPT, IN_TIME, OUT_TIME, DATA
        //        FROM PAY_TIMEDATA
        //        WHERE COMP_CODE=@cCode  
        //        ORDER BY EMP_CODE", conn);

        //            fetchCmd.Parameters.AddWithValue("@cCode", _globalVariableService.GetGlobalVariables().PubCompCode);

        //            using (var fetchReader = await fetchCmd.ExecuteReaderAsync())
        //            {
        //                while (await fetchReader.ReadAsync())
        //                {
        //                    parsedData.Add(new
        //                    {
        //                        Code = fetchReader["EMP_CODE"]?.ToString(),
        //                        SrNo = fetchReader["V_NO"]?.ToString(),
        //                        Date = Convert.ToDateTime(fetchReader["V_DATE"]).ToString("dd-MM-yyyy"),
        //                        Emp_code = fetchReader["EMP_CODE"]?.ToString(),
        //                        Emp_name = fetchReader["EMP_NAME"]?.ToString(),
        //                        Deptt = fetchReader["DEPT"]?.ToString(),
        //                        In_Time = fetchReader["IN_TIME"]?.ToString(),
        //                        Out_Time = fetchReader["OUT_TIME"]?.ToString(),
        //                        Data = fetchReader["DATA"]?.ToString(),
        //                        Type = ""
        //                    });
        //                }
        //            }
        //        }

        //        var sorted = parsedData.OrderBy(x => ((dynamic)x).Emp_code).ToList();
        //        return Json(new { success = true, data = sorted });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}


        private int GetCompCodeFromData(string data)
        {
            string upperData = data.ToUpper();

            if (upperData.StartsWith("HOME") && (upperData.Contains("IN") || upperData.Contains("OUT")) && upperData.Any(char.IsDigit))
                return 1;
            else if (upperData.StartsWith("PL"))
                return 2;
            else if (upperData.StartsWith("PE"))
                return 4;

            return 0; 
        }

        public async Task<IActionResult> ImportFromFile(IFormFile file)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file uploaded." });

            try
            {
                var parsedData = new List<dynamic>();
                var vNoMap = new Dictionary<int, int>(); // COMP_CODE → current V_NO map

                using (var conn = _dbConnection.GetErpConnection())
                {
                    await conn.OpenAsync();

                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        string line;

                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var parts = line.Split('\t');
                            if (parts.Length < 5) continue;

                            var empCode = parts[0].Trim();
                            var rawDateTimeStr = parts[1].Trim();
                            var rawCode = parts[2].Trim();
                            var typeStr = parts[3].Trim();
                            var finalCode = parts[4].Trim();

                            if (!DateTime.TryParseExact(rawDateTimeStr, "dd-MM-yyyy HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime punchDateTime))
                                continue;

                            int type = int.TryParse(typeStr, out int parsedType) ? parsedType : 0;
                            string empCodeForLookup = empCode.TrimStart('0');

                            string empName = "", dept = "";

                            using (var cmd = new SqlCommand("SELECT E.NAME, D.NAME AS DEPT FROM EMP_MAST E LEFT JOIN DEPT_MAST D ON E.DEPT_CODE = D.CODE WHERE E.CODE = @EmpCode", conn))
                            {
                                cmd.Parameters.AddWithValue("@EmpCode", empCodeForLookup);
                                using (var readerEmp = await cmd.ExecuteReaderAsync())
                                {
                                    if (await readerEmp.ReadAsync())
                                    {
                                        empName = readerEmp["NAME"]?.ToString() ?? "";
                                        dept = readerEmp["DEPT"]?.ToString() ?? "";
                                    }
                                }
                            }

                            // Determine COMP_CODE
                            int compCode = GetCompCodeFromData(finalCode);

                            // Get next V_NO per COMP_CODE
                            if (!vNoMap.ContainsKey(compCode))
                            {
                                vNoMap[compCode] = GetNextV_NO(compCode); // Get from DB once
                            }

                            int currentVNo = vNoMap[compCode]++; // Use and increment

                            // Handle IN/OUT logic
                            string inTime = "", outTime = "";
                            string dataToStore = finalCode;
                            int? macCode = null;

                            if (rawCode.ToUpper().Contains("IN"))
                            {
                                inTime = punchDateTime.ToString("HH:mm");
                                dataToStore = "MACD";
                                macCode = 0;
                            }
                            else if (rawCode.ToUpper().Contains("OUT"))
                            {
                                outTime = punchDateTime.ToString("HH:mm");
                                dataToStore = "MOUT";
                                macCode = 0;
                            }

                            string vTypeValue = (dataToStore == "MACD" || dataToStore == "MOUT") ? dataToStore : "TM";

                            // Insert into PAY_TIMEDATA
                            using (var insertCmd = new SqlCommand(@"
                        INSERT INTO PAY_TIMEDATA 
                        (YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DOC_ID, EMP_CODE, EMP_NAME, DEPT, 
                         MAC_CODE, IN_TIME, OUT_TIME, DATA, REMARKS, LATE_MNT, LATE_HRS, LATE_TOT, DEDU_HRS, 
                         NOT_IN_PUNCH, NOT_OUT_PUNCH, STATUS, UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID)
                        VALUES 
                        (@YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO, @V_DATE, @DOC_ID, @EMP_CODE, @EMP_NAME, @DEPT, 
                         @MAC_CODE, @IN_TIME, @OUT_TIME, @DATA, @REMARKS, @LATE_MNT, @LATE_HRS, @LATE_TOT, @DEDU_HRS, 
                         @NOT_IN_PUNCH, @NOT_OUT_PUNCH, @STATUS, @UUSER, @UDATE, @EUSER, @EDATE, @AED, @WSID, @LIP, @LID)", conn))
                            {
                                insertCmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                insertCmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                                insertCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                insertCmd.Parameters.AddWithValue("@V_TYPE", vTypeValue);
                                insertCmd.Parameters.AddWithValue("@V_NO", currentVNo);
                                insertCmd.Parameters.AddWithValue("@V_DATE", punchDateTime);
                                insertCmd.Parameters.AddWithValue("@DOC_ID", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@EMP_CODE", int.TryParse(empCodeForLookup, out int codeVal) ? codeVal : 0);
                                insertCmd.Parameters.AddWithValue("@EMP_NAME", empName);
                                insertCmd.Parameters.AddWithValue("@DEPT", dept);
                                insertCmd.Parameters.AddWithValue("@MAC_CODE", (object?)macCode ?? DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@IN_TIME", string.IsNullOrWhiteSpace(inTime) ? (object)DBNull.Value : inTime);
                                insertCmd.Parameters.AddWithValue("@OUT_TIME", string.IsNullOrWhiteSpace(outTime) ? (object)DBNull.Value : outTime);
                                insertCmd.Parameters.AddWithValue("@DATA", dataToStore);
                                insertCmd.Parameters.AddWithValue("@REMARKS", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@LATE_MNT", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@LATE_HRS", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@LATE_TOT", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@DEDU_HRS", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@NOT_IN_PUNCH", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@NOT_OUT_PUNCH", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@STATUS", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@AED", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@LID", DBNull.Value);

                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // Retrieve data back for display
                    var fetchCmd = new SqlCommand(@"
                SELECT V_NO, V_DATE, EMP_CODE, EMP_NAME, DEPT, IN_TIME, OUT_TIME, DATA
                FROM PAY_TIMEDATA
                WHERE V_TYPE = 'TM' AND YEAR_CODE = @YearCode
                ORDER BY EMP_CODE", conn);

                    fetchCmd.Parameters.AddWithValue("@YearCode", DateTime.Now.Year);

                    using (var fetchReader = await fetchCmd.ExecuteReaderAsync())
                    {
                        while (await fetchReader.ReadAsync())
                        {
                            parsedData.Add(new
                            {
                                Code = fetchReader["EMP_CODE"]?.ToString(),
                                SrNo = fetchReader["V_NO"]?.ToString(),
                                Date = Convert.ToDateTime(fetchReader["V_DATE"]).ToString("dd-MM-yyyy"),
                                Emp_code = fetchReader["EMP_CODE"]?.ToString(),
                                Emp_name = fetchReader["EMP_NAME"]?.ToString(),
                                Deptt = fetchReader["DEPT"]?.ToString(),
                                In_Time = fetchReader["IN_TIME"]?.ToString(),
                                Out_Time = fetchReader["OUT_TIME"]?.ToString(),
                                Data = fetchReader["DATA"]?.ToString(),
                                Type = ""
                            });
                        }
                    }
                }

                var sorted = parsedData.OrderBy(x => ((dynamic)x).Emp_code).ToList();
                return Json(new { success = true, data = sorted });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class TimeMachineRecord
        {
            public string EmployeeCode { get; set; }
            public DateTime PunchDateTime { get; set; }
            public string RawCode { get; set; }
            public int Type { get; set; }
            public string FinalCode { get; set; }
        }


    }
}
