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
                //"City" => _dropdownService.GetCity(gv.PubCompCode),
                //"Courier" => _dropdownService.GetCourier(),
                //"Purpose" => _dropdownService.GetPurpose(),
                "Employee" => _dropdownService.GetEmployee(gv.PubCompCode),
                "printDocType" => _dropdownService.GetDocType(),
                _ => new List<DropdownService.DropdownModel>() 
            };
            return Json(data);
        }
        [HttpGet]
        public JsonResult SearchParty(string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = _dropdownService.SearchParty(gv.PubCompCode, term);
            return Json(data);
        }
        [HttpGet]
        public JsonResult SearchCity(string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = _dropdownService.GetCity(gv.PubCompCode, term);
            return Json(data);
        }

        [HttpGet]
        public JsonResult SearchCourier(string term = "")
        {
            var data = _dropdownService.GetCourier(term);
            return Json(data);
        }
        [HttpGet]
        public JsonResult SearchPurpose(string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = _dropdownService.GetPurpose(gv.PubCompCode, term);
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

        [HttpPost]
        public IActionResult PrintCourierReport([FromBody] PrintCourierReportModel model)
        {
            if (!model.FromDate.HasValue || !model.ToDate.HasValue)
            {
                return Json(new
                {
                    success = false,
                    message = "From Date and To Date are required."
                });
            }

            var report = _repository.PrintCourierReport(model);

            return Json(new
            {
                success = true,
                report = report
            });
        }
        public class CourierTrackingReportModel
        {
            public string Reportname { get; set; }
            public string Database { get; set; }
            public string SelectionFormula { get; set; }
            public List<FormulaFieldModel> FormulaFields { get; set; }
        }

        public class FormulaFieldModel
        {
            public string FormulaName { get; set; }
            public string FormulaValue { get; set; }
        }
        public class PrintCourierReportModel
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public string VType { get; set; }
            public string PartyName { get; set; }
        }
        public class CodeRequest
        {
            public string docNo { get; set; }
            public string docType { get; set; }
        }

    }
}
