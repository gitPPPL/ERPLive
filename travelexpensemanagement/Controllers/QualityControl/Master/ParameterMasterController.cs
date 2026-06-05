using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class ParameterMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DropdownService _dropdownService;
        private readonly LogService.LogService _logService;


        int x;
        public ParameterMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, DropdownService dropdownService, LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _dropdownService = dropdownService;
            _logService = logService;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/ParameterMaster/Index.cshtml");
        }

        //[HttpGet]
        //public async Task<JsonResult> GetUnit()
        //{
        //    try
        //    {
        //        var colorTypeList = await _dbHelper.GetJsonDataAsync(" select distinct CODE, NAME from ITEMUNIT_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "'  order by NAME ");
        //        return Json(new { status = true, data = colorTypeList });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = "Data load failed" });
        //    }
        //}
        public JsonResult GetDropdown(string type)
        {
            string query = "";
            switch (type)
            {
                case "QCUnit":
                    query = $@"Select CODE, NAME from QCPUNIT_MAST where Active=1 Order by Name";
                    break;
            }
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
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
                    using (SqlCommand cmd = new SqlCommand("sp_QualityParameterMast_AED"))
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@AED", "Exist");
                        cmd.Parameters.AddWithValue("@Name", inputData);
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
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
                int code = 0;
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
                        cmd.Parameters.AddWithValue("@wsid", usersessionDt.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@lid", Environment.MachineName);

                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.ReturnValue
                        };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        //await cmd.ExecuteNonQueryAsync();
                        code = Convert.ToInt32(cmd.ExecuteScalar());
                        x = (int)cmd.Parameters["@ReturnVal"].Value;

                    }
                }

                if (x > 0)
                {
                    //===========log insert
                    _logService.InsertLog("QCP_MAST", "QC Parameter Master", "Master", "INSERT", "", code.ToString(), null);
                    return Json(new { status = true, message = "Data Save Successfully" });
                }
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
            var gv = _globalValue.GetGlobalVariables();
            var data = new ParameterModel();
            try
            {
                using(SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using(SqlCommand cmd = new SqlCommand("sp_QualityParameterMast_AED", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "GetById");
                        cmd.Parameters.AddWithValue("@companyCd", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", id);

                        await con.OpenAsync();
                        using(SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if(await reader.ReadAsync())
                            {
                                data.code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : null;
                                data.Name = reader["NAME"]?.ToString();
                                data.ShortName = reader["SHORTNAME"]?.ToString();
                                data.QUnitCd = reader["QUNIT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["QUNIT_CODE"]) : null;
                                data.Qty = reader["QTY"] != DBNull.Value ? Convert.ToInt32(reader["QTY"]) : null;
                                data.active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : null;
                            }
                        }
                        return Json(new { status = true, data = data });
                    }
                }
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
                        cmd.Parameters.AddWithValue("@wsid", usersessionDt.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@lid", Environment.MachineName);

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
                {
                    //===========log insert
                    _logService.InsertLog("QCP_MAST", "QC Parameter Master", "Master", "UPDATE", "", model.code.ToString(), null);
                    return Json(new { status = true, message = "Data update Successfully" });
                }
                return Json(new { status = false, message = "Data update failed" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data update Failed" });
            }

        }

    }
}
