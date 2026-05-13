using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class StoreWeighbridgeEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly GlobalVariableService _globalValue;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly IStoreWeighbridgeEntryRepository _storeWbEntry;
        public StoreWeighbridgeEntryController(DbHelper dbHelper, GlobalVariableService globalValue, 
            GlobalValidationdate globalValidationdate, DropdownService dropdownService, IStoreWeighbridgeEntryRepository storeWbEntry)
        {
            _dbHelper = dbHelper;
            _globalValue = globalValue;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _storeWbEntry = storeWbEntry;
        }

        public IActionResult Index()
        {
            return View("~/Views/Weighbridge/Transaction/StoreWeighbridgeEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetMaxVNo(string V_type)
        {
            try
            {
                var vType = V_type;
                string tableName = "WB1";
                var result = _globalValidationdate.GetVNo(vType, tableName);
                var docId = (V_type) + result;
                var newVno = result;
                var docIdNoList = new { DocId = docId, VNo = newVno };
                return Json(new { status = true, data = docIdNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        //=========Dropdowns==============
        public JsonResult GetDropdown(string type, string VTypeId = "")
        {
            var gv = _globalValue.GetGlobalVariables();
            var data = type switch
            {
                "DocType" => _dropdownService.GetDocTypeWithParam(new List<string> { "KSIN", "KSOT" }),
                "DocStatus" => _dropdownService.GetDocStatus(),
                "Party" => _dropdownService.GetAllParty(gv.PubCompCode),
                "Place" => _dropdownService.GetPlace(gv.PubCompCode),
                "Items" => _dropdownService.GetItems(gv.PubCompCode),
                _ => new List<DropdownService.DropdownModel>()
            };
            return Json(data);
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
                   where g.COMP_CODE={userDt.PubCompCode}  and g.YEAR_CODE={userDt.PubFYearCode} and
                   g.BRANCH_CODE={userDt.PubBranchCode} AND g.V_TYPE IN
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
        public async Task<IActionResult> GetGateEntryDetailList(int V_no, string V_type)
        {
            var gv = _globalValue.GetGlobalVariables();
            try
            {               
                var gatelist = await _dbHelper.GetJsonDataAsync($@"select distinct ITEM_CODE , ITEM_NAME  from GATE2 where COMP_CODE ={gv.PubCompCode} and BRANCH_CODE={gv.PubBranchCode}  and V_NO = {V_no} and V_TYPE = '{V_type}' order by ITEM_NAME");
                return Json(new { status = true, data = gatelist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStoreWeighBridgeById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { status = false, message = "Invalid Id!" });
            }
            var result = await _storeWbEntry.getStoreWbById(id);
            if (!result.status)
            {
                return Json(new { status = result.status, message = result.message });
            }
            if(result == null || result.data == null)
            {
                return Json(new { status = false, message = "Data load failed!" });
            }
            return Json(new { status = result.status, header = result.data.Header, detail = result.data.Detail });
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateStoreWeighBridgeEntry([FromBody] WBEntryModel model)
        {
            if (model == null)
                return Json(new { status = false, message = "Data save failed." });
            var result = await _storeWbEntry.saveOrUpdate(model);
            return Json(new { status = result.status, message = result.message });
        }
    }
}
