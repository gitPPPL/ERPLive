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
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]
    public class FlexQCEntryExcruListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly IFlakesQCEntryExcluListRepository _FlakesQCEntryExcluListRepository;
        public FlexQCEntryExcruListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        ModuleService.ModuleService moduleService , IFlakesQCEntryExcluListRepository flakesQCEntryExcluListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _FlakesQCEntryExcluListRepository = flakesQCEntryExcluListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/FlexQCEntryExcruList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetList(string? searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var globalVars = _globalVariableService.GetGlobalVariables();
                var entries = new List<FlexQCEntryExcru_Model>();
                int totalCount = 0;

                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_FlexQCEntryExcru", conn))
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
                            var model = new FlexQCEntryExcru_Model
                            {
                                Header = new FlexQCEntryExcru_Header
                                {
                                    V_NO = reader["v_no"] as int?,
                                    V_TYPE = reader["v_type"] as string,
                                    V_DATE = reader["v_date"] as DateTime?,
                                    EnmpName = reader["Employee"] as string,
                                    Place = reader["Place"] as string,
                                    SHIFT = reader["Shift"] as string,
                                    REMARKS = reader["Remarks"] as string,
                                    DOC_ID = reader["DOC_ID"] as string      
                                 },


                                Deatils = new List<FlexQCEntryExcru_Details>()

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
            var resultWrapper = new FlexQCEntryExcru_Model
            {
                Header = new FlexQCEntryExcru_Header(),
                Deatils = new List<FlexQCEntryExcru_Details>()
            };

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_FlexQCEntryExcru", con))
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
                                resultWrapper.Header = new FlexQCEntryExcru_Header
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
                    using (SqlCommand cmd2 = new SqlCommand("sp_FlexQCEntryExcru", con))
                    {
                        cmd2.CommandType = CommandType.StoredProcedure;
                        cmd2.Parameters.AddWithValue("@Action", "ShowData");
                        cmd2.Parameters.AddWithValue("@searchOption", "table");
                        cmd2.Parameters.AddWithValue("@V_NO", code);
                        cmd2.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd2.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd2.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd2.Parameters.AddWithValue("@V_TYPE", "SFQC");
                        using (SqlDataReader rdr = cmd2.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                resultWrapper.Deatils.Add(new FlexQCEntryExcru_Details
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    Item_Name = rdr["ITEM_NAME"]?.ToString(),
                                    DEPT_NAME = rdr["DEPT_NAME"]?.ToString(),
                                    BatchNo = rdr["BATCH_NO"]?.ToString(),
                                    BagNo = rdr["BAG_NO"] != DBNull.Value ? Convert.ToDecimal(rdr["BAG_NO"]) : 0,
                                    DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                                    JUMBO_NO = rdr["JUMBO_NO"]?.ToString(),
                                    GrWt = rdr["GROSS_WT"] != DBNull.Value ? Convert.ToDecimal(rdr["GROSS_WT"]) : 0,
                                    WBWt = rdr["WB_WT"] != DBNull.Value ? Convert.ToDecimal(rdr["WB_WT"]) : 0,
                                    WRAPPER = rdr["WRAPPER"] != DBNull.Value ? Convert.ToDecimal(rdr["WRAPPER"]) : 0,
                                    NET_WT = rdr["NET_WT"] != DBNull.Value ? Convert.ToDecimal(rdr["NET_WT"]) : 0,
                                    TrWt = rdr["TARE_WT"] != DBNull.Value ? Convert.ToDecimal(rdr["TARE_WT"]) : 0,
                                    MFI = rdr["MFI"] != DBNull.Value ? Convert.ToDecimal(rdr["MFI"]) : 0,
                                    ASH_CONTENT = rdr["ASH_CONTENT"] != DBNull.Value ? Convert.ToDecimal(rdr["ASH_CONTENT"]) : 0,
                                    MOIS_CONTENT = rdr["MOIS_CONTENT"] != DBNull.Value ? Convert.ToDecimal(rdr["MOIS_CONTENT"]) : 0,
                                    PP = rdr["PP"] != DBNull.Value ? Convert.ToDecimal(rdr["PP"]) : 0,
                                    HD = rdr["PP"] != DBNull.Value ? Convert.ToInt32(rdr["PP"]) : 0,
                                    LD = rdr["LD"] != DBNull.Value ? Convert.ToInt32(rdr["LD"]) : 0,
                                    COLOR_MIX = rdr["COLOR_MIX"] != DBNull.Value ? Convert.ToInt32(rdr["COLOR_MIX"]) : 0,
                                    BOTTOM = rdr["BOTTOM"] != DBNull.Value ? Convert.ToInt32(rdr["BOTTOM"]) : 0,
                                    STATUS_CODE = rdr["STATUS_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS_CODE"]) : 0,
                                    STATUSS = rdr["STATUSS"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    REfType = rdr["REF_TYPE"]?.ToString(),
                                    FOAM = rdr["FOAM"] != DBNull.Value ? Convert.ToInt32(rdr["FOAM"]) : 0,
                                    RUBBER = rdr["RUBBER"] != DBNull.Value ? Convert.ToInt32(rdr["RUBBER"]) : 0,
                                    Refcode = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0
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
                bool result = await _FlakesQCEntryExcluListRepository.Delete(code);

                if (result == true)
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

                    using (SqlCommand cmd = new SqlCommand("sp_FlexQCEntryExcru", con))
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
                                    ItemName = rdr["ItemName"]?.ToString(),
                                    ProdPlace = rdr["ProdPlace"]?.ToString(),
                                    LotNo = rdr["LotNo"]?.ToString(),
                                    Jumbo_No = rdr["Jumbo_No"]?.ToString(),                 
                                    WBQty = rdr["WBQty"] != DBNull.Value ? Convert.ToDecimal(rdr["WBQty"]) : 0,
                                    GrossQty = rdr["GrossQty"] != DBNull.Value ? Convert.ToDecimal(rdr["GrossQty"]) : 0,
                                    TareQty = rdr["TareQty"] != DBNull.Value ? Convert.ToDecimal(rdr["TareQty"]) : 0,
                                    Qty = rdr["Qty"] != DBNull.Value ? Convert.ToDecimal(rdr["Qty"]) : 0,
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
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
                var result = await _FlakesQCEntryExcluListRepository.DocDetailsCode(docCode);
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
                var fileBytes = await _FlakesQCEntryExcluListRepository.ExportToExcel(searchTerm);
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
                var fileBytes = await _FlakesQCEntryExcluListRepository.ExportToPdf(searchTerm);
                return File( fileBytes, "application/pdf", "GateOutwardReport.pdf" );
            }
            catch (Exception ex)
            {
                return Json(new { success = false,  message = "Error exporting pdf.", error = ex.Message });
            }
        }
    }
}
