using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class InsuranceCoverNoteListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly IInsuranceCoverNoteListRepository _insuranceCoverNoteListRepository;

        public InsuranceCoverNoteListController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService,  travelexpensemanagement.LogService.LogService logService,  IInsuranceCoverNoteListRepository insuranceCoverNoteListRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
            _insuranceCoverNoteListRepository = insuranceCoverNoteListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/InsuranceCoverNoteList/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> LoadListData(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var result = await _insuranceCoverNoteListRepository.LoadListDataAsync(searchTerm, pageNo, pageSize);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteData(int vNo, string vType)
        {
            try
            {
                var result = await _insuranceCoverNoteListRepository.DeleteDataAsync(vNo, vType);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new  { success = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            try
            {
                var usersession = _globalVariableService.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", "INSU"},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_Sale3]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

    }
}
