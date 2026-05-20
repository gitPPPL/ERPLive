using Microsoft.AspNetCore.Mvc;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Services;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class QCTemperatureEntryController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IMasterDataService _masterDataService;

        public QCTemperatureEntryController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/QCTemperatureEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string vType, string tableName)
        {
            var maxNo = await _masterDataService.GetMaxVNoAsync(vType, tableName);
            return Json ( maxNo);
        }

        [HttpGet]
        public JsonResult getExistOrNot(DateTime V_DATE, DateTime V_TIME,string SHIFT,int plantCode, int VNo=0)
        {
            try
            {
                bool isExist = false;

                using (var con = _dbcontext.GetErpConnection())
                {
                  var loginDatail= _globalValue.GetGlobalVariables();
                    string  sqlqry = "";
                    if (VNo>0)
                          sqlqry = @$"and V_NO != {VNo}";

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = @$"
                         SELECT CASE 
                        WHEN EXISTS (
                        SELECT 1 
                         FROM TAPE_QUALITY1 
                        WHERE V_DATE=@VDate and FORMAT(V_TIME, 'hh:mm')=@V_time
                        and SHIFT=@shift and DEPT_CODE=@plantCode
                        and COMP_CODE=@CompCode and YEAR_CODE=@YearCode and BRANCH_CODE=1 and V_TYPE=@V_type {sqlqry}
                        ) 
                        THEN 1 ELSE 0 
                        END";

                       string vdate= (V_DATE).ToString("dd-MMM-yyyy");
                        string vtime = (V_TIME).ToString("hh:mm");

                        cmd.Parameters.AddWithValue("@VDate", vdate);
                        cmd.Parameters.AddWithValue("@V_time", vtime);
                        cmd.Parameters.AddWithValue("@shift", SHIFT);
                        cmd.Parameters.AddWithValue("@plantCode", plantCode);
                        cmd.Parameters.AddWithValue("@CompCode", loginDatail.PubCompCode);
                        cmd.Parameters.AddWithValue("@YearCode", loginDatail.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_type", "TAPE");

                        con.Open();
                        var result = cmd.ExecuteScalar();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }

                return Json(new { status = true, exists = isExist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data check failed: " + ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetEmployeeMast()
        {
            var emplist = await _masterDataService.GetEmployeeMastAsync();
            return Json(emplist);
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftMast()
        {
            var shiftList = await _masterDataService.GetShiftMastAsync();
            return Json(shiftList);
        }

        [HttpGet]
        public async Task<IActionResult> GetPlantMast()
        {
            var plantlist = await _masterDataService.GetItemDepartmentMastForProdAsync();
            return Json(plantlist); 
        }
        [HttpGet]
        public async Task<IActionResult> GetDenierMast()
        {
            var denierList = await _masterDataService.GetDenierMastAsync();
            return Json(denierList);
        }

        [HttpGet]
        public async Task<IActionResult> GetMaterialList()
        {
            var winderlist = await _masterDataService.GetRawItemListAsync();
            return Json(winderlist);
        }

        [HttpGet]
        public async Task<IActionResult> GetPlantZoneList()
        {
            try
            {

            var plantlist = await _dbHelper.GetJsonDataAsync(@$"
              select CODE, NAME from TAPE_QUALITY_MAST where V_TYPE = 'ROOM' and COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
              order by NAME
            ");
                return Json(new { status = true, data = plantlist });
            }
            catch(Exception ex)
            {
                return Json(new
                {
                    status = true,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetScrewList()
        {
            try
            {
                var screwlist = await _dbHelper.GetJsonDataAsync(@$"
              select CODE, NAME from TAPE_QUALITY_MAST where V_TYPE = 'SPED' and COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
              order by NAME
             ");
                return Json(new { status = true, data = screwlist });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = true,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> WinderList()
        {
            try
            {
              var screwlist = await _dbHelper.GetJsonDataAsync(@$"
              select CODE, NAME from TAPE_QUALITY_MAST where V_TYPE = 'WIND' and COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
              order by NAME
             ");
                return Json(new { status = true, data = screwlist });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = true,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQcTemperatureById(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var VNo = id.Substring(4);
                var VType = id.Substring(0,4);
        
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", VType},
                    {"@V_NO",  VNo },
                    {"@Action", "QcTempratureHeaderData"}
                };
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", VType},
                    {"@V_NO",  VNo },
                    {"@Action", "QcTempratureDetailData"}
                };
                
                var headerlist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetQcTempratureEntry]", parameter);
                var detaillist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetQcTempratureEntry]", parameter1);
                return Json(new { status = true, header = headerlist, detail = detaillist });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateQcTemperatureEntry([FromBody] QcTemperature model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request: Model is null." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeQuality]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@TapeQuality2", QcTempDataTable(model.TapeQualitys));
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1); 
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                cmd.Parameters.AddWithValue("@V_TIME", model.V_TIME == default ? (object)DBNull.Value : model.V_TIME);
                                cmd.Parameters.AddWithValue("@INCH_CODE", model.INCH_CODE == 0 ? (object)DBNull.Value : model.INCH_CODE);
                                cmd.Parameters.AddWithValue("@OPERATORE_CODE", model.OPERATORE_CODE == 0 ? (object)DBNull.Value : model.OPERATORE_CODE);
                                cmd.Parameters.AddWithValue("@SUP_CODE", model.SUP_CODE == 0 ? (object)DBNull.Value : model.SUP_CODE);
                                cmd.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE == 0 ? (object)DBNull.Value : model.DEPT_CODE);
                                cmd.Parameters.AddWithValue("@SHIFT", string.IsNullOrEmpty(model.SHIFT) ? (object)DBNull.Value : model.SHIFT);
                                cmd.Parameters.AddWithValue("@DENIER", model.DENIER == 0 ? (object)DBNull.Value : model.DENIER);
                                cmd.Parameters.AddWithValue("@REMARK", string.IsNullOrEmpty(model.REMARK) ? (object)DBNull.Value : model.REMARK);
                                cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                                cmd.Parameters.AddWithValue("@UUSER", usersessionDt.PubUserId); 
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", HttpContext.Connection.RemoteIpAddress?.ToString() ?? (object)DBNull.Value);

                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, -1)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);

                                var returnParam = new SqlParameter
                                {
                                    Direction = ParameterDirection.ReturnValue,
                                    SqlDbType = SqlDbType.Int
                                };
                                cmd.Parameters.Add(returnParam);

                                await cmd.ExecuteNonQueryAsync();

                                var returnValue = (int)returnParam.Value;
                                string errorMsg = errorParam.Value?.ToString();

                                if (returnValue > 0)
                                {
                                    transaction.Commit();
                                    return Json(new { status = true, message = "Data saved/updated successfully." });
                                }
                                else
                                {
                                    transaction.Rollback();
                                    return Json(new { status = false, message = errorMsg ?? "Operation failed." });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Unexpected error: " + ex.Message });
            }
        }

        private DataTable QcTempDataTable(List<TapeQuality2> data)
        {
            var table = new DataTable();
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("TYPE", typeof(string));
            table.Columns.Add("V_DATE", typeof(DateTime));
            table.Columns.Add("ROOM_CODE", typeof(int));
            table.Columns.Add("TEMP_READ", typeof(decimal));
            table.Columns.Add("TEMP_REM", typeof(string));
            table.Columns.Add("SPEED_CODE", typeof(int));
            table.Columns.Add("SPEED_READ", typeof(decimal));
            table.Columns.Add("SPEED_READ2", typeof(string));
            table.Columns.Add("WINDER_CODE", typeof(int));
            table.Columns.Add("WIDTH_MM", typeof(decimal));
            table.Columns.Add("DENIER", typeof(decimal));
            table.Columns.Add("BREAKING_LOAD", typeof(decimal));
            table.Columns.Add("TENACITY", typeof(decimal));
            table.Columns.Add("ELONGATION", typeof(decimal));
            table.Columns.Add("MAT_CODE", typeof(int));
            table.Columns.Add("GRADE", typeof(string));
            table.Columns.Add("NO_OF_BAGS", typeof(int));
            table.Columns.Add("MAT_PER", typeof(decimal));
            table.Columns.Add("TIME_TAKEN", typeof(DateTime));

            foreach (var row in data)
            {
                table.Rows.Add(
                    row.SNO,
                    row.TYPE ?? (object)DBNull.Value,
                    row.V_DATE == default ? (object)DBNull.Value : row.V_DATE,
                    row.ROOM_CODE,
                    row.TEMP_READ,
                    row.TEMP_REM ?? (object)DBNull.Value,
                    row.SPEED_CODE,
                    row.SPEED_READ,
                    row.SPEED_READ2 ?? (object)DBNull.Value,
                    row.WINDER_CODE,
                    row.WIDTH_MM,
                    row.DENIER,
                    row.BREAKING_LOAD,
                    row.TENACITY,
                    row.ELONGATION,
                    row.MAT_CODE,
                    row.GRADE ?? (object)DBNull.Value,
                    row.NO_OF_BAGS,
                    row.MAT_PER,
                    row.TIME_TAKEN  
                );
            }

            return table;
        }


    }
}
  