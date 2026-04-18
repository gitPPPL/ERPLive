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
    public class BSMSchaduleMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public BSMSchaduleMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/Setup/BSMSchaduleMaster/Index.cshtml");
        }
        [HttpPost]
        public async Task<IActionResult> SaveSchedule([FromBody] BS_MSCH_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            if (action == "INSERT" && IsDuplicateBSMSchaduleName(model.NAME))
            {
                return Json(new { success = false, message = "Main Schedule name already exists." });
            }
            string result = await SaveOrUpdateSchedule(model, action);

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
        public async Task<string> SaveOrUpdateSchedule(BS_MSCH_MAST schedule, string action)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    if (action.Equals("INSERT", StringComparison.OrdinalIgnoreCase))
                    {
                        using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM BS_MSCH_MAST WHERE NAME = @NAME", con))
                        {
                            checkCmd.Parameters.AddWithValue("@NAME", _dbHelper.Xnull(schedule.NAME));
                            int exists = (int)await checkCmd.ExecuteScalarAsync();
                            if (exists > 0)
                            {
                                return "Name already exists!";
                            }
                        }
                    }
                    using (SqlCommand cmd = new SqlCommand("sp_BS_MSCH_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        var globalVar = _globalVariableService.GetGlobalVariables();
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", schedule.CODE);
                        cmd.Parameters.AddWithValue("@NAME", schedule.NAME ?? "");
                        cmd.Parameters.AddWithValue("@SORT_SRNO", schedule.SORT_SRNO);
                        cmd.Parameters.AddWithValue("@SCH_NO", schedule.SCH_NO ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", schedule.ACTIVE);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", schedule.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        await cmd.ExecuteNonQueryAsync();
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
        public JsonResult DeleteBsmSchaduleMasterByCode(int code)
        {
            //var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BS_MSCH_MAST", con)) // Use your actual SP name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        //cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Cost category deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting cost category.", error = ex.Message });
            }
        }

        private bool IsDuplicateBSMSchaduleName(string mainSchaduleName)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM BS_MSCH_MAST WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", mainSchaduleName ?? "");

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

    }
}
