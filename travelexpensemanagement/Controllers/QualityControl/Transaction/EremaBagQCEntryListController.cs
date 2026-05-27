
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;


namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]
    public class EremaBagQCEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public EremaBagQCEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/EremaBagQCEntryList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetList(string? searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var globalVars = _globalVariableService.GetGlobalVariables();
                var entries = new List<EremaBagQCEntryModel>();
                int totalCount = 0;

                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_EremaBagQCEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm",
                    string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVars.PubCompCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVars.PubFYearCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVars.PubBranchCode);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = new EremaBagQCEntryModel
                            {
                                Header = new EremaBagQCEntry_Header
                                {
                                    DOC_ID = reader["doc_id"] as string,
                                    V_TYPE = reader["v_type"] as string,
                                    V_NO = reader["v_no"] as int?,
                                    V_DATE = reader["v_date"] as DateTime?,
                                    SHIFT = reader["shift"] as string,
                                    REMARKS = reader["remarks"] as string,
                                    EnmpName = reader["Employee"] as string,
                                    Place = reader["Place"] as string
                                },
                                Deatils = new List<EremaBagQCEntry_Details>()
                            };

                            entries.Add(model);
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] as int? ?? 0;
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    lists = entries,
                    totalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching Flakes QC Entry list",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheackConditionCpyFrm(int DeptCode, string Shifttype, DateTime v_date)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_EremaBagQCEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "CheackCondiCpyFrm");
                        cmd.Parameters.AddWithValue("@V_DATE", v_date);
                        cmd.Parameters.AddWithValue("@SHIFT", Shifttype);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", DeptCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            bool hasRows = rdr.HasRows;
                            return Json(new { success = hasRows });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new {  success = false, message = "Error fetching data", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDataCopyForm(int DeptCode, string Shifttype, DateTime v_date)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_EremaBagQCEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;              
                        cmd.Parameters.AddWithValue("@Action", "GETCOPYDATA");
                        cmd.Parameters.AddWithValue("@V_DATE", v_date);
                        cmd.Parameters.AddWithValue("@shift", Shifttype);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", DeptCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();

                            while (await rdr.ReadAsync())
                            {
                                var result = new
                                {
                                    BagNo = rdr["BagNo"]?.ToString(),
                                    ItemName = rdr["ITEM_NAME"]?.ToString(),
                                    ProdPlace = rdr["ProdPlace"]?.ToString(),
                                    LotNo = rdr["LOT_NO"]?.ToString(),
                                    WBQty = rdr["WB_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_QTY"]) : 0,
                                    GrossQty = rdr["GROSS_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["GROSS_QTY"]) : 0,
                                    TareQty = rdr["TARE_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["TARE_QTY"]) : 0,
                                    Qty = rdr["QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["QTY"]) : 0,
                                    VType = rdr["v_type"]?.ToString(),
                                    VNo = rdr["v_no"] != DBNull.Value ? Convert.ToInt32(rdr["v_no"])  : 0,
                                    ItemCode = rdr["item_code"] != DBNull.Value ? Convert.ToInt32(rdr["item_code"]) : 0,
                                    DeptCode = rdr["deptcode"] != DBNull.Value  ? Convert.ToInt32(rdr["deptcode"]) : 0
                                };

                                results.Add(result);
                            }

                            if (results.Any())
                            {
                                return Json(new {  success = true,  message = "Data fetched successfully",  data = results });
                            }
                            else
                            {
                                return Json(new { success = false, message = "No data found" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            var resultWrapper = new EremaBagQCEntryModel
            {
                Header = new EremaBagQCEntry_Header(),
                Deatils = new List<EremaBagQCEntry_Details>()
            };

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_EremaBagQCEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@searchOption", "Header");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "ERQC");

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                resultWrapper.Header = new EremaBagQCEntry_Header
                                {
                                    DOC_ID = rdr["doc_id"]?.ToString(),
                                    V_NO = rdr["v_no"] != DBNull.Value ? Convert.ToInt32(rdr["v_no"]) : 0,
                                    V_DATE = rdr["v_date"] != DBNull.Value ? Convert.ToDateTime(rdr["v_date"]) : DateTime.MinValue,
                                    QCTIME = rdr["QCTIME"]?.ToString(),
                                    SHIFT = rdr["shift"]?.ToString(),
                                    QC_INCHARGE = rdr["QC_INCHARGE"] != DBNull.Value ? Convert.ToInt32(rdr["QC_INCHARGE"]) : 0,
                                    CHEMIST = rdr["CHEMIST"] != DBNull.Value ? Convert.ToInt32(rdr["CHEMIST"]) : 0,
                                    EMP_CODE = rdr["emp_code"] != DBNull.Value ? Convert.ToInt32(rdr["emp_code"]) : 0,
                                    PLACE_CODE = rdr["place_code"] != DBNull.Value ? Convert.ToInt32(rdr["place_code"]) : 0,
                                    REMARKS = rdr["remarks"]?.ToString()
                                };
                            }
                        }
                    }

                    // --------- Second Call: Fetch Details (PREQUEST2)
                    using (SqlCommand cmd2 = new SqlCommand("sp_FlakesQCEntry", con))
                    {
                        cmd2.CommandType = CommandType.StoredProcedure;
                        cmd2.Parameters.AddWithValue("@Action", "ShowData");
                        cmd2.Parameters.AddWithValue("@searchOption", "table");
                        cmd2.Parameters.AddWithValue("@V_NO", code);
                        cmd2.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd2.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd2.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd2.Parameters.AddWithValue("@V_TYPE", "ERQC");
                        using (SqlDataReader rdr = cmd2.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                resultWrapper.Deatils.Add(new EremaBagQCEntry_Details
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    Item_Name = rdr["ITEM_NAME"]?.ToString(),
                                    BatchNo = rdr["PTYPE_NAME"]?.ToString(),
                                    BagNo = rdr["WIDTH"] != DBNull.Value ? Convert.ToDecimal(rdr["WIDTH"]) : 0,
                                    WBWt = rdr["GRAM"] != DBNull.Value ? Convert.ToDecimal(rdr["GRAM"]) : 0,
                                    GrWt = rdr["RESULT1"] != DBNull.Value ? Convert.ToDecimal(rdr["RESULT1"]) : 0,
                                    TrWt = rdr["RESULT2"] != DBNull.Value ? Convert.ToDecimal(rdr["RESULT2"]) : 0,
                                    NET_WT = rdr["PRKG"] != DBNull.Value ? Convert.ToDecimal(rdr["PRKG"]) : 0,
                                    WASTE = rdr["WASTE"] != DBNull.Value ? Convert.ToDecimal(rdr["WASTE"]) : 0,
                                    DNR = rdr["DNR"]?.ToString(),
                                    PC_LOWMELT = rdr["PC_LOWMELT"] != DBNull.Value ? Convert.ToDecimal(rdr["PC_LOWMELT"]) : 0,
                                    CPRDN = rdr["CPRDN"] != DBNull.Value ? Convert.ToDecimal(rdr["CPRDN"]) : 0,
                                    TIME1_WIDTH = rdr["TIME1_WIDTH"] != DBNull.Value ? Convert.ToDecimal(rdr["TIME1_WIDTH"]) : 0,
                                    TIME2_WIDTH = rdr["TIME2_WIDTH"] != DBNull.Value ? Convert.ToDecimal(rdr["TIME2_WIDTH"]) : 0,
                                    TIME3_WIDTH = rdr["TIME3_WIDTH"] != DBNull.Value ? Convert.ToDecimal(rdr["TIME3_WIDTH"]) : 0,
                                    TIME4_WIDTH = rdr["TIME4_WIDTH"] != DBNull.Value ? Convert.ToDecimal(rdr["TIME4_WIDTH"]) : 0,
                                    TIME5_WIDTH = rdr["TIME5_WIDTH"] != DBNull.Value ? Convert.ToDecimal(rdr["TIME5_WIDTH"]) : 0,
                                    GLUE_CONTENT = rdr["GLUE_CONTENT"] != DBNull.Value ? Convert.ToDecimal(rdr["GLUE_CONTENT"]) : 0,
                                    OTHERS = rdr["OTHERS"] != DBNull.Value ? Convert.ToDecimal(rdr["OTHERS"]) : 0,
                                    OTHERP = rdr["OTHERP"] != DBNull.Value ? Convert.ToDecimal(rdr["OTHERP"]) : 0,
                                    GRADE = rdr["GRADE"]?.ToString(),
                                    YELLOWP = rdr["YELLOWP"] != DBNull.Value ? Convert.ToDecimal(rdr["YELLOWP"]) : 0,
                                    BLUEP = rdr["BLUEP"] != DBNull.Value ? Convert.ToDecimal(rdr["BLUEP"]) : 0,
                                    COLOR_CODE = rdr["COLOR_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COLOR_CODE"]) : 0,
                                    COLOR_NAME = rdr["COLOR_NAME"]?.ToString(),
                                    YELLOW160C = rdr["YELLOW160C"] != DBNull.Value ? Convert.ToDecimal(rdr["YELLOW160C"]) : 0,
                                    MOISTURE = rdr["MOISTURE"] != DBNull.Value ? Convert.ToDecimal(rdr["MOISTURE"]) : 0,
                                    BULKDENSITY = rdr["BULKDENSITY"] != DBNull.Value ? Convert.ToDecimal(rdr["BULKDENSITY"]) : 0,
                                    PH_FLAKES = rdr["PH_FLAKES"] != DBNull.Value ? Convert.ToDecimal(rdr["PH_FLAKES"]) : 0,
                                    OVERSIZED = rdr["OVERSIZED"] != DBNull.Value ? Convert.ToDecimal(rdr["OVERSIZED"]) : 0,
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    REfType = rdr["PORD_TYPE"]?.ToString(),                             
                                    Refcode = rdr["PORD_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PORD_NO"]) : 0
                                });
                            }
                        }
                    }

                }

                return Json(new { success = true, data = resultWrapper });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching purchase requisition data", error = ex.Message });
            }
        }

    }
}