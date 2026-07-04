using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class CourierTrackingEntryListController : Controller
    {
        //private readonly CourierTrackingEntryListRepository _repository;

        //public CourierTrackingEntryListController(ICourierTrackingEntryListRepository repository)
        //{
        //    _repository = (CourierTrackingEntryListRepository?)repository;
        //}
        //public IActionResult Index()
        //{
        //    return View("~/Views/GateEntry/Transaction/CourierTrackingEntryList/Index.cshtml");
        //}


        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;

        private int? userLevel;
        private readonly ICourierTrackingEntryListRepository _repository;
        public CourierTrackingEntryListController(GlobalVariableService globalVariableService,
            DropdownService dropdownService, ICourierTrackingEntryListRepository repository)
        {
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;

            _repository = repository;
        }
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/CourierTrackingEntryList/Index.cshtml");
        
        }

        [HttpGet]
        public IActionResult GetCourierTrackingEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            var result = _repository.GetCourierTrackingEntryList(searchTerm, pageNumber, pageSize);
            return Json(new
            {
                status = result.status,
                items = result.data,      
                totalCount = result.totalCount,
                message = result.message
            });
        }
        [HttpPost]
        public async Task<IActionResult> Delete(string vNo, string docType)
        {
            var result = await _repository.DeleteCourierTrackingEntry(vNo, docType);
            return Json(new
            {
                status = result.status,
                message = result.message
            });
        }
    }
}