using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Layout.Element;
using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Models.Purchase.Transaction;
using static System.Runtime.InteropServices.JavaScript.JSType;


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

        public JsonResult GetDocType()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='PurchaseOrder'  ";
                var GetDocType = _dropdownService.GetDropdownList(query);
                return Json(GetDocType);
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

        public JsonResult DDLGridItem(string v_type)
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = "";
                if (v_type == "JORD")
                {
                    query = @"SELECT  a.CODE, a.NAME  FROM ITEM_MAST a
                    LEFT JOIN ITEMUNIT_MAST b ON a.UNIT_CODE = b.CODE AND b.COMP_CODE = a.COMP_CODE
                    LEFT JOIN ITEM_GROUP c ON a.GROUP_CODE = c.CODE  AND c.COMP_CODE = a.COMP_CODE
                    LEFT JOIN ITEM_MGROUP d ON c.MGROUP_CODE = d.CODE AND d.COMP_CODE = a.COMP_CODE
                    WHERE a.COMP_CODE = " + getdata.PubCompCode + @"  AND d.MGROUP_TYPE = 'Service' AND a.ACTIVE = 1
                    GROUP BY a.NAME, a.CODE, b.NAME,  b.CODE, a.HSN_CODE,  a.CATLOG
                    ORDER BY a.NAME;";
                }

                else if(v_type == "RORD")
                {
                        query = @"	SELECT  a.CODE, a.NAME 
                        FROM ITEM_MAST a
                        LEFT JOIN ITEMUNIT_MAST b ON a.UNIT_CODE = b.CODE AND b.COMP_CODE = a.COMP_CODE
                        LEFT JOIN ITEM_GROUP c ON a.GROUP_CODE = c.CODE AND c.COMP_CODE = a.COMP_CODE
                        LEFT JOIN ITEM_MGROUP d ON c.MGROUP_CODE = d.CODE AND d.COMP_CODE = a.COMP_CODE
                        WHERE a.COMP_CODE =  " + getdata.PubCompCode + @"  AND d.MGROUP_TYPE = 'Raw' AND a.ACTIVE = 1
                        GROUP BY a.NAME, a.CODE, b.NAME, b.CODE, a.HSN_CODE, a.CATLOG
                        ORDER BY a.NAME;";
                }
                else
                {
                        query = @"SELECT   a.CODE, a.NAME  FROM ITEM_MAST a
                        LEFT JOIN ITEM_MAKE b ON a.CODE = b.ITEM_CODE  AND b.COMP_CODE = a.COMP_CODE
                        LEFT JOIN ITEMUNIT_MAST c ON a.UNIT_CODE = c.CODE  AND c.COMP_CODE = a.COMP_CODE
                        LEFT JOIN ITEM_GROUP d ON a.GROUP_CODE = d.CODE  AND d.COMP_CODE = a.COMP_CODE
                        LEFT JOIN ITEM_MGROUP e  ON d.MGROUP_CODE = e.CODE AND e.COMP_CODE = a.COMP_CODE
                        WHERE a.COMP_CODE = " + getdata.PubCompCode + @" AND a.ACTIVE = 1
                        GROUP BY a.NAME, a.CODE, c.NAME, c.CODE, a.HSN_CODE, a.CATLOG
                        ORDER BY a.NAME;";
                }                                 

                var DDLGridItemList = _dropdownService.GetDropdownList(query);

                return Json(DDLGridItemList);
            }

        }
        public JsonResult DDLGridMake(int ItemCode = 0)
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {

                string query = "";
                if(ItemCode == 0)
                {
                    query = @"SELECT a.MAKE_CODE  , b.NAME   FROM ITEM_MAKE a
                    LEFT JOIN ITEMMAKE_MAST b ON a.MAKE_CODE = b.CODE AND b.COMP_CODE = " + getdata.PubCompCode + @"  WHERE a.COMP_CODE = " + getdata.PubCompCode;
                }
                else
                {
                    query = @"SELECT a.MAKE_CODE  , b.NAME   FROM ITEM_MAKE a
                    LEFT JOIN ITEMMAKE_MAST b ON a.MAKE_CODE = b.CODE AND b.COMP_CODE = " + getdata.PubCompCode + @"  WHERE  a.ITEM_CODE = "+  ItemCode +"   and  a.COMP_CODE = " + getdata.PubCompCode ;

                }

                var DDLGridMake = _dropdownService.GetDropdownList(query);

                return Json(DDLGridMake);
            }

        }
        public JsonResult DDLUnitList()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"select CODE,NAME from ITEMUNIT_MAST where COMP_CODE="  + getdata.PubCompCode + "  order by name";

                var DDLGridUnit = _dropdownService.GetDropdownList(query);

                return Json(DDLGridUnit);
            }

        }
        public JsonResult DDLPlaceList()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"select code,name from MACHINE_MAST where ACTIVE = 1 and COMP_CODE=" + getdata.PubCompCode + " and TYPE='Store' order by name  ";

                var DDLPlaceList = _dropdownService.GetDropdownList(query);

                return Json(DDLPlaceList);
            }

        }
        public JsonResult DDLDepartmentList()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"select code ,name from ITEMDEPT_MAST where  COMP_CODE=" + getdata.PubCompCode + "  ";
                var DDLDepartmentList = _dropdownService.GetDropdownList(query);
                return Json(DDLDepartmentList);
            }

        }
        public JsonResult DDLTaxTypeList()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"select code , name from TAX_MAST where ACTIVE = 1  ";
                var DDLTaxTypeList = _dropdownService.GetDropdownList(query);
                return Json(DDLTaxTypeList);
            }
        }

        public JsonResult DllStatus()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"Select Code,Name from DOCSTATUS_MAST Order by CODE ";
                var DllStatus = _dropdownService.GetDropdownList(query);
                return Json(DllStatus);
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

        public JsonResult GetWeighBridge(int partyCd)
        {

            if (partyCd != null && partyCd <= 0)
            {
                return Json(new { status = true, data = "" });
            }

            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string sql = @" ;WITH tmpwb AS ( SELECT  V_TYPE , V_NO   FROM WB1 WHERE V_TYPE = 'KANT' AND WB_TYPE = 'Raw Material'
                                AND PARTY_CODE = 21497 AND COMP_CODE = " + getdata.PubCompCode + @" AND BRANCH_CODE = " + getdata.PubBranchCode + @"
                              
                                 UNION ALL

                                SELECT V_TYPE , V_NO  FROM WB1 WHERE V_TYPE = 'KSIN' AND PARTY_CODE = 21497  AND COMP_CODE = " + getdata.PubCompCode + @"
                                AND BRANCH_CODE = " + getdata.PubBranchCode + @"  )
                                SELECT V_TYPE, V_NO FROM tmpwb WHERE V_NO IS NOT NULL  ORDER BY V_NO;";

                var GetWeighBridge = _dropdownService.GetDropdownList(sql);
                return Json(GetWeighBridge);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeightBridgeDetail(string docid, int partyCd)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();

                string strqry = $@" select a.ITEM_CODE,e.Name ITEM_NAME, d.NAME 'Make',d.CODE 'MakeCode',e.UNIT_CODE,e.UNIT_NAME,sum(a.NET_WGT)Qty
                   from WB2 a inner join wb1 b on a.V_TYPE = b.V_TYPE and a.v_no = b.v_no and a.COMP_CODE = b.COMP_CODE
                   and a.BRANCH_CODE = b.BRANCH_CODE and a.YEAR_CODE = b.YEAR_CODE
                   left join ITEM_MAKE c on a.ITEM_CODE = c.ITEM_CODE and a.COMP_CODE = c.COMP_CODE
                   left join ITEMMAKE_MAST d on c.MAKE_CODE = d.CODE and a.COMP_CODE = d.COMP_CODE
                   left join ITEM_MAST e on a.Item_CODE = e.CODE and a.COMP_CODE = e.COMP_CODE
                   where a.item_name<>'' and a.DOC_ID = '{docid}'
                   and a.COMP_CODE = {userSession.PubCompCode}   and e.active = 1 and a.BRANCH_CODE = {userSession.PubBranchCode}  
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
                CalculateSaudaRate(btn, saudaType, saudaNo,  cityCode, effectiveDate, itemList);

                return Json(new { status = true, data = itemList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
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
                    { "@BRANCH_CODE", userSession.PubBranchCode },
                    { "@V_TYPE", VType},
                    { "@V_NO", int.Parse(VNo) },
                    { "@Action", "PurchaseOrderHeader" }
                };

                var parametersDetail = new Dictionary<string, object>
                {
                    { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                    { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                    { "@BRANCH_CODE", userSession.PubBranchCode },
                    { "@V_TYPE", VType},
                    { "@V_NO", int.Parse(VNo) },
                    { "@Action", "PurchaseOrderDetail" }
                };

                var parametersAttachment = new Dictionary<string, object>
                {
                    { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                    { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                    { "@BRANCH_CODE", userSession.PubBranchCode },
                    { "@V_TYPE", VType},
                    { "@V_NO", int.Parse(VNo) },
                    { "@Action", "PurchaseOrderAttachment" }
                };

                var header = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersHeader);
                var detail = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersDetail);
                var attachment = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersAttachment);

                return Json(new { status = true,  header = header, detail = detail, attachment = attachment });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
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
        public string GetText(string query)
        {
            try
            {
                using var con = _dbcontext.GetErpConnection();
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader[0].ToString();
                            }
                            else
                            {
                                return string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetText() Error: " + ex.Message);
                return string.Empty;
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveValidation([FromBody] PurchaseOrder model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            var usersessionDt = _globalValue.GetGlobalVariables();
            var golbalGenSetting = await _globalValue.LoadGeneralSetting();
            string PartyCountry = "";

            string query = "select state_type from STATE_MAST where CODE=(select state_code from SUBGROUP_ADDRESS " +
            "where CODE=" + model.PartyCode + " and isnull(IS_DEFAULT,0)=1 and COMP_CODE= " + usersessionDt.PubCompCode + ")";

            string state_type = getText(query);

            if (model.ImportCurrency != null && state_type == "Import")
            {
                return Json(new { success = false, message = "Party belongs to India. Foreign currency/Ex-Rate not applicable. Please remove." });
            }

            string StateCode = getText("select State_Code from CITY_MAST where code=" + model.BillCity + "");

            string StateType = "";

            if (usersessionDt.STATE_CODE == StateCode)
            {
                StateType = "Local";
            }
            else
            {
                StateType = "Central/Other";
            }

            if ((usersessionDt.STATE_CODE == StateCode) && model.IgstAmt > 0)
            {
                return Json(new { success = false, message = "IGST Not applicable as per Party State type is " + StateType + "" });

            }

            else if (usersessionDt.STATE_CODE != StateCode && (model.CgstAmt + model.SgstAmt) > 0)
            {
                return Json(new { success = false, message = "CGST/SGST not applicable as per Party State type is " + StateType + "" });
            }

            if (model.IgstAmt > 0 && (model.CgstAmt + model.SgstAmt) > 0)
            {
                return Json(new { success = false, message = "CGST+SGST+IGST all three type tax not applicable." });
            }

            if (model.VType == "RORD" && model.SaudaNo > 0)
            {
                string GateNo = getText("Select CONCAT(GATE_TYPE,GATE_NO) from WB1 Where V_TYPE='" + model.VType + "' and V_No=" + model.WbNo + " and Comp_code=" + usersessionDt.PubCompCode + "");
                string SaudaNo = getText("Select CONCAT(REF_TYPE,REF_NO) from GATE2 Where Ref_type='PAUD' and CONCAT(V_TYPE,V_no)='" + GateNo + "' and Comp_code=" + usersessionDt.PubCompCode + "");

                if (!string.IsNullOrEmpty(SaudaNo) && SaudaNo != (model.SaudaType + model.SaudaNo))
                {
                    return Json(new { success = false, message = "Please check Sauda No=>" + SaudaNo + " picked at Gate No=> " + GateNo + ", Please correct it." });
                }
            }

            if (model.PriceType != "" && model.SaudaNo > 0)
            {
                string saudaPricetype = getText(
                    "SELECT ISNULL(FRT_TERM,'') " +
                    "FROM SAUDA " +
                    "WHERE CONCAT(V_TYPE, V_NO) = '" + model.SaudaType + model.SaudaNo + "' " +
                    "AND COMP_CODE = " + usersessionDt.PubCompCode
                );

                if (saudaPricetype != "")
                {
                    if (saudaPricetype != model.PriceType)
                    {
                        return Json(new { success = false, message = "Price type is mismatch from sauda. " + saudaPricetype + " is in Sauda, while here is " + model.PriceType + "" });

                    }
                }
            }

            decimal totOrderQty = 0;


            foreach (var Details in model.ItemRecords)
            {
                if (string.IsNullOrWhiteSpace(Details.ItemName))
                    continue;

                if (model.VType == "RORD")
                {
                    string SAUDA_REQ = getText("SELECT ISNULL(SAUDA_REQ, '') AS SAUDA_REQ FROM ITEM_GROUP WHERE CODE = ( SELECT GROUP_CODE FROM ITEM_MAST  WHERE CODE = '" + Details.ItemCode + "' AND COMP_CODE = " + usersessionDt.PubCompCode + ") AND COMP_CODE = " + usersessionDt.PubCompCode + ";");

                    if (SAUDA_REQ == "false")
                    {
                        if (model.SaudaType != "")
                        {
                            return Json(new { success = false, message = "Sauda Number required of item=> " + Details.ItemName + "" });
                        }
                        else
                        {
                            string saudaICode = getText("select top 1 ITEM_CODE from SAUDA where V_TYPE='" + Details.SaudaType + "' and v_no=" + Details.SaudaNo + " and COMP_CODE=" + usersessionDt.PubCompCode + " and BRANCH_CODE=" + usersessionDt.PubCompCode + "");

                            string sql = "SELECT TOP 1  ISNULL(RATE, 0) AS Rate FROM RMDISC_MAST WHERE ITEM_CODE = " + Details.ItemCode + "  AND COMP_CODE = " + usersessionDt.PubCompCode + "  AND SAUDA_ITEM = " + saudaICode + "   AND EFF_DATE <= '" + model.VDate + "'  ORDER BY EFF_DATE DESC;";

                            string Rate = getText(sql);

                            if (Rate == "")
                            {
                                return Json(new { success = false, message = "Item (" + Details.ItemName + ") not found in discount master, Please contact System Administrator." });
                            }
                        }
                    }
                }

                if (Details.SaudaType != "" && Details.SaudaNo > 0)
                {
                    string sql = "Select FAPROV_STATUS from SAUDA Where V_type='PAUD' and V_no= " + Details.SaudaNo + " and Comp_Code= " + usersessionDt.PubCompCode + "  and branch_code=" + usersessionDt.PubBranchCode + " ";
                    string FAPROV_STATUS = getText(sql);
                    return Json(new { success = false, message = "Please check, Sauda No.=>" + Details.SaudaNo + " not approved yet. so, Order can not create." });
                }

                if (Details.RequestNo > 0)
                {
                    string sql = "select 1 from PREQUEST2 where ITEM_CODE = " + Details.ItemCode + " and V_TYPE = 'STPI' and V_NO = " + Details.RequestNo + " and COMP_CODE = " + usersessionDt.PubCompCode + "  and BRANCH_CODE = " + usersessionDt.PubBranchCode + " and Year_Code = " + usersessionDt.PubFYearCode + ";";
                    string ss = getText(sql);
                    return Json(new { success = false, message = "Item=> " + Details.ItemName + " not found in Request No=>" + Details.RequestNo + " " });
                }

                decimal trate = 0;
                decimal qotRate = 0;
                if (Details.ApprovalNo > 0)
                {
                    string rateText = getText(
                        "SELECT ISNULL(Rate,0) FROM QUOTATION2 " +
                        " WHERE COMP_CODE=" + usersessionDt.PubCompCode +
                        " AND ITEM_CODE=" + Details.ItemCode +
                        " AND PARTY_CODE=" + model.PartyCode +
                        " AND V_TYPE='STAP'" +
                        " AND V_NO=" + Details.ApprovalNo +
                        " AND BRANCH_CODE=" + usersessionDt.PubBranchCode);

                    decimal.TryParse(rateText, out qotRate);

                    if (model.ExRate > 0)
                    {
                        trate = (decimal)(model.ExRate * qotRate);
                    }

                    string prQuery = "SELECT 1 FROM QUOTATION2  WHERE ITEM_CODE = " + Details.ItemCode + "  AND V_TYPE = 'STAP'  AND V_NO = " + Details.SaudaNo + "   AND PARTY_CODE = " + model.PartyCode + "   AND COMP_CODE = " + usersessionDt.PubCompCode + "    AND BRANCH_CODE = " + usersessionDt.PubBranchCode + ";";

                    if (!isExist(prQuery))
                    {
                        if (trate != Details.Rate)
                        {
                            return Json(new { success = false, message = "Rate not matched of Item=>" + Details.ItemName + " From Quotation Approval No=>" + Details.SaudaNo + " " });
                        }
                        else
                        {
                            return Json(new { success = false, message = "Item=> " + Details.ItemName + " not found in Quotation Approval No=>" + Details.SaudaNo + " " });
                        }
                    }

                }

                else if (model.VType == "PORD")
                {
                    string prQuery = "select 1 from MARKET_RATE1 a Left join MARKET_RATE2 b on a.V_type=b.V_type and a.v_no=b.V_no and a.Comp_code=b.Comp_code where b.ITEM_CODE=" + Details.ItemCode + " and a.V_TYPE='MRAT' and " + Details.Rate + " between b.Min_Rate and b.Max_Rate and '" + model.VDate + "' between a.EFF_DATE and a.EXP_DATE and a.COMP_CODE=" + usersessionDt.PubCompCode + " and a.BRANCH_CODE=" + usersessionDt.PubBranchCode + "; ";

                    if (!isExist(prQuery))
                    {
                        return Json(new { success = false, message = "Item=> " + Details.ItemName + " not found in Market Rate master OR Item not found in Rate Approval. " });

                    }

                }

                if (model.VType != "RORD" && model.VType != "DORD" && Details.RequestNo > 0)
                {
                    if (golbalGenSetting.pubDefReqInPO == "false")
                    {

                        string FAPROV_STATUS = getText("Select FAPROV_STATUS from PREQUEST1 where V_TYPE = 'STPI' and V_NO = " + Details.RequestNo + " and COMP_CODE = " + usersessionDt.PubCompCode + " and" +
                        " BRANCH_CODE = " + usersessionDt.PubBranchCode + " and Year_Code = " + usersessionDt.PubFYearCode + "");

                        if (FAPROV_STATUS != "Approved")
                        {
                            return Json(new { success = false, message = "Request no " + Details.RequestNo + " not approved of item=>" + Details.ItemName + "" });
                        }

                    }
                }

                if (model.Status == 1)
                {
                    if (model.VType != "RORD" && model.VType != "JORD" && model.VType != "DORD")
                    {
                        if (golbalGenSetting.pubDefRateAppInPO == "Yes")
                        {
                            string lastAppRateDate = getText(
                                "SELECT TOP 1 V_DATE " +
                                "FROM QUOTATION2 " +
                                "WHERE V_TYPE='STAP' " +
                                "AND ITEM_CODE=" + Details.ItemCode +
                                " AND FAPROV_STATUS='Approved'" +
                                " AND PARTY_CODE=" + model.PartyCode +
                                " AND COMP_CODE=" + usersessionDt.PubCompCode +
                                " AND BRANCH_CODE=" + usersessionDt.PubBranchCode +
                                " ORDER BY V_DATE DESC");

                            PartyCountry = getText(
                               "SELECT STATE_TYPE " +
                               "FROM STATE_MAST " +
                               "WHERE CODE=(" +
                               "SELECT STATE_CODE " +
                               "FROM SUBGROUP_ADDRESS " +
                               "WHERE CODE=" + model.PartyCode +
                               " AND COMP_CODE=" + usersessionDt.PubCompCode +
                               " AND ISNULL(IS_DEFAULT,0)=1)");

                            if (PartyCountry != "Import" && string.IsNullOrWhiteSpace(lastAppRateDate))
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = $"Rate Not approved of Item => '{Details.ItemName}', Party => '{model.PartyName}'."
                                });
                            }
                            else if (PartyCountry != "Import" && !string.IsNullOrWhiteSpace(lastAppRateDate))
                            {
                                if (DateTime.TryParse(lastAppRateDate, out DateTime approvedDate))
                                {
                                    // Equivalent to:
                                    // CDate(lastAppRateDate).Date.AddDays(pubRateExpiredDays) < dtpVDate.Value.Date



                                    int ExpiryDays = Convert.ToInt16(golbalGenSetting.pubRateExpiredDays);

                                    if (approvedDate.Date.AddDays(ExpiryDays) < model.VDate.Value.Date)
                                    {
                                        return Json(new { success = false, message = $"Approved Rate Expired of Party => '{model.PartyName}', Item => '{Details.ItemName}'." });
                                    }
                                }
                            }
                        }
                    }
                }

                if (golbalGenSetting.pubDefRateAppInPO == "Yes")
                {
                    if (Details.PreorityLevel > 2 && Details.PreorityRemarks == "")
                    {
                        return Json(new { success = false, message = "Reason required for item " + Details.ItemName + " in case Approval Level greater than 2" });
                    }
                }

                string HSN_CODE = getText("Select isnull(HSN_CODE,'') from ITEM_MAST where CODE=" + Details.ItemCode + " and COMP_CODE=" + usersessionDt.PubCompCode + " and Active=1");

                if (HSN_CODE == "")
                {
                    return Json(new { success = false, message = "Please check  and  Update HSN Code in item master of item=>" + Details.ItemName + "" });
                }

                if (model.VType != "RORD" && model.VType != "DORD" && model.VDate.HasValue && model.VDate.Value >= new DateTime(2019, 5, 1))
                {
                    if (model.Status == 1)
                    {
                        if (golbalGenSetting.pubDefReqInPO == "yes")
                        {
                            return Json(new { success = false, message = "Request Not created of item " + Details.ItemName + "" });
                        }

                    }
                }

                if (golbalGenSetting.pubDefSaudaInPO == "yes")
                {

                    if (model.SaudaNo > 0)
                    {
                        if (model.VType == "RORD")
                        {
                            Decimal OrderqTY = Convert.ToDecimal(getText("select sum(a.qty) from ORDER2 a inner join ORDER1 b on a.V_NO=b.v_no and a.V_TYPE=b.V_TYPE and a.COMP_CODE=b.COMP_CODE " +
                                " and a.BRANCH_CODE=b.BRANCH_CODE and a.YEAR_CODE=b.YEAR_CODE where  a.V_TYPE='" + model.VType + "' and " +
                                " b.SAUDA_TYPE='PAUD' and b.SAUDA_NO=" + model.SaudaNo + " and a.COMP_CODE=" + usersessionDt.PubCompCode + " and a.BRANCH_CODE= " + usersessionDt.PubBranchCode + " and " +
                                " a.YEAR_CODE=" + usersessionDt.PubFYearCode + " and a.V_NO<>" + model.VNo + ""));

                            decimal SaudaQty = Convert.ToDecimal("select sum(qty) from SAUDA where V_TYPE='PAUD' and V_NO=" + Details.SaudaNo + "and COMP_CODE= " + usersessionDt.PubBranchCode + " and BRANCH_CODE=" + usersessionDt.PubBranchCode + "");



                            decimal pendingSaudaQty = SaudaQty - OrderqTY;


                            if (totOrderQty > pendingSaudaQty + Convert.ToDecimal(golbalGenSetting.pubBPPurchTolQty))
                            {
                                return Json(new { success = false, message = "Please check OrderQty(" + totOrderQty + ")+Tolreance Qty(" + Convert.ToDecimal(golbalGenSetting.pubBPPurchTolQty) + ") > Pending Sauda Qty (" + pendingSaudaQty + ")" });
                            }

                        }
                    }


                }

                if (model.Status == 1)
                {
                    if (Details.ItemName != "" && Details.TaxCode != 0)
                    {

                        return Json(new { success = false, message = "Please check Tax Type not selected." });
                    }
                }

              

            }


            return Json(new { success = true, message = "" });

        }

        [HttpPost]
        public async Task<IActionResult> SavedData([FromBody] PurchaseOrder model)
        {
            if (model == null)
            {
                return Json(new { Status = false, Message = "Input model is null." });
            }

            return await SubmitRequest(model);
        }

         private async Task<JsonResult> SubmitRequest([FromBody] PurchaseOrder POmodel )
         {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var golbalGenSetting = await _globalValue.LoadGeneralSetting();

                System.Boolean isApprovalBody = false;
                System.Boolean isFinalApprovalBody = false;
                string DOC_APPROSTAGE = "";
                string APPROV_USER = "";
                string fappstatus = "";
                string fappRemark = "";
                string gstExmptflg = "0";
                string orders = "";
                decimal PubRes1Dbl = 0;
                using var conn = _dbcontext.GetErpConnection();                 
        
                conn.Open();

                string ss = getText("select 1 from DOC_APPROSTAGE where USER_CODE=" + usersessionDt.PubUserId + "and  DOC_CODE='IORD' and comp_code= " + usersessionDt.PubCompCode + "");


                if(ss == "1")
                {
                    isApprovalBody = true;
                }

                string approval = getText("select APPROV_USER from DOC_APPROSTAGE where USER_CODE=" + usersessionDt.PubUserId + "    and DOC_CODE='" + POmodel.VType + "' and comp_code=" + usersessionDt.PubCompCode + "");


                if(approval == "FINAL")
                {
                    isFinalApprovalBody = true;
                  
                }

                if (isFinalApprovalBody == true)
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                }
                else
                {
                    if (POmodel.VType == "RORD")
                    {
                        int ffg = 0;

                        if (!isFinalApprovalBody && POmodel.VType == "RORD" && !string.IsNullOrWhiteSpace(POmodel.SaudaType) && POmodel.SaudaNo.HasValue && POmodel.BillCity.HasValue && POmodel.VDate.HasValue &&
                        POmodel.ItemRecords != null && POmodel.ItemRecords.Count > 0)
                        {
                            CalculateSaudaRate("SAV", POmodel.SaudaType, POmodel.SaudaNo.Value, POmodel.BillCity.Value, POmodel.VDate.Value, POmodel.ItemRecords);
                        }

                        foreach (var Details in POmodel.ItemRecords)
                        {
                            if (string.IsNullOrWhiteSpace(Details.ItemName))
                                continue;


                            if (POmodel.VType == "RORD")
                            {
                                if (Details.ItemCode != 0)
                                {
                                    if (Details.SaudaNo > 0 && (Details.Rate != Details.CalcRate))
                                    {
                                        ffg = 1;
                                        break;
                                    }
                                }
                            }
                        }

                        if (ffg == 1)
                        {
                            fappstatus = "Approved";
                            fappRemark = "Document Approved.";
                        }

                    }
                }

                if (POmodel.SaudaNo > 0)
                {
                    string qry1 = @"SELECT V_NO, SUM(QTY) AS Qty  FROM ORDER2 WHERE SAUDA_TYPE = 'PAUD' AND SAUDA_NO = @SaudaNo  AND COMP_CODE = @CompCode
                      AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode  AND V_TYPE = 'RORD' AND V_NO <> @VNo GROUP BY V_NO ORDER BY V_NO";

                    DataTable dt1 = new DataTable();

                    using (SqlCommand cmd = new SqlCommand(qry1, conn))
                    {
                        cmd.Parameters.AddWithValue("@SaudaNo", POmodel.SaudaNo);
                        cmd.Parameters.AddWithValue("@CompCode", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", usersessionDt.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", usersessionDt.PubFYearCode);
                        cmd.Parameters.AddWithValue("@VNo", POmodel.VNo);

                        using (SqlDataAdapter adap1 = new SqlDataAdapter(cmd))
                        {
                            adap1.Fill(dt1);
                        }
                    }

                    if (dt1.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt1.Rows)
                        {
                            orders += $"{row["V_NO"]}({Convert.ToDecimal(row["Qty"])}), ";
                        }

                        orders = orders.Substring(0, orders.Length - 2);
                    }
                }


                foreach (var Details in POmodel.ItemRecords)
                {
                    if (string.IsNullOrWhiteSpace(Details.ItemName))
                        continue;

                    PubRes1Dbl += Details.Qty ?? 0;
                }

                if(POmodel.SaudaNo > 0)
                {

                    decimal saudaQty = Convert.ToDecimal(getText("select isnull(sum (QTY),0) from SAUDA where V_TYPE='" + POmodel.SaudaType + "' and V_NO=" + POmodel.SaudaNo + " and COMP_CODE=" + usersessionDt.PubCompCode + " and BRANCH_CODE=" + usersessionDt.PubBranchCode + ""));

                    decimal PubRes2Dbl = Convert.ToDecimal(getText("select isnull(sum (QTY),0) from ORDER2  where SAUDA_TYPE='PAUD' and SAUDA_NO=" + POmodel.SaudaNo + " and COMP_CODE=" + usersessionDt.PubCompCode + " and BRANCH_CODE=" + usersessionDt.PubBranchCode + " and YEAR_CODE=" + usersessionDt.PubFYearCode + "  and V_TYPE='RORD' and v_no<> " + POmodel.VNo + ""));


                    decimal pubBPPurchTolQty = Convert.ToDecimal(golbalGenSetting.pubBPPurchTolQty);

                    if (PubRes1Dbl + PubRes2Dbl > saudaQty + pubBPPurchTolQty)
                    {
                        return Json(new { Status = false, Message = "Total Purchase Order Quantity(" + PubRes1Dbl + PubRes2Dbl + ") is greater than Sauda Quantity (" + saudaQty + ")+Tolrance(" + pubBPPurchTolQty + ")." + Environment.NewLine + "Orders(Qty) are :" + orders + "" });
                    }


                }

                string qry = " Select ITEM_CODE,b.NAME ,sum(recd_qty) as Qty from PURCHASE2 a left join ITEM_MAST b on a.ITEM_CODE=b.CODE and b.COMP_CODE=a.COMP_CODE left join doctype_mast c on a.V_type=c.code" +
                                  " where c.Doctype='MaterialReceipt' and PO_TYPE=@PO_TYPE and PO_NO= @PO_NO and" +
                                  " a.COMP_CODE=@COMP_CODE and a.BRANCH_CODE=@BRANCH_CODE and a.YEAR_CODE=@YEAR_CODE  " +
                                  " group by a.ITEM_CODE,b.NAME";

                DataTable dt2 = new DataTable();

                using (SqlCommand cmd = new SqlCommand(qry, conn))
                {
                    cmd.Parameters.AddWithValue("@PO_TYPE", POmodel.VType);
                    cmd.Parameters.AddWithValue("@PO_NO", POmodel.VNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);

                    using (SqlDataAdapter adap1 = new SqlDataAdapter(cmd))
                    {
                        adap1.Fill(dt2);
                    }
                }


                foreach (DataRow row in dt2.Rows)
                {
                    string itemCode = row["ITEM_CODE"].ToString();
                    decimal itemQty = Convert.ToDecimal(row["Qty"]);
                    bool isFound = false;

                    foreach (var details in POmodel.ItemRecords)
                    {
                        if (itemCode == details.ItemCode.ToString())
                        {
                            isFound = true;
                            break;
                        }
                    }


                    if (!isFound && itemQty > 0)
                    {
                        string message = $"Item :({row["ITEM_CODE"]}) {row["NAME"]} not found in Order, serial no {POmodel.VNo} as per MRN record.";


                        if (usersessionDt.PubUserLevel != "1")
                        {
                            return Json(new { tatus = false,  Message = message });
                        }
                    }
                }

                 qry = @" SELECT   ITEM_CODE, b.NAME, SUM(recd_qty) AS Qty FROM PURCHASE2 a
                        LEFT JOIN ITEM_MAST b   ON a.ITEM_CODE = b.CODE   AND b.COMP_CODE = a.COMP_CODE
                        LEFT JOIN doctype_mast c  ON a.V_TYPE = c.CODE
                        WHERE   c.Doctype = 'MaterialReceipt'  AND PO_TYPE = @PO_TYPE AND PO_NO = @PO_NO
                        AND a.COMP_CODE = @COMP_CODE  AND a.BRANCH_CODE = @BRANCH_CODE AND a.YEAR_CODE = @YEAR_CODE
                        GROUP BY   a.ITEM_CODE,  b.NAME";

                DataTable dt3 = new DataTable();

                using (SqlCommand cmd = new SqlCommand(qry, conn))
                {
                    cmd.Parameters.AddWithValue("@PO_TYPE", POmodel.VType);
                    cmd.Parameters.AddWithValue("@PO_NO", POmodel.VNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);

                    using (SqlDataAdapter adap1 = new SqlDataAdapter(cmd))
                    {
                        adap1.Fill(dt3);
                    }
                }

                foreach (DataRow row in dt3.Rows)
                {
                    string itemCode = row["ITEM_CODE"].ToString();
                    string itemName = row["NAME"].ToString();

                    decimal purchaseQty = Convert.ToDecimal(row["Qty"]);

                    bool isFound = false;
                    decimal orderQty = 0;


                    foreach (var details in POmodel.ItemRecords)
                    {
                        if (itemCode == details.ItemCode.ToString())
                        {
                            isFound = true;

                       
                            orderQty += Convert.ToDecimal(details.Qty);
                        }
                    }


                    // Item not found in Purchase Order
                    if (!isFound && purchaseQty > 0)
                    {
                        string message = $"Item :({itemCode}) {itemName} and Purchase qty = {purchaseQty} not found in Order, serial no {POmodel.VNo} as per MRN record.";

                        if (usersessionDt.PubUserLevel != "1")
                        {
                            return Json(new   { Status = false,  Message = message });
                        }
                    }
                    else
                    {
                        // Purchase Qty greater than Order Qty
                        if (purchaseQty > orderQty)
                        {
                            return Json(new { Status = false,  Message = $"Item : {itemName} and Order Qty ({orderQty}) cannot be less than Purchase Qty ({purchaseQty})" });
                        }
                    }
                }

                if(POmodel.VType == "RORD" && POmodel.WbNo > 0)
                {
                    string pvno = getText("select V_NO from order1 where V_TYPE='RORD' and WB_TYPE= '" + POmodel.WbType + "' and WB_NO=" + POmodel.WbNo + " and v_no<> " + POmodel.VNo + " and COMP_CODE = " + usersessionDt.PubCompCode + " and Branch_Code=" + usersessionDt.PubBranchCode + "");
               
                   if(pvno != "")
                    {
                        return Json(new { Status = false, Message = "WB No already exit in RM Purchase Order No: " + pvno + "." });
                    }           

                }

                if (POmodel.VType == "RORD" &&  (POmodel.SaudaNo == null || POmodel.SaudaNo == 0) && POmodel.ItemRecords != null && POmodel.ItemRecords.Count > 0)
                {
                    int rflg = 0, rctr = 0;

                    foreach (var details in POmodel.ItemRecords)
                    {
                        if (string.IsNullOrWhiteSpace(details.ItemName))
                            continue;

                        if (details.ItemCode > 0)
                        {
                            qry = @"
                                SELECT TOP 1 MIN_RATE, MAX_RATE FROM MARKET_RATE1 a
                                LEFT JOIN MARKET_RATE2 b ON a.V_TYPE = b.V_TYPE AND a.V_NO = b.V_NO AND a.COMP_CODE = b.COMP_CODE
                                AND a.BRANCH_CODE = b.BRANCH_CODE
                                AND a.YEAR_CODE = b.YEAR_CODE
                                WHERE a.COMP_CODE = @COMP_CODE
                                AND a.BRANCH_CODE = @BRANCH_CODE
                                AND a.YEAR_CODE = @YEAR_CODE
                                AND a.FAPROV_STATUS = 'Approved'
                                AND b.ITEM_CODE = @ITEM_CODE
                                AND a.EFF_DATE >= @EFF_DATE
                                ORDER BY a.V_DATE DESC, a.V_NO DESC";

                            DataTable dt4 = new DataTable();

                            using (SqlCommand cmd = new SqlCommand(qry, conn))
                            {
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                cmd.Parameters.AddWithValue("@ITEM_CODE", details.ItemCode);
                                cmd.Parameters.Add("@EFF_DATE", SqlDbType.Date).Value = DateTime.Today.AddDays(-20);

                                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                                {
                                    da.Fill(dt4);
                                }
                            }

                            if (dt4.Rows.Count > 0)
                            {
                                rflg = 1;

                                decimal minRate = Convert.ToDecimal(dt4.Rows[0]["MIN_RATE"]);
                                decimal maxRate = Convert.ToDecimal(dt4.Rows[0]["MAX_RATE"]);
                                decimal itemRate = Convert.ToDecimal(details.Rate);   // Use your rate property

                                if (itemRate >= minRate && itemRate <= maxRate)
                                {
                                    rctr = 1;
                                }
                                else
                                {
                                    rflg = 0;
                                }
                            }
                        }
                    }

                    if (rflg > 0 && rctr > 0 && rflg == rctr)
                    {
                        fappstatus = "Approved";
                        fappRemark = "Document Approved.";
                    }
                    else
                    {
                        fappstatus = "";
                        fappRemark = "";

                        //return Json(new { Status = false,  Message = "Item Not found in Market Rate Master OR Rate out of Minimum and Maximum range." });
                    }
                }

                using (var cmd = new SqlCommand("sp_PurchaseOrder", conn))
                {
                        cmd.CommandType = CommandType.StoredProcedure;

                        if (POmodel.SaveOrUpdate == "Save")


                        {
                            cmd.Parameters.AddWithValue("@Action", "Add");
                        }

                        else
                        {
                            cmd.Parameters.AddWithValue("@Action", "Edit");
                        }

                        cmd.Parameters.AddWithValue("@subAction", "Header");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_NO", _dbHelper.Xnull(POmodel.VNo));
                        cmd.Parameters.AddWithValue("@V_TYPE", _dbHelper.Xnull(POmodel.VType));
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = POmodel.VDate ?? (object)DBNull.Value;
                        cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(POmodel.VType).ToString() + _dbHelper.Xnull(POmodel.VNo).ToString());
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
                        cmd.Parameters.Add("@DELIVERY_DATE", SqlDbType.SmallDateTime).Value = POmodel.DeliveryDate ?? (object)DBNull.Value;
                        cmd.Parameters.Add("@VALIDITY_DATE", SqlDbType.SmallDateTime).Value = POmodel.ValidityDate ?? (object)DBNull.Value;      
                       
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
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fappRemark);
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
                        cmd.ExecuteNonQuery();
                    }

                foreach (var Details in POmodel.ItemRecords)
                {
                    if (string.IsNullOrWhiteSpace(Details.ItemName))
                        continue;

                    using var cmd3 = new SqlCommand("sp_PurchaseOrder", conn) { CommandType = CommandType.StoredProcedure };

                    if (POmodel.SaveOrUpdate == "Save")
                    {
                        cmd3.Parameters.AddWithValue("@Action", "Add");
                    }
                    else
                    {
                        cmd3.Parameters.AddWithValue("@Action", "Edit");
                    }
                             
                    cmd3.Parameters.AddWithValue("@subAction", "ItemTABLE");
                    cmd3.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                    cmd3.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                    cmd3.Parameters.AddWithValue("@V_TYPE", POmodel.VType);
                    cmd3.Parameters.AddWithValue("@V_NO", POmodel.VNo);
                    cmd3.Parameters.AddWithValue("@DOC_ID", (POmodel.VType) + POmodel.VNo);
                    cmd3.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = POmodel.VDate == null ? DBNull.Value : Convert.ToDateTime(POmodel.VDate);        
                    cmd3.Parameters.AddWithValue("@TPLACE_CODE", Details.PlaceCode);
                    cmd3.Parameters.AddWithValue("@ITEM_NAME", Details.ItemName);
                    cmd3.Parameters.AddWithValue("@ITEM_CODE", Details.ItemCode);
                    cmd3.Parameters.AddWithValue("@MAKE_CODE", Details.MakeCode);
                    cmd3.Parameters.AddWithValue("@TNOS", Details.NOS);
                    cmd3.Parameters.AddWithValue("@TQTY", Details.Qty);
                    cmd3.Parameters.AddWithValue("@ADJ_QTY", Details.AdjQty);
                    cmd3.Parameters.AddWithValue("@GATE_QTY", Details.GateQty);
                    cmd3.Parameters.AddWithValue("@UOM_NAME", Details.UomName);
                    cmd3.Parameters.AddWithValue("@UOM_CODE", Details.UomCode);
                    cmd3.Parameters.AddWithValue("@RATE", Details.Rate);
                    cmd3.Parameters.AddWithValue("@IMPORT_RATE", Details.ImportRate);
                    cmd3.Parameters.AddWithValue("@CALC_RATE", Details.CalcRate);
                    cmd3.Parameters.AddWithValue("@TAMOUNT", Details.Amount);
                    cmd3.Parameters.AddWithValue("@PACK_PER", Details.PackPer);
                    cmd3.Parameters.AddWithValue("@TPACK_AMT", Details.PackAmt);
                    cmd3.Parameters.AddWithValue("@DISC_PER", Details.DiscPer);
                    cmd3.Parameters.AddWithValue("@TDISC_AMT", Details.DiscAmt);
                    cmd3.Parameters.AddWithValue("@TTAX_CODE", Details.TaxCode);
                    cmd3.Parameters.AddWithValue("@CGST_PER", Details.CgstPer);
                    cmd3.Parameters.AddWithValue("@TCGST_AMT", Details.CgstAmt);
                    cmd3.Parameters.AddWithValue("@SGST_PER", Details.SgstPer);
                    cmd3.Parameters.AddWithValue("@TSGST_AMT", Details.SgstAmt);
                    cmd3.Parameters.AddWithValue("@IGST_PER", Details.IgstPer);
                    cmd3.Parameters.AddWithValue("@TIGST_AMT", Details.IgstAmt);
                    cmd3.Parameters.AddWithValue("@VAT_PER", Details.VatPer);
                    cmd3.Parameters.AddWithValue("@TVAT_AMT", Details.VatAmt);
                    cmd3.Parameters.AddWithValue("@TCESS_PER", Details.CessPer);
                    cmd3.Parameters.AddWithValue("@TCESS_AMT", Details.CessAmt);
                    cmd3.Parameters.AddWithValue("@TOTH_AMT", Details.OthAmt);
                    cmd3.Parameters.AddWithValue("@TNET_AMT", Details.NetAmt);
                    cmd3.Parameters.AddWithValue("@LAND_RATE", Details.LandRate);
                    cmd3.Parameters.AddWithValue("@TSTATUS", Details.Status);
                    cmd3.Parameters.AddWithValue("@PLACE_USE", Details.PlaceUse);
                    cmd3.Parameters.AddWithValue("@DEPT_NAME", Details.DeptName);
                    cmd3.Parameters.AddWithValue("@TREMARKS", Details.Remarks);
                    cmd3.Parameters.AddWithValue("@PREORITY_LEVEL", Details.PreorityLevel);
                    cmd3.Parameters.AddWithValue("@PREORITY_REMARKS", Details.PreorityRemarks);
                    cmd3.Parameters.AddWithValue("@RATE_QUARTERLY", Details.RateQuarterly);
                    cmd3.Parameters.AddWithValue("@RATE_ANNUALY", Details.RateAnnualy);
                    cmd3.Parameters.AddWithValue("@RATE_SPECIAL", Details.RateSpecial);
                    cmd3.Parameters.AddWithValue("@REQUEST_TYPE", Details.RequestType);
                    cmd3.Parameters.AddWithValue("@REQUEST_NO", Details.RequestNo);
                    cmd3.Parameters.AddWithValue("@APPROVAL_TYPE", Details.ApprovalType);
                    cmd3.Parameters.AddWithValue("@APPROVAL_NO", Details.ApprovalNo);
                    cmd3.Parameters.AddWithValue("@DEPT_CODE", Details.DeptCode);
                    cmd3.Parameters.AddWithValue("@TDELIVERY_DATE", Details.DeliveryDate);
                    cmd3.Parameters.AddWithValue("@TSAUDA_TYPE", Details.SaudaType);
                    cmd3.Parameters.AddWithValue("@TSAUDA_NO", Details.SaudaNo);
                    cmd3.Parameters.AddWithValue("@DISP_THROUGH", Details.DispThrough);
                    cmd3.Parameters.AddWithValue("@DISP_REF", Details.DispRef);
                    cmd3.Parameters.AddWithValue("@DISP_REMARKS", Details.DispRemarks);
                    cmd3.Parameters.AddWithValue("@TENACITY_GRPCODE", Details.TenacityGrpCode);
                    cmd3.Parameters.AddWithValue("@TENACITY_TYPE", Details.TenacityType);
                    cmd3.Parameters.AddWithValue("@TENACITY_CODE", Details.TenacityCode);
                    cmd3.Parameters.AddWithValue("@TENACITY_NAME", Details.TenacityName);
                    cmd3.Parameters.AddWithValue("@TFAPROV_STATUS", Details.FAProvStatus);
                    cmd3.Parameters.AddWithValue("@TFAPROV_REMARKS", Details.FAProvRemarks);
                    cmd3.Parameters.AddWithValue("@COLOR_CODE", Details.ColorCode);
                    cmd3.Parameters.AddWithValue("@GRAM_CODE", Details.GramCode);
                    cmd3.Parameters.AddWithValue("@RATE_MONTHLY", Details.RateMonthly);
                    cmd3.Parameters.AddWithValue("@UUSER", usersessionDt.PubUserId);
                    cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@EUSER", usersessionDt.PubUserId);
                    cmd3.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@AED", "A");
                    cmd3.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID);
                    cmd3.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                    cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd3.ExecuteNonQuery();
                }

                foreach (var Attachment in POmodel.Attachments)
                {
                    if (string.IsNullOrWhiteSpace(Attachment.FileName))
                        continue;

                    using var cmd3 = new SqlCommand("sp_PurchaseOrder", conn)
                    { CommandType = CommandType.StoredProcedure };
                    if (POmodel.SaveOrUpdate == "Save")
                    {
                        cmd3.Parameters.AddWithValue("@Action", "Add");
                    }

                    else
                    {
                        cmd3.Parameters.AddWithValue("@Action", "Edit");
                    }

                    cmd3.Parameters.AddWithValue("@subAction", "Attachment");
                    cmd3.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                    cmd3.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                    cmd3.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(POmodel.VType).ToString() + _dbHelper.Xnull(POmodel.VNo).ToString());
                    cmd3.Parameters.AddWithValue("@V_NO", _dbHelper.Xnull(POmodel.VNo));
                    cmd3.Parameters.AddWithValue("@V_TYPE", _dbHelper.Xnull(POmodel.VType));
                    cmd3.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = POmodel.VDate ?? (object)DBNull.Value;
                    cmd3.Parameters.AddWithValue("@FILE_NAME", Attachment.FileName);

                    byte[]? imageBytes = null;

                    if (!string.IsNullOrWhiteSpace(Attachment.FileContentBase64))
                    {
                        imageBytes = Convert.FromBase64String(Attachment.FileContentBase64);
                    }

                    cmd3.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary, -1).Value = (object?)imageBytes ?? DBNull.Value;

                    cmd3.Parameters.AddWithValue("@FILE_Path", "/attachments/Purchase/" + (Attachment.FileName ?? ""));                   
                    cmd3.Parameters.AddWithValue("@FILE_TYPE", "Purchase Order");
                    cmd3.Parameters.AddWithValue("@UUSER", usersessionDt.PubUserId);
                    cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@EUSER", usersessionDt.PubUserId);
                    cmd3.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd3.Parameters.AddWithValue("@AED", "A");
                    cmd3.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID);
                    cmd3.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                    cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd3.ExecuteNonQuery();
                }

                return Json(new { Status = true, Message = "Data saved successfully." });
            }

            catch (Exception ex)
            {
                return Json(new  { Status = false, Message = ex.Message });
            }
        }

        private void CalculateSaudaRate(string btn, string SaudaType, int saudaNo, int CityCode, DateTime effectiveDate, List<Order2> itemList)
        {
            double saudaRate = 0.0;
            int saudaICode = 0;
            string sdate = "";

            var userdata = _globalValue.GetGlobalVariables();
            string effDate = effectiveDate.ToString("yyyyMMdd");
            // Step 1: Fetch Sauda Rate
            if (saudaNo > 0)
            {
                string query = $@" SELECT TOP 1 ITEM_CODE, RATE, V_DATE FROM SAUDA WHERE V_TYPE = '{SaudaType}'  AND V_NO = {saudaNo} AND COMP_CODE = {userdata.PubCompCode} AND BRANCH_CODE = 1";
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
                        SELECT TOP 1 ISNULL(RATE, 0) AS Rate  FROM RMDISC_MAST WHERE ITEM_CODE = {item.ItemCode}
                        AND COMP_CODE = {userdata.PubCompCode} AND SAUDA_ITEM = {saudaICode} AND EFF_DATE <= '{effDate}'
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

                // Step 4: Tax Calculation
                if (btn == "BTN")
                {
                    var stateCode = getText($"SELECT STATE_CODE FROM CITY_MAST WHERE CODE = {Convert.ToInt32(CityCode)}");
                    double igstPer = Convert.ToDouble(getText($@"
                        SELECT ISNULL(IGST_PER, 0) 
                        FROM ITEM_MAST 
                        WHERE CODE = {item.ItemCode} AND COMP_CODE = {userdata.PubCompCode}"));

                    double cgstPer = Convert.ToDouble(getText($@"
                        SELECT ISNULL(CGST_PER, 0) 
                        FROM ITEM_MAST 
                        WHERE CODE = {item.ItemCode} AND COMP_CODE = {userdata.PubCompCode}"));

                    if (userdata.STATE_CODE == stateCode)
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
        public JsonResult DDlPartyList()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string sql = $@"select distinct sg.CODE, sg.NAME  
                from SUBGROUP_MAST sg left join CITY_MAST cm on sg.CITY_CODE=cm.CODE  where sg.COMP_CODE={getdata.PubCompCode}  and UPPER(NATURE)='SUPPLIER' and
                sg.ACTIVE=1  order by NAME ";

                var DDlPartyList = _dropdownService.GetDropdownList(sql);
                return Json(DDlPartyList);
            }
        }
        public JsonResult GetCurrencyMast()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string sql = $@"select CODE, NAME from CURRENCY_MAST  order by NAME ";

                var GetCurrencyMast = _dropdownService.GetDropdownList(sql);
                return Json(GetCurrencyMast);
            }
        }
        public JsonResult GetPlaceMast()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string sql = $@"select CODE, NAME from PLACE_MAST where COMP_CODE={getdata.PubCompCode} order by NAME ";

                var GetPlaceMast = _dropdownService.GetDropdownList(sql);
                return Json(GetPlaceMast);
            }
        }
        public JsonResult GetPartyAddress(int Partycode)
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string sql = $@"select ADDRESS_ID , ADD1 from SUBGROUP_ADDRESS  where CODE = "+ Partycode + " and COMP_CODE = "+ getdata.PubCompCode + "  ";

                var GetPartyAddress = _dropdownService.GetDropdownList(sql);
                return Json(GetPartyAddress);
            }
        }
        public JsonResult GetDataByPartyCode(int PartyCode, string v_type , int v_no)
         {
            var getdata = _globalValue.GetGlobalVariables();

            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                con.Open();


                string queryadd = "";

                if (v_type != "DORD")
                {
                    queryadd = "and a.NATURE in ('supplier','vendor')";
                }
                                // -------------------- 1st QUERY (Supplier Master) --------------------
                string query1 = @" select a.CODE,ltrim(rtrim(a.name))Name,a.ADD1,a.ADD2,a.ADD3,a.CITY_CODE,a.PINCODE,a.GSTIN from SUBGROUP_MAST a  
                left join  CITY_MAST c on c.CODE = a.CITY_CODE where a.comp_code= @CompCode and a.active=1 and a.CODE = @PartyId  " + queryadd +  "  ; ";
                             


                object Partydetails = null;

                using (SqlCommand cmd = new SqlCommand(query1, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", PartyCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Partydetails = new
                            {
                                CODE = reader["CODE"].ToString(),
                                Name = reader["Name"].ToString(),
                                ADD1 = reader["ADD1"].ToString(),
                                ADD2 = reader["ADD2"].ToString(),
                                ADD3 = reader["ADD3"].ToString(),
                                CITY_CODE = reader["CITY_CODE"].ToString(),
                                PINCODE = reader["PINCODE"].ToString(),
                                GSTIN = reader["GSTIN"].ToString()                      
                            };
                        }
                    }
                }

                object SaudaDetails = null;


                if (v_type == "RORD"  && v_no > 0)
                {
                    string query2 = @" SELECT top 1  b.Name AS P_Name,c.ShortName,a.Qty,a.Rate,a.Remark,a.FRT_TERM FROM Sauda a
                    LEFT JOIN Subgroup_Mast b  ON a.Party_Code = b.Code AND a.Comp_Code = b.Comp_Code
                    LEFT JOIN Item_Mast c ON a.Item_Code = c.Code  AND a.Comp_Code = c.Comp_Code ";

                    //WHERE a.V_TYPE = 'PAUD'
                    //AND a.V_NO = @VNo AND a.COMP_CODE = @CompCode AND a.BRANCH_CODE = @BranchCode; ";
                            

                    using (SqlCommand cmd = new SqlCommand(query2, con))
                    {
                        cmd.Parameters.AddWithValue("@PartyId", PartyCode);
                        cmd.Parameters.AddWithValue("@VNo", v_no);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                SaudaDetails = new
                                {
                                    P_Name = reader["P_Name"].ToString(),
                                    ShortName = reader["ShortName"].ToString(),
                                    Qty = reader["Qty"].ToString(),
                                    Rate = reader["Rate"].ToString(),
                                    Remark = reader["Remark"].ToString(),
                                    FRT_TERM = reader["FRT_TERM"].ToString()
                                };
                            }
                        }
                    }
                }


                string PartyCountry = getText("select state_type from STATE_MAST where CODE=(select state_code from SUBGROUP_ADDRESS where CODE= " +  PartyCode +"   and isnull(IS_DEFAULT,0)=1)");

                // -------------------- FINAL RESPONSE --------------------
                return Json(new { Partydetails = Partydetails , SaudaDetails  = SaudaDetails , PartyCountry  = PartyCountry });
            }
        }
        
        public JsonResult GetSaudaList(int partyCd)
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string sql = $@" SELECT distinct  V_NO, V_TYPE FROM SAUDA WHERE PARTY_CODE={partyCd}  and 
                COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} and BRANCH_CODE = {getdata.PubBranchCode} and STATUS = 1 order by V_NO ";

                var GetSaudaList = _dropdownService.GetDropdownList(sql);
                return Json(GetSaudaList);
            }
        }

        [HttpGet]
        public async Task<object> GetDataByOrder(int V_NO)
        {
            var GetGlobalCode = _globalValue.GetGlobalVariables();
            var Datalist = new List<object>();
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd3 = new SqlCommand("[dbo].[sp_PurchaseOrder]", con))
                    {
                        cmd3.CommandType = CommandType.StoredProcedure;
                        cmd3.Parameters.AddWithValue("@Action", "Order");
                        cmd3.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd3.Parameters.AddWithValue("@BRANCH_CODE", GetGlobalCode.PubBranchCode);
                        cmd3.Parameters.AddWithValue("@SaudaNo", V_NO);

                        using (SqlDataReader rdr = cmd3.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                while (rdr.Read())
                                {
                                    var OrderNo = rdr["OrderNo"]?.ToString();
                                    var Party = rdr["Party"]?.ToString();
                                    var ItemName = rdr["ItemName"]?.ToString();
                                    var Quantity = rdr["Quantity"]?.ToString();
                                    var Rate = rdr["Rate"]?.ToString();
                                                                  
                                        Datalist.Add(new
                                        {
                                            OrderNo = OrderNo,
                                            Party = Party,
                                            ItemName = ItemName,
                                            Quantity = Quantity,
                                            Rate = Rate
                                        });
                                    
                                }
                            }
                        }
                    }
                }

                return (new { success = true, data = Datalist });
            }
            catch (Exception ex)
            {
                return (new { success = false, message = "Error fetching attachment data", error = ex.Message });
            }
        }

        public JsonResult GetDataByShipPartyCode(int PartyCode)
        {
            var getdata = _globalValue.GetGlobalVariables();

            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                con.Open();               

        
                // -------------------- 1st QUERY (Supplier Master) --------------------
                string query1 = @" select a.CODE,ltrim(rtrim(a.name))Name,a.ADD1,a.ADD2,a.ADD3,a.CITY_CODE,a.PINCODE,a.GSTIN from SUBGROUP_MAST a  
                left join  CITY_MAST c on c.CODE = a.CITY_CODE where a.comp_code= @CompCode and a.active=1 and a.CODE = @PartyId   ; ";

                object Partydetails = null;

                using (SqlCommand cmd = new SqlCommand(query1, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", PartyCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Partydetails = new
                            {
                                CODE = reader["CODE"].ToString(),
                                Name = reader["Name"].ToString(),
                                ADD1 = reader["ADD1"].ToString(),
                                ADD2 = reader["ADD2"].ToString(),
                                ADD3 = reader["ADD3"].ToString(),
                                CITY_CODE = reader["CITY_CODE"].ToString(),
                                PINCODE = reader["PINCODE"].ToString(),
                                GSTIN = reader["GSTIN"].ToString()
                            };
                        }
                    }
                }
  
                // -------------------- FINAL RESPONSE --------------------
                return Json(new { Partydetails = Partydetails});
            }
        }

        public JsonResult GetDataByPartyAddressID(int PartyCode, int AddressCode)
        {
            var getdata = _globalValue.GetGlobalVariables();

            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                con.Open();              
        
                // -------------------- 1st QUERY (Supplier Master) --------------------
                    string query1 = @" SELECT   a.Add1, a.Add2, a.Add3,  a.GSTIN, a.City_Code,a.Pincode  FROM Subgroup_Address a
                        LEFT JOIN STATE_MAST b   ON a.STATE_CODE = b.Code
                        LEFT JOIN CITY_MAST c   ON a.CITY_CODE = c.Code
                        WHERE   a.Comp_Code = @CompCode  AND a.Code = @PartyId AND a.Address_Id = @AddressId;   ; ";

                object PartyAddress = null;

                using (SqlCommand cmd = new SqlCommand(query1, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", PartyCode);
                    cmd.Parameters.AddWithValue("@AddressId", AddressCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            PartyAddress = new
                            {
                                Add1 = reader["Add1"].ToString(),
                                Add2 = reader["Add2"].ToString(),
                                Add3 = reader["Add3"].ToString(),
                                GSTIN = reader["GSTIN"].ToString(),
                                City_Code = reader["City_Code"].ToString(),
                                Pincode = reader["Pincode"].ToString()                 
                            };
                        }
                    }
                }

                // -------------------- FINAL RESPONSE --------------------
                return Json(new { PartyAddress = PartyAddress });
            }
        }

        [HttpGet]
        public async Task<object> GetModificationData(int V_NO)
        {
            var globalCode = _globalValue.GetGlobalVariables();
            var dataList = new List<object>();

            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PurchaseOrder]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "MODIFICATIONHISTORY");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalCode.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_NO", V_NO);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                dataList.Add(new
                                {
                                    V_NO = rdr["V_NO"]?.ToString(),
                                    VDate = rdr["VDate"]?.ToString(),
                                    PartyName = rdr["PartyName"]?.ToString(),
                                    PRICE_TYPE = rdr["PRICE_TYPE"]?.ToString(),
                                    PARTY_REF = rdr["PARTY_REF"]?.ToString(),
                                    QTY = rdr["QTY"]?.ToString(),
                                    AMOUNT = rdr["AMOUNT"]?.ToString(),
                                    PACK_AMT = rdr["PACK_AMT"]?.ToString(),
                                    DISC_AMT = rdr["DISC_AMT"]?.ToString(),
                                    CGST_AMT = rdr["CGST_AMT"]?.ToString(),
                                    SGST_AMT = rdr["SGST_AMT"]?.ToString(),
                                    IGST_AMT = rdr["IGST_AMT"]?.ToString(),
                                    OTH_AMT = rdr["OTH_AMT"]?.ToString(),
                                    VAT_AMT = rdr["VAT_AMT"]?.ToString(),
                                    CESS_AMT = rdr["CESS_AMT"]?.ToString(),
                                    NET_AMT = rdr["NET_AMT"]?.ToString(),
                                    DELIVERY_TERM = rdr["DELIVERY_TERM"]?.ToString(),
                                    PAYMENT_TERM = rdr["PAYMENT_TERM"]?.ToString(),
                                    REMARKS = rdr["REMARKS"]?.ToString()         
                                });
                            }
                        }
                    }
                }

                return new
                {
                    success = true,
                    data = dataList
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = "Error fetching modification data.",
                    error = ex.Message
                };
            }
        }

        public JsonResult GetDataByTaxType(int TaxCode)
        {
            var getdata = _globalValue.GetGlobalVariables();

            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                con.Open();


                // -------------------- 1st QUERY (Supplier Master) --------------------
                string query1 = @" select top 1 CGST_PER, SGST_PER,IGST_PER,isnull(VAT_PER,0) as VAT_PER,TDS_PER,TCS_PER,OTH_PER,
                isnull(OTH_PER2,0) as OTH_PER2 from TAX_MAST  where ACTIVE = 1 and CODE =@TaxCode  ";

                object Partydetails = null;

                using (SqlCommand cmd = new SqlCommand(query1, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@TaxCode", TaxCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Partydetails = new
                            {

                                CGST_PER = reader["CGST_PER"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["CGST_PER"]),
                                SGST_PER = reader["SGST_PER"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["SGST_PER"]),
                                IGST_PER = reader["IGST_PER"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["IGST_PER"]),
                                VAT_PER = reader["VAT_PER"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["VAT_PER"]),
                                TDS_PER = reader["TDS_PER"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TDS_PER"]),
                                TCS_PER = reader["TCS_PER"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TCS_PER"]),
                                OTH_PER = reader["OTH_PER"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["OTH_PER"]),
                                OTH_PER2 = reader["OTH_PER2"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["OTH_PER2"])
                            };
                        }
                    }
                }
     
                return Json(new { Partydetails = Partydetails });
            }
        }
        public  JsonResult PrintValidation(string V_TYPE, int V_NO)
        {
            var getdata = _globalValue.GetGlobalVariables();

            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                con.Open();

                string query = @"SELECT FAPROV_STATUS FROM ORDER1 WHERE V_TYPE = @V_TYPE AND V_NO = @V_NO AND COMP_CODE = @COMP_CODE 
                AND BRANCH_CODE = @Branch_Code AND YEAR_CODE = @Year_Code";

                string faProvStatus = string.Empty;

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", V_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", V_NO);
                    cmd.Parameters.AddWithValue("@Branch_Code", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@Year_Code", getdata.PubFYearCode);

                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        faProvStatus = result.ToString();   
                    }
                }

                string query2 = @"  Select PRINTNAME from DOCTYPE_MAST where CODE=@V_TYPE ";

                string Reportname = string.Empty;

                using (SqlCommand cmd1 = new SqlCommand(query2, con))
                {
                    cmd1.Parameters.AddWithValue("@V_TYPE", V_TYPE);      
                    object result = cmd1.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        Reportname = result.ToString();
                    }
                }


                string signatoryList = string.Empty;

                using (SqlCommand cmd = new SqlCommand("sp_PurchaseOrder", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@Action", SqlDbType.VarChar, 50).Value = "GetApprovalName";
                    cmd.Parameters.Add("@DOC_ID", SqlDbType.VarChar, 50).Value = V_TYPE + V_NO;
                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = getdata.PubCompCode;
                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = getdata.PubBranchCode;

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            signatoryList = dr["SignatoryList"].ToString();
                        }
                    }
                }

                return Json(new { status = true,  FAPROV_STATUS = faProvStatus, Reportname = Reportname , signatoryList = signatoryList , message = "Print validation successful." });
            }
        }
        public JsonResult CheackMail(int v_no, string v_type)
        {
            var globalVaraible = _globalValue.GetGlobalVariables();

            string FAPROV_STATUS = GetText("select FAPROV_STATUS from SAUDA where FAPROV_STATUS='Approved' and V_TYPE='PAUD' and V_NO=" + v_no + " and " +
            "COMP_CODE=" + globalVaraible.PubCompCode + " and BRANCH_CODE=" + globalVaraible.PubBranchCode + " and YEAR_CODE=" + globalVaraible.PubFYearCode + " ");


            if (FAPROV_STATUS != "Approved")
            {
                return Json(new { status = false, message = "Document not approved, Mail not sent." });
            }
            else
            {
                return Json(new { status = true ,  message = "" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMail(int PartyCode, int vno,string v_type, IFormFile file)
        {
            try
            {
                var globalVaraible = _globalValue.GetGlobalVariables();

                if (file == null)
                    return Json(new { success = false, message = "Report file missing" });

                using var ms = new MemoryStream();
                file.CopyTo(ms);

                byte[] pdfBytes = ms.ToArray();

                //string Mail = GetText("Select EMAIL from SUBGROUP_MAST WHERE CODE= " + PartyCode +
                //                      " AND COMP_CODE= " + globalVaraible.PubCompCode);

                string Mail = "sg256001@gmail.com";

                if (Mail == "")
                {
                    return Json(new { success = false, message = "Email address is blank for the selected party." });
                }

                string compname = GetText("Select COMP_NAME from COMP_MAST WHERE CODE= " + globalVaraible.PubCompCode);

                string mailBody = "Please find attached Purchase Order.<br><br><br>";
                mailBody += "Kindly send us acceptance mail of Purchase Order within 3 days, otherwise it will deemed to be accepted.";
                mailBody += "<br><br>Regards,<br>" + compname + "<br>" + globalVaraible.Address1 + "<br>" + globalVaraible.Address2;

                return await _globalValidationdate.GlobalSendMail(v_type, vno, Mail, mailBody, file, "Order1", "");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        public async Task<JsonResult> GetDataByItemCode(int Itemcode,string itemname, string v_type, int partycode , string partyname)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var globalGenSetting = await _globalValue.LoadGeneralSetting();
                using SqlConnection con = _dbcontext.GetErpConnection();
                await con.OpenAsync();

                string qry = string.Empty;

                if (v_type != "RORD" && v_type != "DORD")
                {
                    if (partycode > 0 && globalGenSetting.pubDefRateAppInPO == "Yes")
                    {
                            string lastAppRateDate = getText(@"SELECT TOP 1 V_DATE FROM QUOTATION2 WHERE V_TYPE = 'STAP'  AND ITEM_CODE = " + Itemcode +  " AND FAPROV_STATUS = 'Approved'" +
                                " AND PARTY_CODE = " + partycode +  " AND COMP_CODE = " + userSession.PubCompCode + " AND BRANCH_CODE = " + userSession.PubBranchCode +
                                " ORDER BY V_DATE DESC");

                        if (!string.IsNullOrWhiteSpace(lastAppRateDate))
                        {
                            if (DateTime.TryParse(lastAppRateDate, out DateTime approvedRateDate))
                            {                               
                                int expiredDays = Convert.ToInt32(globalGenSetting.pubRateExpiredDays); 
                                if (approvedRateDate.AddDays(expiredDays) < DateTime.Today)
                                {
                                    return Json(new  { Status = false,   Message = "Approved Rate Expired of Party=> "+ partyname + "  Item=>  "+ itemname + " " });
                                }
                            }
                        }

                        qry = @" SELECT TOP 1 d.NAME AS Make, b.UNIT_NAME AS Unit,  a.QTY,   a.RATE, a.PACK_PER, a.DISC_PER,   e.NAME AS TaxType, a.CGST_PER, a.SGST_PER,  a.IGST_PER,
                            a.VAT_PER,  a.CESS_PER,  a.PREORITY_LEVEL, a.OTH_EXPS,  a.TECH_DESC,  a.REQ_TYPE AS ReqType, a.REQ_NO AS ReqNo,  a.V_TYPE AS AppType, a.V_NO AS AppNo,
                            a.MAKE_CODE,  a.TAX_CODE, b.UNIT_CODE FROM QUOTATION2 a  LEFT JOIN ITEM_MAST b  ON a.ITEM_CODE=b.CODE  AND b.COMP_CODE=a.COMP_CODE
                            LEFT JOIN ITEM_MAKE c ON a.ITEM_CODE=c.ITEM_CODE  AND c.COMP_CODE=a.COMP_CODE
                            LEFT JOIN ITEMMAKE_MAST d  ON c.MAKE_CODE=d.CODE AND d.COMP_CODE=a.COMP_CODE
                            LEFT JOIN TAX_MAST e ON a.TAX_CODE=e.CODE
                            WHERE a.PARTY_CODE = @PARTY_CODE AND a.ITEM_CODE = @ITEM_CODE  AND b.ACTIVE = 1  AND a.FAPROV_STATUS = 'Approved'  AND a.COMP_CODE = @COMP_CODE
                            AND a.BRANCH_CODE = @BRANCH_CODE  ORDER BY a.V_DATE DESC, a.V_NO DESC";
                    }
                    else
                    {
                        qry = @"  SELECT TOP 1 d.NAME AS Make,   a.UNIT_NAME AS Unit,  0 AS QTY,    0 AS RATE,
                            0 AS PACK_PER,  0 AS DISC_PER, '' AS TaxType, 0 AS CGST_PER,  0 AS SGST_PER,  0 AS IGST_PER,
                            0 AS VAT_PER,  0 AS CESS_PER, 0 AS PREORITY_LEVEL, 0 AS OTH_EXPS,  '' AS TECH_DESC,  '' AS ReqType,
                            0 AS ReqNo, '' AS AppType,  0 AS AppNo, c.MAKE_CODE, 0 AS TAX_CODE,  a.UNIT_CODE
                            FROM ITEM_MAST a
                            LEFT JOIN ITEM_MAKE c ON a.CODE=c.ITEM_CODE AND c.COMP_CODE=a.COMP_CODE
                            LEFT JOIN ITEMMAKE_MAST d ON c.MAKE_CODE=d.CODE AND d.COMP_CODE=a.COMP_CODE
                            WHERE a.CODE=@ITEM_CODE  AND a.ACTIVE=1 AND a.FAPROV_STATUS='Approved'
                            AND a.COMP_CODE=@COMP_CODE  ORDER BY a.NAME";
                    }

                    using SqlCommand cmd = new SqlCommand(qry, con);

                    cmd.Parameters.AddWithValue("@ITEM_CODE", Itemcode);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", partycode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);

                    using SqlDataReader dr = await cmd.ExecuteReaderAsync();

                    if (await dr.ReadAsync())
                    {
                        var data = new
                        {
                            Make = dr["Make"]?.ToString() ?? "",
                            Unit = dr["Unit"]?.ToString() ?? "",
                            Qty = dr["QTY"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["QTY"]),
                            Rate = dr["RATE"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["RATE"]),
                            PackPer = dr["PACK_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["PACK_PER"]),
                            DiscPer = dr["DISC_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DISC_PER"]),
                            TaxType = dr["TaxType"]?.ToString() ?? "",
                            CGSTPer = dr["CGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["CGST_PER"]),
                            SGSTPer = dr["SGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["SGST_PER"]),
                            IGSTPer = dr["IGST_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["IGST_PER"]),
                            VATPer = dr["VAT_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["VAT_PER"]),
                            CESSPer = dr["CESS_PER"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["CESS_PER"]),
                            PriorityLevel = dr["PREORITY_LEVEL"] == DBNull.Value ? 0 : Convert.ToInt32(dr["PREORITY_LEVEL"]),
                            OthExps = dr["OTH_EXPS"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["OTH_EXPS"]),
                            TechDesc = dr["TECH_DESC"]?.ToString() ?? "",
                            ReqType = dr["ReqType"]?.ToString() ?? "",
                            ReqNo = dr["ReqNo"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ReqNo"]),
                            AppType = dr["AppType"]?.ToString() ?? "",
                            AppNo = dr["AppNo"] == DBNull.Value ? 0 : Convert.ToInt32(dr["AppNo"]),
                            MakeCode = dr["MAKE_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["MAKE_CODE"]),
                            TaxCode = dr["TAX_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["TAX_CODE"]),
                            UnitCode = dr["UNIT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(dr["UNIT_CODE"])
                        };

                        return Json(new {  Status = true, Data = data, Message = "Data Found" });
                    }

                    return Json(new { Status = false,  Message = "No record found." });
                }

                return Json(new { Status = true, Message = "Item validation not applicable for this document type."  });
            }
            catch (Exception ex)
            {
                return Json(new {  Status = false, Message = ex.Message     });
            }
        }

    }
}
