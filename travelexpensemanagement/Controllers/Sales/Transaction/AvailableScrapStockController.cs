using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Sales.Transaction;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class AvailableScrapStockController : Controller
    {

        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataservice;
        public AvailableScrapStockController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataservice = masterDataService;
        }
        public IActionResult Index()
        {
            TempData["loginDate"] = _globalValue.GetGlobalVariables().PubLoginDate;
            ViewBag.CurrentMenu = "Available Scrap Stock Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/AvailableScrapStock/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='AvailableScrap' ");
                return Json(new { status = true, data = Doctype });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            var dataList = await _masterDataservice.GetMaxVNoAsync(V_type, "AVAIL_SCRAPSTK1");
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetItemList()
        {
            try
            {
                var itemlist = await _dbHelper.GetJsonDataAsync($@"select CODE, NAME,HSN_CODE,UNIT_NAME,UNIT_CODE from item_mast where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode} order by NAME");
                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemSource()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync(@$"select CODE,NAME from ITEMPTYPE_MAST where comp_code={companyCd} ");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnitList()
        {
            try
            {
                var unitlist = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from ITEMUNIT_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by name");
                return Json(new { status = true, data = unitlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailScrapForUpdate(string id)
        {
            try
            {
                var vType=id.Substring(0, 4);
                var vNo=id.Substring(4);
                var usersessionDt=_globalValue.GetGlobalVariables();
                var companyCd = usersessionDt.PubCompCode;
                var yearCd = usersessionDt.PubFYearCode;
                var branchCd = 1;

                var parameter1 = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCd},
                    {"@YEAR_CODE", yearCd},
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", vType},
                    {"@V_NO", vNo},
                    {"@Action",  "AvailScrapStock1ForUpdate"}
                };
                var parameter2 = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCd},
                    {"@YEAR_CODE", yearCd},
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", vType},
                    {"@V_NO", vNo},
                    {"@Action",  "AvailScrapStock2ForUpdate"}
                };

                var headerDt = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_AvailblScrapStk]", parameter1);
                var detailDt = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_AvailblScrapStk]", parameter2);

                return Json(new {status=true, header=headerDt, detail=detailDt });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> SaveOrUpdateAvailScrapStock([FromBody] AvailScrapStk model)
        //{
        //    if (model == null)
        //        return Json(new { status = false, message = " data save failed." });
        //    try
        //    {
        //        using (var con = _dbcontext.GetErpConnection())
        //        {
        //            await con.OpenAsync();
        //            var usersessionDt = _globalValue.GetGlobalVariables();
        //            DataTable getAvailScrapTable = GetDttblAvailScrap(model.availScrapStk2list);

        //            using (var transaction = con.BeginTransaction())
        //            {
        //                bool success = true;
        //                try
        //                {
        //                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_AvailblScrapStk]", con, transaction))
        //                    {
        //                        cmd.CommandType = CommandType.StoredProcedure;
        //                        cmd.Transaction = transaction;
        //                        cmd.CommandType = CommandType.StoredProcedure;

        //                        if (model.SaveOrUpdate == "Save")
        //                            cmd.Parameters.AddWithValue("@Action", "Add");
        //                        else
        //                            cmd.Parameters.AddWithValue("@Action", "Edit");

        //                        cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
        //                        cmd.Parameters.AddWithValue("@V_NO", model.VNo);
        //                        cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
        //                        cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(model.VDate));
        //                        cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(model.DocId));
        //                        cmd.Parameters.AddWithValue("@REQUEST_BY", model.RequestBy);
        //                        cmd.Parameters.AddWithValue("@REMARKS", model.Remarks);
        //                        cmd.Parameters.AddWithValue("@STATUS", model.Status);
        //                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", model.FaProvStatus);
        //                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", model.FaProvRemarks);
        //                        cmd.Parameters.AddWithValue("@FROM_DATE", model.FromDate);
        //                        cmd.Parameters.AddWithValue("@TO_DATE", model.ToDate);                                
        //                        cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@LID", usersessionDt.PubLocalLid ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID ?? (object)DBNull.Value);                                
        //                        await cmd.ExecuteNonQueryAsync();                                
        //                    }
        //                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_AvailblScrapStk]", con, transaction))
        //                    {
        //                        cmd.CommandType = CommandType.StoredProcedure;
        //                        cmd.Transaction = transaction;
        //                        cmd.CommandType = CommandType.StoredProcedure;                                
        //                        cmd.Parameters.AddWithValue("@Action", "AddOrEdit");
        //                        cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
        //                        cmd.Parameters.AddWithValue("@V_NO", model.VNo);
        //                        cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
        //                        cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(model.VDate));
        //                        cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(model.DocId));
        //                        cmd.Parameters.AddWithValue("@ScrapStk2Data", getAvailScrapTable);
        //                        cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@LID", usersessionDt.PubLocalLid ?? (object)DBNull.Value);
        //                        cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID ?? (object)DBNull.Value);                               
        //                        await cmd.ExecuteNonQueryAsync();                               
        //                    }

        //                    if (success)
        //                        transaction.Commit();
        //                    else
        //                        transaction.Rollback();

        //                    return Json(new
        //                    {
        //                        status = success,
        //                        message = success ? "Data save/update successfully." : "Failed to save or update some employee details."
        //                    });
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction?.Rollback();
        //                    return Json(new { status = false, message = "Transaction failed: " + ex.Message });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = "Error: " + ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateAvailScrapStock([FromBody] AvailScrapStk model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid data." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    DataTable scrapTable = GetDttblAvailScrap(model.availScrapStk2list);

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            /* ================= HEADER ================= */
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_AvailblScrapStk]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@Action",
                                    model.SaveOrUpdate == "Save" ? "Add" : "Edit");

                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(model.VDate));
                                cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(model.DocId));
                                cmd.Parameters.AddWithValue("@REQUEST_BY", _dbHelper.Xnull(model.RequestBy));
                                cmd.Parameters.AddWithValue("@REMARKS", _dbHelper.Xnull(model.Remarks));
                                cmd.Parameters.AddWithValue("@STATUS", model.Status);
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", _dbHelper.Xnull(model.FaProvStatus));
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", _dbHelper.Xnull(model.FaProvRemarks));
                                cmd.Parameters.AddWithValue("@FROM_DATE", _dbHelper.Xnull(model.FromDate));
                                cmd.Parameters.AddWithValue("@TO_DATE", _dbHelper.Xnull(model.ToDate));
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);

                                await cmd.ExecuteNonQueryAsync();
                            }

                            /* ================= DETAILS ================= */
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_AvailblScrapStk]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@Action", "AddOrEdit");
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                                cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(model.VDate));
                                cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(model.DocId));
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);

                                SqlParameter tvpParam = cmd.Parameters.Add(
                                    "@ScrapStk2Data", SqlDbType.Structured);

                                tvpParam.TypeName = "dbo.Type_AvailblScrapStk";
                                tvpParam.Value = scrapTable;

                                await cmd.ExecuteNonQueryAsync();
                            }

                            transaction.Commit();

                            return Json(new
                            {
                                status = true,
                                message = "Data saved/updated successfully."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new
                            {
                                status = false,
                                message = "Transaction failed : " + ex.Message
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = "Error : " + ex.Message
                });
            }
        }


        private DataTable GetDttblAvailScrap(List<AvailScrapStk2> AvailScrapStk2s)
        {
            var dt = new DataTable();
            dt.Columns.Add("ITEM_CODE", typeof(int));
            dt.Columns.Add("ITEM_NAME", typeof(string));
            dt.Columns.Add("UNIT_CODE", typeof(int));
            dt.Columns.Add("NOS", typeof(int));
            dt.Columns.Add("QTY", typeof(decimal));
            dt.Columns.Add("REMARKS", typeof(string));
            dt.Columns.Add("GIVENTO", typeof(string));
            dt.Columns.Add("GIVENFOR", typeof(string));
            dt.Columns.Add("STATUSQ_SISTERCONCERN", typeof(int));
            dt.Columns.Add("STATUSQ_QTY", typeof(decimal));
            dt.Columns.Add("AFTER_SISTERCONCERN", typeof(int));
            dt.Columns.Add("AFTER_QTY", typeof(decimal));
            dt.Columns.Add("STATUSQ_REUSEQTY", typeof(decimal));
            dt.Columns.Add("AFTER_REUSERQTY", typeof(decimal));
            dt.Columns.Add("SOLD_QTY", typeof(decimal));
            dt.Columns.Add("HOLD_QTY", typeof(decimal));
            dt.Columns.Add("BAL_QTY", typeof(decimal));
            dt.Columns.Add("SNO", typeof(int));
            dt.Columns.Add("ITEM_TYPE", typeof(string));
            dt.Columns.Add("DEPT_CODE", typeof(int));

            // 🔹 Add rows
            foreach (var detail in AvailScrapStk2s)
            {
                dt.Rows.Add(
                    detail.ItemCode,
                    detail.ItemName,
                    detail.UnitCode,
                    detail.Nos,
                    detail.Qty,
                    detail.Remarks,
                    detail.GivenTo,
                    detail.GivenFor,
                    detail.StatusQSisterConcern,
                    detail.StatusQQty,
                    detail.AfterSisterConcern,
                    detail.AfterQty,
                    detail.StatusQReuseQty,
                    detail.AfterReuseQty,
                    detail.SoldQty,
                    detail.HoldQty,
                    detail.BalQty,
                    detail.Sno,
                    detail.ItemType,
                    detail.DeptCode
                );
            }
            return dt;
        }

    }
}

