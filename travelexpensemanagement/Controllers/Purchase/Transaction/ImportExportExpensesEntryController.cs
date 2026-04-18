using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Numerics;
using System.Security.AccessControl;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportExportExpensesEntryController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ImportExportExpensesEntryController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportExportExpensesEntry/Index.cshtml");
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
                var tableName = "PURCHASE1";

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
        public async Task<IActionResult> GetMakeList()
        {
            try
            {
                var makelist = await _dbHelper.GetJsonDataAsync($@" select CODE, NAME from ITEMMAKE_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");
                return Json(new { status = true, data = makelist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
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
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='PurchaseExpenses' ");
                return Json(new { status = true, data = Doctype });

            }
            catch(Exception ex)
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
        public async Task<IActionResult> GetPartyAddress(int partyCd)
        {
            try
            {
                var UserLoginData = _globalValue.GetGlobalVariables();
                var PartyAddList = await _dbHelper.GetJsonDataAsync($@"select distinct sg.code, sg.ADD1,sg.ADD2,sg.ADD3,sg.PINCODE, isnull(cm.NAME, '') as CityName ,sg.CITY_CODE,sg.GSTIN from SUBGROUP_MAST sg left join CITY_MAST cm on sg.CITY_CODE=cm.CODE  where sg.COMP_CODE={UserLoginData.PubCompCode}  and sg.code={partyCd} order by ADD1  ");
                return Json(new { status = true, data = PartyAddList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "data load failed" });
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
        public async Task<IActionResult> GetfreightDebAC()
        {
            try
            {
            var freightlistAc= await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from SUBGROUP_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} and isnull(NATURE, '')='Others' order by NAME ");
            return Json(new { status = true, data = freightlistAc });

            }
            catch(Exception ex)
            {
              return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTransitList()
        {
            try
            {
                var UserSessionData = _globalValue.GetGlobalVariables();
                string strqry = $@"select V_TYPE,V_NO,DOC_ID from  WAYBILL1 where COMP_CODE={UserSessionData.PubCompCode}
                and YEAR_CODE={UserSessionData.PubFYearCode} and BRANCH_CODE=1 and V_TYPE in ('TRIN', 'TROT') order by V_TYPE,V_NO ";

                var referenceList = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = referenceList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReferenceList()
        {
            try
            {
                var UserSessionData = _globalValue.GetGlobalVariables();
                string strqry = $@"select WAYBILL_NO,V_TYPE,V_NO,DOC_ID from  purchase1 where COMP_CODE={UserSessionData.PubCompCode}
                and YEAR_CODE={UserSessionData.PubFYearCode} and BRANCH_CODE=1 and V_TYPE in ('RIMP', 'RMPB') order by V_TYPE,V_NO ";
                var referenceList = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = referenceList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTaxList()
        {
            try
            {
                var taxList = await _dbHelper.GetJsonDataAsync("select CODE,NAME,SGST_PER as CSGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 from TAX_MAST order by NAME");
                return Json(new { status = true, data = taxList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetImpExpExpenseRecordsById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var userSession = _globalValue.GetGlobalVariables();
                string VType = id.Substring(0, 4);
                string VNo = id.Substring(4);
                var parametersHeader = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", VType},
                { "@V_NO", int.Parse(VNo) },
                { "@Action", "ImpExpHeaderData" }
                };

                var parametersDetail = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", VType},
                { "@V_NO", int.Parse(VNo) },
                { "@Action", "ImpExpDetailtableData" }
                };

                var parametersAttachment = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", VType},
                { "@V_NO", int.Parse(VNo) },
                { "@Action", "ImpExpAttachmentData" }
                };

                var header = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetImportExportExpenseEntry]", parametersHeader);
                var detail = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetImportExportExpenseEntry]", parametersDetail);
                var attachment = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetImportExportExpenseEntry]", parametersAttachment);
               
                return Json(new
                {
                    status = true,
                    header = header,
                    detail = detail,
                    attachment = attachment
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }
 
        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateImportExportExpense([FromBody] IEExpenseEntryModel model)
        {
            if (model == null)
                return Json(new { status = false, message = " data save failed." });
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    DataTable purchaseOrderTable = FillDataTable(model.itemDetail, "[dbo].[PURCHASE2_TYPE]");
                    DataTable purchaseOrderAttachmentTable = FillDataTable(model.Attachments, "[dbo].[PURCHASE3_TYPE]");
                   
                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ImportExportExpenseEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Transaction = transaction;
                                cmd.CommandType = CommandType.StoredProcedure;

                                if (model.SaveOrUpdate == "Save")
                                    cmd.Parameters.AddWithValue("@Action", "Add");
                                else
                                    cmd.Parameters.AddWithValue("@Action", "Edit");

                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PARTY_CODE", model.PARTY_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EXCH_RATE", model.EXCH_RATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CREDIT_AC", model.CREDIT_AC ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEBIT_AC", model.DEBIT_AC ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_ADD1", model.BILL_ADD1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_ADD2", model.BILL_ADD2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_ADD3", model.BILL_ADD3 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_CITY", model.BILL_CITY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_PINCODE", model.BILL_PINCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_ADDRESSID", model.BILL_ADDRESSID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_GST", model.BILL_GST ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_CODE", model.SHIP_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_ADD1", model.SHIP_ADD1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_ADD2", model.SHIP_ADD2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_ADD3", model.SHIP_ADD3 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_CITY", model.SHIP_CITY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_PINCODE", model.SHIP_PINCODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_ADDRESSID", model.SHIP_ADDRESSID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_GST", model.SHIP_GST ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_NO", model.BILL_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_DATE", model.BILL_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHALL_NO", model.CHALL_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHALL_DATE", model.CHALL_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UOM_CODE", model.UOM_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_TYPE", model.GATE_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_NO", model.GATE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REF_TYPE", model.REF_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REF_NO", model.REF_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PASS_TYPE", model.PASS_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PASS_NO", model.PASS_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRANSIT_NO", model.TRANSIT_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WAYBILL_NO", model.WAYBILL_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRANSPORT_CODE", model.TRANSPORT_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRANSPORT_NAME", model.TRANSPORT_NAME ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRANSPORT_AC", model.TRANSPORT_AC ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GR_NO", model.GR_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GR_DATE", model.GR_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRUCK_NO", model.TRUCK_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CONTAINER_NO", model.CONTAINER_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SEALED_VEHICLE", model.SEALED_VEHICLE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INPUT_TYPE", model.INPUT_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EXPS_TYPE", model.EXPS_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RECD_QTY", model.RECD_QTY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_QTY", model.BILL_QTY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AMOUNT", model.AMOUNT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISC_PER", model.DISC_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISC_AMT", model.DISC_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PACK_PER", model.PACK_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PACK_AMT", model.PACK_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CGST_PER", model.CGST_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CGST_AMT", model.CGST_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SGST_PER", model.SGST_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SGST_AMT", model.SGST_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IGST_PER", model.IGST_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IGST_AMT", model.IGST_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CESS_PER", model.CESS_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CESS_AMT", model.CESS_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VAT_PER", model.VAT_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VAT_AMT", model.VAT_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@OTH_AMT", model.OTH_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TCS_PER", model.TCS_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TCS_AMT", model.TCS_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ROUND_OFF", model.ROUND_OFF ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@NAMOUNT", model.NAMOUNT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DIFF_AMT", model.DIFF_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BANK_AMT", model.BANK_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BANK_RATE", model.BANK_RATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PL_NO", model.PL_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PL_DATE", model.PL_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILLAMT_USD", model.BILLAMT_USD ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRTPAY_AMT", model.FRTPAY_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRTPAY_TAXPER", model.FRTPAY_TAXPER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRTPAY_TAX", model.FRTPAY_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRTPAY_NAR", model.FRTPAY_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRTPAY_DRAC", model.FRTPAY_DRAC ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRTPAY_CRAC", model.FRTPAY_CRAC ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_TDSPER", model.FRT_TDSPER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_TDS", model.FRT_TDS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DR_FROM_TPT", model.DR_FROM_TPT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TDS_ACT", model.TDS_ACT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TDS_PER", model.TDS_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TDS_AMT", model.TDS_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_AMT", model.WB_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_TDSPER", model.WB_TDSPER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_TDS", model.WB_TDS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_DRACT", model.WB_DRACT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_CRACT", model.WB_CRACT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_NARR", model.WB_NARR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UL_AMT", model.UL_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UL_TDSPER", model.UL_TDSPER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UL_TDS", model.UL_TDS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UL_DRACT", model.UL_DRACT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UL_CRACT", model.UL_CRACT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UL_NARR", model.UL_NARR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QLT_DR_AMT", model.QLT_DR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QLT_DR_TAX", model.QLT_DR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QLT_DR_NAR", model.QLT_DR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QLT_CR_AMT", model.QLT_CR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QLT_CR_TAX", model.QLT_CR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QLT_CR_NAR", model.QLT_CR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RDF_DR_AMT", model.RDF_DR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RDF_DR_TAX", model.RDF_DR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RDF_DR_NAR", model.RDF_DR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RDF_CR_AMT", model.RDF_CR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RDF_CR_TAX", model.RDF_CR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RDF_CR_NAR", model.RDF_CR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QTY_DR_AMT", model.QTY_DR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QTY_DR_TAX", model.QTY_DR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QTY_DR_NAR", model.QTY_DR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QTY_CR_AMT", model.QTY_CR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QTY_CR_TAX", model.QTY_CR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QTY_CR_NAR", model.QTY_CR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_DR_AMT", model.QC_DR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_DR_TAX", model.QC_DR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_DR_NAR", model.QC_DR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_CR_AMT", model.QC_CR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_CR_TAX", model.QC_CR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_CR_NAR", model.QC_CR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@OTH_DR_AMT", model.OTH_DR_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@OTH_DR_TAX", model.OTH_DR_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@OTH_DR_NAR", model.OTH_DR_NAR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_TYPE", model.QC_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@QC_NO", model.QC_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TAX_HOLD", model.TAX_HOLD ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PRICE_TYPE", model.PRICE_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", model.FAPROV_STATUS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", model.FAPROV_REMARKS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@HOLD_PAY", model.HOLD_PAY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@HOLD_REASON", model.HOLD_REASON ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@HOLD_DATE", model.HOLD_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IMPORT_AMT", model.IMPORT_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IMPORT_TAX", model.IMPORT_TAX ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INVLAND_AMT", model.INVLAND_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RCM_NO", model.RCM_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DRNOTE_MAILSEND", model.DRNOTE_MAILSEND ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_BILLNO", model.FRT_BILLNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_BILLDT", model.FRT_BILLDT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_PASSDT", model.FRT_PASSDT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_CHQ", model.FRT_CHQ ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_REMARK", model.FRT_REMARK ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GSTRMAIL_PARTYCNTR", model.GSTRMAIL_PARTYCNTR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GSTRMAIL_BILLCNTR", model.GSTRMAIL_BILLCNTR ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TDS_PER194Q", model.TDS_PER194Q ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TDS_AMT194Q", model.TDS_AMT194Q ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISP_ADDRESS", model.DISP_ADDRESS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISP_CITY", model.DISP_CITY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GSTRECO_REFTYPE", model.GSTRECO_REFTYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GSTRECO_REFNO", model.GSTRECO_REFNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STOREIMG_FLG", model.STOREIMG_FLG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RET_TYPE", model.RET_TYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FEXCH_USD", model.FEXCH_USD ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRP_GSTNO", model.TRP_GSTNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRP_BILLNO", model.TRP_BILLNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRP_BILLDATE", model.TRP_BILLDATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRP_TAXTYPE", model.TRP_TAXTYPE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MONTH_3B", model.MONTH_3B ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MONTH_3BN", model.MONTH_3BN ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRP_MONTH3B", model.TRP_MONTH3B ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MTH_REVYN3B", model.MTH_REVYN3B ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRP_MTHREVYN3B", model.TRP_MTHREVYN3B ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@MONTH_2B", model.MONTH_2B ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EWB_DATE", model.EWB_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EWB_EXPDATE", model.EWB_EXPDATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EWB_INVNO", model.EWB_INVNO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PL_AMT", model.PL_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId);
                                cmd.Parameters.AddWithValue("@WSID", "");
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);

                                var tvp = cmd.Parameters.AddWithValue("@PURCHASE2_TYPE", purchaseOrderTable);
                                tvp.SqlDbType = SqlDbType.Structured;
                                tvp.TypeName = "[dbo].[PURCHASE2_TYPE]";

                                var tvp2 = cmd.Parameters.AddWithValue("@PURCHASE3_TYPE", purchaseOrderAttachmentTable);
                                tvp2.SqlDbType = SqlDbType.Structured;
                                tvp2.TypeName = "[dbo].[PURCHASE3_TYPE]";

                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                cmd.Parameters.Add(returnParam);
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);
                                await cmd.ExecuteNonQueryAsync();
                                string errorMessage = errorParam.Value?.ToString();
                                Int16 ruturnParamMessage = Convert.ToInt16(returnParam.Value);
                                //if (errorMessage != "")
                                //    success = false;
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

        private DataTable FillDataTable<T>(List<T> data, string typeName)
        {
            int x = 1;
            DataTable PurchaseOrderTbl = ToEmptyDataTable(typeName);

            switch (typeName)
            {

                case "[dbo].[PURCHASE3_TYPE]":
                    var attachmentData = data as List<PurchaseAttachment>;
                    if (attachmentData == null || !attachmentData.Any())
                    {
                        return PurchaseOrderTbl;
                    }

                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments", "Purchase");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    foreach (var attachment in attachmentData)
                    {
                        if (attachment.FileName != null && attachment.FileContentBase64 != null)
                        {
                            string sanitizedFileName = Path.GetFileName(attachment.FileName);
                            string fullPath = Path.Combine(folderPath, sanitizedFileName);
                            string relativePath = $"/attachments/Purchase/{sanitizedFileName}";

                            byte[] fileBytes = Convert.FromBase64String(attachment.FileContentBase64);
                            System.IO.File.WriteAllBytes(fullPath, fileBytes);
                            attachment.FilePath = $"/attachments/Purchase/{sanitizedFileName}";

                            PurchaseOrderTbl.Rows.Add(
                                relativePath,
                                sanitizedFileName,
                                x++
                            );

                        }
                    }

                    break;

                case "[dbo].[PURCHASE2_TYPE]":
                    foreach (var detail in data.Cast<Purchase2Model>())
                    {
                        PurchaseOrderTbl.Rows.Add(
    detail.SNO,
    detail.ITEM_CODE,
    detail.ITEM_NAME,
    detail.MAKE_CODE,
    detail.HSN_CODE,
    detail.RCM_YN,
    detail.INPUT_YN,
    detail.UOM_CODE,
    detail.UOM_NAME,
    detail.DEPT_CODE,
    detail.NOS,
    detail.PLUS_MINUSQTY,
    detail.WB_QTY,
    detail.RECD_QTY,
    detail.BILL_QTY,
    detail.USD_RATE,
    detail.EXCH_RATE,
    detail.RATE,
    detail.AMOUNT,
    detail.DISC_PER,
    detail.DISC_AMT,
    detail.PACK_PER,
    detail.PACK_AMT,
    detail.TAX_CODE,
    detail.CGST_PER,
    detail.CGST_AMT,
    detail.SGST_PER,
    detail.SGST_AMT,
    detail.IGST_PER,
    detail.IGST_AMT,
    detail.CESS_PER,
    detail.CESS_AMT,
    detail.VAT_PER,
    detail.VAT_AMT,
    detail.OTH_AMT,
    detail.NET_AMT,
    detail.LAND_RATE,
    detail.LAND_AMT,
    detail.POLAND_RATE,
    detail.PO_RATE,
    detail.BIN_LOCATION,
    detail.BIN_CODE,
    detail.PO_TYPE,
    detail.PO_NO,
    detail.SAUDA_TYPE,
    detail.SAUDA_NO,
    detail.KANTA_TYPE,
    detail.KANTA_NO,
    detail.REQ_TYPE,
    detail.REQ_NO,
    detail.GATE_TYPE,
    detail.GATE_NO,
    detail.REF_TYPE,
    detail.REF_NO,
    detail.QC_TYPE,
    detail.QC_NO,
    detail.PASS_TYPE,
    detail.PASS_NO,
    detail.EMPTY_YN,
    detail.MACH_CODE,
    detail.REMARKS,
    detail.RATE_MONTHLY,
    detail.RATE_QUARTERLY,
    detail.RATE_ANNUALY,
    detail.RATE_SPECIAL,
    detail.FINAL_LOCK
);
                    }
                    break;

                default:
                    PurchaseOrderTbl = null;
                    break;
            }

            return PurchaseOrderTbl;

        }

        private DataTable ToEmptyDataTable(string typeName)
        {
            var dt = new DataTable();
            switch (typeName)
            {
                case "[dbo].[PURCHASE2_TYPE]":
                    dt.Columns.Add("SNO", typeof(int));
                    dt.Columns.Add("ITEM_CODE", typeof(int));
                    dt.Columns.Add("ITEM_NAME", typeof(string));
                    dt.Columns.Add("MAKE_CODE", typeof(int));
                    dt.Columns.Add("HSN_CODE", typeof(string));
                    dt.Columns.Add("RCM_YN", typeof(string));
                    dt.Columns.Add("INPUT_YN", typeof(string));
                    dt.Columns.Add("UOM_CODE", typeof(int));
                    dt.Columns.Add("UOM_NAME", typeof(string));
                    dt.Columns.Add("DEPT_CODE", typeof(int));
                    dt.Columns.Add("NOS", typeof(int));
                    dt.Columns.Add("PLUS_MINUSQTY", typeof(decimal));                   
                    dt.Columns.Add("WB_QTY", typeof(decimal));
                    dt.Columns.Add("RECD_QTY", typeof(decimal));
                    dt.Columns.Add("BILL_QTY", typeof(decimal));
                    dt.Columns.Add("USD_RATE", typeof(decimal));
                    dt.Columns.Add("EXCH_RATE", typeof(decimal));
                    dt.Columns.Add("RATE", typeof(decimal));
                    dt.Columns.Add("AMOUNT", typeof(decimal));
                    dt.Columns.Add("DISC_PER", typeof(decimal));
                    dt.Columns.Add("DISC_AMT", typeof(decimal));
                    dt.Columns.Add("PACK_PER", typeof(decimal));
                    dt.Columns.Add("PACK_AMT", typeof(decimal));
                    dt.Columns.Add("TAX_CODE", typeof(int));
                    dt.Columns.Add("CGST_PER", typeof(decimal));
                    dt.Columns.Add("CGST_AMT", typeof(decimal));
                    dt.Columns.Add("SGST_PER", typeof(decimal));
                    dt.Columns.Add("SGST_AMT", typeof(decimal));
                    dt.Columns.Add("IGST_PER", typeof(decimal));
                    dt.Columns.Add("IGST_AMT", typeof(decimal));
                    dt.Columns.Add("CESS_PER", typeof(decimal));
                    dt.Columns.Add("CESS_AMT", typeof(decimal));
                    dt.Columns.Add("VAT_PER", typeof(decimal));
                    dt.Columns.Add("VAT_AMT", typeof(decimal));
                    dt.Columns.Add("OTH_AMT", typeof(decimal));
                    dt.Columns.Add("NET_AMT", typeof(decimal));
                    dt.Columns.Add("LAND_RATE", typeof(decimal));
                    dt.Columns.Add("LAND_AMT", typeof(decimal));
                    dt.Columns.Add("POLAND_RATE", typeof(decimal));
                    dt.Columns.Add("PO_RATE", typeof(decimal));
                    dt.Columns.Add("BIN_LOCATION", typeof(string));
                    dt.Columns.Add("BIN_CODE", typeof(int));
                    dt.Columns.Add("PO_TYPE", typeof(string));
                    dt.Columns.Add("PO_NO", typeof(int));
                    dt.Columns.Add("SAUDA_TYPE", typeof(string));
                    dt.Columns.Add("SAUDA_NO", typeof(int));
                    dt.Columns.Add("KANTA_TYPE", typeof(string));
                    dt.Columns.Add("KANTA_NO", typeof(int));
                    dt.Columns.Add("REQ_TYPE", typeof(string));
                    dt.Columns.Add("REQ_NO", typeof(int));
                    dt.Columns.Add("GATE_TYPE", typeof(string));
                    dt.Columns.Add("GATE_NO", typeof(int));
                    dt.Columns.Add("REF_TYPE", typeof(string));
                    dt.Columns.Add("REF_NO", typeof(int));
                    dt.Columns.Add("QC_TYPE", typeof(string));
                    dt.Columns.Add("QC_NO", typeof(int));
                    dt.Columns.Add("PASS_TYPE", typeof(string));
                    dt.Columns.Add("PASS_NO", typeof(int));
                    dt.Columns.Add("EMPTY_YN", typeof(string));
                    dt.Columns.Add("MACH_CODE", typeof(int));
                    dt.Columns.Add("REMARKS", typeof(string));
                    dt.Columns.Add("RATE_MONTHLY", typeof(decimal));
                    dt.Columns.Add("RATE_QUARTERLY", typeof(decimal));
                    dt.Columns.Add("RATE_ANNUALY", typeof(decimal));
                    dt.Columns.Add("RATE_SPECIAL", typeof(decimal));
                    dt.Columns.Add("FINAL_LOCK", typeof(string));
                    break;

                case "[dbo].[PURCHASE3_TYPE]":
                    dt.Columns.Add("ATTACHMENT", typeof(string));
                    dt.Columns.Add("FILE_NAME", typeof(string));
                    dt.Columns.Add("SRNO", typeof(int));
                    break;

                default:
                    throw new ArgumentException("Unknown table type: " + typeName);
            }
            return dt;
        }


    }
}
