using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using static travelexpensemanagement.Controllers.AllDashboard.GateEntryDashboardController;

namespace travelexpensemanagement.Controllers.AllDashboard
{
    public class GateEntryDashboardListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;


        public GateEntryDashboardListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public IActionResult Index()
        {
            return View("~/Views/AllDashboard/GateEntryDashboardList/Index.cshtml");
        }

        [HttpPost]
        public IActionResult GatedataDashboardList(string fromDate, string toDate, string type, string label)
        {
            List<GateEntryDashboardModel> list = new List<GateEntryDashboardModel>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_GateEntryDashboardDetaillist", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@FromDate", Convert.ToDateTime(fromDate));
                    cmd.Parameters.AddWithValue("@ToDate", Convert.ToDateTime(toDate));
                    cmd.Parameters.AddWithValue("@label", label);
                    cmd.Parameters.AddWithValue("@Type", type);

                    con.Open();

                    SqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        list.Add(new GateEntryDashboardModel
                        {
                            SRNO = dr["SRNO"].ToString(),
                            TRUCK_NO = dr["TRUCK_NO"].ToString(),
                            DRIVER_NAME = dr["DRIVER_NAME"].ToString(),
                            DRIVER_NO = dr["DRIVER_NO"].ToString(),
                            PURPOSE = dr["PURPOSE"].ToString(),
                            R_TIME = dr["R_TIME"].ToString(),
                            OUT_TIME = dr["OUT_TIME"].ToString()
                        });
                    }

                    con.Close();
                }
            }

            return Json(list);
        }
        public class GateEntryDashboardModel
        {
            public string SRNO { get; set; }

            public string TRUCK_NO { get; set; }

            public string DRIVER_NAME { get; set; }

            public string DRIVER_NO { get; set; }

            public string PARTY_NAME { get; set; }

            public string R_DATE { get; set; }

            public string R_TIME { get; set; }

            public string OUT_DATE { get; set; }

            public string OUT_TIME { get; set; }

            public string PURPOSE { get; set; }
        }




    }
}
