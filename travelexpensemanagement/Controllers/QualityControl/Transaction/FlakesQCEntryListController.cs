using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.ConstrainedExecution;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class FlakesQCEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly IFlakesQCEntryListRepository _flakesQCEntryListRepository;
        public FlakesQCEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        ModuleService.ModuleService moduleService , IFlakesQCEntryListRepository flakesQCEntryListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _flakesQCEntryListRepository = flakesQCEntryListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/FlakesQCEntryList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetList(string? searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var globalVars = _globalVariableService.GetGlobalVariables();
                var entries = new List<FlakesQCEntryLIst_Model>();
                int totalCount = 0;

                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_FlakesQCEntry", conn))
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
                            var model = new FlakesQCEntryLIst_Model
                            {
                                Header = new FlakesQCEntryLIst_Header
                                {
                                    DOC_ID = reader["doc_id"] as string,
                                    V_TYPE = reader["v_type"] as string,
                                    V_NO = reader["v_no"] as int?,
                                    V_DATE = reader["v_date"] as DateTime?,
                                    SHIFT = reader["shift"] as string,
                                    PLACE_CODE = reader["place_code"] as int?,
                                    EMP_CODE = reader["emp_code"] as int?,
                                    REMARKS = reader["remarks"] as string,
                                    QCTIME = reader["QCTIME"] as string,
                                    QC_INCHARGE = reader["QC_INCHARGE"] as int?,
                                    CHEMIST = reader["CHEMIST"] as int?,
                                    QC_INCHARGENAME = reader["QC_INCHARGENAME"] as string,
                                    CHEMISTNAME = reader["CHEMISTNAME"] as string,
                                    EnmpName = reader["Employee"] as string,
                                    Place = reader["Place"] as string
                                },
                                Deatils = new List<FlakesQCEntryList_Details>()
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
        public IActionResult GetDataByCode(int code)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            var resultWrapper = new FlakesQCEntryLIst_Model
            {
                Header = new FlakesQCEntryLIst_Header(),
                Deatils = new List<FlakesQCEntryList_Details>()
            };

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_FlakesQCEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@searchOption", "Header");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "SFQC");

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                resultWrapper.Header = new FlakesQCEntryLIst_Header
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
                        cmd2.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd2.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd2.Parameters.AddWithValue("@V_TYPE", "SFQC");
                        using (SqlDataReader rdr = cmd2.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                resultWrapper.Deatils.Add(new FlakesQCEntryList_Details
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
                                    YELLOW160C = rdr["YELLOW160C"] != DBNull.Value ? Convert.ToDecimal(rdr["YELLOW160C"]) : 0,
                                    MOISTURE = rdr["MOISTURE"] != DBNull.Value ? Convert.ToDecimal(rdr["MOISTURE"]) : 0,
                                    BULKDENSITY = rdr["BULKDENSITY"] != DBNull.Value ? Convert.ToDecimal(rdr["BULKDENSITY"]) : 0,
                                    PH_FLAKES = rdr["PH_FLAKES"] != DBNull.Value ? Convert.ToDecimal(rdr["PH_FLAKES"]) : 0,
                                    OVERSIZED = rdr["OVERSIZED"] != DBNull.Value ? Convert.ToDecimal(rdr["OVERSIZED"]) : 0,
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    REfType = rdr["PORD_TYPE"]?.ToString(),
                                    PlaceCode = rdr["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PLACE_CODE"]) : 0,
                                    PlaceName = rdr["placecode"]?.ToString(),
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

        [HttpDelete]
        public async Task<JsonResult> Delete(int code)
        {
            try
            {
                bool result = await _flakesQCEntryListRepository.Delete(code);

                if (result)
                {
                    return Json(new  { success = true, message = "Flakes QC Entry deleted successfully." });
                }

                return Json(new  { success = false,  message = "Unable to delete Flakes QC Entry."  });
            }
            catch (Exception ex)
            {
                return Json(new  {  success = false,  message = "Error deleting Flakes QC Entry.",  error = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult GetDataTotalppmChangge(float totalPpm, int itemCode, int depotCode , float HDPE , float PVCPPM ,
            float PCLowMelt , float Wrapper , float Metal , float Stone , float Rubber , float Glue , float Yellowp ,
            float BLUEP , float OTHERP , float YELLOW160C)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            int code = 0;
            string grd = "";
            string itemName = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                if (getGlobalCode.PubCompCode == "1")
                {
                    if (depotCode == 32 && new[] { 30012, 33366, 32398, 32507, 32618 }.Contains(itemCode))
                    {
                        if (totalPpm >= 1 && totalPpm <= 10)
                            code = 33366;
                        else if (totalPpm <= 50)
                            code = 32398;
                        else if (totalPpm <= 150)
                            code = 32507;
                        else if (totalPpm < 400)
                            code = 32618;
                        else if (totalPpm <= 1500)
                            code = 35334;
                        else if (totalPpm < 5000)
                            code = 35335;
                        else
                            code = 35336;
                    }
                    else if (depotCode == 1 && new[] { 30012, 32129, 38005, 32130 }.Contains(itemCode))
                    {
                        if (totalPpm <= 50)
                            code = 32129;
                        else if (totalPpm <= 150)
                            code = 38005;
                        else
                            code = 32130;
                    }
                }

                if (getGlobalCode.PubCompCode == "7")
                    {
                        if (itemCode !=  2171) 
                        {
                            if (totalPpm <= 50 && HDPE <= 5 && PVCPPM <= 20 && PCLowMelt == 0 && Wrapper <= 20 && Metal <= 5 &&
                              Stone == 0 && Rubber == 0 && Glue <= 12 && Yellowp <= 0.5 && BLUEP == 0 && OTHERP == 0 &&
                              YELLOW160C <= 1.6)
                            {
                                code = 3974;
                                grd = "A";
                            }

                            else if (totalPpm <= 200 && HDPE <= 10 && PVCPPM <= 100 && PCLowMelt <= 10 && Wrapper <= 60 && Metal <= 10 &&
                                Stone == 0 && Rubber <= 10 && Glue <= 12 && Yellowp <= 0.8 &&  OTHERP == 0 &&
                                YELLOW160C <= 2)
                            {
                                code = 3975;
                                grd = "B";
                            }

                            else
                            {
                                code = 3976;
                                grd = "C";
                            }

                    }
                }


                    if (code > 0)
                    {
                        string sql = "SELECT Name FROM ITEM_MAST WHERE code = @Code AND comp_code = @CompCode";
                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("@Code", code);
                            cmd.Parameters.AddWithValue("@CompCode", getGlobalCode.PubCompCode);

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                   itemName = reader["Name"].ToString();
                                }
                            }
                        }
                    }
            }

            return Json(new
            {
                ItemCode = code,
                ItemName = itemName ?? "" ,
                grd = grd ?? ""
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetDataCopyForm(int DeptCode, string Shifttype, DateTime v_date)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();

            try
            {          
                using (SqlConnection conCheck = _dbConnection.GetErpConnection())

                //using (SqlCommand cmdCheck = new SqlCommand("sp_FlakesQCEntry", conCheck))
                //{
                //    cmdCheck.CommandType = CommandType.StoredProcedure;
                //    cmdCheck.Parameters.AddWithValue("@ACTION", "CheackConditionCpyFrm");
                //    cmdCheck.Parameters.AddWithValue("@DEPT_CODE", DeptCode);
                //    cmdCheck.Parameters.AddWithValue("@SHIFT", Shifttype);
                //    cmdCheck.Parameters.AddWithValue("@v_date", v_date.Date);
                //    cmdCheck.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                //    cmdCheck.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                //    if (DeptCode == 0)
                //        cmdCheck.Parameters["@DEPT_CODE"].Value = DBNull.Value;

                //    DataTable dtCheck = new DataTable();

                //    using (SqlDataAdapter da = new SqlDataAdapter(cmdCheck))
                //    {
                //        da.Fill(dtCheck);
                //    }

                //    if (dtCheck.Rows.Count == 0)
                //    {
                //        return Json(new  { success = false, message = "Condition failed or no data found for copy form." });
                //    }
                //}
                // STEP 2: Fetch actual data
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_FlakesQCEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@DEPT_CODE", DeptCode);
                        cmd.Parameters.AddWithValue("@SHIFT", Shifttype);
                        cmd.Parameters.AddWithValue("@v_date", v_date.Date);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ACTION", "GetDataCopyForm");

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
                                    VNo = rdr["v_no"] != DBNull.Value ? Convert.ToInt32(rdr["v_no"]) : 0,
                                    ItemCode = rdr["item_code"] != DBNull.Value ? Convert.ToInt32(rdr["item_code"]) : 0,
                                    DeptCode = rdr["deptcode"] != DBNull.Value ? Convert.ToInt32(rdr["deptcode"]) : 0,
                                    DeptName = rdr["Dept"]?.ToString()
                                };

                                results.Add(result);
                            }

                            if (results.Any())
                            {
                                return Json(new { success = true, message = "Data fetched successfully",  data = results  });
                            }
                            else
                            {
                                return Json(new {  success = false,  message = "No data found" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data", error = ex.Message  });
            }
        }

        public async Task<IActionResult> DocDetailsCode(string docCode)
        {
            try
            {
                var result = await _flakesQCEntryListRepository.DocDetailsCode(docCode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching document details.", error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {
            try
            {
                var fileBytes = await _flakesQCEntryListRepository.ExportToExcel(searchTerm);
                return File( fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GateOutwardReport.xlsx" );
            }
            catch (Exception ex)
            {
                return Json(new {  success = false, message = "Error exporting excel.",  error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ExportToPdf(string searchTerm = null)
        {
            try
            {
                var fileBytes = await _flakesQCEntryListRepository.ExportToPdf(searchTerm);
                return File( fileBytes, "application/pdf", "GateOutwardReport.pdf" );
            }
            catch (Exception ex)
            {
                return Json(new { success = false,  message = "Error exporting pdf.", error = ex.Message });
            }
        }
    }
}
