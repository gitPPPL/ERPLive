using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class InsuranceCoverNoteController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataservice;
        public InsuranceCoverNoteController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
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
            ViewBag.CurrentMenu = "Insurance Cover Note";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/InsuranceCoverNote/Index.cshtml", model);
        }


        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type= "INSU")
        {
            var dataList = await _masterDataservice.GetMaxVNoAsync(V_type, "sale3");
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetBillTypeList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("Select CODE,NAME from DOCTYPE_MAST where DOCTYPE in ('SalesInvoice','Jobworkissue','SaleChallan') order by SNO ");
                return Json(new { status = true, data = dataList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInsuranceType()
        {
            try
            {
                var companyCd=_globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME  from INSU_MAST where COMP_CODE ={companyCd} and ACTIVE = 1 ");
                return Json(new {
                    status=true,
                    data=dataList
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesOrderForUpdate(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var v_type = id.Substring(0, 4);
                var Vno = id.Substring(4);
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE",  v_type},
                    {"@V_NO", Vno},
                    {"@Action", "SalesOrderForUpdate"}
                };

                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_Sale3]", parameter);
                return Json(new { status = true, data = dataList });

            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateInsuCoverNoteMast([FromBody] Sale3model model)
        {
            if (model == null)
                return Json(new { status = false, message = "Data save failed." });

            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();                               
                var itemName = await _dbHelper.GetExecuteScalarAsync<string>($@" Select TOP 1 ITEM_NAME From SALE2 Where V_TYPE='{model.BillType}' and V_NO between {model.FBillNo} and {model.TBillNo} and Comp_code= {usersessionDt.PubCompCode}");
                
                using (var con = _dbcontext.GetErpConnection())
                {                 
                    
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("[dbo].[sp_Sale3]", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;                                                      
                            cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                            cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                            cmd.Parameters.AddWithValue("@INSU_CODE", model.InsuCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.VType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_NO", model.VNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_DATE", model.VDate ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@BILL_TYPE", model.BillType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@F_BILL_NO", model.FBillNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@T_BILL_NO", model.TBillNo ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DOC_ID", model.DocId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);                           
                            cmd.Parameters.AddWithValue("@SRNO", model.Srno ?? 1);
                            cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ITEM_NAME", itemName ?? (object)DBNull.Value);
                            await con.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                        }

                        return Json(new { status = true, message = "Data save/update successfully" });
                    }
                    catch (Exception ex)
                    {
                        return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }



    }
}
