using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    public class ApprovalStageMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public ApprovalStageMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/Setup/ApprovalStageMaster/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetDocumentTypeList()
        {
            string query = "SELECT CODE, NAME FROM DOCTYPE_MAST ORDER BY NAME ASC";
            var docTypeList = _dropdownService.GetDropdownList(query);
            return Json(docTypeList);
        }
        [HttpGet]
        public IActionResult GetUserNamesList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE, FULL_NAME FROM CONDATABASE.dbo.USER_MAST WHERE COMP_CODE = '"+ compCode + "' and ACTIVE=1 ORDER BY FULL_NAME ASC";
            var userNameList = _dropdownService.GetDropdownList(query);
            return Json(userNameList); 
        }
        [HttpGet]
        public IActionResult GetDepartmentsList()
        {
            string query = "SELECT CODE, NAME FROM DEPT_MAST ORDER BY NAME ASC";
            var departmentList = _dropdownService.GetDropdownList(query);
            return Json(departmentList);
        }
        [HttpGet]
        public IActionResult GetDesignationList()
        {
            string query = "SELECT CODE, NAME FROM DESG_MAST ORDER BY NAME ASC";
            var designationList = _dropdownService.GetDropdownList(query);
            return Json(designationList);
        }

        [HttpGet]
        public JsonResult GetDepartmentAndDesignationByUserName(int userName)
        {
            string deprt = null;
            string desig = null;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = "SELECT DESIGNATION, DEPARTMENT FROM CONDATABASE.dbo.USER_MAST WHERE CODE = @UserName";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserName", userName);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            deprt = reader["DEPARTMENT"]?.ToString();
                            desig = reader["DESIGNATION"]?.ToString();
                        }
                    }
                }
            }

            var result = new
            {
                department = deprt,
                designation = desig
            };

            return Json(result);
        }

        [HttpPost]
        public JsonResult SaveApprovalStages([FromBody] SaveApprovalStageRequest request)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DOC_APPROSTAGE", con))
                    {
                        var globalVar = _globalVariableService.GetGlobalVariables();

                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "INSERT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@DOC_CODE", request.DocCode);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = globalVar.PubWorkStationID ?? "";
                        cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = globalVar.PubLocalId ?? "";
                        cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value = Environment.MachineName;

                        DataTable tvp = new DataTable();
                        tvp.Columns.Add("COMP_CODE", typeof(int));
                        tvp.Columns.Add("DOC_CODE", typeof(string));
                        tvp.Columns.Add("USER_CODE", typeof(int));
                        tvp.Columns.Add("APPROV_USER", typeof(string));
                        tvp.Columns.Add("FLAG_A", typeof(string));
                        tvp.Columns.Add("FLAG_B", typeof(string));
                        tvp.Columns.Add("FLAG_C", typeof(string));
                        tvp.Columns.Add("FLAG_D", typeof(string));
                        tvp.Columns.Add("FLAG_E", typeof(string));
                        tvp.Columns.Add("UUSER", typeof(int));
                        tvp.Columns.Add("UDATE", typeof(DateTime));
                        tvp.Columns.Add("EUSER", typeof(int));
                        tvp.Columns.Add("EDATE", typeof(DateTime));
                        tvp.Columns.Add("AED", typeof(string));
                        tvp.Columns.Add("WSID", typeof(string));
                        tvp.Columns.Add("LIP", typeof(string));
                        tvp.Columns.Add("LID", typeof(string));
                        tvp.Columns.Add("SRNO", typeof(int));
                        tvp.Columns.Add("ACTIVE", typeof(int));


                        foreach (var item in request.DocStageList)
                        {
                            tvp.Rows.Add(
                                item.COMP_CODE,
                                item.DOC_CODE,
                                item.USER_CODE,
                                item.APPROV_USER,
                                item.FLAG_A,
                                item.FLAG_B,
                                item.FLAG_C,
                                item.FLAG_D,
                                item.FLAG_E,
                                globalVar.PubUserId,                     
                                DateTime.Now,
                                DBNull.Value,
                                DBNull.Value,
                                DBNull.Value,
                                globalVar.PubWorkStationID ?? "",
                                globalVar.PubLocalId ?? "",
                                Environment.MachineName,
                                item.SRNO,
                                item.ACTIVE
                            );
                        }

                        SqlParameter tvpParam = cmd.Parameters.AddWithValue("@DOC_STAGE_LIST", tvp);
                        tvpParam.SqlDbType = SqlDbType.Structured;

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                    return Json(new { success = true });
                }

            }
            catch (Exception ex)
            {
                // Handle error
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult IsDuplicateBranchName(string docCode)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM DOC_APPROSTAGE WHERE DOC_CODE = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", docCode ?? "");
                        
                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        return Json(new { exists = true, message = "Document already exists. You can't add new one please edit." });
                    }
                    else
                    {
                        return Json(new { exists = false, message = "" });
                    }
                }
            }
        }

    }
}
