using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Runtime.InteropServices;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Services;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class LaminationQCEntryController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IMasterDataService _masterDataService;
        public LaminationQCEntryController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/LaminationQCEntry/Index.cshtml");
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
                var vType = "RLAM";
                var tableName = "LAMINATION";

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
        public async Task<IActionResult> GetPlaceList()
        {
            var result = await _masterDataService.GetPlaceListAsync();
            return Json(result);
        }

        [HttpGet]
        public  async Task<IActionResult> GetUserList()
        {
            var result = await _masterDataService.GetEmployeeMastAsync();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetStrengthList()
        {
            var result = await _masterDataService.GetStrengthListAsync();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetStatusMast()
        {
            var result = await _masterDataService.GetStatusMastAsync();
            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetShiftList()
        {
            var shiftlist = await _masterDataService.GetShiftMastAsync();
            return Json(shiftlist);
        }

        public async Task<IActionResult> GetQCDataList(string QcAllOrPending, int placeCode, string date)
        {

            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                string QcFilter = "";
                if (QcAllOrPending == "Pending")
                {
                    QcFilter = "and ISNULL(a.QC_UPDATETIME, '')='' ";
                }
                QcFilter += " order by a.shift,a.ITEM_NAME_A,a.ROLL_NO_A,a.v_no";

                var str = $@" select   DOC_ID, V_No RefNo,Shift,LAM_CODE,b.NAME LamName,a.LAMSUP_CODE,a.LAMSUP_NAME,a.LAMOP_CODE,a.LAMOP_NAME, ITEM_CODE_A,
                ITEM_NAME_A ItemName,MTR_A Meter,NT_WT_A NetWt,ROLL_NO_A RollNo,PSIZE UnlamSize,GRAM 'Unlam Gram.',0 Coating,GRAM_A 'Gram.',  
                   cast(round(GRAM_A*19.685,0) as int)GSM,a.NWARPWAY_RES NWarpWay,a.WARPWAY_RES WarpWay,
                   a.NWEFTWAY_RES NWeftWay,a.WEFTWAY_RES WeftWay,a.TENA_CODE_A,a.STATUS_CODE_A StatusCode,
                   a.Elong_Warp,a.Elong_Weft,a.QC_REMARKS Remarks 
                   from LAMINATION a 
                   left join MACHINE_MAST b on a.LAM_CODE=b.code and b.TYPE='Lamination' and a.COMP_CODE=b.COMP_CODE  
                   where a.COMP_CODE= {userSession.PubCompCode}  and a.Branch_Code=  1   and a.Year_Code=  {userSession.PubFYearCode} and a.V_TYPE='RLAM'
                   and a.PLACE_CODE= { placeCode} and a.V_DATE='{date}' {QcFilter} ";

                var itemList = await _dbHelper.GetJsonDataAsync(str);
                return Json(new { status = true, data = itemList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });

            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQCDataListOnSearch(string QcAllOrPending, int placeCode, string date, string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                string QcFilter = "";
                if (QcAllOrPending == "Pending")
                {
                    QcFilter = "and ISNULL(a.QC_UPDATETIME, '')='' ";
                }
                QcFilter += " order by a.shift,a.ITEM_NAME_A,a.ROLL_NO_A,a.v_no";

                var str = $@" select  DOC_ID, V_No RefNo,Shift,LAM_CODE,b.NAME LamName,a.LAMSUP_CODE,a.LAMSUP_NAME,a.LAMOP_CODE,a.LAMOP_NAME, ITEM_CODE_A,
                ITEM_NAME_A ItemName,MTR_A Meter,NT_WT_A NetWt,ROLL_NO_A RollNo,PSIZE UnlamSize,GRAM 'Unlam Gram.',0 Coating,GRAM_A 'Gram.',  
                   cast(round(GRAM_A*19.685,0) as int)GSM,a.NWARPWAY_RES NWarpWay,a.WARPWAY_RES WarpWay,
                   a.NWEFTWAY_RES NWeftWay,a.WEFTWAY_RES WeftWay,a.TENA_CODE_A,a.STATUS_CODE_A StatusCode,
                   a.Elong_Warp,a.Elong_Weft,a.QC_REMARKS Remarks 
                   from LAMINATION a 
                   left join MACHINE_MAST b on a.LAM_CODE=b.code and b.TYPE='Lamination' and a.COMP_CODE=b.COMP_CODE  
                   where a.COMP_CODE= {userSession.PubCompCode}  and a.Branch_Code=  1   and a.Year_Code=  {userSession.PubFYearCode} and a.V_TYPE='RLAM'
                   and a.PLACE_CODE= {placeCode} and a.V_DATE='{date}' order by ITEM_NAME asc ";

                var fullList = await _dbHelper.GetJsonDataAsync(str);

                //var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetLoomFabricEntry]", parameter);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "DOC_ID" };
                            return searchableKeys.Any(key =>
                                dict.ContainsKey(key) &&
                                dict[key]?.ToString().ToLower().Contains(searchTerm) == true
                            );
                        })
                        .ToList();
                }
                var totalCount = fullList.Count;
                var pagedList = fullList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new { status = true, data = pagedList, totalCount });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> UpdateLamination([FromBody] LaminationUpdateModel model)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    var userSession = _globalValue.GetGlobalVariables();

                    foreach (var detail in model.LaminationDetails)
                    {
                        var docid = detail.Docid ?? "000000";
                        var vno = docid.Length >= 5 ? docid.Substring(4) : "0";
                        var vtype = docid.Length >= 4 ? docid.Substring(0, 4) : "0000";

                        using (SqlCommand cmd = new SqlCommand("sp_UpdateLamination", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_NO", vno);
                            cmd.Parameters.AddWithValue("@V_TYPE", vtype);

                            // Optional parameters
                            cmd.Parameters.AddWithValue("@NWARPWAY_RES", (object?)detail.NWARPWAY_RES ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WARPWAY_RES", (object?)detail.WARPWAY_RES ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@NWEFTWAY_RES", (object?)detail.NWEFTWAY_RES ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@WEFTWAY_RES", (object?)detail.WEFTWAY_RES ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ELONG_WARP", (object?)detail.ELONG_WARP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ELONG_WEFT", (object?)detail.ELONG_WEFT ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@QC_REMARKS", (object?)detail.QC_REMARKS ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@STATUS_CODE_A", (object?)detail.STATUS_CODE_A ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@TENA_CODE_A", (object?)detail.TENA_CODE_A ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAMSUP_CODE", (object?)detail.LAMSUP_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAMSUP_NAME", (object?)detail.LAMSUP_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAMOP_CODE", (object?)detail.LAMOP_CODE ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@LAMOP_NAME", (object?)detail.LAMOP_NAME ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@QCUSER", (object?)detail.QCUSER ?? DBNull.Value);

                             
                            var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 5000)
                            {
                                Direction = ParameterDirection.Output
                            };
                            cmd.Parameters.Add(errorParam);

                            SqlParameter returnValue = new SqlParameter("@ReturnVal", SqlDbType.Int)
                            {
                                Direction = ParameterDirection.ReturnValue
                            };
                            cmd.Parameters.Add(returnValue);

                            if (con.State != ConnectionState.Open)
                                await con.OpenAsync();

                            await cmd.ExecuteNonQueryAsync();

                            int result = (int)returnValue.Value;
                            string error = errorParam.Value?.ToString();

                            if (result != 1)
                            {
                                // Optional: collect all errors and return them together
                                return BadRequest(new { success = false, message = error ?? "Unknown error during update." });
                            }
                        }
                    }

                    return Json(new { status = true, message = "All lamination records updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json( new { status = false, message = ex.Message });
            }
        }

    }
}
