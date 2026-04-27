
using Microsoft.AspNetCore.Mvc;

using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Master;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VehicleGatepassEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public VehicleGatepassEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/GateEntry/Transaction/VehicleGatepassEntry/Index.cshtml");
        }

        public JsonResult ddlVehicleNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string ddlQuery = @"SELECT DISTINCT TRUCK_NO AS value, TRUCK_NO AS text 
                            FROM gate1 WHERE COMP_CODE = " + getdata.PubCompCode +
                            " AND YEAR_CODE = " + getdata.PubFYearCode + "";

                var data = _dropdownService.GetDropdownList(ddlQuery);

                return Json(data);
            }
        }

        public JsonResult Getdata(DateTime FromDate, DateTime ToDate, string WBType, string VehicleNo)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand("sp_GetGateWBData", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@FromDate", FromDate);
                    cmd.Parameters.AddWithValue("@ToDate", ToDate);

                    cmd.Parameters.AddWithValue("@WBType", string.IsNullOrEmpty(WBType) ? (object)DBNull.Value : WBType);
                    cmd.Parameters.AddWithValue("@VehicleNo", string.IsNullOrEmpty(VehicleNo) ? (object)DBNull.Value : VehicleNo);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                GateNo = reader["GateNo"].ToString(),
                                GateDate = reader["GateDate"]?.ToString(),
                                WbNo = reader["WBNo"].ToString(),
                                wbDate = reader["WBDate"].ToString(),
                                WbType = reader["WBType"].ToString(),
                                PartyName = reader["PartyName"].ToString(),
                                VehicleNo = reader["VehicleNo"].ToString(),
                                BillNo = reader["BILL_NO"].ToString(),
                                WbQty = reader["WBQty"].ToString(),
                                FinalRemarks = reader["FinalRemarks"].ToString(),
                                OutAllowed = reader["OUT_ALLOWED"].ToString(),
                                GateOut = reader["OUT_DATE"].ToString(),
                                PARTY_WBSLIPNO = reader["PARTY_WBSLIPNO"].ToString()
                            });
                        }
                    }
                }
            }
            return Json(dataList);
        }
        
        [HttpPost]
        public IActionResult SaveSingleRow([FromBody] SelectedRowModel model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data" });

            var globalVars = _globalVariableService.GetGlobalVariables();

            using (var con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (var cmd = new SqlCommand(@"UPDATE GATE1 SET OUT_ALLOWED = @OUT_ALLOWED WHERE CONCAT(V_type, V_no) = @GateNo 
                   AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode", con))
                {
                    cmd.Parameters.AddWithValue("@OUT_ALLOWED", "Yes");
                    cmd.Parameters.AddWithValue("@GateNo", model.GateNo ?? "");
                    cmd.Parameters.AddWithValue("@CompCode", globalVars.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", 1);
                    cmd.Parameters.AddWithValue("@YearCode", globalVars.PubFYearCode);
                    int rows = cmd.ExecuteNonQuery();
                    if (rows > 0)
                        return Json(new { success = true, message = "Saved successfully." });
                    else
                        return Json(new { success = false, message = "No record updated." });
                }
            }
        }
        public class SelectedRowModel
        {
            public string GateNo { get; set; }
            public string WbNo { get; set; }
            public string WbType { get; set; }
        }

    }
}
