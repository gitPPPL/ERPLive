using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VehicleInwardEntryListController : Controller
    {
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IVehicleInwardListRepository _VehicleInwardListRepository;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbContext;
        private readonly GlobalValidationdate _globalValidationdate;
        public VehicleInwardEntryListController(ModuleService.ModuleService moduleService, IVehicleInwardListRepository VehicleInwardListRepository, 
            GlobalVariableService globalVariableService, DataBaseConnection dbContext, GlobalValidationdate globalValidationdate)
        {
            _moduleService = moduleService;
            _VehicleInwardListRepository = VehicleInwardListRepository;
            _globalVariableService = globalVariableService;
            _dbContext = dbContext;
            _globalValidationdate = globalValidationdate;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Vehicle Inward";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string databaseName;
            using (var connection = _dbContext.GetErpConnection())
            {
                databaseName = connection.Database; // Get the database name
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/GateEntry/Transaction/VehicleInwardEntryList/Index.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> GetTransportInwardList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _VehicleInwardListRepository.GetTransportInwardList(searchTerm, pageNumber, pageSize);
            return Json(new { success = result.status, data = result.data, totalCount = result.totalCount });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteVehicleInwardEntry(string docId)
        {
            var result = await _VehicleInwardListRepository.DeleteTransportInward(docId);
            return Json(new { success = result.status, message = result.message });
        }
        [HttpGet]
        public async Task<IActionResult> GetVehicleInwardEntryDetails(string docid)
        {
            var result = await _VehicleInwardListRepository.VehicleInwardEntryDetails(docid);
            return Json(new { status = result.status, data = result.data, message = result.message });
        }
        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@YEAR_CODE", gv.PubFYearCode },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    { "@Action", "Excel" }
                };

                var fileBytes = _globalValidationdate.ExportToExcel("sp_GetTransportInwardEntry", "Transport Inward", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"TransportInward_{DateTime.Now:ddMMyyyy}.xlsx"
                );
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
