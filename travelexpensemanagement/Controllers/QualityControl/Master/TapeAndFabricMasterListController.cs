using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class TapeAndFabricMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
       
        public TapeAndFabricMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/TapeAndFabricMasterList/Index.cshtml");
        }
        [HttpGet]
        public async Task<IActionResult> GetTape_FabricList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
               	SELECT DISTINCT tnf.CODE, tnf.NAME, mm.NAME AS MESH_CODE, tnf.STD_GRAM, tnf.MIN_GRAM, tnf.MAX_GRAM, tnf.GSM, tnf.DENIER, tnf.UNIT_NAME, isnull(cm.NAME, '') AS COLOR_CODE, tnf.WIDTH, tnf.GPD, tnf.MIN_GPD, tnf.MAX_GPD, tnf.STD_STRENGTH, tnf.STRENGTH_MAX, tnf.STRENGTH_MIN, tnf.STD_ELONG, tnf.ELONG_MAX, tnf.ELONG_MIN, tnf.UNLAM_FAB, tnf.LAM_FAB, tnf.ACTIVE, tnf.UUSER, tnf.UDATE, tnf.AED, tnf.LIP, tnf.LID 
                FROM TAPE_NFABRIC_MAST tnf LEFT JOIN COLOR_MAST cm ON cm.CODE = tnf.COLOR_CODE LEFT JOIN MESH_MAST mm ON mm.CODE = tnf.MESH_CODE WHERE tnf.COMP_CODE = '{UsersessionDt.PubCompCode}' ORDER BY tnf.NAME ";

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
        public async Task<IActionResult> DelTape_FabricMast(int Code)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeNFabricMast_AED]", con))
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
