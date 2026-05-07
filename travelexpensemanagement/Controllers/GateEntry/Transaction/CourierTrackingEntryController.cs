using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.PortableExecutable;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class CourierTrackingEntryController : Controller
    {
   
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
 
        private int? userLevel;
        private readonly ICourierTrackingEntryRepository _repository;
        public CourierTrackingEntryController( GlobalVariableService globalVariableService,
            DropdownService dropdownService, ICourierTrackingEntryRepository repository)
        {
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
      
            _repository = repository;
        }
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/CourierTrackingEntry/Index.cshtml");
        }
        public JsonResult GetDropdown(string type)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = type switch
            {
                "DocType" => _dropdownService.GetDocType(),
                "City" => _dropdownService.GetCity(gv.PubCompCode),
                "Party" => _dropdownService.GetParty(gv.PubCompCode),
                "Courier" => _dropdownService.GetCourier(),
                "Purpose" => _dropdownService.GetPurpose(),
                "Employee" => _dropdownService.GetEmployee(gv.PubCompCode),
                _ => new List<DropdownService.DropdownModel>() 
            };
            return Json(data);
        }
        public JsonResult GetDocNo(string docType)
        {
            int nextVNo = _repository.GetNextDocNo(docType);
            return Json(new { success = true, nextVNo });
        }
        [HttpPost]
        public JsonResult SaveCourierData([FromBody] CourierTrackingModel model)
        {
            var message = _repository.SaveCourierData(model);
            return Json(new { success = true, message });
        }
        [HttpPost]
        public IActionResult GetCourierDataList([FromBody] CodeRequest request)
        {
            var data = _repository.GetCourierData(request.docType, request.docNo);
            if (data == null)
                return NotFound(new { message = "No data found" });
            return Json(data);
        }
        public class CodeRequest
        {
            public string docNo { get; set; }
            public string docType { get; set; }
        }

    }
}
