using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.AllDashboard
{
    public class GateEntryDashboardController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;


        public GateEntryDashboardController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            return View("~/Views/AllDashboard/GateEntryDashboard/Index.cshtml");
        }

        [HttpPost]
        public IActionResult GetData(string FromDate, string ToDate)
        {
            GateEntryDashboardCount obj = new GateEntryDashboardCount();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_GateEntryDashboardCount", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@FromDate", Convert.ToDateTime(FromDate));
                    cmd.Parameters.AddWithValue("@ToDate", Convert.ToDateTime(ToDate));

                    con.Open();

                    SqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        obj.InVehicleCount = Convert.ToInt32(dr["InVehicleCount"]);
                        obj.OutVehicleCount = Convert.ToInt32(dr["OutVehicleCount"]);
                        obj.PendingVehicleCount = Convert.ToInt32(dr["PendingVehicleCount"]);
                    }

                    con.Close();
                }
            }

            return Json(obj);
        }

        public class GateEntryDashboardCount
        {
            public int InVehicleCount { get; set; }
            public int OutVehicleCount { get; set; }
            public int PendingVehicleCount { get; set; }
        }
    }
}
