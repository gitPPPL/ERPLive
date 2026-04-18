using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System.Data;
using System.Data.SqlTypes;
using System.Numerics;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class AttendenceEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public AttendenceEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/AttendenceEntry/Index.cshtml");
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
                    string lastV_NO_Query = "select max(V_no) from PAY_ATTEN where V_TYPE=@V_TYPE and COMP_CODE= @CompCode and BRANCH_CODE= 1 and YEAR_CODE= @YearCode  ";
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
                string query = "SELECT CODE, NAME FROM DOCTYPE_MAST  WHERE Code in ('ATTN')";
                var DDlDoctype = _dropdownService.GetDropdownList(query);
                return Json(DDlDoctype);
            }


        }

        public JsonResult DDlDepartment()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code, name from DEPT_MAST where COMP_CODE = " + getdata.PubCompCode + " order by NAME";
                var DDlDepartment = _dropdownService.GetDropdownList(query);
                return Json(DDlDepartment);
            }


        }

        public JsonResult DDlDesignation()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code, name from DESG_MAST where COMP_CODE =" + getdata.PubCompCode + " order by NAME ";
                var DDlDesignation = _dropdownService.GetDropdownList(query);
                return Json(DDlDesignation);
            }


        }

        public JsonResult CheckPaySalaryDate(DateTime dtdate)
        {
            var FDate = dtdate.ToString("yyyy-MM-dd");

            var getdata = _globalVariableService.GetGlobalVariables();

            string qry = @"SELECT COUNT(*) FROM PAY_SALARY 
                   WHERE sdate >= @Date 
                   AND FINAL = 'Y' 
                   AND COMP_CODE = @CompCode 
                   AND BRANCH_CODE = @BranchCode";

            int count = 0;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@Date", FDate);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);

                    con.Open();
                    count = (int)cmd.ExecuteScalar();
                }
            }

            if (count >= 20 && getdata.PubUserId != "1")
            {
                return Json(new
                {
                    Success = false,
                    Message = "No Addition/Alteration/Deletion Allowed, Salary is Final, Contact your System Administrator."
                });
            }

            return Json(new
            {
                Success = true,
                Message = ""
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetDataWithoutMac(int DeptCode, int Desigcode, DateTime v_date )
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            string query = "";
            string deptCondition = "";
            string desigCondition = "";

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    if (DeptCode != 0)
                    {
                        deptCondition = " AND A.DEPT_CODE = @dept ";
                    }

                    if (Desigcode != 0)
                    {
                        desigCondition = " AND A.DESG_CODE = @desigcode ";
                    }

                     query = @"
                        SELECT   B.NAME AS DEPARTMENT, A.NAME,  C.NAME AS DESIGNATION,  D.SHIFT,   D.STATUS, 
                        D.REMARK,    a.CODE as  EMP_CODE,   D.OFFDAY,  D.SNO,   D.v_type,    D.v_no 
                        FROM EMP_MAST A
                        LEFT JOIN DEPT_MAST B  ON B.CODE = A.DEPT_CODE AND B.COMP_CODE = A.COMP_CODE
                        LEFT JOIN DESG_MAST C  ON C.CODE = A.DESG_CODE   AND C.COMP_CODE = A.COMP_CODE
                        LEFT JOIN PAY_ATTEN D    ON D.EMP_CODE = A.CODE    AND D.COMP_CODE = A.COMP_CODE   AND D.BRANCH_CODE = @BranchCode AND D.V_DATE = @v_date
                        WHERE A.COMP_CODE = @CompCode   AND (A.resign_date IS NULL OR A.resign_date > @v_date)    AND A.JOIN_DATE <= @v_date
                        " + deptCondition + desigCondition + @"
                        ORDER BY   B.NAME,   A.CODE;";


                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                  
                        cmd.Parameters.Add("@v_date", SqlDbType.SmallDateTime).Value = v_date;

                        if (Desigcode != 0)
                        {
                            cmd.Parameters.Add("@desigcode", SqlDbType.Int).Value = Desigcode;
                        }

                        if (DeptCode != 0)
                        {
                            cmd.Parameters.Add("@dept", SqlDbType.Int).Value = DeptCode;
                        }

                        cmd.Parameters.Add("@compCode", SqlDbType.Int).Value = GetGlobalCode.PubCompCode;
                        cmd.Parameters.Add("@branchCode", SqlDbType.Int).Value = 1;

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();
                            while (await rdr.ReadAsync())
                            {
                                var result = new
                                {
                                    DEPARTMENT = rdr["DEPARTMENT"]?.ToString(),
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                    NAME = rdr["NAME"]?.ToString(),
                                    DESIGNATION = rdr["DESIGNATION"]?.ToString(),
                                    SHIFT = rdr["SHIFT"]?.ToString(),
                                    STATUS = rdr["STATUS"]?.ToString(),
                                    REMARK = rdr["REMARK"]?.ToString(),
                                    OFFDAY = rdr["OFFDAY"]?.ToString(),
                                    SNO = rdr["SNO"] != DBNull.Value ? Convert.ToDecimal(rdr["SNO"]) : 0,
                                    v_type = rdr["v_type"]?.ToString(),
                                    v_no = rdr["v_no"] != DBNull.Value ? Convert.ToInt32(rdr["v_no"]) : 0
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

        [HttpGet]
        public async Task<IActionResult> GetDataWithMac(int DeptCode, int Desigcode, DateTime v_date, string vtype)
        {
            var global = _globalVariableService.GetGlobalVariables();
            string deptCondition = DeptCode != 0 ? " AND A.DEPT_CODE = @DeptCode " : "";
            string desigCondition = Desigcode != 0 ? " AND A.DESG_CODE = @DesigCode " : "";

            string query = $@"
                SELECT 
                B.NAME AS DEPARTMENT, A.CODE AS EMP_CODE, A.NAME, 
                C.NAME AS DESIGNATION, A.SHIFT, A.FITMENT 
                FROM EMP_MAST A
                LEFT JOIN DEPT_MAST B ON B.CODE = A.DEPT_CODE AND B.COMP_CODE = A.COMP_CODE  
                LEFT JOIN DESG_MAST C ON C.CODE = A.DESG_CODE AND C.COMP_CODE = A.COMP_CODE 
                WHERE A.COMP_CODE = @CompCode 
                AND (A.resign_date IS NULL OR A.resign_date > @v_date)  
                AND A.JOIN_date <= @v_date 
                {deptCondition} {desigCondition}
                ORDER BY B.NAME, A.CODE";

            var result = new List<object>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    var employees = new List<dynamic>();

                    // Step 1: Fetch employee master data
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@v_date", v_date);

                        if (DeptCode != 0)
                            cmd.Parameters.AddWithValue("@DeptCode", DeptCode);

                        if (Desigcode != 0)
                            cmd.Parameters.AddWithValue("@DesigCode", Desigcode);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                employees.Add(new
                                {
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["EMP_CODE"]) : 0,
                                    NAME = rdr["NAME"]?.ToString(),
                                    DEPARTMENT = rdr["DEPARTMENT"]?.ToString(),
                                    DESIGNATION = rdr["DESIGNATION"]?.ToString(),
                                    SHIFT = rdr["SHIFT"]?.ToString(),
                                    FITMENT = rdr["FITMENT"]?.ToString()
                                });
                            }
                        }
                    }

                    // Step 2: Loop over each employee and run time data queries on the SAME connection
                    foreach (var emp in employees)
                    {
                        string finalShift = "";
                        string finalStatus = "";
                        string remark = "";

                        string timeQuery = (emp.FITMENT == "NOIDA") ? @"
                            SELECT TOP 1 * FROM PAY_timedata 
                            WHERE v_type = 'MACD' AND COMP_CODE = @CompCode 
                            AND YEAR_CODE = @YearCode AND BRANCH_CODE = @BranchCode 
                            AND FORMAT(v_date, 'yyyyMMdd') = @vDate 
                            AND EMP_CODE = @EmpCode 
                            AND NOT EXISTS (
                            SELECT * FROM PAY_ATTEN 
                            WHERE V_TYPE = @VType AND COMP_CODE = @CompCode 
                            AND YEAR_CODE = @YearCode AND BRANCH_CODE = @BranchCode 
                            AND FORMAT(v_date, 'yyyyMMdd') = @vDate 
                            AND EMP_CODE = @EmpCode AND STATUS = 'R'
                            )
                            ORDER BY in_time" :
                            @"
                            SELECT * FROM PAY_timedata 
                            WHERE v_type = 'MACD' AND COMP_CODE = @CompCode 
                            AND YEAR_CODE = @YearCode AND BRANCH_CODE = @BranchCode 
                            AND FORMAT(v_date, 'yyyyMMdd') = @vDate 
                            AND EMP_CODE = @EmpCode 
                            AND NOT EXISTS (
                            SELECT * FROM PAY_ATTEN 
                            WHERE V_TYPE = @VType AND COMP_CODE = @CompCode 
                            AND YEAR_CODE = @YearCode AND BRANCH_CODE = @BranchCode 
                            AND FORMAT(v_date, 'yyyyMMdd') = @vDate 
                            AND EMP_CODE = @EmpCode AND STATUS = 'R'
                            )
                            ORDER BY in_time ASC";

                        using (SqlCommand timeCmd = new SqlCommand(timeQuery, con))
                        {
                            timeCmd.Parameters.AddWithValue("@CompCode", global.PubCompCode);
                            timeCmd.Parameters.AddWithValue("@YearCode", global.PubFYearCode);
                            timeCmd.Parameters.AddWithValue("@BranchCode", 1);
                            timeCmd.Parameters.AddWithValue("@vDate", v_date.ToString("yyyyMMdd"));
                            timeCmd.Parameters.AddWithValue("@EmpCode", emp.EMP_CODE);
                            timeCmd.Parameters.AddWithValue("@VType", vtype);

                            using (var timeReader = await timeCmd.ExecuteReaderAsync())
                            {
                                if (await timeReader.ReadAsync())
                                {
                                    int inTime = timeReader["IN_TIME"] != DBNull.Value ? Convert.ToInt32(timeReader["IN_TIME"]) : 0;
                                    int outTime = timeReader["OUT_TIME"] != DBNull.Value ? Convert.ToInt32(timeReader["OUT_TIME"]) : 0;

                                    if (inTime >= 500 && inTime < 1100)
                                    {
                                        finalShift = "A";
                                        finalStatus = "P";

                                    }
                                    else if (inTime >= 1730)
                                    {
                                        finalShift = "B";
                                        finalStatus = "P";

                                    }
                                    else
                                    {
                                        remark = "In time outside shift range";
                                    }

                                    if (string.IsNullOrEmpty(timeReader["OUT_TIME"]?.ToString()))
                                    {
                                        finalStatus = "";
                                        remark = "OUT punch missing";
                                    }
                                    else if ((outTime - inTime) <= 100 && (outTime - inTime) > 0)
                                    {
                                        finalStatus = "";
                                        remark = "Short working duration";
                                    }
                                }
                                else
                                {
                                    remark = "No punch record found";
                                }
                            }
                        }


                        if (finalShift == "A" && finalStatus == "P")
                        {
                            string prevQuery = @"
                                SELECT COUNT(*) FROM PAY_ATTEN 
                                WHERE EMP_CODE = @EmpCode 
                                AND FORMAT(v_date, 'yyyyMMdd') = @PrevDate 
                                AND COMP_code = @CompCode 
                                AND BRANCH_CODE = 1 
                                AND YEAR_CODE = @YearCode 
                                AND status = 'P' 
                                AND shift = 'B' 
                                AND V_TYPE = @VType";

                            using (SqlCommand prevCmd2 = new SqlCommand(prevQuery, con))
                            {
                                prevCmd2.Parameters.AddWithValue("@EmpCode", emp.EMP_CODE);
                                prevCmd2.Parameters.AddWithValue("@PrevDate", v_date.AddDays(-1).ToString("yyyyMMdd"));
                                prevCmd2.Parameters.AddWithValue("@CompCode", global.PubCompCode);
                                prevCmd2.Parameters.AddWithValue("@YearCode", global.PubFYearCode);
                                prevCmd2.Parameters.AddWithValue("@VType", vtype);

                                int prevCount = Convert.ToInt32(await prevCmd2.ExecuteScalarAsync());

                                if (prevCount > 0)
                                {
                                    finalShift = "";
                                    finalStatus = "";
                                    remark = "Present in previous day's B shift";
                                }
                            }
                        }

                        // Final result entry
                        result.Add(new
                        {
                            DEPARTMENT = emp.DEPARTMENT,
                            EMP_CODE = emp.EMP_CODE,
                            NAME = emp.NAME,
                            DESIGNATION = emp.DESIGNATION,
                            SHIFT = finalShift,
                            STATUS = finalStatus,
                            REMARK = remark,
                            OFFDAY = "",
                            SNO = 0,
                            v_type = "",
                            v_no = 0
                        });
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Data fetched with MAC logic",
                        data = result
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching data",
                    error = ex.Message
                });
            }
        }

        public JsonResult GetVNoByDate(DateTime? VDate)
        {
            string vNo = null;

            if (!VDate.HasValue || VDate.Value < (DateTime)SqlDateTime.MinValue)
            {
                return Json(new { v_NO = vNo }); 
            }

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT V_no FROM PAY_ATTEN WHERE V_DATE = @VDate";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@VDate", VDate.Value.Date);

                    con.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        vNo = result.ToString();
                    }
                }
            }

            return Json(new { v_NO = vNo });
        }

        public JsonResult pubpaysalaryDate(DateTime dtdate)
        {
            var FDate = dtdate.ToString("yyyy-MM-dd");

            var getdata = _globalVariableService.GetGlobalVariables();

            string qry = @"SELECT COUNT(*) FROM PAY_SALARY 
                   WHERE sdate >= @Date 
                   AND FINAL = 'Y' 
                   AND COMP_CODE = @CompCode 
                   AND BRANCH_CODE = @BranchCode";

            int count = 0;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@Date", FDate);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);

                    con.Open();
                    count = (int)cmd.ExecuteScalar();
                }
            }

            if (count >= 20 && getdata.PubUserId != "1")
            {
                return Json(new
                {
                    Success = false,
                    Message = "No Addition/Alteration/Deletion Allowed, Salary is Final, Contact your System Administrator."
                });
            }

            return Json(new
            {
                Success = true,
                Message = ""
            });
        }

        public JsonResult CheackFinancialyear(DateTime dtdate)
        {
            var FDate = dtdate.ToString("yyyy-MM-dd");

            var getdata = _globalVariableService.GetGlobalVariables();

            string qry = @"select 1 from YEAR_MAST where @Date between START_DATE and END_DATE and code=@CompCode";

            int count = 0;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (var cmd = new SqlCommand(qry, con))
                {
                    cmd.Parameters.AddWithValue("@Date", FDate);
                    cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                   con.Open();
                    count = (int)cmd.ExecuteScalar();
                }
            }

            if (count >= 20 && getdata.PubUserId != "1")
            {
                return Json(new
                {
                    Success = false,
                    Message = "No Addition/Alteration/Deletion Allowed, Salary is Final, Contact your System Administrator."
                });
            }

            return Json(new
            {
                Success = true,
                Message = ""
            });
        }

        [HttpPost]
        public IActionResult SaveAttendance([FromBody] List<AttendenceEntry> data)
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
                    DELETE FROM PAY_ATTEN 
                    WHERE COMP_CODE = @COMP_CODE 
                    AND BRANCH_CODE = @BRANCH_CODE 
                    AND YEAR_CODE = @YEAR_CODE 
                    AND V_NO = @V_NO 
                    AND V_TYPE = @V_TYPE 
                    AND EMP_CODE = @EMP_CODE 
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
                        deleteCmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE);
                    
                        deleteCmd.ExecuteNonQuery();
                    }

                   
                    using (var cmd = new SqlCommand("sp_AttendencesEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        cmd.Parameters.AddWithValue("@Action", "save");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", entry.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", entry.V_DATE);
                        cmd.Parameters.AddWithValue("@V_TYPE", entry.V_TYPE );
                        cmd.Parameters.AddWithValue("@DOC_ID", entry.DOC_ID );
                        cmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE);
                        cmd.Parameters.AddWithValue("@SNO", entry.SNO);
                        cmd.Parameters.AddWithValue("@SHIFT", entry.SHIFT);
                        cmd.Parameters.AddWithValue("@STATUS", entry.STATUS );
                        cmd.Parameters.AddWithValue("@REMARK", entry.REMARK);
                        cmd.Parameters.AddWithValue("@OFFDAY", entry.OFFDAY);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



    }
}
