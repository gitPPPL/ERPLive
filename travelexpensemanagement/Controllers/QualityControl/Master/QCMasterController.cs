using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;
using static StackExchange.Redis.Role;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class QCMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly IQCMasterRepository _iqCMasterRepository;

        public QCMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     DropdownService dropdownService, DbHelper dbHelper, IQCMasterRepository iqCMasterRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _iqCMasterRepository = iqCMasterRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/QCMaster/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetDropdown(string type)
        {
            string query = "";
  
            switch (type)
            {
                case "QCGroup":
                    query = $@"Select Code, Name From QCG_MAST order by Name asc";
                    break;
            }
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetddlParameter()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $@"Select a.CODE as Code, a.name as Name, b.NAME as Unit, b.CODE as Ucode from QCP_MAST a left join QCPUNIT_MAST b on a.QUNIT_CODE =b.code where a.comp_code={compCode} order by name";
            var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = dataList });
        }

        [HttpGet]
        public async Task<JsonResult> getExistOrNot(string inputData)
        {
            if (string.IsNullOrEmpty(inputData))
            {
                return Json(new { success = false, message = "Invalid Name" });
            }
            var result = await _iqCMasterRepository.GetExistOrNotAsync(inputData);
            if (result.data)
            {
                return Json(new { status = result.status, exists = result.data });
            }
            return Json(new { status = result.status, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> InsertDataQcMaster([FromBody] QCMaster model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid Data!" });
            }
            var result = await _iqCMasterRepository.InsertDataQcMasterAsync(model);
            return Json(new { success = result.status, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDataQcMaster([FromBody] QCMaster model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid Data!" });
            }
            var result = await _iqCMasterRepository.UpdateDataQcMasterAsync(model);
            return Json(new { success = result.status, message = result.message });
        }
        
        [HttpPost]
        public async Task<JsonResult> SaveDeductRates([FromBody] List<DeductRateModel> rates)
        {
            if (rates == null)
            {
                return Json(new { success = false, message = "Invalid Rate!" });
            }
            var result = await _iqCMasterRepository.SaveDeductRatesAsync(rates);
            return Json(new { success = result.status, message = result.message });
        }
        
        [HttpPost]
        public async Task<IActionResult> CheckDeductRates([FromBody] CheckDeductRateRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid request!" });
            }
            var result = await _iqCMasterRepository.CheckDeductRatesAsync(request);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data});
            }
            return Json(new { success = result.status, message = result.message });
        }
        
        [HttpPost]
        public async Task<JsonResult> GetQCMasterListByCode([FromBody] CodeRequest request)
        {
            if(request == null)
            {
                return Json(new { success = false, message = "Invalid request!" });
            }
            var result = await _iqCMasterRepository.GetQCMasterListByCodeAsync(request.code);
            if(result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }
        
        public class CodeRequest
        {
            public int code { get; set; }
        }

        [HttpGet]
        public async Task<JsonResult> CheckDeductRateExist(int code, int qcpCode)
        {
            if(code <= 0 || qcpCode <= 0)
            {
                return Json(new{success = false, message = "Invalid data!"});
            }
            var result = await _iqCMasterRepository.CheckDeductRateExistAsync(code, qcpCode);
            if (result.data)
            {
                return Json(new{success = result.status, exists = result.data});
            }
            return Json(new{success = result.status, exists = result.data, message = result.message });
        }

    }
}
