using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.ModuleService;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseRequestModel;
using PurchaseDocuments = travelexpensemanagement.Models.Purchase.Transaction.PurchaseRequestModel.PurchaseDocuments;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseRequestListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly ModuleService.ModuleService _moduleService;
        public PurchaseRequestListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Transit EWwaybill";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel,
            };

            return View("~/Views/Purchase/Transaction/PurchaseRequestList/Index.cshtml", model);
        }
        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();
            var PurchaseHeader = new List<PurchaseRequestModel.Header>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_PurchaseReq1", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getvariabledata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getvariabledata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                     
                        while (reader.Read())
                        {
                            PurchaseHeader.Add(new PurchaseRequestModel.Header
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["V_TYPE"] != DBNull.Value ? reader["V_TYPE"].ToString() : string.Empty,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : DateTime.MinValue,
                                DEPT_NAME = reader["DEPT_NAME"] != DBNull.Value ? reader["DEPT_NAME"].ToString() : string.Empty,
                                DEPT_CODE = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0,
                                OWNER_NAME = reader["OWNER_NAME"] != DBNull.Value ? reader["OWNER_NAME"].ToString() : string.Empty,
                                PlaceName = reader["PlaceName"] != DBNull.Value ? reader["PlaceName"].ToString() : string.Empty,
                                OWNER_CODE = reader["OWNER_CODE"] != DBNull.Value ? Convert.ToInt32(reader["OWNER_CODE"]) : 0,
                                VALID_DATE = reader["VALID_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["VALID_DATE"]) : DateTime.MinValue,
                                TARGET_DATE = reader["TARGET_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["TARGET_DATE"]) : DateTime.MinValue,
                                REMARKS = reader["REMARKS"] != DBNull.Value ? reader["REMARKS"].ToString() : string.Empty,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching Purchase Request", error = ex.Message });
            }
            return Json(new { success = true, lists = PurchaseHeader, totalCount });
        }
        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            var resultWrapper = new PurchaseRequest_model
            {
                Header = new Header(),
                ItamDetails = new List<ItamDetails>(),
                PurchaseDocuments = new List<PurchaseDocuments>()
            };

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@searchOption", "Header");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                resultWrapper.Header = new Header
                                {
                                    DOC_ID = rdr["DOC_ID"]?.ToString(),
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                                    DEPT_NAME = rdr["DEPT_NAME"]?.ToString(),
                                    TARGET_DATE = rdr["TARGET_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["TARGET_DATE"]) : DateTime.MinValue,
                                    REASON = rdr["REASON"]?.ToString(),
                                    PLACE_CODE = rdr["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PLACE_CODE"]) : 0,
                                    PlaceName = rdr["PlaceName"]?.ToString(),
                                    URGENT_REQUEST = rdr["URGENT_REQUEST"] != DBNull.Value ? Convert.ToInt32(rdr["URGENT_REQUEST"]) : 0,
                                    OWNER_NAME = rdr["OWNER_NAME"]?.ToString(),
                                    OWNER_CODE = rdr["OWNER_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["OWNER_CODE"]) : 0,
                                    STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : 0,
                                    PLAN_NO = rdr["PLAN_NO"] != DBNull.Value ? Convert.ToInt32(rdr["PLAN_NO"]) : 0,
                                    PLAN_TYPE = rdr["PLAN_TYPE"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString()
                                };
                            }
                        }
                    }

                    // --------- Second Call: Fetch Details (PREQUEST2)
                    using (SqlCommand cmd2 = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd2.CommandType = CommandType.StoredProcedure;
                        cmd2.Parameters.AddWithValue("@Action", "SELECT");
                        cmd2.Parameters.AddWithValue("@SaveAction", "table");
                        cmd2.Parameters.AddWithValue("@V_NO", code);
                        cmd2.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd2.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd2.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd2.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                resultWrapper.ItamDetails.Add(new ItamDetails
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    ItemName = rdr["ItemName"]?.ToString(),
                                    MAKE_CODE = rdr["make_code"] != DBNull.Value ? Convert.ToInt32(rdr["make_code"]) : 0,
                                    Make = rdr["Make"]?.ToString(),
                                    TECH_DESC = rdr["TechnicalDescription"]?.ToString(),
                                    APROX_RATE = rdr["Aprox_Rate"] != DBNull.Value ? Convert.ToDecimal(rdr["Aprox_Rate"]) : 0,
                                    
                                    APROV_CODE = rdr["APROV_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["APROV_CODE"]) : 0,
                                    APROV_STATUS = rdr["APROV_STATUS"]?.ToString(),
                                    
                                    APROV_REMARKS = rdr["APROV_REMARKS"]?.ToString(),
                                    STD_REQ = rdr["STD_REQ"] != DBNull.Value ? Convert.ToDecimal(rdr["STD_REQ"]) : 0,
                                    CUR_STK = rdr["CUR_STK"] != DBNull.Value ? Convert.ToDecimal(rdr["CUR_STK"]) : 0,
                                    AVG_CONS = rdr["AVG_CONS"] != DBNull.Value ? Convert.ToDecimal(rdr["AVG_CONS"]) : 0,
                                    RESERVE_QTY = rdr["RESERVE_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["RESERVE_QTY"]) : 0,
                                    OPEN_POQTY = rdr["OPEN_POQTY"] != DBNull.Value ? Convert.ToDecimal(rdr["OPEN_POQTY"]) : 0,
                                    OPEN_RQQTY = rdr["OPEN_RQQTY"] != DBNull.Value ? Convert.ToDecimal(rdr["OPEN_RQQTY"]) : 0,
                                    USER_QTY = rdr["USER_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["USER_QTY"]) : 0,
                                    REQ_QTY = rdr["REQ_QTY"] != DBNull.Value ? Convert.ToDecimal(rdr["REQ_QTY"]) : 0,
                                    REQ_REASON = rdr["REQ_REASON"]?.ToString(),
                                    PLACE_Code = rdr["PLACE_USECODE"] != DBNull.Value ? Convert.ToInt32(rdr["PLACE_USECODE"]) : 0,
                                    PLACE_USE = rdr["PLACE_USE"]?.ToString(),
                                    
                                    PRIORITY_CODE = rdr["PRIORITY_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PRIORITY_CODE"]) : 0,
                                    PRIORITY_TYPE = rdr["PRIORITY_TYPE"]?.ToString(),
                                    
                                    SCRAP_TYPE = rdr["SCRAP_TYPE"]?.ToString(),
                                    
                                    WORK_TYPECODE = rdr["WORK_TYPECODE"] != DBNull.Value ? Convert.ToInt32(rdr["WORK_TYPECODE"]) : 0,
                                    WORK_TYPE = rdr["WORK_TYPE"]?.ToString(),
                                    
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    STATUS = rdr["STATUS"] != DBNull.Value ? Convert.ToInt32(rdr["STATUS"]) : 0
                                });
                            }
                        }
                    }

                    // --------- Third Call: Fetch Attachments (Purchase Documents)
                    using (SqlCommand cmd3 = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd3.CommandType = CommandType.StoredProcedure;
                        cmd3.Parameters.AddWithValue("@Action", "SELECT");
                        cmd3.Parameters.AddWithValue("@SaveAction", "Attachment");
                        cmd3.Parameters.AddWithValue("@V_NO", code);
                        cmd3.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd3.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd3.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd3.Parameters.AddWithValue("@V_TYPE", "STPI");

                        using (SqlDataReader rdr = cmd3.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                resultWrapper.PurchaseDocuments.Add(new PurchaseDocuments
                                {
                                    FILE_NAME = rdr["FILE_NAME"]?.ToString(),
                                    FILE_Path = rdr["FILE_Path"]?.ToString()
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
        [HttpGet]
        public async Task<IActionResult> GetDataCopyForm()
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync(); 

                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "OpenForm");
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        //cmd.Parameters.AddWithValue("@V_Type", "STAP");
                        cmd.Parameters.AddWithValue("@V_Type", "STQT");
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync()) 
                        {
                            var results = new List<object>();
                            while (await rdr.ReadAsync()) 
                            {  
                                var result = new
                                {
                                    V_NO = rdr["VNo"] != DBNull.Value ? Convert.ToInt32(rdr["VNo"]) : 0,
                                    V_TYPE = rdr["VType"]?.ToString(),
                                    V_DATE = rdr["VDate"] != DBNull.Value ? Convert.ToDateTime(rdr["VDate"]) : DateTime.MinValue,
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    ItemName = rdr["ItemName"]?.ToString(),
                                    Make = rdr["Make"]?.ToString(),
                                    TechDesc = rdr["TechDesc"]?.ToString(),
                                    Unit = rdr["Unit"]?.ToString(),
                                    Qty = rdr["Qty"] != DBNull.Value ? Convert.ToInt32(rdr["Qty"]) : 0,
                                    MakeCode = rdr["MakeCode"] != DBNull.Value ? Convert.ToInt32(rdr["MakeCode"]) : 0,
                                    UCode = rdr["UCode"]?.ToString(),
                                    TaxCode = rdr["TaxCode"] != DBNull.Value ? Convert.ToInt32(rdr["TaxCode"]) : 0
                                };

                                results.Add(result);
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
                    message = "Error fetching purchase requisition data",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDataMonthlyRequirement(int Deptid)
        {

            var GetGlobalCode = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "MonthlyReq");
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_Type", "STAP");
                        cmd.Parameters.AddWithValue("@DEPT_CODE", Deptid);
                        
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<ItamDetails>();
                            //var results = new List<object>();

                            while (await rdr.ReadAsync())
                            {
                 
                                //var result = new
                                //{
                                                 
                                //    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                //    ItemName = rdr["ItemName"]?.ToString(),
                                //    Make = rdr["Make"]?.ToString(),
                                //    Unit = rdr["Unit"]?.ToString(),
                                //    MakeCode = rdr["MakeCode"] != DBNull.Value ? Convert.ToInt32(rdr["MakeCode"]) : 0,
                                //    UCode = rdr["UCode"]?.ToString(),
                                //    currentStock = rdr["CurStk"] != DBNull.Value ? Convert.ToInt32(rdr["CurStk"]) : 0,
                                //    ReserveQty = rdr["RESERVE_QTY"] != DBNull.Value ? Convert.ToInt32(rdr["RESERVE_QTY"]) : 0,
                                //    DeptQty = rdr["DEPT_QTY"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_QTY"]) : 0

                                //};
                                var result = new ItamDetails
                                {
                                                 
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    ItemName = rdr["ItemName"]?.ToString(),
                                    Make = rdr["Make"]?.ToString(),
                                    MAKE_CODE = rdr["MakeCode"] != DBNull.Value ? Convert.ToInt32(rdr["MakeCode"]) : 0,
                                    UOM_CODE = rdr["UCode"] != DBNull.Value ? Convert.ToInt32(rdr["UCode"]) : 0,
                                    CUR_STK = rdr["CurStk"] != DBNull.Value ? Convert.ToInt32(rdr["CurStk"]) : 0,
                                    RESERVE_QTY = rdr["RESERVE_QTY"] != DBNull.Value ? Convert.ToInt32(rdr["RESERVE_QTY"]) : 0,
                                };

                                results.Add(result);
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
                    message = "Error fetching purchase requisition data",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        [HttpPost]
        public JsonResult Delete(int code)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseReq1", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = " Purchase Request deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Purchase Request.", error = ex.Message });
            }
        }


    }
}
