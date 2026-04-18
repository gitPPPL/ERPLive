using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class CheckListMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CheckListMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            //return View();
            return View("~/Views/Admin/Setup/CheckListMaster/Index.cshtml");
        }
        public IActionResult GetUserList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,USER_NAME FROM CONDATABASE.dbo.USER_MAST WHERE ACTIVE = 1 ORDER BY USER_NAME ";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpPost]
        public IActionResult SaveCheckList([FromBody] CHECK_LIST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            var result = SaveOrUpdateCheckList(model, action);

            TempData["Message"] = result;

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }


        public string SaveOrUpdateCheckList(CHECK_LIST checklist, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_CheckList", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", checklist.CODE);
                    cmd.Parameters.AddWithValue("@NATURE", checklist.NATURE ?? "");
                    cmd.Parameters.AddWithValue("@CHECKLIST_NAME", checklist.CHECKLIST_NAME ?? "");
                    cmd.Parameters.AddWithValue("@TASK_NAME", checklist.TASK_NAME ?? "");
                    cmd.Parameters.AddWithValue("@RESPONSIBLE_USER", (object)checklist.RESPONSIBLE_USER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@APPROVAL_USER", (object)checklist.APPROVAL_USER ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DUE_DATE", (object)checklist.DUE_DATE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FREQUENCY_CODE", (object)checklist.FREQUENCY_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FREQUENCY", checklist.FREQUENCY ?? "");
                    cmd.Parameters.AddWithValue("@ALERT_DAYS", checklist.ALERT_DAYS);
                    cmd.Parameters.AddWithValue("@ALERT_DAYS2", checklist.ALERT_DAYS2);
                    cmd.Parameters.AddWithValue("@STATUS", checklist.STATUS ?? "");
                    cmd.Parameters.AddWithValue("@REMARKS", checklist.REMARKS ?? "");
                    cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", checklist.AED ?? "A");
                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

                    con.Open();
                    cmd.ExecuteNonQuery();

                    return "Success";
                }
            }
        }

        [HttpPost]
        public JsonResult DeleteChecklist(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CheckList", con)) 
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting this record.", error = ex.Message });
            }
        }


    }
}
