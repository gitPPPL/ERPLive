using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Payroll.MonthlyTransaction;
using travelexpensemanagement.Models.QualityControl.Master;
using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesDiscountGroupController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public SalesDiscountGroupController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {           
            return View("~/Views/Sales/Transaction/SalesDiscountGroup/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetColorList()
        {
            var dataList=await _masterDataService.GetColorListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetSizeList()
        {
            var dataList=await _masterDataService.GetItemSizeMastListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetGramList()
        {
            var dataList= await _masterDataService.GetItemCatMastListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetMeshList()
        {
            var dataList= await _masterDataService.GetMeshListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesDiscountGroup()
        {
            try
            {
                var compCode = _globalValue.GetGlobalVariables().PubCompCode;

                var code= await _dbHelper.GetExecuteScalarAsync<int>( $@"select ISNULL(max(CODE),0)+1 from Disc_mast WHERE V_TYPE = 'sale' AND COMP_CODE = {compCode}" );

                string sql1 = $@" select a.code,a.name, d.COLOR_DIFF from COLOR_MAST a left join DISC_MAST d on a.CODE=d.COLOR_CODE and a.COMP_CODE=d.COMP_CODE and d.CODE={code}
                                 where a.COMP_CODE={compCode} order by a.Name ";

                string sql2 = $@"  select a.code,a.name,d.SIZE_DIFF from ITEMSIZE_MAST a left join DISC_MAST d on a.CODE=d.SIZE_CODE and a.COMP_CODE=d.COMP_CODE and d.CODE={code}
                               where a.COMP_CODE={compCode} order by a.Name";

                string sql3 = $@"  select a.code,a.name, d.GRAM_DIFF from ITEMCAT_MAST a left join DISC_MAST d on a.CODE=d.GRAM_CODE and a.COMP_CODE=d.COMP_CODE and d.CODE={code}
                                where a.COMP_CODE={compCode} order by a.Name";

                string sql4 = $@"  select a.code,a.name, d.GRAM_DIFF from MESHCONV_MAST a left join DISC_MAST d on a.CODE=d.MESH_CODE and a.COMP_CODE=d.COMP_CODE and d.CODE={code}
                                where a.COMP_CODE={compCode} order by Name";

                var ColorList = await _dbHelper.GetJsonDataAsync(sql1);
                var SizeList = await _dbHelper.GetJsonDataAsync(sql2);
                var GramList = await _dbHelper.GetJsonDataAsync(sql3);
                var MeshList = await _dbHelper.GetJsonDataAsync(sql4);
                return Json(new
                {
                    status = true,
                    ColorList = ColorList,
                    SizeList = SizeList,
                    GramList = GramList,
                    MeshList = MeshList
                });

            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesDiscountMastForUpdate(int code)
        {
            try
            {
                var companyCd =  _globalValue.GetGlobalVariables().PubCompCode;
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCd},
                    {"@V_TYPE", "SALE"},
                    {"@CODE", code},
                    {"@Action", "UpdateDiscMastData"}
                };

                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_DiscMast]", parameter);
                return Json(new { status = true, data = dataList });
            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateSalesDiscountMast([FromBody] SalesDiscountMast model)
        {
            if (model == null)
                return Json(new { status = false, message = "Data save failed." });
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var code = await _dbHelper.GetExecuteScalarAsync<int>($@"select ISNULL(max(CODE),0)+1 from Disc_mast WHERE V_TYPE = 'sale' AND COMP_CODE = {usersessionDt.PubCompCode}");
                int NewCode = 0;
                if(model.CODE > 0)
                {
                    NewCode=Convert.ToInt16(model.CODE);
                }
                else
                {
                    NewCode=code;
                }
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();            
                    bool success = true;
                    try
                    {  
                        using (SqlCommand cmd = new SqlCommand("[dbo].[sp_DiscMast]", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Add");
                            cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@V_TYPE", "SALE");
                            cmd.Parameters.AddWithValue("@CODE", NewCode);
                            cmd.Parameters.AddWithValue("@NAME", model.NAME);
                            cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            var tvp = new SqlParameter("@DiscMastType", SqlDbType.Structured)
                            {
                                TypeName = "type_DiscMast",
                                Value = FillDataTable(model.salesDiscountMastDetails)
                            };
                            cmd.Parameters.Add(tvp);
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

        private DataTable FillDataTable(List<SalesDiscountMastDetail> items)
        {
            var table = new DataTable();

            table.Columns.Add("ITEM_CODE", typeof(int));
            table.Columns.Add("ITEM_DIFF", typeof(decimal));
            table.Columns.Add("GRAM_CODE", typeof(int));
            table.Columns.Add("GRAM_DIFF", typeof(decimal));
            table.Columns.Add("COLOR_CODE", typeof(int));
            table.Columns.Add("COLOR_DIFF", typeof(decimal));
            table.Columns.Add("SIZE_CODE", typeof(int));
            table.Columns.Add("SIZE_DIFF", typeof(decimal));
            table.Columns.Add("MESH_CODE", typeof(int));
            table.Columns.Add("MESH_DIFF", typeof(decimal));

            foreach (var item in items ?? new List<SalesDiscountMastDetail>())
            {
                table.Rows.Add(
                    item.ITEM_CODE ?? 0,
                    item.ITEM_DIFF ?? 0m,
                    item.GRAM_CODE ?? 0,
                    item.GRAM_DIFF ?? 0m,
                    item.COLOR_CODE ?? 0,
                    item.COLOR_DIFF ?? 0m,
                    item.SIZE_CODE ?? 0,
                    item.SIZE_DIFF ?? 0m,
                    item.MESH_CODE ?? 0,
                    item.MESH_DIFF ?? 0m
                );
            }

            return table;
        }


    }
}

