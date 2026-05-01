using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VehicleInwardEntryController : Controller
    {
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IVehicleInwardRepository _VehicleInwardRepository;
        public VehicleInwardEntryController(ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate,
            IVehicleInwardRepository VehicleInwardRepository)
        {
            _globalValidationdate = globalValidationdate;
            _VehicleInwardRepository = VehicleInwardRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/VehicleInwardEntry/Index.cshtml");
        }

        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            var result = await _VehicleInwardRepository.MaxVNo(V_type);
            return Json(new { status = result.status, data = result.data, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            var result = await _VehicleInwardRepository.DocType();
            return Json(new { status = result.status, data = result.data, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            var result = await _VehicleInwardRepository.PartyList();
            return Json(new { status = result.status, data = result.data, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GetTransportationList()
        {
            var result = await _VehicleInwardRepository.TransportationList();
            return Json(new { status = result.status, data = result.data, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GetDONo()
        {
            var result = await _VehicleInwardRepository.DONo();
            return Json(new { status = result.status, data = result.data, message = result.message });
        }
        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateTransportInward([FromForm] TransportInwardModel POmodel)
        {
            if (POmodel == null)
            {
                return Json(new { status = false, message = " Invalid data." });
            }
            var result = await _VehicleInwardRepository.SaveOrUpdate(POmodel);
            return Json(new {status = result.status, message = result.message});
        }
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("Gate1", vdate, vtype, vno);
            return Ok(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetDriverDetails(string mobileNo)
        {
            if (string.IsNullOrEmpty(mobileNo))
            {
                return Json(new { success = false, message = "Invalid mobile number!" });
            }
            var result = await _VehicleInwardRepository.DriverDetails(mobileNo);
            return Json(new {status = result.status, message = result.message, driverDetails  = result.data });
        }
        [HttpGet]
        public async Task<JsonResult> GetVehcleinfo([FromQuery] string rc_number/*, string VType, int VNo*/)
        {
            if (string.IsNullOrEmpty(rc_number))
            {
                return Json(new { success = false, message = "Invalid rc number!" });
            }
            var result = await _VehicleInwardRepository.VehicleInfoApi(rc_number);
            return Json(new {status = result.status, message = result.message, vehicleInfo  = result.data});
        }
        [HttpGet]
        public async Task<JsonResult> GetVehicleInfoFromDB(string vehicleNo)
        {
            if (string.IsNullOrEmpty(vehicleNo))
            {
                return Json(new { success = false, message = "Invalid vehicle number!" });
            }
            var result = await _VehicleInwardRepository.VehicleInfoFromDB(vehicleNo);
            return Json(new { success = result.status, message = result.message, vehicleInfo = result.data });
        }
        [HttpGet]
        public async Task<IActionResult> GetTransportInwardRecordsById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = await _VehicleInwardRepository.TransportInwardRecordsById(id);
            return Json(new { status = result.status, message = result.message, data = result.data });
        }
    }
}
