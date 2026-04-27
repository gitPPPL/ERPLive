using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class DiwaliListCreationController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly GlobalVariableService _globalValue;
        public DiwaliListCreationController(DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/DiwaliListCreation/Index.cshtml");
        }
         
        [HttpGet]
        public async Task<IActionResult> GetDiwaliBonusList(string CDate, string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();               
                var fullList = await _dbHelper.GetJsonDataAsync($@"
                select  PAY_DIWALI.V_DATE, PAY_DIWALI.EMP_CODE ,EMP_MAST.type, EMP_MAST.NAME[EMP_NAME], 
DESG_MAST.NAME[DESG_NAME],EMP_MAST.JOIN_DATE,  WORK_MNTH, PAVG, WAGES,	PERC, AMOUNT,
PAY_AMT, COND, PAY_DIWALI.UUSER ,PAY_DIWALI.UDATE,SNO    
FROM PAY_DIWALI LEFT JOIN EMP_MAST ON EMP_MAST.CODE = PAY_DIWALI.EMP_CODE 
AND PAY_DIWALI.COMP_CODE = EMP_MAST.COMP_CODE LEFT JOIN DESG_MAST ON DESG_MAST.CODE = EMP_MAST.DESG_CODE
AND EMP_MAST.COMP_CODE = DESG_MAST.COMP_CODE   
WHERE  PAY_DIWALI.V_DATE = '{CDate}' AND  PAY_DIWALI.COMP_CODE= {usersession.PubCompCode} AND PAY_DIWALI.BRANCH_CODE =1
AND PAY_DIWALI.YEAR_CODE = {usersession.PubFYearCode} and PAY_DIWALI.COND<>99 
ORDER BY EMP_MAST.type,EMP_MAST.join_date,emp_mast.code  
                ");

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "EMP_CODE" };
                            return searchableKeys.Any(key =>
                                dict.ContainsKey(key) &&
                                dict[key]?.ToString().ToLower().Contains(searchTerm) == true
                            );
                        })
                        .ToList();
                }
                var totalCount = fullList.Count;
                var pagedList = fullList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new { status = true, data = pagedList, totalCount });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

    }
}
