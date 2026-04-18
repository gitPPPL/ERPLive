using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TruckOutTimeUpdationController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public TruckOutTimeUpdationController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/GateEntry/Transaction/TruckOutTimeUpdation/Index.cshtml");
        }
        public IActionResult GetDocTypeList()
        {
            string query = "SELECT DISTINCT CODE,NAME FROM DOCTYPE_MAST WHERE DOCTYPE= 'GateInward' ORDER BY NAME DESC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public IActionResult GetTruckOutRecords(DateTime FromDate, DateTime ToDate, string OutType, string DocType, string VehicleNo, string QrCode, int pageNumber = 1, int pageSize = 10)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();
            var gate1List = new List<dynamic>();
            int totalCount = 0;

            try
            {
                if (FromDate < new DateTime(1753, 1, 1)) FromDate = new DateTime(1753, 1, 1);
                if (ToDate < new DateTime(1753, 1, 1)) ToDate = new DateTime(1753, 1, 1);

                int offset = (pageNumber - 1) * pageSize;

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    string query = @"SELECT 
                            a.V_TYPE,a.DOC_ID,
                            d.Name AS In_Type,
                            a.V_NO,
                            FORMAT(a.V_date, 'dd/MM/yyyy') AS V_Date,
                            a.PARTY_CODE,
                            b.Name AS PartyName,
                            c.Name AS Transport,
                            a.TRUCK_NO,
                            a.DRIVER_NAME,
                            a.V_TIME,
                            a.R_TIME,
                            FORMAT(a.OUT_DATE, 'dd/MM/yyyy') AS OUT_DATE,
                            a.OUT_TIME,
                            a.Remarks
                        FROM gate1 a
                        LEFT JOIN SUBGROUP_MAST b ON a.PARTY_CODE = b.code AND a.COMP_CODE = b.COMP_CODE
                        LEFT JOIN TRANSPORT_MAST c ON a.TRANSPORT_CODE = c.code AND a.COMP_CODE = c.COMP_CODE
                        LEFT JOIN Doctype_Mast d ON a.V_type = d.code
                        WHERE 
                            a.V_DATE BETWEEN @FromDate AND @ToDate
                            AND (@OutType IS NULL OR a.OUT_ALLOWED = @OutType)
                            AND (@DocType IS NULL OR a.V_TYPE = @DocType)
                            AND (@VehicleNo IS NULL OR @VehicleNo = '' OR a.TRUCK_NO LIKE '%' + @VehicleNo + '%')
                            AND (@QrCode IS NULL OR @QrCode = '' OR a.QRCODE_NO LIKE '%' + @QrCode + '%')
                        ORDER BY a.V_NO DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
                        
                        -- 2. Query to get total count
                        SELECT COUNT(*) 
                        FROM gate1 a
                        LEFT JOIN SUBGROUP_MAST b ON a.PARTY_CODE = b.code AND a.COMP_CODE = b.COMP_CODE
                        LEFT JOIN TRANSPORT_MAST c ON a.TRANSPORT_CODE = c.code AND a.COMP_CODE = c.COMP_CODE
                        LEFT JOIN Doctype_Mast d ON a.V_type = d.code
                        WHERE 
                            a.V_DATE BETWEEN @FromDate AND @ToDate
                            AND (@OutType IS NULL OR a.OUT_ALLOWED = @OutType)
                            AND (@DocType IS NULL OR a.V_TYPE = @DocType)
                            AND (@VehicleNo IS NULL OR @VehicleNo = '' OR a.TRUCK_NO LIKE '%' + @VehicleNo + '%')
                            AND (@QrCode IS NULL OR @QrCode = '' OR a.QRCODE_NO LIKE '%' + @QrCode + '%');
                        ";


                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDate", FromDate);
                        cmd.Parameters.AddWithValue("@ToDate", ToDate);
                        cmd.Parameters.AddWithValue("@OutType", string.IsNullOrEmpty(OutType) ? DBNull.Value : (object)OutType);
                        cmd.Parameters.AddWithValue("@DocType", string.IsNullOrEmpty(DocType) ? DBNull.Value : (object)DocType);
                        cmd.Parameters.AddWithValue("@VehicleNo", string.IsNullOrEmpty(VehicleNo) ? DBNull.Value : (object)VehicleNo);
                        cmd.Parameters.AddWithValue("@QrCode", string.IsNullOrEmpty(QrCode) ? DBNull.Value : (object)QrCode);
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // First result: paged data
                            while (reader.Read())
                            {
                                gate1List.Add(new
                                {
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    In_Type = reader["In_Type"]?.ToString(),
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    V_Date = reader["V_Date"]?.ToString(),
                                    PARTY_CODE = reader["PARTY_CODE"]?.ToString(),
                                    PartyName = reader["PartyName"]?.ToString(),
                                    Transport = reader["Transport"]?.ToString(),
                                    TRUCK_NO = reader["TRUCK_NO"]?.ToString(),
                                    DRIVER_NAME = reader["DRIVER_NAME"]?.ToString(),
                                    V_TIME = reader["V_TIME"]?.ToString(),
                                    R_TIME = reader["R_TIME"]?.ToString(),
                                    OUT_DATE = reader["OUT_DATE"]?.ToString(),
                                    OUT_TIME = reader["OUT_TIME"]?.ToString(),
                                    REMARKS = reader["REMARKS"]?.ToString()
                                });
                            }

                            // Move to next result: total count
                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = Convert.ToInt32(reader[0]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching truck out records", error = ex.Message });
            }

            return Json(new { success = true, gate1List, totalCount });
        }


        [HttpPost]
        public IActionResult SaveOutTimes([FromBody] List<TruckOutRecord> records)
        {
            if (records == null || !records.Any())
                return BadRequest("No records received.");

            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    foreach (var record in records)
                    {
                        string query = @"
                    UPDATE GATE1
                    SET OUT_DATE = @OUT_DATE,
                        OUT_TIME = @OUT_TIME,
                        EDATE = GETDATE()
                    WHERE V_NO = @V_NO
                        AND YEAR_CODE = @YEAR_CODE
                        AND COMP_CODE = @COMP_CODE
                        AND BRANCH_CODE = @BRANCH_CODE
                        AND DOC_ID = @DOC_ID";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@V_NO", record.V_NO);
                            cmd.Parameters.AddWithValue("@OUT_DATE", DateTime.Parse(record.OUT_DATE));
                            cmd.Parameters.AddWithValue("@OUT_TIME", record.OUT_TIME);
                            cmd.Parameters.AddWithValue("@DOC_ID", record.DOC_ID);

                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1); // Update if dynamic

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                return Ok(new { success = true, message = "Records updated successfully." });
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
