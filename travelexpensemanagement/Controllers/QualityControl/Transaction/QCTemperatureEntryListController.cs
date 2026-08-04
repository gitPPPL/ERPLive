using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.GlobalExcel;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]
    public class QCTemperatureEntryListController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IQCTemperatureEntryListRepository _qCTempListRepository;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly GlobalExcelExport _excel;
        public QCTemperatureEntryListController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, 
            GlobalVariableService globalValue, ModuleService.ModuleService moduleService, IQCTemperatureEntryListRepository qCTempListRepository, 
            GlobalValidationdate globalValidationdate, GlobalExcelExport excel)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _qCTempListRepository = qCTempListRepository;
            _globalValidationdate = globalValidationdate;
            _excel = excel;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "QC Temprature Entry";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/QualityControl/Transaction/QCTemperatureEntryList/Index.cshtml", model);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetQcTempratureList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var result = await _qCTempListRepository.GetList(searchTerm, pageNumber, pageSize);
            if(result.data != null)
            {
                return Json(new { success = result.status, data = result.data, totalCount = result.totalCount });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQcTempratureEntry(string docId)
        {
            if (string.IsNullOrEmpty(docId))
            {
                return Json(new { status = false, message = "Invalid ID" });
            }
            var result = await _qCTempListRepository.Delete(docId);
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public async Task<IActionResult> GetQcTempratureEntryDetails(string docid)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                if (string.IsNullOrEmpty(docid))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE",  usersession.PubBranchCode},
                    {"@V_TYPE", "TAPE" },
                    {"@V_NO", docid.Substring(4) },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetQcTempratureEntry]", parameter);
                return Json(new { status = true, data = entryDetailList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> ExportAllDocs()
        //{
        //    try
        //    {
        //        var usersession = _globalValue.GetGlobalVariables();
        //        var parameter = new Dictionary<string, object>
        //        {
        //            {"@COMP_CODE", usersession.PubCompCode },
        //            {"@YEAR_CODE", usersession.PubFYearCode },
        //            {"@BRANCH_CODE", 1},
        //            {"@V_TYPE",  "TAPE"},
        //            {"@Action", "Excel" }
        //        };
        //        var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetQcTempratureEntry]", parameter);

        //        return Json(new { status = true, data = dataList });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }
        //}
        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalValue.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@YEAR_CODE", gv.PubFYearCode },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    {"@V_TYPE",  "TAPE"},
                    { "@Action", "Excel" }
                };

                var fileBytes = _excel.ExportToExcel("sp_GetQcTempratureEntry", "QC Tape Line", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"QCTapeLine_{DateTime.Now:ddMMyyyy}.xlsx"
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
  