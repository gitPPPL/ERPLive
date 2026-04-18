using AngleSharp.Dom;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.Approval
{
    public class ApprovalReceivedListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ApprovalReceivedListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Approval/ApprovalReceivedList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetApprovalList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var documentData = new List<ApprovalListModel>();
            var UserID = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(@"SELECT doc_name, v_no, send_code, b.USER_NAME as sendname, send_date, status as Documentstatus,
                    Approval_remark, c.NAME as Approvalstatus, remarks, d.name as remarkname, new_modify, 
                    a.Department, origin_name, origin_date, a.v_type,user_code,form_name FROM approval_status a LEFT JOIN CONDATABASE.dbo.USER_MAST b ON a.send_code = b.CODE
                    LEFT JOIN DOCSTATUS_MAST c ON a.Approval_Code = c.CODE LEFT JOIN APPROVAL_RMKS d ON a.remarks = d.CODE
                    WHERE send_code = @send_code AND (doc_name LIKE @searchTerm OR v_no LIKE @searchTerm)
                    ORDER BY send_date OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", conn))
                    {
                        cmd.Parameters.AddWithValue("@send_code", UserID.PubUserId);
                        cmd.Parameters.AddWithValue("@searchTerm", "%" + searchTerm + "%");
                        cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                documentData.Add(new ApprovalListModel
                                {
                                    DocName = reader["doc_name"]?.ToString(),
                                    VNo = reader["v_no"] != DBNull.Value ? Convert.ToInt32(reader["v_no"]) : 0,
                                    send_code = reader["send_code"]?.ToString(),
                                    SendName = reader["sendname"]?.ToString(),

                                    SendDate = reader["send_date"] != DBNull.Value ? Convert.ToDateTime(reader["send_date"]) : DateTime.MinValue,
                                    DocumentStatus = reader["Documentstatus"]?.ToString(),
                                    ApprovalRemark = reader["Approval_remark"]?.ToString(),
                                    ApprovalStatus = reader["Approvalstatus"]?.ToString(),

                                    RemarkName = reader["remarkname"]?.ToString(),
                                    NewModify = reader["new_modify"]?.ToString(),
                                    Department = reader["Department"]?.ToString(),
                                    OriginName = reader["origin_name"]?.ToString(),

                                    OriginDate = reader["origin_date"] != DBNull.Value ? Convert.ToDateTime(reader["origin_date"]) : DateTime.MinValue,
                                    VType = reader["v_type"]?.ToString(),
                                    user_code = reader["user_code"]?.ToString(),
                                    form_name = reader["form_name"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching document data", error = ex.Message });
            }

            var totalCount = documentData.Count; 
            return Json(new { success = true, data = documentData, totalCount });
        }
        public IActionResult GetApprovalReceived()
        {
            string query = $@"select code,name from DOCSTATUS_MAST where V_TYPE='Approval' order by code";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public IActionResult GetRemarks()
        {
            var Companycode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $@"Select CODE,NAME from APPROVAL_RMKS where COMP_CODE= {Companycode}";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public IActionResult GetForwardto(string vNo, string userCode, string vType)
        {
            var glv = _globalVariableService.GetGlobalVariables();
            string query = $@"
                 ;WITH tmp AS (
                     SELECT a.USER_CODE, b.FULL_NAME, a.SRNO
                     FROM DOC_APPROSTAGE a
                     LEFT JOIN CONDATABASE.dbo.USER_MAST b ON a.USER_CODE = b.CODE
                     LEFT JOIN CONDATABASE.dbo.SUBUSER_MAST c ON b.CODE = c.USER_CODE AND c.COMP_CODE = {glv.PubCompCode}
                     WHERE a.USER_CODE <> {userCode}
                     AND a.DOC_CODE = '{vType}'
                     AND a.COMP_CODE = {glv.PubCompCode}

                     UNION ALL
                     SELECT send_code, send_name, 0
                     FROM APPROVAL_STATUS
                     WHERE comp_code = {glv.PubCompCode}
                     AND branch_code = {glv.PubBranchCode}
                     AND year_code = {glv.PubFYearCode}
                     AND v_no = '{vNo}'
                     AND v_type = '{vType}'
                 )
                 SELECT USER_CODE, FULL_NAME FROM tmp  WHERE USER_CODE <> {userCode} and FULL_NAME<>'' GROUP BY USER_CODE, FULL_NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpPost]
        public ActionResult SaveApprovaldata([FromBody] ApprovalDataRequest request)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            foreach (var rowData in request.Rows)
            {
                var docname = rowData.DocName;
                //var vtype = rowData.VType;
                var deptname = rowData.Department;  
                var origincode = globalVar.PubUserId;

                var PubCompCode = globalVar.PubCompCode;
                var PubFYearCode = globalVar.PubFYearCode;
                var PubBranchCode = globalVar.PubBranchCode;

                var originDate = rowData.OriginDate;
                var formname = rowData.form_name;
                var approvalCode = rowData.ApprovalStatus;
                var vno = rowData.VNo;  

                string mType = rowData.VType;
                string docid = "";

                var sendTo = rowData.ForwardTo; 
                var remarks = rowData.Remarks;
                if (string.IsNullOrEmpty(sendTo) || string.IsNullOrEmpty(remarks))
                {
                    return Json(new { success = false, message = "Send To and Remarks are required." });
                }
                try
                {
                    using (var con = _dbConnection.GetErpConnection())
                    {
                        con.Open();
                        using (var tran = con.BeginTransaction())
                        {
                            // Get next SRNO
                            string srnoQuery = @"SELECT ISNULL(MAX(srno), 0) + 1 
                                         FROM approval_status 
                                         WHERE COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                            int srno;
                            using (var cmd = new SqlCommand(srnoQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@COMP_CODE", PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", PubBranchCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", PubFYearCode);
                                srno = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            // Insert into approval_status table
                            string insertQuery = @"INSERT INTO approval_status 
                            (SRNO, YEAR_CODE, BRANCH_CODE, COMP_CODE, MENU_CODE, ORIGIN_CODE, ORIGIN_NAME, ORIGIN_DATE, 
                             DEPARTMENT, SEND_CODE, SEND_NAME, USER_CODE, USER_NAME, FORM_NAME, DOC_NAME, DOC_ID, 
                             V_TYPE, V_NO, V_DATE, SEND_DATE, STATUS, Approval_remark, REMARKS, 
                             Approval_Code, New_Modify, WSID, LIP, LID) VALUES 
                            (@SRNO, @YEAR_CODE, @BRANCH_CODE, @COMP_CODE, @MENU_CODE, @ORIGIN_CODE, @ORIGIN_NAME, @ORIGIN_DATE, 
                             @DEPARTMENT, @SEND_CODE, @SEND_NAME, @USER_CODE, @USER_NAME, @FORM_NAME, @DOC_NAME, @DOC_ID, 
                             @V_TYPE, @V_NO, @V_DATE, 
                             FORMAT(GETDATE(), 'yyyy-MM-dd HH:mm'), 'OPEN', @Approval_remark, @REMARKS, 
                             @Approval_Code, 'New', @WSID, @LIP, @LID)";

                            using (var cmd = new SqlCommand(insertQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@SRNO", srno);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", PubFYearCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", PubBranchCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", PubCompCode);
                                cmd.Parameters.AddWithValue("@MENU_CODE", 0); 
                                cmd.Parameters.AddWithValue("@ORIGIN_CODE", origincode);
                                cmd.Parameters.AddWithValue("@ORIGIN_NAME", "Origin Name");
                                cmd.Parameters.AddWithValue("@ORIGIN_DATE", originDate);
                                cmd.Parameters.AddWithValue("@DEPARTMENT", deptname);
                                //cmd.Parameters.AddWithValue("@SEND_CODE", globalVar.PubUserId);
                                cmd.Parameters.AddWithValue("@SEND_CODE", sendTo);
                                cmd.Parameters.AddWithValue("@SEND_NAME", "Login Code"); 
                                cmd.Parameters.AddWithValue("@USER_CODE", globalVar.PubUserId);
                                cmd.Parameters.AddWithValue("@USER_NAME", "Sender Name");
                                cmd.Parameters.AddWithValue("@FORM_NAME", formname);
                                cmd.Parameters.AddWithValue("@DOC_NAME", docname);
                                cmd.Parameters.AddWithValue("@DOC_ID", docid);
                                cmd.Parameters.AddWithValue("@V_TYPE", mType);
                                cmd.Parameters.AddWithValue("@V_NO", vno);
                                cmd.Parameters.AddWithValue("@V_DATE", originDate);
                                cmd.Parameters.AddWithValue("@Approval_remark", remarks);
                                cmd.Parameters.AddWithValue("@REMARKS", remarks);
                                cmd.Parameters.AddWithValue("@Approval_Code", approvalCode);
                                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                cmd.ExecuteNonQuery();
                            }
                            tran.Commit();
                        }
                    }

                    return Json(new { success = true, message = "Approval sent successfully." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error: " + ex.Message });
                }
            }
            return Json(new { success = false, message = "No rows to process." });
        }

        public class ApprovalListModel
        {
            public string DocName { get; set; }
            public int VNo { get; set; }
            public string send_code { get; set; }
            public string SendName { get; set; }
            public DateTime SendDate { get; set; }
            public string DocumentStatus { get; set; }
            public string ApprovalRemark { get; set; }
            public string ApprovalStatus { get; set; }
            public string RemarkName { get; set; }
            public string NewModify { get; set; }
            public string Department { get; set; }
            public string OriginName { get; set; }
            public DateTime OriginDate { get; set; }
            public string VType { get; set; }
            public string user_code { get; set; }
            public string? form_name { get; set; }
        }

        public class ApprovalDataModel
        {
            public string UserCode { get; set; }
            public string VNo { get; set; }
            public string VType { get; set; }
            public string DocName { get; set; }
            public string SendName { get; set; }
            public string SendDate { get; set; }
            public string DocumentStatus { get; set; }
            public string ApprovalStatus { get; set; }
            public string Remarks { get; set; }
            public string NewModify { get; set; }
            public string Department { get; set; }
            public string OriginName { get; set; }
            public string OriginDate { get; set; }
            public string ForwardTo { get; set; }
            public string form_name { get; set; }
        }

        public class ApprovalDataRequest
        {
            public List<ApprovalDataModel> Rows { get; set; }
        }
    }

}

