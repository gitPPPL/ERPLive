using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.AddAttachmentService;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class DailyLateEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly FileHelper _filehelper;
        public DailyLateEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/Payroll/Transaction/DailyLateEntry/Index.cshtml");
        }
        public IActionResult GetDocTypeList()
        {
            string query = "SELECT * FROM DOCTYPE_MAST WHERE Name like '%Gatepass Late Entry%'";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public int GetNextV_NO(string yearCode)
        {
            string newV_NO = "00000";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // Execute query to get PREFIXYR
                string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = '" + yearCode + "'";
                SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                // Execute query to get last V_NO
                string lastV_NO_Query = "SELECT TOP 1 V_NO FROM PAY_GATEPASS ORDER BY V_NO DESC";
                SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                string lastV_NO = lastVnoCmd.ExecuteScalar()?.ToString();

                int lastNumber = 0;
                if (!string.IsNullOrEmpty(lastV_NO) && lastV_NO.Length >= 9)
                {
                    string numericPart = lastV_NO.Substring(lastV_NO.Length - 5);
                    int.TryParse(numericPart, out lastNumber);
                }

                // Increment and format the new V_NO
                string newRunningNo = (lastNumber + 1).ToString("D5");
                newV_NO = prefixYR + newRunningNo;
            }

            return Convert.ToInt32(newV_NO);
        }

        public IActionResult shiftList(int cCode)
        {
            string query = "SELECT DISTINCT SHIFT AS Code, SHIFT AS Name FROM SHIFT_MAST";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult HodList(int cCode)
        {
            string query = "SELECT DISTINCT EMP_CODE As Code,EMP_NAME As Name FROM [dbo].[PAYGATE_HOD] WHERE COMP_CODE = '" + cCode + "' ORDER BY EMP_NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public IActionResult GetEmployeeNameByCode(string empCode, int compCode)
        {
            string empName = "";

            string query = "SELECT NAME FROM [EMP_MAST] WHERE COMP_CODE = @compCode AND CODE = @empCode AND RESIGN_DATE IS NULL";

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add("@compCode", SqlDbType.Int).Value = compCode;
                    cmd.Parameters.Add("@empCode", SqlDbType.VarChar).Value = empCode;

                    conn.Open();

                    var result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        empName = result.ToString();
                    }
                }
            }

            return Json(empName);
        }
        public IActionResult empList(int cCode)
        {
            string query = "SELECT CODE As Code, NAME as Name FROM [EMP_MAST] WHERE COMP_CODE = '" + cCode + "' AND RESIGN_DATE IS NULL  ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public IActionResult GetGatePassList(DateTime docDate, int docNo, string docType)
        {
            List<PAY_GATEPASS> gatePassList = new List<PAY_GATEPASS>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string query = @"SELECT 
                                    PGP.YEAR_CODE,
                                    PGP.BRANCH_CODE,
                                    PGP.COMP_CODE,
                                    PGP.V_DATE,
                                    PGP.V_TYPE,
                                    PGP.V_NO,
                                    PGP.DOC_ID,
                                    PGP.EMP_CODE,
                                    EM.EMP_NAME,
                                    EM.SHIFT,
                                    PGP.BHRS,
                                    PGP.AHRS,
                                    PGP.IN_TIME,
                                    PGP.OUT_TIME,
                                    PGP.REASON,
                                    PGP.HOD_CODE,
                                    PGP.AUTH_BY,
                                    PGP.DUR,
                                    PGP.REMARK,
                                    PGP.SNO,
                                    PGP.UDATE,
                                    PGP.UUSER,
                                    PGP.EDATE,
                                    PGP.EUSER,
                                    PGP.AED,
                                    PGP.WSID,
                                    PGP.LIP,
                                    PGP.LID,
                                    PGP.MAC_IN,
                                    PGP.MAC_OUT
                                FROM PAY_GATEPASS PGP
                                OUTER APPLY (
                                    SELECT TOP 1 NAME AS EMP_NAME, SHIFT
                                    FROM EMP_MAST EM
                                    WHERE EM.CODE = PGP.EMP_CODE
                                ) EM
                                WHERE 
                                    PGP.V_TYPE = @DocType
                                    AND PGP.V_DATE = @DocDate
                                    AND PGP.V_NO = @DocNo
                                ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DocDate", docDate);
                    cmd.Parameters.AddWithValue("@DocNo", docNo);
                    cmd.Parameters.AddWithValue("@DocType", docType);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PAY_GATEPASS item = new PAY_GATEPASS
                            {
                                YEAR_CODE = reader.GetInt32(reader.GetOrdinal("YEAR_CODE")),
                                COMP_CODE = reader.GetInt32(reader.GetOrdinal("COMP_CODE")),
                                BRANCH_CODE = reader.GetInt32(reader.GetOrdinal("BRANCH_CODE")),
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                V_NO = reader.GetInt32(reader.GetOrdinal("V_NO")),
                                V_DATE = reader.GetDateTime(reader.GetOrdinal("V_DATE")),
                                DOC_ID = reader["DOC_ID"]?.ToString(),
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("EMP_CODE")) : 0,
                                EMP_NAME = reader["EMP_NAME"]?.ToString(),
                                SNO = reader.GetInt32(reader.GetOrdinal("SNO")),
                                AHRS = reader["AHRS"] != DBNull.Value ? reader.GetDecimal(reader.GetOrdinal("AHRS")) : 0,
                                BHRS = reader["BHRS"] != DBNull.Value ? reader.GetDecimal(reader.GetOrdinal("BHRS")) : 0,
                                REMARK = reader["REMARK"]?.ToString(),
                                IN_TIME = reader["IN_TIME"]?.ToString(),
                                OUT_TIME = reader["OUT_TIME"]?.ToString(),
                                REASON = reader["REASON"]?.ToString(),
                                AUTH_BY = reader["AUTH_BY"]?.ToString(),
                                HOD_CODE = reader["HOD_CODE"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("HOD_CODE")) : 0,
                                DUR = reader["DUR"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("DUR")) : 0,
                                UUSER = reader["UUSER"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("UUSER")) : 0,
                                UDATE = reader["UDATE"] != DBNull.Value ? reader.GetDateTime(reader.GetOrdinal("UDATE")) : DateTime.MinValue,
                                EUSER = reader["EUSER"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("EUSER")) : 0,
                                EDATE = reader["EDATE"] != DBNull.Value ? reader.GetDateTime(reader.GetOrdinal("EDATE")) : DateTime.MinValue,
                                SHIFT = reader["SHIFT"]?.ToString(),
                                AED = reader["AED"]?.ToString(),
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString(),
                                MAC_IN = reader["MAC_IN"]?.ToString(),
                                MAC_OUT = reader["MAC_OUT"]?.ToString()
                            };

                            gatePassList.Add(item);
                        }
                    }
                }
            }

            return Json(gatePassList);
        }

        [HttpPost]
        public IActionResult SaveGatePassList([FromBody] List<PAY_GATEPASS> entries)
        {
            if (entries == null || entries.Count == 0)
                return BadRequest("No data to save.");

            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        var firstEntry = entries.First();

                        string deleteQuery = @"
                    DELETE FROM PAY_GATEPASS 
                    WHERE V_NO = @V_NO AND V_DATE = @V_DATE AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE
                    AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

                        using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, con, tran))
                        {
                            deleteCmd.Parameters.AddWithValue("@V_NO", entries.FirstOrDefault().V_NO);
                            deleteCmd.Parameters.AddWithValue("@V_DATE", (object)firstEntry.V_DATE ?? DBNull.Value);
                            deleteCmd.Parameters.AddWithValue("@V_TYPE", (object)firstEntry.V_TYPE ?? DBNull.Value);
                            deleteCmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            deleteCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                            deleteCmd.ExecuteNonQuery();
                        }

                        int snoCounter = 1;

                        foreach (var item in entries)
                        {
                            var deptData = GetDepartmentNameCodeBy(item.EMP_CODE);
                            item.V_NO = entries.FirstOrDefault().V_NO;
                            item.SNO = snoCounter++;

                            string insertQuery = @"
                    INSERT INTO PAY_GATEPASS (
                        YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DOC_ID,
                        EMP_CODE, EMP_NAME, SNO, DEPT_CODE, DEPT_NAME,
                        WORKPLACE_NAME, WORKPLACE_CODE, AHRS, BHRS, REMARK, DUTY_TIME,
                        IN_TIME, OUT_TIME, SYS_TIME, REASON, REASON_CODE, AUTH_BY,
                        GP_NO, GATE_NO, HOD_CODE, DUR, REF_TYPE, REF_NO, RETUN, COND,
                        APROV_STATUS, APROV_REMARKS, FAPROV_STATUS, FAPROV_REMARKS,
                        UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID,
                        MAC_IN, MAC_OUT, DESG_CODE, DESG_NAME, REQ_NOS, PRESENT_NOS
                    )
                    VALUES (
                        @YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO, @V_DATE, @DOC_ID,
                        @EMP_CODE, @EMP_NAME, @SNO, @DEPT_CODE, @DEPT_NAME,
                        @WORKPLACE_NAME, @WORKPLACE_CODE, @AHRS, @BHRS, @REMARK, @DUTY_TIME,
                        @IN_TIME, @OUT_TIME, @SYS_TIME, @REASON, @REASON_CODE, @AUTH_BY,
                        @GP_NO, @GATE_NO, @HOD_CODE, @DUR, @REF_TYPE, @REF_NO, @RETUN, @COND,
                        @APROV_STATUS, @APROV_REMARKS, @FAPROV_STATUS, @FAPROV_REMARKS,
                        @UUSER, @UDATE, @EUSER, @EDATE, @AED, @WSID, @LIP, @LID,
                        @MAC_IN, @MAC_OUT, @DESG_CODE, @DESG_NAME, @REQ_NOS, @PRESENT_NOS
                    )";

                            using (SqlCommand cmd = new SqlCommand(insertQuery, con, tran))
                            {
                                object DBNullIfNull(object v) => v ?? DBNull.Value;

                                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                                cmd.Parameters.AddWithValue("@V_TYPE", DBNullIfNull(item.V_TYPE));
                                cmd.Parameters.AddWithValue("@V_NO", item.V_NO);
                                cmd.Parameters.AddWithValue("@V_DATE", DBNullIfNull(item.V_DATE));
                                cmd.Parameters.AddWithValue("@DOC_ID", item.V_TYPE + item.V_NO);

                                cmd.Parameters.AddWithValue("@EMP_CODE", DBNullIfNull(item.EMP_CODE));
                                cmd.Parameters.AddWithValue("@EMP_NAME", DBNullIfNull(item.EMP_NAME));
                                cmd.Parameters.AddWithValue("@SNO", item.SNO);

                                cmd.Parameters.AddWithValue("@DEPT_CODE", DBNullIfNull(deptData.DeptCode));
                                cmd.Parameters.AddWithValue("@DEPT_NAME", DBNullIfNull(deptData.DeptName));

                                cmd.Parameters.AddWithValue("@WORKPLACE_NAME", DBNullIfNull(item.WORKPLACE_NAME));
                                cmd.Parameters.AddWithValue("@WORKPLACE_CODE", DBNullIfNull(item.WORKPLACE_CODE));
                                cmd.Parameters.AddWithValue("@AHRS", DBNullIfNull(item.AHRS));
                                cmd.Parameters.AddWithValue("@BHRS", DBNullIfNull(item.BHRS));
                                cmd.Parameters.AddWithValue("@REMARK", DBNullIfNull(item.REMARK));
                                cmd.Parameters.AddWithValue("@DUTY_TIME", DBNullIfNull(item.DUTY_TIME));
                                cmd.Parameters.AddWithValue("@IN_TIME", DBNullIfNull(item.IN_TIME));
                                cmd.Parameters.AddWithValue("@OUT_TIME", DBNullIfNull(item.OUT_TIME));
                                cmd.Parameters.AddWithValue("@SYS_TIME", DBNullIfNull(item.SYS_TIME));
                                cmd.Parameters.AddWithValue("@REASON", DBNullIfNull(item.REASON));
                                cmd.Parameters.AddWithValue("@REASON_CODE", DBNullIfNull(item.REASON_CODE));
                                cmd.Parameters.AddWithValue("@AUTH_BY", DBNullIfNull(item.AUTH_BY));
                                cmd.Parameters.AddWithValue("@GP_NO", DBNullIfNull(item.GP_NO));
                                cmd.Parameters.AddWithValue("@GATE_NO", DBNullIfNull(item.GATE_NO));
                                cmd.Parameters.AddWithValue("@HOD_CODE", DBNullIfNull(item.HOD_CODE));
                                cmd.Parameters.AddWithValue("@DUR", DBNullIfNull(item.DUR));
                                cmd.Parameters.AddWithValue("@REF_TYPE", DBNullIfNull(item.REF_TYPE));
                                cmd.Parameters.AddWithValue("@REF_NO", DBNullIfNull(item.REF_NO));
                                cmd.Parameters.AddWithValue("@RETUN", DBNullIfNull(item.RETUN));
                                cmd.Parameters.AddWithValue("@COND", DBNullIfNull(item.COND));
                                cmd.Parameters.AddWithValue("@APROV_STATUS", DBNullIfNull(item.APROV_STATUS));
                                cmd.Parameters.AddWithValue("@APROV_REMARKS", DBNullIfNull(item.APROV_REMARKS));
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", DBNullIfNull(item.FAPROV_STATUS));
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", DBNullIfNull(item.FAPROV_REMARKS));

                                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@AED", DBNullIfNull(item.AED ?? "A"));
                                cmd.Parameters.AddWithValue("@WSID", DBNullIfNull(globalVar.PubWorkStationID ?? "WEB"));
                                cmd.Parameters.AddWithValue("@LIP", DBNullIfNull(globalVar.PubLocalId ?? "127.0.0.1"));
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                cmd.Parameters.AddWithValue("@MAC_IN", DBNullIfNull(item.MAC_IN));
                                cmd.Parameters.AddWithValue("@MAC_OUT", DBNullIfNull(item.MAC_OUT));
                                cmd.Parameters.AddWithValue("@DESG_CODE", DBNullIfNull(item.DESG_CODE));
                                cmd.Parameters.AddWithValue("@DESG_NAME", DBNullIfNull(item.DESG_NAME));
                                cmd.Parameters.AddWithValue("@REQ_NOS", DBNullIfNull(item.REQ_NOS));
                                cmd.Parameters.AddWithValue("@PRESENT_NOS", DBNullIfNull(item.PRESENT_NOS));

                                cmd.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        return Ok(new { Message = "Gate pass entries saved successfully."});
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return StatusCode(500, $"Failed to save gate pass entries: {ex.Message}");
                    }
                }
            }
        }


        //[HttpPost]
        //public IActionResult SaveGatePassList([FromBody] List<PAY_GATEPASS> entries)
        //{
        //    if (entries == null || entries.Count == 0)
        //        return BadRequest("No data to save.");

        //    var globalVar = _globalVariableService.GetGlobalVariables();

        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        con.Open();
        //        using (SqlTransaction tran = con.BeginTransaction())
        //        {
        //            try
        //            {
        //                int? vno = entries.FirstOrDefault()?.V_NO > 0
        //                    ? entries.First().V_NO
        //                    : GetNextV_NO(globalVar.PubFYearCode);

        //                int snoCounter = 1;

        //                foreach (var item in entries)
        //                {
        //                    var deptData = GetDepartmentNameCodeBy(item.EMP_CODE);
        //                    item.V_NO = vno;
        //                    item.SNO = snoCounter++;

        //                    string insertQuery = @"
        //                INSERT INTO PAY_GATEPASS (
        //                    YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DOC_ID,
        //                    EMP_CODE, EMP_NAME, SNO, DEPT_CODE, DEPT_NAME,
        //                    WORKPLACE_NAME, WORKPLACE_CODE, AHRS, BHRS, REMARK, DUTY_TIME,
        //                    IN_TIME, OUT_TIME, SYS_TIME, REASON, REASON_CODE, AUTH_BY,
        //                    GP_NO, GATE_NO, HOD_CODE, DUR, REF_TYPE, REF_NO, RETUN, COND,
        //                    APROV_STATUS, APROV_REMARKS, FAPROV_STATUS, FAPROV_REMARKS,
        //                    UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID,
        //                    MAC_IN, MAC_OUT, DESG_CODE, DESG_NAME, REQ_NOS, PRESENT_NOS
        //                )
        //                VALUES (
        //                    @YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO, @V_DATE, @DOC_ID,
        //                    @EMP_CODE, @EMP_NAME, @SNO, @DEPT_CODE, @DEPT_NAME,
        //                    @WORKPLACE_NAME, @WORKPLACE_CODE, @AHRS, @BHRS, @REMARK, @DUTY_TIME,
        //                    @IN_TIME, @OUT_TIME, @SYS_TIME, @REASON, @REASON_CODE, @AUTH_BY,
        //                    @GP_NO, @GATE_NO, @HOD_CODE, @DUR, @REF_TYPE, @REF_NO, @RETUN, @COND,
        //                    @APROV_STATUS, @APROV_REMARKS, @FAPROV_STATUS, @FAPROV_REMARKS,
        //                    @UUSER, @UDATE, @EUSER, @EDATE, @AED, @WSID, @LIP, @LID,
        //                    @MAC_IN, @MAC_OUT, @DESG_CODE, @DESG_NAME, @REQ_NOS, @PRESENT_NOS
        //                )";

        //                    using (SqlCommand cmd = new SqlCommand(insertQuery, con, tran))
        //                    {
        //                        object DBNullIfNull(object v) => v ?? DBNull.Value;

        //                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
        //                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

        //                        cmd.Parameters.AddWithValue("@V_TYPE", DBNullIfNull(item.V_TYPE));
        //                        cmd.Parameters.AddWithValue("@V_NO", item.V_NO);
        //                        cmd.Parameters.AddWithValue("@V_DATE", DBNullIfNull(item.V_DATE));
        //                        cmd.Parameters.AddWithValue("@DOC_ID", item.V_TYPE + item.V_NO);

        //                        cmd.Parameters.AddWithValue("@EMP_CODE", DBNullIfNull(item.EMP_CODE));
        //                        cmd.Parameters.AddWithValue("@EMP_NAME", DBNullIfNull(item.EMP_NAME));
        //                        cmd.Parameters.AddWithValue("@SNO", item.SNO);

        //                        cmd.Parameters.AddWithValue("@DEPT_CODE", DBNullIfNull(deptData.DeptCode));
        //                        cmd.Parameters.AddWithValue("@DEPT_NAME", DBNullIfNull(deptData.DeptName));

        //                        cmd.Parameters.AddWithValue("@WORKPLACE_NAME", DBNullIfNull(item.WORKPLACE_NAME));
        //                        cmd.Parameters.AddWithValue("@WORKPLACE_CODE", DBNullIfNull(item.WORKPLACE_CODE));
        //                        cmd.Parameters.AddWithValue("@AHRS", DBNullIfNull(item.AHRS));
        //                        cmd.Parameters.AddWithValue("@BHRS", DBNullIfNull(item.BHRS));
        //                        cmd.Parameters.AddWithValue("@REMARK", DBNullIfNull(item.REMARK));
        //                        cmd.Parameters.AddWithValue("@DUTY_TIME", DBNullIfNull(item.DUTY_TIME));
        //                        cmd.Parameters.AddWithValue("@IN_TIME", DBNullIfNull(item.IN_TIME));
        //                        cmd.Parameters.AddWithValue("@OUT_TIME", DBNullIfNull(item.OUT_TIME));
        //                        cmd.Parameters.AddWithValue("@SYS_TIME", DBNullIfNull(item.SYS_TIME));
        //                        cmd.Parameters.AddWithValue("@REASON", DBNullIfNull(item.REASON));
        //                        cmd.Parameters.AddWithValue("@REASON_CODE", DBNullIfNull(item.REASON_CODE));
        //                        cmd.Parameters.AddWithValue("@AUTH_BY", DBNullIfNull(item.AUTH_BY));
        //                        cmd.Parameters.AddWithValue("@GP_NO", DBNullIfNull(item.GP_NO));
        //                        cmd.Parameters.AddWithValue("@GATE_NO", DBNullIfNull(item.GATE_NO));
        //                        cmd.Parameters.AddWithValue("@HOD_CODE", DBNullIfNull(item.HOD_CODE));
        //                        cmd.Parameters.AddWithValue("@DUR", DBNullIfNull(item.DUR));
        //                        cmd.Parameters.AddWithValue("@REF_TYPE", DBNullIfNull(item.REF_TYPE));
        //                        cmd.Parameters.AddWithValue("@REF_NO", DBNullIfNull(item.REF_NO));
        //                        cmd.Parameters.AddWithValue("@RETUN", DBNullIfNull(item.RETUN));
        //                        cmd.Parameters.AddWithValue("@COND", DBNullIfNull(item.COND));
        //                        cmd.Parameters.AddWithValue("@APROV_STATUS", DBNullIfNull(item.APROV_STATUS));
        //                        cmd.Parameters.AddWithValue("@APROV_REMARKS", DBNullIfNull(item.APROV_REMARKS));
        //                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", DBNullIfNull(item.FAPROV_STATUS));
        //                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", DBNullIfNull(item.FAPROV_REMARKS));

        //                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
        //                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
        //                        cmd.Parameters.AddWithValue("@AED", DBNullIfNull(item.AED ?? "A"));
        //                        cmd.Parameters.AddWithValue("@WSID", DBNullIfNull(globalVar.PubWorkStationID ?? "WEB"));
        //                        cmd.Parameters.AddWithValue("@LIP", DBNullIfNull(globalVar.PubLocalId ?? "127.0.0.1"));
        //                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

        //                        cmd.Parameters.AddWithValue("@MAC_IN", DBNullIfNull(item.MAC_IN));
        //                        cmd.Parameters.AddWithValue("@MAC_OUT", DBNullIfNull(item.MAC_OUT));
        //                        cmd.Parameters.AddWithValue("@DESG_CODE", DBNullIfNull(item.DESG_CODE));
        //                        cmd.Parameters.AddWithValue("@DESG_NAME", DBNullIfNull(item.DESG_NAME));
        //                        cmd.Parameters.AddWithValue("@REQ_NOS", DBNullIfNull(item.REQ_NOS));
        //                        cmd.Parameters.AddWithValue("@PRESENT_NOS", DBNullIfNull(item.PRESENT_NOS));

        //                        cmd.ExecuteNonQuery();
        //                    }
        //                }

        //                tran.Commit();
        //                return Ok(new { Message = "Gate pass entries saved successfully.", V_NO = vno });
        //            }
        //            catch (Exception ex)
        //            {
        //                tran.Rollback();
        //                return StatusCode(500, $"Failed to save gate pass entries: {ex.Message}");
        //            }
        //        }
        //    }
        //}

        [HttpGet]
        public DepartmentInfoDto GetDepartmentNameCodeBy(int? EMPCODE)
        {
            var result = new DepartmentInfoDto
            {
                DeptCode = 0,
                DeptName = ""
            };

            var globalVar = _globalVariableService.GetGlobalVariables();
            var query = @"
        SELECT TOP(1) EM.DEPT_CODE, DM.NAME 
        FROM EMP_MAST EM 
        RIGHT JOIN DEPT_MAST DM ON DM.CODE = EM.DEPT_CODE 
        WHERE EM.CODE = @EMP_CODE AND EM.COMP_CODE = @COMP_CODE";

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EMP_CODE", EMPCODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode ?? (object)DBNull.Value);

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())  
                        {
                            result.DeptCode = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0;
                            result.DeptName = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : "";
                        }
                    }
                }
            }

            return result;
        }


    }
}