using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Models.GateEntry.Transaction;
using static travelexpensemanagement.ModuleService.ModuleService;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class ExtraDutyEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ExtraDutyEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/ExtraDutyEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetDocumentTypeList()
        
        {
            string query = "SELECT CODE, NAME FROM DOCTYPE_MAST  where doctype= 'paygatepas' AND CODE = 'GTED' ORDER BY NAME ASC";
            var docTypeList = _dropdownService.GetDropdownList(query);
            return Json(new { status = "success", data = docTypeList });
        }

        [HttpGet]
        public IActionResult GetDepartmentList()
        {
            var varibales = _globalVariableService.GetGlobalVariables();
            string query = "Select Code,Name from DEPT_MAST where COMP_CODE=" + varibales.PubCompCode + " and Active=1 order by Name ";
            var docTypeList = _dropdownService.GetDropdownList(query);
            return Json(new { status = "success", data = docTypeList });

        }

        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            try
            {
                var userSession = _globalVariableService.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = "1";
                var vType = V_type;
                var tableName = "PAY_GATEPASS";

                var yearParams = new Dictionary<string, object> { { "@YearCd", yearCode } };
                var vnoParams = new Dictionary<string, object>
                {
                    { "@COMP_CODE", companyCode },
                    { "@BRANCH_CODE", branchCode },
                    { "@YEAR_CODE", yearCode },
                    { "@V_TYPE", vType },
                    { "@TableName", tableName }
                };

                string nextVNo = await _dbHelper.GetExecuteScalarAsync<string>("sp_GetMaxVNo", vnoParams, isStoredProc: true);
                string year = await _dbHelper.GetExecuteScalarAsync<string>("SELECT dbo.fn_GetCurrentYear(@YearCd)", yearParams);
                var docId = (vType) + (year) + (nextVNo);
                var newVno = year + nextVNo;
                var docIdNoList = new { DocId = docId, VNo = newVno };
                return Json(new { status = true, data = docIdNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }


        }


        [HttpGet]
        public IActionResult GetEmpList()
        {
            var varibales = _globalVariableService.GetGlobalVariables();
            string query = " SELECT CODE,NAME, DEPT_CODE, DESG_CODE FROM EMP_MAST where COMP_CODE=" + varibales.PubCompCode + " AND ACTIVE=1  AND RESIGN_DATE IS NULL ORDER BY NAME ";
            var docEmpList = _dropdownService.GetEmpdataList(query);

            return Json(new { status = "success", data = docEmpList });
        }


        [HttpGet]
        public IActionResult GetDesglist()
        {
            var varibales = _globalVariableService.GetGlobalVariables();
            string query = " SELECT CODE,NAME FROM DESG_MAST where COMP_CODE=" + varibales.PubCompCode + " AND ACTIVE=1   ORDER BY NAME ";
            var docTypeList = _dropdownService.GetDropdownList(query);
            return Json(new { status = "success", data = docTypeList });
        }



        [HttpGet]
        public IActionResult GetReasonlist(string vtype)
        {
            var varibales = _globalVariableService.GetGlobalVariables();
            string query = " SELECT  CODE,NAME, DEDUCT_TYPE FROM PAY_REASON_MAST where REASON_TYPE = 'GatePass' ";
            if (vtype == "GTED")
            {
                query += " and code in (4,13,14)";
            }
            else if (vtype == "GTLE")
            {
                query += " and code in (3,10)";
            }
            query += "group by NAME,code,DEDUCT_TYPE  order by name ";

            var docTypeList = _dropdownService.GetEmpReasonList(query);
            return Json(new { status = "success", data = docTypeList });
        }

        //Select DISTINCT EMP_CODE,EMP_NAME ,CONVERT(VARCHAR,EMP_CODE)+EMP_NAME [CODENM] from PAYGATE_HOD a left join emp_mast b on a.emp_code=b.code and a.COMP_CODE=b.COMP_CODE WHERE a.DEPT_CODE=(select dept_code from emp_mast where code= " & Val(dataNulltoEmpty(MyDGV1.CurrentRow.Cells(1).Value)) & " and comp_code=" & pubCompCode & ") AND ALLOW='Y' AND a.COMP_CODE= " & pubCompCode & " and b.RESIGN_DATE is null 

        [HttpGet]
        public IActionResult GetHODlist(int deptCode)
        {
            var varibales = _globalVariableService.GetGlobalVariables();
            string query = "Select DISTINCT EMP_CODE,EMP_NAME  from PAYGATE_HOD a " +
                " left join emp_mast b on a.emp_code=b.code and a.COMP_CODE=b.COMP_CODE WHERE a.DEPT_CODE=" + deptCode +
                " AND ALLOW='Y' AND a.COMP_CODE= " + varibales.PubCompCode + " and b.RESIGN_DATE is null  ";

            var itemList = _dropdownService.GetDropdownList(query);
            return Json(new { status = "success", data = itemList });
        }


        [HttpPost]
        public IActionResult SaveExtraDuty([FromBody] ExtraDutyEntryModel request)
        {
            if (request?.header.Action == null)
            {
                // Log the error and return a response if the Header is null
                return Json(new { success = false, message = "Input model is null" });
            }

            var action = request.header.Action == "INSERT" ? "INSERT" : "UPDATE";
            var resultTask = SubmitRequest(request.header, request.details.TableData, action);

            // Fix: Await the Task<string> result before comparing it to "Success"
            var result = resultTask.Result;

            return result == "Success"
                ? Json(new { success = true })
                : Json(new { success = false, message = result });
        }

        private async Task<string> SubmitRequest(ExtraDutyEntryModel.Header header, List<ExtraDutyEntryModel.TableRow>? details, string action)
        {
            SqlTransaction tran;
            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                string fappstatus = "", fappRemark = "";
                bool isApprovalBody = false;
                bool isFinalApprovalBody = false;

                if (await _dbHelper.IsDataExist("select 1 from DOC_APPROSTAGE where USER_CODE=" + g.PubUserId + " and DOC_CODE='" + header.DocType +
                                          "' and comp_code=" + g.PubCompCode))
                {
                    isApprovalBody = true;
                }

                string approvalUser = await _dbHelper.ExecuteScalarAsync("select APPROV_USER from DOC_APPROSTAGE where USER_CODE=" +
                            Convert.ToInt32(g.PubUserId) +
                            " and DOC_CODE='" + header.DocType + "' and comp_code=" + g.PubCompCode);

                if (approvalUser == "FINAL")
                {
                    isFinalApprovalBody = true;
                }

                if (isFinalApprovalBody == true)
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                }

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                tran = conn.BeginTransaction();

                string delsql = @"DELETE FROM Pay_GatePass WHERE V_no=@V_no  AND comp_code=@comp_code AND v_type=@V_TYPE 
                                 AND Year_code=@year_code AND branch_code=@branch_code";

                using (SqlCommand pubCmd = new SqlCommand(delsql, conn, tran))
                {
                    pubCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    pubCmd.Parameters.AddWithValue("@year_code", g.PubFYearCode);
                    pubCmd.Parameters.AddWithValue("@branch_code", 1);
                    pubCmd.Parameters.AddWithValue("@V_no", header.Vno);
                    pubCmd.Parameters.AddWithValue("@v_TYPE", header.DocType);
                    pubCmd.ExecuteNonQuery();
                }

                int counter = 0;

                if (details == null || details.Count == 0)
                    return "Error: No details provided";

                int index = 0;
                foreach (ExtraDutyEntryModel.TableRow row in details)
                {
                    if (row.EmpName == "") continue;

                    if (Convert.ToInt32(row.EmpCode) > 0)
                    {
                        string sql = @"INSERT INTO pay_gatepass 
                        (DOC_ID,YEAR_CODE,BRANCH_CODE,COMP_CODE,V_TYPE,V_NO,V_DATE,EMP_CODE,EMP_NAME,
                         AHRS,BHRS,DUTY_TIME,REQ_NOS,PRESENT_NOS,IN_TIME,OUT_TIME,
                         REASON,AUTH_BY,APROV_STATUS,APROV_REMARKS,REMARK,REASON_CODE,HOD_CODE,
                         FAPROV_STATUS,FAPROV_REMARKS,SNO,REF_TYPE,REF_NO,GATE_NO,
                         DEPT_NAME,DEPT_CODE,DESG_NAME,DESG_CODE, ";

                        if (action == "INSERT")
                            sql += "UUSER, UDATE,";
                        else if (action == "UPDATE")
                            sql += "EUSER, EDATE,";

                        sql += @" AED,WSID,LIP,LID,Dur,Sys_Time)
                            VALUES (@DOC_ID,@YEAR_CODE,@BRANCH_CODE,@COMP_CODE,@V_TYPE,@V_NO,@V_DATE,@EMP_CODE,@EMP_NAME,
                          @AHRS,@BHRS,@DUTY_TIME,@REQ_NOS,@PRESENT_NOS,
                          @IN_TIME,@OUT_TIME,@REASON,@AUTH_BY,@APROV_STATUS,@APROV_REMARKS,
                          @REMARK,@REASON_CODE,@HOD_CODE,@FAPROV_STATUS,@FAPROV_REMARKS,@SNO,
                          @REF_TYPE,@REF_NO,@GATE_NO,@DEPT_NAME,@DEPT_CODE,@DESG_NAME,@DESG_CODE,
                          " + g.PubUserId + ", format(getdate(),'yyyy-MM-dd HH:mm'),@AED,@WSID,@LIP,@LID,@Dur,@Sys_Time)";

                        using (SqlCommand pubCmd = new SqlCommand(sql, conn, tran))
                        {
                            pubCmd.Parameters.AddWithValue("@DOC_ID", header.DocType + header.Vno);
                            pubCmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                            pubCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                            pubCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            pubCmd.Parameters.AddWithValue("@V_TYPE", header.DocType);
                            pubCmd.Parameters.AddWithValue("@V_NO", header.Vno);
                            pubCmd.Parameters.AddWithValue("@V_DATE", header.DocDate);

                            pubCmd.Parameters.AddWithValue("@EMP_CODE", Convert.ToInt32(row.EmpCode));
                            pubCmd.Parameters.AddWithValue("@EMP_NAME", Convert.ToString(row.EmpName));

                            pubCmd.Parameters.AddWithValue("@BHRS", row.Before == null ? (object)DBNull.Value : Convert.ToDecimal("0"+row.Before));
                            pubCmd.Parameters.AddWithValue("@AHRS", row.After == null ? (object)DBNull.Value : Convert.ToDecimal("0"+row.After));

                            pubCmd.Parameters.AddWithValue("@DUTY_TIME", Convert.ToString(row.Shift));
                            
                            pubCmd.Parameters.AddWithValue("@REQ_NOS", Convert.ToDecimal("0" + row.Required));
                            pubCmd.Parameters.AddWithValue("@PRESENT_NOS", Convert.ToDecimal("0" + row.Present));

                            pubCmd.Parameters.AddWithValue("@IN_TIME", string.IsNullOrEmpty(Convert.ToString(row.InTime)) ? (object)DBNull.Value : row.InTime);
                            pubCmd.Parameters.AddWithValue("@OUT_TIME", string.IsNullOrEmpty(Convert.ToString(row.OutTime)) ? (object)DBNull.Value : row.OutTime);

                            pubCmd.Parameters.AddWithValue("@REASON", Convert.ToString(row.Reason));

                            if (Convert.ToInt32("0" + row.HodName) != 0)
                            {
                                string AuthName = await _dbHelper.ExecuteScalarAsync("select DESG_MAST.NAME from emp_mast " +
                                                        "left join DESG_MAST on DESG_MAST.CODE = EMP_MAST.DESG_CODE " +
                                                        "and DESG_MAST.COMP_CODE = EMP_MAST.COMP_CODE " +
                                                        "where emp_mast.CODE = " + Convert.ToInt32("0" + row.HodName) +
                                                        " and emp_mast.comp_code=" + g.PubCompCode);

                                pubCmd.Parameters.AddWithValue("@AUTH_BY", AuthName);
                            }
                            else
                            {
                                pubCmd.Parameters.AddWithValue("@AUTH_BY", Convert.ToString(row.AuthBy));
                            }

                            pubCmd.Parameters.AddWithValue("@APROV_STATUS", Convert.ToString(row.Approval));
                            pubCmd.Parameters.AddWithValue("@APROV_REMARKS", Convert.ToString(row.ApprovalRemarks));
                            pubCmd.Parameters.AddWithValue("@REMARK", Convert.ToString(row.Remarks));
                            pubCmd.Parameters.AddWithValue("@REASON_CODE", Convert.ToInt32("0" + row.Reason));
                            pubCmd.Parameters.AddWithValue("@HOD_CODE", Convert.ToInt32("0" + row.HodName));
                            pubCmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                            pubCmd.Parameters.AddWithValue("@FAPROV_REMARKS", fappRemark);
                            pubCmd.Parameters.AddWithValue("@REF_TYPE", Convert.ToString(row.RefType));
                            pubCmd.Parameters.AddWithValue("@REF_NO", Convert.ToInt32("0" + row.RefNo));
                            pubCmd.Parameters.AddWithValue("@GATE_NO", Convert.ToInt32("0" + row.GateNo));
                            pubCmd.Parameters.AddWithValue("@DEPT_NAME", Convert.ToString(row.Department));
                            pubCmd.Parameters.AddWithValue("@DEPT_CODE", Convert.ToInt32("0" + row.Department));
                            pubCmd.Parameters.AddWithValue("@DESG_NAME", Convert.ToString(row.Designation));
                            pubCmd.Parameters.AddWithValue("@DESG_CODE", Convert.ToInt32("0" + row.Designation));
                            pubCmd.Parameters.AddWithValue("@SNO", 1 + index);

                            pubCmd.Parameters.AddWithValue("@AED", row.Action == 0 ? "A" : "E");
                            pubCmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                            pubCmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                            pubCmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            pubCmd.Parameters.AddWithValue("@DUR", row.Duration ?? (object)DBNull.Value);

                            if (Convert.ToInt32(row.Reason) == 4 && string.IsNullOrEmpty(Convert.ToString(row.MacTime)))
                            {
                                string st = await _dbHelper.ExecuteScalarAsync("SELECT ISNULL(STATUS,'' ) FROM PAY_ATTEN " +
                                                    "WHERE EMP_CODE = " + Convert.ToInt32(row.EmpCode) +
                                                    " AND V_DATE='" + header.DocDate + "'" +
                                                    " AND COMP_CODE=" + g.PubCompCode +
                                                    " AND STATUS ='P' AND BRANCH_CODE=1 ");
                                pubCmd.Parameters.AddWithValue("@SYS_TIME", st);
                            }
                            else
                            {
                                pubCmd.Parameters.AddWithValue("@SYS_TIME", Convert.ToString(row.MacTime));
                            }

                            if (pubCmd.ExecuteNonQuery() > 0)
                                counter++;
                            else
                                counter = 0;
                        }
                    }
                    index++;
                }

                // commit or rollback
                if (counter > 0)
                {
                    tran.Commit();
                    // ... (rest of approval_status update + logging logic goes here, convert similar to above)
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        //GetExtradutyDatabyId

        [HttpGet]
        public IActionResult GetExtradutyDatabyId(int DocNo)
        {
            ExtraDutyEntryModel extraDuty = new ExtraDutyEntryModel();
            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                string query = "SELECT  YEAR_CODE,COMP_CODE,BRANCH_CODE, V_TYPE,  V_NO,V_DATE, DOC_ID, EMP_CODE, EMP_NAME, SNO," +
                              " DEPT_CODE,DEPT_NAME, WORKPLACE_NAME, WORKPLACE_CODE,  AHRS, BHRS, REMARK, DUTY_TIME, IN_TIME,  OUT_TIME," +
                              " SYS_TIME, REASON,REASON_CODE,  AUTH_BY,  GP_NO, GATE_NO, HOD_CODE,  DUR, REF_TYPE, REF_NO, RETUN,  COND, " +
                              " APROV_STATUS, APROV_REMARKS, FAPROV_STATUS, FAPROV_REMARKS,  MAC_IN, MAC_OUT, DESG_CODE, DESG_NAME, REQ_NOS," +
                              " PRESENT_NOS FROM  PAY_GATEPASS  A WHERE A.V_TYPE = 'GTED'   AND A.V_NO = " + DocNo + "  AND A.COMP_CODE  =" + g.PubCompCode +
                              " AND A.YEAR_CODE = " + g.PubFYearCode + "   ";

                SqlConnection conn = _dbConnection.GetErpConnection();
                conn.Open();
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = query;
                    SqlDataReader reader = command.ExecuteReader();

                    int index = 0;
                    List<ExtraDutyEntryModel.TableRow> tableDataList = new List<ExtraDutyEntryModel.TableRow>();

                    while (reader.Read())
                    {
                        if (index == 0) // Populate header only once
                        {
                            extraDuty.header = new ExtraDutyEntryModel.Header
                            {
                                DocType = reader["V_TYPE"]?.ToString(),
                                Vno = reader["V_NO"] != DBNull.Value ? Convert.ToString(reader["V_NO"]) : "",
                                DocDate = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd") : "",
                                Action = "UPDATE"
                            };
                        }

                        tableDataList.Add(new ExtraDutyEntryModel.TableRow()
                        {
                            EmpCode = reader["EMP_CODE"] != DBNull.Value ? Convert.ToString(reader["EMP_CODE"]) : "",
                            EmpName = reader["EMP_NAME"]?.ToString() ?? "",
                            DeptCode = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToString(reader["DEPT_CODE"]) : "",
                            Department = reader["DEPT_NAME"]?.ToString() ?? "",
                            DesgCode = reader["DESG_CODE"] != DBNull.Value ? Convert.ToString(reader["DESG_CODE"]) : "",
                            Designation = reader["DESG_NAME"]?.ToString() ?? "",
                            Before = reader["BHRS"] != DBNull.Value ? Convert.ToString(reader["BHRS"]) : (string?)null,
                            After = reader["AHRS"] != DBNull.Value ? Convert.ToString(reader["AHRS"]) : (string?)null,
                            Shift = reader["DUTY_TIME"]?.ToString() ?? "",
                            Duration = reader["DUR"]?.ToString() ?? "",
                            Required = reader["REQ_NOS"] != DBNull.Value ? Convert.ToString(reader["REQ_NOS"]) : "0",
                            Present = reader["PRESENT_NOS"] != DBNull.Value ? Convert.ToString(reader["PRESENT_NOS"]) : "0",
                            InTime = reader["IN_TIME"] != DBNull.Value ? Convert.ToDateTime(reader["IN_TIME"]).ToString("HH:mm") : "",
                            OutTime = reader["OUT_TIME"] != DBNull.Value ? Convert.ToDateTime(reader["OUT_TIME"]).ToString("HH:mm") : "",
                            Reason = reader["REASON_CODE"]?.ToString() ?? "",
                            AuthBy = reader["AUTH_BY"]?.ToString() ?? "",
                            HodName = reader["HOD_CODE"] != DBNull.Value ? Convert.ToString(reader["HOD_CODE"]) : "",
                            Approval = reader["APROV_STATUS"]?.ToString() ?? "",
                            ApprovalRemarks = reader["APROV_REMARKS"]?.ToString() ?? "",
                            Remarks = reader["REMARK"]?.ToString() ?? "",
                            RefType = reader["REF_TYPE"]?.ToString() ?? "",
                            RefNo = reader["REF_NO"] != DBNull.Value ? Convert.ToString(reader["REF_NO"]) : "",
                            GateNo = reader["GATE_NO"] != DBNull.Value ? Convert.ToString(reader["GATE_NO"]) : "",
                            MacTime = reader["SYS_TIME"]?.ToString() ?? "",
                            Action = 0 // Default action for existing records
                        });

                        index++;

                    }

                    extraDuty.details = new ExtraDutyEntryModel.Details
                    {
                        TableData = tableDataList
                    };

                }


                return Json(new { status = "success", data = extraDuty });

            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

    }

}
