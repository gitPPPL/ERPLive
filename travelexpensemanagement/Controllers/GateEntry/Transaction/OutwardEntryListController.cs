using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;



namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class OutwardEntryListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly IOutwardEntryListRepository _outwardEntryListRepository;
        public OutwardEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
         DbHelper dbHelper, ModuleService.ModuleService moduleService, IOutwardEntryListRepository outwardEntryListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _outwardEntryListRepository = outwardEntryListRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/OutwardEntryList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetList(  string searchTerm = "", int pageNumber = 1,  int pageSize = 10)
        {
            try
            {
                var result = await _outwardEntryListRepository.GetList(  searchTerm, pageNumber,  pageSize);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {  success = false,  message = "Error fetching data.",  error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetDataByCode(  [FromForm] int rowId,  [FromForm] string vtype)
        {
            try
            {
                var result = await _outwardEntryListRepository.GetDataByCode(rowId, vtype);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new  { success = false, message = "Error fetching data.",  error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string docId)
        {
            try
            {
                var result = await _outwardEntryListRepository.Delete(docId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new  { success = false, message = "Error deleting data.", error = ex.Message });
            }
        }

        public async Task<IActionResult> DocDetailsCode(string docCode)
        {
            try
            {
                var result = await _outwardEntryListRepository.DocDetailsCode(docCode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new  { success = false, message = "Error fetching document details.",  error = ex.Message  });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {
            try
            {
                var fileBytes = await _outwardEntryListRepository
                    .ExportToExcel(searchTerm);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "GateInward.xlsx");
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error exporting excel.",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf(string searchTerm = null)
        {
            try
            {
                var fileBytes = await _outwardEntryListRepository .ExportToPdf(searchTerm);
                return File( fileBytes,  "application/pdf", "GateInward.pdf");
            }
            catch (Exception ex)
            {
                return Json(new { success = false,  message = "Error exporting pdf.",  error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDataByPendingorder( int PartyCode, string Type, DateTime v_date,  int BILL_NO)
        {
            try
            {
                var result = await _outwardEntryListRepository.GetDataByPendingorder( PartyCode,  Type,  v_date, BILL_NO);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new  { success = false, message = "Error fetching pending order data.",  error = ex.Message });
            }
        }

    }
}  