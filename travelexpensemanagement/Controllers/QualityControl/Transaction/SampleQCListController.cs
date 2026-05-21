using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class SampleQCListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public SampleQCListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DropdownService dropdownService,
            DbHelper dbHelper, ModuleService.ModuleService moduleService )
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/SampleQCList/Index.cshtml");
        }


        [HttpGet]
        public JsonResult GetQCTemperatureEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var results = new List<object>();
            int totalCount = 0;

            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                using (var con = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("usp_SampleQCRM", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Required parameters
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "QCSM"); // <-- supply it (null if not filtering)

                    // Paging + search
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    con.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new
                            {
                                SearchCode = reader["SearchCode"]?.ToString(),
                                DOC_ID = reader["DOC_ID"]?.ToString(),
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                DocTypeName = reader["DocTypeName"]?.ToString(),
                                V_NO = reader["V_NO"]?.ToString(),
                                V_DATE = reader["V_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["V_DATE"]),
                                MRN_NO = reader["MRN_NO"]?.ToString(),
                                MRN_TYPE = reader["MRN_TYPE"]?.ToString(),
                                SAMPLE_RECDBY = reader["SAMPLE_RECDBY"]?.ToString(),
                                CONTAINER_NO = reader["CONTAINER_NO"]?.ToString(),
                                PARTY_CODE = reader["PARTY_CODE"]?.ToString(),
                                PartyName = reader["PartyName"]?.ToString(),
                                QC_INCHARGE = reader["QC_INCHARGE"]?.ToString(),
                                QC_INCHARGENAME = reader["QC_INCHARGENAME"]?.ToString(),
                                CHEMIST = reader["CHEMIST"]?.ToString(),
                                CHEMISTNAME = reader["CHEMISTNAME"]?.ToString(),
                                TransportName = reader["TransportName"]?.ToString(),
                                TruckNo = reader["TruckNo"]?.ToString(),
                                RECD_QTY = reader["RECD_QTY"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["RECD_QTY"]),
                                REMARKS = reader["REMARKS"]?.ToString(),
                                DEDUCT_AMT = reader["DEDUCT_AMT"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["DEDUCT_AMT"]),
                                DEDUCT_NARR = reader["DEDUCT_NARR"]?.ToString()
                            });
                        }
                        // Read total count (2nd resultset)
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader.GetInt32(0);
                        }
                    }
                }
                return Json(new { items = results, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while fetching the QC Sample List.",
                    error = ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> GetAllDatadetails([FromBody] RequestModel request)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new GateSamepleQCRM();
            try
            {
                if (!int.TryParse(request.vNo, out int vNo))
                    return BadRequest("Invalid gate number format.");

                string strVType = "QCSM";  ///request.vType?.Length >= 4 ? request.vType.Substring(0, 4) : request.vType;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (var command = new SqlCommand("usp_SampleQCRM", con))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    //command.Parameters.AddWithValue("@V_TYPE", "INFU");
                    command.Parameters.AddWithValue("@V_TYPE", strVType);
                    //command.Parameters.AddWithValue("@V_NO", 66);
                    command.Parameters.AddWithValue("@V_NO", vNo);
                    command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    command.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    command.Parameters.AddWithValue("@ACTION", "VIEW");                 
                    await con.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // ----------- Header List -----------
                        while (await reader.ReadAsync())
                        {
                            var header = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                                header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            response.Header.Add(header);
                        }
                        // ----------- Items List (pivot result) -----------
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var item = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                    item[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                response.Items.Add(item);
                            }
                        }
                    }
                }
                return Json(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetAllItems([FromBody] RequestModel request)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new List<object>();
            try
            {
                if (!int.TryParse(request.vNo, out int vNo))
                    return BadRequest("Invalid gate number format.");

                string strVType = "QCSM";  ///request.vType?.Length >= 4 ? request.vType.Substring(0, 4) : request.vType;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (var command = new SqlCommand("usp_SampleQCRM", con))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    //command.Parameters.AddWithValue("@V_TYPE", "INFU");
                    command.Parameters.AddWithValue("@V_TYPE", strVType);
                    //command.Parameters.AddWithValue("@V_NO", 66);
                    command.Parameters.AddWithValue("@V_NO", vNo);
                    command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    command.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    command.Parameters.AddWithValue("@ACTION", "ITEMS");

                    await con.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // ----------- Header List -----------
                        while (await reader.ReadAsync())
                        {

                            var record = new
                            {
                                itemCode = reader["ItemCode"]?.ToString(),
                                Item_Name = reader["ItemName"]?.ToString(),
                                QC_CODE = reader["QC_CODE"]?.ToString(),
                                qcpid = reader["QCP_CODE"]?.ToString(),
                                Parameter = reader["Parameter"]?.ToString(),
                                Unit = reader["Unit"]?.ToString(),
                                QCP_STD = reader["QCP_STD"]?.ToString(),
                                QTY = reader["QTY"] != DBNull.Value ? Convert.ToDecimal(reader["QTY"]) : 0
                            };
                            response.Add(record);
                        }
                        // ----------- Items List (pivot result) -----------
                        
                        
                    }
                }
                return Json(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }




        [HttpPost]
        public IActionResult DeleteDocByCode(string DocNo)
        {
            try
            {
                // Add logic for deleting a document by code here.
                // For now, returning a placeholder success response.

                string vtype = "QCSM";
                var globalVar = _globalVariableService.GetGlobalVariables();

                string deleteQuery = @"DELETE FROM QC1 WHERE COMP_CODE = @CompCode AND V_TYPE = @VType AND V_NO = @VNo
                                     AND YEAR_CODE = @YearCode   " + Environment.NewLine  + 
                                    " DELETE FROM QC2 WHERE COMP_CODE = @CompCode AND V_TYPE = @VType AND V_NO = @VNo" +
                                    " AND YEAR_CODE = @YearCode ";

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@VType", vtype);
                        cmd.Parameters.AddWithValue("@VNo", DocNo);
                        cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }

                return Json(new { success = true, message = "Document deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }

        public class RequestModel
        {
            public string vNo { get; set; }
            public string vType { get; set; }
        }

    }

}
