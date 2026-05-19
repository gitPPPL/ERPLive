using DocumentFormat.OpenXml.Spreadsheet;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Reflection.Emit;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    [SessionAuthorize]
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

        //===Validate VDate
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("WB1", vdate, vtype, vno);
            return Ok(result);
        }
        //===Dropdowns
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
                string strqry = $@"select a.V_TYPE as V_TYPE, a.V_NO as V_NO, b.NAME as NAME from GATE1 a left join DOCTYPE_MAST b on b.CODE = a.V_TYPE 
                                where a.COMP_CODE ={userDt.PubCompCode} and a.YEAR_CODE ={userDt.PubFYearCode} and a.BRANCH_CODE ={userDt.PubBranchCode}
                                and b.DOCTYPE in ('GateInward') order by a.v_no";
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
                var parameter = new Dictionary<string, object>
                {
                    {"@Action", "GetGateDetailsByGateNo" },
                    {"@COMP_CODE", gv.PubCompCode },
                    //{"@YEAR_CODE", gv.PubFYearCode },
                    {"@BRANCH_CODE", gv.PubBranchCode},
                    {"@V_NO",  V_no},
                    {"@V_TYPE", V_type }
                };
                var gatelist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter);
    //            var gatelist = $@"
    //               select distinct a.ITEM_CODE , a.ITEM_NAME, b.PARTY_CODE as Party from 
    //GATE2 a 
    //left join GATE1 b on a.COMP_CODE =b.COMP_CODE and a.BRANCH_CODE =b.BRANCH_CODE
    //and b.YEAR_CODE =a.YEAR_CODE and b.V_TYPE =a.V_TYPE and b.V_NO =a.V_NO 
    //left join ITEM_MAST c on c.CODE =a.ITEM_CODE and c.COMP_CODE = a.COMP_CODE 
    //where a.COMP_CODE ={gv.PubCompCode} and a.BRANCH_CODE={gv.PubBranchCode}  and 
    //a.V_NO = {V_no} and a.V_TYPE = {V_type}
    //order by ITEM_NAME ";
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
