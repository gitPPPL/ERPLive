using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class ShiftMasterListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;

        public ShiftMasterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/ShiftMasterList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
             select sm.code,isnull(sm.SHIFT,'') as ShiftName,isnull(dm.NAME, '') as Designation,sm.DESG_CODE,sm.SHIFT_STARTTIME,sm.SHIFT_ENDTIME, sm.FROM_START,sm.TO_START,sm.OT_GRACE,sm.LATE_GRACE 
             from SHIFT_MAST sm left join DESG_MAST dm on sm.DESG_CODE=dm.CODE and sm.COMP_CODE=dm.COMP_CODE 
             WHERE sm.COMP_CODE = '{UsersessionDt.PubCompCode}' order by ShiftName ";
                var fullList = await _dbHelper.GetJsonDataAsync(strqry);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "ShiftName" };
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
        public async Task<IActionResult> DelShiftMastDt(int Code)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ShiftMast_AED]", con))
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
