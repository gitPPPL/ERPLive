using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    public class TypeMasterListController : Controller
    {      

        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        string yearPrefix, VNO;
        public TypeMasterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Master/TypeMasterList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetItemTypeData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
                select CODE,NAME,SHORTNAME,PTYPE,ACTIVE from ITEMPTYPE_MAST 
               WHERE COMP_CODE = '{UsersessionDt.PubCompCode}' order by NAME ";

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
        public async Task<IActionResult> DelTypeMasterDt(int Code)
        {
            try
            {
                int x;
                using(var con=_dbcontext.GetErpConnection())
                {
                    using(SqlCommand cmd=new SqlCommand("[dbo].[sp_ItemTypeMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(Code));                       
                        await con.OpenAsync();
                        x= await cmd.ExecuteNonQueryAsync();

                    }
                } 
                
                return Json(new { status = true, message = "Data delete successfully" });
                 
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data delete failed" });
            }
        }

    }
}
