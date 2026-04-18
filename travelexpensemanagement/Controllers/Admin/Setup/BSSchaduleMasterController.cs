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
    public class BSSchaduleMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public BSSchaduleMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/Setup/BSSchaduleMaster/Index.cshtml");
        }

         
        [HttpPost]
        public IActionResult SaveSchedule([FromBody] BS_SCH_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";


            // Check for duplicate name before insert
            if (action == "INSERT" && IsDuplicateBSSchaduleName(model.NAME))
            {
                return Json(new { success = false, message = "Schadule name already exists." });
            }

            var result = SaveOrUpdateSchedule(model, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }


        [HttpPost]
        public string SaveOrUpdateSchedule(BS_SCH_MAST schedule, string action)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BS_SCH_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var globalVar = _globalVariableService.GetGlobalVariables();

                        cmd.Parameters.Add("@Action", SqlDbType.NVarChar).Value = action;
                        //cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalVar.PubCompCode;
                        cmd.Parameters.Add("@CODE", SqlDbType.Int).Value = schedule.CODE;
                        cmd.Parameters.Add("@NAME", SqlDbType.NVarChar, 200).Value = schedule.NAME ?? "";
                        cmd.Parameters.Add("@SORT_SR_NO", SqlDbType.Int).Value = schedule.SORT_SR_NO;

                        cmd.Parameters.Add("@SCH_NO", SqlDbType.NVarChar, 3).Value = ""; // Since SCH_NO isn't used in the form

                        cmd.Parameters.Add("@ACTIVE", SqlDbType.Int).Value = schedule.ACTIVE;

                        cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;

                        cmd.Parameters.Add("@EUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;

                        cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = schedule.AED ?? "A";
                        cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = globalVar.PubWorkStationID ?? "WEB";
                        cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = globalVar.PubLocalId ?? "127.0.0.1";
                        cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value = Environment.MachineName;

                        con.Open();
                        cmd.ExecuteNonQuery();

                        return "Success";
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return $"SQL Error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [HttpPost]
        public JsonResult DeleteBsSchaduleMasterByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BS_SCH_MAST", con)) 
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        //cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting record.", error = ex.Message });
            }
        }

        private bool IsDuplicateBSSchaduleName(string schaduleName)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM BS_SCH_MAST WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", schaduleName ?? "");

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }


    }
}
