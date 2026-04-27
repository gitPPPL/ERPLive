using System.Data;
using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class ParameterMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public ParameterMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/ParameterMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> GetUnit()
        {
            try
            {
                var colorTypeList = await _dbHelper.GetJsonDataAsync(" select distinct CODE, NAME from ITEMUNIT_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "'  order by NAME ");
                return Json(new { status = true, data = colorTypeList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        public class ParameterModel
        {
            public int? code { get; set; }        
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public int? QUnitCd { get; set; }
            public int? Qty { get; set; }
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
                        FROM QCP_MAST 
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
        public async Task<IActionResult> SaveQParamMast([FromBody] ParameterModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QualityParameterMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));                     
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@QUnitCd", _dbHelper.Xnull(model.QUnitCd));
                        cmd.Parameters.AddWithValue("@Qty", _dbHelper.Xnull(model.Qty));
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
        public async Task<IActionResult> GetQParameterDetailsById(string id)
        {
            try
            {
                string strqry = $@"
               select distinct  CODE,NAME,SHORTNAME,QUNIT_CODE,QTY,ACTIVE from QCP_MAST
                WHERE COMP_CODE = '{_globalValue.GetGlobalVariables().PubCompCode}' and code={id} ";
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
        public async Task<IActionResult> UpdateQParameterMast([FromBody] ParameterModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QualityParameterMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@QUnitCd", _dbHelper.Xnull(model.QUnitCd));
                        cmd.Parameters.AddWithValue("@Qty", _dbHelper.Xnull(model.Qty));
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
                    return Json(new { status = true, message = "Data update Successfully" });
                return Json(new { status = false, message = "Data update failed" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data update Failed" });
            }

        }



    }
}
