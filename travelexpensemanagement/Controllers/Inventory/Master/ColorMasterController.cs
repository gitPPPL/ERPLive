using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    public class ColorMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public ColorMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/Inventory/Master/ColorMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> GetColorGrpName()
        {
            try
            {
                var colorGrpList =await _dbHelper.GetJsonDataAsync("select CODE,  isnull(NAME, '') NAME from COLORGROUP_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "' and isnull(NAME, '')<>'' order by NAME ");
                return Json(new { status = true, data = colorGrpList });

            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetColorType()
        {
            try
            {
                var colorTypeList = await _dbHelper.GetJsonDataAsync("select  distinct isnull(COLOR_TYPE, '') ColorType from COLORGROUP_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "' and isnull(COLOR_TYPE, '')<>'' order by ColorType ");
                return Json(new { status = true, data = colorTypeList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        public class ColorModel
        {
            public int? code { get; set; }
            public int? ColorGroup { get; set; }
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public string? CType { get; set; } 
            public int? active { get; set; }
        }

        [HttpGet]
        public JsonResult getExistOrNot(string inputData)
        {
            try
            {
                bool isExist = false;

                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = @"
                         SELECT CASE 
                        WHEN EXISTS (
                        SELECT 1 
                        FROM COLOR_MAST 
                        WHERE UPPER(ISNULL(NAME, '')) = UPPER(@Inputdata) 
                        AND COMP_CODE = @CompCode
                        ) 
                        THEN 1 ELSE 0 
                        END";

                        cmd.Parameters.AddWithValue("@Inputdata", inputData);
                        cmd.Parameters.AddWithValue("@CompCode", _globalValue.GetGlobalVariables().PubCompCode);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }

                return Json(new { status = true, exists = isExist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data check failed: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveColorMast([FromBody] ColorModel model)
        {
            try
            {

                if (model == null)
                {
                    return Json(new { status = false, message = "Data Save Failed" });
                }

                using (var con = _dbcontext.GetErpConnection())
                {
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ColorMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));
                        cmd.Parameters.AddWithValue("@ColorGrp", _dbHelper.Xnull(model.ColorGroup));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@CType", _dbHelper.Xnull(model.CType)); 
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.active));
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);

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
                    return Json(new { status = true, message = "Data Save Successfully" });
                return Json(new { status = false, message = "Data Save Failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Save Failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetColorDetailsById(string id)
        {
            try
            {
                string strqry = $@"
                select distinct cm.CODE,isnull(cm.COLOR_GROUP, '') as COLOR_GROUP,cm.NAME,cm.SHORTNAME,cm.CTYPE,cm.ACTIVE from COLOR_MAST cm  
                WHERE cm.COMP_CODE = '{_globalValue.GetGlobalVariables().PubCompCode}' and cm.code={id} ";
                var data = await _dbHelper.GetJsonDataAsync(strqry);
                if (data.Count > 0)
                    return Json(new { status = true, data = data[0] });

                return Json(new { status = false, message = "Not found" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> UpdateColorMast([FromBody] ColorModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { status = false, message = "Data update Failed" });
                }

                using (var con = _dbcontext.GetErpConnection())
                {
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ColorMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));
                        cmd.Parameters.AddWithValue("@ColorGrp", _dbHelper.Xnull(model.ColorGroup));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@CType", _dbHelper.Xnull(model.CType));
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.active));
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);

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
                if(x>0)
                    return Json(new { status = true, message = "Data Save Successfully" });
                return Json(new { status = false, message = "Data Save failed" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data update Failed" });
            }

        }



    }
}
