using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Models.Weighbridge.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class LoomFabricStrengthEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public LoomFabricStrengthEntryController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/LoomFabricStrengthEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo()
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = "1";
                var vType = "LMQC";
                var tableName = "PROD1_QC";

                var yearParams = new Dictionary<string, object> { { "@YearCd", yearCode } };
                var vnoParams = new Dictionary<string, object>
            {
            { "@COMP_CODE", companyCode },
            { "@BRANCH_CODE", branchCode },
            { "@YEAR_CODE", yearCode },
            { "@V_TYPE", vType },
            { "@TableName", tableName }
            };

                string nextVNo = await _dbHelper.GetExecuteScalarAsync<string>("sp_GetMaxVNo", vnoParams, isStoredProc: true);
                string year = await _dbHelper.GetExecuteScalarAsync<string>("SELECT dbo.fn_GetCurrentYear(@YearCd)", yearParams);
                var docId = (vType) + (year) + (nextVNo);
                var newVno = year + nextVNo;
                var docIdNoList = new { DocId = docId, VNo = newVno };
                return Json(new { status = true, data = docIdNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }
      
        [HttpGet]
        public async Task<IActionResult> GetPlaceMast()
        {
            try
            {
                var placelist = await _dbHelper.GetJsonDataAsync(@$" select CODE , NAME from PLACE_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");
                return Json(new { status = true, data = placelist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetShiftList()
        {
            try
            {
                var itemlist = await _dbHelper.GetJsonDataAsync($@" select distinct SHIFT as CODE,SHIFT as NAME from SHIFT_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode}   order by SHIFT");
                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserMast()
        {
            try
            {
                var itemlist = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from EMP_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode}  order by NAME");
                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLoomList(int PlaceCode=0)
        {
            try
            {
                string placeFilter = "";
                if (PlaceCode != 0)
                {
                    placeFilter = $" AND PLACE_CODE = {PlaceCode} ";
                }

                string strqry = $@"
            SELECT CODE, NAME 
            FROM MACHINE_MAST
            WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
            AND Type = 'Loom' {placeFilter}
            ORDER BY NAME";
                var itemlist = await _dbHelper.GetJsonDataAsync(strqry);

                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProd2List(int LoomCode = 0)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();

                string query = $@"
                SELECT TOP 1
                ISNULL(ITEM_CODE, '') AS ITEM_CODE,
                ISNULL(PTYPE_NAME, '') AS PTYPE_NAME,
                ISNULL(PTYPE_CODE, '') AS PTYPE_CODE,
                ISNULL(WIDTH, 0) AS WIDTH,
                ISNULL(MESH_CODE, '') AS MESH_CODE,
                ISNULL(MESH, 0) AS MESH,
                ISNULL(COLOR_CODE, '') AS COLOR_CODE,
                ISNULL(COLOR_NAME, '') AS COLOR_NAME,
                ISNULL(GRAM, 0.00) AS GRAM,
                ISNULL(DNR, '') AS DNR
               FROM prod2
               WHERE COMP_CODE = {userSession.PubCompCode}
              AND YEAR_CODE = {userSession.PubFYearCode}
              AND BRANCH_CODE = 1
              AND LOOM_CODE = '{LoomCode}'
             order by v_date desc,shift desc";

                var itemList = await _dbHelper.GetJsonDataAsync(query);

                return Json(new { status = true, data = itemList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemList(int itemCode=0)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                string itemFilter = "";
                if(itemCode != 0)
                {
                    itemFilter = $" and IM.CODE={itemCode} ";
                }

                string str = $@"SELECT 
                ISNULL(IM.CODE, 0) AS CODE,
                ISNULL(IM.NAME, '') AS NAME,
                ISNULL(IM.PTYPE_CODE, 0) AS PTYPE_CODE,
                ISNULL(PTYPE.NAME, '') AS PTYPE_NAME,
                ISNULL(IM.WIDTH, 0) AS WIDTH,
                ISNULL(IM.MESHCONV_CODE, 0) AS MESHCONV_CODE,
                ISNULL(IM.COLOR_CODE, 0) AS COLOR_CODE,
                ISNULL(COLOR.NAME, '') AS COLOR_NAME,
                ISNULL(IM.GRAM_CODE, 0) AS GRAM_CODE,
                ISNULL(GRAM.NAME, '') AS GRAM_NAME
            FROM ITEM_MAST IM
            LEFT JOIN ITEMPTYPE_MAST PTYPE
                ON IM.PTYPE_CODE = PTYPE.CODE AND IM.COMP_CODE = PTYPE.COMP_CODE
            LEFT JOIN COLOR_MAST COLOR
                ON IM.COLOR_CODE = COLOR.CODE AND IM.COMP_CODE = COLOR.COMP_CODE
            LEFT JOIN ITEMGRAM_MAST GRAM
                ON IM.GRAM_CODE = GRAM.CODE AND IM.COMP_CODE = GRAM.COMP_CODE
            WHERE IM.COMP_CODE = {usersession.PubCompCode} {itemFilter} order by NAME ";
                var itemlist = await _dbHelper.GetJsonDataAsync(str);
                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetColor()
        {
            try
            {
                var placelist = await _dbHelper.GetJsonDataAsync(@$" select CODE,NAME from COLOR_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");
                return Json(new { status = true, data = placelist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetItemType()
        {
            try
            {
                var itemlist = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from ITEMPTYPE_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode}  order by NAME");
                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStrengthList(int minStd = 0, int maxStd = 0)
        {
            try
            {
                bool isExist = false;
                string strengthFilter = "";
                string strqry = "";
                var matchingCode = "" ;
                //if (minStd != 0 && maxStd != 0)
                //{
                    strengthFilter = $" and  MIN_STD = {minStd} and MAX_STD = {maxStd} ";
                //}
                strqry = $@"select CODE, NAME from TENACITY_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode}
                               {strengthFilter} order by NAME";
                var itemlist1 = await _dbHelper.GetJsonDataAsync(strqry);

                if (itemlist1.Count > 0)
                {
                    isExist = true;
                    //dynamic first = itemlist1[0];
                    //matchingCode = first.NAME;
                    matchingCode = minStd+"-"+maxStd;
                }
                else
                    isExist = false;

                strqry = $@"select CODE, NAME from TENACITY_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode}
                                order by NAME";
                var allList = await _dbHelper.GetJsonDataAsync(strqry);

                return Json(new
                {
                    status = true,
                    data = allList,
                    isExist = isExist,
                    matchingCode = matchingCode
                });

                //return Json(new { status = true, data = itemlist, isExist = isExist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLoomFabricSById(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@DOC_ID", id},
                    {"@Action", "LFSEntryHeaderData"}
                };
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@DOC_ID", id},
                    {"@Action", "LoomFSEntryDetailData"}
                };

                var headerlist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetLoomFabricEntry]", parameter);
                var detaillist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetLoomFabricEntry]", parameter1);
                return Json(new { status = true, header = headerlist, detail = detaillist });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateLoomFabricEntry([FromBody] LoomFabricEntryModel model)
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
                            DataTable prod2Table = await ToProd2QCDataTable(model.Prod2QCData);

                            using (SqlCommand cmd = new SqlCommand())
                            {
                                cmd.Connection = _dbcontext.GetErpConnection();
                                cmd.Connection.Open();
                                
                                foreach (DataRow row in prod2Table.Rows)
                                {
                                    
                                    var meshCode = row["MESH"].ToString();
                                    if (meshCode != null && meshCode != "")
                                    {
                                        var result1 = Convert.ToInt32(row["RESULT1"]);
                                        var result2 = Convert.ToInt32(row["RESULT2"]);

                                        cmd.CommandText = "SELECT COUNT(1) FROM TENACITY_MAST WHERE COMP_CODE = @CompCode AND NAME = @MeshCode";
                                        cmd.Parameters.Clear();
                                        cmd.Parameters.AddWithValue("@CompCode", usersessionDt.PubCompCode);
                                        cmd.Parameters.AddWithValue("@MeshCode", meshCode);

                                        int exists = Convert.ToInt32(cmd.ExecuteScalar());

                                        if (exists == 0)
                                        {
                                            cmd.CommandText = "SELECT ISNULL(MAX(CODE), 0) + 1 FROM TENACITY_MAST WHERE COMP_CODE = @CompCode";
                                            cmd.Parameters.Clear();
                                            cmd.Parameters.AddWithValue("@CompCode", usersessionDt.PubCompCode);

                                        int maxCode = Convert.ToInt32(cmd.ExecuteScalar());

                                        cmd.CommandText = @"INSERT INTO TENACITY_MAST 
                                        (COMP_CODE, CODE, NAME, TENACITY_TYPE, MIN_STD, MAX_STD, ACTIVE, UUSER, UDATE, AED, LIP, LID) 
                                        VALUES 
                                        (@CompCode, @Code, @Name, 'NA', @MinStd, @MaxStd, '1', @UUser, GETDATE(), 'A', @LIP, HOST_NAME())";
                                            cmd.Parameters.Clear();
                                            cmd.Parameters.AddWithValue("@CompCode", usersessionDt.PubCompCode);
                                            cmd.Parameters.AddWithValue("@Code", maxCode);
                                            cmd.Parameters.AddWithValue("@Name", meshCode);
                                            cmd.Parameters.AddWithValue("@MinStd", result1);
                                            cmd.Parameters.AddWithValue("@MaxStd", result2);
                                            cmd.Parameters.AddWithValue("@UUser", usersessionDt.PubUserId);
                                            cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);

                                            cmd.ExecuteNonQuery();
                                        }
                                    }

                                }
                                cmd.Connection.Close();
                            }

                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_LoomFabricEntry]", con, transaction))
                            {
                               cmd.CommandType = CommandType.StoredProcedure;                                
                               var docId = (model.VType) + (model.V_No);     
                                
                               if(model.SaveOrUpdate== "Save")                                
                                    cmd.Parameters.AddWithValue("@Action", "Add");
                               else
                                    cmd.Parameters.AddWithValue("@Action", "Edit");

                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_No);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                cmd.Parameters.AddWithValue("@DOC_ID", docId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QCTIME", model.QCTIME ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_INCHARGE", model.QC_INCHARGE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHEMIST", model.CHEMIST ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_INCHARGENAME", model.QC_INCHARGENAME ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHEMISTNAME", model.CHEMISTNAME ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId);                               
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                                cmd.Parameters.AddWithValue("@SRNO", model.SRNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Prod2QCData", prod2Table);
                                
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 5000)
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

        private async Task<DataTable> ToProd2QCDataTable(List<Prod2QCDetailModel> data)
        {
            var table = new DataTable();
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("PLACE_CODE", typeof(int));
            table.Columns.Add("LOOM_CODE", typeof(int));
            table.Columns.Add("EMP_CODE", typeof(int));
            table.Columns.Add("ITEM_CODE", typeof(int));
            table.Columns.Add("PTYPE_CODE", typeof(int));
            table.Columns.Add("PTYPE_NAME", typeof(string));
            table.Columns.Add("WIDTH", typeof(decimal));
            table.Columns.Add("GRAM", typeof(decimal));
            table.Columns.Add("MESH", typeof(string));
            table.Columns.Add("MESH_CODE", typeof(int));
            table.Columns.Add("COLOR_CODE", typeof(int));
            table.Columns.Add("COLOR_NAME", typeof(string));
            table.Columns.Add("RUNNO", typeof(int));
            table.Columns.Add("LOOM_TYPE", typeof(string));
            table.Columns.Add("MAKE_T", typeof(string));
            table.Columns.Add("DNR", typeof(string));
            table.Columns.Add("RESULT1", typeof(decimal));
            table.Columns.Add("REMARKS1", typeof(string));
            table.Columns.Add("RESULT2", typeof(decimal));
            table.Columns.Add("REMARKS2", typeof(string));
            table.Columns.Add("PRKG", typeof(decimal));
            table.Columns.Add("WASTE", typeof(decimal));
            table.Columns.Add("PSIZE", typeof(decimal));
            table.Columns.Add("REMARKS", typeof(string));
            table.Columns.Add("CPRDN", typeof(decimal));
            table.Columns.Add("PAISA_TYPE", typeof(string));
            table.Columns.Add("PAISA_SIZE", typeof(string));
            table.Columns.Add("PAISA_MTR", typeof(int));
            table.Columns.Add("PAISA_TYPE1", typeof(string));
            table.Columns.Add("PORD_TYPE", typeof(string));
            table.Columns.Add("PORD_NO", typeof(int));
            table.Columns.Add("COND1", typeof(short));
            table.Columns.Add("COND2", typeof(short));
            table.Columns.Add("SHIFT_SCH", typeof(string));
            table.Columns.Add("REPORT_FILTER", typeof(int));
            table.Columns.Add("TIME1_WIDTH", typeof(decimal));
            table.Columns.Add("TIME2_WIDTH", typeof(decimal));
            table.Columns.Add("TIME3_WIDTH", typeof(decimal));
            table.Columns.Add("TIME4_WIDTH", typeof(decimal));
            table.Columns.Add("TIME5_WIDTH", typeof(decimal));
            table.Columns.Add("PC_LOWMELT", typeof(decimal));
            table.Columns.Add("GLUE_CONTENT", typeof(decimal));
            table.Columns.Add("OTHERS", typeof(decimal));
            table.Columns.Add("YELLOWP", typeof(decimal));
            table.Columns.Add("BLUEP", typeof(decimal));
            table.Columns.Add("OTHERP", typeof(decimal));
            table.Columns.Add("GRADE", typeof(string));
            table.Columns.Add("YELLOW160C", typeof(decimal));
            table.Columns.Add("MOISTURE", typeof(decimal));
            table.Columns.Add("BULKDENSITY", typeof(decimal));
            table.Columns.Add("PH_FLAKES", typeof(decimal));
            table.Columns.Add("OVERSIZED", typeof(decimal));
            table.Columns.Add("SRNO", typeof(int));
            table.Columns.Add("WARP_ELONG", typeof(decimal));
            table.Columns.Add("WEFT_ELONG", typeof(decimal));
            table.Columns.Add("WARP_MESH", typeof(decimal));
            table.Columns.Add("WEFT_MESH", typeof(decimal));
            table.Columns.Add("SUPPLY_TYPE", typeof(string));
            table.Columns.Add("COLOR_TYPE", typeof(string));
            int x = 1;
            foreach (var row in data)
            {
                if ((Convert.ToString(row.MESH_CODE) == row.MESH) && row.MESH_CODE !=null)
                {
                    string strqry = @$"SELECT ISNULL(MAX(CODE), 0) +{x} FROM TENACITY_MAST WHERE COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}";
                    int nextVNo = await _dbHelper.GetExecuteScalarAsync<int>(strqry);
                }

                x++;
                table.Rows.Add(
                    row.SNO,
                    row.PLACE_CODE,
                    row.LOOM_CODE,
                    row.EMP_CODE,
                    row.ITEM_CODE,
                    row.PTYPE_CODE,
                    row.PTYPE_NAME,
                    row.WIDTH,
                    row.GRAM,
                    row.MESH,
                    row.MESH_CODE,
                    row.COLOR_CODE,
                    row.COLOR_NAME,
                    row.RUNNO,
                    row.LOOM_TYPE,
                    row.MAKE_T,
                    row.DNR,
                    row.RESULT1,
                    row.REMARKS1,
                    row.RESULT2,
                    row.REMARKS2,
                    row.PRKG,
                    row.WASTE,
                    row.PSIZE,
                    row.REMARKS,
                    row.CPRDN,
                    row.PAISA_TYPE,
                    row.PAISA_SIZE,
                    row.PAISA_MTR,
                    row.PAISA_TYPE1,
                    row.PORD_TYPE,
                    row.PORD_NO,
                    row.COND1,
                    row.COND2,
                    row.SHIFT_SCH,
                    row.REPORT_FILTER,
                    row.TIME1_WIDTH,
                    row.TIME2_WIDTH,
                    row.TIME3_WIDTH,
                    row.TIME4_WIDTH,
                    row.TIME5_WIDTH,
                    row.PC_LOWMELT,
                    row.GLUE_CONTENT,
                    row.OTHERS,
                    row.YELLOWP,
                    row.BLUEP,
                    row.OTHERP,
                    row.GRADE,
                    row.YELLOW160C,
                    row.MOISTURE,
                    row.BULKDENSITY,
                    row.PH_FLAKES,
                    row.OVERSIZED,
                    row.SRNO,
                    row.WARP_ELONG,
                    row.WEFT_ELONG,
                    row.WARP_MESH,
                    row.WEFT_MESH,
                    row.SUPPLY_TYPE,
                    row.COLOR_TYPE
                );
               
            }

            return table;
        }


    }
}
