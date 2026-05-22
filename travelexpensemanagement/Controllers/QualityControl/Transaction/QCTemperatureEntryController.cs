using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;
using System.Data;
using System.Dynamic;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories;
using travelexpensemanagement.Services;


namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]
    public class QCTemperatureEntryController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly IMasterDataService _masterDataService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;

        public QCTemperatureEntryController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, 
            GlobalVariableService globalValue, ModuleService.ModuleService moduleService, IMasterDataService masterDataService, 
            GlobalValidationdate globalValidationdate, DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _masterDataService = masterDataService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
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
                        and COMP_CODE=@CompCode and YEAR_CODE=@YearCode and BRANCH_CODE=@BRANCH_CODE and V_TYPE=@V_type {sqlqry}
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
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", loginDatail.PubBranchCode);
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

        public JsonResult GetDropdown(string type, string VTypeId = "")
        {
            //var gv = _globalValue.GetGlobalVariables();
            string query = "";
            switch (type)
            {
                case "Employee":
                    query = $@"SELECT CODE, CONCAT(CODE, ' || ', NAME) as  NAME ,DEPT_CODE
                            FROM EMP_MAST 
                            WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} 
                              AND ACTIVE = 1  and RESIGN_DATE is null
                            ORDER BY NAME
                    ";
                    break;
                case "Shift":
                    query = $@"
                        SELECT DISTINCT SHIFT AS CODE, SHIFT AS NAME 
                        FROM SHIFT_MAST 
                        WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} 
                        ORDER BY NAME
                    ";
                    break;
                case "Plant":
                    query = $@"
                        select CODE, NAME from ITEMDEPT_MAST where TRAN_TYPE='Production' and PLACE_TYPE IN ('Tapeline', 'Lamination') and COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME 
                    ";
                    break;
                case "Denier":
                    query = $@"
                            select CODE, NAME from TAPE_NFABRIC_MAST 
                            where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} 
                            order by NAME 
                    ";
                    break;
                case "Material":
                    query = $@"
                        SELECT ITEM_MAST.CODE, ITEM_MAST.NAME
                        FROM ITEM_MAST left join ITEM_GROUP
                        on ITEM_MAST.GROUP_CODE= ITEM_GROUP.CODE and ITEM_MAST.COMP_CODE= ITEM_GROUP.COMP_CODE
                        WHERE ITEM_MAST.COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} and ITEM_GROUP.SALE_GROUP= 'Raw'
                        order by ITEM_MAST.NAME ";
                    break;
                case "Winder":
                    query = $@"
                      select CODE, NAME from TAPE_QUALITY_MAST where V_TYPE = 'WIND' and COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
                      order by NAME
                     ";
                    break;
                
            }
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetPlantZoneList()
        {
            try
            {

            var plantlist = await _dbHelper.GetJsonDataAsync(@$"
              select CODE, NAME from TAPE_QUALITY_MAST where V_TYPE = 'ROOM' and COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
              order by SORT_NO
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
              order by SORT_NO
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
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", VType},
                    {"@V_NO",  VNo },
                    {"@Action", "QcTempratureHeaderData"}
                };
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", usersession.PubBranchCode},
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

        //===Validate VDate
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("WB1", vdate, vtype, vno);
            return Ok(result);
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
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode); 
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
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);

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

        [HttpGet]
        public async Task<JsonResult> ImportDataByReading(int timeInterval, string type, string shift, int deptCode, string vType)
        {
            var gv = _globalValue.GetGlobalVariables();
            var dataList = new List<dynamic>();
            try
            {
                using(SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using(SqlCommand cmd = new SqlCommand("sp_TapeQuality", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ImportReadingData");
                        cmd.Parameters.AddWithValue("@TYPE", type ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIFT", shift ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", deptCode != 0 ? (object)deptCode : DBNull.Value);
                        cmd.Parameters.AddWithValue("@TimeInterval", timeInterval != 0 ? (object)timeInterval : DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new ExpandoObject() as IDictionary<string, object>;
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
                                }
                                dataList.Add(row);
                            }
                        }
                         return Json(new { success = true, data = dataList }); 
                    }
                }
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
  