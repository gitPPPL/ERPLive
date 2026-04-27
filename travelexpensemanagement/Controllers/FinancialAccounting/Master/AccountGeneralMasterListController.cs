using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Master;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    [SessionAuthorize]
    public class AccountGeneralMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public AccountGeneralMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "A/c GL Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); 

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/FinancialAccounting/Master/AccountGeneralMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetSubgroups(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var subgroups = new List<SUBGROUP_MAST>(); // define SubgroupDto to match your returned data
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SUBGROUP_MAST", conn)) // Your stored procedure
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                        // Add any other parameters your SP needs (pass DBNull.Value if not used)

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                subgroups.Add(new SUBGROUP_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    GROUP_CODE = reader["GROUP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["GROUP_CODE"]) : 0,  // Adjust field names as per your DB
                                    NATURE = reader["NATURE"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching subgroups", error = ex.Message });
            }

            return Json(new { success = true, lists = subgroups, totalCount });
        }

        [HttpGet]
        public IActionResult GetSubGroupByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            SUBGROUP_MAST subgroup = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SUBGROUP_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                subgroup = new SUBGROUP_MAST
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    AC_NO = rdr["AC_NO"]?.ToString(),
                                    NAME = rdr["NAME"]?.ToString(),
                                    SHORTNAME = rdr["SHORTNAME"]?.ToString(),
                                    GROUP_CODE = rdr["GROUP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["GROUP_CODE"]) : 0,
                                    ALIASNAME = rdr["ALIASNAME"]?.ToString(),
                                    ADD1 = rdr["ADD1"]?.ToString(),
                                    ADD2 = rdr["ADD2"]?.ToString(),
                                    ADD3 = rdr["ADD3"]?.ToString(),
                                    PAN = rdr["PAN"]?.ToString(),
                                    BANK_NAME = rdr["BANK_NAME"]?.ToString(),
                                    BANK_BRANCH = rdr["BANK_BRANCH"]?.ToString(),
                                    IFSC_CODE = rdr["IFSC_CODE"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
                                    NATURE = rdr["NATURE"]?.ToString()
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = subgroup });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching subgroup", error = ex.Message });
            }
        }

        public IActionResult ExportAllDocs()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var docList = new List<SUBGROUPExport>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_SUBGROUP_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "Export");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", int.MaxValue);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            docList.Add(new SUBGROUPExport
                            {
                                Code = reader["Code"]?.ToString(),
                                NAME = reader["NAME"]?.ToString(),
                                SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                SubGROUP_NAME = reader["SubGROUP_NAME"]?.ToString(),
                                NATURE = reader["NATURE"]?.ToString(),
                                STATUS = reader["STATUS"]?.ToString(),
                            });
                        }
                    }
                }
            }
            return Json(docList);
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            List<ItemGroupDetailDto> docDetails = new List<ItemGroupDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_SUBGROUP_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DocDetailID");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@Code", docCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new ItemGroupDetailDto
                            {
                                Code = reader["Code"]?.ToString(),
                                UUser = reader["UUser"]?.ToString(),
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            };
                            docDetails.Add(detail);
                        }
                    }
                }
            }

            return Json(new { success = true, data = docDetails });
        }
        public JsonResult Getapprovaldata(string id)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var docname = "GL master";
            //var vtype = "GLMS";
            var vtype = "BPMS";

            // Query for remark dropdown
            string queryRemark = "SELECT CODE, NAME FROM APPROVAL_RMKS";
            var moduelListRemark = _dropdownService.GetDropdownList(queryRemark);

            string querySendTo = $@" SELECT DISTINCT a.USER_CODE, b.FULL_NAME FROM DOC_APPROSTAGE a LEFT JOIN CONDATABASE.dbo.USER_MAST b ON a.USER_CODE = b.CODE 
            LEFT JOIN CONDATABASE.dbo.SUBUSER_MAST c ON b.CODE = c.USER_CODE AND c.COMP_CODE = {globalVar.PubCompCode} WHERE b.Active = 1 AND a.USER_CODE <> 1 AND a.DOC_CODE = '{vtype}'";
            var moduelListSendTo = _dropdownService.GetDropdownList(querySendTo);

            // Ensure that you're passing data correctly back as a JSON object
            return Json(new
            {
                success = true,
                data = new
                {
                    remarkDropdown = moduelListRemark,
                    sendToDropdown = moduelListSendTo,
                    id = id
                }
            });
        }

        [HttpPost]
        public IActionResult SendApproval(string sendTo, string remarks, string id)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            var docname = "GL master";
            //var vtype = "GLMS";
            var vtype = "BPMS";
            var deptname = "Accounts";
            var origincode = globalVar.PubUserId;

            var PubCompCode = globalVar.PubCompCode;
            var PubFYearCode = globalVar.PubFYearCode;
            var PubBranchCode = globalVar.PubBranchCode;

            var originDate = DateTime.Now;
            var formname = "Account General Master";
            var approvalCode = 0;
            var vno = id;
            
            string mType = "";
            string docid = "";

            string getQuery = @"SELECT M_TYPE, CONCAT(CAST(code AS VARCHAR(10)), M_TYPE) AS docid FROM SUBGROUP_MAST WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE";
            using (var con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (var cmd = new SqlCommand(getQuery, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", id);
                    using (var dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            mType = dr["M_TYPE"].ToString();
                            docid = dr["docid"].ToString();
                        }
                        else
                        {
                            return Json(new { success = false, message = "Invalid Code! No matching SUBGROUP_MAST entry found." });
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(sendTo) || string.IsNullOrEmpty(remarks))
            {
                return Json(new { success = false, message = "Send To and Remarks are required." });
            }
            bool isApprovalSent = false;
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (var tran = con.BeginTransaction())
                    {
                        string srnoQuery = @"SELECT ISNULL(MAX(srno), 0) + 1 FROM approval_status WHERE COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                        int srno;
                        using (var cmd = new SqlCommand(srnoQuery, con, tran))
                        {
                            cmd.Parameters.AddWithValue("@COMP_CODE", PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", PubFYearCode);
                            srno = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        string insertQuery = @"INSERT INTO approval_status 
                      (SRNO, YEAR_CODE, BRANCH_CODE, COMP_CODE, MENU_CODE, ORIGIN_CODE, ORIGIN_NAME, ORIGIN_DATE, 
                       DEPARTMENT, SEND_CODE, SEND_NAME, USER_CODE, USER_NAME, FORM_NAME, DOC_NAME, DOC_ID, 
                       V_TYPE, V_NO, V_DATE, SEND_DATE, STATUS, Approval_remark, REMARKS, 
                       Approval_Code, New_Modify, WSID, LIP, LID) 
                      VALUES 
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
                            cmd.Parameters.AddWithValue("@ORIGIN_DATE", originDate.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@DEPARTMENT", deptname);
                            //cmd.Parameters.AddWithValue("@SEND_CODE", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@SEND_CODE", sendTo);
                            cmd.Parameters.AddWithValue("@SEND_NAME", "Login Code");
                            //cmd.Parameters.AddWithValue("@USER_CODE", sendTo);
                            cmd.Parameters.AddWithValue("@USER_CODE", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@USER_NAME", "Sender Name");
                            cmd.Parameters.AddWithValue("@FORM_NAME", formname);
                            cmd.Parameters.AddWithValue("@DOC_NAME", docname);
                            cmd.Parameters.AddWithValue("@DOC_ID", docid);  
                            cmd.Parameters.AddWithValue("@V_TYPE", mType);  
                            cmd.Parameters.AddWithValue("@V_NO", vno);
                            cmd.Parameters.AddWithValue("@V_DATE", originDate.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@Approval_remark", remarks);
                            cmd.Parameters.AddWithValue("@REMARKS", remarks);
                            cmd.Parameters.AddWithValue("@Approval_Code", approvalCode);
                            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.ExecuteNonQuery();
                        }

                        tran.Commit();
                        isApprovalSent = true;
                    }
                }
                return Json(new { success = true, message = "Approval sent successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        //[HttpPost]
        //public IActionResult SendApproval(string sendTo, string remarks, string id)
        //{
        //    var globalVar = _globalVariableService.GetGlobalVariables();
        //    var docname = "GL master";
        //    //var vtype = "GLMS";
        //    var vtype = "BPMS";
        //    var deptname = "Accounts";
        //    var origincode = globalVar.PubUserId;
        //    var PubCompCode = globalVar.PubCompCode;
        //    var PubFYearCode = globalVar.PubFYearCode;
        //    var PubBranchCode = globalVar.PubBranchCode;
        //    var originDate = DateTime.Now;
        //    var tableName = "SUBGROUP_MAST";
        //    var formname = "Account General Master List";
        //    var tabletype = "Master";

        //    var docid = "";
        //    var vno = id;  
        //    var approvalCode = 1;  
        //    var v_date = originDate;


        //        if (string.IsNullOrEmpty(sendTo) || string.IsNullOrEmpty(remarks))
        //        {
        //            return Json(new { success = false, message = "Send To and Remarks are required." });
        //        }

        //    bool isApprovalSent = false;
        //    try
        //    {
        //        using (var con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();
        //            using (var tran = con.BeginTransaction())
        //            {
        //                //var delsql = "DELETE FROM approval_status2 WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE";
        //                //using (var cmd = new SqlCommand(delsql, con, tran))
        //                //{
        //                //    cmd.Parameters.AddWithValue("@V_TYPE", vtype);
        //                //    cmd.Parameters.AddWithValue("@V_NO", vno); 
        //                //    cmd.Parameters.AddWithValue("@COMP_CODE", PubCompCode);
        //                //    cmd.Parameters.AddWithValue("@YEAR_CODE", PubFYearCode);
        //                //    cmd.Parameters.AddWithValue("@BRANCH_CODE", PubBranchCode);
        //                //    cmd.ExecuteNonQuery();
        //                //}

        //                // Get the next SRNO
        //                var srnoQuery = "SELECT ISNULL(MAX(srno), 0) + 1 FROM approval_status WHERE COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
        //                int srno;
        //                using (var cmd = new SqlCommand(srnoQuery, con, tran))
        //                {
        //                    cmd.Parameters.AddWithValue("@COMP_CODE", PubCompCode);
        //                    cmd.Parameters.AddWithValue("@BRANCH_CODE", PubBranchCode);
        //                    cmd.Parameters.AddWithValue("@YEAR_CODE", PubFYearCode);
        //                    srno = Convert.ToInt32(cmd.ExecuteScalar());
        //                }

        //                // Insert the approval data into approval_status
        //                var insertQuery = @"INSERT INTO approval_status 
        //                      (SRNO, YEAR_CODE, BRANCH_CODE, COMP_CODE, MENU_CODE, ORIGIN_CODE, ORIGIN_NAME, ORIGIN_DATE, DEPARTMENT, SEND_CODE, SEND_NAME, 
        //                       USER_CODE, USER_NAME, FORM_NAME, DOC_NAME, DOC_ID, V_TYPE, V_NO, V_DATE, SEND_DATE, STATUS, Approval_remark, REMARKS, 
        //                       Approval_Code, New_Modify, WSID, LIP, LID) VALUES 
        //                      (@SRNO, @YEAR_CODE, @BRANCH_CODE, @COMP_CODE, @MENU_CODE, @ORIGIN_CODE, @ORIGIN_NAME, @ORIGIN_DATE, @DEPARTMENT, 
        //                       @SEND_CODE, @SEND_NAME, @USER_CODE, @USER_NAME, @FORM_NAME, @DOC_NAME, @DOC_ID, @V_TYPE, @V_NO, @V_DATE, 
        //                       FORMAT(GETDATE(), 'yyyy-MM-dd HH:mm'), 'OPEN', @Approval_remark, @REMARKS, @Approval_Code, 'New', @WSID, @LIP, @LID)";

        //                using (var cmd = new SqlCommand(insertQuery, con, tran))
        //                {
        //                    // Add the necessary parameters
        //                    cmd.Parameters.AddWithValue("@SRNO", srno);
        //                    cmd.Parameters.AddWithValue("@YEAR_CODE", PubFYearCode);
        //                    cmd.Parameters.AddWithValue("@BRANCH_CODE", PubBranchCode);
        //                    cmd.Parameters.AddWithValue("@COMP_CODE", PubCompCode);
        //                    cmd.Parameters.AddWithValue("@MENU_CODE", 123);  
        //                    cmd.Parameters.AddWithValue("@ORIGIN_CODE", origincode);
        //                    cmd.Parameters.AddWithValue("@ORIGIN_NAME", "Origin Name"); 
        //                    cmd.Parameters.AddWithValue("@ORIGIN_DATE", originDate.ToString("yyyy-MM-dd"));
        //                    cmd.Parameters.AddWithValue("@DEPARTMENT", deptname);
        //                    cmd.Parameters.AddWithValue("@SEND_CODE", sendTo); 
        //                    cmd.Parameters.AddWithValue("@SEND_NAME", "SenderName"); 
        //                    cmd.Parameters.AddWithValue("@USER_CODE", globalVar.PubUserId);
        //                    cmd.Parameters.AddWithValue("@USER_NAME", "sendTo");
        //                    cmd.Parameters.AddWithValue("@FORM_NAME", formname);
        //                    cmd.Parameters.AddWithValue("@DOC_NAME", docname);
        //                    cmd.Parameters.AddWithValue("@DOC_ID", docid); 
        //                    cmd.Parameters.AddWithValue("@V_TYPE", vtype);
        //                    cmd.Parameters.AddWithValue("@V_NO", vno);  
        //                    cmd.Parameters.AddWithValue("@V_DATE", v_date.ToString("yyyy-MM-dd"));
        //                    cmd.Parameters.AddWithValue("@Approval_remark", remarks);
        //                    cmd.Parameters.AddWithValue("@REMARKS", remarks);
        //                    cmd.Parameters.AddWithValue("@Approval_Code", approvalCode);  
        //                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
        //                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
        //                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

        //                    cmd.ExecuteNonQuery();
        //                }
        //                tran.Commit();
        //                isApprovalSent = true;
        //            }
        //        }
        //        if (isApprovalSent)
        //        {
        //            return Json(new { success = true, message = "Approval sent successfully." });
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = "Failed to send approval." });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error in sending approval: " + ex.Message });
        //    }
        //}

    }
}

