using iTextSharp.text.pdf.parser.clipper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class JobworkIssueChallanController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataservice;
        public JobworkIssueChallanController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataservice = masterDataService;
        }
        public IActionResult Index()
        {
            TempData["LoginDate"] = _globalValue.GetGlobalVariables().PubLoginDate;
            ViewBag.CurrentMenu = "Jobwork Issue Challan";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/JobworkIssueChallan/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            var dataList = await _masterDataservice.GetMaxVNoAsync(V_type, "sale1");
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetDoNoList(string vType, int vNo)
        {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var Docid = vType + vNo;

                var dataList = await _dbHelper.GetJsonDataAsync($@"
                select concat(V_Type,V_No) as DocId from DO1 where Concat(ref_type,ref_No)='{Docid}'
                and comp_code={usersessionDt.PubCompCode} and Branch_Code=1 and Year_Code={usersessionDt.PubFYearCode}
                ");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrecyList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("Select CODE,ShortName as NAME from currency_mast Order by CODE");
                return Json(new { status = true, data = dataList });
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
                var dataList = await _dbHelper.GetJsonDataAsync("Select CODE,NAME from DOCTYPE_MAST where DOCTYPE in ('SalesInvoice','SaleChallan') and CODE<>'SASI' order by NAME ");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetFormList()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"Select CODE,NAME from FORM_MAST where comp_code={companyCd} order by NAME ");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetItemGroupList()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"Select Distinct SALE_GROUP from ITEM_GROUP where comp_code={companyCd} order by SALE_GROUP ");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetGodownList()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"Select CODE,NAME from GODOWN_MAST where comp_code={companyCd} order by SNO");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetTransportModeList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("Select CODE,NAME from TRANSPORT_MODE order by CODE");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetDocStatusList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("Select CODE,NAME from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            var dataList = await _masterDataservice.GetPartyListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyAddress(int partyCd)
        {
            try
            {
                var UserLoginData = _globalValue.GetGlobalVariables();
                var PartyAddList = await _dbHelper.GetJsonDataAsync($@"select distinct sg.code, sg.ADD1,sg.ADD2,sg.ADD3,sg.PINCODE, isnull(cm.NAME, '') as CityName ,sg.CITY_CODE,sg.GSTIN from SUBGROUP_ADDRESS sg left join CITY_MAST cm on sg.CITY_CODE=cm.CODE  where sg.COMP_CODE={UserLoginData.PubCompCode}  and sg.code={partyCd} order by ADD1  ");
                return Json(new { status = true, data = PartyAddList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPayTermList()
        {
            var dataList = await _masterDataservice.GetPaymentTermListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetLCList()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"Select LC_NO LCNO from LC_MAST where Comp_code={companyCd} Order by LC_DATE desc");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetNatureList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("  Select distinct INSU_DETAIL as NAME from SALE1 where V_type in ('SAJI','SAJR')  and isnull(INSU_DETAIL,'')<>'' and comp_code=1");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTaxMastList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync("select CODE,NAME  from TAX_MAST order by NAME ");
                return Json(new { status = true, data = dataList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = "data load failed" + ex });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSaudaList()
        {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var companyCd = usersessionDt.PubCompCode;
                var yearCd = usersessionDt.PubFYearCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"  Select DOC_ID,V_TYPE,V_NO from SAUDA where COMP_CODE={companyCd} and YEAR_CODE={yearCd} and BRANCH_CODE=1 order by DOC_ID ");
                return Json(new { status = true, data = dataList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = "data load failed" + ex });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeighBridge()
        {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var companyCd = usersessionDt.PubCompCode;
                var yearCd = usersessionDt.PubFYearCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"Select DOC_ID,V_TYPE,V_NO from WB1 where COMP_CODE={companyCd} and YEAR_CODE={yearCd} and BRANCH_CODE=1 order by DOC_ID ");
                return Json(new { status = true, data = dataList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = "data load failed" + ex });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTransportMastList()
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var dataList = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from TRANSPORT_MAST where COMP_CODE={companyCd} order by NAME ");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCityMastList()
        {
            var dataList = await _masterDataservice.GetCityListAsync();
            return Json(dataList);
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
       public async Task<IActionResult> GetJobworkIssueData(string id)
       {
            try
            {
                if(id==null || id=="")
                {
                    return Json(new { status = false, message = "data load failed" });
                }
                var usersessionDt = _globalValue.GetGlobalVariables();
                var vtype= id.Substring(0, 4);
                var vNo= id.Substring(4);
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersessionDt.PubCompCode },
                    {"@YEAR_CODE", usersessionDt.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", vtype},
                    {"@V_NO", vNo},
                    {"@Action", "Sale1ForUpdate" }
                };
                var parameter1 = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersessionDt.PubCompCode },
                    {"@YEAR_CODE", usersessionDt.PubFYearCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE", vtype},
                    {"@V_NO", vNo},
                    {"@Action", "Sale2ForUpdate"}
                };

                var header = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_SaleDataList]", parameter);
                var detail = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_SaleDataList]", parameter1);

                return Json(new { status = true, header = header, detail=detail });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
       }
        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateJobWorkIssueData([FromBody] Sale1 model)
        {
            try
            {
                if (model == null)
                    return Json(new { status = false, message = "data save failed" });
                var usersessionDt = _globalValue.GetGlobalVariables();
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            int result = 0;
                            DataTable dataTable = new DataTable();
                            dataTable = FillDataTable(model.sale2s);

                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_SaleEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                // Add all parameters from the Sale1 model
                                cmd.Parameters.AddWithValue("@Action", model.SaveOrUpdate == "Save" ? "Add" : "Edit");
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@V_TYPE", _dbHelper.Xnull(model.V_TYPE));
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(model.DOC_ID));
                                cmd.Parameters.AddWithValue("@FORM_CODE", model.FORM_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_CODE", model.BILL_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_NAME", _dbHelper.Xnull(model.BILL_NAME));
                                cmd.Parameters.AddWithValue("@BILL_ADD1", _dbHelper.Xnull(model.BILL_ADD1));
                                cmd.Parameters.AddWithValue("@BILL_ADD2", _dbHelper.Xnull(model.BILL_ADD2));
                                cmd.Parameters.AddWithValue("@BILL_ADD3", _dbHelper.Xnull(model.BILL_ADD3));
                                cmd.Parameters.AddWithValue("@BILL_CITY", model.BILL_CITY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILL_GST", _dbHelper.Xnull(model.BILL_GST));
                                cmd.Parameters.AddWithValue("@BILL_PINCODE", _dbHelper.Xnull(model.BILL_PINCODE));
                                cmd.Parameters.AddWithValue("@BILL_ADDRESSID", model.BILL_ADDRESSID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GODOWN_CODE", model.GODOWN_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_CODE", model.SHIP_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_NAME", _dbHelper.Xnull(model.SHIP_NAME));
                                cmd.Parameters.AddWithValue("@SHIP_ADD1", _dbHelper.Xnull(model.SHIP_ADD1));
                                cmd.Parameters.AddWithValue("@SHIP_ADD2", _dbHelper.Xnull(model.SHIP_ADD2));
                                cmd.Parameters.AddWithValue("@SHIP_ADD3", _dbHelper.Xnull(model.SHIP_ADD3));
                                cmd.Parameters.AddWithValue("@SHIP_CITY", model.SHIP_CITY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_GST", _dbHelper.Xnull(model.SHIP_GST));
                                cmd.Parameters.AddWithValue("@SHIP_PINCODE", _dbHelper.Xnull(model.SHIP_PINCODE));
                                cmd.Parameters.AddWithValue("@SHIP_ADDRESSID", model.SHIP_ADDRESSID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", _dbHelper.Xnull(model.IMPORT_CURRENCY));
                                cmd.Parameters.AddWithValue("@EXRATE", model.EXRATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TAX_CODE", model.TAX_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PACK_TYPE", _dbHelper.Xnull(model.PACK_TYPE));
                                cmd.Parameters.AddWithValue("@PACK_NO", model.PACK_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ITEM_TYPE", _dbHelper.Xnull(model.ITEM_TYPE));
                                cmd.Parameters.AddWithValue("@AGENT_CODE", model.AGENT_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEFECTIVE_GOODS", model.DEFECTIVE_GOODS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CAL_ONPCS", model.CAL_ONPCS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PRINT_DETAIL", model.PRINT_DETAIL ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TOT_NOS", model.TOT_NOS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TOT_GROSS", model.TOT_GROSS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TOT_NET", model.TOT_NET ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_QTY", model.WB_QTY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AMOUNT", model.AMOUNT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TCS_PER", model.TCS_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TCS_AMT", model.TCS_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PACK_PER", model.PACK_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PACK_AMT", model.PACK_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISC_PER", model.DISC_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISC_AMT", model.DISC_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CGST_PER", model.CGST_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CGST_AMT", model.CGST_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SGST_PER", model.SGST_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SGST_AMT", model.SGST_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IGST_PER", model.IGST_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IGST_AMT", model.IGST_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CESS_PER", model.CESS_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CESS_AMT", model.CESS_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INSU_PER", model.INSU_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INSU_AMT", model.INSU_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ROUND_OFF", model.ROUND_OFF ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@NAMOUNT", model.NAMOUNT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TDS_PER", model.TDS_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TDS_AMT", model.TDS_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_AMT", model.FRT_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_TOPAY", model.FRT_TOPAY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_BILLNO", _dbHelper.Xnull(model.FRT_BILLNO));
                                cmd.Parameters.AddWithValue("@FRT_BILLDT", _dbHelper.Xnull(model.FRT_BILLDT));
                                cmd.Parameters.AddWithValue("@FRT_PASSDT", model.FRT_PASSDT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_CHQ", _dbHelper.Xnull(model.FRT_CHQ));
                                cmd.Parameters.AddWithValue("@FRT_REMARK", _dbHelper.Xnull(model.FRT_REMARK));
                                cmd.Parameters.AddWithValue("@TRANSPORT_CODE", model.TRANSPORT_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TRANSPORT_NAME", _dbHelper.Xnull(model.TRANSPORT_NAME));
                                cmd.Parameters.AddWithValue("@GR_NO", _dbHelper.Xnull(model.GR_NO));
                                cmd.Parameters.AddWithValue("@GR_DATE", model.GR_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@VEHICLE_NO", _dbHelper.Xnull(model.VEHICLE_NO));
                                cmd.Parameters.AddWithValue("@DRIVER_NAME", _dbHelper.Xnull(model.DRIVER_NAME));
                                cmd.Parameters.AddWithValue("@DRIVER_NO", _dbHelper.Xnull(model.DRIVER_NO));
                                cmd.Parameters.AddWithValue("@TPT_MODE", model.TPT_MODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TPT_DISTANCE", model.TPT_DISTANCE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARK", _dbHelper.Xnull(model.REMARK));
                                cmd.Parameters.AddWithValue("@WB_TYPE", _dbHelper.Xnull(model.WB_TYPE));
                                cmd.Parameters.AddWithValue("@WB_NO", model.WB_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INSU_TYPE", _dbHelper.Xnull(model.INSU_TYPE));
                                cmd.Parameters.AddWithValue("@LOAD_PER", model.LOAD_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LOAD_AMT", model.LOAD_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LOAD_REM", _dbHelper.Xnull(model.LOAD_REM));
                                cmd.Parameters.AddWithValue("@LOAD_AC", _dbHelper.Xnull(model.LOAD_AC));
                                cmd.Parameters.AddWithValue("@WB_AMT", model.WB_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WB_AC", _dbHelper.Xnull(model.WB_AC));
                                cmd.Parameters.AddWithValue("@WB_REM", _dbHelper.Xnull(model.WB_REM));
                                cmd.Parameters.AddWithValue("@WAYBILL_NO", _dbHelper.Xnull(model.WAYBILL_NO));
                                cmd.Parameters.AddWithValue("@INSU_NO", model.INSU_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SAUDA_TYPE", _dbHelper.Xnull(model.SAUDA_TYPE));
                                cmd.Parameters.AddWithValue("@SAUDA_NO", model.SAUDA_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SAUDA_RATE", model.SAUDA_RATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ORD_AMT", model.ORD_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMM_RATE1", model.COMM_RATE1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMM_RATE2", model.COMM_RATE2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GST_RATE", model.GST_RATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@TDS_RATE", model.TDS_RATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@STATUS", model.STATUS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@RCM_NO", _dbHelper.Xnull(model.RCM_NO));
                                cmd.Parameters.AddWithValue("@PAYREF_DOCID", _dbHelper.Xnull(model.PAYREF_DOCID));
                                cmd.Parameters.AddWithValue("@PAY_AMT", model.PAY_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@GATE_TYPE", _dbHelper.Xnull(model.GATE_TYPE));
                                cmd.Parameters.AddWithValue("@GATE_NO", model.GATE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REF_TYPE", _dbHelper.Xnull(model.REF_TYPE));
                                cmd.Parameters.AddWithValue("@REF_NO", model.REF_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REF_DATE", model.REF_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@ISSUE_TYPE", _dbHelper.Xnull(model.ISSUE_TYPE));
                                cmd.Parameters.AddWithValue("@ISSUE_NO", model.ISSUE_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BUYER_ORDNO", _dbHelper.Xnull(model.BUYER_ORDNO));
                                cmd.Parameters.AddWithValue("@PLACE_RECEIPT", _dbHelper.Xnull(model.PLACE_RECEIPT));
                                cmd.Parameters.AddWithValue("@PORT_LOADING", _dbHelper.Xnull(model.PORT_LOADING));
                                cmd.Parameters.AddWithValue("@PORT_DISCHARGE", _dbHelper.Xnull(model.PORT_DISCHARGE));
                                cmd.Parameters.AddWithValue("@FINAL_DEST", _dbHelper.Xnull(model.FINAL_DEST));
                                cmd.Parameters.AddWithValue("@FINAL_DEST_COUNTRY", _dbHelper.Xnull(model.FINAL_DEST_COUNTRY));
                                cmd.Parameters.AddWithValue("@DELIVERY_TERMS", _dbHelper.Xnull(model.DELIVERY_TERMS));
                                cmd.Parameters.AddWithValue("@LUT_DETAIL", _dbHelper.Xnull(model.LUT_DETAIL));
                                cmd.Parameters.AddWithValue("@INSU_DETAIL", _dbHelper.Xnull(model.INSU_DETAIL));
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", _dbHelper.Xnull(model.FAPROV_STATUS));
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", _dbHelper.Xnull(model.FAPROV_REMARKS));
                                cmd.Parameters.AddWithValue("@COND_DATE", model.COND_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COND_MNTH", model.COND_MNTH ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@APPROVAL_USER", model.APPROVAL_USER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@IRN", _dbHelper.Xnull(model.IRN));
                                cmd.Parameters.AddWithValue("@SIGNED_JSON", _dbHelper.Xnull(model.SIGNED_JSON));
                                cmd.Parameters.AddWithValue("@SIGNED_QR", _dbHelper.Xnull(model.SIGNED_QR));
                                cmd.Parameters.AddWithValue("@EINVOICE_FLG", model.EINVOICE_FLG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EWAYBILL_FLG", model.EWAYBILL_FLG ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@EWAYBILL_NO", _dbHelper.Xnull(model.EWAYBILL_NO));
                                cmd.Parameters.AddWithValue("@EWAYBILL_JSON", _dbHelper.Xnull(model.EWAYBILL_JSON));
                                cmd.Parameters.AddWithValue("@EWAYBILL_DATE", _dbHelper.Xnull(model.EWAYBILL_DATE));
                                cmd.Parameters.AddWithValue("@CDISC_AMT", model.CDISC_AMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CDISC_PER", model.CDISC_PER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SUPPLY_TYPE", _dbHelper.Xnull(model.SUPPLY_TYPE));
                                cmd.Parameters.AddWithValue("@LC_NO", _dbHelper.Xnull(model.LC_NO));
                                cmd.Parameters.AddWithValue("@TRADE_TERM", _dbHelper.Xnull(model.TRADE_TERM));
                                cmd.Parameters.AddWithValue("@DISP_PLACE", _dbHelper.Xnull(model.DISP_PLACE));
                                cmd.Parameters.AddWithValue("@SHIPMENT_TYPE", _dbHelper.Xnull(model.SHIPMENT_TYPE));
                                cmd.Parameters.AddWithValue("@MODEOF_PAYMENT", _dbHelper.Xnull(model.MODEOF_PAYMENT));
                                cmd.Parameters.AddWithValue("@INCOTERM", _dbHelper.Xnull(model.INCOTERM));
                                cmd.Parameters.AddWithValue("@FRT_TAXPER", model.FRT_TAXPER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FRT_TAXAMT", model.FRT_TAXAMT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SB_NO", _dbHelper.Xnull(model.SB_NO));
                                cmd.Parameters.AddWithValue("@SB_DATE", model.SB_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PORT_CODE", _dbHelper.Xnull(model.PORT_CODE));
                                cmd.Parameters.AddWithValue("@TRAN_TYPE", _dbHelper.Xnull(model.TRAN_TYPE));
                                cmd.Parameters.AddWithValue("@EWAYBILL_VALIDDATE", _dbHelper.Xnull(model.EWAYBILL_VALIDDATE));
                                cmd.Parameters.AddWithValue("@DEL_DATE", model.DEL_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FOB_VALUE", model.FOB_VALUE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FOB_FRT", model.FOB_FRT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FOB_INSU", model.FOB_INSU ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@FOB_OTHER", model.FOB_OTHER ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BILLOF_LADING", _dbHelper.Xnull(model.BILLOF_LADING));
                                cmd.Parameters.AddWithValue("@EXPV_TYPE", _dbHelper.Xnull(model.EXPV_TYPE));
                                cmd.Parameters.AddWithValue("@EXPV_NO", model.EXPV_NO ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_TYPE", _dbHelper.Xnull(model.SHIP_TYPE));
                                cmd.Parameters.AddWithValue("@CURRENCY", _dbHelper.Xnull(model.CURRENCY));
                                cmd.Parameters.AddWithValue("@BANK_CODE", model.BANK_CODE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEL_SCH", _dbHelper.Xnull(model.DEL_SCH));
                                cmd.Parameters.AddWithValue("@LUT_NO", _dbHelper.Xnull(model.LUT_NO));
                                cmd.Parameters.AddWithValue("@LUT_DATE", model.LUT_DATE ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PAY_TERM", model.PAY_TERM ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SOLD_BY", model.SOLD_BY ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INV_STATUS", model.INV_STATUS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@INSUCR_DAYS", model.INSUCR_DAYS ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@CONTAINER_SIZE", _dbHelper.Xnull(model.CONTAINER_SIZE));
                                // Add user/session parameters
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
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
                                if ((int)returnParam.Value <= 0 && errorMessage == "")
                                    result = 1;
                            }
                            if (result > 0)
                            {
                                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_SaleEntry]", con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@Action", "AddOrEdit");
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                    cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                    cmd.Parameters.AddWithValue("@V_TYPE", _dbHelper.Xnull(model.V_TYPE));
                                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(model.DOC_ID));
                                    cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@TypeSale2", dataTable);
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
                                    if ((int)returnParam.Value <= 0 && errorMessage=="")
                                        result = 1;
                                    else
                                        result = 0;

                                }
                            }

                            if (result > 0)
                            {
                                transaction.Commit();
                                return Json(new
                                {
                                    status = true,
                                    message = "Data saved successfully"
                                });                               
                            }
                            else
                            {
                                transaction.Rollback();
                                return Json(new
                                {
                                    status = false,
                                    message = "Data save failed"
                                });
                            }
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            return Json(new
                            {
                                status = false,
                                message = "Data save failed"
                            });
                        }
                    }
                }
            }
            catch (Exception)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Unexpected error occurred"
                });
            }
        }

        private DataTable FillDataTable(List<Sale2> data)
        {
            DataTable dt = new DataTable();
            // ===== Define Columns (Same order as SQL Type) =====
            dt.Columns.Add("ITEM_CODE", typeof(int));
            dt.Columns.Add("ITEM_NAME", typeof(string));
            dt.Columns.Add("SNO", typeof(int));
            dt.Columns.Add("UNIT_NAME", typeof(string));
            dt.Columns.Add("UNIT_CODE", typeof(int));
            dt.Columns.Add("HSN_CODE", typeof(string));
            dt.Columns.Add("NOS", typeof(int));
            dt.Columns.Add("QTY", typeof(decimal));
            dt.Columns.Add("GROSS_QTY", typeof(decimal));
            dt.Columns.Add("GATE_QTY", typeof(decimal));
            dt.Columns.Add("RATE", typeof(decimal));
            dt.Columns.Add("FOR_RATE", typeof(decimal));
            dt.Columns.Add("AMOUNT", typeof(decimal));
            dt.Columns.Add("PACK_PER", typeof(decimal));
            dt.Columns.Add("PACK_AMT", typeof(decimal));
            dt.Columns.Add("DISC_PER", typeof(decimal));
            dt.Columns.Add("DISC_AMT", typeof(decimal));
            dt.Columns.Add("TAX_CODE", typeof(int));
            dt.Columns.Add("CGST_PER", typeof(decimal));
            dt.Columns.Add("CGST_AMT", typeof(decimal));
            dt.Columns.Add("SGST_PER", typeof(decimal));
            dt.Columns.Add("SGST_AMT", typeof(decimal));
            dt.Columns.Add("IGST_PER", typeof(decimal));
            dt.Columns.Add("IGST_AMT", typeof(decimal));
            dt.Columns.Add("CESS_PER", typeof(decimal));
            dt.Columns.Add("CESS_AMT", typeof(decimal));
            dt.Columns.Add("LAND_RATE", typeof(decimal));
            dt.Columns.Add("LAND_AMT", typeof(decimal));
            dt.Columns.Add("REMARK", typeof(string));
            dt.Columns.Add("PACK_TYPE", typeof(string));
            dt.Columns.Add("PACK_NO", typeof(int));
            dt.Columns.Add("ORD_TYPE", typeof(string));
            dt.Columns.Add("ORD_NO", typeof(int));
            dt.Columns.Add("ORD_RATE", typeof(decimal));
            dt.Columns.Add("SAUDA_TYPE", typeof(string));
            dt.Columns.Add("SAUDA_NO", typeof(int));
            dt.Columns.Add("SAUDA_RATE", typeof(decimal));
            dt.Columns.Add("LOT_No", typeof(string));
            dt.Columns.Add("DEPT_CODE", typeof(int));
            dt.Columns.Add("DCN_TYPE", typeof(string));
            dt.Columns.Add("DCN_NO", typeof(int));
            dt.Columns.Add("FINAL_LOCK", typeof(string));
            dt.Columns.Add("STATUS", typeof(int));
            dt.Columns.Add("CDISC_AMT", typeof(decimal));
            dt.Columns.Add("INSU_AMT", typeof(decimal));
            dt.Columns.Add("FRT_AMT", typeof(decimal));
            dt.Columns.Add("WBQTY", typeof(decimal));
            dt.Columns.Add("FEXCH_USD", typeof(decimal));
            dt.Columns.Add("ROW_ID", typeof(string));
            dt.Columns.Add("GATE_INQTY", typeof(decimal));
            dt.Columns.Add("MIS_GROUP", typeof(string));
            dt.Columns.Add("FREIGHT_AMT", typeof(decimal));
            dt.Columns.Add("PROD_DESC", typeof(string));

            // ===== Fill Rows =====
            foreach (var d in data)
            {
                dt.Rows.Add(
                    d.ITEM_CODE,
                    d.ITEM_NAME,
                    d.SNO,
                    d.UNIT_NAME,
                    d.UNIT_CODE,
                    d.HSN_CODE,
                    d.NOS,
                    d.QTY,
                    d.GROSS_QTY,
                    d.GATE_QTY,
                    d.RATE,
                    d.FOR_RATE,
                    d.AMOUNT,
                    d.PACK_PER,
                    d.PACK_AMT,
                    d.DISC_PER,
                    d.DISC_AMT,
                    d.TAX_CODE,
                    d.CGST_PER,
                    d.CGST_AMT,
                    d.SGST_PER,
                    d.SGST_AMT,
                    d.IGST_PER,
                    d.IGST_AMT,
                    d.CESS_PER,
                    d.CESS_AMT,
                    d.LAND_RATE,
                    d.LAND_AMT,
                    d.REMARK,
                    d.PACK_TYPE,
                    d.PACK_NO,
                    d.ORD_TYPE,
                    d.ORD_NO,
                    d.ORD_RATE,
                    d.SAUDA_TYPE,
                    d.SAUDA_NO,
                    d.SAUDA_RATE,
                    d.LOT_No,
                    d.DEPT_CODE,
                    d.DCN_TYPE,
                    d.DCN_NO,
                    d.FINAL_LOCK,
                    d.STATUS,
                    d.CDISC_AMT,
                    d.INSU_AMT,
                    d.FRT_AMT,
                    d.WBQTY,
                    d.FEXCH_USD,
                    d.ROW_ID,
                    d.GATE_INQTY,
                    d.MIS_GROUP,
                    d.FREIGHT_AMT,
                    d.PROD_DESC
                );
            }

            return dt;
        }



    }
}
