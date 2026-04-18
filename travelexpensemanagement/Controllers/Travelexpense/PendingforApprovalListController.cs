using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.Travelexpense
{
    public class PendingforApprovalListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        int PendingForApproval = 0;
        int SentRequests = 0;
        public PendingforApprovalListController(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        //public IActionResult Index(int? type)
        //{
        //    if (HttpContext.Session.GetString("USER_NAME") == null)
        //    {
        //        return RedirectToAction("Index", "Login");
        //    }

        //    if (type == 1)
        //    {
        //        // Logic for pending for approval
        //    }
        //    else if (type == 2)
        //    {
        //        // Logic for sent requests
        //    }
        //    return View();
        //}

        public IActionResult Index(int? type)
        {
            if (HttpContext.Session.GetString("USER_NAME") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            if (type == 1)
            {
                HttpContext.Session.SetInt32("ApprovalType", 1);
            }
            else if (type == 2)
            {
                HttpContext.Session.SetInt32("ApprovalType", 2);
            }

            //return View();
            return View("Index");
        }

        //states ddl banding code start block
        [HttpGet]
        public JsonResult GetStatussList()
        {
            List<object> status = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select ID, Name From StatusMaster";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    status.Add(new
                    {
                        Value = reader["ID"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }
            return Json(status);
        }
        //states ddl banding code end block
        [HttpGet]
        public JsonResult GetPendingforApprovalList(int page, int pageSize)
        {
            int approvalType = HttpContext.Session.GetInt32("ApprovalType") ?? 0;
            List<object> EmployeeList = new List<object>();
            var GetUserID = HttpContext.Session.GetString("CODE");

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = string.Empty;

                if (approvalType == 1)
                {
                    query = "SELECT * FROM Approval_Status WHERE send_code = @SendCode";
                }
                else if (approvalType == 2)
                {
                    query = "SELECT * FROM Approval_Status WHERE user_code = @SendCode";
                }
                else
                {
                    query = "SELECT * FROM Approval_Status WHERE send_code = @SendCode"; // default fallback
                }
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@SendCode", GetUserID ?? "System");

                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        EmployeeList.Add(new
                        {
                            SrNo = reader["SRNo"],
                            UserName = reader["User_Name"].ToString(),
                            SendDate = reader["Send_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Send_Date"]).ToString("yyyy-MM-dd") : "",
                            Remarks = reader["Remarks"].ToString(),
                            Status = reader["Status"].ToString(),
                            RequestId = reader["RequestId"].ToString(),
                            Send_Name = reader["Send_Name"].ToString()
                        });
                    }
                }
            }

            return Json(new { success = true, employeeList = EmployeeList, totalCount = EmployeeList.Count });
        }

        [HttpPost]
        public JsonResult UpdateApprovalStatus([FromBody] PendingforApprovalList model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Model is null!" });
            }
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            string query = @"UPDATE Approval_Status 
                                     SET User_Name = @UserName, Send_Date = @SendDate, Remarks = @Remarks, Status = @Status
                                     WHERE SRNo = @SrNo";
                            using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@SrNo", model.SrNo);
                                cmd.Parameters.AddWithValue("@UserName", HttpContext.Session.GetString("UserName") ?? "");
                                cmd.Parameters.AddWithValue("@SendDate", DateTime.Now);
                                cmd.Parameters.AddWithValue("@Remarks", model.Remarks ?? "");
                                cmd.Parameters.AddWithValue("@Status", model.Status ?? "");
                                cmd.ExecuteNonQuery();
                            }
                            if (model.RequestId.HasValue)
                            {
                                string updateRequestQuery = @"UPDATE TravelRequest 
                                                      SET Status = @Status 
                                                      WHERE RequestID = @RequestId";

                                using (SqlCommand cmdUpdate = new SqlCommand(updateRequestQuery, con, transaction))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@Status", model.Status ?? "");
                                    cmdUpdate.Parameters.AddWithValue("@RequestId", model.RequestId);
                                    cmdUpdate.ExecuteNonQuery();
                                }
                            }
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetPagedTravelRequestsJourneyDetails(int page = 1, int pageSizeJourneyDetails = 10, int requestId = 0)
        {
            try
            {
                List<object> travelRequests = new List<object>();
                int totalCount = 0;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    // Count total records based on RequestID
                    string countQuery = "SELECT COUNT(*) FROM JourneyDetails WHERE RequestID = @RequestID";
                    using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                    {
                        countCmd.Parameters.AddWithValue("@RequestID", requestId);
                        totalCount = (int)countCmd.ExecuteScalar();
                    }
                    int startRow = (page - 1) * pageSizeJourneyDetails + 1;
                    int endRow = page * pageSizeJourneyDetails;

                    string query = @"
                SELECT JourneyID, RequestID, TravelFrom, TravelTo,
                       TravelDate, ExpenseCategoryMaster, TotalCost, TransportMode, Purpose, TravelType,
                       CASE 
                           WHEN Journeytype = '1' THEN 'TO'
                           WHEN Journeytype = '2' THEN 'From'
                           ELSE 'Null'
                       END AS Journeytype
                FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY JourneyID DESC) AS RowNum
                    FROM JourneyDetails
                    WHERE RequestID = @RequestID
                ) AS RowConstrainedResult
                WHERE RowNum BETWEEN @StartRow AND @EndRow
                ORDER BY JourneyID DESC";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@RequestID", requestId);
                        cmd.Parameters.AddWithValue("@StartRow", startRow);
                        cmd.Parameters.AddWithValue("@EndRow", endRow);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                travelRequests.Add(new
                                {
                                    JourneyID = reader["JourneyID"],
                                    RequestID = reader["RequestID"],
                                    TravelFrom = reader["TravelFrom"]?.ToString(),
                                    TravelTo = reader["TravelTo"]?.ToString(),
                                    TravelDate = reader["TravelDate"] != DBNull.Value ? Convert.ToDateTime(reader["TravelDate"]).ToString("yyyy-MM-dd") : "",
                                    ExpenseCategoryMaster = reader["ExpenseCategoryMaster"]?.ToString(),
                                    TotalCost = reader["TotalCost"],
                                    TransportMode = reader["TransportMode"]?.ToString(),
                                    Purpose = reader["Purpose"]?.ToString(),
                                    TravelType = reader["TravelType"]?.ToString(),
                                    Journeytype = reader["Journeytype"]?.ToString()
                                });
                            }
                        }
                    }
                }
                return Json(new { success = true, travelRequests, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetFromCityMasterddl1()
        {
            List<SelectListItem> cityList = new List<SelectListItem>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT code, Name FROM CITY_MAST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        cityList.Add(new SelectListItem
                        {
                            Value = reader["code"].ToString(),
                            Text = reader["Name"].ToString()
                        });
                    }
                }
            }
            return Json(cityList);
        }
        [HttpGet]
        public JsonResult GetFromCityMasterddlto()
        {
            List<SelectListItem> cityList = new List<SelectListItem>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT code, Name FROM CITY_MAST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        cityList.Add(new SelectListItem
                        {
                            Value = reader["code"].ToString(),
                            Text = reader["Name"].ToString()
                        });
                    }
                }
            }
            return Json(cityList);
        }
        [HttpGet]
        public JsonResult GetTransportationModeMasterList()
        {
            List<SelectListItem> TransportationModeMaster = new List<SelectListItem>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "Select ModeID, ModeName from TransportationModeMaster order by ModeID asc";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        TransportationModeMaster.Add(new SelectListItem
                        {
                            Value = reader["ModeID"].ToString(),
                            Text = reader["ModeName"].ToString()
                        });
                    }
                    conn.Close();
                }
            }
            return Json(TransportationModeMaster);
        }
    }
}

