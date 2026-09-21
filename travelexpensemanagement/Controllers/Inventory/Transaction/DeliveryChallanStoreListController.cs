using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class DeliveryChallanStoreListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DbHelper _dbHelper;
        private readonly GlobalExcelExport _excel;
        private readonly IDeliveryChallanStoreListRepository _deliveryChallanStoreListRepository;
        public DeliveryChallanStoreListController(GlobalVariableService globalVariableService, GlobalValidationdate globalValidationdate, DbHelper dbHelper,
            IDeliveryChallanStoreListRepository deliveryChallanStoreListRepository, GlobalExcelExport excel)
        {
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _deliveryChallanStoreListRepository = deliveryChallanStoreListRepository;
            _excel = excel;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/DeliveryChallanStoreList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetAllDeliveryChallanList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = _deliveryChallanStoreListRepository.GetAllDeliveryChallanList(searchTerm, pageNumber, pageSize);
            if (result.status)
            {
                return Json(new { status = true, data = result.data, totalCount = result.totalCount });
            }
            else
            {
                return Json(new { status = false, message = result.message });
            }
        }
     
        [HttpGet]
        public JsonResult checkModificationDays(DateTime? vDate)
        {
            if (!vDate.HasValue)
            {
                return Json(new { success = false, message = "Doc Date is empty!!" });
            }
            var (allowed, message) = _globalValidationdate.CheckModificationDays(vDate.Value);
            return Json(new { success = true, isAllowed = allowed, message = message });
        }

        [HttpPost]
        public IActionResult Delete(int vNo, string docType)
        {
            var result = _deliveryChallanStoreListRepository.Delete(vNo, docType);
            return Json(new { status = result.status, message = result.message });
        }
      
        [HttpGet]
        public async Task<IActionResult> GetApprovalStatus(int vNo, string vType)
        {
            var result = await _deliveryChallanStoreListRepository.GetApprovalStatus(vNo, vType);
            return Json(new { success = result.status, exists = result.data });
        }

        [HttpGet]
        public async Task<JsonResult> PBPEntryDetails(string vNo, string vType)
        {
            try
            {
                var usersession = _globalVariableService.GetGlobalVariables();
                if (string.IsNullOrEmpty(vNo))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", vType},
                    {"@V_NO", vNo },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_DeliveryChallanStore]", parameter);
                return Json(new { status = true, data = entryDetailList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
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

                var fileBytes = _excel.ExportToExcel("sp_DeliveryChallanStore", "Delivery Challan Store", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"DeliveryChallanStore{DateTime.Now:ddMMyyyy}.xlsx"
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
