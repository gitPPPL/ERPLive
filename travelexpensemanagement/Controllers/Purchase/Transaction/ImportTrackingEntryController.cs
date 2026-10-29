using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportTrackingEntryController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IImportTrackingEntryRepository _importTrackingEntryRepository;
        public ImportTrackingEntryController(GlobalVariableService globalVariableService, DbHelper dbHelper, GlobalValidationdate globalValidationdate, 
            IImportTrackingEntryRepository importTrackingEntryRepository)
        {
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _importTrackingEntryRepository = importTrackingEntryRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportTrackingEntry/Index.cshtml");
        }

        public async Task<IActionResult> GetDropdown(string type, string data = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "sauda":
                    qry = $@"Select V_no as text, doc_id as value from SAUDA Where V_type='PAUD' and comp_code = {gv.PubCompCode} and Branch_code = {gv.PubBranchCode} 
                            order by V_No desc";
                    break;
                case "doctype":
                    qry = "Select Code as value, Name as text from DOCTYPE_MAST where Code='EXIM' order by Name";
                    break;
                case "supplier":
                    qry = $@"select CODE as value, name as text from SUBGROUP_MAST where Nature in ('Customer','Supplier') and COMP_CODE = {gv.PubCompCode} 
                            AND ACTIVE=1 order by name";
                    break;
                case "agent":
                    qry = $@"select CODE as value, name as text from SUBGROUP_MAST where Nature in ('Broker') and COMP_CODE = {gv.PubCompCode} AND ACTIVE=1 
                            order by name";
                    break;
                case "item":
                    qry = $@"select a.CODE as value, a.NAME as text from ITEM_MAST a left join ITEM_MGROUP b on b.CODE =a.MGROUP_CODE and b.COMP_CODE = 
                            a.COMP_CODE where b.MGROUP_TYPE in ('RAW') and a.Active=1 and a.comp_code = {gv.PubCompCode} order by a.name";
                    break;
                case "delivery":
                    qry = "Select Code as value, Name as text from City_mast where active=1 order by Name";
                    break;
                case "payterm":
                    qry = $"Select Code as value, Name as text from PAYTERM_MAST where comp_code = {gv.PubCompCode} order by Name";
                    break;
                case "paybank":
                    qry = $"select distinct BANK_CODE as value, BANK_NAME as text from BANKTD_MAST where COMP_CODE = {gv.PubCompCode} order by BANK_NAME";
                    break;
                case "country":
                    qry = "Select Code as value, Name as text from COUNTRY_MAST order by Name";
                    break;
                case "port":
                    qry = "Select Portcode as value, Name as text from PORT_MAST order by Name";
                    break;
                case "currency":
                    qry = "Select Code as value, shortName as text from Currency_mast where Active=1 order by shortName";
                    break;
                case "cha":
                    qry = $"select CODE as value, name as text from SUBGROUP_MAST where Nature in ('Supplier') and COMP_CODE = {gv.PubCompCode} AND ACTIVE=1 order by name";
                    break;
                case "bank":
                    qry = "Select Code as value, Name as text from BANK_MAST where active=1 order by Name";
                    break;
                case "transport":
                    qry = "select name as text, code as value from TRANSPORT_MAST where ACTIVE = 1 order by name";
                    break;
                case "expsupplier":
                    qry = $"select name as text, code as value from Subgroup_mast where Comp_code= {gv.PubCompCode} and ACTIVE = 1  order by name";
                    break;
                case "expensetype":
                    qry = @"select ExpsType as value, ExpsType as text from (Select 'Clearing Agent' ExpsType union all select 'Clearing Exps' union all select 'Container' 
                            union all select 'Custom Duty' union all select 'Detention' union all select 'Freight' union all select 'Lab Test' 
                            union all select 'Shipping Line') x";
                    break;
                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }

        // ==========V_NO=========
        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "EXIM1");
            return Json(new { status = true, V_NO = result });
        }

        // ===========Sauda Details=======
        [HttpGet]
        public async Task<JsonResult> GetSaudaDetails(string saudaNo)
        {
            try
            {
                var result = await _importTrackingEntryRepository.GetSaudaDetails(saudaNo);
                if (result.data == null)
                    return Json(new { success = false, message = "Sauda Details not found!" });
                return Json(new { success = true, data = result.data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unable to load Sauda details." + ex.Message });
            }
        }

        // ===========Check existing entry for a Sauda=======
        [HttpGet]
        public async Task<JsonResult> GetEntryBySauda(string saudaNo, string vType)
        {
            var result = await _importTrackingEntryRepository.GetEntryBySauda(saudaNo, vType);

            if (!result.status)
                return Json(new { success = false, message = result.message });

            return Json(new
            {
                success = true,
                exists = ((dynamic)result.data).exists,
                vNo = ((dynamic)result.data).vNo
            });
        }

        // ===========Load existing entry (DBTOTEXT)=======
        [HttpGet]
        public async Task<JsonResult> GetEntry(string vType, int vNo)
        {
            try
            {
                var result = await _importTrackingEntryRepository.GetEntry(vType, vNo);
                if (result == null)
                    return Json(new { success = false, message = "Import Tracking Entry not found." });
                dynamic data = result.data;
                return Json(new { success = true, data = data.header, containers = data.containers, expenses = data.expenses, files = data.files });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unable to load entry. " + ex.Message });
            }
        }

        // ===========Validation=======
        [HttpGet]
        public JsonResult GetValidationFlags()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            bool skipDocChecks = gv.PubUserId == "258" && gv.PubCompCode == "1";
            return Json(new { success = true, skipDocChecks });
        }

        // ===========Save & Update=======
        [HttpPost]
        public async Task<JsonResult> Save([FromBody] ImportTrackingSaveRequest req)
        {
            if (req == null)
                return Json(new { success = false, message = "Invalid request data." });
            try
            {
                var result = await _importTrackingEntryRepository.Save(req);
                return Json(new { success = result.status, message = result.message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Unable to save. " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetGlobalValues()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var response = new
                {
                    compCode = gv.PubCompCode,
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
