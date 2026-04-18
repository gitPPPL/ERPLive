using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class TapeAndFabricMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public TapeAndFabricMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/TapeAndFabricMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> GetColorName()
        {
            try
            {
                var colorNameList = await _dbHelper.GetJsonDataAsync(" select distinct code, Name from COLOR_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "'  order by Name ");
                return Json(new { status = true, data = colorNameList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetMeshName()
        {
            try
            {
                var MeshNameList = await _dbHelper.GetJsonDataAsync(" select distinct code, Name from MESH_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "'  order by Name ");
                return Json(new { status = true, data = MeshNameList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        public class TapeNFabricModel
        {          
            public int? Code { get; set; }
            public string? Name { get; set; }
            public int? MeshCode { get; set; }
            public decimal? StdGram { get; set; }
            public decimal? MinGram { get; set; }
            public decimal? MaxGram { get; set; }
            public decimal? Gsm { get; set; }
            public decimal? Denier { get; set; }
            public string? UnitName { get; set; }
            public int? ColorCode { get; set; }
            public decimal? Width { get; set; }
            public decimal? Gpd { get; set; }
            public decimal? MinGpd { get; set; }
            public decimal? MaxGpd { get; set; }
            public decimal? StdStrength { get; set; }
            public decimal? StrengthMax { get; set; }
            public decimal? StrengthMin { get; set; }
            public decimal? StdElong { get; set; }
            public decimal? ElongMax { get; set; }
            public decimal? ElongMin { get; set; }
            public decimal? UnlamFab { get; set; }
            public decimal? LamFab { get; set; }
            public int? Active { get; set; }
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
                        FROM TAPE_NFABRIC_MAST 
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
        public async Task<IActionResult> SaveTape_NFabricMast([FromBody] TapeNFabricModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeNFabricMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);                        
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@MESH_CODE", _dbHelper.Xnull(model.MeshCode));
                        cmd.Parameters.AddWithValue("@STD_GRAM", _dbHelper.Vnull(model.StdGram));
                        cmd.Parameters.AddWithValue("@MIN_GRAM", _dbHelper.Vnull(model.MinGram));
                        cmd.Parameters.AddWithValue("@MAX_GRAM", _dbHelper.Vnull(model.MaxGram));
                        cmd.Parameters.AddWithValue("@GSM", _dbHelper.Vnull(model.Gsm));
                        cmd.Parameters.AddWithValue("@DENIER", _dbHelper.Vnull(model.Denier));
                        cmd.Parameters.AddWithValue("@UNIT_NAME", _dbHelper.Xnull(model.UnitName));
                        cmd.Parameters.AddWithValue("@COLOR_CODE", _dbHelper.Xnull(model.ColorCode));
                        cmd.Parameters.AddWithValue("@WIDTH", _dbHelper.Vnull(model.Width));
                        cmd.Parameters.AddWithValue("@GPD", _dbHelper.Vnull(model.Gpd));
                        cmd.Parameters.AddWithValue("@MIN_GPD", _dbHelper.Vnull(model.MinGpd));
                        cmd.Parameters.AddWithValue("@MAX_GPD", _dbHelper.Vnull(model.MaxGpd));
                        cmd.Parameters.AddWithValue("@STD_STRENGTH", _dbHelper.Vnull(model.StdStrength));
                        cmd.Parameters.AddWithValue("@STRENGTH_MAX", _dbHelper.Vnull(model.StrengthMax));
                        cmd.Parameters.AddWithValue("@STRENGTH_MIN", _dbHelper.Vnull(model.StrengthMin));
                        cmd.Parameters.AddWithValue("@STD_ELONG", _dbHelper.Vnull(model.StdElong));
                        cmd.Parameters.AddWithValue("@ELONG_MAX", _dbHelper.Vnull(model.ElongMax));
                        cmd.Parameters.AddWithValue("@ELONG_MIN", _dbHelper.Vnull(model.ElongMin));
                        cmd.Parameters.AddWithValue("@UNLAM_FAB", _dbHelper.Vnull(model.UnlamFab));
                        cmd.Parameters.AddWithValue("@LAM_FAB", _dbHelper.Vnull(model.LamFab));
                        cmd.Parameters.AddWithValue("@ACTIVE", _dbHelper.Xnull(model.Active));
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
 
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
        public async Task<IActionResult> GetTape_NFabricDetailsById(string id)
        {
            try
            {
                string strqry = $@"
             	SELECT DISTINCT tnf.CODE, tnf.NAME, tnf.MESH_CODE, tnf.STD_GRAM, tnf.MIN_GRAM, tnf.MAX_GRAM, tnf.GSM, tnf.DENIER, tnf.UNIT_NAME, tnf.COLOR_CODE, tnf.WIDTH, tnf.GPD, tnf.MIN_GPD, tnf.MAX_GPD, tnf.STD_STRENGTH, tnf.STRENGTH_MAX, tnf.STRENGTH_MIN, tnf.STD_ELONG, tnf.ELONG_MAX, tnf.ELONG_MIN, tnf.UNLAM_FAB, tnf.LAM_FAB, tnf.ACTIVE, tnf.UUSER, tnf.UDATE, tnf.AED, tnf.LIP, tnf.LID 
                FROM TAPE_NFABRIC_MAST tnf LEFT JOIN COLOR_MAST cm ON cm.CODE = tnf.COLOR_CODE LEFT JOIN MESH_MAST mm ON mm.CODE = tnf.MESH_CODE WHERE tnf.COMP_CODE = '{_globalValue.GetGlobalVariables().PubCompCode}' and tnf.code={id} ORDER BY tnf.NAME "; 
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
        public async Task<IActionResult> UpdateTape_NFabricMast([FromBody] TapeNFabricModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeNFabricMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.Code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@MESH_CODE", _dbHelper.Xnull(model.MeshCode));
                        cmd.Parameters.AddWithValue("@STD_GRAM", _dbHelper.Vnull(model.StdGram));
                        cmd.Parameters.AddWithValue("@MIN_GRAM", _dbHelper.Vnull(model.MinGram));
                        cmd.Parameters.AddWithValue("@MAX_GRAM", _dbHelper.Vnull(model.MaxGram));
                        cmd.Parameters.AddWithValue("@GSM", _dbHelper.Vnull(model.Gsm));
                        cmd.Parameters.AddWithValue("@DENIER", _dbHelper.Vnull(model.Denier));
                        cmd.Parameters.AddWithValue("@UNIT_NAME", _dbHelper.Xnull(model.UnitName));
                        cmd.Parameters.AddWithValue("@COLOR_CODE", _dbHelper.Xnull(model.ColorCode));
                        cmd.Parameters.AddWithValue("@WIDTH", _dbHelper.Vnull(model.Width));
                        cmd.Parameters.AddWithValue("@GPD", _dbHelper.Vnull(model.Gpd));
                        cmd.Parameters.AddWithValue("@MIN_GPD", _dbHelper.Vnull(model.MinGpd));
                        cmd.Parameters.AddWithValue("@MAX_GPD", _dbHelper.Vnull(model.MaxGpd));
                        cmd.Parameters.AddWithValue("@STD_STRENGTH", _dbHelper.Vnull(model.StdStrength));
                        cmd.Parameters.AddWithValue("@STRENGTH_MAX", _dbHelper.Vnull(model.StrengthMax));
                        cmd.Parameters.AddWithValue("@STRENGTH_MIN", _dbHelper.Vnull(model.StrengthMin));
                        cmd.Parameters.AddWithValue("@STD_ELONG", _dbHelper.Vnull(model.StdElong));
                        cmd.Parameters.AddWithValue("@ELONG_MAX", _dbHelper.Vnull(model.ElongMax));
                        cmd.Parameters.AddWithValue("@ELONG_MIN", _dbHelper.Vnull(model.ElongMin));
                        cmd.Parameters.AddWithValue("@UNLAM_FAB", _dbHelper.Vnull(model.UnlamFab));
                        cmd.Parameters.AddWithValue("@LAM_FAB", _dbHelper.Vnull(model.LamFab));
                        cmd.Parameters.AddWithValue("@ACTIVE", _dbHelper.Xnull(model.Active));
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);

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
