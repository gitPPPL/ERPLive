using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Gate_Entry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    [SessionAuthorize]
    public class VisitorEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly IVisitorRepository _visitorRepo;

        public VisitorEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
        ModuleService.ModuleService moduleService , GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, IVisitorRepository visitorRepo)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
            _visitorRepo = visitorRepo;
        }

        public IActionResult Index()
        {
            //var globalVar = _globalVariableService.GetGlobalVariables();
            //ViewBag.CompCode = globalVar.PubCompCode;
            //ViewBag.BranchCode = globalVar.PubBranchCode;
            //ViewBag.YearCode = globalVar.PubFYearCode;
            //ViewBag.GlobalVariable = _globalVariableService.GetGlobalVariables();
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database; 
            }
            ViewBag.DatabaseName = databaseName;
            var globalVariables = _globalVariableService.GetGlobalVariables();

            ViewBag.GlobalVariables = globalVariables;
            return View("~/Views/GateEntry/Transaction/VisitorEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetEmpList()
        {
            string query = "SELECT CODE,NAME FROM EMP_MAST WHERE ACTIVE=1 AND NAME<>'' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public JsonResult GenerateVNo()
        {
            try
            {
                string vNo = _visitorRepo.GenerateVNo();
                return Json(new { v_NO = vNo, v_TYPE = "VISI" });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveVisitorEntry([FromBody] VisitorWrapper data)
        {
            if (data?.Visitor == null)
                return Json(new { success = false, message = "Invalid data" });

            var model = data.Visitor;

            model.V_TYPE = "VISI";
            model.DOC_ID = model.V_TYPE + model.V_NO;

            string action = _visitorRepo.IsDuplicate(model.DOC_ID) ? "UPDATE" : "INSERT";

            try
            {
                // ================= IMAGE HANDLING =================

                if (data.Image != null && data.Image.IsRemoved)
                {
                    model.IMG_FILE = null;
                    model.FILE_NAME = "";
                }
                else if (data.Image != null && !string.IsNullOrEmpty(data.Image.Base64Content))
                {
                    var base64 = data.Image.Base64Content.Split(',').Last();
                    model.IMG_FILE = Convert.FromBase64String(base64);
                    model.FILE_NAME = $"{model.V_NO}_{data.Image.FileName}";
                }
                else if (action == "UPDATE")
                {
                    var oldData = _visitorRepo.GetVisitorImage(model.DOC_ID);

                    if (oldData != null)
                    {
                        model.IMG_FILE = oldData.IMG_FILE;
                        model.FILE_NAME = oldData.FILE_NAME;
                    }
                }

                // SAVE VIA REPO
                bool result = _visitorRepo.SaveUpdateVisitor(model, action);

                if (!result)
                    return Json(new { success = false, message = "Save failed" });

                _logService.InsertLog("VISITOR", "Visitor Entry", "TRANSACTION", action, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);

                //if (action == "UPDATE")
                //{
                //    _globalValidationdate.LogInsertUpdateDelete(
                //       destinationTable: "VISITOR",
                //       sourceTable: "VISITOR",
                //        transactionType: "Transaction",
                //      codeVNo: model.V_NO.ToString(),
                //       vtype: model.V_TYPE
                //   );
                //}

                return Json(new
                {
                    success = true,
                    message = action == "INSERT"
                        ? "Visitor Saved Successfully"
                        : "Visitor Updated Successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult DeleteVisitorEntry(string docId)
        {
            string VType = docId.Substring(0, 4);
            string VNo = docId.Substring(4);

            try
            {
                bool result = _visitorRepo.DeleteVisitor(docId);

                if (result)
                {
                    _logService.InsertLog("VISITOR", "Visitor Entry", "TRANSACTION", "DELETE", VType, VNo, null
                );

                    return Json(new { success = true, message = "Visitor deleted successfully." });
                }

                return Json(new { success = false, message = "Delete failed" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetVisitorByMobile(string mobileNo)
        {
            var result = _visitorRepo.GetVisitorByMobile(mobileNo);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("VISITOR", vdate, vtype, vno);
            Console.WriteLine("vdate: " + vdate);
            Console.WriteLine("today: " + DateTime.Today);
            Console.WriteLine("LoginDate: " + global.PubLoginDate.Date);
            return Ok(result);
        }

        //===Check Modification Days============
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

    }
}
 