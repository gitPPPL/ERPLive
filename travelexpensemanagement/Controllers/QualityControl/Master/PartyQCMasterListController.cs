using System.Data;
using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class PartyQCMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public PartyQCMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/PartyQCMasterList/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyQCList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"               
SELECT pm.CODE,  isnull(sg.NAME, '') AS PARTY, isnull(qcm.NAME, '') AS QC_Name, isnull(qcp.NAME, '') AS QC_Parameter, pm.QCP_CODE, pm.UNIT_CODE, pm.STD, pm.FROM_RESULT, pm.TO_RESULT
FROM PARTY_QCMAST pm
LEFT JOIN SUBGROUP_MAST sg ON pm.PARTY_CODE = sg.CODE AND pm.COMP_CODE = sg.COMP_CODE
LEFT JOIN QC_MAST qcm ON pm.QC_CODE = qcm.CODE AND pm.COMP_CODE = qcm.COMP_CODE
LEFT JOIN QCP_MAST qcp ON pm.QCP_CODE = qcp.CODE AND pm.COMP_CODE = qcp.COMP_CODE 
                WHERE pm.COMP_CODE = '{UsersessionDt.PubCompCode}' order by PARTY ";
                var fullList = await _dbHelper.GetJsonDataAsync(strqry);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "PARTY" };
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
        public async Task<IActionResult> DelPartyQCMast(int Code)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PartyQCMast_AED]", con))
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
