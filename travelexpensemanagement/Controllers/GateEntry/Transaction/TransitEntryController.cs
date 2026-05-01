using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Models;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;


namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TransitEntryController : Controller
    {
        private readonly ITransitEntryRepository _iTransitEntryRepository;
        public TransitEntryController(ITransitEntryRepository iTransitEntryRepository)
        {
            _iTransitEntryRepository = iTransitEntryRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/TransitEntry/Index.cshtml");
        }
        public async Task<JsonResult> GetVNo(string Vtype)
        {
            var result = await _iTransitEntryRepository.MaxVNo(Vtype);
            return Json(new {status = result.status, message = result.message, V_NO = result.data });
        }
        public JsonResult GetDropdown(string type, string VTypeId = "")
        {
            var result = _iTransitEntryRepository.GetDDl(type, VTypeId);
            return Json(result.data);
        }
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
    }
}
