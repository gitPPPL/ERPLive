using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseQuotationListController : Controller 
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly IPurchaseQuotationListRepository _quotationRepository;
        public PurchaseQuotationListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper,
        ModuleService.ModuleService moduleService, IPurchaseQuotationListRepository quotationRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _quotationRepository = quotationRepository;
        }

        public IActionResult Index()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = globalVar.PubBranchCode;
            ViewBag.YearCode = globalVar.PubFYearCode;
            ViewBag.CurrentMenu = "Purchase Quotation";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Purchase/Transaction/PurchaseQuotationList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllQuotations(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var result = await _quotationRepository.GetAllQuotationsAsync(searchTerm, pageNumber, pageSize);

                return Json(new { success = true, quotations = result.Quotations, totalCount = result.TotalCount });
            }
            catch (Exception ex)
            {
                return Json(new  { success = false, message = "Error fetching quotations", error = ex.Message});
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQuotationByCode(int vNo, string vType)
        {
            try
            {
                var quotation = await _quotationRepository.GetQuotationByCodeAsync(vNo, vType);

                return Json(new { success = true, data = quotation});
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = "Error fetching quotation", error = ex.Message});
            }
        }

    }
}
 