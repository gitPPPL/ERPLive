using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X509.Qualified;
using Org.BouncyCastle.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Inventory.Master;
using travelexpensemanagement.Models.Purchase.Transaction;
using static travelexpensemanagement.Controllers.Master.StateMasterController;
using travelexpensemanagement.Common.DropdownService;
namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseOrderController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        public PurchaseOrderController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
        }
        public IActionResult Index()
        {
            var globalVariables = _globalValue.GetGlobalVariables();
            string databaseName;
            using (var connection = _dbcontext.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            return View("~/Views/Purchase/Transaction/PurchaseOrder/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='PurchaseOrder' ");
                return Json(new { status = true, data = Doctype });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
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
                var tableName = "ORDER1";

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
                var UserLoginData = _globalValue.GetGlobalVariables();
                var placelist = await _dbHelper.GetJsonDataAsync($@"select CODE, NAME from PLACE_MAST where COMP_CODE={UserLoginData.PubCompCode} order by NAME ");
                return Json(new { status = true, data = placelist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrencyMast()
        {
            try
            {
                var currencylist = await _dbHelper.GetJsonDataAsync($@"select CODE, NAME from CURRENCY_MAST  order by NAME ");
                return Json(new { status = true, data = currencylist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            try
            {
                var UserLoginData = _globalValue.GetGlobalVariables();
                var PartyList = await _dbHelper.GetJsonDataAsync($@"select distinct sg.CODE, sg.NAME, sg.ADD1,sg.ADD2,sg.ADD3,sg.PINCODE, isnull(cm.NAME, '') as CityName ,
                sg.CITY_CODE,sg.GSTIN from SUBGROUP_MAST sg left join CITY_MAST cm on sg.CITY_CODE=cm.CODE  where sg.COMP_CODE={UserLoginData.PubCompCode}  and UPPER(NATURE)='SUPPLIER' and
                sg.ACTIVE=1  order by NAME ");
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
                var PartyAddList = await _dbHelper.GetJsonDataAsync($@"select distinct sg.code, sg.ADD1,sg.ADD2,sg.ADD3,sg.PINCODE, isnull(cm.NAME, '') as CityName ,sg.CITY_CODE,sg.GSTIN from SUBGROUP_ADDRESS sg left join CITY_MAST cm on sg.CITY_CODE=cm.CODE  where sg.COMP_CODE={UserLoginData.PubCompCode}  and sg.code={partyCd} order by ADD1  ");
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
        public async Task<IActionResult> GetUnitList()
        {
            try
            {
                var unitlist = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from ITEMUNIT_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by name");
                return Json(new { status = true, data = unitlist });
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
        public async Task<IActionResult> GetPayTermList()
        {
            try
            {
                var PayTermList = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from PAYTERM_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME");
                return Json(new { status = true, data = PayTermList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMachineMastList()
        {
            try
            {
                var MachineList = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME from MACHINE_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");
                return Json(new { status = true, data = MachineList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeighBridge(int partyCd)
        {
            try
            {
                if (partyCd != null && partyCd <= 0)
                {
                    return Json(new { status = true, data = "" });
                }
                var wbList = await _dbHelper.GetJsonDataAsync($@"SELECT distinct DOC_ID, V_NO FROM wb1 WHERE PARTY_CODE={partyCd} AND V_TYPE='KANT' and COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} and BRANCH_CODE=1 order by V_NO ");
                return Json(new { status = true, data = wbList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeightBridgeDetail(string docid, int partyCd)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                //string strqry = $@" SELECT distinct wb1.DOC_ID, wb1.V_NO,wb2.ITEM_CODE,im.UNIT_CODE,wb2.NET_WGT
                //FROM wb1 left join wb2 on wb1.DOC_ID=WB2.DOC_ID and wb1.COMP_CODE=wb2.COMP_CODE
                //left join ITEM_MAST im on wb2.ITEM_CODE=im.CODE and WB2.COMP_CODE=im.COMP_CODE
                //WHERE wb2.DOC_ID='{docid}' and isnull(wb2.ITEM_CODE, 0)<>0
                //and WB1.COMP_CODE={userSession.PubCompCode} and wb1.BRANCH_CODE=1  ";


                string strqry = $@" select a.ITEM_CODE,e.Name ITEM_NAME, d.NAME 'Make',d.CODE 'MakeCode',e.UNIT_CODE,e.UNIT_NAME,sum(a.NET_WGT)Qty
                   from WB2 a inner join wb1 b on a.V_TYPE = b.V_TYPE and a.v_no = b.v_no and a.COMP_CODE = b.COMP_CODE
                   and a.BRANCH_CODE = b.BRANCH_CODE and a.YEAR_CODE = b.YEAR_CODE
                   left join ITEM_MAKE c on a.ITEM_CODE = c.ITEM_CODE and a.COMP_CODE = c.COMP_CODE
                   left join ITEMMAKE_MAST d on c.MAKE_CODE = d.CODE and a.COMP_CODE = d.COMP_CODE
                   left join ITEM_MAST e on a.Item_CODE = e.CODE and a.COMP_CODE = e.COMP_CODE
                   where a.item_name<>'' and a.DOC_ID = '{docid}'
                   and a.COMP_CODE = {userSession.PubCompCode}   and e.active = 1 and a.BRANCH_CODE = 1
                   and b.PARTY_CODE = {partyCd}
                   group by a.ITEM_CODE,e.Name,d.NAME,d.CODE,e.UNIT_CODE,e.UNIT_NAME ";
                var wbDetail = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = wbDetail });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        //[HttpPost]
        //public IActionResult CalculationBySaudaNo([FromBody] SaudaCalculationRequest model)
        //{
        //    try
        //    {        
        //        string btn = model.Btn;
        //        int saudaNo = model.SaudaNo ?? 0;
        //        string saudaType = model.SaudaType ?? "";
        //        int stateCode = model.StateCode ?? 0;
        //        int cityCode = model.CityCode ?? 0;
        //        DateTime effectiveDate = model.EffectiveDate ?? DateTime.Now;  

        //        List<Order2> itemList = model.Orders ?? new List<Order2>();
        //        CalculateSaudaRate(btn, saudaType, saudaNo, stateCode, cityCode, effectiveDate, itemList);

        //        return Json(new { status = true, data = itemList });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        public JsonResult CalculationBySaudaNo([FromBody] SaudaCalculationRequest model)
        {
            try
            {
                string btn = model.Btn;
                int saudaNo = model.SaudaNo ?? 0;
                string saudaType = model.SaudaType ?? "";
                int stateCode = model.StateCode ?? 0;
                int cityCode = model.CityCode ?? 0;
                DateTime effectiveDate = model.EffectiveDate ?? DateTime.Now;

                List<Order2> itemList = model.Orders ?? new List<Order2>();                 
                CalculateSaudaRate(btn, saudaType, saudaNo, stateCode, cityCode, effectiveDate, itemList);

                return Json(new { status = true, data = itemList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetSaudaList(int partyCd)
        {
            try
            {
                if (partyCd != null && partyCd <= 0)
                {
                    return Json(new { status = true, data = "" });
                }
                var SaudaNO = await _dbHelper.GetJsonDataAsync($@" SELECT distinct DOC_ID, V_NO, V_TYPE FROM SAUDA WHERE PARTY_CODE={partyCd}  and COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} and BRANCH_CODE = 1 and STATUS = 1 order by V_NO ");
                return Json(new { status = true, data = SaudaNO });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSaudaDetail(string docid)
        {
            try
            {
                var saudaDetail = await _dbHelper.GetJsonDataAsync($@"select distinct RATE,TAX_CODE,DISC_TYPE,DISC_PER,PACK_TYPE  from sauda where DOC_ID='{docid}'  and COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} and BRANCH_CODE=1 AND  V_TYPE='PAUD' ORDER BY RATE ");
                return Json(new { status = true, data = saudaDetail });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderRecordsById(string id)
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
                { "@Action", "PurchaseOrderHeader" }
                };

                var parametersDetail = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", VType},
                { "@V_NO", int.Parse(VNo) },
                { "@Action", "PurchaseOrderDetail" }
                };

                var parametersAttachment = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", VType},
                { "@V_NO", int.Parse(VNo) },
                { "@Action", "PurchaseOrderAttachment" }
                };

                var header = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersHeader);
                var detail = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersDetail);
                var attachment = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersAttachment);

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

        [HttpGet]
        public async Task<IActionResult> GetPurchaseRtApprovalList()
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                string strqry = $@" SELECT pr2.COMP_CODE, PR2.V_TYPE, PR2.V_NO,  format(PR2.V_DATE, 'dd-MMM-yyyy') V_DATE, PR2.ITEM_CODE,isnull(im.NAME, '') as ItemName, PR2.MAKE_CODE,isnull(imm.NAME, '') as makename, isnull(PR2.DEPT_CODE, '') DEPT_CODE,
                isnull(D.NAME, '') AS Department, isnull(PR2.TECH_DESC, '') TECH_DESC, isnull(PR2.UOM_CODE, '') UOM_CODE,isnull(ium.NAME, '') as Unit, PR2.REMARKS, PR2.PLACE_USECODE, PR2.PLACE_USE,
                PR2.WORK_TYPECODE, PR2.WORK_TYPE, PR2.APROV_CODE, PR2.APROV_STATUS, PR2.APROV_REMARKS, PR2.STATUS, PR2.DOC_ID,PR2.REQ_QTY, PR2.RATE, PR2.AMOUNT, PR2.TAX_CODE, PR2.PACK_PER, PR2.DISC_PER, PR2.CGST_PER, 
                PR2.SGST_PER, PR2.IGST_PER, PR2.PACK_AMT, PR2.DISC_AMT, PR2.CGST_AMT, PR2.SGST_AMT, PR2.IGST_MAT, PR2.ACTIVE
                FROM PREQUEST2 PR2
                LEFT JOIN item_mast IM ON IM.CODE=PR2.ITEM_CODE AND IM.COMP_CODE=PR2.COMP_CODE LEFT JOIN ITEMUNIT_MAST IUM ON IUM.CODE=PR2.UOM_CODE AND IUM.COMP_CODE=PR2.COMP_CODE
                LEFT JOIN ITEMMAKE_MAST IMM ON IMM.CODE=PR2.MAKE_CODE AND IMM.COMP_CODE=PR2.COMP_CODE LEFT JOIN ITEMDEPT_MAST D ON D.CODE=PR2.DEPT_CODE AND D.COMP_CODE=PR2.COMP_CODE 
                WHERE PR2.COMP_CODE = {userSession.PubCompCode} AND PR2.YEAR_CODE ={userSession.PubFYearCode} AND PR2.BRANCH_CODE = 1 
                ORDER BY PR2.DOC_ID DESC ";

                var PRApprovalList = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = PRApprovalList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQuotationRtAprvlLiast(int partyCode)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                string strqry = $@"   SELECT Q2.YEAR_CODE, Q2.COMP_CODE, Q2.BRANCH_CODE, Q2.V_NO, Q2.V_TYPE, isnull(Q2.V_DATE, '') V_DATE, isnull(Q2.PARTY_CODE, '') PARTY_CODE,isnull(sg.NAME, '') as party, Q2.ITEM_CODE,
                   Q2.MAKE_CODE, Q2.TECH_DESC, Q2.UOM_CODE, 
                   IM.NAME AS itemName,isnull(ium.NAME, '') as Unit, isnull(imm.NAME, '') as make,
                   isnull(Q2.REF_NO, 0) REF_NO, Q2.REF_DATE, isnull(Q2.REF_TYPE, '') REF_TYPE, Q2.REF_DOCID, Q2.QTY, Q2.RATE,
                   Q2.AMOUNT, Q2.PACK_PER, Q2.PACK_AMT, Q2.DISC_PER, Q2.DISC_AMT, Q2.FREIGHT,isnull(tm.NAME, '') taxType,Q2.TAX_CODE, Q2.CGST_PER,
                   Q2.CGST_AMT, Q2.SGST_PER, Q2.SGST_AMT, Q2.IGST_PER, Q2.IGST_AMT, Q2.VAT_PER, Q2.VAT_AMT, Q2.CESS_PER,
                   Q2.CESS_AMT, Q2.OTH_EXPS, Q2.LD_RATE, Q2.NET_AMT, Q2.BULK_QTY, Q2.BULK_RATE, Q2.BULK_DISC_PER,
                   Q2.PREORITY_LEVEL, Q2.REQ_TYPE, Q2.REQ_NO, Q2.STATUS, 
                   Q2.APROV_STATUS, Q2.APROV_REMARKS, Q2.FAPROV_STATUS, Q2.FAPROV_REMARKS,Q2.DOC_ID
                   FROM QUOTATION2 Q2
                   left join SUBGROUP_MAST sg on sg.code=q2.PARTY_CODE and sg.COMP_CODE=q2.COMP_CODE
                   left join TAX_MAST tm on tm.code=q2.TAX_CODE 
                   LEFT JOIN item_mast IM ON IM.CODE=Q2.ITEM_CODE AND IM.COMP_CODE=Q2.COMP_CODE
                   LEFT JOIN ITEMUNIT_MAST IUM ON IUM.CODE=Q2.UOM_CODE AND IUM.COMP_CODE=Q2.COMP_CODE
                   LEFT JOIN ITEMMAKE_MAST IMM ON IMM.CODE=Q2.MAKE_CODE AND IMM.COMP_CODE=Q2.COMP_CODE 
                   WHERE Q2.COMP_CODE = {usersession.PubCompCode} AND Q2.YEAR_CODE = {usersession.PubFYearCode} AND Q2.BRANCH_CODE = 1 and Q2.V_TYPE='STAP' and Q2.PARTY_CODE={partyCode}
                    ORDER BY Q2.DOC_ID DESC ";

                var QuotationRtList = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = QuotationRtList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdatePurchaseOrder([FromBody] PurchaseOrder POmodel)
        {
            try
            {

                if (POmodel == null)
                return Json(new { status = false, message = " data save failed." });


                //var result = await saveValidateData(POmodel) as JsonResult;

                //if (result is JsonResult json)
                //{
                //    dynamic data = json.Value;

                //    if (data.status == false)
                //    {
                //        return Json(new  { status = false, message = data.message  });
                //    }
                //}


                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    DataTable purchaseOrderTable = FillDataTable(POmodel.ItemRecords, "[dbo].[Type_Order2]");


                    // 🔥 REPLACE FillDataTable with this

                    DataTable purchaseOrderAttachmentTable = new DataTable();

                    purchaseOrderAttachmentTable.Columns.Add("FILE_Path", typeof(string));
                    purchaseOrderAttachmentTable.Columns.Add("FILE_NAME", typeof(string));
                    purchaseOrderAttachmentTable.Columns.Add("SRNO", typeof(int));

                    int srno = 1;


                    if (POmodel.Attachments != null)
                    {
                        foreach (var a in POmodel.Attachments)
                        {
                            purchaseOrderAttachmentTable.Rows.Add(
                                "/attachments/Purchase/" + (a.FileName ?? ""),
                                a.FileName,
                                srno++
                            );
                        }
                    }

                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;

                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PurchaseOrder_AE]", con, transaction))
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
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                cmd.Parameters.AddWithValue("@V_NO", _dbHelper.Xnull(POmodel.VNo));
                                cmd.Parameters.AddWithValue("@V_TYPE", _dbHelper.Xnull(POmodel.VType));
                                cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(POmodel.VDate));
                                cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(POmodel.DocId));
                                cmd.Parameters.AddWithValue("@PLACE_CODE", _dbHelper.Xnull(POmodel.PlaceCode));
                                cmd.Parameters.AddWithValue("@WB_TYPE", _dbHelper.Xnull(POmodel.WbType));
                                cmd.Parameters.AddWithValue("@WB_NO", _dbHelper.Xnull(POmodel.WbNo));
                                cmd.Parameters.AddWithValue("@PARTY_CODE", _dbHelper.Xnull(POmodel.PartyCode));
                                cmd.Parameters.AddWithValue("@SHIP_CODE", _dbHelper.Xnull(POmodel.ShipCode));
                                cmd.Parameters.AddWithValue("@SHIP_FROM", _dbHelper.Xnull(POmodel.ShipFrom));
                                cmd.Parameters.AddWithValue("@PRICE_TYPE", _dbHelper.Xnull(POmodel.PriceType));
                                cmd.Parameters.AddWithValue("@PARTY_REF", _dbHelper.Xnull(POmodel.PartyRef));
                                cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", _dbHelper.Xnull(POmodel.ImportCurrency));
                                cmd.Parameters.AddWithValue("@EXRATE", _dbHelper.Xnull(POmodel.ExRate));
                                cmd.Parameters.AddWithValue("@NOS", _dbHelper.Xnull(POmodel.Nos));
                                cmd.Parameters.AddWithValue("@QTY", _dbHelper.Xnull(POmodel.Qty));
                                cmd.Parameters.AddWithValue("@AMOUNT", _dbHelper.Xnull(POmodel.Amount));
                                cmd.Parameters.AddWithValue("@PACK_AMT", _dbHelper.Xnull(POmodel.PackAmt));
                                cmd.Parameters.AddWithValue("@DISC_AMT", _dbHelper.Xnull(POmodel.DiscAmt));
                                cmd.Parameters.AddWithValue("@CGST_AMT", _dbHelper.Xnull(POmodel.CgstAmt));
                                cmd.Parameters.AddWithValue("@SGST_AMT", _dbHelper.Xnull(POmodel.SgstAmt));
                                cmd.Parameters.AddWithValue("@IGST_AMT", _dbHelper.Xnull(POmodel.IgstAmt));
                                cmd.Parameters.AddWithValue("@OTH_AMT", _dbHelper.Xnull(POmodel.OthAmt));
                                cmd.Parameters.AddWithValue("@VAT_AMT", _dbHelper.Xnull(POmodel.VatAmt));
                                cmd.Parameters.AddWithValue("@CESS_PER", _dbHelper.Xnull(POmodel.CessPer));
                                cmd.Parameters.AddWithValue("@CESS_AMT", _dbHelper.Xnull(POmodel.CessAmt));
                                cmd.Parameters.AddWithValue("@TCS_PER", _dbHelper.Xnull(POmodel.TcsPer));
                                cmd.Parameters.AddWithValue("@TCS_AMT", _dbHelper.Xnull(POmodel.TcsAmt));
                                cmd.Parameters.AddWithValue("@NET_AMT", _dbHelper.Xnull(POmodel.NetAmt));
                                cmd.Parameters.AddWithValue("@DELIVERY_TERM", _dbHelper.Xnull(POmodel.DeliveryTerm));
                                cmd.Parameters.AddWithValue("@DELIVERY_DATE", _dbHelper.Xnull(POmodel.DeliveryDate));
                                cmd.Parameters.AddWithValue("@VALIDITY_DATE", _dbHelper.Xnull(POmodel.ValidityDate));
                                cmd.Parameters.AddWithValue("@TRANSPORT_TERM", _dbHelper.Xnull(POmodel.TransportTerm));
                                cmd.Parameters.AddWithValue("@PAYTERM_CODE", _dbHelper.Xnull(POmodel.PaytermCode));
                                cmd.Parameters.AddWithValue("@PAYMENT_TERM", _dbHelper.Xnull(POmodel.PaymentTerm));
                                cmd.Parameters.AddWithValue("@PRICE_TERM", _dbHelper.Xnull(POmodel.PriceTerm));
                                cmd.Parameters.AddWithValue("@SAUDA_TYPE", _dbHelper.Xnull(POmodel.SaudaType));
                                cmd.Parameters.AddWithValue("@SAUDA_NO", _dbHelper.Xnull(POmodel.SaudaNo));
                                cmd.Parameters.AddWithValue("@DELIVERY_PERIOD", _dbHelper.Xnull(POmodel.DeliveryPeriod));
                                cmd.Parameters.AddWithValue("@DELIVERY_TO", _dbHelper.Xnull(POmodel.DeliveryTo));
                                cmd.Parameters.AddWithValue("@REMARKS", _dbHelper.Xnull(POmodel.Remarks));
                                cmd.Parameters.AddWithValue("@POTYPE", _dbHelper.Xnull(POmodel.PoType));
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", _dbHelper.Xnull(POmodel.FAProvStatus));
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", _dbHelper.Xnull(POmodel.FAProvRemarks));
                                cmd.Parameters.AddWithValue("@MAILSEND", _dbHelper.Xnull(POmodel.MailSend));
                                cmd.Parameters.AddWithValue("@CDISC_AMT", _dbHelper.Xnull(POmodel.CDiscAmt));
                                cmd.Parameters.AddWithValue("@AUTOGEN_PO", _dbHelper.Xnull(POmodel.AutoGenPo));
                                cmd.Parameters.AddWithValue("@POACCEPT_FLG", _dbHelper.Xnull(POmodel.PoAcceptFlg));
                                cmd.Parameters.AddWithValue("@POATTACH_PATH", _dbHelper.Xnull(POmodel.PoAttachPath));
                                cmd.Parameters.AddWithValue("@POATTCH_DATE", _dbHelper.Xnull(POmodel.PoAttachDate));
                                cmd.Parameters.AddWithValue("@BILL_ADD1", _dbHelper.Xnull(POmodel.BillAdd1));
                                cmd.Parameters.AddWithValue("@BILL_ADD2", _dbHelper.Xnull(POmodel.BillAdd2));
                                cmd.Parameters.AddWithValue("@BILL_ADD3", _dbHelper.Xnull(POmodel.BillAdd3));
                                cmd.Parameters.AddWithValue("@BILL_CITY", _dbHelper.Xnull(POmodel.BillCity));
                                cmd.Parameters.AddWithValue("@BILL_PINCODE", _dbHelper.Xnull(POmodel.BillPincode));
                                cmd.Parameters.AddWithValue("@BILL_GST", _dbHelper.Xnull(POmodel.BillGst));
                                cmd.Parameters.AddWithValue("@SHIP_ADD1", _dbHelper.Xnull(POmodel.ShipAdd1));
                                cmd.Parameters.AddWithValue("@SHIP_ADD2", _dbHelper.Xnull(POmodel.ShipAdd2));
                                cmd.Parameters.AddWithValue("@SHIP_ADD3", _dbHelper.Xnull(POmodel.ShipAdd3));
                                cmd.Parameters.AddWithValue("@SHIP_CITY", _dbHelper.Xnull(POmodel.ShipCity));
                                cmd.Parameters.AddWithValue("@SHIP_PINCODE", _dbHelper.Xnull(POmodel.ShipPincode));
                                cmd.Parameters.AddWithValue("@SHIP_GST", _dbHelper.Xnull(POmodel.ShipGst));
                                cmd.Parameters.AddWithValue("@TAX_CODE", _dbHelper.Xnull(POmodel.TaxCode));
                                cmd.Parameters.AddWithValue("@ITEM_TYPE", _dbHelper.Xnull(POmodel.ItemType));
                                cmd.Parameters.AddWithValue("@SUPPLY_TYPE", _dbHelper.Xnull(POmodel.SupplyType));
                                cmd.Parameters.AddWithValue("@TRAN_TYPE", _dbHelper.Xnull(POmodel.TranType));
                                cmd.Parameters.AddWithValue("@FORM_CODE", _dbHelper.Xnull(POmodel.FormCode));
                                cmd.Parameters.AddWithValue("@VEHICLE_NO", _dbHelper.Xnull(POmodel.VehicleNo));
                                cmd.Parameters.AddWithValue("@INV_TYPE", _dbHelper.Xnull(POmodel.InvType));
                                cmd.Parameters.AddWithValue("@INV_NO", _dbHelper.Xnull(POmodel.InvNo));
                                cmd.Parameters.AddWithValue("@PARTY_NAME", _dbHelper.Xnull(POmodel.PartyName));
                                cmd.Parameters.AddWithValue("@SHIP_NAME", _dbHelper.Xnull(POmodel.ShipName));
                                cmd.Parameters.AddWithValue("@status", 1);
                                cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);


                                var tvp = cmd.Parameters.AddWithValue("@ItemRecord", purchaseOrderTable);
                                tvp.SqlDbType = SqlDbType.Structured;
                                tvp.TypeName = "[dbo].[Type_Order2]";

                                var tvp2 = cmd.Parameters.AddWithValue("@attachment", purchaseOrderAttachmentTable);
                                tvp2.SqlDbType = SqlDbType.Structured;
                                tvp2.TypeName = "[dbo].[Type_order3]";

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

                            return Json(new  {   status = success, message = success ? "Data save/update successfully." : "Failed to save or update some employee details." });
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

                case "[dbo].[Type_order3]":
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

                case "[dbo].[Type_Order2]":
                    foreach (var detail in data.Cast<Order2>())
                    {
                        PurchaseOrderTbl.Rows.Add(
                            detail.SNO,
                            detail.PlaceCode,
                            detail.ItemName,
                            detail.ItemCode,
                            detail.MakeCode,
                            detail.NOS,
                            detail.Qty,
                            detail.AdjQty,
                            detail.GateQty,
                            detail.UomName,
                            detail.UomCode,
                            detail.Rate,
                            detail.ImportRate,
                            detail.CalcRate,
                            detail.Amount,
                            detail.PackPer,
                            detail.PackAmt,
                            detail.DiscPer,
                            detail.DiscAmt,
                            detail.TaxCode,
                            detail.CgstPer,
                            detail.CgstAmt,
                            detail.SgstPer,
                            detail.SgstAmt,
                            detail.IgstPer,
                            detail.IgstAmt,
                            detail.VatPer,
                            detail.VatAmt,
                            detail.CessPer,
                            detail.CessAmt,
                            detail.OthAmt,
                            detail.NetAmt,
                            detail.LandRate,
                            detail.Status,
                            detail.PlaceUse,
                            detail.DeptName,
                            detail.Remarks,
                            detail.PreorityLevel,
                            detail.PreorityRemarks,
                            detail.RateMonthly,
                            detail.RateQuarterly,
                            detail.RateAnnualy,
                            detail.RateSpecial,
                            detail.RequestType,
                            detail.RequestNo,
                            detail.ApprovalType,
                            detail.ApprovalNo,
                            detail.DeptCode,
                            detail.DeliveryDate,
                            detail.SaudaType,
                            detail.SaudaNo,
                            detail.DispThrough,
                            detail.DispRef,
                            detail.DispRemarks,
                            detail.TenacityGrpCode,
                            detail.TenacityType,
                            detail.TenacityCode,
                            detail.TenacityName,
                            detail.FAProvStatus,
                            detail.FAProvRemarks,
                            detail.ColorCode,
                            detail.GramCode
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

                case "[dbo].[Type_Order2]":
                    dt.Columns.Add("SNO", typeof(int));
                    dt.Columns.Add("PLACE_CODE", typeof(int));
                    dt.Columns.Add("ITEM_NAME", typeof(string));
                    dt.Columns.Add("ITEM_CODE", typeof(int));
                    dt.Columns.Add("MAKE_CODE", typeof(int));
                    dt.Columns.Add("NOS", typeof(int));
                    dt.Columns.Add("QTY", typeof(decimal));
                    dt.Columns.Add("ADJ_QTY", typeof(decimal));
                    dt.Columns.Add("GATE_QTY", typeof(decimal));
                    dt.Columns.Add("UOM_NAME", typeof(string));
                    dt.Columns.Add("UOM_CODE", typeof(int));
                    dt.Columns.Add("RATE", typeof(decimal));
                    dt.Columns.Add("IMPORT_RATE", typeof(decimal));
                    dt.Columns.Add("CALC_RATE", typeof(decimal));
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
                    dt.Columns.Add("VAT_PER", typeof(decimal));
                    dt.Columns.Add("VAT_AMT", typeof(decimal));
                    dt.Columns.Add("CESS_PER", typeof(decimal));
                    dt.Columns.Add("CESS_AMT", typeof(decimal));
                    dt.Columns.Add("OTH_AMT", typeof(decimal));
                    dt.Columns.Add("NET_AMT", typeof(decimal));
                    dt.Columns.Add("LAND_RATE", typeof(decimal));
                    dt.Columns.Add("STATUS", typeof(int));
                    dt.Columns.Add("PLACE_USE", typeof(string));
                    dt.Columns.Add("DEPT_NAME", typeof(string));
                    dt.Columns.Add("REMARKS", typeof(string));
                    dt.Columns.Add("PREORITY_LEVEL", typeof(int));
                    dt.Columns.Add("PREORITY_REMARKS", typeof(string));
                    dt.Columns.Add("RATE_MONTHLY", typeof(decimal));
                    dt.Columns.Add("RATE_QUARTERLY", typeof(decimal));
                    dt.Columns.Add("RATE_ANNUALY", typeof(decimal));
                    dt.Columns.Add("RATE_SPECIAL", typeof(decimal));
                    dt.Columns.Add("REQUEST_TYPE", typeof(string));
                    dt.Columns.Add("REQUEST_NO", typeof(int));
                    dt.Columns.Add("APPROVAL_TYPE", typeof(string));
                    dt.Columns.Add("APPROVAL_NO", typeof(int));
                    dt.Columns.Add("DEPT_CODE", typeof(int));
                    dt.Columns.Add("DELIVERY_DATE", typeof(DateTime));
                    dt.Columns.Add("SAUDA_TYPE", typeof(string));
                    dt.Columns.Add("SAUDA_NO", typeof(int));
                    dt.Columns.Add("DISP_THROUGH", typeof(string));
                    dt.Columns.Add("DISP_REF", typeof(string));
                    dt.Columns.Add("DISP_REMARKS", typeof(string));
                    dt.Columns.Add("TENACITY_GRPCODE", typeof(int));
                    dt.Columns.Add("TENACITY_TYPE", typeof(string));
                    dt.Columns.Add("TENACITY_CODE", typeof(int));
                    dt.Columns.Add("TENACITY_NAME", typeof(string));
                    dt.Columns.Add("FAPROV_STATUS", typeof(string));
                    dt.Columns.Add("FAPROV_REMARKS", typeof(string));
                    dt.Columns.Add("COLOR_CODE", typeof(int));
                    dt.Columns.Add("GRAM_CODE", typeof(int));
                    break;

                case "[dbo].[Type_order3]":
                    dt.Columns.Add("FILE_Path", typeof(string));
                    dt.Columns.Add("FILE_NAME", typeof(string));
                    dt.Columns.Add("SRNO", typeof(int));
                    break;

                default:
                    throw new ArgumentException("Unknown table type: " + typeName);
            }
            return dt;
        }
         
        private void CalculateSaudaRate(string btn,string SaudaType, int saudaNo,int StateCode,int CityCode,DateTime effectiveDate, List<Order2> itemList)
        {
            double saudaRate = 0.0;
            int saudaICode = 0;
            string sdate = "";

            var userdata = _globalValue.GetGlobalVariables();

            // Step 1: Fetch Sauda Rate
            if (saudaNo > 0)
            {
                string query = $@"
            SELECT TOP 1 ITEM_CODE, RATE, V_DATE FROM SAUDA
            WHERE V_TYPE = '{SaudaType}' 
              AND V_NO = {saudaNo}
              AND COMP_CODE = {userdata.PubCompCode}
              AND BRANCH_CODE = 1";

                var dt = LoadDataInDataTable(query);
                if (dt.Rows.Count > 0)
                {
                    saudaICode = Convert.ToInt32(dt.Rows[0]["ITEM_CODE"]);
                    saudaRate = Convert.ToDouble(dt.Rows[0]["RATE"]);
                    sdate = Convert.ToDateTime(dt.Rows[0]["V_DATE"]).ToString("yyyy-MM-dd");
                }
            }

            if (saudaRate == 0)
            {
                throw new Exception("Sauda Rate is blank.");
            }

            // Step 2: Loop through items
            foreach (var item in itemList)
            {
                if (item.ItemCode == null || item.ItemCode <= 0)
                    continue;

                double rate = 0.0;

                // Step 3: Check discount master
                if (isExist($"SELECT 1 FROM RMDISC_MAST WHERE SAUDA_ITEM = {saudaICode} AND COMP_CODE = {userdata.PubCompCode}"))
                {
                    string discountQuery = $@"
                SELECT TOP 1 ISNULL(RATE, 0) AS Rate 
                FROM RMDISC_MAST
                WHERE ITEM_CODE = {item.ItemCode}
                  AND COMP_CODE = {userdata.PubCompCode}
                  AND SAUDA_ITEM = {saudaICode}
                  AND EFF_DATE <= '{effectiveDate}'
                ORDER BY EFF_DATE DESC";

                    var RMDiscRate = LoadDataInDataTable(discountQuery);
                    if (RMDiscRate.Rows.Count == 0)
                    {
                        throw new Exception($"Item ({item.ItemName}) not found in discount master.");
                    }

                    rate = Convert.ToDouble(RMDiscRate.Rows[0]["Rate"]);

                    // Special condition
                    if (rate != 0 && string.Compare(sdate, "20250503") <= 0 && item.ItemCode == 30003)
                    {
                        rate += 7;
                    }

                    // Assign calculated rate
                    if (btn == "BTN")
                    {
                        item.CalcRate = (decimal)formatVal(saudaRate + rate, "AMT");
                    }
                    else if (btn == "SAV" && item.CalcRate > (decimal)(saudaRate + rate))
                    {
                        item.CalcRate = (decimal)formatVal(saudaRate + rate, "AMT");
                    }
                }
                else
                {
                    item.CalcRate = (decimal)formatVal(saudaRate + rate, "AMT");
                }

                // Final values
                item.Rate = item.CalcRate;
                item.SaudaNo = saudaNo;

                // Optional: Store metadata if needed
                // item.SomeTag = txtSaudaNo.Tag?.ToString();

                // Step 4: Tax Calculation
                if (btn == "BTN")
                {
                    var stateCode = getText($"SELECT STATE_CODE FROM CITY_MAST WHERE CODE = {Convert.ToInt32(CityCode)}");
                    int stateCodeInt = string.IsNullOrWhiteSpace(stateCode) ? 0 : Convert.ToInt32(stateCode);
                    double igstPer = Convert.ToDouble(getText($@"
                SELECT ISNULL(IGST_PER, 0) 
                FROM ITEM_MAST 
                WHERE CODE = {item.ItemCode} AND COMP_CODE = {userdata.PubCompCode}"));

                    double cgstPer = Convert.ToDouble(getText($@"
                SELECT ISNULL(CGST_PER, 0) 
                FROM ITEM_MAST 
                WHERE CODE = {item.ItemCode} AND COMP_CODE = {userdata.PubCompCode}"));

                    if (Convert.ToInt32(StateCode) == (stateCodeInt))
                    {
                        // Intra-state: CGST + SGST
                        if (cgstPer > 0)
                        {
                            item.TaxCode = Convert.ToInt32(getText($"SELECT CODE FROM TAX_MAST WHERE CGST_PER = {cgstPer}"));
                            item.CgstPer = (decimal)cgstPer;
                            item.SgstPer = (decimal)cgstPer;
                            item.IgstPer = 0;
                        }
                    }
                    else
                    {
                        // Inter-state: IGST
                        if (igstPer > 0)
                        {
                            item.TaxCode = Convert.ToInt32(getText($"SELECT CODE FROM TAX_MAST WHERE IGST_PER = {igstPer}"));
                            item.CgstPer = 0;
                            item.SgstPer = 0;
                            item.IgstPer = (decimal)igstPer;
                        }
                    }
                }

                 
            }
        }

        private double formatVal(double value, string formatType)
        {
            switch (formatType.ToUpper())
            {
                case "AMT":
                    return Math.Round(value, 2);
                case "QTY":
                    return Math.Round(value, 3);
                default:
                    return value;
            }
        }

        private DataTable LoadDataInDataTable(string query)
        {
            DataTable dt = new DataTable();
            using (var con = _dbcontext.GetErpConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, con))
                {
                    adapter.Fill(dt);
                }
            }
            return dt;
        }

        private string getText(string query)
        {
            using (var con = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    var result = cmd.ExecuteScalar();
                    return result != null ? result.ToString() : string.Empty;
                }
            }
        }

        private bool isExist(string query)
        {
            using (var con = _dbcontext.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    var result = cmd.ExecuteScalar();                  
                    return result != null && result != DBNull.Value;
                }
            }
        }

        private List<ValidationResult> ValidateQuotationItems(DataTable dt)
        {
            var results = new List<ValidationResult>();
            var partyCode = 1;
            var compCode = 1;
            var branchCode = 1;
            var today = DateTime.Today;
            int rateExpiredDays = 180;
            string vTypeSelected = "";
            //var msgHandler = new MessageHandler();
            var ErrorMessage=string.Empty;
            for (int j = 0; j < dt.Rows.Count; j++)
            {
                var row = dt.Rows[j];
                var itemCode = Convert.ToInt32(row["Item Code"]);
                var itemName = row["Item Name"].ToString();
                var vType = row["VType"].ToString();
                var reqType = row["ReqType"].ToString();
                var reqNo = Convert.ToInt32(row["ReqNo"]);

                string sqlDate = $"SELECT TOP 1 V_DATE FROM QUOTATION2 WHERE V_TYPE='STAP' AND ITEM_CODE={itemCode} AND FAPROV_STATUS='Approved' AND PARTY_CODE={partyCode} AND COMP_CODE={compCode} AND BRANCH_CODE={branchCode} ORDER BY V_DATE DESC";
                var lastAppRateDateStr = getText(sqlDate);

                if (vTypeSelected != "RORD" && !string.IsNullOrEmpty(lastAppRateDateStr))
                {
                    if (DateTime.Parse(lastAppRateDateStr).AddDays(rateExpiredDays) < today)
                    {
                        //msgHandler.AddError();
                        ErrorMessage = $"Approved Rate Expired for Party '{partyCode}', Item '{itemName}'.";
                        continue;
                    }
                }

                if (vType == "STPI")
                {
                    string prQuery = $"SELECT 1 FROM PREQUEST2 WHERE ITEM_CODE={itemCode} AND APROV_STATUS='Approved' AND V_TYPE='{reqType}' AND V_NO={reqNo} AND COMP_CODE={compCode} AND BRANCH_CODE={branchCode}";
                    if (!isExist(prQuery))
                    {
                        //msgHandler.AddError();
                        ErrorMessage = $"Status is Hold/Reject for '{itemName}'. Item cannot be added.";
                        continue;
                    }
                }

                string qcQuery = $"SELECT 1 FROM QUOTATION2 WHERE V_TYPE='STAP' AND PARTY_CODE={partyCode} AND ITEM_CODE={itemCode}";
                if (!isExist(qcQuery))
                {
                    //msgHandler.AddError( );
                    ErrorMessage = $"Item: ({itemCode}) {itemName} of Party: /*PartyName*/ — Rate Not Approved.";
                    continue;
                }
                //results.Add({ ItemCode = itemCode, errorMsg = ErrorMessage });
               
            }
 
            return results;
        }

        private void ValidatePurchaseRequest()
        {
            string copyVType = "";
            //FrmCopyFrom.arrlist = new List<int>();
            string Vtype = "STPI";
            var userData = _globalValue.GetGlobalVariables();
            var companyCode = userData.PubCompCode;
            var PubDate = DateTime.Now.ToString("dd-MMM-yyyy");
            var branchCode = 1;
            var PartyCode = 1;
            var dtpVDate = DateTime.Now.ToString("dd-MMM-yyyy");
            if (Vtype == "STPI")
            {
                copyVType = "STPI";

            string grdquery = $@"
            SELECT a.V_NO AS 'VNo', a.V_TYPE AS 'VType', FORMAT(a.V_DATE, '{PubDate}') AS 'VDate',
                   b.ITEM_CODE AS 'Item Code', c.name AS 'Item Name', d.Name AS 'Unit', e.name AS 'Make',
                   b.TECH_DESC AS 'Tech Desc', ISNULL(b.Req_Qty, 0) - ISNULL(b.Adj_Qty, 0) AS Req_Qty,
                   FORMAT(a.VALID_DATE, '{PubDate}') AS 'Valid Date', a.Remarks, a.Status,
                   f.name AS 'Department', a.DEPT_CODE AS 'Dept Code', b.MAKE_CODE AS 'Make Code',
                   a.V_NO AS 'ReqNo', a.V_TYPE AS 'ReqType', b.UOM_CODE AS 'UCode'
            FROM PREQUEST1 a
            INNER JOIN PREQUEST2 b ON a.V_NO = b.V_NO AND a.V_TYPE = b.V_TYPE AND a.V_DATE = b.V_DATE 
                AND a.COMP_CODE = b.COMP_CODE AND a.BRANCH_CODE = b.BRANCH_CODE AND a.YEAR_CODE = b.YEAR_CODE
            LEFT JOIN ITEM_MAST c ON b.ITEM_CODE = c.CODE AND c.COMP_CODE = {companyCode}
            LEFT JOIN ITEMUNIT_MAST d ON b.UOM_CODE = d.CODE AND d.COMP_CODE = {companyCode}
            LEFT JOIN ITEMMAKE_MAST e ON b.MAKE_CODE = e.CODE AND e.COMP_CODE = {companyCode}
            LEFT JOIN ITEMDEPT_MAST f ON a.DEPT_CODE = f.CODE AND f.COMP_CODE = {companyCode}
            WHERE a.V_TYPE = 'STPI' AND a.V_DATE <= {dtpVDate} AND a.Status = 1
            AND a.COMP_CODE = {companyCode} AND a.BRANCH_CODE = {branchCode}
            ORDER BY a.V_TYPE, a.V_DATE, a.V_NO, b.SRNO";
            //FrmCopyFrom.arrlist.Add(8);  

            }
            else if (Vtype == "STAP")
            {
                if (PartyCode==0)
                {
                    //MessageBox.Show("Please select Party.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    //txtPartyName.Focus();
                    return;
                }

                copyVType = "STAP";

                string  grdquery = $@"
            SELECT a.V_NO AS 'VNo', a.V_TYPE AS 'VType', FORMAT(a.V_DATE, '{PubDate}') AS 'VDate',
                   a.ITEM_CODE AS 'Item Code', d.name AS 'Item Name', e.Name AS 'Unit', f.name AS 'Make',
                   a.TECH_DESC AS 'Tech Desc', a.Qty, a.Rate, a.Amount, a.PACK_PER AS 'Pack%', a.PACK_AMT AS 'Pack Amt',
                   a.Freight, a.DISC_PER AS 'Disc%', a.DISC_AMT AS 'Disc Amt', g.Name AS 'Tax Type',
                   a.CGST_PER AS 'CGST%', a.CGST_AMT AS 'CGST Amt', a.SGST_PER AS 'SGST%', a.SGST_AMT AS 'SGST Amt',
                   a.IGST_PER AS 'IGST%', a.IGST_AMT AS 'IGST Amt', a.Cess_Per AS 'CessP', a.Cess_Amt AS 'Cess',
                   a.OTH_EXPS AS 'Oth Amt', a.LD_RATE AS 'LD Rate', a.NET_AMT AS 'Net Amt', c.name AS 'Party Name',
                   a.REF_NO AS 'Quote No', a.REF_DATE AS 'Quote Date', a.TECH_DESC AS 'Remarks', a.Status,
                   a.REQ_TYPE AS 'ReqType', a.REQ_NO AS 'ReqNo', a.TAX_CODE AS 'Tax Code', a.MAKE_CODE AS 'Make Code',
                   a.UOM_CODE AS 'UCode'
            FROM QUOTATION2 a
            LEFT JOIN Quotation1 b ON a.V_TYPE = b.V_TYPE AND a.V_NO = b.V_NO AND a.COMP_CODE = b.COMP_CODE 
                AND a.BRANCH_CODE = b.BRANCH_CODE AND a.YEAR_CODE = b.YEAR_CODE
            LEFT JOIN SUBGROUP_MAST c ON a.PARTY_CODE = c.CODE AND c.COMP_CODE = {companyCode}
            LEFT JOIN ITEM_MAST d ON a.ITEM_CODE = d.CODE AND d.COMP_CODE = {companyCode}
            LEFT JOIN ITEMUNIT_MAST e ON a.UOM_CODE = e.CODE AND e.COMP_CODE = {companyCode}
            LEFT JOIN ITEMMAKE_MAST f ON a.MAKE_CODE = f.CODE AND f.COMP_CODE = {companyCode}
            LEFT JOIN TAX_MAST g ON a.TAX_CODE = g.CODE
            WHERE a.Status = 1 AND a.FAPROV_STATUS = 'Approved' AND a.PARTY_CODE = {PartyCode}
                AND c.active = 1 AND a.V_TYPE = 'STAP' AND a.COMP_CODE = {companyCode}
                AND a.BRANCH_CODE = {branchCode}
            ORDER BY a.V_TYPE, a.V_DATE, a.V_NO";

                for (int i = 8; i <= 25; i++)
                {
                    //FrmCopyFrom.arrlist.Add(i);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("ORDER1", vdate, vtype, vno);
            return Ok(result);
        }


        public async Task<IActionResult> saveValidateData([FromBody] PurchaseOrder POmodel)
        {
            if (POmodel == null)
                return Json(new { status = false, message = " data save failed." });
            try
            {

                var userData = _globalValue.GetCompanydata();

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    DataTable purchaseOrderTable = FillDataTable(POmodel.ItemRecords, "[dbo].[Type_Order2]");



                    DataTable purchaseOrderAttachmentTable = new DataTable();

                    purchaseOrderAttachmentTable.Columns.Add("FILE_Path", typeof(string));
                    purchaseOrderAttachmentTable.Columns.Add("FILE_NAME", typeof(string));
                    purchaseOrderAttachmentTable.Columns.Add("SRNO", typeof(int));

                    int srno = 1;


                    if (POmodel.Attachments != null)
                    {
                        foreach (var a in POmodel.Attachments)
                        {
                            purchaseOrderAttachmentTable.Rows.Add(
                                "/attachments/Purchase/" + (a.FileName ?? ""),
                                a.FileName,
                                srno++
                            );
                        }
                    }


                    string StateType = "";

                    if (POmodel.PartyCode > 0)
                    {
                        string state_type = getText(
                            "SELECT STATE_TYPE FROM STATE_MAST WHERE CODE = " +
                            "(SELECT STATE_CODE FROM SUBGROUP_ADDRESS " +
                            "WHERE CODE = " + POmodel.PartyCode +
                            " AND ISNULL(IS_DEFAULT,0)=1 " +
                            "AND COMP_CODE = " + usersessionDt.PubCompCode + ")");

                        if (!string.IsNullOrEmpty(POmodel.ImportCurrency) && state_type != "Import")
                        {
                            return Json(new  { status = false, message = "Party belongs to India. Foreign currency/Ex-Rate not applicable. Please remove." });
                        }

                        string StateCode = getText("SELECT State_Code FROM CITY_MAST WHERE Code = " + POmodel.BillCity);

                        if (userData.STATE_CODE == StateCode)
                        {
                            StateType = "Local";
                        }
                        else
                        {
                            StateType = "Central/Other";
                        }

                        // Local Party - IGST should not be applicable
                        if (userData.STATE_CODE == StateCode && POmodel.IgstAmt > 0)
                        {
                            return Json(new { status = false, message = $"IGST not applicable as per Party State type is {StateType}."  });
                        }
                        // Interstate Party - CGST/SGST should not be applicable
                        else if (userData.STATE_CODE != StateCode &&
                                 (POmodel.CgstAmt + POmodel.SgstAmt) > 0)
                        {
                            return Json(new { status = false, message = $"CGST/SGST not applicable as per Party State type is {StateType}." });
                        }

                        // Both tax types cannot exist together
                        if (POmodel.IgstAmt > 0 && (POmodel.CgstAmt + POmodel.SgstAmt) > 0)
                        {
                            return Json(new { status = false, message = "CGST+SGST+IGST all three type tax not applicable." });
                        }
                    }



                    if (POmodel.VType == "RORD" && POmodel.VNo > 0)
                    {
                        var GateNo = getText(
                            "SELECT CONCAT(GATE_TYPE,GATE_NO) FROM WB1 " +
                            "WHERE V_TYPE='" + POmodel.WbType + "' " +
                            "AND V_No=" + POmodel.WbNo + " " +
                            "AND Comp_code=" + usersessionDt.PubCompCode
                        );

                        var SaudaNo = getText(
                            "SELECT CONCAT(REF_TYPE,REF_NO) FROM GATE2 " +
                            "WHERE Ref_type='PAUD' " +
                            "AND CONCAT(V_TYPE,V_no)='" + GateNo + "' " +
                            "AND Comp_code=" + usersessionDt.PubCompCode
                        );

                        string currentSauda = POmodel.SaudaType + POmodel.SaudaNo;

                        if (!string.IsNullOrEmpty(SaudaNo) && SaudaNo != currentSauda)
                        {
                            return Json(new
                            {
                                status = false,
                                message = $"Sauda No mismatch. Gate has {SaudaNo}, but current is {currentSauda}. Please check Gate Pass."
                            });
                        }
                    }

                    if(POmodel.PriceType != "" && POmodel.SaudaNo > 0 )
                    {
                        var saudaPricetype = getText("Select isnull(FRT_TERM,'') From Sauda Where Concat(V_type,V_no)='" + Convert.ToString(POmodel.SaudaNo) + Convert.ToString(POmodel.SaudaType) + "' and Comp_code=" + usersessionDt.PubCompCode + "");


                        if(saudaPricetype != "")
                        {

                            if(saudaPricetype != POmodel.PriceType)
                            {

                                return Json(new
                                {
                                    status = false,
                                    message = $"Price type is mismatch from sauda. {saudaPricetype}, is in Sauda, while here is {POmodel.PriceType}"
                                });

                            }

                        }

                    }

                    return Json(new { status = true});
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }



        public JsonResult DDLCityMast()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = "select CODE , NAME from CITY_MAST  where active = 1 ";
                var DDLCityMast = _dropdownService.GetDropdownList(query);
                return Json(DDLCityMast);
            }
        }



    }
}
