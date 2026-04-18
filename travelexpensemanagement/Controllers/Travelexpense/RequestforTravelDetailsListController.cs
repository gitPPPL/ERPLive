using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Reflection.Metadata;

namespace travelexpensemanagement.Controllers.Travelexpense
{
    public class RequestforTravelDetailsListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        public RequestforTravelDetailsListController(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("USER_NAME") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            //return View();
            return View("Index");
        }
        [HttpGet]
        public JsonResult GetPagedTravelRequests(int page = 1, int pageSize = 10)
        {
            try
            {
                List<object> travelRequests = new List<object>();
                int totalCount = 0;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Count total records
                    string countQuery = "SELECT COUNT(*) FROM TravelRequest1 WHERE TravelFrom IS NOT NULL";
                    using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                    {
                        totalCount = (int)countCmd.ExecuteScalar();
                    }
                    // Paged data query
                    string query = $@"
                SELECT SRNO, USER_NAME AS EmployeeName, TravelFrom, TravelTo, TravelDate, TotalCost, Purpose, TravelType, 
                    CASE 
                        WHEN Journeytype = '1' THEN 'TO'
                        WHEN Journeytype = '2' THEN 'From'
                        ELSE 'Null'
                    END AS Journeytype,
                    Status
                FROM (SELECT tr.*, um.USER_NAME, ROW_NUMBER() OVER (ORDER BY tr.SRNO DESC) AS RowNum
                    FROM TravelRequest1 tr JOIN CONDATABASE..USER_MAST um ON tr.EmployeeName = um.CODE
                    WHERE tr.TravelFrom IS NOT NULL) AS RowConstrainedResult
                WHERE RowNum BETWEEN {(page - 1) * pageSize + 1} AND {page * pageSize}
                ORDER BY SRNO DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                travelRequests.Add(new
                                {
                                    SRNO = reader["SRNO"],
                                    EmployeeName = reader["EmployeeName"]?.ToString(),
                                    TravelFrom = reader["TravelFrom"]?.ToString(),
                                    TravelTo = reader["TravelTo"]?.ToString(),
                                    TravelDate = reader["TravelDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["TravelDate"]).ToString("yyyy-MM-dd")
                                        : "",
                                    TotalCost = reader["TotalCost"],
                                    Purpose = reader["Purpose"]?.ToString(),
                                    TravelType = reader["TravelType"]?.ToString(),
                                    Journeytype = reader["Journeytype"]?.ToString(),
                                    Status = reader["Status"]?.ToString()
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


        [HttpPost]
        public JsonResult UpdateTravelRequest([FromBody] TravelRequestModel model)
            {
            try
            {
                if (model == null || model.SRNO <= 0)
                {
                    return Json(new { success = false, message = "Invalid data submitted." });
                }

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string query = @"UPDATE TravelRequest1 SET 
                            EmployeeName = @EmployeeName,
                            TravelFrom = @TravelFrom,
                            TravelTo = @TravelTo,
                            TravelDate = @TravelDate,
                            TotalCost = @TotalCost,
                            Purpose = @Purpose,
                            TravelType = @TravelType,
                            Journeytype = @Journeytype
                        WHERE SRNO = @RequestID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@RequestID", model.SRNO);
                        cmd.Parameters.AddWithValue("@EmployeeName", model.EmployeeName ?? "");
                        cmd.Parameters.AddWithValue("@TravelFrom", model.TravelFrom ?? "");
                        cmd.Parameters.AddWithValue("@TravelTo", model.TravelTo ?? "");
                        //cmd.Parameters.AddWithValue("@TravelDate", model.TravelDate ?? "");
                        cmd.Parameters.AddWithValue("@TravelDate", model.TravelDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TotalCost", model.TotalCost);
                        cmd.Parameters.AddWithValue("@Purpose", model.Purpose ?? "");
                        cmd.Parameters.AddWithValue("@TravelType", model.TravelType ?? "");

                        if (string.Equals(model.Journeytype, "TO", StringComparison.OrdinalIgnoreCase))
                        {
                            cmd.Parameters.AddWithValue("@Journeytype", 1);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@Journeytype", 2);
                        }
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
        //Journey Details start block  
        [HttpGet]
        public JsonResult GetPagedTravelRequestsJourneyDetails(int page = 1, int pageSizeJourneyDetails = 10, int SRNO = 0)
        {
            try
            {
                List<object> travelRequests = new List<object>();
                int totalCount = 0;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    // Count total records based on RequestID
                    string countQuery = "SELECT COUNT(*) FROM TravelRequest2 WHERE sno = @SRNO";
                    using (SqlCommand countCmd = new SqlCommand(countQuery, con))
                    {
                        countCmd.Parameters.AddWithValue("@SRNO", SRNO);
                        totalCount = (int)countCmd.ExecuteScalar();
                    }
                    int startRow = (page - 1) * pageSizeJourneyDetails + 1;
                    int endRow = page * pageSizeJourneyDetails;

                    string query = @"
                SELECT Code, sno, TravelFrom, TravelTo,
                       TravelDate, ExpenseCategoryMaster, TotalCost, TransportMode, Purpose, TravelType,
                       CASE 
                           WHEN Journeytype = '1' THEN 'TO'
                           WHEN Journeytype = '2' THEN 'From'
                           ELSE 'Null'
                       END AS Journeytype
                FROM (
                    SELECT *, ROW_NUMBER() OVER (ORDER BY Code DESC) AS RowNum
                    FROM TravelRequest2
                    WHERE sno = @SRNO
                ) AS RowConstrainedResult
                WHERE RowNum BETWEEN @StartRow AND @EndRow
                ORDER BY Code DESC";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@SRNO", SRNO);
                        cmd.Parameters.AddWithValue("@StartRow", startRow);
                        cmd.Parameters.AddWithValue("@EndRow", endRow);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                travelRequests.Add(new
                                {
                                    Code = reader["Code"],
                                    SRNO = reader["sno"],
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
        [HttpPost]
        public JsonResult UpdateTravelRequestJourneyDetails([FromBody] JourneyDetailsModel model)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string updateQuery = @"
                UPDATE JourneyDetails SET
                    TravelFrom = @TravelFrom,
                    TravelTo = @TravelTo,
                    TravelDate = @TravelDate,
                    TotalCost = @TotalCost,
                    Purpose = @Purpose,
                    TravelType = @TravelType,
                    Journeytype = CASE 
                        WHEN @Journeytype = 'TO' THEN '1'
                        WHEN @Journeytype = 'From' THEN '2'
                        ELSE NULL
                    END
                WHERE JourneyID = @JourneyID";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@JourneyID", model.Code);
                        cmd.Parameters.AddWithValue("@TravelFrom", model.TravelFrom ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TravelTo", model.TravelTo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TravelDate", model.TravelDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TotalCost", model.TotalCost ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Purpose", model.Purpose ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TravelType", model.TravelType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Journeytype", model.Journeytype ?? (object)DBNull.Value);

                        int rows = cmd.ExecuteNonQuery();
                        return Json(new { success = rows > 0 });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // Add Row Multiple Journey Details start Block
        [HttpGet]
        public JsonResult GetFromCityMasterddlReturn()
        {
            List<SelectListItem> Fromonewayddl = new List<SelectListItem>();
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "Select code, Name From CITY_MAST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Fromonewayddl.Add(new SelectListItem
                        {
                            Value = reader["code"].ToString(),
                            Text = reader["Name"].ToString()
                        });
                    }
                    conn.Close();
                }
            }
            return Json(Fromonewayddl);
        }
        // Add Row Multiple Journey Details End Block
        [HttpGet]
        public JsonResult GetExpenseCategories()
        {
            List<SelectListItem> ExpenseCategoryMaster = new List<SelectListItem>();
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CategoryID, CategoryName FROM ExpenseCategoryMaster ORDER BY CategoryID ASC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        ExpenseCategoryMaster.Add(new SelectListItem
                        {
                            Value = reader["CategoryID"].ToString(),
                            Text = reader["CategoryName"].ToString()
                        });
                    }
                    conn.Close();
                }
            }
            return Json(ExpenseCategoryMaster);
        }
        //TransportationModeMaster ddl banding in start block
        [HttpGet]
        public JsonResult GetTransportationModeMaster()
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
        //From City Master dropdown List
        [HttpGet]
        public JsonResult GetFromCityMasterddl()
        {
            List<SelectListItem> Fromonewayddl = new List<SelectListItem>();
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "Select code, Name From CITY_MAST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Fromonewayddl.Add(new SelectListItem
                        {
                            Value = reader["code"].ToString(),
                            Text = reader["Name"].ToString()
                        });
                    }
                    conn.Close();
                }
            }
            return Json(Fromonewayddl);
        }
        [HttpPost]
        public IActionResult SaveTravelExpensesReturn([FromBody] TravelExpenseWrapper data)
        {
            if (data == null || data.TravelDetails == null || !data.TravelDetails.Any())
            {
                return BadRequest("No data received.");
            }
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open(); // ✅ Corrected from `con.Open()`

                    foreach (var travel in data.TravelDetails)
                    {
                        using (SqlCommand insertCmd = new SqlCommand(@"
                    INSERT INTO JourneyDetails 
                    (
                        RequestID, TravelFrom, TravelTo, TravelDate, 
                        ExpenseCategoryMaster, TotalCost, TransportMode, 
                        Purpose, TravelType, Active, CreatedAt, Journeytype
                    ) 
                    VALUES 
                    (
                        @RequestID, @TravelFrom, @TravelTo, @TravelDate, 
                        @ExpenseCategoryMaster, @TotalCost, @TransportMode, 
                        @Purpose, @TravelType, 1, GETDATE(), 1
                    )", conn)) // ✅ using `conn` here
                        {
                            insertCmd.Parameters.AddWithValue("@RequestID", travel.RequestIdGet); // 🔴 This line causes the error
                            insertCmd.Parameters.AddWithValue("@TravelFrom", (object?)travel.FromOneWay ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@TravelTo", (object?)travel.ToOneWay ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@TravelDate", travel.TravelDate ?? (object)DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@ExpenseCategoryMaster", (object?)travel.ExpenseCategoryMaster ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@TotalCost", travel.Cost.HasValue && travel.Cost.Value != 0 ? travel.Cost.Value : DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@TransportMode", (object?)travel.TransportationModeMaster ?? DBNull.Value);
                            insertCmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(travel.Purpose) ? DBNull.Value : travel.Purpose.Trim());
                            insertCmd.Parameters.AddWithValue("@TravelType", DBNull.Value); // Update if needed
                            insertCmd.Parameters.AddWithValue("@Journeytype", 1);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                    return Ok(new { message = "Return travel expenses saved successfully!" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        //dll users banding start block
        [HttpGet]
        public JsonResult GetUserddl()
        {
            List<object> designation = new List<object>();
            int empId = Convert.ToInt32(HttpContext.Session.GetString("CODE"));

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT us.CODE AS UserID, us.USER_NAME AS Username FROM CONDATABASE.dbo.USER_MAST us INNER JOIN TravelRequest tr ON us.CODE = tr.EmpID 
            WHERE tr.Journeytype = 1 AND us.CODE NOT IN (@EmpId)";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@EmpId", empId);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            designation.Add(new
                            {
                                Value = reader["UserID"].ToString(),
                                Text = reader["Username"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(designation);
        }

        //dll users banding End block

        //Document start block dll banding code
        public JsonResult GetDocumentddl()
        {
            List<object> designation = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //string query = "Select top 1 Code, Name  from Approval_Remark";
                string query = "Select top 1 Code, Name  from APPROVAL_RMKS";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    designation.Add(new
                    {
                        Value = reader["Code"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }
            return Json(designation);
        }
        //Document End block dll banding code
        [HttpPost]
        public JsonResult SendApproval([FromBody] List<ApprovalViewModel> approvalData)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        foreach (var item in approvalData)
                        {
                            // Get next srno
                            string getSrnoQuery = "SELECT ISNULL(MAX(srno), 0) + 1 FROM Approval_Status";
                            SqlCommand srnoCmd = new SqlCommand(getSrnoQuery, con, transaction);
                            int nextSrno = (int)srnoCmd.ExecuteScalar();

                            string insertQuery = @"INSERT INTO Approval_Status 
                    (srno, RequestId, Comp_Code, Branch_Code, Year_Code, Menu_Code, Department,
                     Origin_Code, Origin_Name, Origin_Date, Send_Code, Send_Name,
                     User_Code, User_Name, Form_Name, Doc_Name, Doc_Id,
                     V_Type, V_No, V_Date, Send_Date, Close_Date,
                     Status, Approval_Code, Approval_Remark, Remarks)
                    VALUES
                    (@srno, @RequestId, @Comp_Code, @Branch_Code, @Year_Code, @Menu_Code, @Department,
                     @Origin_Code, @Origin_Name, @Origin_Date, @Send_Code, @Send_Name,
                     @User_Code, @User_Name, @Form_Name, @Doc_Name, @Doc_Id,
                     @V_Type, @V_No, @V_Date, @Send_Date, @Close_Date,
                     @Status, @Approval_Code, @Approval_Remark, @Remarks)";

                            using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@srno", nextSrno);
                                cmd.Parameters.AddWithValue("@RequestId", item.RequestId != 0 ? item.RequestId : 1);
                                cmd.Parameters.AddWithValue("@Comp_Code", 1);
                                cmd.Parameters.AddWithValue("@Branch_Code", 1);
                                cmd.Parameters.AddWithValue("@Year_Code", 1);
                                cmd.Parameters.AddWithValue("@Menu_Code", 1);
                                cmd.Parameters.AddWithValue("@Department", "1");
                                cmd.Parameters.AddWithValue("@Origin_Code", 1);
                                cmd.Parameters.AddWithValue("@Origin_Name", "1");
                                cmd.Parameters.AddWithValue("@Origin_Date", DateTime.Now);
                                cmd.Parameters.AddWithValue("@Send_Code", item.User ?? "1");
                                cmd.Parameters.AddWithValue("@Send_Name", item.SendUserName ?? "1");
                                cmd.Parameters.AddWithValue("@User_Code", HttpContext.Session.GetString("CODE") ?? "1");
                                cmd.Parameters.AddWithValue("@User_Name", HttpContext.Session.GetString("USER_NAME") ?? "1");
                                cmd.Parameters.AddWithValue("@Form_Name", "1");
                                cmd.Parameters.AddWithValue("@Doc_Name", item.Document ?? "1");
                                cmd.Parameters.AddWithValue("@Doc_Id", "1");
                                cmd.Parameters.AddWithValue("@V_Type", "1");
                                cmd.Parameters.AddWithValue("@V_No", 1);
                                cmd.Parameters.AddWithValue("@V_Date", DateTime.Now);
                                cmd.Parameters.AddWithValue("@Send_Date", DateTime.Now);
                                cmd.Parameters.AddWithValue("@Close_Date", DateTime.Now);
                                cmd.Parameters.AddWithValue("@Status", "1");
                                cmd.Parameters.AddWithValue("@Approval_Code", 1);
                                cmd.Parameters.AddWithValue("@Approval_Remark", "1");
                                cmd.Parameters.AddWithValue("@Remarks", item.Remark ?? "1");

                                cmd.ExecuteNonQuery();
                            }

                            // Update TravelRequest status
                            string updateQuery = @"UPDATE TravelRequest SET Status = 1 WHERE RequestID = @RequestId";
                            using (SqlCommand cmdUpdate = new SqlCommand(updateQuery, con, transaction))
                            {
                                cmdUpdate.Parameters.AddWithValue("@RequestId", item.RequestId);
                                cmdUpdate.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return Json(new { success = true });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, error = ex.Message });
                    }
                }
            }
        }
        public class TravelExpenseWrapper
        {
            public List<TravelExpense> TravelDetails { get; set; }
        }
        public class TravelExpense
        {
            public int RequestIdGet { get; set; }
            public int? FromOneWay { get; set; }
            public int? ToOneWay { get; set; }
            //public string TravelDate { get; set; }
            public DateTime? TravelDate { get; set; }
            public int? ExpenseCategoryMaster { get; set; }
            public decimal? Cost { get; set; }
            public int? TransportationModeMaster { get; set; }
            public string Purpose { get; set; }
        }

    }
}
