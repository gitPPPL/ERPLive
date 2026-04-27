using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.Cms;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll;
using static iTextSharp.text.pdf.events.IndexEvents;
namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class ShiftScheduleController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ShiftScheduleController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/ShiftSchedule/Index.cshtml");
        }

        public JsonResult GetVNo(string Vtype)
        {
            string newV_NO = "00000";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Get PREFIXYR from YEAR_MAST table
                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    using (SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con))
                    {
                        prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";


                        string lastV_NO_Query = @"
                              SELECT MAX(CAST(V_NO AS INT)) 
                              FROM PAY_SHIFT_SCH 
                              WHERE COMP_CODE = @CompCode 
                              AND YEAR_CODE = @YearCode 
                              AND BRANCH_CODE = @BranchCode 
                              AND V_TYPE = @Vtype";

                        using (SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con))
                        {
                            lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                            lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                            lastVnoCmd.Parameters.AddWithValue("@BranchCode", 1);
                            lastVnoCmd.Parameters.AddWithValue("@Vtype", Vtype);

                            object result = lastVnoCmd.ExecuteScalar();

                            if (result != DBNull.Value && result != null)
                            {
                                int lastV_NO = Convert.ToInt32(result);
                                newV_NO = (lastV_NO + 1).ToString("D5");
                            }
                            else
                            {
                                newV_NO = prefixYR + "00001";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }

        public JsonResult DDlDeptType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from DOCTYPE_MAST where DOCTYPE= 'ShiftSchedule' ORDER  BY NAME";
                var DDlDeptType = _dropdownService.GetDropdownList(query);
                return Json(DDlDeptType);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDataCopyForm()
        {
            try
            {
                var globalVars = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string query = @"
                        SELECT DISTINCT 
                        PSS.V_TYPE, 
                        DTY.NAME AS DOCTYPE_NAME, 
                        PSS.V_NO, 
                        PSS.V_DATE, 
                        PSS.DOC_ID 
                        FROM PAY_SHIFT_SCH PSS 
                        LEFT JOIN DOCTYPE_MAST DTY ON DTY.CODE = PSS.V_TYPE 
                        WHERE 
                        PSS.COMP_CODE = @compCode AND 
                        PSS.BRANCH_CODE = @branchCode AND 
                        PSS.YEAR_CODE = @yearCode 
                        ORDER BY PSS.V_NO";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@compCode", globalVars.PubCompCode);
                        cmd.Parameters.AddWithValue("@branchCode", globalVars.PubBranchCode);
                        cmd.Parameters.AddWithValue("@yearCode", globalVars.PubFYearCode);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();

                            while (await reader.ReadAsync())
                            {
                                var item = new
                                {
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    DOCTYPE_NAME = reader["DOCTYPE_NAME"]?.ToString(),
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToDecimal(reader["V_NO"]) : 0,
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd") : null
                                };

                                results.Add(item);
                            }

                            if (results.Any())
                            {
                                return Json(new
                                {
                                    success = true,
                                    message = "Data fetched successfully",
                                    data = results
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "No data found"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching data",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        public JsonResult DDlEmployee()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT code,name FROM EMP_MAST WHERE COMP_CODE="+ getdata.PubCompCode +" and ACTIVE = 1 ";
                var DDlEmployee = _dropdownService.GetDropdownList(query);
                return Json(DDlEmployee);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDataCopyMainForm([FromQuery] List<string> doc_id)
        {
            try
            {
                var globalVars = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();


                    var docIdParams = doc_id.Select((id, index) => $"@doc_id{index}").ToList();
                    var inClause = string.Join(", ", docIdParams);

                    string query = $@"
                        SELECT
                        V_TYPE,
                        DTY.NAME AS DOCTYPE_NAME,
                        V_NO,
                        V_DATE,
                        DOC_ID,
                        PSS.EMP_CODE,
                        EM.NAME AS EMP_NAME,
                        SR, S1, S2, S3, S4, S5, S6, S7, S8, S9, S10, S11, S12, S13, S14, S15, S16,
                        S17, S18, S19, S20, S21, S22, S23, S24, S25, S26, S27, S28, S29, S30, S31,
                        off1, off2, off3, off4, off5,
                        PSS.UUSER,
                        PSS.UDATE
                        FROM [PAY_SHIFT_SCH] PSS
                        LEFT JOIN DOCTYPE_MAST DTY ON DTY.CODE = PSS.V_TYPE
                        LEFT JOIN EMP_MAST EM ON EM.CODE = PSS.EMP_CODE AND PSS.COMP_CODE = EM.COMP_CODE
                        WHERE PSS.DOC_ID IN ({inClause})
                        AND PSS.COMP_CODE = @compCode
                        AND PSS.BRANCH_CODE = @branchCode
                        AND PSS.YEAR_CODE = @yearCode
                        ORDER BY PSS.EMP_CODE, PSS.SR";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {

                        for (int i = 0; i < doc_id.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@doc_id{i}", doc_id[i]);
                        }


                        cmd.Parameters.AddWithValue("@compCode", globalVars.PubCompCode);
                        cmd.Parameters.AddWithValue("@branchCode", globalVars.PubBranchCode);
                        cmd.Parameters.AddWithValue("@yearCode", globalVars.PubFYearCode);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();

                            while (await reader.ReadAsync())
                            {
                                var item = new
                                {
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    DOCTYPE_NAME = reader["DOCTYPE_NAME"]?.ToString(),
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToDecimal(reader["V_NO"]) : 0,
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd") : null,
                                    EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToDecimal(reader["EMP_CODE"]) : 0,
                                    EMP_NAME = reader["EMP_NAME"]?.ToString(),
                                    SR = reader["SR"]?.ToString(),
                                    S1 = reader["S1"]?.ToString(),
                                    S2 = reader["S2"]?.ToString(),
                                    S3 = reader["S3"]?.ToString(),
                                    S4 = reader["S4"]?.ToString(),
                                    S5 = reader["S5"]?.ToString(),
                                    S6 = reader["S6"]?.ToString(),
                                    S7 = reader["S7"]?.ToString(),
                                    S8 = reader["S8"]?.ToString(),
                                    S9 = reader["S9"]?.ToString(),
                                    S10 = reader["S10"]?.ToString(),
                                    S11 = reader["S11"]?.ToString(),
                                    S12 = reader["S12"]?.ToString(),
                                    S13 = reader["S13"]?.ToString(),
                                    S14 = reader["S14"]?.ToString(),
                                    S15 = reader["S15"]?.ToString(),
                                    S16 = reader["S16"]?.ToString(),
                                    S17 = reader["S17"]?.ToString(),
                                    S18 = reader["S18"]?.ToString(),
                                    S19 = reader["S19"]?.ToString(),
                                    S20 = reader["S20"]?.ToString(),
                                    S21 = reader["S21"]?.ToString(),
                                    S22 = reader["S22"]?.ToString(),
                                    S23 = reader["S23"]?.ToString(),
                                    S24 = reader["S24"]?.ToString(),
                                    S25 = reader["S25"]?.ToString(),
                                    S26 = reader["S26"]?.ToString(),
                                    S27 = reader["S27"]?.ToString(),
                                    S28 = reader["S28"]?.ToString(),
                                    S29 = reader["S29"]?.ToString(),
                                    S30 = reader["S30"]?.ToString(),
                                    S31 = reader["S31"]?.ToString(),

                                    off1 = reader["off1"]?.ToString(),
                                    off2 = reader["off2"]?.ToString(),
                                    off3 = reader["off3"]?.ToString(),
                                    off4 = reader["off4"]?.ToString(),
                                    off5 = reader["off5"]?.ToString(),

                                    UUSER = reader["UUSER"]?.ToString(),
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]).ToString("yyyy-MM-dd HH:mm:ss") : null
                                };

                                results.Add(item);
                            }

                            if (results.Any())
                            {
                                return Json(new
                                {
                                    success = true,
                                    message = "Data fetched successfully",
                                    data = results
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "No data found"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching data",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost]
        public IActionResult SaveData([FromBody] List<ShiftSchedule_Model> data)
        {
            if (data == null || !data.Any())
                return Json(new { success = false, message = "No data received." });

            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                using var transaction = conn.BeginTransaction();

                var docIds = data
                    .Select(x => x.H_V_TYPE + x.H_V_NO)
                    .Distinct()
                    .ToList();

                string deleteQuery = @"
                DELETE FROM PAY_SHIFT_SCH 
                WHERE DOC_ID = @DOC_ID 
                AND COMP_CODE = @COMP_CODE 
                AND BRANCH_CODE = @BRANCH_CODE 
                AND YEAR_CODE = @YEAR_CODE";

                foreach (var docId in docIds)
                {
                    using var deleteCmd = new SqlCommand(deleteQuery, conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@DOC_ID", docId);
                    deleteCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    deleteCmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    deleteCmd.ExecuteNonQuery();
                }

                foreach (var entry in data)
                {
                    using var cmd = new SqlCommand("sp_ShiftSchedule", conn, transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@Action", "save");
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", entry.H_V_TYPE + entry.H_V_NO);
                    cmd.Parameters.AddWithValue("@V_NO", entry.H_V_NO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@V_DATE", entry.H_V_DATE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@V_TYPE", entry.H_V_TYPE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@EMP_CODE", entry.EMP_CODE ?? (object)DBNull.Value);

                    // S1 to S31
                    for (int i = 1; i <= 31; i++)
                    {
                        var prop = entry.GetType().GetProperty($"S{i}");
                        var value = prop?.GetValue(entry) ?? (object)DBNull.Value;
                        cmd.Parameters.AddWithValue($"@S{i}", value);
                    }

                    // OFF1 to OFF5
                    cmd.Parameters.AddWithValue("@off1", entry.off1 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@off2", entry.off2 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@off3", entry.off3 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@off4", entry.off4 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@off5", entry.off5 ?? (object)DBNull.Value);

                    // Audit
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return Json(new { success = true, message = "Shift schedule saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving shift schedule.", error = ex.Message });
            }
        }





        [HttpGet]
        public async Task<IActionResult> GetDataImportFrom()
        {
            try
            {
                var globalVars = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();


                    string query = $@"
                     SELECT   CODE , NAME , OFFDAY FROM EMP_MAST where  COMP_CODE = @compCode  and code not in ('')
                     and RESIGN_DATE <> '' and active = 1  ";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                                             

                        cmd.Parameters.AddWithValue("@compCode", globalVars.PubCompCode);
                        cmd.Parameters.AddWithValue("@branchCode", globalVars.PubBranchCode);
                        cmd.Parameters.AddWithValue("@yearCode", globalVars.PubFYearCode);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();

                            while (await reader.ReadAsync())
                            {
                                var item = new
                                {
                                    EMP_CODE = reader["CODE"] != DBNull.Value ? Convert.ToDecimal(reader["CODE"]) : 0,
                                    EMP_NAME = reader["NAME"]?.ToString(),
                                    OFFDAY = reader["OFFDAY"]?.ToString()                 
                                                            
                                };

                                results.Add(item);
                            }

                            if (results.Any())
                            {
                                return Json(new
                                {
                                    success = true,
                                    message = "Data fetched successfully",
                                    data = results
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "No data found"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching data",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }





    }
}
