using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    public class CostMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public CostMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            //return View();
            return View("~/Views/Admin/Setup/CostMaster/Index.cshtml");
        }


        [HttpGet]
        public async Task<JsonResult> GetACpayableName()
        {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables(); 
                var AC_PayableNm = await _dbHelper.GetJsonDataAsync(" select code, name from  SUBGROUP_MAST where NATURE in ('cash', 'bank') and COMP_CODE='5' order by name ");

                return Json(new { status = true, data = AC_PayableNm });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
        }
    }



    }

}
