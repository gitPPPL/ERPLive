using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesCreditLimitEntryController : Controller
    {
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly ISalesCreditLimitEntryRepository _salesCreditLimitRepository;
        public SalesCreditLimitEntryController(GlobalValidationdate globalValidationdate, GlobalVariableService globalVariableService,
            DbHelper dbHelper, ISalesCreditLimitEntryRepository salesCreditLimitRepository)
        {
            _globalValidationdate = globalValidationdate;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _salesCreditLimitRepository = salesCreditLimitRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/SalesCreditLimitEntry/Index.cshtml");
        }
        const string doctype = "CLMT";

        [HttpGet]
        public JsonResult GetVNo()
        {
            var result = _globalValidationdate.GetVNo(doctype, "CREDIT_LIMIT");
            return Json(new { status = true, V_NO = result });
        }

        public async Task<IActionResult> GetDropdown(string type)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "party":
                    qry = $@"SELECT a.CODE AS value, a.NAME AS text, a.GROUP_CODE, b.NAME as GroupName, c.CR_LIMIT, c.CR_DAYS, c.OURCR_DAYS, c.APPROVAL_TYPE, 
                            c.OURAPPROVAL_TYPE FROM SUBGROUP_MAST a
                            LEFT JOIN MGROUP_MAST b ON b.CODE = a.GROUP_CODE AND b.COMP_CODE = a.COMP_CODE
                            LEFT JOIN CRLIMIT_MAST c ON c.PARTY_CODE = a.CODE AND c.COMP_CODE = a.COMP_CODE
                            WHERE a.COMP_CODE = {gv.PubCompCode} AND a.NATURE IN ('Customer','Supplier','Broker') AND a.ACTIVE = 1 
                            ORDER BY a.NAME";
                    break;

                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = doctype;
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("CREDIT_LIMIT", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDrCrAmtByPartyCode(int code)
        {
            var result = await _salesCreditLimitRepository.GetDrCrAmtByPartyCodeAsync(code);
            dynamic data = result.data!;
            return Json(new { success = result.status, DrAmt = data.DrAmt, CrAmt = data.CrAmt });
        }

        [HttpPost]
        public async Task<IActionResult> SaveSalesCreditLimit([FromBody] SalesCreditLimitModel model)
        {
            if (model == null) return Json(new { success = false, message = "Invalid Request" });
            var result = await _salesCreditLimitRepository.SaveSalesCreditLimitAsync(model, doctype);
            return Json(new { success = result.status, message = result.message });

        }
        
        [HttpGet]
        public async Task<IActionResult> GetDataById(int vNo)
        {
            if (vNo <= 0)
            {
                return Json(new { success = false, message = "Invalid Request!" });
            }
            var result = await _salesCreditLimitRepository.GetDataById(vNo, doctype);
            if (result.data == null)
            {
                return Json(new { success = false, message = result.message });
            }
            return Json(new { success = true, data = result.data });
        }
    }
}
