using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Dashboard
{
    public class DashboardController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public DashboardController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper,
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
            var userName = HttpContext.Session.GetString("USER_NAME");

            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Index", "Login");
            }
            //if (HttpContext.Session.GetString("USER_NAME") == null)
            //{
            //    return RedirectToAction("Index", "Login");
            //}
            return View("Index");
        }


        //public JsonResult GetAllDashboardCount()
        //{
        //    var userId = HttpContext.Session.GetString("CODE");
        //    var compCode = HttpContext.Session.GetString("COMP_CODE");

        //    int requestCount = 0;
        //    int sendRequestCount = 0;

        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        SqlCommand cmd = new SqlCommand("GetDashboardRequestCount", con);
        //        cmd.CommandType = CommandType.StoredProcedure;

        //        cmd.Parameters.AddWithValue("@GetCODE", userId);
        //        cmd.Parameters.AddWithValue("@comp_code", compCode);

        //        con.Open();

        //        using (SqlDataReader reader = cmd.ExecuteReader())
        //        {
        //            if (reader.Read())
        //                requestCount = Convert.ToInt32(reader["TotalCount"]);

        //            if (reader.NextResult() && reader.Read())
        //                sendRequestCount = Convert.ToInt32(reader["SendTotalCount"]);
        //        }
        //    }

        //    return Json(new { success = true, requestCount, sendRequestCount });
        //}


    }
}
