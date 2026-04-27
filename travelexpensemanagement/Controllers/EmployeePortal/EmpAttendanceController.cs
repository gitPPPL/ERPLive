using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.EmployeePortal
{
    public class EmpAttendanceController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly DropdownService _dropdownService;
        public EmpAttendanceController(DataBaseConnection dbConnection, DropdownService dropdownService)
        {
            _dbConnection = dbConnection;
            _dropdownService = dropdownService;
        }

        public IActionResult Index()
        {
            return View("~/Views/EmployeePortal/EmpAttendance/Index.cshtml");
            //return View();
        }

        [HttpGet]
        public IActionResult GetAttendanceDetails(string month )
        {
            string compCodeStr = HttpContext.Session.GetString("COMP_CODE");
            int? empId = HttpContext.Session.GetInt32("EMP_ID");

            if (!empId.HasValue || string.IsNullOrEmpty(compCodeStr))
                return Json(new { success = false, message = "Session expired" });

            int compCode = Convert.ToInt32(compCodeStr);

            // default to current month if month param is null
            if (string.IsNullOrEmpty(month))
                month = DateTime.Now.ToString("yyyy-MM");

            List<object> list = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("usp_getEmpAttendata", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CompCode", compCode);
                    cmd.Parameters.AddWithValue("@Empcode", empId);
                    cmd.Parameters.AddWithValue("@date", month + "-01"); // convert to date

                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new
                            {
                                date = Convert.ToDateTime(dr["vdate"]).ToString("dd-MMM-yyyy"),
                                inTime = dr["Intime"].ToString(),
                                outTime = dr["OutTime"].ToString(),
                                status = dr["AttenStatus"].ToString(),
                                TotalHours = dr["TotalHours"].ToString(),
                                AHRS = dr["AHRS"].ToString(),
                                BHRS = dr["BHRS"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(list);
        }
        [HttpGet]
        public IActionResult GetMULTIPLEEMPAttendanceDetails(string month, List<int> EMPID)
        {
            string compCodeStr = HttpContext.Session.GetString("COMP_CODE");

            if (string.IsNullOrEmpty(compCodeStr))
                return Json(new { success = false, message = "Session expired" });

            int compCode = Convert.ToInt32(compCodeStr);

            if (string.IsNullOrEmpty(month))
                month = DateTime.Now.ToString("yyyy-MM");

            List<object> list = new List<object>();

            string empIds = string.Join(",", EMPID);

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("usp_getEmpAttendata", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@CompCode", compCode);
                    cmd.Parameters.AddWithValue("@Empcode", empIds);
                    cmd.Parameters.AddWithValue("@date", month + "-01");

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new
                            {
                                empId = dr["empcode"],
                                date = Convert.ToDateTime(dr["vdate"]).ToString("dd-MMM-yyyy"),
                                inTime = dr["Intime"].ToString(),
                                outTime = dr["OutTime"].ToString(),
                                status = dr["AttenStatus"].ToString(),
                                EmployeeName = dr["EmployeeName"].ToString(),
                                TotalHours = dr["TotalHours"].ToString(),
                                AHRS = dr["AHRS"].ToString(),
                                BHRS = dr["BHRS"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(list);
        }

        public JsonResult DDLUserMast()
        {
            using (SqlConnection con = _dbConnection.GetConDbConnection())
            {
                string compCode = HttpContext.Session.GetString("COMP_CODE");
                int? empId = HttpContext.Session.GetInt32("EMP_ID");
                string query = "select a.CHILD_DESG, b.USER_NAME from ORG_DESG_MAST  a left join  USER_MAST b  on b.EMP_CODE = a.CHILD_DESG where PARENT_DESG = 10463 ";

                var DDLUserMast = _dropdownService.GetDropdownList(query);

                return Json(DDLUserMast);
            }
        }
        public JsonResult CheackParentId()
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                int empId = HttpContext.Session.GetInt32("EMP_ID") ?? 0;

                string query = @"SELECT 1 
                         FROM ORG_DESG_MAST a
                         LEFT JOIN USER_MAST b ON b.EMP_CODE = a.CHILD_DESG
                         WHERE PARENT_DESG = @EmpId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    // Add parameter to avoid SQL injection
                    cmd.Parameters.AddWithValue("@EmpId", empId);

                    con.Open();
                    object result = cmd.ExecuteScalar(); 

                    bool exists = result != null;

                    return Json(new { success = exists });
                }
            }
        }
        public class EmpAttendanceVM
        {
            public DateTime VDate { get; set; }
            public long EmpCode { get; set; }
            public string InTime { get; set; }
            public string OutTime { get; set; }
            public string AttenStatus { get; set; }
            public string TotalHours { get; set; }
        }
    }
}
