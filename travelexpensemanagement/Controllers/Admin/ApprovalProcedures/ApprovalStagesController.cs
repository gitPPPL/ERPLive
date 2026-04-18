using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.ApprovalProcedures;
namespace travelexpensemanagement.Controllers.Admin.ApprovalProcedures
{
    public class ApprovalStagesController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ApprovalStagesController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/ApprovalProcedures/ApprovalStages/Index.cshtml");
        }
        public JsonResult DDLDocumentType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from DOCTYPE_MAST  order by name";

                var DDLDocumentType = _dropdownService.GetDropdownList(query);

                return Json(DDLDocumentType);
            }

        }
        public JsonResult DDLFullName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT a.CODE,a.FULL_NAME " +
                " from CONDATABASE.dbo.USER_MAST a " +
                " left join CONDATABASE.dbo.SUBUSER_MAST b on a.code=b.USER_CODE " +
                " where a.active=1 and b.COMP_CODE= " + getdata.PubCompCode +
                " Order by a.Full_name";

                var DDLFullName = _dropdownService.GetDropdownList(query);
                return Json(DDLFullName);
            }
        }
        [HttpGet]
        public JsonResult DDLFullNamedetails(int code)
        {
            var resultList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT a.CODE, a.FULL_NAME, a.DESIGNATION, a.DEPARTMENT, 
                    a.DEPT_CODE, a.DESG_CODE
                    FROM CONDATABASE.dbo.USER_MAST a
                    LEFT JOIN CONDATABASE.dbo.SUBUSER_MAST b 
                    ON a.CODE = b.USER_CODE
                    WHERE a.ACTIVE = 1 AND b.COMP_CODE = 1 AND a.CODE = @Code";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultList.Add(new
                            {
                                CODE = reader["CODE"],
                                FULL_NAME = reader["FULL_NAME"],
                                DESIGNATION = reader["DESIGNATION"],
                                DEPARTMENT = reader["DEPARTMENT"],
                                DEPT_CODE = reader["DEPT_CODE"],
                                DESG_CODE = reader["DESG_CODE"]
                            });
                        }
                    }
                }
            }

            return Json(resultList);
        }
        public class Form12SaveRequest
        {
            public DateTime VDate { get; set; }


            public String Action { get; set; }


            public List<ApprovalStage_Model> Data { get; set; }
        }

        [HttpPost]
        public IActionResult SaveData([FromBody] Form12SaveRequest request)
        {
            if (request.Data == null || !request.Data.Any())
                return Json(new { success = false, message = "No data received." });

            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                using var transaction = conn.BeginTransaction();

                // Assume all entries share the same DOC_CODE
                var docCode = request.Data.First().DOC_CODE;

                if (request.Action == "Insert")
                {
                    string checkSql = @"
                        SELECT COUNT(1) 
                        FROM DOC_APPROSTAGE 
                        WHERE DOC_CODE = @DOC_CODE 
                        AND COMP_CODE = @COMP_CODE;";

                    using (var checkCmd = new SqlCommand(checkSql, conn, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@DOC_CODE", docCode);

                        int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (existingCount > 0)
                        {
                            // Data already exists — rollback and return message
                            transaction.Rollback();
                            return Json(new { success = false, message = "Data already exists for this DOC_CODE." });
                        }
                    }
                }





                string deleteSql = @"
                    DELETE FROM DOC_APPROSTAGE 
                    WHERE DOC_CODE = @DOC_CODE 
                    AND COMP_CODE = @COMP_CODE;";

                using (var deleteCmd = new SqlCommand(deleteSql, conn, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@DOC_CODE", docCode);
                    deleteCmd.ExecuteNonQuery();
                } 

                // 🔹 Insert new data for each user in the request
                foreach (var entry in request.Data)
                {
                    
                    using var cmd = new SqlCommand("sp_Approval_Stage", conn, transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                   
                    cmd.Parameters.AddWithValue("@Action", "INSERT");
                    
                    cmd.Parameters.AddWithValue("@DOC_CODE", entry.DOC_CODE);
                    cmd.Parameters.AddWithValue("@USER_CODE", entry.USER_CODE);
                    cmd.Parameters.AddWithValue("@APPROV_USER", entry.APPROV_USER);
       
                    cmd.Parameters.AddWithValue("@FLAG_A", entry.FLAG_A);
                    cmd.Parameters.AddWithValue("@FLAG_B", entry.FLAG_B);
                    cmd.Parameters.AddWithValue("@FLAG_C", entry.FLAG_C);
                    cmd.Parameters.AddWithValue("@FLAG_D", entry.FLAG_D);
                    cmd.Parameters.AddWithValue("@FLAG_E", entry.FLAG_E);
                   

                    // Audit info
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();

                return Json(new { success = true, message = " Approval Stage saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving Approval Stage .", error = ex.Message });
            }
        }

    }
}
