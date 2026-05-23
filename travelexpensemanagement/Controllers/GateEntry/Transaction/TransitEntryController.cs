using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Models;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TransitEntryController : Controller
    {
        private readonly ITransitEntryRepository _iTransitEntryRepository;
        private readonly GlobalValidationdate _globalValidationdate;
        public TransitEntryController(ITransitEntryRepository iTransitEntryRepository, GlobalValidationdate globalValidationdate)
        {
            _iTransitEntryRepository = iTransitEntryRepository;
            _globalValidationdate = globalValidationdate;
        }
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/TransitEntry/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetVNo(string Vtype)
        {
            var result = _globalValidationdate.GetVNo(Vtype, "WAYBILL1");
            return Json(new {status = true, V_NO = result });
        }
        [HttpGet]
        public async Task<JsonResult> GetExist(int vNo, string form_No)
        {
            if(vNo == 0 && string.IsNullOrEmpty(form_No))
            {
                return Json(new { status = false, message = "Invalid data!"});
            }
            var result = await _iTransitEntryRepository.IsExist(vNo, form_No);
            return Json(new { status = result.status, message = result.message, data = result.data });
        }
        [HttpGet]
        public JsonResult GetDropdown(string type, string VTypeId = "")
        {
            var result = _iTransitEntryRepository.GetDDl(type, VTypeId);
            return Json(result.data);
        }
        [HttpGet]
        public async Task<JsonResult> fetchPartyGstinNo(int Partycode)
        {
            var result = await _iTransitEntryRepository.PartyGstinNo(Partycode);
            return Json(result.data);
        }
        [HttpPost]
        public async Task<IActionResult> Savedata([FromBody] TransitEntryModel data)
        {
            var result = await _iTransitEntryRepository.SaveData(data);
            return Json(new { success = result.status, message = result.message });
        }
        [HttpGet]
        public async Task<JsonResult> GetEWayBillDatacall(DateTime edate, string inoutdata)
        {
            try
            {
                var result = await _globalValidationdate.GetEWayBillData(edate, inoutdata);
                return result;
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }
    }
}
