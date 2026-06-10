using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{

    [SessionAuthorize]
    public class LoomFabricStrengthEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly ILoomFabricStrengthEntryRepository _loomFabricStrengthEntry;
        public LoomFabricStrengthEntryController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, ILoomFabricStrengthEntryRepository loomFabricStrengthEntry)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
            _loomFabricStrengthEntry = loomFabricStrengthEntry;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Transaction/LoomFabricStrengthEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo()
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetMaxVNoAsync();

                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlaceMast()
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetPlaceMastAsync();

                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftList()
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetShiftListAsync();
                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserMast()
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetUserMastAsync();
                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLoomList(int placeCode = 0)
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetLoomListAsync(placeCode);

                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    message = "Data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProd2List(int loomCode = 0, int placeCode = 0, DateTime? vDate = null)
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetProd2ListAsync(loomCode, placeCode, vDate);

                return Json(new
                {
                    status = true,
                    data = data
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

        [HttpGet]
        public async Task<IActionResult> GetItemList(int itemCode = 0)
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetItemListAsync(itemCode);

                return Json(new
                {
                    status = true,
                    data = data
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

        [HttpGet]
        public async Task<IActionResult> GetColor()
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetColorAsync();

                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemType()
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetItemTypeAsync();

                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStrengthList(decimal minStd = 0, decimal maxStd = 0)
        {
            try
            {
                bool isExist = false;
                string strengthFilter = "";
                string strqry = "";
                var matchingCode = "";
                //if (minStd != 0 && maxStd != 0)
                //{
                strengthFilter = $" and  MIN_STD = {minStd} and MAX_STD = {maxStd} ";
                //}
                strqry = $@"select CODE, NAME from TENACITY_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode}
                               {strengthFilter} order by NAME";
                var itemlist1 = await _dbHelper.GetJsonDataAsync(strqry);

                //if (itemlist1.Count > 0)
                //{
                //    isExist = true;
                //    //dynamic first = itemlist1[0];
                //    //matchingCode = first.NAME;
                //    matchingCode = minStd+"-"+maxStd;
                //}

                if (itemlist1.Count > 0)
                {
                    isExist = true;

                    dynamic first = itemlist1[0];

                   // matchingCode = $"{minStd} - {maxStd}";
                   matchingCode = first.CODE.ToString();
                }
                else
                    isExist = false;

                strqry = $@"select CODE, NAME from TENACITY_MAST where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode}
                                order by NAME";
                var allList = await _dbHelper.GetJsonDataAsync(strqry);

                return Json(new
                {
                    status = true,
                    data = allList,
                    isExist = isExist,
                    matchingCode = matchingCode
                });

                //return Json(new { status = true, data = itemlist, isExist = isExist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLoomFabricSById(string id)
        {
            try
            {
                var result = await _loomFabricStrengthEntry
                    .GetLoomFabricSByIdAsync(id);

                return Json(new
                {
                    status = true,
                    header = result.Header,
                    detail = result.Detail
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateLoomFabricEntry([FromBody] LoomFabricEntryModel model)
        {
            if (model == null)
            {
                return Json(new
                {
                    status = false,
                    message = "Invalid request: Model is null."
                });
            }

            try
            {
                var result = await _loomFabricStrengthEntry.SaveOrUpdateLoomFabricEntryAsync(model);

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

        [HttpGet]
        public async Task<IActionResult> GetLastQCEntry()
        {
            try
            {
                var data = await _loomFabricStrengthEntry.GetLastQCEntryAsync();

                if (data == null)
                {
                    return Json(new
                    {
                        status = false,
                        message = "No previous record found"
                    });
                }

                return Json(new
                {
                    status = true,
                    data = data
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
