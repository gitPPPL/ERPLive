using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Ocsp;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Travelexpense
{
    public class RequestforTravelController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public RequestforTravelController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, travelexpensemanagement.ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("USER_NAME") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            ViewBag.CurrentMenu = "Travel Management";
            var permissions = _moduleService.GetUserMenuPermissions();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions
            };
            return View(model);
            //return View("Index");
        }
        [HttpGet]
        public JsonResult GetUserNameEmailAndTravelData()
        {
            string userName = HttpContext.Session.GetString("USER_NAME");
            string empId = HttpContext.Session.GetString("CODE");
            string compCode = HttpContext.Session.GetString("COMP_CODE");
            string trimmedUserName = userName?.Trim();
            if (string.IsNullOrEmpty(trimmedUserName))
            {
                return Json(new { success = false, message = "User is not logged in." });
            }
            List<object> travelRequests = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string travelQuery = @"SELECT EmployeeName, TravelFrom, TravelTo, TravelDate, TotalCost, Purpose, TravelType
                               FROM TravelRequest1 WHERE EmpID = @EmpID AND Journeytype = 1";
                using (SqlCommand travelCmd = new SqlCommand(travelQuery, con))
                {
                    travelCmd.Parameters.AddWithValue("@EmpID", HttpContext.Session.GetString("CODE"));
                    using (SqlDataReader travelReader = travelCmd.ExecuteReader())
                    {
                        while (travelReader.Read())
                        {
                            travelRequests.Add(new
                            {
                                EmployeeName = travelReader["EmployeeName"] != DBNull.Value ? travelReader["EmployeeName"].ToString() : "",
                                TravelFrom = travelReader["TravelFrom"] != DBNull.Value ? travelReader["TravelFrom"].ToString() : "",
                                TravelTo = travelReader["TravelTo"] != DBNull.Value ? travelReader["TravelTo"].ToString() : "",
                                TravelDate = travelReader["TravelDate"] != DBNull.Value ? Convert.ToDateTime(travelReader["TravelDate"]).ToString("yyyy-MM-dd") : "",
                                TotalCost = travelReader["TotalCost"] != DBNull.Value ? Convert.ToDecimal(travelReader["TotalCost"]) : 0,
                                Purpose = travelReader["Purpose"] != DBNull.Value ? travelReader["Purpose"].ToString() : "",
                                TravelType = travelReader["TravelType"] != DBNull.Value ? travelReader["TravelType"].ToString() : null
                            });
                        }
                    }
                }
            }
            if (travelRequests.Count > 0)
            {
                return Json(new
                {
                    success = true,
                    empId,
                    userName,
                    travelRequests
                });
            }
            else
            {
                return Json(new
                {
                    success = true,
                    empId,
                    userName
                });
            }
        }
        //Meals ddl banding in start block
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
            List<SelectListItem> FromonewayCityddl = new List<SelectListItem>();
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "Select code, Name From CITY_MAST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        FromonewayCityddl.Add(new SelectListItem
                        {
                            Value = reader["code"].ToString(),
                            Text = reader["Name"].ToString()
                        });
                    }
                    conn.Close();
                }
            }
            return Json(FromonewayCityddl);
        }
        [HttpGet]
        public JsonResult GetUserMaster()
        {
            List<SelectListItem> usermaster = new List<SelectListItem>();
            using (SqlConnection conn = _dbConnection.GetConDbConnection())
            {
                string query = "SELECT Code, USER_NAME FROM USER_MAST";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        usermaster.Add(new SelectListItem
                        {
                            Value = reader["Code"].ToString(),
                            Text = reader["USER_NAME"].ToString()
                        });
                    }
                    conn.Close();
                }
            }

            var UserCode = HttpContext?.Session.GetString("CODE");

            return Json(new
            {
                Users = usermaster,
                SelectedUser = UserCode
            });
        }


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
        [HttpPost]
        public IActionResult SaveTravelExpenses([FromBody] TravelExpenseWrapper data)
        {
            if (data == null)
                return BadRequest("Data is null");

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    int empId = Convert.ToInt32(HttpContext.Session.GetString("CODE"));
                    var sessionData = _globalVariableService.GetGlobalVariables();

                    string compCode = sessionData.PubCompCode;
                    string yearCode = sessionData.PubFYearCode;
                    int branchCode = 1;
                    string vType = "TRVR";

                    int nextVNo = GetNextVNo(con, compCode, yearCode, branchCode, vType);

                    // Insert into TravelRequest1
                    using (SqlCommand cmd = new SqlCommand("sp_InsertTravelRequest", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@comp_code", compCode);
                        cmd.Parameters.AddWithValue("@year_code", yearCode);
                        cmd.Parameters.AddWithValue("@branch_code", branchCode);
                        cmd.Parameters.AddWithValue("@v_type", vType);
                        cmd.Parameters.AddWithValue("@v_no", nextVNo);

                        cmd.Parameters.AddWithValue("@EmpID", empId);
                        cmd.Parameters.AddWithValue("@EmployeeName", data.Employee?.Trim());
                        cmd.Parameters.AddWithValue("@TravelFrom", data.From?.Trim());
                        cmd.Parameters.AddWithValue("@TravelTo", data.To?.Trim());
                        cmd.Parameters.AddWithValue("@TravelDate", data.TravelDate);
                        cmd.Parameters.AddWithValue("@TotalCost", data.Cost ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(data.Purpose) ? (object)DBNull.Value : data.Purpose.Trim());
                        cmd.Parameters.AddWithValue("@Status", DBNull.Value);
                        cmd.Parameters.AddWithValue("@TravelType", data.TravelType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Active", DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Journeytype", "1");

                        cmd.Parameters.AddWithValue("@UUSER", sessionData.PubUserName);
                        cmd.Parameters.AddWithValue("@EUSER", "");
                        cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@LIP", sessionData.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", sessionData.PubUserName);

                        cmd.ExecuteNonQuery();
                    }

                    // Insert journey details
                    if (data.RequestType == "Wrapper" && data.TravelDetails != null)
                    {
                        int? SRNO = null;
                        //string fetchQuery = "SELECT TOP 1 SRNO FROM TravelRequest1 ";
                        string fetchQuery = "SELECT TOP 1 SRNO FROM TravelRequest1 WHERE EmpID = @EmpID ORDER BY SRNO DESC";
                        using (SqlCommand fetchCmd = new SqlCommand(fetchQuery, con))
                        {
                            fetchCmd.Parameters.AddWithValue("@EmpID", empId);
                            var result = fetchCmd.ExecuteScalar();
                            if (result != null)
                                SRNO = Convert.ToInt32(result);
                        }

                        if (SRNO.HasValue)
                        {
                            foreach (var item in data.TravelDetails)
                            {
                                InsertJourneyDetail(item, SRNO.Value, nextVNo, con);
                            }
                        }
                    }

                    return Json(new { success = true, message = "Travel details saved successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // 🔧 Auto-increment function
        private int GetNextVNo(SqlConnection con, string compCode, string yearCode, int branchCode, string vType)
        {
            string query = @"
        SELECT ISNULL(MAX(v_no), 252600015) + 1 
        FROM TravelRequest1 
        WHERE comp_code = @comp_code AND year_code = @year_code AND branch_code = @branch_code AND v_type = @v_type";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@comp_code", compCode);
                cmd.Parameters.AddWithValue("@year_code", yearCode);
                cmd.Parameters.AddWithValue("@branch_code", branchCode);
                cmd.Parameters.AddWithValue("@v_type", vType);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
        private void InsertJourneyDetail(RequestforTravel item, int SRNO, int nextVNo, SqlConnection con)
        {
            if (item == null ||
                item.FromOneWay == null &&
                item.ToOneWay == null &&
                item.TravelDate == null &&
                item.ExpenseCategoryMaster == null &&
                (item.Cost == null || item.Cost == 0) &&
                item.TransportationModeMaster == null &&
                string.IsNullOrWhiteSpace(item.Purpose))
            {
                return;
            }

            var session = _globalVariableService.GetGlobalVariables();
            using (SqlCommand cmd = new SqlCommand("sp_InsertTravelRequest2", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@comp_code", session.PubCompCode);
                cmd.Parameters.AddWithValue("@year_code", session.PubFYearCode);
                cmd.Parameters.AddWithValue("@branch_code", 1);
                cmd.Parameters.AddWithValue("@v_type", "TRVR");
                cmd.Parameters.AddWithValue("@v_no", nextVNo);
                cmd.Parameters.AddWithValue("@sno", SRNO);
                cmd.Parameters.AddWithValue("@TravelFrom", (object?)item.FromOneWay ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TravelTo", (object?)item.ToOneWay ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TravelDate", item.TravelDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ExpenseCategoryMaster", (object?)item.ExpenseCategoryMaster ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TotalCost", item.Cost.HasValue && item.Cost.Value != 0 ? item.Cost.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@TransportMode", (object?)item.TransportationModeMaster ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(item.Purpose) ? DBNull.Value : item.Purpose.Trim());
                cmd.Parameters.AddWithValue("@TravelType", (object?)item.TransportationModeMaster ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Journeytype", "1"); 
                cmd.Parameters.AddWithValue("@UUSER", session.PubUserName);
                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now); 
                cmd.Parameters.AddWithValue("@EUSER", session.PubUserName);
                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                cmd.Parameters.AddWithValue("@LIP", session.PubLocalId);
                cmd.Parameters.AddWithValue("@LID", session.PubUserName);


                cmd.ExecuteNonQuery();
            }
        }

        [HttpGet]
        public JsonResult GetUserNameEmailAndTravelDataReturn()
        {
            string userName = HttpContext.Session.GetString("USER_NAME");
            string trimmedUserName = userName?.Trim();
            string empId = HttpContext.Session.GetString("CODE");
            if (string.IsNullOrEmpty(trimmedUserName))
            {
                return Json(new { success = false, message = "User is not logged in." });
            }
            List<object> travelRequests = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string travelQuery = @"SELECT EmployeeName, TravelFrom, TravelTo, TravelDate, TotalCost, Purpose, TravelType
                       FROM TravelRequest1 
                       WHERE EmpID = @EmpID AND Journeytype = 2";

                using (SqlCommand cmd = new SqlCommand(travelQuery, con))
                {
                    cmd.Parameters.AddWithValue("@EmpID", HttpContext.Session.GetString("CODE")); // Make sure 'empId' is defined and contains the desired value
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string employeeName = reader["EmployeeName"].ToString();
                            string travelFrom = reader["TravelFrom"].ToString();
                            string travelTo = reader["TravelTo"].ToString();
                            DateTime travelDate = Convert.ToDateTime(reader["TravelDate"]);
                            decimal totalCost = Convert.ToDecimal(reader["TotalCost"]);
                            string purpose = reader["Purpose"].ToString();
                            string travelType = reader["TravelType"].ToString();

                            // Use these values as needed
                        }
                    }
                }
                using (SqlCommand travelCmd = new SqlCommand(travelQuery, con))
                {
                    travelCmd.Parameters.AddWithValue("@EmpID", HttpContext.Session.GetString("CODE"));
                    using (SqlDataReader travelReader = travelCmd.ExecuteReader())
                    {
                        while (travelReader.Read())
                        {
                            travelRequests.Add(new
                            {
                                EmployeeName = travelReader["EmployeeName"] != DBNull.Value ? travelReader["EmployeeName"].ToString() : "",
                                TravelFrom = travelReader["TravelFrom"] != DBNull.Value ? travelReader["TravelFrom"].ToString() : "",
                                TravelTo = travelReader["TravelTo"] != DBNull.Value ? travelReader["TravelTo"].ToString() : "",
                                TravelDate = travelReader["TravelDate"] != DBNull.Value ? Convert.ToDateTime(travelReader["TravelDate"]).ToString("yyyy-MM-dd") : "",
                                TotalCost = travelReader["TotalCost"] != DBNull.Value ? Convert.ToDecimal(travelReader["TotalCost"]) : 0,
                                Purpose = travelReader["Purpose"] != DBNull.Value ? travelReader["Purpose"].ToString() : "",
                                TravelType = travelReader["TravelType"] != DBNull.Value ? travelReader["TravelType"].ToString() : null
                            });
                        }
                    }
                }
            }
            if (travelRequests.Count > 0)
            {
                return Json(new
                {
                    success = true,
                    empId,
                    userName,
                    travelRequests
                });
            }
            else
            {
                return Json(new
                {
                    success = true,
                    empId,
                    userName
                });
            }
        }
        [HttpPost]
        public IActionResult SaveTravelExpensesReturn([FromBody] TravelExpenseWrapper data)
        {
            if (data == null)
                return BadRequest("Data is null");
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("Sp_InsertTravelRequest", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@EmpID", HttpContext.Session.GetString("CODE"));
                        cmd.Parameters.AddWithValue("@EmployeeName", data.Employee?.Trim());
                        cmd.Parameters.AddWithValue("@TravelFrom", data.From?.Trim());
                        cmd.Parameters.AddWithValue("@TravelTo", data.To?.Trim());
                        cmd.Parameters.AddWithValue("@TravelDate", data.TravelDate); 
                        cmd.Parameters.AddWithValue("@TotalCost", data.Cost ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(data.Purpose) ? DBNull.Value : data.Purpose.Trim());
                        cmd.Parameters.AddWithValue("@Status", DBNull.Value); 
                        cmd.Parameters.AddWithValue("@TravelType", data.TravelType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Active", 1);
                        cmd.Parameters.AddWithValue("@Email", HttpContext.Session.GetString("Email"));
                        cmd.Parameters.AddWithValue("@Journeytype", 2);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                            return Json(new { success = false, message = "No record found to insert for the given EmpID." });
                    }
                    // Fetch latest RequestID
                    int? requestId = null;
                    string fetchQuery = "SELECT TOP 1 RequestID FROM TravelRequest WHERE EmpID = @EmpID ORDER BY RequestID DESC";
                    using (SqlCommand fetchCmd = new SqlCommand(fetchQuery, con))
                    {
                        fetchCmd.Parameters.AddWithValue("@EmpID", HttpContext.Session.GetString("CODE"));
                        var result = fetchCmd.ExecuteScalar();
                        if (result != null)
                            requestId = Convert.ToInt32(result);
                    }
                    if (requestId != null)
                    {
                        if (data.RequestType == "Wrapper" && data.TravelDetails != null)
                        {
                            foreach (var item in data.TravelDetails)
                            {
                                InsertJourneyDetailReturn(item, requestId.Value, con);
                            }
                        }
                    }
                    return Json(new { success = true, message = "Travel details saved successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
        private void InsertJourneyDetailReturn(RequestforTravel item, int requestId, SqlConnection con)
        {
            // Check if item is null or all its properties are null/empty
            if (item == null ||
                item.FromOneWay == null &&
                 item.ToOneWay == null &&
                 item.TravelDate == null &&
                 item.ExpenseCategoryMaster == null &&
                 (item.Cost == null || item.Cost == 0) &&
                 item.TransportationModeMaster == null &&
                 string.IsNullOrWhiteSpace(item.Purpose))
            {
                return;
            }

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
        @Purpose, @TravelType, 1, GETDATE(), @Journeytype
    )", con))
            {
                insertCmd.Parameters.AddWithValue("@RequestID", requestId);
                insertCmd.Parameters.AddWithValue("@TravelFrom", (object?)item.FromOneWay ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@TravelTo", (object?)item.ToOneWay ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@TravelDate", item.TravelDate ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ExpenseCategoryMaster", (object?)item.ExpenseCategoryMaster ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@TotalCost", item.Cost.HasValue && item.Cost.Value != 0 ? item.Cost.Value : DBNull.Value);
                insertCmd.Parameters.AddWithValue("@TransportMode", (object?)item.TransportationModeMaster ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Purpose", string.IsNullOrWhiteSpace(item.Purpose) ? DBNull.Value : item.Purpose.Trim());
                insertCmd.Parameters.AddWithValue("@TravelType", DBNull.Value);
                insertCmd.Parameters.AddWithValue("@Journeytype", "2"); 

                insertCmd.ExecuteNonQuery();
            }
        }

    }
   

}
