using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge.Transaction;
using static iTextSharp.text.pdf.AcroFields;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class BigWeighbridgeController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly IBigWeighbridgeRepository _bigWeighbridgeRepository;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        public BigWeighbridgeController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, IBigWeighbridgeRepository bigWeighbridgeRepository, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _bigWeighbridgeRepository = bigWeighbridgeRepository;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }

        public IActionResult Index()
        {
            var userSession = _globalValue.GetGlobalVariables();
            ViewBag.PubCompCode = userSession?.PubCompCode ?? "0";

            return View("~/Views/Weighbridge/Transaction/BigWeighbridge/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            try
            {
                var result = await _bigWeighbridgeRepository.GetMaxVNoAsync(V_type);

                return Json(new
                {
                    status = true,
                    data = new
                    {
                        DocId = result.DocId,
                        VNo = result.VNo
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGateNo(string wbType)
        {
            try
            {
                var result = await _bigWeighbridgeRepository.GetGateNoAsync(wbType);

                return Json(new
                {
                    status = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var result = await _bigWeighbridgeRepository.GetDocTypeAsync();

                return Json(new
                {
                    status = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemList()
        {
            try
            {
                var result = await _bigWeighbridgeRepository.GetItemListAsync();

                return Json(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlaceMast()
        {
            try
            {
                var result = await _bigWeighbridgeRepository.GetPlaceMastAsync();

                return Json(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            try
            {
                var result = await _bigWeighbridgeRepository.GetPartyListAsync();

                return Json(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeighBridgeById(string id)
        {
            try
            {
                var result = await _bigWeighbridgeRepository.GetWeighBridgeByIdAsync(id);

                return Json(new
                {
                    status = true,
                    header = result.Header,
                    detail = result.Detail
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateWeighBridgeEntry([FromBody] WBEntryModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Invalid request data"
                    });
                }

                var result = await _bigWeighbridgeRepository.SaveOrUpdateWeighBridgeEntryAsync(model);

                return Json(new
                {
                    status = result.Status,
                    message = result.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalValue.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("WB1", vdate, vtype, vno);
            return Ok(result);
        }

    }
}
