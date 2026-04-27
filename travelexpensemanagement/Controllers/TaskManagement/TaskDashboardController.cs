using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Crypto;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;


namespace travelexpensemanagement.Controllers.TaskManagement
{
    public class TaskDashboardController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TaskDashboardController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper,
            ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;   
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/TaskManagement/TaskDashboard/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDEshBoardCount()
        {
            try
            {
                var usersession = _globalVariableService.GetGlobalVariables();

                var result = new Dictionary<string, object>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TodoList]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DeshBoardCount");
                        cmd.Parameters.AddWithValue("@COMP_CODE", usersession.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", usersession.PubFYearCode);
                        cmd.Parameters.Add("@YesterDay", SqlDbType.SmallDateTime)
                        .Value = usersession.PubLoginDate.Date.AddDays(-1);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime)
                        .Value = usersession.PubLoginDate.Date;

                        cmd.Parameters.AddWithValue("@UUSER", usersession.PubUserId);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            // 1️⃣ TodayCount
                            if (await rdr.ReadAsync())
                                result["TodayCount"] = rdr[0];

                            // 2️⃣ YesterDayCount
                            await rdr.NextResultAsync();
                            if (await rdr.ReadAsync())
                                result["YesterDayCount"] = rdr[0];

                            // 3️⃣ TotalActiveTask
                            await rdr.NextResultAsync();
                            if (await rdr.ReadAsync())
                                result["TotalActiveTask"] = rdr[0];

                            // 4️⃣ ASBPending
                            await rdr.NextResultAsync();
                            if (await rdr.ReadAsync())
                                result["ASBPending"] = rdr[0];

                            // 5️⃣ ASBCompleted
                            await rdr.NextResultAsync();
                            if (await rdr.ReadAsync())
                                result["ASBCompleted"] = rdr[0];

                            // 6️⃣ ASBClose
                            await rdr.NextResultAsync();
                            if (await rdr.ReadAsync())
                                result["ASBClose"] = rdr[0];

                            // 7️⃣ ATBPending
                            await rdr.NextResultAsync();
                            if (await rdr.ReadAsync())
                                result["ATBPending"] = rdr[0];

                            // 8️⃣ ATBComplete
                            await rdr.NextResultAsync();
                            if (await rdr.ReadAsync())
                                result["ATBComplete"] = rdr[0];

                            // 9️⃣ ATBPENDING
                            await rdr.NextResultAsync();
                            if (await rdr.ReadAsync())
                                result["TodayToMe"] = rdr[0];
                        }
                    }
                }

                return Json(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        public async Task<JsonResult> GetdataForBellIcon()
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
                            {"@Action", "BellIconNotification"},                        
                            {"@SubAction", "NOTIFICATION"},               
                            {"@UUSER",globalVariable.PubUserId },
                            {"@COMP_CODE",globalVariable.PubCompCode },
                            {"@BRANCH_CODE",globalVariable.PubBranchCode },
                            {"@YEAR_CODE",globalVariable.PubFYearCode }
                        };

                        var Notification = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TodoList]", parameterlist);
                                             
                        var Data = new
                        {
                            Notification = Notification                          
                        };
                        return Json(new { Success = true, data = Data });
                    }
                }
                catch (Exception er)
                {
                    return Json(new { success = false, message = er.Message });
                }
            }
        }

        public async Task<JsonResult> GetdataForBellIconCount()
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
                            {"@Action", "BellIconNotification"},
                            {"@SubAction", "NOTIFICATIONCOUNT"},
                            {"@UUSER",globalVariable.PubUserId },
                            {"@COMP_CODE",globalVariable.PubCompCode },
                            {"@BRANCH_CODE",globalVariable.PubBranchCode },
                            {"@YEAR_CODE",globalVariable.PubFYearCode }
                        };

                        var NotificationCOUNT = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TodoList]", parameterlist);

                        var Data = new
                        {
                            Notification = NotificationCOUNT
                        };
                        return Json(new { Success = true, data = Data });
                    }
                }
                catch (Exception er)
                {
                    return Json(new { success = false, message = er.Message });
                }
            }
        }

        public async Task<JsonResult> GetDashBoardSearch(string SEARCH)
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
                            {"@Action", "DashBoardSearch"},
                            {"@UUSER",globalVariable.PubUserId },
                            {"@COMP_CODE",globalVariable.PubCompCode },
                            {"@BRANCH_CODE",globalVariable.PubBranchCode },
                            {"@YEAR_CODE",globalVariable.PubFYearCode },
                            {"@TaskSearch",SEARCH}
                        };

                        var NotificationCOUNT = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TodoList]", parameterlist);

                        var Data = new
                        {
                            Notification = NotificationCOUNT
                        };
                        return Json(new { Success = true, data = Data });
                    }
                }
                catch (Exception er)
                {
                    return Json(new { success = false, message = er.Message });
                }
            }
        }

    }
}
