using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class DeliveryOrderListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly IDeliveryOrderListRepositroy _deliveryOrderListRepository;

        public DeliveryOrderListController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService, IDeliveryOrderListRepositroy deliveryOrderListRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
            _deliveryOrderListRepository = deliveryOrderListRepository;
        }

        //public IActionResult Index()
        //{
        //    return View("~/Views/Sales/Transaction/DeliveryOrderList/Index.cshtml");
        //}

        public IActionResult Index(string mode = "DOrder")
        {
            ViewBag.FormMode = mode;
            return View("~/Views/Sales/Transaction/DeliveryOrderList/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> LoadListData(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var result = await _deliveryOrderListRepository.LoadListDataAsync(searchTerm, pageNo, pageSize);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }




    }
}
