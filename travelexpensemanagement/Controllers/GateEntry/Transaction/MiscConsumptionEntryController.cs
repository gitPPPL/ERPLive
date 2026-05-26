using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    [SessionAuthorize]
    public class MiscConsumptionEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IMiscConsumptionRepository _repository;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        public MiscConsumptionEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DropdownService dropdownService, DbHelper dbHelper, GlobalValidationdate globalValidationdate, IMiscConsumptionRepository repository,
            ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _repository = repository;
            _logService = logService;
        }

        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/MiscConsumptionEntry/Index.cshtml");
        }

        public JsonResult DDLItemMaster()
        {
            var data = _repository.GetItemList();
            return Json(data);
        }

        public JsonResult DDLDeptMaster()
        {
            var data = _repository.GetDeptList();
            return Json(data);
        }

        public JsonResult DDLUnit()
        {
            var data = _repository.GetUnitList();
            return Json(data);
        }

        public JsonResult GetDropdown(string type)
        {
            var data = _repository.GetDropdown(type);
            return Json(data);
        }

        public JsonResult GetAddressByPartyCode(int partyId)
        {
            var data = _repository.GetAddressByPartyCode(partyId);
            return Json(data);
        }
         
        [HttpGet]
        public JsonResult GetVNo(string Vtype)
        {
            try
            {
                var vno = _repository.GenerateVNo(Vtype);
                return Json(new {success = true, V_NO = vno});
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message});
            }
        }

        [HttpPost]
        public IActionResult SaveData([FromBody] MiscConsumptionEntryModel request)
        {
            if (request?.Header == null)
            {
                return Json(new
                {
                    success = false, message = "Invalid request data"
                });
            }

            try
            {
                var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";

                var result = _repository.SaveMiscConsumption( request.Header, request.Deatils, action );

                //_logservice.insertlog("gate1", "miscconsumptionentry", "transaction", action, request.header.v_type, request.header.v_no.tostring(),
                //          request.header.v_date);

                //_logservice.insertlog("gate2", "miscconsumptionentry", "transaction", action, request.header.v_type, request.header.v_no.tostring(),
                //          request.header.v_date);

                //if (action == "update")
                //{
                //    _globalvalidationdate.loginsertupdatedelete("gate1", "gate1", "transaction", request.header.v_no.tostring(), request.header.v_type);

                //    _globalvalidationdate.loginsertupdatedelete("gate2", "gate2", "transaction", request.header.v_no.tostring(), request.header.v_type);
                //}

                if (result == "Success")
                {
                    return Json(new { success = true, message = "Saved successfully" });
                }
                else
                {
                    return Json(new{success = false, message = result});
                }
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("GATE1", vdate, vtype, vno);
            return Ok(result);
        }

    }
}
