using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Repositories.Implementations.Purchase.Transaction.PendingSaudaOrderRepository;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PendingSaudaOrderController : Controller
    {
        private readonly IPendingSaudaOrderRepository _repository;
        private readonly DropdownService _dropdownService;
        public PendingSaudaOrderController(DropdownService dropdownService, IPendingSaudaOrderRepository repository)
        {
            _repository = repository;
            _dropdownService = dropdownService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/PendingSaudaOrder/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetddlDocType()
        {
            return _repository.GetddlDocType();
        }
        [HttpGet]
        public JsonResult GetdocNumber(string vType)
        {
            return _repository.GetdocNumber(vType);
        }
        [HttpGet]
        public JsonResult GetfilterType(string vType)
        {
            return _repository.GetfilterType(vType);
        }
        [HttpGet]
        public JsonResult GetStatus()
        {
            return _repository.GetStatus();
        }
        [HttpGet]
        public JsonResult GetPendingData(string vType,string refType,string status,string source,DateTime fromDate,DateTime toDate, string itemSearch)
        {
            return _repository.GetPendingData(vType, refType, status, source, fromDate, toDate, itemSearch);
        }
        [HttpPost]
        public IActionResult SaveData([FromBody] PendingSaudaOrderSaveModel request)
        {
            return _repository.SaveData(request);
        }
    }
}
