
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
    public class ApprovalSendListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ApprovalSendListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Approval/ApprovalSendList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetApprovalList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var documentData = new List<ApprovalListModel>();
            var user = _globalVariableService.GetGlobalVariables();
            int totalCount = 0;
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (SqlCommand countCmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM approval_status a 
                        LEFT JOIN CONDATABASE.dbo.USER_MAST b ON a.send_code = b.CODE
                        LEFT JOIN DOCSTATUS_MAST c ON a.Approval_Code = c.CODE 
                        LEFT JOIN APPROVAL_RMKS d ON a.remarks = d.CODE
                        WHERE send_code = @send_code AND status IN ('OPEN') AND (doc_name LIKE @searchTerm OR v_no LIKE @searchTerm) ", conn))
                    {
                        countCmd.Parameters.AddWithValue("@send_code", user.PubUserId);
                        countCmd.Parameters.AddWithValue("@searchTerm", "%" + searchTerm + "%");
                        totalCount = (int)countCmd.ExecuteScalar();
                    }
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT doc_name, v_no, send_code, b.USER_NAME as sendname, send_date, 
                        status AS Documentstatus, Approval_remark, c.NAME as Approvalstatus, remarks, 
                        d.name as remarkname, new_modify, a.Department, origin_name, origin_date, 
                        a.v_type, user_code, form_name
                        FROM approval_status a 
                        LEFT JOIN CONDATABASE.dbo.USER_MAST b ON a.send_code = b.CODE
                        LEFT JOIN DOCSTATUS_MAST c ON a.Approval_Code = c.CODE 
                        LEFT JOIN APPROVAL_RMKS d ON a.remarks = d.CODE
                        WHERE user_code = @send_code AND status IN ('OPEN')
                        AND (doc_name LIKE @searchTerm OR v_no LIKE @searchTerm)
                        ORDER BY send_date OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY ", conn))
                    {
                        cmd.Parameters.AddWithValue("@send_code", user.PubUserId);
                        cmd.Parameters.AddWithValue("@searchTerm", "%" + searchTerm + "%");
                        cmd.Parameters.AddWithValue("@Offset", (pageNumber - 1) * pageSize);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

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
                return Json(new { success = true, data = documentData, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
    }

}

