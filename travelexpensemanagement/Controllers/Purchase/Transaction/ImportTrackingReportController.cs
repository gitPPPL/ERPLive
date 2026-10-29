using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportTrackingReportController : Controller
    {

        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly IImportTrackingReportRepository _repo;

        public ImportTrackingReportController(GlobalVariableService globalValue, DbHelper dbHelper, IImportTrackingReportRepository repo)
        {
            _globalValue = globalValue;
            _dbHelper = dbHelper;
            _repo = repo;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportTrackingReport/Index.cshtml");
        }

        //================Import Tracking List==================
        [HttpGet]
        public async Task<object> GetReportData(DateTime FromDate, DateTime ToDate)
        {
            try
            {
                var result = await _repo.GetReportData(FromDate, ToDate);
                if (result.data != null)
                {
                    return new { success = true, data = result.data.Rows, totalUpdatedRecords = result.data.TotalUpdatedRecords };
                }
                return new { success = result.status, message = result.message };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        //================Documents to be Send==================
        public async Task<IActionResult> GetDropdown(string type, string data = "")
        {
            var gv = _globalValue.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "vtype":
                    qry = "Select Code as value, Name as text from Doctype_mast where Code in ('EXIM') order by Name";
                    break;
                case "doctype":
                    qry = "Select distinct doctype as value, doctype as text from Doctype_mast where doctype in ('EXIMTracking') order by doctype";
                    break;
                case "sendto":
                    qry = $@"select distinct d.CODE as value, d.full_name as text
                             from DOCFLOW_USERS a
                             left join DOCTYPE_MAST b on a.Doc_code = b.code
                             left join SUBUSER_MAST c on a.USER_CODE = c.USER_CODE
                             left join USER_MAST d on c.USER_CODE = d.code and a.COMP_CODE = d.COMP_CODE
                             where a.Comp_code = {gv.PubCompCode} and a.DOC_CODE = '{data}' and a.SACTION = 'Receive Document'
                             order by d.full_name";
                    break;
                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }

        [HttpGet]
        public async Task<object> GetDocumentListData(DateTime FromDate, DateTime ToDate, string DocStatus, string Searchby, string SearchText, string vType)
        {

            try
            {
                var result = await _repo.GetDocumentListData(FromDate, ToDate, vType, DocStatus, Searchby, SearchText);

                if (result.data != null)
                    return new { success = true, data = result.data };
                else
                    return new { success = result.status, message = result.message };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        [HttpPost]
        public IActionResult SendDocument([FromBody] List<ImportTrackingReport> model)
        {
            if (model == null || model.Count == 0)
                return BadRequest(new { success = false, message = "No data received" });

            if (model.Any(x => x.SEND_TO == null || x.SEND_TO == 0))
                return Json(new { success = false, validation = true, message = "Please select 'Send To' User." });

            var res = _repo.SendDocument(model);

            int sent = res.data?.Sent ?? 0;
            int already = res.data?.Already ?? 0;

            if (!string.IsNullOrEmpty(res.message))
                return Json(new { success = false, sent, already, message = res.message });

            return Json(new { success = res.status, sent, already });
        }
    }
}
