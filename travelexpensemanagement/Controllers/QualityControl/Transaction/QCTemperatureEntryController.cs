using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;


namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    [SessionAuthorize]
    public class QCTemperatureEntryController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly IQCTemperatureEntryRepository _qcTemperatureEntryRepository;

        public QCTemperatureEntryController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, 
            GlobalVariableService globalValue, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, DropdownService dropdownService, IQCTemperatureEntryRepository qCTemperatureEntryRepository)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _qcTemperatureEntryRepository = qCTemperatureEntryRepository;
        }
        public IActionResult Index()
        {
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;
            ViewBag.CompCode = compCode;
            return View("~/Views/QualityControl/Transaction/QCTemperatureEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetMaxVNo(string vType)
        {
            string docId = "";
            if (string.IsNullOrEmpty(vType))
            {
                return Json(new { status = false, message = "Invalid doc type" });
            }
            var result = _globalValidationdate.GetVNo(vType, "TAPE_QUALITY1");
            if(result != null)
            {
                docId = vType + result;
            }
            return Json(new { status = true, vNo = result, DocId = docId });
        }

        [HttpGet]
        public async Task<JsonResult> getExistOrNot(DateTime V_DATE, DateTime V_TIME,string SHIFT,int plantCode, int VNo=0)
        {
            if (V_DATE == default(DateTime) ||    V_TIME == default(DateTime) ||    string.IsNullOrWhiteSpace(SHIFT) ||    plantCode <= 0)
            {
                return Json(new { success = false, message = "Invalid data" });
            }
            var result = await _qcTemperatureEntryRepository.getExist(V_DATE, V_TIME, SHIFT, plantCode, VNo = 0);
            return Json(new { status = result.status, exists = result.data, message = result.message });
        }

        public JsonResult GetDropdown(string type, string VTypeId = "")
        {
            var gv = _globalValue.GetGlobalVariables();
            string query = "";
            switch (type)
            {
                case "Employee":
                    query = $@"SELECT CODE, CONCAT(CODE, ' || ', NAME) as  NAME ,DEPT_CODE
                            FROM EMP_MAST 
                            WHERE COMP_CODE = {gv.PubCompCode} 
                              AND ACTIVE = 1  and RESIGN_DATE is null
                            ORDER BY CODE
                    ";
                    break;
                case "Shift":
                    query = $@"
                        SELECT DISTINCT SHIFT AS CODE, SHIFT AS NAME 
                        FROM SHIFT_MAST 
                        WHERE COMP_CODE = {gv.PubCompCode} 
                        ORDER BY NAME
                    ";
                    break;
                case "Plant":
                    query = $@"
                        select CODE, NAME from ITEMDEPT_MAST where TRAN_TYPE='Production' and PLACE_TYPE IN ('Tapeline', 'Lamination') and COMP_CODE={gv.PubCompCode} order by NAME 
                    ";
                    break;
                case "Denier":
                    query = $@"
                            select CODE, NAME from TAPE_NFABRIC_MAST 
                            where COMP_CODE={gv.PubCompCode} 
                            order by NAME 
                    ";
                    break;
                case "Material":
                    query = $@"
                        SELECT ITEM_MAST.CODE, ITEM_MAST.NAME
                        FROM ITEM_MAST left join ITEM_GROUP
                        on ITEM_MAST.GROUP_CODE= ITEM_GROUP.CODE and ITEM_MAST.COMP_CODE= ITEM_GROUP.COMP_CODE
                        WHERE ITEM_MAST.COMP_CODE = {gv.PubCompCode} and ITEM_GROUP.SALE_GROUP= 'Raw'
                        order by ITEM_MAST.NAME ";
                    break;
                case "Winder":
                    query = $@"
                      select CODE, NAME from TAPE_QUALITY_MAST where V_TYPE = 'WIND' and COMP_CODE = {gv.PubCompCode}
                      order by NAME
                     ";
                    break;
                    //-----------------------------------------------------------------------------------------------------------------------
                case "Line":
                    query = $@"
                      select code,name from ITEMDEPT_MAST where comp_code={gv.PubCompCode} and TRAN_TYPE='Production' and PLACE_TYPE='Fibreline' order by name
                     ";
                    break;
                case "TestParameter":
                    query = $@"
                      select code, name from TAPE_QUALITY_MAST where V_TYPE = 'ROOM' and COMP_CODE = {gv.PubCompCode}
              order by SORT_NO
                     ";
                    break;

            }
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetPlantZoneList()
        {
            try
            {

            var plantlist = await _dbHelper.GetJsonDataAsync(@$"
              select CODE, NAME from TAPE_QUALITY_MAST where V_TYPE = 'ROOM' and COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
              order by SORT_NO
            ");
                return Json(new { status = true, data = plantlist });
            }
            catch(Exception ex)
            {
                return Json(new
                {
                    status = true,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetScrewList()
        {
            try
            {
                var screwlist = await _dbHelper.GetJsonDataAsync(@$"
              select CODE, NAME from TAPE_QUALITY_MAST where V_TYPE = 'SPED' and COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}
              order by SORT_NO
             ");
                return Json(new { status = true, data = screwlist });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = true,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetQcTemperatureById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { status = false, message = "Invalid Id" }); 
            }
            var result = await _qcTemperatureEntryRepository.GetById(id);
            if(result.data != null)
            {
                return Json(new { status = result.status, header = result.data.Header, detail = result.data.Detail});
            }
            return Json(new { status = result.status, message = result.message });
        }

        //===Validate VDate
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("WB1", vdate, vtype, vno);
            return Ok(result);
        }

        //===Check Modification Days
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
        public async Task<IActionResult> SaveOrUpdateQcTemperatureEntry([FromBody] QcTemperature model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request: Model is null." });
            var result = await _qcTemperatureEntryRepository.saveOrUpdate(model);
            return Json(new { status = result.status, message = result.message });
        }

        [HttpGet]
        public async Task<JsonResult> ImportDataByReading(int timeInterval, string type, string shift, int deptCode, string vType)
        {
            if (timeInterval <= 0 || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(shift) || deptCode <= 0 || string.IsNullOrWhiteSpace(vType))
            {
                return Json(new { success = false, message = "Invalid data"});
            }
            var result = await _qcTemperatureEntryRepository.ImportDataByReading(timeInterval, type, shift, deptCode, vType);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public async Task<IActionResult> FillDataByLineNo(int deptCode)
        {
            if (deptCode <= 0)
            {
                return Json(new { success = false, message = "Invalid department code." });
            }
            var result = await _qcTemperatureEntryRepository.FillDataByLineNo(deptCode);
            if (result.status)
            {
                return Json(new { success = true, data = result.data });
            }
            return Json(new { success = false, message = result.message });
        }
    }
}
  