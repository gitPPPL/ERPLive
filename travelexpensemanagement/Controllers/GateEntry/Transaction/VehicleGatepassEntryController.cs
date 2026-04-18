
using Microsoft.AspNetCore.Mvc;

using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;



namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VehicleGatepassEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public VehicleGatepassEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/GateEntry/Transaction/VehicleGatepassEntry/Index.cshtml");
        }

        public JsonResult Getdata(DateTime FromDate, DateTime ToDate, string WBType, string VehicleNo)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();


            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string query = @"
                                SELECT  
                                CONCAT(a.V_type, a.V_no) AS GateNo,
                                a.V_date AS GateDate,
                                CONCAT(b.V_TYPE, b.V_no) AS WBNo,  
                                b.V_Date AS WBDate,
                                b.WB_TYPE AS WBType, 
                                c.name AS PartyName,  
                                VEHICLE_NO AS VehicleNo,  
                                a.BILL_NO,  
                                b.NET_WGT AS WBQty,
                                a.Remarks,  
                                FINAL_REM AS FinalRemarks, 
                                a.OUT_ALLOWED , a.OUT_DATE , a.OUT_TIME,a.PARTY_WBSLIPNO
                                FROM Gate1 a
                                LEFT JOIN WB1 b ON 
                                a.V_TYPE = b.Gate_TYPE AND 
                                a.V_NO = b.gate_NO AND 
                                a.COMP_CODE = b.COMP_CODE AND 
                                a.BRANCH_CODE = b.BRANCH_CODE
                                LEFT JOIN SUBGROUP_MAST c ON 
                                a.PARTY_CODE = c.code AND 
                                a.COMP_CODE = c.COMP_CODE
                                LEFT JOIN DOCTYPE_MAST d ON 
                                a.V_TYPE = d.code  
                                WHERE 
                                d.DOCTYPE = 'GateInward' AND 
                                ISNULL(b.Status, 0) <> 1 AND 
                                a.comp_code = @CompCode AND 
                                a.Branch_code = @BRANCH_CODE AND 
                                a.Year_code = @YEAR_CODE AND 
                                a.V_date BETWEEN @FromDate AND @ToDate  ";

                if (!string.IsNullOrEmpty(WBType))
                {
                    query += " AND b.WB_Type = @WBType";
                }

                if (!string.IsNullOrEmpty(VehicleNo))
                {
                    query += " AND  b.VEHICLE_NO like @VehicleNo";
                }


                query += " ORDER BY a.V_date DESC, b.WB_type, a.V_type, a.v_no DESC;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@FromDate", FromDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@ToDate", ToDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@WBType", WBType);
                    cmd.Parameters.AddWithValue("@VehicleNo", VehicleNo);
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
        public JsonResult SavedData(string ModelListJson)
        {
            var result = new { success = false, message = "Something went wrong." };
            try
            {
                // Deserialize the JSON string into your existing model list
                var modelList = JsonConvert.DeserializeObject<List<VehicleGatePassModel>>(ModelListJson);

                var globalVars = _globalVariableService.GetGlobalVariables();
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    foreach (var model in modelList)
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE GATE1 SET OUT_TIME=@OUT_TIME, OUT_DATE=@OUT_DATE, Remarks=@FinalRemarks " +
                            "WHERE Concat(V_type,V_no)  = @GateNo  AND COMP_CODE=@CompCode AND BRANCH_CODE=@BranchCode AND YEAR_CODE=@YearCode",
                            con))
                        {

                                    cmd.Parameters.AddWithValue("@OUT_TIME", DateTime.Now.ToString("HH:mm"));
                                    cmd.Parameters.AddWithValue("@OUT_DATE", DateTime.Now.ToString("yyyy-MM-dd"));
                                    cmd.Parameters.AddWithValue("@FinalRemarks", model.FinalRemarks ?? "");
                                    cmd.Parameters.AddWithValue("@GateNo", model.GateNo ?? "");
                                    cmd.Parameters.AddWithValue("@CompCode", globalVars.PubCompCode);
                                    cmd.Parameters.AddWithValue("@BranchCode", 1);
                                    cmd.Parameters.AddWithValue("@YearCode", globalVars.PubFYearCode);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                result = new { success = true, message = "Saved successfully." };
            }
            catch (Exception ex)
            {
                result = new { success = false, message = ex.Message };
            }

            return Json(result);
        }


    }
}
