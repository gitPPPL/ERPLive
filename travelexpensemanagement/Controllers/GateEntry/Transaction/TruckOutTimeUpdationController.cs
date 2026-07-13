using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TruckOutTimeUpdationController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public TruckOutTimeUpdationController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
        }
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/TruckOutTimeUpdation/Index.cshtml");
        }
        public IActionResult GetDocTypeList()
        {
            string query = "SELECT DISTINCT CODE,NAME FROM DOCTYPE_MAST WHERE DOCTYPE= 'GateInward' ORDER BY NAME DESC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
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

        [HttpGet]
        public JsonResult GetTruckOutRecords(
            DateTime FromDate, DateTime ToDate,
            string OutType, string DocType,
            string VehicleNo, string QrCode,
            int pageNumber = 1, int pageSize = 10)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();
            var list = new List<dynamic>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand("sp_GetTruckOutRecords", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // ✅ Required params
                    cmd.Parameters.AddWithValue("@CompCode", globelVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globelVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@FromDate", FromDate);
                    cmd.Parameters.AddWithValue("@ToDate", ToDate);

                    // ✅ FIX: Send filters properly
                    cmd.Parameters.AddWithValue("@OutType", string.IsNullOrEmpty(OutType) ? DBNull.Value : (object)OutType);
                    cmd.Parameters.AddWithValue("@DocType", string.IsNullOrEmpty(DocType) ? DBNull.Value : (object)DocType);
                    cmd.Parameters.AddWithValue("@VehicleNo", string.IsNullOrEmpty(VehicleNo) ? DBNull.Value : (object)VehicleNo);
                    cmd.Parameters.AddWithValue("@QrCode", string.IsNullOrEmpty(QrCode) ? DBNull.Value : (object)QrCode);

                    // ✅ FIX: Use PageNumber instead of Offset
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                doc_id = reader["DOC_ID"]?.ToString(),
                                v_NO = Convert.ToInt32(reader["V_NO"]),
                                in_Type = reader["In_Type"]?.ToString(),
                                v_Date = reader["V_Date"]?.ToString(),
                                partyName = reader["PartyName"]?.ToString(),
                                transport = reader["Transport"]?.ToString(),
                                trucK_NO = reader["TRUCK_NO"]?.ToString(),
                                v_TIME = reader["V_TIME"]?.ToString(),
                                r_TIME = reader["R_TIME"]?.ToString(),
                                ouT_DATE = reader["OUT_DATE"]?.ToString(),
                                ouT_TIME = reader["OUT_TIME"]?.ToString(),
                                remarks = reader["Remarks"]?.ToString(),
                                qrcodE_NO = reader["QRCode_No"]?.ToString(),
                                ouT_ALLOWED = reader["Out_Allowed"]?.ToString()
                            });
                        }

                        // ✅ Read total count (second result set)
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = Convert.ToInt32(reader[0]);
                        }
                    }
                }
            }
            return Json(new { success = true, data = list, totalCount });

        }

        [HttpPost]
        public IActionResult SaveOutTimes([FromBody] List<TruckOutRecord> records)
        {
            if (records == null || !records.Any())
                return BadRequest("No records received.");

            var globalVar = _globalVariableService.GetGlobalVariables();

            int totalUpdated = 0; //  counter

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    foreach (var record in records)
                    {
                        string query = @" UPDATE GATE1 SET OUT_DATE=@OUT_DATE, OUT_TIME=@OUT_TIME, REMARKS=@REMARKS,
                          OUT_ALLOWED=@OUT_ALLOWED,INOUT_ACTIVE='No',EDATE=GETDATE() WHERE V_NO=@V_NO AND
                          DOC_ID=@DOC_ID AND YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@V_NO", record.V_NO);
                            if (!string.IsNullOrWhiteSpace(record.OUT_DATE))
                            {
                                cmd.Parameters.AddWithValue("@OUT_DATE", DateTime.Parse(record.OUT_DATE));
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@OUT_DATE", DBNull.Value);
                            }
                            cmd.Parameters.AddWithValue("@OUT_TIME", record.OUT_TIME);
                            cmd.Parameters.AddWithValue("@DOC_ID", record.DOC_ID);
                            cmd.Parameters.AddWithValue("@REMARKS", record.remarks);
                            cmd.Parameters.AddWithValue("@OUT_ALLOWED", record.OUT_ALLOWED);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                            int rows = cmd.ExecuteNonQuery(); 
                            totalUpdated += rows;
                        }
                    }
                }
                //  CHECK HERE
                if (totalUpdated == 0)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "No data saved (No matching records found)."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"{totalUpdated} record(s) updated successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating records.",
                    details = ex.Message
                });
            }
        }
    }
}
    
