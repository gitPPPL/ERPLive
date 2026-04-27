using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using System;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    public class SizeMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;      
        int x;
        public SizeMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/Inventory/Master/SizeMaster/Index.cshtml");
        }


        public class SizeMastModel
        {
            public int? code { get; set; }
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public decimal? Inch { get; set; }
            public decimal? MiliMtr { get; set; }
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
                        FROM ITEMSIZE_MAST 
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
        public async Task<IActionResult> SaveSizeMast([FromBody] SizeMastModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ItemSizeMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@Inch", _dbHelper.Xnull(model.Inch));
                        cmd.Parameters.AddWithValue("@MiliMtr", _dbHelper.Xnull(model.MiliMtr));
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

                if(x > 0)
                    return Json(new { status = true, message = "Data Save Successfully" });
                return Json(new { status = false, message = "Data Save Failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Save Failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetSizeDetailsById(string id)
        {
            try
            {
                string strqry = $@"
                select code,NAME,SHORTNAME,isnull(INCH, 0) INCH,ISNULL(Milimeter, 0) Milimeter,ACTIVE from ITEMSIZE_MAST  
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
        public async Task<IActionResult> UpdateSizeMast([FromBody] SizeMastModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ItemSizeMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@Inch", _dbHelper.Xnull(model.Inch));
                        cmd.Parameters.AddWithValue("@MiliMtr", _dbHelper.Xnull(model.MiliMtr));
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
                    return Json(new { status = true, message = "Data Save Successfully" });
                
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data update Failed" });
            }

        }


    }
}
