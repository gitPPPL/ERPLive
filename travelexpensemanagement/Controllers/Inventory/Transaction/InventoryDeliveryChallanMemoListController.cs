using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    [SessionAuthorize]
    public class InventoryDeliveryChallanMemoListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly GlobalExcelExport _excel;
        private readonly DbHelper _dbHelper;
        private readonly IInventoryDeliveryChallanMemoListRepository _repo;
        public InventoryDeliveryChallanMemoListController(GlobalVariableService globalVariableService, 
            GlobalValidationdate globalValidationdate, GlobalExcelExport excel, DbHelper dbHelper, IInventoryDeliveryChallanMemoListRepository repo)
        {
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _excel = excel;
            _dbHelper = dbHelper;
            _repo = repo;
        }
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/InventoryDeliveryChallanMemoList/Index.cshtml");
        }
        const string doctype = "GTMO";
        [HttpGet]
        public async Task<IActionResult> GetAllDeliveryMemoList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _repo.GetAllDeliveryMemoList(searchTerm, pageNumber, pageSize);
            if(result.data == null || !result.status)
            {
                return Json(new { status = false, message = result.message });
            }
            return Json(new { status = true, data = result.data, totalCount = result.totalCount });
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
        public IActionResult Delete(int vNo)
        {
            var result = _repo.Delete(vNo);
            return Json(new { status = result.status, message = result.message });
        }

        [HttpGet]
        public async Task<JsonResult> PBPEntryDetails(string vNo)
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
                    {"@V_TYPE", doctype},
                    {"@V_NO", vNo },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_InventoryDeliveryChallanMemo]", parameter);
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

                var fileBytes = _excel.ExportToExcel("sp_InventoryDeliveryChallanMemo", "Delivery Challan Memo", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"DeliveryChallanMemo{DateTime.Now:ddMMyyyy}.xlsx"
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
