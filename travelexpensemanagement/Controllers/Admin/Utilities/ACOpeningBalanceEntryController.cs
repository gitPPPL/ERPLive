using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Admin.SystemInitilization;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Utilities;
using travelexpensemanagement.Models.Payroll.Transaction;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    [SessionAuthorize]
    public class ACOpeningBalanceEntryController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public ACOpeningBalanceEntryController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/ACOpeningBalanceEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            var datalist = await _masterDataService.GetPartyListAsync();
            return Json(datalist);
        }

        [HttpGet]
        public async Task<IActionResult> GetVtypeList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("select CODE,NAME from DOCTYPE_MAST where DOCTYPE='OpeningBalance' order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetOpeningEntriesData(int partyCode)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCd = userSession.PubCompCode;
                var yearCd = userSession.PubFYearCode;
                //AND DOCTYPE_MAST.DOCTYPE = 'Openingbalance' ORDER BY LEDGER2.SNO
             string strqry= $@"
            select DOCTYPE_MAST.NAME[DOCTYPE_NAME],DOC_ID, V_TYPE,V_NO,V_DATE,SRNO,LEDGER2.SNO, NARRATION,
            (case when ISNULL(CR_CODe,0 )= {partyCode}  then 'CR' else 'DR' end)[DR_CR],CR_CODE,DR_CODE, AMT, BILL_NO,BILL_DATE  from LEDGER2 
            LEFT JOIN DOCTYPE_MAST ON DOCTYPE_MAST.CODE= LEDGER2.V_TYPE   where COMP_CODE = {companyCd} AND BRANCH_CODE = 1  AND YEAR_CODE = {yearCd}
            And (CR_CODE = {partyCode} Or DR_CODE = {partyCode})  
            AND DOCTYPE_MAST.DOCTYPE = 'Openingbalance' ORDER BY LEDGER2.SNO
            ";
                var dataList = await _dbHelper.GetJsonDataAsync(strqry);
            return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
            return Json(new { status = false, message = "data load failed" });
            }
           
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateAcOpeningBalanceEntry([FromBody] AcOpeningBalEntry model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request: Model is null." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    try
                    {
                        DataTable dtTable = await ToDataTable(model.ledger2);
                       var partyCode = model.partyCode;

                        using (SqlCommand cmd = new SqlCommand("[dbo].[sp_Ledger2]", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                            cmd.Parameters.AddWithValue("@DR_CODE", partyCode);
                            cmd.Parameters.AddWithValue("@CR_CODE", partyCode);
                            cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                            cmd.Parameters.AddWithValue("@Ledger2Table", dtTable);
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

                                return Json(new { status = true, message = "Data saved/updated successfully." });
                            }
                            else
                            {

                                return Json(new { status = false, message = errorMsg ?? "Operation failed." });
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                    }

                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Unexpected error: " + ex.Message });
            }
        }

        private async Task<DataTable> ToDataTable(List<ledger2Model> data)
        {
            var table = new DataTable();

            // Define columns matching the TVP Type_Ledger2
            table.Columns.Add("DOC_ID", typeof(string));
            table.Columns.Add("V_NO", typeof(int));
            table.Columns.Add("V_TYPE", typeof(string));
            table.Columns.Add("V_DATE", typeof(DateTime));
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("DR_CODE", typeof(int));
            table.Columns.Add("CR_CODE", typeof(int));
            table.Columns.Add("AMT", typeof(decimal));
            table.Columns.Add("NARRATION", typeof(string));
            table.Columns.Add("CHQ_NO", typeof(string));
            table.Columns.Add("CHQ_DATE", typeof(DateTime));
            table.Columns.Add("CLG_DATE", typeof(DateTime));
            table.Columns.Add("RTGS_TYPE", typeof(string));
            table.Columns.Add("RTGS_NO", typeof(string));
            table.Columns.Add("BILL_NO", typeof(string));
            table.Columns.Add("BILL_DATE", typeof(DateTime));
            table.Columns.Add("HOLD_TYPE", typeof(string));
            table.Columns.Add("HOLD_DATE", typeof(DateTime));
            table.Columns.Add("EMP_CODE", typeof(int));
            table.Columns.Add("SRNO", typeof(int));
            table.Columns.Add("USD_AMT", typeof(decimal));
            table.Columns.Add("USD_RATE", typeof(decimal));
            table.Columns.Add("FEXCH_BANKUSD", typeof(string));

            if (data != null && data.Count > 0)
            {
                foreach (var row in data)
                {
                    table.Rows.Add(
                        row.DOC_ID,
                        row.V_NO,
                        row.V_TYPE,
                        row.V_DATE,
                        row.SNO,
                        row.DR_CODE.HasValue ? row.DR_CODE.Value : (object)DBNull.Value,
                        row.CR_CODE.HasValue ? row.CR_CODE.Value : (object)DBNull.Value,
                        row.AMT.HasValue ? row.AMT.Value : (object)DBNull.Value,
                        row.NARRATION ?? (object)DBNull.Value,
                        row.CHQ_NO ?? (object)DBNull.Value,
                        row.CHQ_DATE.HasValue ? row.CHQ_DATE.Value : (object)DBNull.Value,
                        row.CLG_DATE.HasValue ? row.CLG_DATE.Value : (object)DBNull.Value,
                        row.RTGS_TYPE ?? (object)DBNull.Value,
                        row.RTGS_NO ?? (object)DBNull.Value,
                        row.BILL_NO ?? (object)DBNull.Value,
                        row.BILL_DATE.HasValue ? row.BILL_DATE.Value : (object)DBNull.Value,
                        row.HOLD_TYPE ?? (object)DBNull.Value,
                        row.HOLD_DATE.HasValue ? row.HOLD_DATE.Value : (object)DBNull.Value,
                        row.EMP_CODE.HasValue ? row.EMP_CODE.Value : (object)DBNull.Value,
                        row.SRNO.HasValue ? row.SRNO.Value : (object)DBNull.Value,
                        row.USD_AMT.HasValue ? row.USD_AMT.Value : (object)DBNull.Value,
                        row.USD_RATE.HasValue ? row.USD_RATE.Value : (object)DBNull.Value,
                        row.FEXCH_BANKUSD ?? (object)DBNull.Value
                    );
                }
            }

            return await Task.FromResult(table);
        }



    }
}
