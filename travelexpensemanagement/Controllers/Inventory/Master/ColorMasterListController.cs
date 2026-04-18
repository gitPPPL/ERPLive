using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    public class ColorMasterListController : Controller
    {

        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        string yearPrefix, VNO;
        public ColorMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Master/ColorMasterList/Index.cshtml");
        }
 
        [HttpGet]
        public async Task<IActionResult> GetColorMastList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
                select distinct cm.CODE,isnull(cgm.name, '') as COLOR_GROUP,cm.NAME,cm.SHORTNAME,cm.CTYPE,cm.ACTIVE from COLOR_MAST cm left join COLORGROUP_MAST cgm on cm.COLOR_GROUP=cgm.code and cm.COMP_CODE=cgm.COMP_CODE
                WHERE cm.COMP_CODE = '{UsersessionDt.PubCompCode}' order by NAME ";
                var fullList = await _dbHelper.GetJsonDataAsync(strqry);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "NAME" };
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
        public async Task<IActionResult> DelColorMast(int Code)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ColorMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(Code));
                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.ReturnValue
                        };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        x = (int)cmd.Parameters["@ReturnVal"].Value;
                    }
                }
                if (x > 0)
                    return Json(new { status = true, message = "Data delete successfully" });
                return Json(new { status = false, message = "data delete failed" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data delete failed" });
            }
        }


    }
}
