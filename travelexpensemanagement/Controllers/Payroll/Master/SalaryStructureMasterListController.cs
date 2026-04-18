using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Data;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class SalaryStructureMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;

        public SalaryStructureMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/SalaryStructureMasterList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetSalaryStructData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
             select distinct  pss.CODE,pss.DEPT_CODE,pss.DESG_CODE,isnull(dpm.NAME, '') Department,isnull(dgm.NAME, '') Designation ,pss.MAX_SALARY,pss.MIN_SALARY
			 from PAY_SALARY_STRUCTURE pss 
             left join DEPT_MAST dpm on pss.DEPT_CODE=dpm.CODE and pss.COMP_CODE=dpm.COMP_CODE
             left join DESG_MAST dgm on pss.DESG_CODE=dgm.CODE and pss.COMP_CODE=dgm.COMP_CODE		 
             WHERE pss.COMP_CODE = '{UsersessionDt.PubCompCode}' order by  Department,Designation ";
                var fullList = await _dbHelper.GetJsonDataAsync(strqry);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "Designation" };
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

        [HttpDelete]
        public async Task<IActionResult> DelSalaryStructMastDt(int Code)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_SalaryStructMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(Code));

                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        x = (int)cmd.Parameters["@ReturnVal"].Value;
                    }
                }

                if (x > 0)
                    return Json(new { status = true, message = "Data delete Successfully" });
                return Json(new { status = false, message = "Data delete failed" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data delete failed" });
            }
        }


    }
}
