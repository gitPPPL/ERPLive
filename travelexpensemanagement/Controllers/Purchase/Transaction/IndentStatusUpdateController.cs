using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.IndentStatusUpdateModel;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class IndentStatusUpdateController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IIndentStatusUpdateRepository _indentStatusUpdateRepository;
        public IndentStatusUpdateController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IIndentStatusUpdateRepository indentStatusUpdateRepository)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _indentStatusUpdateRepository = indentStatusUpdateRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/IndentStatusUpdate/Index.cshtml");
        }

        //==========For text box dropdown search==================
        [HttpGet]
        public JsonResult GetDropdown(string type, string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var data = type switch
            {
                "SupplierName" => _dropdownService.GetSupplierName(gv.PubCompCode, term),
                _ => new List<DropdownService.DropdownModel>()
            };

            return Json(data);
        }
           
        [HttpGet]
        public JsonResult SearchSupplierName(string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var data = _dropdownService.GetSupplierName(gv.PubCompCode, term);

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetStorePurchaseOrderStatus(DateTime fromDate,DateTime toDate,int? supplierCode)
        {
            try
            {
                var list = await _indentStatusUpdateRepository.GetStorePurchaseOrderStatusAsync(fromDate, toDate, supplierCode);

                return Json(new
                {
                    success = true,
                    data = list
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveIndentStatus([FromBody] List<IndentStatusUpdateSaveModel> model)
        {
            try
            {
                var result = await _indentStatusUpdateRepository.SaveIndentStatusAsync(model);

                return Json(new
                {
                    success = result.Success,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }

}
