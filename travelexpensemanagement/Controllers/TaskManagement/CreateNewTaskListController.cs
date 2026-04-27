using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.TaskManagement_Model;

namespace travelexpensemanagement.Controllers.TaskManagement
{
    public class CreateNewTaskListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly DropdownService _dropdownService;
        public CreateNewTaskListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
           DbHelper dbHelper, ModuleService.ModuleService moduleService, DropdownService dropdownService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _dropdownService = dropdownService;
        }

        public IActionResult Index()
        {
            return View("~/Views/TaskManagement/CreateNewTaskList/Index.cshtml");
        }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            if (getvariabledata == null)
            {
                return Json(new { success = false, message = "Global variable data is null." });
            }

            int totalCount = 0;
            var headerList = new List<CreateTask_Model>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_CreateTask", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getvariabledata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getvariabledata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getvariabledata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@UUSER", getvariabledata.PubUserId);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            headerList.Add(new CreateTask_Model
                            {
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                V_NO = reader["V_no"] != DBNull.Value ? Convert.ToInt32(reader["V_no"]) : 0,
                                ASSIGN_PERSON_CODE = reader["ASSIGN_PERSON_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ASSIGN_PERSON_CODE"]) : 0,
                                CC_CODE = reader["CC_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CC_CODE"]) : 0,
                                BCC_CODE = reader["BCC_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BCC_CODE"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                START_DATETIME = reader["START_DATETIME"] != DBNull.Value ? Convert.ToDateTime(reader["START_DATETIME"]) : DateTime.MinValue,
                                END_DATETIME = reader["END_DATETIME"] != DBNull.Value ? Convert.ToDateTime(reader["END_DATETIME"]) : DateTime.MinValue,
                                TASK_SUBJECT = reader["TASK_SUBJECT"] != DBNull.Value ? reader["TASK_SUBJECT"].ToString() : string.Empty,
                                TASK_DESC = reader["TASK_DESC"] != DBNull.Value ? reader["TASK_DESC"].ToString() : string.Empty,
                                AssignedBy = reader["AssignedBy"] != DBNull.Value ? reader["AssignedBy"].ToString() : string.Empty,
                                AssignTo = reader["AssignTo"] != DBNull.Value ? reader["AssignTo"].ToString() : string.Empty,
                                PRIORITY = reader["Priority"] != DBNull.Value ? reader["Priority"].ToString() : string.Empty,
                                STATUS = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : string.Empty,

                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data.", error = ex.Message });
            }
            return Json(new { success = true, lists = headerList, totalCount });
        }

        [HttpPost]
        public JsonResult Delete(string code)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CreateTask", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_ID", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", getGlobalCode.PubBranchCode);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "TASK deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting TASK .", error = ex.Message });
            }
        }

        public async Task<JsonResult> GetDataByCode(string Code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                try
                {
                    {
                        con.Open();
                        var parameterlist = new Dictionary<string, object>
                        {
                            {"@Action", "ShowData"},
                            {"@DOC_ID", Code },
                            {"@COMP_CODE", globalVariable.PubCompCode},
                            {"@BRANCH_CODE", globalVariable.PubBranchCode},
                            {"@YEAR_CODE", globalVariable.PubFYearCode}
                        };
                        var Data = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_CreateTask]", parameterlist);
                        return Json(new { Success = true, data = Data });
                    }
                }
                catch (Exception er)
                {
                    return Json(new { success = false, message = er.Message });
                }
            }
        }

        [HttpGet]
        public IActionResult Dropdown_CCTo()
        {

            var getdata = _globalVariableService.GetGlobalVariables();

            string query = "SELECT DISTINCT  a.code,UPPER(a.Full_name) AS Full_name FROM CONDATABASE.dbo.USER_MAST AS a " +
                " WHERE a.Active = 1 and  a.COMP_CODE  = "+ getdata.PubCompCode +"    ORDER BY Full_name;";

            var moduleList = _dropdownService.GetDropdownList(query);

            return Json(moduleList);
        }

        [HttpPost]
        public IActionResult Update([FromBody] CreateTask_Model data)
        {
            if (data == null)
                return Json(new { success = false, message = "Invalid or empty data." });

         
            string result = SaveData(data);

            if (result == "Success")
                return Json(new { success = true, message = "Saved successfully!" });

            return Json(new { success = false, message = result });
        }

        private string SaveData(CreateTask_Model data)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_CreateTask", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "UPDATEDATABYLIST");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);                                    
                        cmd.Parameters.AddWithValue("@DOC_ID", "TASK" + data.V_NO);
                        cmd.Parameters.AddWithValue("@ORDER_NO", data.V_NO);
                        cmd.Parameters.AddWithValue("@ASSIGN_PERSON_CODE", data.ASSIGN_PERSON_CODE);
                        cmd.Parameters.AddWithValue("@ASSIGN_PERSON", data.ASSIGN_PERSON);
                        cmd.Parameters.AddWithValue("@CC_CODE", data.CC_CODE);
                        cmd.Parameters.AddWithValue("@CC_PERSON", data.CC_PERSON);
                        cmd.Parameters.AddWithValue("@BCC_CODE", data.BCC_CODE);                    
                        cmd.Parameters.AddWithValue("@BCC_PERSON", data.BCC_PERSON);                    
                        cmd.Parameters.Add("@START_DATETIME", SqlDbType.SmallDateTime).Value = data.START_DATETIME;
                        cmd.Parameters.Add("@END_DATETIME", SqlDbType.SmallDateTime).Value = data.END_DATETIME;
                        cmd.Parameters.AddWithValue("@STATUS", data.STATUS);
                        cmd.Parameters.AddWithValue("@PRIORITY", data.PRIORITY);
                        cmd.ExecuteNonQuery();
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [HttpPost]
        public IActionResult CloseTask([FromBody] CreateTask_Model data)
        {
            if (data == null)
                return Json(new { success = false, message = "Invalid or empty data." });
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_CreateTask", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "CLOSETASK");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@DOC_ID","TASK"  + data.V_NO);
                        cmd.Parameters.AddWithValue("@ORDER_NO", data.V_NO);
                        cmd.Parameters.AddWithValue("@STATUS", data.STATUS);            
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

    }
}
