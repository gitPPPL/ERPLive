using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Weighbridge.Transaction;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class InHouseWeighbridgeEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalValidationdate _globalValidationdate;
        public InHouseWeighbridgeEntryController(DataBaseConnection dbcontext, DbHelper dbHelper, 
        travelexpensemanagement.Common.DropdownService.DropdownService dropdownService , GlobalVariableService globalValue,
        ModuleService.ModuleService moduleService, DataBaseConnection dbConnection, GlobalValidationdate globalValidationdate)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _dropdownService = dropdownService;
            _globalValidationdate = globalValidationdate;
            _dbConnection = dbConnection;
        }


        public IActionResult Index()
        {
            return View("~/Views/Weighbridge/Transaction/InHouseWeighbridgeEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = "1";
                var vType = V_type;
                var tableName = "WB1";

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

        public async Task<IActionResult> GetGateNo()
        {
            try
            {
                var userDt = _globalValue.GetGlobalVariables();
                //string strqry= $@"SELECT V_NO,V_TYPE,TRUCK_NO,PARTY_CODE FROM GATE1 where COMP_CODE={userDt.PubCompCode} and YEAR_CODE={userDt.PubFYearCode} and BRANCH_CODE=1 AND V_TYPE IN ( select  DISTINCT CODE from DOCTYPE_MAST where DOCTYPE='GateInward' ) ";
                string strqry = $@"
               SELECT V_NO,V_TYPE,TRUCK_NO, PARTY_CODE, sg.NAME partyName, d.NAME as VtypeName FROM GATE1 g 
               left join SUBGROUP_MAST sg on g.PARTY_CODE=sg.CODE and g.COMP_CODE=sg.COMP_CODE
               left join DOCTYPE_MAST d on g.V_TYPE=d.CODE 
               where g.COMP_CODE={userDt.PubCompCode}  and g.YEAR_CODE={userDt.PubFYearCode} and g.BRANCH_CODE=1 AND g.V_TYPE IN
               ( select  DISTINCT CODE from DOCTYPE_MAST where DOCTYPE='GateInward' ) order by V_NO ";

                var gateList = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = gateList });
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
        public JsonResult GetTareSlipNo()
        {

            var usersession = _globalValue.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string sql = $@"select V_NO ,V_TYPE  from WB1 where V_TYPE='KINH' and WB_TYPE='Tare' and COMP_CODE={usersession.PubCompCode} and YEAR_CODE={usersession.PubFYearCode} and BRANCH_CODE=1 order by V_NO ";
                var GetTareSlipNo = _dropdownService.GetDropdownList(sql);
                return Json(GetTareSlipNo);
            }
        }


        [HttpGet]
        public JsonResult GetGrossSlipNo()
        {

            var usersession = _globalValue.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string sql = $@"
                SELECT  V_NO ,V_TYPE  FROM WB1 WHERE V_TYPE = 'KINH' AND WB_TYPE = 'Gross' AND COMP_CODE = {usersession.PubCompCode}
                AND YEAR_CODE = {usersession.PubFYearCode}  AND BRANCH_CODE = {usersession.PubBranchCode} ORDER BY V_NO";
                var Partylist = _dropdownService.GetDropdownList(sql);
                return Json(Partylist);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeighBridgeBySlipNo(int SlipNo, string vType)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var strqry = $@"SELECT top 2   b.VEHICLE_NO, a.*, b.* FROM WB2 a LEFT JOIN WB1 b  ON a.V_NO = b.V_NO AND a.V_TYPE = b.V_TYPE AND a.COMP_CODE = b.COMP_CODE
                    AND a.BRANCH_CODE = b.BRANCH_CODE AND a.YEAR_CODE = b.YEAR_CODE WHERE   a.V_NO = 252603890 ; ";
                    //WHERE   a.V_NO = {SlipNo} AND a.V_TYPE = '{vType}'
                    //AND b.WB_TYPE = 'Tare' AND a.COMP_CODE = {usersession.PubCompCode}  AND a.YEAR_CODE = {usersession.PubFYearCode} AND a.BRANCH_CODE = {usersession.PubBranchCode};";

                    var tareNoList = await _dbHelper.GetJsonDataAsync(strqry);
                    return Json(new { status = true, data = tareNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeighBridgeByGrossSlipNo(int SlipNo, string vType)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();

                var strqry = $@"SELECT  b.VEHICLE_NO, a.*,  b.* FROM WB2 a LEFT JOIN WB1 b 
                ON a.V_NO = b.V_NO AND a.V_TYPE = b.V_TYPE  AND a.COMP_CODE = b.COMP_CODE  AND a.BRANCH_CODE = b.BRANCH_CODE
                AND a.YEAR_CODE = b.YEAR_CODE WHERE 
                AND a.V_TYPE = '{vType}'
                AND a.TYPE = 'Gross' AND a.COMP_CODE = {usersession.PubCompCode}  AND a.YEAR_CODE = {usersession.PubFYearCode} AND a.BRANCH_CODE = {usersession.PubBranchCode}  ORDER BY   a.SNO DESC; ";       
                
                var tareNoList = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = tareNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='KantaInhouse' ");
                return Json(new { status = true, data = Doctype });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
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
        public async Task<IActionResult> GetInHouseWeighBridgeById(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@DOC_ID", id},
                    {"@Action", "WBEntryHeaderData"}
                };
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@DOC_ID", id},
                    {"@Action", "WBEntryDetailData"}
                };

                var headerlist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter);
                var detaillist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter1);
                return Json(new { status = true, header = headerlist, detail = detaillist });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateInHouseWeighBridgeEntry([FromBody] WBEntryModel model)
        {
            if (model == null)
                return Json(new { status = false, message = "Data save failed." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;
                        try
                        {              
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_WBEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                
                                if (model.SaveOrUpdate == "Save")
                                    cmd.Parameters.AddWithValue("@Action", "Add");
                                else
                                    cmd.Parameters.AddWithValue("@Action", "Edit");

                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_SHIFT", model.V_SHIFT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_TYPE", model.WB_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_TYPE", model.GATE_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_NO", model.GATE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_QTY", model.PARTY_QTY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GROSS_NO", model.GROSS_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TARE_NO", model.TARE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VEHICLE_NO", model.VEHICLE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STATUS_DATE", model.STATUS_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@NET_WGT", model.NET_WGT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FINAL_TYPE", model.FINAL_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FINAL_REM", model.FINAL_REM ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_GROSSWT", model.PARTY_GROSSWT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_TRWT", model.PARTY_TRWT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_WBNO", model.PARTY_WBNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SMALL_BAG", model.SMALL_BAG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MEDIUM_BAG", model.MEDIUM_BAG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LARGE_BAG", model.LARGE_BAG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);

                                var tvp = new SqlParameter("@WB2Data", SqlDbType.Structured)
                                {
                                    TypeName = "Type_WB2",
                                    Value = ToWB2DataTable(model.WB2Data)
                                };
                                cmd.Parameters.Add(tvp);
                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.ReturnValue
                                };
                                cmd.Parameters.Add(returnParam);
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 54000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);
                                await cmd.ExecuteNonQueryAsync();

                                string errorMessage = errorParam.Value?.ToString();
                                if ((int)returnParam.Value <= 0)
                                    success = false;
                            }

                            if (success)
                                transaction.Commit();
                            else
                                transaction.Rollback();

                            return Json(new
                            {
                                status = success,
                                message = success ? "Data save/update successfully." : "Failed to save or update some entry details."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction?.Rollback();
                            return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        private DataTable ToWB2DataTable(List<TypeWB2> items)
        {
            var table = new DataTable();
            table.Columns.Add("V_SHIFT", typeof(string));
            table.Columns.Add("TYPE", typeof(string));
            table.Columns.Add("WEIGHT", typeof(decimal));
            table.Columns.Add("TARE_WGT", typeof(decimal));
            table.Columns.Add("NET_WGT", typeof(decimal));
            table.Columns.Add("WGT_DATE", typeof(DateTime));
            table.Columns.Add("WGT_TIME", typeof(string));
            table.Columns.Add("FROM_PLACE", typeof(int));
            table.Columns.Add("FROM_NAME", typeof(string));
            table.Columns.Add("TO_PLACE", typeof(int));
            table.Columns.Add("TO_NAME", typeof(string));
            table.Columns.Add("ITEM_CODE", typeof(int));
            table.Columns.Add("ITEM_NAME", typeof(string));
            table.Columns.Add("REMARKS", typeof(string));
            table.Columns.Add("STATUS", typeof(string));
            table.Columns.Add("Ref_type", typeof(string));
            table.Columns.Add("Ref_no", typeof(int));
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("wb_time", typeof(string));
            table.Columns.Add("COND", typeof(string));
            table.Columns.Add("MOIS_PER", typeof(decimal));
            table.Columns.Add("MOIS_WT", typeof(decimal));

            int srno = 1;
            foreach (var item in items ?? new List<TypeWB2>())
            {
                table.Rows.Add(
                    item.V_SHIFT, item.TYPE, item.WEIGHT, item.TARE_WGT, item.NET_WGT,
                    item.WGT_DATE, item.WGT_TIME, item.FROM_PLACE, item.FROM_NAME,
                    item.TO_PLACE, item.TO_NAME, item.ITEM_CODE, item.ITEM_NAME,
                    item.REMARKS, item.STATUS, item.Ref_type, item.Ref_no,
                    srno, item.wb_time, item.COND, item.MOIS_PER, item.MOIS_WT
                );
                srno++;
            }

            return table;
        }


        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("Gate1", vdate, vtype, vno);
            return Ok(result);
        }






    }
}
