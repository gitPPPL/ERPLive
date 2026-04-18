using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Purchase.Master
{
    public class POMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        string yearPrefix, VNO;
        public POMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }


        public IActionResult Index()
        {
            return View("~/Views/Purchase/Master/POMasterList/Index.cshtml");
        }

      
        [HttpGet]
        public async Task<IActionResult> GetPOMastData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
            SELECT V_NO, V_TYPE, DOC_ID, V_DATE, FROM_DATE, TO_DATE, 
                   STORE_AMT, CAPITAL_AMT, REMARK, FAPROV_REMARKS, 
                   FAPROV_STATUS, STATUS 
            FROM PO_MAST 
            WHERE COMP_CODE = '{UsersessionDt.PubCompCode}' 
              AND BRANCH_CODE = 1 
              AND YEAR_CODE = '{UsersessionDt.PubFYearCode}' 
              AND V_TYPE = 'POMT'
        ";

                var fullList = await _dbHelper.GetJsonDataAsync(strqry);              
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>) x;                           
                            string[] searchableKeys = { "DOC_ID" };
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
