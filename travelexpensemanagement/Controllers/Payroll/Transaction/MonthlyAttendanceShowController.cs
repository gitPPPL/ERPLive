using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.Cms;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{


    public class MonthlyAttendanceShowController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public MonthlyAttendanceShowController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/MonthlyAttendanceShow/Index.cshtml");
        }

        public JsonResult DDlDeptType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code, name from dept_mast where comp_code= " + getdata.PubCompCode + " ORDER  BY NAME";
                var DDlDeptType = _dropdownService.GetDropdownList(query);
                return Json(DDlDeptType);
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadData(int deptcode, int EMP_ID, int DeptCheack, DateTime Vdate)
        {
            try
            {
                var GetGlobalCode = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_MonthlyAttendanceShow", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@V_DATE", Vdate);
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@Comp_Code", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@Year_Code", GetGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@Dept_Code", deptcode != 0 ? deptcode : DBNull.Value);
                        cmd.Parameters.AddWithValue("@DeptCheck", DeptCheack);
                        cmd.Parameters.AddWithValue("@Emp_Id", EMP_ID != 0 ? EMP_ID : DBNull.Value);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();

                            while (await rdr.ReadAsync())
                            {
                                double.TryParse(rdr["AHRS"]?.ToString(), out double AHRS);
                                double.TryParse(rdr["BHRS"]?.ToString(), out double BHRS);
                                double ot = AHRS - BHRS;

                                var result = new
                                {
                                    STATUS = rdr["STATUS"]?.ToString(),
                                    OFFDAY = rdr["OFFDAY"]?.ToString(),
                                    AHRS = AHRS.ToString("0.00"),
                                    BHRS = BHRS.ToString("0.00"),
                                    ot = ot.ToString("0.00"),
                                    V_DATE = rdr["V_DATE"] != DBNull.Value
                                        ? Convert.ToDateTime(rdr["V_DATE"])
                                        : (DateTime?)null,
                                    EMP_CODE = rdr["EMP_CODE"] != DBNull.Value
                                        ? Convert.ToInt32(rdr["EMP_CODE"])
                                        : 0,
                                    SNO = rdr["SNO"] != DBNull.Value
                                        ? Convert.ToInt32(rdr["SNO"])
                                        : 0,
                                    DEPT_NAME = rdr["DEPT_NAME"]?.ToString(),
                                    EMP_NAME = rdr["EMP_NAME"]?.ToString(),
                                    Designation = rdr["Designation"]?.ToString()
                                };

                                results.Add(result);
                            }

                            if (results.Any())
                                return Json(new { success = true, message = "Data fetched successfully", data = results });
                            else
                                return Json(new { success = false, message = "No Data Found" });
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return Json(new { success = false, message = "Database error occurred.", error = sqlEx.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An unexpected error occurred.", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }




        [HttpPost]
        public IActionResult SaveAttendance([FromBody] SaveAttendanceRequest request)
        {
            if (request?.Data == null || !request.Data.Any())
                return null; // or use: return NoContent();

            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                foreach (var entry in request.Data)
                {
                    int vno = 0;

                    string selectVnoQuery = @"SELECT ISNULL(MAX(V_NO), 0) 
                                      FROM PAY_ATTEN 
                                      WHERE v_TYPE = 'ATTN' 
                                        AND EMP_CODE = @EMP_CODE 
                                        AND V_DATE = @V_DATE 
                                        AND COMP_CODE = @COMP_CODE 
                                        AND BRANCH_CODE = @BRANCH_CODE";

                    using (var cmd = new SqlCommand(selectVnoQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE);
                        cmd.Parameters.AddWithValue("@V_DATE", entry.V_DATE);

                        var result = cmd.ExecuteScalar();
                        vno = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
                    }

                    // 2. If V_NO not found, get next global V_NO
                    if (vno == 0)
                    {
                        string nextVnoQuery = @"SELECT ISNULL(MAX(V_NO), 0) + 1 
                                        FROM PAY_ATTEN 
                                        WHERE v_TYPE = 'ATTN' 
                                          AND COMP_CODE = @COMP_CODE 
                                          AND BRANCH_CODE = @BRANCH_CODE 
                                          AND YEAR_CODE = @YEAR_CODE";

                        using (var cmd = new SqlCommand(nextVnoQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);

                            var result = cmd.ExecuteScalar();
                            entry.V_NO = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 1;
                        }
                    }

                    var FDate = entry.V_DATE?.ToString("yyyy-MM-dd") ?? "";

                    string SaveAction = @"SELECT 1 FROM PAY_ATTEN 
                                  WHERE v_TYPE = 'ATTN' 
                                    AND EMP_CODE = @EMP_CODE 
                                    AND V_DATE = '" + FDate + @"' 
                                    AND COMP_CODE = @COMP_CODE 
                                    AND Branch_Code = @BRANCH_CODE 
                                    AND YEAR_CODE = @YEAR_CODE";

                    using (var cmd = new SqlCommand(SaveAction, conn))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_DATE", FDate);
                        cmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE);

                        var results = cmd.ExecuteScalar();
                        int count = (results != null && results != DBNull.Value) ? Convert.ToInt32(results) : 0;

                        request.Action = (count > 0) ? "UPDATE" : "INSERT";
                    }

                    // 4. Save using stored procedure
                    using (var cmd = new SqlCommand("sp_MonthlyAttendanceShow", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@Action", "SaveData");
                        cmd.Parameters.AddWithValue("@SaveAction", request.Action);
                        cmd.Parameters.AddWithValue("@DOC_ID", "ATTN" + entry.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", "ATTN");
                        cmd.Parameters.AddWithValue("@V_NO", entry.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", entry.V_DATE);
                        cmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE);
                        cmd.Parameters.AddWithValue("@SHIFT", entry.SHIFT);
                        cmd.Parameters.AddWithValue("@STATUS", entry.STATUS);
                        cmd.Parameters.AddWithValue("@OFFDAY", entry.OFFDAY);
                        cmd.Parameters.AddWithValue("@REMARK", entry.REMARK);
                        cmd.Parameters.AddWithValue("@SNO", entry.SNO);

                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Attendance saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }




        public class SaveAttendanceRequest
        {
            public string Action { get; set; }
            public List<MonthlyAttendanceShow> Data { get; set; }
        }





    }
}
