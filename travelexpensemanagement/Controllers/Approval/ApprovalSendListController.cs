
using AngleSharp.Dom;
using Dapper;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.Approval
{
    public class ApprovalSendListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ApprovalSendListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Approval/ApprovalSendList/Index.cshtml");
        }

        public IActionResult GetApprovalList(string searchTerm = "")
        {
            var documentData = new List<ApprovalListModel>();
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand(@"
                    SELECT doc_name, v_no, send_code, b.USER_NAME as sendname, send_date,
                    status AS Documentstatus, Approval_remark, c.NAME as Approvalstatus,
                    remarks, a.remarks as remarkname, new_modify, a.Department,
                    origin_name, origin_date, a.v_type, user_code, form_name
                    FROM approval_status a
                    LEFT JOIN CONDATABASE.dbo.USER_MAST b ON a.send_code = b.CODE
                    LEFT JOIN DOCSTATUS_MAST c ON a.Approval_Code = c.CODE
                    WHERE send_code = @send_code
                    AND a.comp_code = @comp_code
                    AND a.year_code = @year_code
                    AND (doc_name LIKE @searchTerm OR v_no LIKE @searchTerm)
                    ORDER BY send_date DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@send_code", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@searchTerm", "%" + searchTerm + "%");
                        cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@year_code", gv.PubFYearCode);

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

                return Json(new { success = true, data = documentData });
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

