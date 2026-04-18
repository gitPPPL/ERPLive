using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class GatePassListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly GlobalVariableService _globalValue;
        public GatePassListController(travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/GatePassList/Index.cshtml");
        }
          
        [HttpGet]
        public async Task<IActionResult> GetGatePassList(string CDate, string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var fullList = await _dbHelper.GetJsonDataAsync($@"
                select V_type,V_no,V_date,Shift,Emp_code,Emp_Name,Dept_name,GP_TYPE,GP_NO,HOD_NAME,REMARKS,E_TIME,IN_TIME 
                from PAY_INOUT where V_type='OUT' and COMP_CODE={usersession.PubCompCode} and v_date='{CDate}' 
                ");

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "Emp_code" };
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

        //query = " Update PAY_INOUT SET E_TIME=@E_TIME,IN_TIME=@IN_TIME,Remarks=@Remarks where V_TYPE='OUT' and V_NO=@V_NO " &
        //            " and EMP_CODE=@EMP_CODE and COMP_CODE=@COMP_CODE and BRANCH_CODE=@BRANCH_CODE and YEAR_CODE=@YEAR_CODE"


    }
}
