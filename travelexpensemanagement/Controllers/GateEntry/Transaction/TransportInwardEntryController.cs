using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TransportInwardEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TransportInwardEntryController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/TransportInwardEntry/Index.cshtml");
        }

        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = "1";
                var vType = V_type;
                var tableName = "GATE1";

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
        public async Task<IActionResult> GetEmployeeList()
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
                select distinct e.CODE as EmpCd, e.NAME as EmpName, e.FATHER_NAME,isnull(d.NAME, '') as DEPT_CODE
                from EMP_MAST e left join DEPT_MAST d on e.DEPT_CODE=d.CODE 
                and e.COMP_CODE=d.COMP_CODE
                where e.COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} ";
                var data = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = data });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
              
        [HttpGet]
        public async Task<IActionResult> GetDepartmentList()
        {
            try
            {
                var departmentList = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from ITEMDEPT_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME");
                return Json(new { status = true, data = departmentList });
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
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='TruckInward' ");
                return Json(new { status = true, data = Doctype });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            try
            {
                var UserLoginData = _globalValue.GetGlobalVariables();
                var PartyList = await _dbHelper.GetJsonDataAsync($@"select distinct sg.CODE, sg.NAME, sg.ADD1,sg.ADD2,sg.ADD3,sg.PINCODE, isnull(cm.NAME, '') as CityName, isnull(s.name, '') state, sg.STATE_CODE,sg.CITY_CODE,sg.GSTIN from SUBGROUP_MAST sg left join CITY_MAST cm on sg.CITY_CODE=cm.CODE left join STATE_MAST s on s.code=sg.STATE_CODE  where sg.COMP_CODE={UserLoginData.PubCompCode} order by NAME ");
                return Json(new { status = true, data = PartyList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "data load failed" });
            }
        }
  
        [HttpGet]
        public async Task<IActionResult> GetTransportationList()
        {
            try
            {
                var transactionList = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME,PARTY_CODE from TRANSPORT_MAST where  COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");
                return Json(new { status = true, data = transactionList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTransportInwardRecordsById(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@DOC_ID", id},
                    {"@Action", "TransportInwardDataByID"}
                };
                var transportlist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetTransportInwardEntry]", parameter);
                return Json(new { status = true, data = transportlist });

            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateTransportInward([FromBody] TransportInwardModel POmodel)
        {
            if (POmodel == null)
                return Json(new { status = false, message = " data save failed." });
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
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TransportInwardEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Transaction = transaction;
                                cmd.CommandType = CommandType.StoredProcedure;

                                if (POmodel.SaveOrUpdate == "Save")
                                    cmd.Parameters.AddWithValue("@Action", "Add");
                                else
                                    cmd.Parameters.AddWithValue("@Action", "Edit");

                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);                                 
                                cmd.Parameters.AddWithValue("@V_NO", _dbHelper.Xnull(POmodel.V_NO));
                                cmd.Parameters.AddWithValue("@V_TYPE", _dbHelper.Xnull(POmodel.V_TYPE));
                                cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(POmodel.DOC_ID));
                                cmd.Parameters.AddWithValue("@TRF_TYPE", _dbHelper.Xnull(POmodel.TRF_TYPE));
                                cmd.Parameters.AddWithValue("@TRF_NO", _dbHelper.Xnull(POmodel.TRF_NO));
                                cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(POmodel.V_DATE));
                                cmd.Parameters.AddWithValue("@V_TIME", _dbHelper.Xnull(POmodel.V_TIME));
                                cmd.Parameters.AddWithValue("@ITEM_TYPE", _dbHelper.Xnull(POmodel.ITEM_TYPE));
                                cmd.Parameters.AddWithValue("@PARTY_CODE", _dbHelper.Xnull(POmodel.PARTY_CODE));
                                cmd.Parameters.AddWithValue("@ADD1", _dbHelper.Xnull(POmodel.ADD1));
                                cmd.Parameters.AddWithValue("@ADD2", _dbHelper.Xnull(POmodel.ADD2));
                                cmd.Parameters.AddWithValue("@ADD3", _dbHelper.Xnull(POmodel.ADD3));
                                cmd.Parameters.AddWithValue("@PARTY_CITY", _dbHelper.Xnull(POmodel.PARTY_CITY));
                                cmd.Parameters.AddWithValue("@PARTY_GST", _dbHelper.Xnull(POmodel.PARTY_GST));
                                cmd.Parameters.AddWithValue("@PARTY_PINCODE", _dbHelper.Xnull(POmodel.PARTY_PINCODE));
                                cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", _dbHelper.Xnull(POmodel.PARTY_ADDRESSID));
                                cmd.Parameters.AddWithValue("@BILL_NO", _dbHelper.Xnull(POmodel.BILL_NO));
                                cmd.Parameters.AddWithValue("@BILL_DATE", _dbHelper.Xnull(POmodel.BILL_DATE));
                                cmd.Parameters.AddWithValue("@CHALL_NO", _dbHelper.Xnull(POmodel.CHALL_NO));
                                cmd.Parameters.AddWithValue("@CHALL_DATE", _dbHelper.Xnull(POmodel.CHALL_DATE));
                                cmd.Parameters.AddWithValue("@TRUCK_NO", _dbHelper.Xnull(POmodel.TRUCK_NO));
                                cmd.Parameters.AddWithValue("@TRANSPORT_CODE", _dbHelper.Xnull(POmodel.TRANSPORT_CODE));
                                cmd.Parameters.AddWithValue("@DRIVER_NAME", _dbHelper.Xnull(POmodel.DRIVER_NAME));
                                cmd.Parameters.AddWithValue("@DRIVER_NO", _dbHelper.Xnull(POmodel.DRIVER_NO));
                                cmd.Parameters.AddWithValue("@TRANSIT_NO", _dbHelper.Xnull(POmodel.TRANSIT_NO));
                                cmd.Parameters.AddWithValue("@WAYBILL_NO", _dbHelper.Xnull(POmodel.WAYBILL_NO));
                                cmd.Parameters.AddWithValue("@BILL_AMT", _dbHelper.Xnull(POmodel.BILL_AMT));
                                cmd.Parameters.AddWithValue("@REMARKS", _dbHelper.Xnull(POmodel.REMARKS));
                                cmd.Parameters.AddWithValue("@DISP_PLAN_NO", _dbHelper.Xnull(POmodel.DISP_PLAN_NO));
                                cmd.Parameters.AddWithValue("@DISP_PLAN_TYPE", _dbHelper.Xnull(POmodel.DISP_PLAN_TYPE));
                                cmd.Parameters.AddWithValue("@WB_TYPE", _dbHelper.Xnull(POmodel.WB_TYPE));
                                cmd.Parameters.AddWithValue("@WB_NO", _dbHelper.Xnull(POmodel.WB_NO));
                                cmd.Parameters.AddWithValue("@MRN_TYPE", _dbHelper.Xnull(POmodel.MRN_TYPE));
                                cmd.Parameters.AddWithValue("@MRN_NO", _dbHelper.Xnull(POmodel.MRN_NO));
                                cmd.Parameters.AddWithValue("@REF_TYPE", _dbHelper.Xnull(POmodel.REF_TYPE));
                                cmd.Parameters.AddWithValue("@REF_NO", _dbHelper.Xnull(POmodel.REF_NO));
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", _dbHelper.Xnull(POmodel.FAPROV_STATUS));
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", _dbHelper.Xnull(POmodel.FAPROV_REMARKS));
                                cmd.Parameters.AddWithValue("@STATUS", _dbHelper.Xnull(POmodel.STATUS));
                                cmd.Parameters.AddWithValue("@ACTIVE", _dbHelper.Xnull(POmodel.ACTIVE));
                                cmd.Parameters.AddWithValue("@Remarks2", _dbHelper.Xnull(POmodel.Remarks2));
                                cmd.Parameters.AddWithValue("@PARTY_NAME", _dbHelper.Xnull(POmodel.PARTY_NAME));
                                cmd.Parameters.AddWithValue("@RC_NO", _dbHelper.Xnull(POmodel.RC_NO));
                                cmd.Parameters.AddWithValue("@DL_NO", _dbHelper.Xnull(POmodel.DL_NO));
                                cmd.Parameters.AddWithValue("@INSU_NO", _dbHelper.Xnull(POmodel.INSU_NO));
                                cmd.Parameters.AddWithValue("@PAN_NO", _dbHelper.Xnull(POmodel.PAN_NO));
                                cmd.Parameters.AddWithValue("@PURPOSE", _dbHelper.Xnull(POmodel.PURPOSE));
                                cmd.Parameters.AddWithValue("@IMAGEPATH", _dbHelper.Xnull(POmodel.IMAGEPATH));
                                cmd.Parameters.AddWithValue("@R_TIME", _dbHelper.Xnull(POmodel.R_TIME));
                                cmd.Parameters.AddWithValue("@OUT_TIME", _dbHelper.Xnull(POmodel.OUT_TIME));
                                cmd.Parameters.AddWithValue("@R_DATE", _dbHelper.Xnull(POmodel.R_DATE));
                                cmd.Parameters.AddWithValue("@OUT_DATE", _dbHelper.Xnull(POmodel.OUT_DATE));
                                cmd.Parameters.AddWithValue("@RETURN_TYPE", _dbHelper.Xnull(POmodel.RETURN_TYPE));
                                cmd.Parameters.AddWithValue("@QRCODE_NO", _dbHelper.Xnull(POmodel.QRCODE_NO));
                                cmd.Parameters.AddWithValue("@INOUT_ACTIVE", _dbHelper.Xnull(POmodel.INOUT_ACTIVE));
                                cmd.Parameters.AddWithValue("@OUT_ALLOWED", _dbHelper.Xnull(POmodel.OUT_ALLOWED));
                                cmd.Parameters.AddWithValue("@OUT_ALLOWEDBY", _dbHelper.Xnull(POmodel.OUT_ALLOWEDBY));
                                cmd.Parameters.AddWithValue("@RETURN_DATE", _dbHelper.Xnull(POmodel.RETURN_DATE));
                                cmd.Parameters.AddWithValue("@RESPONSIBLE_PERSON", _dbHelper.Xnull(POmodel.RESPONSIBLE_PERSON));
                                cmd.Parameters.AddWithValue("@INSU_EXPDT", _dbHelper.Xnull(POmodel.INSU_EXPDT));
                                cmd.Parameters.AddWithValue("@DL_EXPDT", _dbHelper.Xnull(POmodel.DL_EXPDT));
                                cmd.Parameters.AddWithValue("@CONTAINER_NO", _dbHelper.Xnull(POmodel.CONTAINER_NO));
                                cmd.Parameters.AddWithValue("@CONTAINER_SIZE", _dbHelper.Xnull(POmodel.CONTAINER_SIZE));
                                cmd.Parameters.AddWithValue("@SHIP_PARTY", _dbHelper.Xnull(POmodel.SHIP_PARTY));
                                cmd.Parameters.AddWithValue("@SHIP_BILLNO", _dbHelper.Xnull(POmodel.SHIP_BILLNO));
                                cmd.Parameters.AddWithValue("@SHIP_BILLDATE", _dbHelper.Xnull(POmodel.SHIP_BILLDATE));
                                cmd.Parameters.AddWithValue("@EWB_DATE", _dbHelper.Xnull(POmodel.EWB_DATE));
                                cmd.Parameters.AddWithValue("@EWB_EXPDATE", _dbHelper.Xnull(POmodel.EWB_EXPDATE));
                                cmd.Parameters.AddWithValue("@PARTY_WBTIME", _dbHelper.Xnull(POmodel.PARTY_WBTIME));
                                cmd.Parameters.AddWithValue("@EWB_INVNO", _dbHelper.Xnull(POmodel.EWB_INVNO));
                                cmd.Parameters.AddWithValue("@EWB_INVAMT", _dbHelper.Xnull(POmodel.EWB_INVAMT));
                                cmd.Parameters.AddWithValue("@PARTY_WBSLIPNO", _dbHelper.Xnull(POmodel.PARTY_WBSLIPNO));
                                cmd.Parameters.AddWithValue("@PARTY_WBGRWT", _dbHelper.Xnull(POmodel.PARTY_WBGRWT));
                                cmd.Parameters.AddWithValue("@PARTY_WBTRWT", _dbHelper.Xnull(POmodel.PARTY_WBTRWT));
                                cmd.Parameters.AddWithValue("@PARTY_EWBCITY", _dbHelper.Xnull(POmodel.PARTY_EWBCITY));
                                cmd.Parameters.AddWithValue("@GR_NO", _dbHelper.Xnull(POmodel.GR_NO));
                                cmd.Parameters.AddWithValue("@GR_DATE", _dbHelper.Xnull(POmodel.GR_DATE));
                                //cmd.Parameters.AddWithValue("@status", 1);
                                cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                                              
                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                cmd.Parameters.Add(returnParam);
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
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
                                message = success ? "Data save/update successfully." : "Failed to save or update some employee details."
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


    }
}
