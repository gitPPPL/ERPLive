using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using static iTextSharp.text.pdf.AcroFields;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class BigWeighbridgeController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public BigWeighbridgeController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            
            return View("~/Views/Weighbridge/Transaction/BigWeighbridge/Index.cshtml");
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

        public async Task<IActionResult> GetGateNo(string wbType)
        {
            try
            {
                var WbCondtion = string.Empty;
               
                if(wbType == "Raw Material")
                {
                    WbCondtion = " AND g.V_TYPE IN ('INRM') ";
                }
                else if (wbType == "Store")
                {
                    WbCondtion = "  AND g.V_TYPE IN ('INST') ";
                }
                else if (wbType == "Fuel")
                {
                    WbCondtion = "  AND g.V_TYPE IN ('INFU') ";
                }
                else if (wbType == "Sales")
                {
                    WbCondtion = "  AND g.V_TYPE IN ('TRGI') ";
                }                 
                else if (wbType == "Misc")
                {
                    WbCondtion = "  AND g.V_TYPE IN ('INMS') ";
                }
                else if (wbType == "JobWork")
                {
                    WbCondtion = "  AND g.V_TYPE IN ('INJB') ";
                }
                else if (wbType == "RGP")
                {
                    WbCondtion = "  AND g.V_TYPE IN ('INRT') ";
                }
                else if (wbType == "Sales Return" || wbType == "Outsider")
                {
                    WbCondtion = " AND g.V_TYPE IN ( select  DISTINCT CODE from DOCTYPE_MAST where DOCTYPE='GateInward' ) ";
                }
                else
                {
                    WbCondtion = " AND g.V_TYPE IN ( select  DISTINCT CODE from DOCTYPE_MAST where DOCTYPE='GateInward' ) ";
                }


               var userDt = _globalValue.GetGlobalVariables();
               string strqry = $@"
               SELECT V_NO,V_TYPE,TRUCK_NO, PARTY_CODE, sg.NAME partyName, d.NAME as VtypeName FROM GATE1 g 
               left join SUBGROUP_MAST sg on g.PARTY_CODE=sg.CODE and g.COMP_CODE=sg.COMP_CODE
               left join DOCTYPE_MAST d on g.V_TYPE=d.CODE 
               where g.COMP_CODE={userDt.PubCompCode}  and g.YEAR_CODE={userDt.PubFYearCode} and g.BRANCH_CODE=1  
               {WbCondtion} order by V_NO ";
                
                var gateList = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = gateList });
            }
            catch (Exception ex)
            {               
                return Json(new {status=false, message="data load failed"});
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='KantaBig' ");
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
                var itemlist = await _dbHelper.GetJsonDataAsync($@"select CODE, NAME,HSN_CODE,UNIT_NAME,UNIT_CODE from item_mast where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode} and WB_YN='Yes' order by NAME");
                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetPlaceMast()
        {
            try
            {
                var placelist = await _dbHelper.GetJsonDataAsync(@$" select CODE , NAME from ITEMDEPT_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} and TRAN_TYPE='Production' order by NAME ");
                return Json(new { status = true, data = placelist });
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
                var itemlist = await _dbHelper.GetJsonDataAsync($@"select CODE, NAME from SUBGROUP_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode} and ACTIVE=1 order by NAME");
                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeighBridgeById(string id)
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
                return Json(new { status = true, header = headerlist,  detail= detaillist });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateWeighBridgeEntry([FromBody] WBEntryModel model)
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
                            //var wgtDt = Convert.ToDateTime(model.V_DATE).ToString("dd-MMM-yyyy");
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_WBEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;                                
                                cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");                              
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE?? (object)DBNull.Value);
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

                                // ✅ Use only one STATUS parameter — FIXED here
                                cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                                // ❌ Removed this line: cmd.Parameters.AddWithValue("@status", 1);

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

                                // Table-valued parameter
                                var tvp = new SqlParameter("@WB2Data", SqlDbType.Structured)
                                {
                                    TypeName = "Type_WB2",
                                    Value = ToWB2DataTable(model.WB2Data)
                                };
                                cmd.Parameters.Add(tvp);

                                // Output and return value
                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                {
                                    Direction = ParameterDirection.ReturnValue
                                };
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

            foreach (var item in items ?? new List<TypeWB2>())
            {
                if (item.WEIGHT != null && item.WGT_DATE != null && item.WGT_TIME != null)
                {
                    //var wgtDt = Convert.ToDateTime(item.WGT_DATE).ToString("dd-MMM-yyyy");
                    decimal netWeight = Math.Abs(item.NET_WGT);
                    table.Rows.Add(
                        item.V_SHIFT, item.TYPE, item.WEIGHT, item.TARE_WGT, netWeight,
                        item.WGT_DATE, item.WGT_TIME, item.FROM_PLACE, item.FROM_NAME,
                        item.TO_PLACE, item.TO_NAME, item.ITEM_CODE, item.ITEM_NAME,
                        item.REMARKS, item.STATUS, item.Ref_type, item.Ref_no,
                        item.SNO, item.WGT_TIME, item.COND, item.MOIS_PER, item.MOIS_WT
                    );
                }
               
            }

            return table;
        }


    }
}
